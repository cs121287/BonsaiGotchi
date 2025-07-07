using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace BonsaiGotchiGame.Converters
{
    public class StringNotEmptyToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // If value is null or empty string, return Collapsed, otherwise Visible
            if (value == null || string.IsNullOrEmpty(value.ToString()))
                return Visibility.Collapsed;

            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}