using System;
using System.Collections.Generic;
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

        public MainWindow()
        {
            InitializeComponent();
            LoadShowProperties();
            Run();
        }

        private void LoadShowProperties()
        {
            this.Top = Properties.Settings.Default.x;
            this.Left = Properties.Settings.Default.y;
            tbPropList.Text = $"{this.Top.ToString()}\n{this.Left.ToString()}";
        }
        
        private void Run()
        {
            try
            {
                ControllerFTDI controller = new ControllerFTDI();
                tbPropList.Text += controller.ShowDeviceInfo();
            }
            catch(Exception e)
            {
                tbPropList.Text += $"\n{e.Message}";
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            Properties.Settings.Default.x  = this.Top;
            Properties.Settings.Default.y = this.Left;
            Properties.Settings.Default.Save();
        }
    }
}
