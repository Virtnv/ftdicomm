using System;
using FTD2XX_NET;

namespace ftdicomm
{
    class Controller
    {
        private FTDI ftdi;
        private FTDI.FT_STATUS status;
        private FTDI.FT_DEVICE_INFO_NODE[] deviceList;

        #region Constructors

        public Controller()
        {
            ftdi = new FTDI();
        }
        public Controller(string description) : this()
        {
            Open(description);
            Initialization();
        }

        #endregion

        #region Main Functions
        public void Open(string description)
        {
            status = ftdi.OpenByDescription(description);
            CheckStatus(status);       
        }

        #endregion
        #region Additional
        private void CheckStatus(FTDI.FT_STATUS status)
        {
            if (status != FTDI.FT_STATUS.FT_OK)
            {
                FTDI.FT_EXCEPTION ex = new FTDI.FT_EXCEPTION($"Error: {status.ToString()}");
                throw ex;
            }
        }
        private void Initialization()
        {
            this.status = this.ftdi.SetBitMode(0xDF, FTDI.FT_BIT_MODES.FT_BIT_MODE_SYNC_BITBANG);
            CheckStatus(this.status);
            this.status = this.ftdi.SetBaudRate(200000);
            CheckStatus(this.status);
            this.status = this.ftdi.SetLatency(5);
            CheckStatus(this.status);
            this.status = this.ftdi.SetTimeouts(250, 250);
            CheckStatus(this.status);
        }

        // показать информацию о подключенном оборудовании
        public string ShowDeviceInfo()
        {
            uint devCount = 0;
            status = ftdi.GetNumberOfDevices(ref devCount);
            CheckStatus(status);
            if (devCount == 0)
            {
                throw new Exception($"Devices not connected!");
            }
            this.deviceList = new FTDI.FT_DEVICE_INFO_NODE[devCount];
            if (this.deviceList == null) throw new Exception("Device List is empty!");
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
        #endregion
    }
}
