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

namespace GTGSoft
{
    /// <summary>
    /// WinStock.xaml 的交互逻辑
    /// </summary>
    public partial class WinStock : Window
    {
        CSHelper.Msg csMsg = new CSHelper.Msg();
        CSHelper.SQL csSql = new CSHelper.SQL();

        public string Type;
        public string PosCode;
        public string DrugOnlyCode;
        public string DrugName;
        public string DrugSpec;
        public string DrugFactory;

        public WinStock()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            tbDrugName.Text = DrugName;
            tbDrugSpec.Text = DrugSpec;
            tbDrugFactory.Text = DrugFactory;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            string num = tbNum.Text.Trim();
            if (!string.IsNullOrEmpty(num))
            {
                int i;
                if(int.TryParse(num, out i))
                {
                    string sql = "update drug_pos set drugnum=(drugnum{0}{1}) where maccode='{2}' and poscode='{3}' and drugonlycode='{4}'";
                    sql = string.Format(sql, Type, i, Config.Soft.MacCode, PosCode, DrugOnlyCode);
                    csSql.ExecuteSql(sql, Config.Soft.ConnString);
                    csMsg.ShowInfo("操作成功", true);
                }
                else
                    csMsg.ShowWarning("请输入正确的数量", true);
            }
            else
                csMsg.ShowWarning("请输入加药或取药数量", true);
        }
    }
}
