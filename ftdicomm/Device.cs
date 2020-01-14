using System;
using System.Threading;
using FTD2XX_NET;

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

        public Device() // выполнять базовый констр в спец констре.
        {
            data = new byte[8];
            buffer = new byte[200];
        }
        public Device(FTDI ftdi) : this() // выполнять базовый констр в спец констре.
        {
            this.ftdi = ftdi;
        }
        #region MainFunctions
        public void ReadADC() { }
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
        public bool SetFTDI(FTDI ftdi)
        {
            try
            {
                this.ftdi = ftdi;
                return true;
            }
            catch (Exception)
            {
                throw new Exception("ftdi null");
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
