using System;
using System.Globalization;
using System.Windows.Data;

using HandyControl.Data;

namespace IPConfig.Converters;

public class SkinTypeToImageConverter : IValueConverter
{
    private static readonly Uri _dark = new("/Resources/crescent_moon_3d.png", UriKind.Relative);

    private static readonly Uri _light = new("/Resources/sun_3d.png", UriKind.Relative);

    private static readonly Uri _system = new("/Resources/artist_palette_3d.png", UriKind.Relative);

    private static readonly Uri _violet = new("/Resources/purple_circle_3d.png", UriKind.Relative);

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var skinType = value as SkinType?;

        return skinType switch {
            SkinType.Default => _light,
            SkinType.Dark => _dark,
            SkinType.Violet => _violet,
            _ => _system
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
