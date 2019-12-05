using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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


        public ControllerFTDI()
        {
        }

        public ControllerFTDI(string description, string serialNumber)
        {
            this.DESCRIPTION = description;
            this.SERIAL_NUMBER = serialNumber;

            ftdi = new FTDI();
            status = ftdi.GetNumberOfDevices(ref this.devCount);
            if(status != FTDI.FT_STATUS.FT_OK)
            {
                Exception ex = new Exception($"Error: {status.ToString()}");
                throw ex;
            }
            else if(this.devCount == 0)
            {
                throw new Exception($"Devices not connected!");
            }
            this.deviceList = new FTDI.FT_DEVICE_INFO_NODE[this.devCount];
            

        }

        // показать информацию о подключенном оборудовании
        public string ShowDeviceInfo()
        {
            status = ftdi.GetDeviceList(this.deviceList);
            if (status != FTDI.FT_STATUS.FT_OK)
            {
                Exception ex = new Exception($"Error: {status.ToString()}");
                throw ex;
            }
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
    }
}
