using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace OOP_FINALS
{
    public class StatusToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string status = value as string;
            if (status == null) return new SolidColorBrush(Color.FromRgb(158, 158, 158));

            if (status == "Paid")
                return new SolidColorBrush(Color.FromRgb(76, 175, 80));
            else if (status == "Pending")
                return new SolidColorBrush(Color.FromRgb(255, 152, 0));
            else if (status == "Failed")
                return new SolidColorBrush(Color.FromRgb(244, 67, 54));
            else
                return new SolidColorBrush(Color.FromRgb(158, 158, 158));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}