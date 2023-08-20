using System;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Windows.Data;

namespace IPConfig.Converters;

public class GetIPCIDRConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        string? str = value as string;

        if (IPAddress.TryParse(str, out var mask))
        {
            return mask.GetAddressBytes().Sum(x => System.Convert.ToString(x, 2).Count(x => x == '1'));
        }

        return null;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
