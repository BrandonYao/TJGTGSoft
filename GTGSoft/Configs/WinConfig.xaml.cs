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
using System.IO.Ports;
using System.Data.SqlClient;
using System.Data.Sql;
using System.Data;
using System.Configuration;

namespace GTGSoft
{
    /// <summary>
    /// WinConfig.xaml 的交互逻辑
    /// </summary>
    public partial class WinConfig : Window
    {
        CSHelper.Msg csMsg = new CSHelper.Msg();
        CSHelper.XML csXml = new CSHelper.XML();
        CSHelper.SQL csSql = new CSHelper.SQL();

        public delegate void SetKey(bool show);
        public static SetKey ShowKey;

        public WinConfig()
        {
            InitializeComponent();

            YFS.Add(new YF() { YFNo = "MZ", YFName = "门诊药房" });
            YFS.Add(new YF() { YFNo = "JZ", YFName = "急诊药房" });
            YFS.Add(new YF() { YFNo = "ZY", YFName = "住院药房" });
            YFS.Add(new YF() { YFNo = "JP", YFName = "静配中心" });
        }

        private void ShowPorts()
        {
            foreach (string s in SerialPort.GetPortNames())
            {
            }
        }

        public class YF
        {
            public string YFNo { get; set; }
            public string YFName { get; set; }
        }

        private List<YF> _yfs = new List<YF>();
        public List<YF> YFS
        {
            get { return _yfs; }
            set { _yfs = value; }
        }

        private void ShowYF()
        {
            Dictionary<string, string> mydic = new Dictionary<string, string>() 
            { 
            {"MZ","门诊药房"},
            {"JZ","急诊药房"},
            {"ZY","住院药房"},
            {"JP","静配中心"}
            };
            cbYFNo.ItemsSource = mydic;
            cbYFNo.SelectedValuePath = "Key";
            cbYFNo.DisplayMemberPath = "Value";
        }

        private void ShowConfig()
        {
            cbServer.Text = Config.Soft.Server;
            tbUserID.Text = Config.Soft.UserID;
            tbPassword.Text = Config.Soft.Password;
            cbDatabase.Text = Config.Soft.Database;

            cbYFNo.SelectedValue = Config.Soft.YFNo;
            cbMacCode.Text = Config.Soft.MacCode;
            cbHISType.Text = Config.Soft.HISType;
            tbHIS.Text = Config.Soft.ConnString_HIS;
            chkShow.IsChecked = Config.Soft.Set == "N" ? true : false;
        }

        private void bt_Close_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void bt_Sure_Click(object sender, RoutedEventArgs e)
        {
            Dictionary<string, string> dics = new Dictionary<string, string>();
            if (string.IsNullOrEmpty(cbServer.Text))
            {
                tcConfig.SelectedIndex = 0;
                csMsg.ShowWarning("服务器不能为空", false);
                return;
            }
            dics.Add("Server", cbServer.Text.Trim());

            if (string.IsNullOrEmpty(tbUserID.Text))
            {
                tcConfig.SelectedIndex = 0;
                csMsg.ShowWarning("用户名不能为空", false);
                return;
            }
            dics.Add("UserID", tbUserID.Text.Trim());

            dics.Add("Password", tbPassword.Text.Trim());
            if (string.IsNullOrEmpty(cbDatabase.Text))
            {
                tcConfig.SelectedIndex = 0;
                csMsg.ShowWarning("数据库不能为空", false);
                return;
            }
            dics.Add("Database", cbDatabase.Text.Trim()); 
            
            if (string.IsNullOrEmpty(cbYFNo.Text))
            {
                tcConfig.SelectedIndex = 1;
                csMsg.ShowWarning("药房不能为空", false);
                return;
            }
            dics.Add("YFNo", cbYFNo.Text.Trim());

            if (string.IsNullOrEmpty(cbMacCode.Text))
            {
                tcConfig.SelectedIndex = 1;
                csMsg.ShowWarning("设备号不能为空", false);
                return;
            }
            dics.Add("MacCode", cbMacCode.Text.Trim());

            if (string.IsNullOrEmpty(cbHISType.Text))
            {
                tcConfig.SelectedIndex = 1;
                csMsg.ShowWarning("HIS数据库类型不能为空", false);
                return;
            }
            dics.Add("HISType", cbHISType.Text.Trim());

            if (string.IsNullOrEmpty(tbHIS.Text))
            {
                tcConfig.SelectedIndex = 0;
                csMsg.ShowWarning("HIS数据库连接不能为空", false);
                return;
            }
            dics.Add("ConnString_HIS", tbHIS.Text.Trim());
           
            if ((bool)chkShow.IsChecked)
                dics.Add("Set", "N");
            else
                dics.Add("Set", "Y");

            Configuration cf = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            foreach (string key in dics.Keys)
            {
                cf.AppSettings.Settings[key].Value = dics[key];
            }
            cf.Save(ConfigurationSaveMode.Minimal, true);

            Config.InitialConfig_Client();
            Config.InitialConfig_Server();

            new MainWindow().Show();
            this.Close();
        }

        private void bt_Test_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(cbServer.Text))
            {
                csMsg.ShowWarning("服务器不能为空", false);
                return;
            }
            if (string.IsNullOrEmpty(tbUserID.Text))
            {
                csMsg.ShowWarning("用户名不能为空", false);
                return;
            }
            if (string.IsNullOrEmpty(cbDatabase.Text))
            {
                csMsg.ShowWarning("数据库不能为空", false);
                return;
            }
            string connStr = "Data Source=" + cbServer.Text.Trim() + ";User ID=" + tbUserID.Text.Trim() + ";Password=" + tbPassword.Text.Trim() + ";Initial Catalog=" + cbDatabase.Text.Trim();
            using (SqlConnection conn = new SqlConnection(connStr))
            {
                try
                {
                    conn.Open();
                    csMsg.ShowInfo("测试成功", false);
                }
                catch (Exception ex)
                {
                    csMsg.ShowWarning(ex.Message, false);
                }
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            ShowYF();
            ShowConfig();
        }

        private void cbServer_DropDownOpened(object sender, EventArgs e)
        {
            Cursor = Cursors.Wait;
            SqlDataSourceEnumerator instance = SqlDataSourceEnumerator.Instance;
            DataTable dtServer = instance.GetDataSources();
            cbServer.Items.Clear();
            foreach (DataRow row in dtServer.Rows)
            {
                cbServer.Items.Add(row["servername"].ToString());
            }
            Cursor = null;
        }

        private void cbDatabase_DropDownOpened(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(cbServer.Text))
            {
                csMsg.ShowWarning("服务器不能为空", false);
                return;
            }
            if (string.IsNullOrEmpty(tbUserID.Text))
            {
                csMsg.ShowWarning("用户名不能为空", false);
                return;
            }

            Cursor = Cursors.Wait;
            string connStr = "Data Source=" + cbServer.Text.Trim() + ";User ID=" + tbUserID.Text.Trim() + ";Password=" + tbPassword.Text.Trim() + ";Initial Catalog=master";
            using (SqlConnection conn = new SqlConnection(connStr))
            {
                string sql = "select name from sys.databases where database_id > 4";
                SqlDataAdapter sda = new SqlDataAdapter(sql, conn);
                DataTable dt = new DataTable();
                try
                {
                    sda.Fill(dt);
                }
                catch (Exception ex)
                {
                    csMsg.ShowWarning(ex.Message, false);
                }

                cbDatabase.Items.Clear();
                foreach (DataRow row in dt.Rows)
                {
                    cbDatabase.Items.Add(row["name"].ToString());
                }
            }
            Cursor = null;
        }

        private void cbMacCode_DropDownOpened(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(cbServer.Text))
            {
                tcConfig.SelectedIndex = 0;
                csMsg.ShowWarning("服务器不能为空", false);
                return;
            }
            if (string.IsNullOrEmpty(tbUserID.Text))
            {
                tcConfig.SelectedIndex = 0;
                csMsg.ShowWarning("用户名不能为空", false);
                return;
            }
            if (string.IsNullOrEmpty(cbDatabase.Text))
            {
                tcConfig.SelectedIndex = 0;
                csMsg.ShowWarning("数据库不能为空", false);
                return;
            }
            string connStr = "server=" + cbServer.Text.Trim() + ";User ID=" + tbUserID.Text.Trim() + ";Password=" + tbPassword.Text.Trim() + ";database=" + cbDatabase.Text.Trim();
            DataTable dt;
            string sql = "select maccode from sys_macinfo where mactype='G'";

            csSql.ExecuteSelect(sql, connStr, out dt);
            cbMacCode.Items.Clear();
            foreach (DataRow row in dt.Rows)
            {
                cbMacCode.Items.Add(row["maccode"].ToString());
            }
        }

        private void tbCode_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            //tbCode.SelectAll();
            //ShowKey(true);
            //csKey.Show(300, 300);
        }
        private void tbCode_LostFocus(object sender, RoutedEventArgs e)
        {
            //ShowKey(false);
            //csKey.Close();
        }
    }
}
