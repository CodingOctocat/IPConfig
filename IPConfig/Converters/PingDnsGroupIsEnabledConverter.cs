using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

using IPConfig.Helpers;
using IPConfig.Models;

namespace IPConfig.Converters;

public class PingDnsGroupIsEnabledConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is CollectionViewGroup group)
        {
            var dnsItems = GroupItemHelper.GetGroupItems<IPv4Dns>(group);

            return dnsItems.All(x => !x.IsRunning);
        }

        return false;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
