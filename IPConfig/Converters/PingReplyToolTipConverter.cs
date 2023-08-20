using System;
using System.Globalization;
using System.Net.NetworkInformation;
using System.Windows.Data;

using IPConfig.Extensions;
using IPConfig.Languages;

namespace IPConfig.Converters;

public class PingReplyToolTipConverter : IMultiValueConverter
{
    public object? Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values is [null, string dns1])
        {
            return $"ping {dns1}";
        }
        else if (values is [PingReply reply, _])
        {
            return $"""
                {Lang.ReplyFrom_Format.Format(reply.Address)}:
                {Lang.Bytes}={reply.Buffer.Length}
                {Lang.Time}={reply.RoundtripTime}ms
                TTL={reply.Options?.Ttl}
                {reply.Status}
                """;
        }

        return Binding.DoNothing;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
