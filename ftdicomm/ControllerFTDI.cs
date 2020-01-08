using System;
using System.Threading;
using System.Collections.Generic;
using FTD2XX_NET;

namespace ftdicomm
{
    class ControllerFTDI
    {
        private FTDI ftdi;
        private FTDI.FT_STATUS status;
        private uint devCount;
        private FTDI.FT_DEVICE_INFO_NODE[] deviceList;
        private readonly string DESCRIPTION;
        private readonly string SERIAL_NUMBER;

        public byte[] dataOut { get; private set; }  // передающий буффер
        public byte[] dataIn { get; private set; }
        public byte[] dataInBuf { get; private set; }

        public float pressureSI = 0f;
        public float temperatureSI = 0f;

        public ushort pressureADC = 0;
        public ushort temperatureADC = 0;

        public List<Sensor> SensorsList;

        #region ctors
        public ControllerFTDI()
        {
        }

        public ControllerFTDI(string description, string serialNumber)
        {
            this.dataOut = new byte[8];
            this.dataInBuf = new byte[200];
            this.DESCRIPTION = description;
            this.SERIAL_NUMBER = serialNumber;
            this.SensorsList = new List<Sensor>();

            ftdi = new FTDI();
            status = ftdi.GetNumberOfDevices(ref this.devCount);
            CheckStatus(status);
            if (this.devCount == 0)
            {
                throw new Exception($"Devices not connected!");
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
            UpdateConnectedSensors(10, SensorsList);
        }
        #endregion

        public void Cycle()
        {
            Thread thread = new Thread(CycleThread);
            thread.Start();
            thread.Join();
        }
        private void CycleThread()
        {
            if (this.SensorsList is null)
            {
                throw new FTDI.FT_EXCEPTION("Error: No connected sensors!");
            }
            byte type = 1;
            foreach (var sensor in this.SensorsList)
            {
                RequestData(sensor, type); // si - 1
            }
        }

        public string ShowConnectedSensors()
        {
            if (this.SensorsList == null)
            {
                throw new FTDI.FT_EXCEPTION("Error: No connected sensors!");
            }
            string result = "";
            foreach (var sensor in this.SensorsList)
            {
                result += $"address: {sensor.Address}\n";
            }
            return result;
        }
        

        private void DataExchange(int timeToSleep)
        {
            uint numBytesWritten = 0;
            uint numBytesRead = 0;
            byte[] encodedDataToWrite = EncDec.EncodeData(dataOut);

            Purge();
            ftdi.Write(encodedDataToWrite, encodedDataToWrite.Length, ref numBytesWritten);
            ftdi.Read(dataInBuf, (uint)dataInBuf.Length, ref numBytesRead);
            Thread.Sleep(timeToSleep);
        }

        public void RequestData(Sensor sensor, byte type)        // запрос данных о давлении или температуре
        {                                                       // адрес прибора, тип - ацп (0) или si (1)
            dataIn = new byte[200];

            dataOut[0] = 0x1A;
            dataOut[1] = 0x00;
            dataOut[2] = sensor.Address;
            dataOut[3] = type;
            dataOut[4] = 0x55;
            dataOut[5] = 0xAA;
            dataOut[6] = 0xFF;
            dataOut[7] = 0xFF;

            DataExchange(250);
            dataOut[0] = 0x22;
            DataExchange(20);
            dataIn = EncDec.DecodeData(dataInBuf);
            switch (type)
            {
                case 0:
                    EncDec.CodeToADC(dataIn, out this.pressureADC, out this.temperatureADC);
                    sensor.P_ADC = this.pressureADC; sensor.T_ADC = this.temperatureADC;
                    break;
                case 1:
                    EncDec.CodeToSI(dataIn, out this.pressureSI, out this.temperatureSI);
                    sensor.P_SI = this.pressureSI; sensor.T_SI = this.temperatureSI;

                    break;
                default:
                    break;
            }
        }

        public void RequestData(byte address, byte type)        // запрос данных о давлении или температуре
        {                                                       // адрес прибора, тип - ацп (0) или si (1)
            dataIn = new byte[200];

            dataOut[0] = 0x1A;
            dataOut[1] = 0x00;
            dataOut[2] = address;
            dataOut[3] = type;
            dataOut[4] = 0x55;
            dataOut[5] = 0xAA;
            dataOut[6] = 0xFF;
            dataOut[7] = 0xFF;

            DataExchange(250);
            dataOut[0] = 0x22;
            DataExchange(20);
            dataIn = EncDec.DecodeData(dataInBuf);
            switch (type)
            {
                case 0:
                    EncDec.CodeToADC(dataIn, out this.pressureADC, out this.temperatureADC);
                    break;
                case 1:
                    EncDec.CodeToSI(dataIn, out this.pressureSI, out this.temperatureSI);
                    break;
                default:
                    break;
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

        private void Purge()
        {
            this.ftdi.Purge(FTDI.FT_PURGE.FT_PURGE_RX);
            this.ftdi.Purge(FTDI.FT_PURGE.FT_PURGE_TX);
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
            if(status != FTDI.FT_STATUS.FT_OK)
            {
                FTDI.FT_EXCEPTION ex = new FTDI.FT_EXCEPTION($"Error: {status.ToString()}");
                throw ex;
            }
        }

        // показать информацию о подключенном оборудовании
        public string ShowDeviceInfo()
        {
            status = ftdi.GetDeviceList(this.deviceList);
            CheckStatus(status);
            string info = "";
            foreach (var device in this.deviceList)
            {
                info += $"\n\nDescription: {device.Description}\n" +
                        $"Flags: {device.Flags}\n" +
                        $"ftHandle: {device.ftHandle}\n" +
                        $"ID: {device.ID}\n" +
                        $"LocID: {device.LocId}\n" +
                        $"Serial Number: {device.SerialNumber}\n" +
                        $"Type: {device.Type}\n";
            }
            return info;
        }

        private void UpdateConnectedSensors(byte limit, List<Sensor> sensors) // определение подключенных датчиков
        {
            sensors.Clear();
            for (byte i = 1; i <= limit; i++)
            {
                RequestData(i, 0);
                if(this.pressureADC != 12_336) //13364
                {
                    Sensor s = new Sensor(i);
                    UpdateSensorInfo(s);
                    sensors.Add(s);
                }
            }
        }

        private void UpdateSensorInfo(Sensor sensor)
        {
            sensor.P_ADC = this.pressureADC;
            sensor.P_SI = this.pressureSI;
            sensor.T_ADC = this.temperatureADC;
            sensor.T_SI = this.temperatureSI;
        }

        public double ReadCoeff(byte address, byte coeffNumber, out ushort sm_p, out ushort sn)
        {
            dataIn = new byte[8];

            dataOut[0] = 0x1A;
            dataOut[1] = 0x00;
            dataOut[2] = (byte)(address + 0x20);
            if (coeffNumber == 8) coeffNumber = 7;
            dataOut[3] = coeffNumber;
            dataOut[4] = 0x55;
            dataOut[5] = 0xAA;
            dataOut[6] = 0xFF;
            dataOut[7] = 0xFF;

            DataExchange(40);
            dataOut[0] = 0x22;
            DataExchange(40);
            dataIn = EncDec.DecodeData(dataInBuf);
            byte[] dataLimited = { dataIn[1], dataIn[2], dataIn[3], dataIn[4] };
            sm_p = 0;
            sn = 0;
            double coefficientValue;
            if (coeffNumber == 7)
            {
                EncDec.CodeToADC(dataIn, out sm_p, out sn);
                coefficientValue = 0;
            }
            else
            {
                coefficientValue = BitConverter.ToSingle(dataLimited, 0);
            }
            return coefficientValue;
        }
    }
}
