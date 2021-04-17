using System;
using System.Windows.Data;

namespace SynLight.Converter
{
    class BoolToVisib : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            bool toCompare = true;

            if (parameter != null)
                if (bool.TryParse(parameter.ToString(), out bool b_parameter))
                    toCompare = b_parameter;

            if (bool.TryParse(value.ToString(), out bool _value))
                return _value == toCompare ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;

            return System.Windows.Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return 0;
        }
    }
}