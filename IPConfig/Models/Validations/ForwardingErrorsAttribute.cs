using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace IPConfig.Models.Validations;

/// <summary>
/// 错误转发。用于解决嵌套(子)属性为复杂对象时，错误能够转发到调用类。
/// <para>
/// 属性还需注册 <seealso cref="INotifyDataErrorInfo.ErrorsChanged"/> 事件以执行此验证。
/// <code>
/// ObjcetProperty.ErrorsChanged += (s, e) => ValidateProperty(ObjcetProperty, nameof(ObjcetProperty));
/// </code>
/// </para>
/// </summary>
public sealed class ForwardingErrorsAttribute : ValidationAttribute
{
    public override bool IsValid(object? value)
    {
        ArgumentNullException.ThrowIfNull(value);

        return !((INotifyDataErrorInfo)value).HasErrors;
    }
}
