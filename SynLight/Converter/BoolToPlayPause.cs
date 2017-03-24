using System;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows;


namespace SynLight.Converter
{
    class BoolToPlayPause : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if ((bool)value)
            {
                return "STOP";
            }
            else
            {
                return "START";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
