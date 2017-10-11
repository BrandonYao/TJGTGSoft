using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Threading;

namespace GTGSoft
{
    /// <summary>
    /// WinFirst.xaml 的交互逻辑
    /// </summary>
    public partial class WinLogo : Window
    {
        public WinLogo()
        {
            InitializeComponent();
        }

        private void DoubleAnimation_Completed(object sender, EventArgs e)
        {
            Config.InitialConfig_Client();

            if (Config.Soft.Set == "Y")
                new WinConfig().Show();
            else
                new MainWindow().Show();

            Close();
        }
    }
}
