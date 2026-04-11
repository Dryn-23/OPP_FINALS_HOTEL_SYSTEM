using System;
using System.Globalization;
using System.Windows.Data;

namespace CustomerDashboard
{
    public class HighlightConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values[0] == null || values[1] == null)
                return false;

            string text = values[0].ToString().ToLower();
            string search = values[1].ToString().ToLower();

            if (string.IsNullOrEmpty(search))
                return false;

            return text.Contains(search);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}