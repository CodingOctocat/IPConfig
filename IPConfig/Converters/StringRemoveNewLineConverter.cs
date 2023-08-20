using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows.Data;

namespace IPConfig.Converters;

public partial class StringRemoveNewLineConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string str)
        {
            return RemoveNewLineRegex().Replace(str, " ");
        }

        return value?.ToString();
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }

    [GeneratedRegex(@"\t|\n|\r")]
    private static partial Regex RemoveNewLineRegex();
}
