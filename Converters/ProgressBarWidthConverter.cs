using System;
using System.Globalization;
using System.Windows.Data;

namespace BonsaiGotchiGame.Converters
{
    public class ProgressBarWidthConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 4 ||
                !double.TryParse(values[0]?.ToString(), out double value) ||
                !double.TryParse(values[1]?.ToString(), out double min) ||
                !double.TryParse(values[2]?.ToString(), out double max) ||
                !double.TryParse(values[3]?.ToString(), out double actualWidth))
            {
                return 0.0;
            }

            // Enhanced divide-by-zero protection with epsilon value for floating point comparison
            if (Math.Abs(max - min) < 0.0001)
                return 0.0;

            // Ensure value is within bounds
            value = Math.Clamp(value, min, max);

            // Calculate proportion of total width
            return (value - min) / (max - min) * actualWidth;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}