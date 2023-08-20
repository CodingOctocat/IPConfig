using System;
using System.Globalization;
using System.Windows.Data;

using IPConfig.Languages;
using IPConfig.Models;

namespace IPConfig.Converters;

public class SelectedNicIPConfigNameConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is IPConfigModel iPConfig)
        {
            return iPConfig.Name;
        }

        return LangSource.Instance[LangKey.AdapterNotFound];
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
