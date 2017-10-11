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
using System.Windows.Threading;
using System.Data;
using System.ComponentModel;

namespace GTGSoft
{
    /// <summary>
    /// UCAuto.xaml 的交互逻辑
    /// </summary>
    public partial class UCAuto : UserControl
    {
        CSHelper.SQL csSql = new CSHelper.SQL();
        CSHelper.Oracle csOracle = new CSHelper.Oracle();
        CSHelper.Msg csMsg = new CSHelper.Msg();

        public delegate void ShowMsg(string msg);
        public static ShowMsg ThrowMsg;

        public delegate void SetKey(bool show);
        public static SetKey ShowKey;

        public UCAuto()
        {
            InitializeComponent();
        }

        public class Presc
        {
            public string PrescNo { get; set; }
            public string PatName { get; set; }
            public string WindowNo { get; set; }
            public string WindowName { get; set; }
            public string DoType { get; set; }
            public string IOFlag { get; set; }
            public string BackColor { get; set; }
        }

        public class Drug : INotifyPropertyChanged
        {
            public string MacCode { get; set; }
            public string PosCode { get; set; }
            public string DrugOnlyCode { get; set; }
            public string DrugName { get; set; }
            public string DrugSpec { get; set; }
            public string DrugFactory { get; set; }
            public string NumString { get; set; }
            public string DrugNum { get; set; }
            public string DrugStock { get; set; }

            private string backColor;
            public string BackColor
            {
                get { return backColor; }
                set
                {
                    backColor = value;
                    OnPropertyChanged(new PropertyChangedEventArgs("BackColor"));
                }
            }

            public event PropertyChangedEventHandler PropertyChanged;
            private void OnPropertyChanged(PropertyChangedEventArgs e)
            {
                if (PropertyChanged != null)
                    PropertyChanged(this, e);
            }
        }

        public enum PrescType
        {
            Wait,
            OverTime,
            Done
        }

        private void RefreshList()
        {
            switch (tcList.SelectedIndex)
            {
                case 0:
                    RefreshPrescList(PrescType.Wait);
                    break;
                case 1:
                    RefreshPrescList(PrescType.OverTime);
                    break;
                case 2:
                    RefreshPrescList(PrescType.Done);
                    break;
            }
        }

        private void RefreshPrescList(PrescType pt)
        {
            tbRefresh.IsEnabled = tcList.IsEnabled = false;
            Cursor = Cursors.Wait;
            string wins = GetWins(Config.Soft.MacCode);
            string code = tbCode.Text.Trim();
            List<Presc> ps = new List<Presc>();

            string s1 = "select distinct pp.prescno ";
            string s2 = ",patname,pp.windowno,sw.windowname,dotype,paytime ";
            string s3 = @"from pat_prescinfo pp,pat_druginfo pd,sys_window sw
where paytime>=convert(varchar(100),getdate(),23) and
pp.doflag in('W','D','O') and pp.prescno=pd.prescno and pp.windowno=sw.windowno and pp.prescno like '%{0}%' and pp.windowno in ({2}) ";
            // paytime>=convert(varchar(100),getdate(),23) and
            //and pd.drugonlycode in(select distinct drugonlycode from drug_pos where maccode='{1}')
            string sql = "";
            switch (pt)
            {
                case PrescType.Wait:
                    sql = s1 + s2 + s3 + " and pd.doflag='N' and getdate()<=DateAdd(Minute," + Config.Mac.OverTime + ",paytime) ";
                    break;
                case PrescType.OverTime:
                    sql = s1 + s2 + s3 + " and pd.doflag='N' and getdate()>DateAdd(Minute," + Config.Mac.OverTime + ",paytime) ";
                    break;
                case PrescType.Done:
                    sql = s1 + s2 + s3 + " and pp.prescno not in(" + s1 + s3 + " and pd.doflag='N')";
                    break;
            }
            sql += " order by paytime";
            sql = string.Format(sql, code, Config.Soft.MacCode, wins);
            DataTable dtPresc;
            csSql.ExecuteSelect(sql, Config.Soft.ConnString, out dtPresc);
            if (dtPresc != null && dtPresc.Rows.Count > 0)
            {
                foreach (DataRow row in dtPresc.Rows)
                {
                    string type = row["dotype"].ToString().Trim();
                    Presc p = new Presc()
                    {
                        PrescNo = row["prescno"].ToString().Trim(),
                        PatName = row["PatName"].ToString().Trim(),
                        WindowNo = row["windowno"].ToString().Trim(),
                        WindowName = row["windowname"].ToString().Trim()
                    };
                    switch (type)
                    {
                        case "P":
                            p.DoType = "调配";
                            p.BackColor = "LightBlue";
                            break;
                        case "T":
                            p.DoType = "退药";
                            p.BackColor = "LightCoral";
                            break;
                        case "D":
                            p.DoType = "调拨";
                            p.BackColor = "Khaki";
                            break;
                    }
                    ps.Add(p);
                }
            }
            switch (pt)
            {
                case PrescType.Wait:
                    lvWait.ItemsSource = ps;
                    if (ps.Count == 1 && lvIn.Items.Count == 0)
                    {
                        //ShowPresc(ps[0].PrescNo, ps[0].PatName, ps[0].WindowName, ps[0].DoType, true);
                    }
                    break;
                case PrescType.OverTime:
                    lvOverTime.ItemsSource = ps;
                    break;
                case PrescType.Done:
                    lvDone.ItemsSource = ps;
                    break;
            }
            Cursor = null;
            tbRefresh.IsEnabled = tcList.IsEnabled = true;
        }

        //获取设备对应窗口组合
        private string GetWins(string maccode)
        {
            string result = "";
            string sql = "select windowno from sys_window_mac where maccode='{0}'";
            sql = string.Format(sql, Config.Soft.MacCode);
            DataTable dt;
            csSql.ExecuteSelect(sql, Config.Soft.ConnString, out dt);
            foreach (DataRow row in dt.Rows)
            {
                result += "'" + row["windowno"].ToString().Trim() + "',";
            }
            if (result.Length > 0)
                result = result.Substring(0, result.Length - 1);
            return result;
        }

        string TurnFX = "";
        private void ShowPresc(string prescNo, string name,string windowName, string type, bool showOut)
        {
            //Class.Key.Close();
            Cursor = Cursors.Wait;

            //显示台头
            tbOrderNo.Text = prescNo; tbName.Text = name; tbWindowName.Text = windowName; tbType.Text = type;
            
            //查询明细
           string sql = @"select dp.maccode,dp.poscode,dp.drugnum as drugstock,pd.doflag,pd.drugunit,
pd.drugonlycode,di.drugname,di.drugspec,di.drugfactory,sum(pd.drugnum) as drugnum,di.drugpackunit,isnull(di.drugpacknum,1) as drugpacknum,di.drugsplitunit,isnull(di.drugsplitnum,0) as drugsplitnum
from pat_druginfo pd
left join drug_pos dp on pd.drugonlycode=dp.drugonlycode
left join drug_info di on pd.drugonlycode=di.drugonlycode
where prescno='{0}' and createtime>=convert(varchar(100),getdate(),23)
group by dp.maccode,dp.poscode,dp.drugnum,pd.doflag,pd.drugunit,
pd.drugonlycode,di.drugname,di.drugspec,di.drugfactory,di.drugpackunit,di.drugpacknum,di.drugsplitunit,di.drugsplitnum
order by dp.maccode,dp.poscode,pd.doflag";
           // and createtime>=convert(varchar(100),getdate(),23)
            sql = string.Format(sql, prescNo);
            DataTable dtDetails;
            csSql.ExecuteSelect(sql, Config.Soft.ConnString, out dtDetails);

            if (dtDetails != null && dtDetails.Rows.Count > 0)
            {
                //显示机号指示灯
                Dictionary<string, string> macs = new Dictionary<string, string>();
                bool isOK = true;
                DataRow[] rows_Mac = dtDetails.Select("maccode is not null");
                if (rows_Mac.Length > 0)
                {
                    foreach (DataRow row in dtDetails.Rows)
                    {
                        string mac = row["maccode"].ToString().Trim();
                        string flag = row["doflag"].ToString().Trim();
                        if (!string.IsNullOrEmpty(mac))
                        {
                            if (!macs.ContainsKey(mac))
                                macs.Add(mac, flag);
                            if (flag == "N")
                            {
                                isOK = false;
                                macs[mac] = flag;
                            }
                        }
                    }
                    //配药完成（O）
                    if (isOK)
                    {
                        //sql = "update pat_druginfo set prescno='{0}_M' where prescno='{0}'";
                        //sql = string.Format(sql, prescNo);
                        //csSql.ExecuteSql(sql, Config.Soft.ConnString);
                    }
                }
                ShowMacs(macs);

                //显示药品明细
                List<Drug> ds_In = new List<Drug>();
                //计算转动起始层
                DataRow[] rows_Turn = dtDetails.Select("maccode='" + Config.Soft.MacCode + "' and doflag='N'");
                if (rows_Turn.Length > 0)
                {
                    List<string> poss = new List<string>();
                    foreach (DataRow row in rows_Turn)
                    {
                        string posCode = row["poscode"].ToString().Trim();
                        poss.Add(posCode);
                    }
                    //转动起始层
                    int startLay;
                    GetStart(poss, out startLay, out TurnFX);
                    //显示待陪明细
                    if (TurnFX == "down") //向下转，升序
                    {
                        //目标层上部
                        DataRow[] rows_Wait_1 = dtDetails.Select("maccode='" + Config.Soft.MacCode + "' and doflag='N' and substring(poscode,2,2)>='" + startLay.ToString().PadLeft(2, '0') + "'");
                        if (rows_Wait_1.Length > 0)
                        {
                            DataTable dt_Wait_1 = new DataTable();
                            foreach (DataColumn col in dtDetails.Columns)
                            {
                                dt_Wait_1.Columns.Add(col.ToString().Trim());
                            }
                            rows_Wait_1.CopyToDataTable(dt_Wait_1, LoadOption.OverwriteChanges);
                            DataView dv = dt_Wait_1.DefaultView;
                            dv.Sort = "poscode";
                            dt_Wait_1 = dv.ToTable();
                            foreach (DataRow row in dt_Wait_1.Rows)
                            {
                                ds_In.Add(new Drug()
                                {
                                    MacCode = row["maccode"].ToString().Trim(),
                                    PosCode = row["poscode"].ToString().Trim(),
                                    DrugOnlyCode = row["drugonlycode"].ToString().Trim(),
                                    DrugName = row["drugname"].ToString().Trim(),
                                    DrugSpec = row["drugspec"].ToString().Trim(),
                                    DrugFactory = row["drugfactory"].ToString().Trim(),
                                    //数量转换
                                    NumString = GetNumString(int.Parse(row["drugnum"].ToString()), row["drugsplitunit"].ToString(), int.Parse(row["drugpacknum"].ToString()), row["drugpackunit"].ToString(), int.Parse(row["drugsplitnum"].ToString()), row["drugsplitunit"].ToString()),
                                    DrugNum = GetNum(row["drugnum"].ToString().Trim(), row["drugsplitunit"].ToString(), int.Parse(row["drugpacknum"].ToString()), row["drugpackunit"].ToString(), int.Parse(row["drugsplitnum"].ToString()), row["drugsplitunit"].ToString()),
                                    DrugStock = row["drugstock"].ToString().Trim(),
                                    BackColor = "Khaki"
                                });
                            }
                        }
                        //目标层下部
                        DataRow[] rows_Wait_2 = dtDetails.Select("maccode='" + Config.Soft.MacCode + "' and doflag='N' and substring(poscode,2,2)<'" + startLay.ToString().PadLeft(2, '0') + "'");
                        if (rows_Wait_2.Length > 0)
                        {
                            DataTable dt_Wait_2 = new DataTable();
                            foreach (DataColumn col in dtDetails.Columns)
                            {
                                dt_Wait_2.Columns.Add(col.ToString().Trim());
                            }
                            rows_Wait_2.CopyToDataTable(dt_Wait_2, LoadOption.OverwriteChanges);
                            DataView dv = dt_Wait_2.DefaultView;
                            dv.Sort = "poscode";
                            dt_Wait_2 = dv.ToTable();
                            foreach (DataRow row in dt_Wait_2.Rows)
                            {
                                ds_In.Add(new Drug()
                                {
                                    MacCode = row["maccode"].ToString().Trim(),
                                    PosCode = row["poscode"].ToString().Trim(),
                                    DrugOnlyCode = row["drugonlycode"].ToString().Trim(),
                                    DrugName = row["drugname"].ToString().Trim(),
                                    DrugSpec = row["drugspec"].ToString().Trim(),
                                    DrugFactory = row["drugfactory"].ToString().Trim(),
                                    //数量转换
                                    NumString = GetNumString(int.Parse(row["drugnum"].ToString()), row["drugsplitunit"].ToString(), int.Parse(row["drugpacknum"].ToString()), row["drugpackunit"].ToString(), int.Parse(row["drugsplitnum"].ToString()), row["drugsplitunit"].ToString()),
                                    DrugNum = GetNum(row["drugnum"].ToString().Trim(), row["drugsplitunit"].ToString(), int.Parse(row["drugpacknum"].ToString()), row["drugpackunit"].ToString(), int.Parse(row["drugsplitnum"].ToString()), row["drugsplitunit"].ToString()),
                                    DrugStock = row["drugstock"].ToString().Trim(),
                                    BackColor = "Khaki"
                                });
                            }
                        }
                    }
                    else//向上转，降序
                    {
                        //小于目标层+上部储位
                        DataRow[] rows_Wait_1 = dtDetails.Select("maccode='" + Config.Soft.MacCode + "' and doflag='N' and substring(poscode,2,2)<='" + (startLay + 1).ToString().PadLeft(2, '0') + "'");
                        if (rows_Wait_1.Length > 0)
                        {
                            DataTable dt_Wait_1 = new DataTable();
                            foreach (DataColumn col in dtDetails.Columns)
                            {
                                dt_Wait_1.Columns.Add(col.ToString().Trim());
                            }
                            rows_Wait_1.CopyToDataTable(dt_Wait_1, LoadOption.OverwriteChanges);
                            DataView dv = dt_Wait_1.DefaultView;
                            dv.Sort = "poscode desc";
                            dt_Wait_1 = dv.ToTable();
                            foreach (DataRow row in dt_Wait_1.Rows)
                            {
                                ds_In.Add(new Drug()
                                {
                                    MacCode = row["maccode"].ToString().Trim(),
                                    PosCode = row["poscode"].ToString().Trim(),
                                    DrugOnlyCode = row["drugonlycode"].ToString().Trim(),
                                    DrugName = row["drugname"].ToString().Trim(),
                                    DrugSpec = row["drugspec"].ToString().Trim(),
                                    DrugFactory = row["drugfactory"].ToString().Trim(),
                                    //数量转换
                                    NumString = GetNumString(int.Parse(row["drugnum"].ToString()), row["drugsplitunit"].ToString(), int.Parse(row["drugpacknum"].ToString()), row["drugpackunit"].ToString(), int.Parse(row["drugsplitnum"].ToString()), row["drugsplitunit"].ToString()),
                                    DrugNum = GetNum(row["drugnum"].ToString().Trim(), row["drugsplitunit"].ToString(), int.Parse(row["drugpacknum"].ToString()), row["drugpackunit"].ToString(), int.Parse(row["drugsplitnum"].ToString()), row["drugsplitunit"].ToString()),
                                    DrugStock = row["drugstock"].ToString().Trim(),
                                    BackColor = "Khaki"
                                });
                            }
                        }
                        //大于目标层+上部储位
                        DataRow[] rows_Wait_2 = dtDetails.Select("maccode='" + Config.Soft.MacCode + "' and doflag='N' and substring(poscode,2,2)>'" + (startLay + 1).ToString().PadLeft(2, '0') + "'");
                        if (rows_Wait_2.Length > 0)
                        {
                            DataTable dt_Wait_2 = new DataTable();
                            foreach (DataColumn col in dtDetails.Columns)
                            {
                                dt_Wait_2.Columns.Add(col.ToString().Trim());
                            }
                            rows_Wait_2.CopyToDataTable(dt_Wait_2, LoadOption.OverwriteChanges);
                            DataView dv = dt_Wait_2.DefaultView;
                            dv.Sort = "poscode desc";
                            dt_Wait_2 = dv.ToTable();
                            foreach (DataRow row in dt_Wait_2.Rows)
                            {
                                ds_In.Add(new Drug()
                                {
                                    MacCode = row["maccode"].ToString().Trim(),
                                    PosCode = row["poscode"].ToString().Trim(),
                                    DrugOnlyCode = row["drugonlycode"].ToString().Trim(),
                                    DrugName = row["drugname"].ToString().Trim(),
                                    DrugSpec = row["drugspec"].ToString().Trim(),
                                    DrugFactory = row["drugfactory"].ToString().Trim(),
                                    //数量转换
                                    NumString = GetNumString(int.Parse(row["drugnum"].ToString()), row["drugsplitunit"].ToString(), int.Parse(row["drugpacknum"].ToString()), row["drugpackunit"].ToString(), int.Parse(row["drugsplitnum"].ToString()), row["drugsplitunit"].ToString()),
                                    DrugNum = GetNum(row["drugnum"].ToString().Trim(), row["drugsplitunit"].ToString(), int.Parse(row["drugpacknum"].ToString()), row["drugpackunit"].ToString(), int.Parse(row["drugsplitnum"].ToString()), row["drugsplitunit"].ToString()),
                                    DrugStock = row["drugstock"].ToString().Trim(),
                                    BackColor = "Khaki"
                                });
                            }
                        }
                    }
                }
                else
                {
                    PLC.LightAllNum(PLC.LightType.Close);
                }

                //显示已配明细
                DataRow[] rows_Over = dtDetails.Select("maccode='" + Config.Soft.MacCode + "' and doflag='Y'");
                if (rows_Over.Length > 0)
                {
                    DataTable dt_Over = new DataTable();
                    foreach (DataColumn col in dtDetails.Columns)
                    {
                        dt_Over.Columns.Add(col.ToString().Trim());
                    }
                    rows_Over.CopyToDataTable(dt_Over, LoadOption.OverwriteChanges);
                    DataView dv = dt_Over.DefaultView;
                    dv.Sort = "poscode";
                    dt_Over = dv.ToTable();
                    foreach (DataRow row in dt_Over.Rows)
                    {
                        ds_In.Add(new Drug()
                        {
                            MacCode = row["maccode"].ToString().Trim(),
                            PosCode = row["poscode"].ToString().Trim(),
                            DrugOnlyCode = row["drugonlycode"].ToString().Trim(),
                            DrugName = row["drugname"].ToString().Trim(),
                            DrugSpec = row["drugspec"].ToString().Trim(),
                            DrugFactory = row["drugfactory"].ToString().Trim(),
                            //数量转换
                            NumString = GetNumString(int.Parse(row["drugnum"].ToString()), row["drugsplitunit"].ToString(), int.Parse(row["drugpacknum"].ToString()), row["drugpackunit"].ToString(), int.Parse(row["drugsplitnum"].ToString()), row["drugsplitunit"].ToString()),
                            DrugNum = GetNum(row["drugnum"].ToString().Trim(), row["drugsplitunit"].ToString(), int.Parse(row["drugpacknum"].ToString()), row["drugpackunit"].ToString(), int.Parse(row["drugsplitnum"].ToString()), row["drugsplitunit"].ToString()),
                            DrugStock = row["drugstock"].ToString().Trim(),
                            BackColor = "LimeGreen"
                        });
                    }
                }
                if (ds_In.Count > 0)
                {
                    lvIn.ItemsSource = ds_In;

                    TurnPresc();
                }
                else
                {
                    lvIn.ItemsSource = null;
                    lvIn.Items.Clear();
                }

                if (showOut)
                {
                    List<Drug> ds_Out = new List<Drug>();
                    //显示机外药品明细
                    DataRow[] rows_Out = dtDetails.Select(" maccode<> '" + Config.Soft.MacCode + "' or maccode is null");
                    if (rows_Out.Length > 0)
                    {
                        DataTable dt_Out = new DataTable();
                        foreach (DataColumn col in dtDetails.Columns)
                        {
                            dt_Out.Columns.Add(col.ToString().Trim());
                        }
                        rows_Out.CopyToDataTable(dt_Out, LoadOption.OverwriteChanges);

                        DataView dv = dt_Out.DefaultView;
                        dv.Sort = "maccode";
                        DataTable dtDrugOut = dv.ToTable();

                        foreach (DataRow row in dtDrugOut.Rows)
                        {
                            Drug d = new Drug()
                            {
                                MacCode = row["maccode"].ToString().Trim(),
                                PosCode = row["poscode"].ToString().Trim(),
                                DrugOnlyCode = row["drugonlycode"].ToString().Trim(),
                                DrugName = row["drugname"].ToString().Trim(),
                                DrugSpec = row["drugspec"].ToString().Trim(),
                                DrugFactory = row["drugfactory"].ToString().Trim(),
                                //数量转换
                                NumString = GetNumString(int.Parse(row["drugnum"].ToString()), row["drugsplitunit"].ToString(), int.Parse(row["drugpacknum"].ToString()), row["drugpackunit"].ToString(), int.Parse(row["drugsplitnum"].ToString()), row["drugsplitunit"].ToString()),
                                DrugNum = GetNum(row["drugnum"].ToString().Trim(), row["drugsplitunit"].ToString(), int.Parse(row["drugpacknum"].ToString()), row["drugpackunit"].ToString(), int.Parse(row["drugsplitnum"].ToString()), row["drugsplitunit"].ToString()),
                                DrugStock = row["drugstock"].ToString().Trim()
                            };
                            d.BackColor = string.IsNullOrEmpty(d.MacCode) ? "Gray" : "LightBlue";
                            ds_Out.Add(d);
                        }
                    }
                    lvOut.ItemsSource = ds_Out;
                }
            }
            Cursor = null;
        }

        private void GetStart(List<string> outPoss, out int startLay, out string turnDir)
        {
            startLay = 0;
            turnDir = "";
            outPoss.Sort();

            //存放待配层的集合，以及每层与正向相邻层的间隔
            Dictionary<int, int> LayAndSpaces = new Dictionary<int, int>();
            //待选目标层
            //Dictionary<int, string> TargetLays = new Dictionary<int, string>();
            //保存待配层
            foreach (string pos in outPoss)
            {
                int lay = int.Parse(pos.Substring(1, 2));
                if (!LayAndSpaces.Keys.Contains(lay))
                    LayAndSpaces.Add(lay, 0);
            }

            //判断起始层和终点层
            #region
            int lay1 = 0, lay2 = 0; //起始层和终点层
            //只有一层
            if (LayAndSpaces.Count == 1)
            {
                List<int> keys = LayAndSpaces.Keys.ToList<int>();
                startLay = keys[0];
            }
            //多层，计算间隔，判断最大间隔
            //以最大间隔为首尾两端，添加待选目标层
            else
            {
                int maxSpace = 0; 
                //将键转成列表，可以按索引访问
                List<int> keys = LayAndSpaces.Keys.ToList<int>();
                int c = keys.Count;
                for (int i = 0; i < c; i++)
                {
                    int j = 0;
                    if (i != c - 1)  //不是最后一项，间隔为差值
                    {
                        j = keys[i + 1] - keys[i];
                        if (j > maxSpace)
                        {
                            maxSpace = j;
                            lay1 = keys[i];
                            lay2 = keys[i + 1];
                        }
                    }

                    else  //最后一项
                    {
                        j = keys[0] + Config.Mac.Count_GT * 2 - keys[i];
                        if (j >= maxSpace)
                        {
                            maxSpace = j;
                            lay1 = keys[i];
                            lay2 = keys[0];
                        }
                    }
                    LayAndSpaces[keys[i]] = j;
                }

                //判断最近目标层和方向
                #region
                //当前层
                int nowLay = PLC.ReadNowLay();

                if (lay1 < lay2)
                {
                    int m = 0, n = 0;
                    if (lay1 <= nowLay && nowLay <= lay2)
                    {
                        m = Math.Abs(nowLay - lay1);
                        n = Math.Abs(nowLay - lay2);
                    }
                    else if (nowLay < lay1)
                    {
                        m = Math.Abs(nowLay - lay1);
                        n = Math.Abs(nowLay + 20 - lay2);
                    }
                    else
                    {
                        m = Math.Abs(nowLay - 20 - lay1);
                        n = Math.Abs(nowLay - lay2);
                    }
                    if (m <= n)
                    {
                        startLay = lay1;
                        turnDir = "up";
                    }
                    else
                    {
                        startLay = lay2;
                        turnDir = "down";
                    }
                }
                else
                {
                    int m = 0, n = 0;
                    if (lay2 <= nowLay && nowLay <= lay1)
                    {
                        m = Math.Abs(nowLay - lay1);
                        n = Math.Abs(nowLay - lay2);
                    }
                    else if (nowLay < lay2)
                    {
                        m = Math.Abs(nowLay + 20 - lay1);
                        n = Math.Abs(nowLay - lay2);
                    }
                    else
                    {
                        m = Math.Abs(nowLay - lay1);
                        n = Math.Abs(nowLay - 20 - lay2);
                    }
                    if (m <= n)
                    {
                        startLay = lay1;
                        turnDir = "up";
                    }
                    else
                    {
                        startLay = lay2;
                        turnDir = "down";
                    }
                }
                #endregion
            }
            #endregion
        }
        //开始转动
        private void TurnPresc()
        {
            bool toTurn = false;
            if (lvIn.Items.Count > 0)
            {
                List<Drug> ds = lvIn.ItemsSource as List<Drug>;
                foreach (Drug d in ds)
                {
                    if (d.BackColor == "Khaki")
                    {
                        toTurn = true;
                        break;
                    }
                }
            }
            if (toTurn)
            {
                int firstLay = 0;
                int toLay = 0;
                List<string> turnPoss = new List<string>();
                List<int> turnLay = new List<int>();

                List<Drug> ds = lvIn.ItemsSource as List<Drug>;
                foreach (Drug d in ds)
                {
                    string pos = d.PosCode;
                    int lay = int.Parse(pos.Substring(1, 2));
                    bool add = false;
                    if (d.BackColor == "Khaki")
                    {
                        if (firstLay == 0)
                        {
                            firstLay = lay;
                            add = true;
                        }
                        else
                        {
                            if (firstLay % 2 == 0)
                            {
                                if ((firstLay == 20 && (lay == 1 || lay == 19)) || (firstLay <= 18 && firstLay + 1 >= lay && lay >= firstLay - 1))
                                {
                                    add = true;
                                }
                            }
                            else
                            {
                                if (TurnFX == "up")
                                {
                                    if ( (firstLay == 1 && lay >= 19) || (firstLay <= 19 && firstLay >= lay && lay >= firstLay - 2) )
                                    {
                                        add = true;
                                    }
                                }
                                else
                                {
                                    if ((firstLay <= 17 && firstLay <= lay && lay <= firstLay + 2) || (firstLay == 19 && (19 <= lay || lay == 1)))
                                    {
                                        add = true;
                                    }
                                }
                            }
                        }
                    }
                    if (add)
                    {
                        if (!turnPoss.Contains(pos))
                            turnPoss.Add(pos);
                        if (!turnLay.Contains(lay))
                            turnLay.Add(lay);
                        d.BackColor = "LightCoral";
                    }
                }

                int nowLay = PLC.ReadNowLay();
                turnLay.Sort();
                if (turnLay.Count == 1)
                {
                    if (turnLay[0] % 2 == 0) //偶数，一个目标层
                        toLay = turnLay[0] - 1;
                    else //奇数，两个目标层
                    {
                        int lay1 = 0, lay2 = 0;
                        if (turnLay[0] == 1)
                        {
                            lay1 = 19; lay2 = 1;
                        }
                        else
                        {
                            lay1 = turnLay[0] - 2;
                            lay2 = turnLay[0];
                        }
                        //判断更近层
                        if (nowLay == lay1)
                            toLay = lay1;
                        else if (nowLay == lay2)
                            toLay = lay2;
                        else
                        {
                            if (lay1 < lay2)
                            {
                                int m = 0, n = 0;
                                if (nowLay < lay1)
                                {
                                    m = Math.Abs(nowLay - lay1);
                                    n = Math.Abs(nowLay + 20 - lay2);
                                }
                                else
                                {
                                    m = Math.Abs(nowLay - 20 - lay1);
                                    n = Math.Abs(nowLay - lay2);
                                }
                                if (m <= n)
                                {
                                    toLay = lay1;
                                }
                                else
                                {
                                    toLay = lay2;
                                }
                            }
                            else
                            {
                                int m = 0, n = 0;
                                m = Math.Abs(nowLay - lay1);
                                n = Math.Abs(nowLay - lay2);
                                if (m <= n)
                                {
                                    toLay = lay1;
                                }
                                else
                                {
                                    toLay = lay2;
                                }
                            }
                        }
                    }
                }
                else
                {
                    toLay = turnLay[0]; 
                    if (toLay % 2 == 0) //偶数
                        toLay -= 1;
                }
                PLC.TurnTo(toLay);
                //亮灯
                List<string> lightPoss = new List<string>();
                foreach (string pos in turnPoss)
                {
                    int lay = int.Parse(pos.Substring(1, 2));
                    if (lay >= toLay)
                        lightPoss.Add((lay - toLay + 1).ToString().PadLeft(2, '0') + pos.Substring(3, 2));
                    else
                        lightPoss.Add((Config.Mac.Count_GT*2 + lay - toLay + 1).ToString().PadLeft(2, '0') + pos.Substring(3, 2));
                }
                PLC.LightMutilNum(lightPoss.ToArray());
                lvIn.ItemsSource = ds.ToList();
            }
        }
        //转换数量显示
        private string GetNumString(int num,string unit, int pack, string packUnit, int split, string splitUnit)
        {
            string s = "";
            if (unit == packUnit)
                s = num + packUnit;
            else
            {
                int n = num / (pack / split);
                int m = num % (pack / split);
                if (n > 0)
                    s += n + packUnit;
                if (m > 0)
                    s += m + splitUnit;
            }
            return s;
        }
        private string GetNum(string num, string unit, int pack, string packUnit, int split, string splitUnit)
        {
            string s = "";
            if (unit == packUnit)
                s = num;
            else
            {
                int i = int.Parse(num) % (pack / split);
                if (i == 0)
                    s = (int.Parse(num) / (pack / split)).ToString();
                else s = i.ToString();
            }
            return s;
        }

        //初始化机号指示灯
        private void InitialMacs()
        {
            string sql = "select maccode from sys_macinfo where mactype='G' order by maccode";
            DataTable dtMac;
            csSql.ExecuteSelect(sql, Config.Soft.ConnString, out dtMac);
            if (dtMac != null && dtMac.Rows.Count > 0)
            {
                foreach (DataRow row in dtMac.Rows)
                {
                    string maccode = row["maccode"].ToString();

                    TextBlock tb = new TextBlock();
                    tb.Text = maccode;
                    tb.HorizontalAlignment = HorizontalAlignment.Center;
                    tb.VerticalAlignment = VerticalAlignment.Center;

                    Border b = new Border();
                    b.Tag = maccode;
                    b.Width = 50; b.Height = 50; b.CornerRadius = new CornerRadius(25); b.Margin = new Thickness(10);
                    b.Background = new SolidColorBrush(Colors.Gray);
                    Grid gd = new Grid();
                    gd.Children.Add(tb);
                    b.Child = gd;
                    spMacs.Children.Add(b);
                }
            }
        }

        List<string> SSMacs = new List<string>();
        //控制机号指示灯
        private void ShowMacs(Dictionary<string, string> macs)
        {
            SSMacs.Clear();
            timer_SS.Stop(); 
            if (macs != null)
            {
                foreach (Border b in spMacs.Children)
                {
                    string mac = b.Tag.ToString();
                    if (macs.ContainsKey(mac))
                    {
                        if (macs[mac] == "Y")
                            b.Background = new SolidColorBrush(Colors.LimeGreen);
                        else
                            SSMacs.Add(mac);
                    }
                    else
                        b.Background = new SolidColorBrush(Colors.Gray);
                }
                if (SSMacs.Count > 0)
                    timer_SS.Start();
            }
            else
            {
                foreach (Border b in spMacs.Children)
                {
                    b.Background = new SolidColorBrush(Colors.Gray);
                }
            }
        }

        DispatcherTimer timer_Presc = new DispatcherTimer();
        DispatcherTimer timer_SS = new DispatcherTimer();
        DispatcherTimer timer_Turn = new DispatcherTimer();
        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            InitialMacs();

            RefreshList();

            timer_Presc.Interval = TimeSpan.FromSeconds(10);
            timer_Presc.Tick += new EventHandler(timer_Presc_Tick);
            //timer_Presc.Start();

            timer_SS.Interval = TimeSpan.FromSeconds(0.5);
            timer_SS.Tick += new EventHandler(timer_SS_Tick);

            timer_Turn.Interval = TimeSpan.FromSeconds(Config.Mac.TurnSpan);
            timer_Turn.Tick += new EventHandler(timer_Turn_Tick);
            if (Config.Mac.AutoTurn == "Y")
            {
                AutoTurn();
                timer_Turn.Start();
            }
        }

        private void AutoTurn()
        {
            string sql_His = @"Select mrno From (Select * From view_interface_recipejz
Where to_char(sysdate,'yyyy-mm-dd')=to_char(paytime,'yyyy-mm-dd') 
Order By sendtime Desc) Where Rownum<=1";
            string v; csOracle.ExecuteScalar(sql_His, Config.Soft.ConnString_HIS, out v);
            if (v != null)
            {
                string prescNo = v.ToString().Trim();

                //ThrowMsg(prescNo)
                    ;
                string sql = "select hisflag from pat_prescinfo where prescno='{0}'"; 
                sql = string.Format(sql, prescNo);
                csSql.ExecuteScalar(sql, Config.Soft.ConnString, out v);
                if (v != null)
                {
                    if (v.ToString() != "1")
                    {
                        sql = "select prescno,patname,windowname,dotype from pat_prescinfo pp,sys_window sw where prescno='{0}' and pp.windowno=sw.windowno";
                        sql = string.Format(sql, prescNo);
                        DataTable dtPresc;
                        csSql.ExecuteSelect(sql, Config.Soft.ConnString, out dtPresc);
                        if (dtPresc != null && dtPresc.Rows.Count > 0)
                        {
                            //自动确认上一张处方
                            string oldPrescNo = tbOrderNo.Text.Trim();
                            if (oldPrescNo != prescNo) //
                            {
                                //sql = "";
                                //List<Drug> ds = lvIn.ItemsSource as List<Drug>;
                                //foreach (Drug d in ds)
                                //{
                                sql = "update pat_druginfo set doflag='Y',dotime=getdate() where prescno='{0}' and doflag='N'";
                                    //sql = "delete from pat_prescinfo where prescno='{0}'; delete from pat_druginfo where prescno='{0}'";
                                    //sql = "update pat_prescinfo set prescno=(prescno+'_xxx') where prescno='{0}'; update pat_druginfo set prescno=(prescno+'_xxx'),doflag='Y',dotime=getdate() where prescno='{0}' and doflag='N'";
                                sql = string.Format(sql, oldPrescNo);
                                //}
                                csSql.ExecuteSql(sql, Config.Soft.ConnString);
                                PLC.LightAllNum(PLC.LightType.Close);
                                //自动减库存
                            }
                            //旋转当前处方
                            sql = " update pat_prescinfo set hisflag='1',histime=getdate() where prescno='{0}'";
                            sql = string.Format(sql, prescNo);
                            csSql.ExecuteSql(sql, Config.Soft.ConnString);

                            string type = dtPresc.Rows[0]["dotype"].ToString().Trim();
                            switch (type)
                            {
                                case "P":
                                    type = "调配";
                                    break;
                                case "T":
                                    type = "退药";
                                    break;
                                case "D":
                                    type = "调拨";
                                    break;
                            }
                            ShowPresc(prescNo, dtPresc.Rows[0]["patname"].ToString().Trim(), dtPresc.Rows[0]["windowname"].ToString().Trim(), type, true);

                        }
                    }
                    else
                    {
                        //ThrowMsg(prescNo + ":处方已配药");
                    }
                }
                else
                {
                    //ThrowMsg(prescNo + ":处方不存在");
                }
            }
        }
        void timer_Turn_Tick(object sender, EventArgs e)
        {
            AutoTurn();
        }

        void timer_Presc_Tick(object sender, EventArgs e)
        {
            RefreshList();
        }

        int record = 0;
        private void timer_SS_Tick(object sender, EventArgs e)
        {
            record++;
            Color c = new Color();
            if (record % 2 == 0)
            {
                c = Colors.Crimson;
            }
            else
            {
                c = Colors.Orange;
            }
            foreach (Border b in spMacs.Children)
            {
                string mac = b.Tag.ToString();
                if (SSMacs.Contains(mac))
                    b.Background = new SolidColorBrush(c);
            }
        }

        private void btStart_Click(object sender, RoutedEventArgs e)
        {
            Button b = sender as Button;
            string prescNo = b.Tag.ToString();
            List<Presc> ps = lvWait.ItemsSource as List<Presc>;
            foreach(Presc p in ps)
            {
                if (p.PrescNo == prescNo)
                {
                    ShowPresc(p.PrescNo, p.PatName, p.WindowName, p.DoType, true);
                    break;
                }
            }
        }
        private void btDelete_Click(object sender, RoutedEventArgs e)
        {
            Button b = sender as Button;
            string prescNo = b.Tag.ToString();
            List<Presc> ps = lvWait.ItemsSource as List<Presc>;
            foreach (Presc p in ps)
            {
                if (p.PrescNo == prescNo)
                {
                    if (csMsg.ShowQuestion("确定要删除该处方吗？", false))
                    {
                        string sql = "update pat_prescinfo set doflag='C' where prescno='{0}'";
                        sql = string.Format(sql, prescNo);
                        csSql.ExecuteSql(sql, Config.Soft.ConnString);

                        ps.Remove(p);
                        lvWait.ItemsSource = ps.ToList();
                        break;
                    }
                }
            }
        }

        private void btStart_Over_Click(object sender, RoutedEventArgs e)
        {
            Button b = sender as Button;
            string prescNo = b.Tag.ToString();
            List<Presc> ps = lvOverTime.ItemsSource as List<Presc>;
            foreach (Presc p in ps)
            {
                if (p.PrescNo == prescNo)
                {
                    ShowPresc(p.PrescNo, p.PatName, p.WindowName, p.DoType, true);
                    break;
                }
            }
        }
        private void btDelete_Over_Click(object sender, RoutedEventArgs e)
        {
            Button b = sender as Button;
            string prescNo = b.Tag.ToString();
            List<Presc> ps = lvOverTime.ItemsSource as List<Presc>;
            foreach (Presc p in ps)
            {
                if (p.PrescNo == prescNo)
                {
                    if (csMsg.ShowQuestion("确定要删除该处方吗？", false))
                    {
                        string sql = "update pat_prescinfo set doflag='C' where prescno='{0}'";
                        sql = string.Format(sql, prescNo);
                        csSql.ExecuteSql(sql, Config.Soft.ConnString);

                        ps.Remove(p);
                        lvOverTime.ItemsSource = ps.ToList();
                        break;
                    }
                }
            }
        }

        private void btRePei_Click(object sender, RoutedEventArgs e)
        {
            Button b = sender as Button;
            string prescNo = b.Tag.ToString();
            List<Presc> ps = lvDone.ItemsSource as List<Presc>;
            foreach (Presc p in ps)
            {
                if (p.PrescNo == prescNo)
                {
                    string s = "update pat_druginfo set doflag='N' where prescno='{0}';";
                    string sql = string.Format(s, prescNo);
                    csSql.ExecuteSql(sql, Config.Soft.ConnString);

                    ps.Remove(p);
                    lvDone.ItemsSource = ps.ToList();
                    break;
                }
            }
        }

        private void tbRefresh_Click(object sender, RoutedEventArgs e)
        {
            RefreshList();
        }

        private void btClear_Click(object sender, RoutedEventArgs e)
        {
            List<Presc> ps = lvWait.ItemsSource as List<Presc>;
            string sql = "";
            foreach (Presc p in ps)
            {
                string s = "update pat_prescsinfo set doflag='C' where prescno='{0}';";
                sql += string.Format(s, p.PrescNo);
            }
            if (!string.IsNullOrEmpty(sql))
                csSql.ExecuteSql(sql, Config.Soft.ConnString);

            ps.Clear();
            lvWait.ItemsSource = ps.ToList();
        }

        private void btConfirm_Click(object sender, RoutedEventArgs e)
        {
            string prescNo = tbOrderNo.Text.Trim();
            if (lvIn.Items.Count > 0)
            {
                string name = tbName.Text.Trim();
                //string windowNo = tbWindowNo.Text.Trim();
                string windowName = tbWindowName.Text.Trim();
                string type = tbType.Text.Trim();

                List<Drug> ds = lvIn.ItemsSource as List<Drug>;

                foreach (Drug d in ds)
                {
                    if (d.BackColor == "LightCoral")
                    {
                        string sql = @"update pat_druginfo set doflag='Y',dotime=getdate() where prescno='{0}' and drugonlycode='{1}' and doflag='N';
update drug_pos set drugnum=(drugnum-{2}) where maccode='{3}' and poscode='{4}' and drugonlycode='{5}'";
                        sql = string.Format(sql, prescNo, d.DrugOnlyCode, d.DrugNum, Config.Soft.MacCode, d.PosCode, d.DrugOnlyCode);
                        csSql.ExecuteSql(sql, Config.Soft.ConnString);
                        //d.BackColor = "LimeGreen";
                    }
                }
                PLC.LightAllNum(PLC.LightType.Close);
                ShowPresc(prescNo, name, windowName, type, true);
            } 
            //机外直接完成
            if (lvOut.Items.Count > 0)
            {
                List<Drug> ds = lvOut.ItemsSource as List<Drug>;

                string sql = "";
                foreach (Drug d in ds)
                {
                    if (string.IsNullOrEmpty(d.MacCode))
                    {
                        string s = "update pat_druginfo set doflag='Y',dotime=getdate() where prescno='{0}' and drugonlycode='{1}' and doflag='N';";
                        sql += string.Format(s, prescNo, d.DrugOnlyCode);
                    }
                }
                if (!string.IsNullOrEmpty(sql))
                    csSql.ExecuteSql(sql, Config.Soft.ConnString);
            }
        }
        private void btContinue_Click(object sender, RoutedEventArgs e)
        {
            string prescNo = tbOrderNo.Text.Trim();
            string name = tbName.Text.Trim();
            //string windowNo = tbWindowNo.Text.Trim();
            string windowName = tbWindowName.Text.Trim();
            string type = tbType.Text.Trim();
            if (!string.IsNullOrEmpty(prescNo))
                ShowPresc(prescNo, name, windowName, type, true);
        }

        private void tbCode_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            tbCode.SelectAll();
            ShowKey(true);
            //csKey.Show(300, 300);
        }

        private void tbCode_TextChanged(object sender, TextChangedEventArgs e)
        {
            RefreshList();
        }

        private void tbCode_LostFocus(object sender, RoutedEventArgs e)
        {
            ShowKey(false);
            //csKey.Close();
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            timer_Presc.Stop();
            timer_SS.Stop();
            timer_Turn.Stop();
            PLC.LightAllNum(PLC.LightType.Close);
        }

        private void lvIn_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lvIn.SelectedItem != null)
            {
                List<Drug> ds = lvIn.ItemsSource as List<Drug>;
                foreach (Drug d in ds)
                {
                    if (d.BackColor == "LightCoral")
                        d.BackColor = "Khaki";
                }

                Drug dd = lvIn.SelectedItem as Drug;
                if (dd.BackColor == "Khaki")
                {
                    
                    dd.BackColor = "LightCoral";
                }
                ShowPos(dd.PosCode);
            }
        }
        public static void ShowPos(string pos)
        {
            int lay = int.Parse(pos.Substring(1, 2));
            int toLay = 0;
            if (lay % 2 == 0) //偶数，一个目标层
                toLay = lay - 1;
            else //奇数，两个目标层
            {
                int lay1 = 0, lay2 = 0;
                int nowLay = PLC.ReadNowLay();
                if (lay == 1)
                {
                    lay1 = 19; lay2 = 1;
                }
                else
                {
                    lay1 = lay - 2;
                    lay2 = lay;
                }
                //判断更近层
                if (nowLay == lay1)
                    toLay = lay1;
                else if (nowLay == lay2)
                    toLay = lay2;
                else
                {
                    if (lay1 < lay2)
                    {
                        int m = 0, n = 0;
                        if (nowLay < lay1)
                        {
                            m = Math.Abs(nowLay - lay1);
                            n = Math.Abs(nowLay + 20 - lay2);
                        }
                        else
                        {
                            m = Math.Abs(nowLay - 20 - lay1);
                            n = Math.Abs(nowLay - lay2);
                        }
                        if (m <= n)
                        {
                            toLay = lay1;
                        }
                        else
                        {
                            toLay = lay2;
                        }
                    }
                    else
                    {
                        int m = 0, n = 0;
                        m = Math.Abs(nowLay - lay1);
                        n = Math.Abs(nowLay - lay2);
                        if (m <= n)
                        {
                            toLay = lay1;
                        }
                        else
                        {
                            toLay = lay2;
                        }
                    }
                }
            }


            PLC.TurnTo(toLay);
            PLC.LightAllNum(PLC.LightType.Close);
            //亮灯
            List<string> lightPoss = new List<string>();
            if (lay >= toLay)
                PLC.LightSingleNum(lay - toLay + 1, int.Parse(pos.Substring(3, 2)), PLC.LightType.Open);
            else
                PLC.LightSingleNum(Config.Mac.Count_GT * 2 + lay - toLay + 1, int.Parse(pos.Substring(3, 2)), PLC.LightType.Open);
        }
    }
}
