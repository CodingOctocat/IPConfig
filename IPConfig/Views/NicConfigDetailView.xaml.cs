using System.Windows;
using System.Windows.Controls;

using CommunityToolkit.Mvvm.ComponentModel;

using IPConfig.ViewModels;

using Microsoft.Extensions.DependencyInjection;

namespace IPConfig.Views;

/// <summary>
/// NicConfigDetailView.xaml 的交互逻辑
/// </summary>
[INotifyPropertyChanged]
public partial class NicConfigDetailView : UserControl
{
    private double _scrollViewerVOffset;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TitleWidthMargin))]
    private double _titleWidth = 120;

    public Thickness TitleWidthMargin => new(TitleWidth, 0, 0, 0);

    public NicConfigDetailViewModel ViewModel => (NicConfigDetailViewModel)DataContext;

    public NicConfigDetailView()
    {
        InitializeComponent();

        if (App.IsInDesignMode)
        {
            return;
        }

        DataContext = App.Current.Services.GetService<NicConfigDetailViewModel>();

        App.Current.ThemeChanging += (s, e) => _scrollViewerVOffset = scrollViewer.VerticalOffset;
        App.Current.ThemeChanged += (s, e) => scrollViewer.ScrollToVerticalOffset(_scrollViewerVOffset);
    }

    private void UserControl_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        ViewModel.CanUpdate = (bool)e.NewValue;
    }
}
