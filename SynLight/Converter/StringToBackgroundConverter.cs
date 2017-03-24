using System;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows;


namespace SynLight.Converter
{
    class StringToBackgroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if ((string)value == "true")
                return "Lime";
            if ((string)value == "false")
                return "Red";
            LinearGradientBrush myVerticalGradient = new LinearGradientBrush();
            myVerticalGradient.StartPoint = new Point(0.5, 0);
            myVerticalGradient.EndPoint = new Point(0.5, 1);
            myVerticalGradient.GradientStops.Add(new GradientStop(Colors.White, 0));
            myVerticalGradient.GradientStops.Add(new GradientStop(Colors.LightGray, 1.0));
            return myVerticalGradient;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
