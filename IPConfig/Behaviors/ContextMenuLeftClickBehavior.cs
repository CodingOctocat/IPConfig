using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;

namespace IPConfig.Behaviors;

/// <summary>
/// <see href="https://stackoverflow.com/a/29123964/4380178">Show ContextMenu on Left Click using only XAML</see>
/// </summary>
public static class ContextMenuLeftClickBehavior
{
    public static readonly DependencyProperty IsLeftClickEnabledProperty = DependencyProperty.RegisterAttached(
        "IsLeftClickEnabled", typeof(bool), typeof(ContextMenuLeftClickBehavior),
        new UIPropertyMetadata(false, OnIsLeftClickEnabledChanged));

    public static bool GetIsLeftClickEnabled(DependencyObject obj)
    {
        return (bool)obj.GetValue(IsLeftClickEnabledProperty);
    }

    public static void SetIsLeftClickEnabled(DependencyObject obj, bool value)
    {
        obj.SetValue(IsLeftClickEnabledProperty, value);
    }

    private static void OnIsLeftClickEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is UIElement uiElement)
        {
            bool isEnabled = e.NewValue is bool b && b;

            if (isEnabled)
            {
                if (uiElement is ButtonBase btn)
                {
                    btn.Click += OnMouseLeftButtonUp;
                }
                else
                {
                    uiElement.MouseLeftButtonUp += OnMouseLeftButtonUp;
                }
            }
            else
            {
                if (uiElement is ButtonBase btn)
                {
                    btn.Click -= OnMouseLeftButtonUp;
                }
                else
                {
                    uiElement.MouseLeftButtonUp -= OnMouseLeftButtonUp;
                }
            }
        }
    }

    private static void OnMouseLeftButtonUp(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe)
        {
            // 如果我们在上下文菜单中使用绑定, 则当我们在左键单击时显示菜单时, 它的 “数据上下文” 将不会被设置。
            // （当用户右键单击控件时, 似乎在 WPF 中设置了 “数据上下文”, 尽管我不确定）
            // 所以我们必须在这里手动设置 ContextMenu 的数据上下文。
            if (fe.ContextMenu.DataContext is null)
            {
                fe.ContextMenu.SetBinding(FrameworkElement.DataContextProperty, new Binding { Source = fe.DataContext });
            }

            ContextMenuService.SetPlacementTarget(fe.ContextMenu, fe);

            ContextMenuService.SetPlacement(fe.ContextMenu, ContextMenuService.GetPlacement(fe));
            ContextMenuService.SetPlacementRectangle(fe.ContextMenu, ContextMenuService.GetPlacementRectangle(fe));
            ContextMenuService.SetHorizontalOffset(fe.ContextMenu, ContextMenuService.GetHorizontalOffset(fe));
            ContextMenuService.SetVerticalOffset(fe.ContextMenu, ContextMenuService.GetVerticalOffset(fe));

            fe.ContextMenu.IsOpen = true;

            // 设置切换形式打开上下文菜单。
            // 建议在 XAML 设置 ContextMenuService.IsEnabled="False" 以禁用右键打开功能，避免产生打开状态的切换冲突。
            fe.ContextMenu.Closed += (s, e) => fe.IsEnabled = true;
            fe.IsEnabled = false;
        }
    }
}
