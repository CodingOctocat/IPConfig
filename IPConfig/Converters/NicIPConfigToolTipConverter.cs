using System;
using System.Globalization;
using System.Windows.Data;

using IPConfig.Languages;
using IPConfig.Models;

namespace IPConfig.Converters;

public class NicIPConfigToolTipConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values is [Nic nic, IPConfigModel config])
        {
            return $"{nic}\n\n{config?.IPv4Config}\n\n{Lang.Shortcut_F11}".Trim();
        }

        return Lang.NoData;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
