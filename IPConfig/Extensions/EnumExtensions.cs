using System;
using System.ComponentModel;

namespace IPConfig.Extensions;

public static class EnumExtensions
{
    public static string GetDescription(this Enum enumValue)
    {
        var field = enumValue.GetType().GetField(enumValue.ToString());

        if (field is null)
        {
            return enumValue.ToString();
        }

        object[] attributes = field.GetCustomAttributes(typeof(DescriptionAttribute), false);

        if (Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute)) is DescriptionAttribute attribute)
        {
            return attribute.Description;
        }

        return enumValue.ToString();
    }
}
