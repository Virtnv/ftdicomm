using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ftdicomm
{
    public static class EncDec // 
    {
        private static uint numBits = 20;
        private static uint bytesToWrite = (8 * numBits) + 10;
        private static uint length = 7;

        public static void FillByteArray(ref byte[] byteArray, byte value)
        {
            for (int i = 0; i < byteArray.Length; i++)
            {
                byteArray[i] = value;
            }
        }

        public static byte[] EncodeData(byte[] data) //кодировать данные
        {
            uint index = 0;
            byte b = 0;
            byte[] encodedData = new byte[bytesToWrite];

            FillByteArray(ref encodedData, 0x01);

            for (int i = 0; i <= length; i++)
            {
                b = data[i];
                for (int j = 0; j <= length; j++)
                {
                    index = (uint)(2 * j + 1 + numBits * i);
                    encodedData[index] |= 0x80;
                    if ((b & 0x80) == 0x80)
                    {
                        index = (uint)(2 * j + 0 + numBits * i);
                        encodedData[index] += 0x40;
                        index = (uint)(2 * j + 1 + numBits * i);
                        encodedData[index] += 0x40;
                    }
                    b = (byte)(b << 1);
                }
            }
            return encodedData;
        }

        public static byte[] DecodeData(byte[] dataIn) // декодировать данные
        {
            byte[] decodedData = new byte[8];
            byte b = 0;
            for (int i = 0; i <= length; i++)
            {
                b = 0;
                for (int j = 0; j <= length; j++)
                {
                    b *= 2;
                    if ((dataIn[2 * j + 2 + numBits * i] & 0x20) != 0)
                    {
                        b++;
                    }
                }
                decodedData[i] = b;
            }
            return decodedData;
        }

        #region Code To SI or ADC
        public static void CodeToSI(byte[] data, out float pressureSI, out float temperatureSI)
        {
            pressureSI = 0.02f * (short)(data[1] + 256 * data[2]);
            temperatureSI = 0.02f * (short)(data[3] + 256 * data[4]);
        }
        public static void CodeToADC(byte[] dataIn, out ushort pressureADC, out ushort temperatureADC)
        {
            pressureADC = (ushort)(dataIn[1] + 256 * dataIn[2]);
            temperatureADC = (ushort)(dataIn[3] + 256 * dataIn[4]);
        } 
        #endregion
    }
}

