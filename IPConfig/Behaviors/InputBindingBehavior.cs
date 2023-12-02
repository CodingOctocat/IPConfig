using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace IPConfig.Behaviors;

/// <summary>
/// <see href="https://stackoverflow.com/a/55903060/4380178">InputBindings work only when focused</see>
/// <para>
/// 此行为将在运行时引发 XAML 绑定错误，但不影响工作。
/// System.Windows.Data Error: Cannot find governing FrameworkElement or FrameworkContentElement for target element.
/// </para>
/// </summary>
public class InputBindingBehavior
{
    public static readonly DependencyProperty PropagateInputBindingsToWindowProperty =
        DependencyProperty.RegisterAttached(
            "PropagateInputBindingsToWindow",
            typeof(bool),
            typeof(InputBindingBehavior),
            new PropertyMetadata(false, OnPropagateInputBindingsToWindowChanged));

    private static readonly Dictionary<int, Tuple<WeakReference<FrameworkElement>, List<InputBinding>>> _trackedFrameWorkElementsToBindings = [];

    public static bool GetPropagateInputBindingsToWindow(FrameworkElement obj)
    {
        return (bool)obj.GetValue(PropagateInputBindingsToWindowProperty);
    }

    public static void SetPropagateInputBindingsToWindow(FrameworkElement obj, bool value)
    {
        obj.SetValue(PropagateInputBindingsToWindowProperty, value);
    }

    private static void CleanupBindingsDictionary(Window window, Dictionary<int, Tuple<WeakReference<FrameworkElement>, List<InputBinding>>> bindingsDictionary)
    {
        foreach (int hashCode in bindingsDictionary.Keys.ToList())
        {
            if (bindingsDictionary.TryGetValue(hashCode, out var trackedData) &&
                !trackedData.Item1.TryGetTarget(out _))
            {
                Debug.WriteLine($"InputBindingBehavior: FrameWorkElement {hashCode} did never unload but was GCed, cleaning up leftover KeyBindings.");

                foreach (var binding in trackedData.Item2)
                {
                    window.InputBindings.Remove(binding);
                }

                trackedData.Item2.Clear();
                bindingsDictionary.Remove(hashCode);
            }
        }
    }

    private static void OnFrameworkElementLoaded(object sender, RoutedEventArgs e)
    {
        var frameworkElement = (FrameworkElement)sender;

        var window = Window.GetWindow(frameworkElement);

        if (window is not null)
        {
            // Transfer InputBindings into our control.
            if (!_trackedFrameWorkElementsToBindings.TryGetValue(frameworkElement.GetHashCode(), out var trackingData))
            {
                trackingData = Tuple.Create(
                    new WeakReference<FrameworkElement>(frameworkElement),
                    frameworkElement.InputBindings.Cast<InputBinding>().ToList());

                _trackedFrameWorkElementsToBindings.Add(
                    frameworkElement.GetHashCode(), trackingData);
            }

            // Apply Bindings to Window.
            foreach (var inputBinding in trackingData.Item2)
            {
                window.InputBindings.Add(inputBinding);
            }

            frameworkElement.InputBindings.Clear();
        }
    }

    private static void OnFrameworkElementUnLoaded(object sender, RoutedEventArgs e)
    {
        var frameworkElement = (FrameworkElement)sender;
        var window = Window.GetWindow(frameworkElement);
        int hashCode = frameworkElement.GetHashCode();

        // Remove Bindings from Window.
        if (window is not null)
        {
            if (_trackedFrameWorkElementsToBindings.TryGetValue(hashCode, out var trackedData))
            {
                foreach (var binding in trackedData.Item2)
                {
                    frameworkElement.InputBindings.Add(binding);
                    window.InputBindings.Remove(binding);
                }

                trackedData.Item2.Clear();
                _trackedFrameWorkElementsToBindings.Remove(hashCode);

                // Catch removed and orphaned entries.
                CleanupBindingsDictionary(window, _trackedFrameWorkElementsToBindings);
            }
        }
    }

    private static void OnPropagateInputBindingsToWindowChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        ((FrameworkElement)d).Loaded += OnFrameworkElementLoaded;
        ((FrameworkElement)d).Unloaded += OnFrameworkElementUnLoaded;
    }
}
