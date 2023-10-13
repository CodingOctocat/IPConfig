using System;
using System.Windows;
using System.Windows.Controls;

namespace IPConfig.Controls;

/// <summary>
/// <see href="https://stackoverflow.com/a/26543731/4380178">Deferred loading of XAML</see>
/// </summary>
public class DeferredContent : ContentPresenter
{
    public static readonly DependencyProperty DeferredContentTemplateProperty =
        DependencyProperty.Register("DeferredContentTemplate",
        typeof(DataTemplate), typeof(DeferredContent), null);

    public DataTemplate DeferredContentTemplate
    {
        get => (DataTemplate)GetValue(DeferredContentTemplateProperty);
        set => SetValue(DeferredContentTemplateProperty, value);
    }

    public event EventHandler<RoutedEventArgs>? DeferredContentLoaded;

    public DeferredContent()
    {
        Loaded += HandleLoaded;
    }

    public void ShowDeferredContent()
    {
        if (DeferredContentTemplate is not null)
        {
            Content = DeferredContentTemplate.LoadContent();
            RaiseDeferredContentLoaded();
        }
    }

    private void HandleLoaded(object sender, RoutedEventArgs e)
    {
        Loaded -= HandleLoaded;
        Dispatcher.BeginInvoke(ShowDeferredContent);
    }

    private void RaiseDeferredContentLoaded()
    {
        DeferredContentLoaded?.Invoke(this, new RoutedEventArgs());
    }
}
