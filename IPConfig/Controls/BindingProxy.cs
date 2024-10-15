using System.Windows;

namespace IPConfig.Controls;

/// <summary>
/// <see cref="[WPF] How to bind to data when the DataContext is not inherited">https://thomaslevesque.com/2011/03/21/wpf-how-to-bind-to-data-when-the-datacontext-is-not-inherited/</see>
/// </summary>
public class BindingProxy : Freezable
{
    #region Overrides of Freezable

    protected override Freezable CreateInstanceCore()
    {
        return new BindingProxy();
    }

    #endregion Overrides of Freezable

    // Using a DependencyProperty as the backing store for Data.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty DataProperty =
        DependencyProperty.Register(
            "Data",
            typeof(object),
            typeof(BindingProxy),
            new UIPropertyMetadata(null));

    public object Data
    {
        get => GetValue(DataProperty);
        set => SetValue(DataProperty, value);
    }
}
