using System;
using System.Globalization;
using System.Windows.Data;

using IPConfig.Helpers;
using IPConfig.Languages;

namespace IPConfig.Converters;

public class BytesToFileSizeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        string size = BytesFormatter.ToFileSize(System.Convert.ToInt64(value));
        var key = (LangKey)parameter;
        string transport = LangSource.Instance[key];

        return $"{transport}: ~ {size}";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
