﻿using System;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

using CommunityToolkit.Mvvm.Messaging;

using IPConfig.Helpers;
using IPConfig.Models.Messages;
using IPConfig.ViewModels;

using Microsoft.Extensions.DependencyInjection;

namespace IPConfig.Views;

/// <summary>
/// IPConfigListView.xaml 的交互逻辑
/// </summary>
public partial class IPConfigListView : UserControl
{
    private ScrollViewer? _scrollViewer;

    private double _scrollViewerVOffset;

    public IPConfigListViewModel ViewModel => (IPConfigListViewModel)DataContext;

    public IPConfigListView()
    {
        InitializeComponent();

        if (App.IsInDesignMode)
        {
            return;
        }

        DataContext = App.Current.Services.GetRequiredService<IPConfigListViewModel>();

        WeakReferenceMessenger.Default.Register<IPConfigListView, KeyPressMessage>(this,
            (r, m) => {
                if (m.GestureEquals("Ctrl+F"))
                {
                    tbSearchBar.Focus();
                }
                else if (m.GestureEquals("Esc") && Keyboard.FocusedElement == tbSearchBar)
                {
                    string text = tbSearchBar.Text;

                    tbSearchBar.Clear();

                    if (String.IsNullOrEmpty(text))
                    {
                        lbIPConfigs.Focus();
                    }
                }
                else if (m.GestureEquals("F9"))
                {
                    lbIPConfigs.UpdateLayout();

                    if (lbIPConfigs.SelectedItem is null && lbIPConfigs.HasItems)
                    {
                        lbIPConfigs.SelectedItem = lbIPConfigs.Items[0];
                    }

                    var listBoxItem = lbIPConfigs.ItemContainerGenerator.ContainerFromItem(lbIPConfigs.SelectedItem) as ListBoxItem;
                    listBoxItem?.Focus();
                }
            });

        ThemeManager.ThemeChanging += (s, e) => _scrollViewerVOffset = _scrollViewer?.VerticalOffset ?? 0;
        ThemeManager.ThemeChanged += (s, e) => Dispatcher.Invoke(() => _scrollViewer?.ScrollToVerticalOffset(_scrollViewerVOffset));
    }

    private void LbIPConfigs_Loaded(object sender, System.Windows.RoutedEventArgs e)
    {
        var border = (Border)VisualTreeHelper.GetChild(lbIPConfigs, 0);
        _scrollViewer = (ScrollViewer)VisualTreeHelper.GetChild(border, 0);
    }

    private void LbIPConfigs_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // 修复多选时总是滚动到 SelectedItem 的问题。
        if (lbIPConfigs.SelectedItems.Count == 1)
        {
            lbIPConfigs.ScrollIntoView(lbIPConfigs.SelectedItem);
        }
    }
}
