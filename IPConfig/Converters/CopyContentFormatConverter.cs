using System;
using System.Globalization;
using System.Windows.Data;

using IPConfig.Languages;

namespace IPConfig.Converters;

public class CopyContentFormatConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return String.Format(LangSource.Instance[LangKey.CopyContent_Format_], value);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
