using System;
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


        private byte[] data;
        private readonly string DESCRIPTION; 
        private readonly string SERIAL_NUMBER;

        public Device() // выполнять базовый констр в спец констре.
        {
            data = new byte[8];
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
            return 0;
        }
        #endregion
    }
}
