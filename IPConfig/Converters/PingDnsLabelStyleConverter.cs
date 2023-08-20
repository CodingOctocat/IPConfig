using System;
using System.Globalization;
using System.Net.NetworkInformation;
using System.Windows.Data;

namespace IPConfig.Converters;

public class PingDnsLabelStyleConverter : IValueConverter
{
    public required object ErrorStyle { get; init; }

    public required object FastStyle { get; init; }

    public required object InitStyle { get; init; }

    public required object NormalStyle { get; init; }

    public required object SlowStyle { get; init; }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not PingReply reply)
        {
            return InitStyle;
        }

        if (reply.Status != IPStatus.Success)
        {
            return ErrorStyle;
        }

        return reply.RoundtripTime switch {
            < 40 => FastStyle,
            < 70 => NormalStyle,
            < 5000 => SlowStyle,
            _ => ErrorStyle
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
