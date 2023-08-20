using System;
using System.Net;

using IPConfig.Languages;

namespace IPConfig.Models.Validations;

public sealed class IPValidationAttribute : ValidationLangAttributeBase
{
    public IPValidationAttribute(LangKey langKey) : base()
    {
        LangKey = langKey;
    }

    public override bool IsValid(object? value)
    {
        if (String.IsNullOrEmpty(value?.ToString()))
        {
            return true;
        }

        return IPAddress.TryParse(value?.ToString(), out _);
    }
}
