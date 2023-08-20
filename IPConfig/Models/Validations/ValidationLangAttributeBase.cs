using System;
using System.ComponentModel.DataAnnotations;

using IPConfig.Languages;

namespace IPConfig.Models.Validations;

public abstract class ValidationLangAttributeBase : ValidationAttribute
{
    protected LangKey _langKey;

    public LangKey LangKey
    {
        get => _langKey;
        set
        {
            _langKey = value;
            ErrorMessageResourceType = typeof(Lang);
            ErrorMessageResourceName = value.ToString();
        }
    }

    protected ValidationLangAttributeBase(LangKey langKey) : base()
    {
        LangKey = langKey;
    }

    protected ValidationLangAttributeBase() : base()
    { }

    protected ValidationLangAttributeBase(Func<string> errorMessageAccessor) : base(errorMessageAccessor)
    { }

    protected ValidationLangAttributeBase(string errorMessage) : base(errorMessage)
    { }
}
