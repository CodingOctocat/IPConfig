using System;
using System.Globalization;
using System.Windows.Data;

using IPConfig.Extensions;

namespace IPConfig.Converters;

public class EnumDescriptionConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var @enum = (Enum)value;

        return @enum.GetDescription();
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
