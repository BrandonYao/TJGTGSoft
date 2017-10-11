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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Configuration;

namespace GTGSoft
{
    /// <summary>
    /// UCConfig.xaml 的交互逻辑
    /// </summary>
    public partial class UCConfig : UserControl
    {
        public UCConfig()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            cbServer.Text = Config.Soft.Server;
            tbUserID.Text = Config.Soft.UserID;
            tbPassword.Text = Config.Soft.Password;
            cbDatabase.Text = Config.Soft.Database;
            cbMacCode.Text = Config.Soft.MacCode;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            string server = cbServer.Text.Trim();
            string userID = tbUserID.Text.Trim();
            string password = tbPassword.Text.Trim();
            string database = cbDatabase.Text.Trim();
            string macCode = cbMacCode.Text.Trim();

            Configuration cf = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            cf.AppSettings.Settings["Server"].Value = server;
            cf.AppSettings.Settings["UserID"].Value = userID;
            cf.AppSettings.Settings["Password"].Value = password;
            cf.AppSettings.Settings["Database"].Value = database;
            cf.AppSettings.Settings["MacCode"].Value = macCode;
            cf.Save(ConfigurationSaveMode.Minimal, true);

        }
    }
}
