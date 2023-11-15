using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

using IPConfig.Languages;

namespace IPConfig.Models.Validations;

public sealed class RequiredIfAttribute<T>(string propertyName, T? desiredValue) : ValidationAttribute
{
    public T? DesiredValue { get; set; } = desiredValue;

    public string PropertyName { get; set; } = propertyName;

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
