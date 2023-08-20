using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;
using System.Windows.Controls;
using System.Windows.Data;

namespace IPConfig.Converters;

public class ValidationErrorsToolTipConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var sb = new StringBuilder();

        if (value is ReadOnlyObservableCollection<ValidationError> errors)
        {
            if (errors is [var err])
            {
                return err.ErrorContent;
            }

            foreach (var error in errors)
            {
                sb.AppendLine($"• {error.ErrorContent}");
            }

            string errStr = sb.ToString().Trim();

            return String.IsNullOrEmpty(errStr) ? null : errStr;
        }

        return null;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
