using System.Windows.Data;

namespace SynLight.Converters
{
    class PercentInvertConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            try
            {
                return (100 - int.Parse(value.ToString())).ToString();
            }
            catch
            {
                return "NAN";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return 0;
        }
    }
}
