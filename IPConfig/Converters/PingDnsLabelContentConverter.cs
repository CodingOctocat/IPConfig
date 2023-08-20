using System;
using System.Globalization;
using System.Net.NetworkInformation;
using System.Windows.Data;

using IPConfig.Languages;

namespace IPConfig.Converters;

public class PingDnsLabelContentConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not PingReply reply)
        {
            return Lang.Test;
        }

        if (reply.Status == IPStatus.TimedOut)
        {
            return Lang.TimedOut;
        }

        if (reply.Status != IPStatus.Success)
        {
            return Lang.Error;
        }

        return $"{reply.RoundtripTime}ms";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
