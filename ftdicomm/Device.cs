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

        private int testInt = 0;

        public Device() // выполнять базовый констр в спец констре.
        {
            data = new byte[8];
            buffer = new byte[200];
            ftdi = new FTDI();
            devCount = 0;
        }
        
        public Device(string description, string serialNumber) : this()
        {
            this.DESCRIPTION = description;
            this.SERIAL_NUMBER = serialNumber;
            Open();
        }

        #region MainFunctions
        public void ReadADC()
        {
            Task task = new Task(ReaderHandler);
            //public void RequestData(byte address, byte type)        // запрос данных о давлении или температуре
            //{                                                       // адрес прибора, тип - ацп (0) или si (1)
            //    dataIn = new byte[200];

            //    dataOut[0] = 0x1A;
            //    dataOut[1] = 0x00;
            //    dataOut[2] = address;
            //    dataOut[3] = type;
            //    dataOut[4] = 0x55;
            //    dataOut[5] = 0xAA;
            //    dataOut[6] = 0xFF;
            //    dataOut[7] = 0xFF;

            //    DataExchange(250);
            //    dataOut[0] = 0x22;
            //    DataExchange(20);
            //    dataIn = EncDec.DecodeData(dataInBuf);
            //    switch (type)
            //    {
            //        case 0:
            //            EncDec.CodeToADC(dataIn, out this.pressureADC, out this.temperatureADC);
            //            break;
            //        case 1:
            //            EncDec.CodeToSI(dataIn, out this.pressureSI, out this.temperatureSI);
            //            break;
            //        default:
            //            break;
            //    }
            //}
        }
        private void ReaderHandler()
        {
            testInt = 100;
        }

        public void ReadSI() { }
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
