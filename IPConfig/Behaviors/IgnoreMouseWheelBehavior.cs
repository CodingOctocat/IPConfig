using System.Windows;
using System.Windows.Input;

using Microsoft.Xaml.Behaviors;

namespace IPConfig.Behaviors;

/// <summary>
/// 忽略 <seealso cref="UIElement"/> 的鼠标滚轮行为，兼容附加属性模式。
/// <para>
/// <see href="https://stackoverflow.com/a/15904265/4380178">Bubbling scroll events from a ListView to its parent</see>
/// </para>
/// </summary>
public sealed class IgnoreMouseWheelBehavior : Behavior<UIElement>
{
    // Using a DependencyProperty as the backing store for Enabled.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty EnabledProperty =
        DependencyProperty.RegisterAttached(
            "Enabled",
            typeof(bool),
            typeof(IgnoreMouseWheelBehavior),
            new PropertyMetadata(false, OnEnabledChanged));

    public static bool GetEnabled(DependencyObject obj)
    {
        return (bool)obj.GetValue(EnabledProperty);
    }

    public static void SetEnabled(DependencyObject obj, bool value)
    {
        obj.SetValue(EnabledProperty, value);
    }

    protected override void OnAttached()
    {
        base.OnAttached();

        AssociatedObject.PreviewMouseWheel += AssociatedObject_PreviewMouseWheel;
    }

    protected override void OnDetaching()
    {
        AssociatedObject.PreviewMouseWheel -= AssociatedObject_PreviewMouseWheel;

        base.OnDetaching();
    }

    private static void OnEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is UIElement uie)
        {
            if ((bool)e.NewValue)
            {
                uie.PreviewMouseWheel += OnPreviewMouseWheel;
            }
            else
            {
                uie.PreviewMouseWheel -= OnPreviewMouseWheel;
            }
        }
    }

    private static void OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        e.Handled = true;

        var e2 = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta) {
            RoutedEvent = UIElement.MouseWheelEvent
        };

        ((UIElement)sender).RaiseEvent(e2);
    }

    private void AssociatedObject_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        e.Handled = true;

        var e2 = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta) {
            RoutedEvent = UIElement.MouseWheelEvent
        };

        AssociatedObject.RaiseEvent(e2);
    }
}
