using System;
using System.Threading;
using System.Threading.Tasks;
using FTD2XX_NET;


//полностью автономный класс для опроса через ftdi
namespace ftdicomm
{
    enum CoefficientName : byte
    {
        kap = 1, kbp, kcp, kdp,
        kat, kbt, kct,
        sm_p,
        numdev,
        kep3
    }
    struct Options
    {
        public byte address;
        public byte type;

        public Options(byte address, byte type)
        {
            this.address = address;
            this.type = type;
        }
    }

    struct Outcome
    {
        public float pressureSI;
        public float temperatureSI;
        public ushort pressureADC;
        public ushort temperatureADC;

        public Outcome(float pressureSI = 0f, float temperatureSI = 0f, ushort pressureADC = 0, ushort temperatureADC = 0)
        {
            this.pressureSI = pressureSI;
            this.temperatureSI = temperatureSI;
            this.pressureADC = pressureADC;
            this.temperatureADC = temperatureADC;
        }

        public override string ToString()
        {
            return $"ADC: P {pressureADC}, T {temperatureADC}\nSI: P {pressureSI} kgs/cm2, T {temperatureSI} C";
        }
    }

    class Device
    {
        private FTDI ftdi;
        private byte[] data;
        private byte[] buffer;
        private readonly string DESCRIPTION; 
        private readonly string SERIAL_NUMBER;

        private FTDI.FT_STATUS status;
        private uint devCount;
        private FTDI.FT_DEVICE_INFO_NODE[] deviceList;


        private object locker;
        public event Action<Outcome> Readed;

        #region Internal Values
        private float pressureSI = 0f;
        private float temperatureSI = 0f;
        
        private ushort pressureADC = 0;
        private ushort temperatureADC = 0;
        #endregion

        private int testInt = 0;

        public Device() // выполнять базовый констр в спец констре.
        {
            data = new byte[8];
            buffer = new byte[200];
            ftdi = new FTDI();
            devCount = 0;
            locker = new object();
        }
        
        public Device(string description, string serialNumber) : this()
        {
            this.DESCRIPTION = description;
            this.SERIAL_NUMBER = serialNumber;
            Open();
        }

        #region MainFunctions
        public void ReadADC(byte address)
        {
            
            Options opt = new Options(address, 0);
            var task = new Task<Outcome>(ReadHandler, opt);
            task.Start();
        }

        public void ReadSI(byte address)
        {
            Options opt = new Options(address, 1);
            var task = new Task<Outcome>(ReadHandler, opt);
            task.Start();
        }
        
        public void Cycle(int count = 0)
        {
            for (int i = 0; i < count; i++)
            {
                this.ReadSI(8);
            }
        }

        public void ReadCoefficient(byte address)
        { }
        public byte WriteCoefficient(byte address, CoefficientName name, float value)
        {
            data[0] = (byte)(address + 0x10);
            data[1] = (byte)name;
            byte[] dataLimited = BitConverter.GetBytes(value);
            data[2] = dataLimited[0];
            data[3] = dataLimited[1];
            data[4] = dataLimited[2];
            data[5] = dataLimited[3];
            // sender guy thread support
            Thread thread = new Thread(DataExchange);
            thread.Start(250);
            thread.Join();

            return 0;
        }
        #endregion
        #region Additional function

        public void Open()
        {
            status = ftdi.GetNumberOfDevices(ref this.devCount);
            CheckStatus(status);
            if (this.devCount == 0)
            {
                throw new Exception($"No connected devices!");
            }
            this.deviceList = new FTDI.FT_DEVICE_INFO_NODE[this.devCount];
            status = ftdi.OpenByDescription(DESCRIPTION);
            CheckStatus(status);
            Initialization();
            Purge();
            if (ResetAVR() != 80)
            {
                throw new FTDI.FT_EXCEPTION("Error: Reset AVR");
            }
        }

        private uint ResetAVR()
        {
            byte[] resetData = new byte[80];
            EncDec.FillByteArray(ref resetData, 0x01);
            uint written = 0;
            ftdi.Write(resetData, resetData.Length, ref written);
            Thread.Sleep(100);
            return written;
        }

        private void Initialization()
        {
            status = ftdi.SetBitMode(0xDF, FTDI.FT_BIT_MODES.FT_BIT_MODE_SYNC_BITBANG);
            CheckStatus(status);
            status = ftdi.SetBaudRate(200000);
            CheckStatus(status);
            status = ftdi.SetLatency(5);
            CheckStatus(status);
            status = ftdi.SetTimeouts(250, 250);
            CheckStatus(status);
        }

        private void CheckStatus(FTDI.FT_STATUS status)
        {
            if (status != FTDI.FT_STATUS.FT_OK)
            {
                FTDI.FT_EXCEPTION ex = new FTDI.FT_EXCEPTION($"Error: {status.ToString()}");
                throw ex;
            }
        }

        private Outcome ReadHandler(object options)
        {
            lock (locker)
            {
                Options opt = (Options)options;
                data[0] = 0x1A;
                data[1] = 0x00;
                data[2] = opt.address;
                data[3] = opt.type;
                data[4] = 0x55;
                data[5] = 0xAA;
                data[6] = 0xFF;
                data[7] = 0xFF;

                DataExchange(250);
                data[0] = 0x22;
                DataExchange(20);

                byte[] decodedData = new byte[200];
                decodedData = EncDec.DecodeData(buffer);
                Outcome outt;
                switch (opt.type)
                {
                    case 0:
                        EncDec.CodeToADC(decodedData, out this.pressureADC, out this.temperatureADC);
                        outt = new Outcome(pressureADC: this.pressureADC, temperatureADC: this.temperatureADC);
                        break;
                    case 1:
                        EncDec.CodeToSI(decodedData, out this.pressureSI, out this.temperatureSI);
                        outt = new Outcome(pressureSI: this.pressureSI, temperatureSI: this.temperatureSI);
                        break;
                    default:
                        outt = new Outcome();
                        break;
                }
                Readed(outt);
                return outt; 
            }
        }

        private void DataExchange(object timeToSleep)
        {
            uint numBytesWritten = 0;
            uint numBytesRead = 0;
            byte[] encodedDataToWrite = EncDec.EncodeData(data);

            Purge();
            ftdi.Write(encodedDataToWrite, encodedDataToWrite.Length, ref numBytesWritten);
            ftdi.Read(buffer, (uint)buffer.Length, ref numBytesRead);
            Thread.Sleep((int)timeToSleep);
        }

        private void DataExchange(int timeToSleep)
        {
            uint numBytesWritten = 0;
            uint numBytesRead = 0;
            byte[] encodedDataToWrite = EncDec.EncodeData(data);

            Purge();
            ftdi.Write(encodedDataToWrite, encodedDataToWrite.Length, ref numBytesWritten);
            ftdi.Read(buffer, (uint)buffer.Length, ref numBytesRead);
            Thread.Sleep(timeToSleep);
        }

        private void Purge()
        {
            this.ftdi.Purge(FTDI.FT_PURGE.FT_PURGE_RX);
            this.ftdi.Purge(FTDI.FT_PURGE.FT_PURGE_TX);
        }
        #endregion
    }
}
