using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace IPConfig.Behaviors;

/// <summary>
/// <see href="https://stackoverflow.com/a/42297391/4380178">Why does WPF textbox not support triple-click to select all text</see>
/// </summary>
public class TripleClickToSelectAllBehavior
{
    public static readonly DependencyProperty EnabledProperty = DependencyProperty.RegisterAttached(
          "Enabled", typeof(bool), typeof(TripleClickToSelectAllBehavior), new PropertyMetadata(false, OnPropertyChanged));

    public static bool GetEnabled(DependencyObject element)
    {
        return (bool)element.GetValue(EnabledProperty);
    }

    public static void SetEnabled(DependencyObject element, bool value)
    {
        element.SetValue(EnabledProperty, value);
    }

    private static void OnPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TextBox tb)
        {
            bool enabled = (bool)e.NewValue;

            if (enabled)
            {
                tb.PreviewMouseLeftButtonDown += OnTextBoxMouseDown;
            }
            else
            {
                tb.PreviewMouseLeftButtonDown -= OnTextBoxMouseDown;
            }
        }
    }

    private static void OnTextBoxMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is TextBox textBox && e.ClickCount == 3)
        {
            textBox.SelectAll();
            textBox.ScrollToHome();
        }
    }
}
