using System.Windows.Controls;

using IPConfig.ViewModels;

using Microsoft.Extensions.DependencyInjection;

namespace IPConfig.Views;

/// <summary>
/// VersionInfoView.xaml 的交互逻辑
/// </summary>
public partial class VersionInfoView : UserControl
{
    public VersionInfoView()
    {
        InitializeComponent();

        if (App.IsInDesignMode)
        {
            return;
        }

        DataContext = App.Current.Services.GetRequiredService<VersionInfoViewModel>();
    }
}
