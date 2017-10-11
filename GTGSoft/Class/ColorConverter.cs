using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Windows.Media;
using System.Globalization;

namespace GTGSoft
{
   public sealed class ColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string s = "";
            string t = value.GetType().ToString();
            switch (t)
            {
                case "GTGSoft.UCAuto+Drug":
                    UCAuto.Drug v1 = value as UCAuto.Drug;
                    s = v1.BackColor;
                    break;
                case "GTGSoft.UCAuto+Presc":
                    UCAuto.Presc v2 = value as UCAuto.Presc;
                    s = v2.BackColor;
                    break;
            }

            Color c = Colors.White;
            switch (s)
            {
                case "LightBlue":
                    c = Colors.LightBlue;
                    break;
                case "LimeGreen":
                    c = Colors.LimeGreen;
                    break;
                case "LightSalmon":
                    c = Colors.LightSalmon;
                    break;
                case "Crimson":
                    c = Colors.Crimson;
                    break;
                case "Gray":
                    c = Colors.Gray;
                    break;
                case "LightGray":
                    c = Colors.LightGray;
                    break;
                case "Red":
                    c = Colors.Red;
                    break;
                case "DarkOrange":
                    c = Colors.DarkOrange;
                    break;
                case "Khaki":
                    c = Colors.Khaki;
                    break;
                case "LightCoral":
                    c = Colors.LightCoral;
                    break;
            }
            return new SolidColorBrush(c);
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
