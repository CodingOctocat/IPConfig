using System;
using System.Globalization;
using System.Windows.Data;

namespace IPConfig.Converters;

public class StringToIntConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return Int32.TryParse(value?.ToString(), out int v) ? v : value;
    }

    public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value?.ToString();
    }
}
