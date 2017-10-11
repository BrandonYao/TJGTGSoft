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
using System.Windows.Media.Effects;
using System.Windows.Threading;
using System.Threading;
using System.Data;

namespace GTGSoft
{
    /// <summary>
    /// UCDebug.xaml 的交互逻辑
    /// </summary>
    public partial class UCDebug : UserControl
    {
        CSHelper.Msg csMsg = new CSHelper.Msg();
        CSHelper.SQL csSql = new CSHelper.SQL();

        public UCDebug()
        {
            InitializeComponent();
        }

        private void ShowLayerSet()
        {
            for (int i = 0; i < 2; i++)
            {
                gridLayerSet.RowDefinitions.Add(new RowDefinition());
                for (int j = 0; j < 5; j++)
                {
                    if (i == 0)
                        gridLayerSet.ColumnDefinitions.Add(new ColumnDefinition());
                    Button b = new Button();
                    b.Content = "设定" + (i * 10 + (j * 2 + 1)).ToString().PadLeft(2, '0') + "层";
                    b.Tag = i * 10 + (j * 2 + 1);
                    b.Margin = new Thickness(5);
                    b.Click += new RoutedEventHandler(Set_Click);
                    gridLayerSet.Children.Add(b);
                    Grid.SetRow(b, i);
                    Grid.SetColumn(b, j);
                }
            }
        }

        void Set_Click(object sender, RoutedEventArgs e)
        {
            Button bt = sender as Button;
            string lay = bt.Tag.ToString();
            if (csMsg.ShowQuestion("确定要设置该层脉冲吗？", false))
            {
                //读取当前脉冲
                int pulse = PLC.ReadNowPulse();
                PLC.SavePulse(int.Parse(lay), pulse);
            }
        }

        private void ShowLayerTurn()
        {
            for (int i = 0; i < 2; i++)
            {
                gridLayerTurn.RowDefinitions.Add(new RowDefinition());
                for (int j = 0; j < 5; j++)
                {
                    if (i == 0)
                        gridLayerTurn.ColumnDefinitions.Add(new ColumnDefinition());
                    Button b = new Button();
                    b.Content = "转到" + (i * 10 + (j * 2 + 1)).ToString().PadLeft(2, '0') + "层";
                    b.Tag = i * 10 + (j * 2 + 1);
                    b.Margin = new Thickness(5);
                    b.Click += new RoutedEventHandler(Turn_Click);
                    gridLayerTurn.Children.Add(b);
                    Grid.SetRow(b, i);
                    Grid.SetColumn(b, j);
                }
            }
            //for (int i = 0; i < 10; i++)
            //{
            //    gridLayerTurn.ColumnDefinitions.Add(new ColumnDefinition());
            //    Button b = new Button();
            //    b.Content = "转到" + (i * 2 + 1).ToString().PadLeft(2, '0') + "层";
            //    b.Tag = i * 2 + 1;
            //    b.Margin = new Thickness(5);
            //    b.Click += new RoutedEventHandler(Turn_Click);
            //    gridLayerTurn.Children.Add(b);
            //    Grid.SetColumn(b, i);
            //}
        }
        void Turn_Click(object sender, RoutedEventArgs e)
        {
            Button bt = sender as Button;
            string lay = bt.Tag.ToString();
            PLC.TurnTo(int.Parse(lay));
        }

        private void ShowLasers()
        {
            for (int i = 0; i < Config.Mac.ShowRowCount; i++)
            {
                gridLaser.RowDefinitions.Add(new RowDefinition());
                for (int j = 0; j < Config.Mac.Count_Col; j++)
                {
                    if (i == 0)
                        gridLaser.ColumnDefinitions.Add(new ColumnDefinition());

                    TextBlock tb = new TextBlock();
                    tb.Text = (i + 1).ToString() + "-" + (j + 1).ToString();
                    tb.HorizontalAlignment = HorizontalAlignment.Center;
                    tb.VerticalAlignment = VerticalAlignment.Center;
                    tb.Foreground = new SolidColorBrush(Colors.Black);
                    gridLaser.Children.Add(tb);
                    Grid.SetRow(tb, 2 - i);
                    Grid.SetColumn(tb, j);

                    Ellipse b = new Ellipse();
                    b.Fill = new SolidColorBrush(Color.FromArgb(150, 128, 128, 128));
                    double width = gridLaser.ActualWidth / 12 - 10;
                    double height = gridLaser.ActualHeight / 3 - 10;
                    b.Width = b.Height = width < height ? width : height;
                    b.Margin = new Thickness(5);
                    b.Stroke = new SolidColorBrush(Colors.Black);
                    DropShadowEffect dse = new DropShadowEffect();
                    dse.ShadowDepth = 0;
                    b.Effect = dse;
                    b.Tag = (i + 1).ToString() + "-" + (j + 1).ToString() + "-0";
                    b.MouseDown += new MouseButtonEventHandler(b_MouseDown);
                    gridLaser.Children.Add(b);
                    Grid.SetRow(b, 2 - i);
                    Grid.SetColumn(b, j);

                }
            }
        }
        void b_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Ellipse b = sender as Ellipse;
            string[] ss = b.Tag.ToString().Split(new char[] { '-' });
            int i = Math.Abs(int.Parse(ss[2]) - 1);
            b.Tag = ss[0] + "-" + ss[1] + "-" + i.ToString();
            switch (i)
            {
                case 0:
                    b.Fill = new SolidColorBrush(Color.FromArgb(150, 128, 128, 128));
                    PLC.LightSingleNum(int.Parse(ss[0]), int.Parse(ss[1]), PLC.LightType.Close);
                    break;
                case 1:
                    b.Fill = new SolidColorBrush(Color.FromArgb(150, 220, 20, 60));
                    PLC.LightSingleNum(int.Parse(ss[0]), int.Parse(ss[1]), PLC.LightType.Open);
                    break;
            }
        }

        DispatcherTimer timer_Pulse = new DispatcherTimer();

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            timer_Pulse.Interval = TimeSpan.FromSeconds(2);
            timer_Pulse.Tick += new EventHandler(timer_Pulse_Tick);

            if (Config.Mac.PLCIsEnable == "Y")
            {
                tbPulse.Text = PLC.ReadNowPulse().ToString();
                tbLayer.Text = PLC.ReadNowLay().ToString();
                timer_Pulse.Start();
            }

            ShowLayerSet();
            ShowLayerTurn();
            ShowLasers();
        }

        void timer_Pulse_Tick(object sender, EventArgs e)
        {
            tbPulse.Text = PLC.ReadNowPulse().ToString();
            tbLayer.Text = PLC.ReadNowLay().ToString();
        }

        private void btZero_Click(object sender, RoutedEventArgs e)
        {
            Cursor = Cursors.Wait;
            PLC.BackZero();
            DateTime timeBegin = DateTime.Now;
            Thread.Sleep(1000);

            while (!PLC.BackZeroIsFinished())
            {
                if (DateTime.Now > timeBegin.AddSeconds(60))
                {
                    csMsg.ShowWarning("原点返回超时", false);
                    break;
                }
                Thread.Sleep(1000);
            }

            if (PLC.BackZeroIsFinished())
            {
                csMsg.ShowInfo("原点返回完成", false);
            }
            Cursor = null;
        }

        private void btPerimeter_Click(object sender, RoutedEventArgs e)
        {
            Cursor = Cursors.Wait;
            PLC.TestZC();
            DateTime timeBegin = DateTime.Now;
            Thread.Sleep(1000);

            while (!PLC.TestZCIsFinished())
            {
                if (DateTime.Now > timeBegin.AddSeconds(60))
                {
                    csMsg.ShowWarning("测周长失败", false);
                    break;
                }
                Thread.Sleep(1000);
            }

            if (PLC.TestZCIsFinished())
            {
                csMsg.ShowInfo("测周长完成", false); 
                int sumPulse = PLC.ReadNowPulse();
                Config.SaveConfig(Config.Soft.MacCode, "Perimeter", sumPulse.ToString());
            }
            Cursor = null;
        }

        private void btPresc_Click(object sender, RoutedEventArgs e)
        {
                string prescNo = Guid.NewGuid().ToString();
                string payTime = DateTime.Now.ToString();

                string sql = "select distinct drugonlycode from drug_pos where maccode='{0}'";
                sql = string.Format(sql, Config.Soft.MacCode);
                DataTable dtDrug;
                csSql.ExecuteSelect(sql, Config.Soft.ConnString, out dtDrug);
                if (dtDrug != null && dtDrug.Rows.Count > 0)
                {
                    string win = "1"; string name = GetName();
                    string w = "select windowno from sys_window";
                    DataTable dtWin; csSql.ExecuteSelect(w, Config.Soft.ConnString, out dtWin);
                    if (dtWin != null && dtWin.Rows.Count > 0)
                    {
                        Random rd = new Random();
                        win = dtWin.Rows[rd.Next(0, dtWin.Rows.Count)][0].ToString().Trim();
                    }
                    string s = "insert into pat_prescinfo (prescno,windowno,mrno,patname,depname,paytime,doflag,createtime,dotype) values ('{0}',{1},'001','{2}','科室A','{3}','W',getdate(),'P');";
                    sql = string.Format(s, prescNo, win, name, payTime);
                    //默认种数
                    int num = 10;// Config.Mac.DrugCount;
                    int m = dtDrug.Rows.Count;
                    if (m < num)
                        num = m;

                    List<string> ds = new List<string>();
                    //随机出药
                    for (int i = 1; i <= num; i++)
                    {
                        Random rd = new Random();
                        int n = rd.Next(0, m);
                        string drugonlycode = dtDrug.Rows[n]["drugonlycode"].ToString().Trim();
                        if (!ds.Contains(drugonlycode))
                            ds.Add(drugonlycode);
                        else
                        {
                            i--;
                            continue;
                        }
                        int count = rd.Next(1, 5);
                        s = "insert into pat_druginfo(prescno,drugonlycode,drugnum,drugunit,doflag) select '{0}','{1}','{2}',drugpackunit,'N' from drug_info where drugonlycode='{1}'";
                        sql += string.Format(s, prescNo, drugonlycode, count);
                    }
                    if (csSql.ExecuteSql(sql, Config.Soft.ConnString))
                        csMsg.ShowInfo("生成处方", false);
                }
                else
                    csMsg.ShowWarning("本机未分配任何药品", false);
        }

        private void btUp_Click(object sender, RoutedEventArgs e)
        {
            btUp.IsEnabled = false;
            btStop.IsEnabled = true;
            btDown.IsEnabled = false;
            PLC.Turn(PLC.TurnType.Up);
        }
        private void btDown_Click(object sender, RoutedEventArgs e)
        {
            btUp.IsEnabled = false;
            btStop.IsEnabled = true;
            btDown.IsEnabled = false;
            PLC.Turn(PLC.TurnType.Down);
        }
        private void btStop_Click(object sender, RoutedEventArgs e)
        {
            btUp.IsEnabled = true;
            btStop.IsEnabled = false;
            btDown.IsEnabled = true;
            PLC.Turn(PLC.TurnType.Stop);
        }

        private void btAllOn_Click(object sender, RoutedEventArgs e)
        {
            PLC.LightAllNum(PLC.LightType.Open);
        }
        private void btAllOff_Click(object sender, RoutedEventArgs e)
        {
            PLC.LightAllNum(PLC.LightType.Close);
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            PLC.LightAllNum(PLC.LightType.Close);
            timer_Pulse.Stop();
        }

        #region"随机姓名"
        string[] FirstNames = new string[]{
                               "赵","钱","孙","李","周","吴","郑","王","冯","陈","褚","卫","蒋","沈","韩","杨","朱","秦","尤","许",
                               "何","吕","施","张","孔","曹","严","华","金","魏","陶","姜","戚","谢","邹","喻","柏","水","窦","章",
                               "云","苏","潘","葛","奚","范","彭","郎","鲁","韦","昌","马","苗","凤","花","方","俞","任","袁","柳",
                               "酆","鲍","史","唐","费","廉","岑","薛","雷","贺","倪","汤","滕","殷","罗","毕","郝","邬","安","常",
                               "乐","于","时","傅","皮","卞","齐","康","伍","余","元","卜","顾","孟","平","黄","和","穆","萧","尹",
                               "姚","邵","湛","汪","祁","毛","禹","狄","米","贝","明","臧","计","伏","成","戴","谈","宋","茅","庞",
                               "熊","纪","舒","屈","项","祝","董","梁","杜","阮","蓝","闵","席","季","麻","强","贾","路","娄","危",
                               "江","童","颜","郭","梅","盛","林","刁","钟","徐","邱","骆","高","夏","蔡","田","樊","胡","凌","霍",
                               "虞","万","支","柯","昝","管","卢","莫","经","房","裘","缪","干","解","应","宗","丁","宣","贲","邓",
                               "郁","单","杭","洪","包","诸","左","石","崔","吉","钮","龚","程","嵇","邢","滑","裴","陆","荣","翁",
                               "荀","羊","於","惠","甄","曲","家","封","芮","羿","储","靳","汲","邴","糜","松","井","段","富","巫",
                               "乌","焦","巴","弓","牧","隗","山","谷","车","侯","宓","蓬","全","郗","班","仰","秋","仲","伊","宫",
                               "宁","仇","栾","暴","甘","钭","厉","戎","祖","武","符","刘","景","詹","束","龙","叶","幸","司","韶",
                               "郜","黎","蓟","薄","印","宿","白","怀","蒲","台","丛","鄂","索","咸","籍","赖","卓","蔺","屠","蒙",
                               "池","乔","阴","郁","胥","能","苍","双","闻","莘","党","翟","谭","贡","劳","逄","姬","申","扶","堵",
                               "冉","宰","郦","雍","却","璩","桑","桂","濮","牛","寿","通","边","扈","燕","冀","郏","浦","尚","农",
                               "温","别","庄","晏","柴","瞿","阎","充","慕","连","茹","习","宦","艾","鱼","容","向","古","易","慎",
                               "戈","廖","庚","终","暨","居","衡","步","都","耿","满","弘","匡","国","文","寇","广","禄","阙","东",
                               "殴","殳","沃","利","蔚","越","夔","隆","师","巩","厍","聂","晁","勾","敖","融","冷","訾","辛","阚",
                               "那","简","饶","空","曾","毋","沙","乜","养","鞠","须","丰","巢","关","蒯","相","查","后","荆","红",
                               "游","竺","权逯","盖益","桓","公","万俟","司马","上官","欧阳","夏侯","诸葛",
                               "闻人","东方","赫连","皇甫","尉迟","公羊","澹台","公冶","宗政","濮阳",
                               "淳于","单于","太叔","申屠","公孙","仲孙","轩辕","令狐","钟离","宇文",
                               "长孙","慕容","鲜于","闾丘","司徒","司空","亓官","司寇","仉","督","子车",
                               "颛孙","端木","巫马","公西","漆雕","乐正","壤驷","公良","拓跋","夹谷",
                               "宰父","谷粱","晋","楚","闫","法","汝","鄢","涂","钦","段干","百里","东郭","南门",
                               "呼延","归海","羊舌","微生","岳","帅","缑","亢","况","郈","有","琴","梁丘","左丘",
                               "东门","西门","商","牟","佘","佴","伯","赏","宫","墨","哈","谯","笪","年","爱","阳","佟",
                               "第五","言福"       
                             };
        string[] SecondNames = new string[]{
                                  "白", "赤", "凉", "靖", "剑", "谙", "仪", "翔", "遐", "翚", "桓", "鸠", "梅", "美", "笛", "古",
                                  "弘", "勋", "秀", "晴", "子", "竞", "溢", "澜", "云", "启", "宣", "恭", "劲", "聪", "冀", "洪", 
                                  "景", "炎", "昌", "久", "零", "落", "千", "言", "弼", "光", "缘", "逸", "欣", "宥", "远", "霞", 
                                  "碧", "空", "长", "虹", "耀", "月", "鹏", "飞", "宗", "翰", "毓", "灵", "星", "辉", "辅", "国", 
                                  "靖", "初", "君", "让", "昭", "寒", "攸", "讳", "天", "佑", "晨", "曦", "北", "辰", "敬", "弦", 
                                  "起", "乾", "承", "嗣", "云", "啸", "海", "潜", "百", "炼", "万", "言", "炳", "之", "语", "晴", 
                                  "无", "咎", "不", "疑", "复", "生", "鸢", "戾", "申", "曦", "益", "川", "休", "雨", "毅", "玄", 
                                  "宏", "述", "汤", "浩", "震", "岳", "晓", "岚", "天", "邃", "之", "嘉", "鲲", "鹏", "颜", "承", 
                                  "若", "子", "谅", "霖", "朔", "风", "凯", "逍", "遥", "欢", "芷", "庆", "哲", "有", "涯", "公", 
                                  "焕", "海", "程", "城", "龙", "雪", "君" 
                              };
        private string GetName()
        {
            Random rd = new Random();
            return FirstNames[rd.Next(FirstNames.Length)] + SecondNames[rd.Next(SecondNames.Length)];
        }
        #endregion

    }
}
