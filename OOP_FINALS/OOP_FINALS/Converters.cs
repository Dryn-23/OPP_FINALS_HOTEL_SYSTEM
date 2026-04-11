using System;
using System.Globalization;
using System.Windows.Data;

namespace OOP_FINALS.Converters
{
    public class WidthToComboBoxConverter : IValueConverter
    {
        public static readonly WidthToComboBoxConverter Instance = new WidthToComboBoxConverter();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double width && parameter != null && double.TryParse(parameter.ToString(), out double pct))
            {
                return Math.Max(120.0, width * pct);
            }
            return 150.0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class WidthToTextBoxConverter : IValueConverter
    {
        public static readonly WidthToTextBoxConverter Instance = new WidthToTextBoxConverter();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double width && parameter != null && double.TryParse(parameter.ToString(), out double pct))
            {
                return Math.Max(180.0, width * pct);
            }
            return 220.0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}