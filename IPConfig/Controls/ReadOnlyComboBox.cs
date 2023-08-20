using System.Windows.Controls;

using HcComboBox = HandyControl.Controls.ComboBox;

namespace IPConfig.Controls;

/// <summary>
/// <see href="https://stackoverflow.com/a/66093753/4380178">XAML ReadOnly ComboBox</see>
/// </summary>
public class ReadOnlyComboBox : HcComboBox
{
    private int _oldSelectedIndex = -1;

    //static ReadOnlyComboBox()
    //{
    //    IsDropDownOpenProperty.OverrideMetadata(typeof(ReadOnlyComboBox), new FrameworkPropertyMetadata(
    //        propertyChangedCallback: delegate { },
    //        coerceValueCallback: (d, value) => {
    //            if (((ReadOnlyComboBox)d).IsReadOnly)
    //            {
    //                // Prohibit opening the drop down when read only.
    //                return false;
    //            }

    //            return value;
    //        }));

    //    IsReadOnlyProperty.OverrideMetadata(typeof(ReadOnlyComboBox), new FrameworkPropertyMetadata(
    //        propertyChangedCallback: (d, e) => {
    //            // When setting "read only" to false, close the drop down.
    //            if (e.NewValue is true)
    //            {
    //                ((ReadOnlyComboBox)d).IsDropDownOpen = false;
    //            }
    //        }));
    //}

    public override bool VerifyData()
    {
        // 设置初始状态不提示错误。
        if (ErrorStr is null)
        {
            return false;
        }

        // 修复 HandyControl 无法去除验证错误信息的问题。
        // 另一种解决方法是在样式中重写 ErrorTemplate，并用一个与背景色相同的 Border 覆盖 ErrorStr。
        bool hasError = base.VerifyData();

        if (!hasError)
        {
            // 此 ErrorStr 是 HandyControl 的属性，不影响 Validation.Errors 获取错误信息。
            ErrorStr = "";
        }

        return hasError;
    }

    protected override void OnSelectionChanged(SelectionChangedEventArgs e)
    {
        if (IsReadOnly)
        {
            SelectedIndex = _oldSelectedIndex;

            // Disallow changing the selection when read only.
            e.Handled = true;

            return;
        }

        _oldSelectedIndex = SelectedIndex;

        base.OnSelectionChanged(e);
    }
}
