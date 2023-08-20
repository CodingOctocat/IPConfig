using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

using IPConfig.Languages;

namespace IPConfig.Models.Validations;

public sealed class RequiredIfAttribute<T> : ValidationAttribute
{
    public T? DesiredValue { get; set; }

    public string PropertyName { get; set; }

    public RequiredIfAttribute(string propertyName, T? desiredValue)
    {
        PropertyName = propertyName;
        DesiredValue = desiredValue;
    }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        object instance = validationContext.ObjectInstance;
        object? otherValue = instance.GetType().GetProperty(PropertyName)?.GetValue(instance);

        bool equals = EqualityComparer<object>.Default.Equals(otherValue, DesiredValue);

        if (equals && String.IsNullOrEmpty(value?.ToString()))
        {
            return new(Lang.Required);
        }

        return ValidationResult.Success;
    }
}
