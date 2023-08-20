using System.Collections;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace IPConfig.Behaviors;

/// <summary>
/// 不要对绑定的 SelectedItems 属性重新赋值，此行为将对列表内部数据进行增删操作。
/// <para>
/// <see href="https://www.codeproject.com/Tips/1207965/SelectedItems-Behavior-for-ListBox-and-MultiSelect">SelectedItems-Behavior-for-ListBox-and-MultiSelect</see>
/// </para>
/// </summary>
public sealed class SelectedItemsBehavior
{
    public static readonly DependencyProperty SelectedItemsProperty =
        DependencyProperty.RegisterAttached("SelectedItems",
            typeof(INotifyCollectionChanged),
            typeof(SelectedItemsBehavior),
            new PropertyMetadata(default(IList), OnSelectedItemsChanged));

    private static readonly DependencyProperty _isBusyProperty =
        DependencyProperty.RegisterAttached("IsBusy",
            typeof(bool),
            typeof(SelectedItemsBehavior),
            new PropertyMetadata(default(bool)));

    public static IList GetSelectedItems(DependencyObject element)
    {
        return (IList)element.GetValue(SelectedItemsProperty);
    }

    public static void SetSelectedItems(DependencyObject d, INotifyCollectionChanged value)
    {
        d.SetValue(SelectedItemsProperty, value);
    }

    private static bool GetIsBusy(DependencyObject element)
    {
        return (bool)element.GetValue(_isBusyProperty);
    }

    private static void OnSelectedItemsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        IList? selectedItems = null;

        void CollectionChangedEventHandler(object? sender, NotifyCollectionChangedEventArgs args)
        {
            if (args.OldItems is not null)
            {
                foreach (object? item in args.OldItems)
                {
                    if (selectedItems.Contains(item))
                    {
                        selectedItems.Remove(item);
                    }
                }
            }

            if (args.NewItems is not null)
            {
                foreach (object? item in args.NewItems)
                {
                    if (!selectedItems.Contains(item))
                    {
                        selectedItems.Add(item);
                    }
                }
            }
        }

        if (d is ListView listView)
        {
            selectedItems = listView.SelectedItems;
            listView.SelectionChanged += OnSelectionChanged;
        }
        else if (d is ListBox listBox)
        {
            selectedItems = listBox.SelectedItems;
            listBox.SelectionChanged += OnSelectionChanged;
        }
        else if (d is MultiSelector multiSelector)
        {
            selectedItems = multiSelector.SelectedItems;
            multiSelector.SelectionChanged += OnSelectionChanged;
        }

        if (selectedItems is null)
        {
            return;
        }

        if (e.OldValue is INotifyCollectionChanged oldValue)
        {
            oldValue.CollectionChanged -= CollectionChangedEventHandler;
        }

        if (e.NewValue is INotifyCollectionChanged newValue)
        {
            newValue.CollectionChanged += CollectionChangedEventHandler;
        }
    }

    private static void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is DependencyObject d)
        {
            if (!GetIsBusy(d))
            {
                SetIsBusy(d, true);
                var list = GetSelectedItems(d);

                foreach (object? item in e.RemovedItems)
                {
                    if (list.Contains(item))
                    {
                        list.Remove(item);
                    }
                }

                foreach (object? item in e.AddedItems)
                {
                    if (!list.Contains(item))
                    {
                        list.Add(item);
                    }
                }

                SetIsBusy(d, false);
            }
        }
    }

    private static void SetIsBusy(DependencyObject element, bool value)
    {
        element.SetValue(_isBusyProperty, value);
    }
}
