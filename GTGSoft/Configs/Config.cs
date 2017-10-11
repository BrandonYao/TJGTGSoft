using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
using System.Data;

namespace GTGSoft
{
    class Config
    {
        static CSHelper.XML csXml = new CSHelper.XML();
        static CSHelper.SQL csSql = new CSHelper.SQL();

        //软件配置
        static Dictionary<string, string> DicsSoft = new Dictionary<string, string>();
        //设备配置
        static Dictionary<string, string> DicsMac = new Dictionary<string, string>();
        //系统配置
        static Dictionary<string, string> DicsSys = new Dictionary<string, string>();

        public class Soft
        {
            public static string Set;

            public static string ConnString;

            public static string Server;
            public static string UserID;
            public static string Password;
            public static string Database;

            public static string HISType;
            public static string ConnString_HIS;

            public static string YFNo;
            public static string MacCode;
            public static string Mode;

            //public static string UserCode;
            //public static string UserName;
        }

        public class Mac
        {
            public static string Port_PLC;

            public static int Count_Unit;
            public static int Count_GT;
            public static int Count_Col;
            public static int Perimeter;

            public static string Speed_Manual;
            public static string Speed_Auto;

            public static string PLCIsEnable;
            public static int RefreshSpan;
            public static int OverTime;

            public static int ShowRowCount;
            public static string AutoTurn;
            public static int TurnSpan;
        }

        public class Sys
        {
            public static string HospitalName;
            public static int EnableTime;
            public static string DrugTime;
        }

        //初始化配置
        public static void InitialConfig_Client()
        {
            Soft.Set = ConfigurationManager.AppSettings["Set"];

            Soft.Server = ConfigurationManager.AppSettings["Server"];
            Soft.UserID = ConfigurationManager.AppSettings["UserID"];
            Soft.Password = ConfigurationManager.AppSettings["Password"];
            Soft.Database = ConfigurationManager.AppSettings["Database"];

            Soft.HISType = ConfigurationManager.AppSettings["HISType"];
            Soft.ConnString_HIS = ConfigurationManager.AppSettings["ConnString_HIS"];

            Soft.YFNo = ConfigurationManager.AppSettings["YFNo"];
            Soft.MacCode = ConfigurationManager.AppSettings["MacCode"];
            Soft.Mode = ConfigurationManager.AppSettings["Mode"];

            Soft.ConnString = "server=" + Soft.Server + ";User ID=" + Soft.UserID + ";Password=" + Soft.Password + ";database=" + Soft.Database + ";connect timeout=1";

        }

        public static void InitialConfig_Server()
        {
            DicsMac = ReadConfig(Soft.MacCode);
            Mac.Port_PLC = DicsMac["Port_PLC"];

            Mac.Count_Unit = int.Parse(DicsMac["Count_Unit"]);
            Mac.Count_GT = int.Parse(DicsMac["Count_GT"]);
            Mac.Count_Col = int.Parse(DicsMac["Count_Col"]);
            Mac.Perimeter = int.Parse(DicsMac["Perimeter"]);

            Mac.Speed_Manual = DicsMac["Speed_Manual"];
            Mac.Speed_Auto = DicsMac["Speed_Auto"];

            Mac.PLCIsEnable = DicsMac["PLCIsEnable"];
            Mac.RefreshSpan = int.Parse(DicsMac["RefreshSpan"]);
            Mac.OverTime = int.Parse(DicsMac["OverTime"]);

            Mac.ShowRowCount = int.Parse(DicsMac["ShowRowCount"]);
            Mac.AutoTurn = DicsMac["AutoTurn"];
            Mac.TurnSpan = int.Parse(DicsMac["TurnSpan"]);

            DicsSys = ReadConfig("Sys");
            Sys.HospitalName = DicsSys["HospitalName"];
            Sys.EnableTime = int.Parse(DicsSys["EnableTime"]);
            Sys.DrugTime = DicsSys["DrugTime"];
        }
        //根据类型读取配置
        static Dictionary<string, string> ReadConfig(string type)
        {
            Dictionary<string, string> dics = new Dictionary<string, string>();

            DataTable dt = new DataTable();
            string sql = "select paracode,paravalue from Sys_Parameter where paragroup='" + type + "'";
            csSql.ExecuteSelect(sql, Config.Soft.ConnString, out dt);
            foreach (DataRow row in dt.Rows)
            {
                dics.Add(row[0].ToString().Trim(), row[1].ToString().Trim());
            }
            return dics;
        }
        //保存配置值
        public static void SaveConfig(string group, string code, string value)
        {
            string sql = "update Sys_Parameter set paravalue='" + value + "' where paragroup='" + group + "' and paracode='" + code + "'";
            csSql.ExecuteSql(sql, Soft.ConnString);
            InitialConfig_Client();
            InitialConfig_Server();
        }
    }
}
