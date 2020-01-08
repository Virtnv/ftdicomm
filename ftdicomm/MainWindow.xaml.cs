using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ftdicomm
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private bool cont = true;

        public MainWindow()
        {
            InitializeComponent();
            LoadingProperties();
            //Run();
        }

        private void Run()
        {
            try
            {
                ControllerFTDI controller = new ControllerFTDI(Properties.Settings.Default.Description, Properties.Settings.Default.SerialNumber);
                tbPropList.Text += controller.ShowDeviceInfo();
                string sensorData = "";
                tbPropList.Text += controller.ShowConnectedSensors();
                //foreach (var sensor in controller.SensorsList)
                //{
                //    sensorData += $"address: {sensor.Address}\npressure: {sensor.P_SI} kgs/sm2, temperature: {sensor.T_SI} C\n";
                //}
                //tbPropList.Text += sensorData;
            }
            catch (Exception e)
            {
                tbPropList.Text += $"\n{e.Message}";
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            SavingProperties();
        }

        private void LoadingProperties() // загрузка свойств
        {
            this.Top = Properties.Settings.Default.x;
            this.Left = Properties.Settings.Default.y;
        }
        
        private void SavingProperties() // сохранение свойств
        {
            Properties.Settings.Default.x = this.Top;
            Properties.Settings.Default.y = this.Left;
            Properties.Settings.Default.Save();
        }

        private void BtnAsyncCycle_Click(object sender, RoutedEventArgs e)
        {
            BackgroundWorker worker = new BackgroundWorker();
            worker.WorkerReportsProgress = true;
            worker.DoWork += worker_DoWork;
            worker.ProgressChanged += worker_ProgressChanged;
            worker.RunWorkerAsync();
        }

        void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            ControllerFTDI controller = new ControllerFTDI(Properties.Settings.Default.Description, Properties.Settings.Default.SerialNumber);
            cont = true;
            List<Sensor> ls = new List<Sensor>();
            while (cont)
            {
                controller.Cycle();
                ls = controller.SensorsList;
                ((BackgroundWorker)sender).ReportProgress(1, ls);
            }
            //string sensorData = "";

            //foreach (var sensor in controller.SensorsList)
            //{
            //    sensorData += $"address: {sensor.Address}\npressure: {sensor.P_SI} kgs/sm2, temperature: {sensor.T_SI} C\n";
            //}
            //((BackgroundWorker)sender).ReportProgress(1, sensorData);

        }

        void worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            List<Sensor> ls = (List<Sensor>)e.UserState;
            foreach (var sensor in ls)
            {
                tbPropList.Text += sensor.ToString();
            }
        }

        private void BtnStop_Click(object sender, RoutedEventArgs e)
        {
            cont = false;
        }

        private void BtnTest_Click(object sender, RoutedEventArgs e)
        {
            Controller cont = new Controller(Properties.Settings.Default.Description);
            tbPropList.Text += "O God!";
            tbPropList.Text += cont.ShowDeviceInfo();
        }
    }
}
