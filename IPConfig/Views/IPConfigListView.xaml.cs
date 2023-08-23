using System;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

using CommunityToolkit.Mvvm.Messaging;

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

        WeakReferenceMessenger.Default.Register<KeyPressMessage>(this,
            (r, m) => {
                if (m.Gesture == "Ctrl+F")
                {
                    tbSearchBar.Focus();
                }
                else if (m.Gesture == "Esc" && Keyboard.FocusedElement == tbSearchBar)
                {
                    string text = tbSearchBar.Text;

                    tbSearchBar.Clear();

                    if (String.IsNullOrEmpty(text))
                    {
                        lbIPConfigs.Focus();
                    }
                }
                else if (m.Gesture == "F9")
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

        App.Current.ThemeChanging += (s, e) => _scrollViewerVOffset = _scrollViewer?.VerticalOffset ?? 0;
        App.Current.ThemeChanged += (s, e) => _scrollViewer?.ScrollToVerticalOffset(_scrollViewerVOffset);
    }

    private void LbIPConfigs_Loaded(object sender, System.Windows.RoutedEventArgs e)
    {
        var border = (Border)VisualTreeHelper.GetChild(lbIPConfigs, 0);
        _scrollViewer = (ScrollViewer)VisualTreeHelper.GetChild(border, 0);
    }

    private void LbIPConfigs_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // 防止多选时总是滚动到 SelectedItem。
        if (lbIPConfigs.SelectedItems.Count == 1)
        {
            lbIPConfigs.ScrollIntoView(lbIPConfigs.SelectedItem);
            lbIPConfigs.Focus();
            lbIPConfigs.UpdateLayout();
        }
    }
}
