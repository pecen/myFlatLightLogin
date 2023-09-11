using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace myFlatLightLogin.UI.Wpf.Converter
{
    public class BoolToVisibilityPwdText : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            //var v = value;// as string;
            //if (!string.IsNullOrEmpty(v) && v.Length >= 1)
            //    return Visibility.Collapsed; //(bool)value ? Visibility.Visible : Visibility.Collapsed;
            //else
            //    return Visibility.Visible;


            return (bool)value ? Visibility.Collapsed : Visibility.Visible;

            //return !string.IsNullOrEmpty(v) && v.Length >= 1;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
