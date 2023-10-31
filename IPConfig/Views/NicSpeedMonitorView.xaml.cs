using System.Windows.Controls;

using IPConfig.ViewModels;

using Microsoft.Extensions.DependencyInjection;

namespace IPConfig.Views;

/// <summary>
/// NicSpeedMonitorView.xaml 的交互逻辑
/// </summary>
public partial class NicSpeedMonitorView : UserControl
{
    public NicSpeedMonitorView()
    {
        InitializeComponent();

        if (App.IsInDesignMode)
        {
            return;
        }

        DataContext = App.Current.Services.GetRequiredService<NicViewModel>();
    }
}
