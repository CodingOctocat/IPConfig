using System;
using System.Globalization;
using System.Net.NetworkInformation;
using System.Windows.Data;

using IPConfig.Languages;

namespace IPConfig.Converters;

public class OperationalStatusToolTipConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is OperationalStatus v)
        {
            return v switch {
                OperationalStatus.Up => Lang.OpStatus_Up_ToolTip,
                OperationalStatus.Down => Lang.OpStatus_Down_ToolTip,
                OperationalStatus.Testing => Lang.OpStatus_Testing_ToolTip,
                OperationalStatus.Unknown => Lang.OpStatus_Unknown_ToolTip,
                OperationalStatus.Dormant => Lang.OpStatus_Dormant_ToolTip,
                OperationalStatus.NotPresent => Lang.OpStatus_NotPresent_ToolTip,
                OperationalStatus.LowerLayerDown => Lang.OpStatus_LowerLayerDown_ToolTip,
                _ => Binding.DoNothing
            };
        }

        return value?.ToString();
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
