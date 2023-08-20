using System;
using System.Globalization;
using System.Windows.Data;

using HandyControl.Data;

using IPConfig.Languages;

namespace IPConfig.Converters;

public class SkinTypeToolTipConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var skinType = value as SkinType?;

        return skinType switch {
            SkinType.Default => $"{Lang.Theme}: {Lang.Light}",
            SkinType.Dark => $"{Lang.Theme}: {Lang.Dark}",
            SkinType.Violet => $"{Lang.Theme}: {Lang.Violet}",
            _ => $"{Lang.Theme}: {Lang.UseSystemSetting}"
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
