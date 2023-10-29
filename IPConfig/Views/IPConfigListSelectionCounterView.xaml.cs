using System.Windows.Controls;

using IPConfig.ViewModels;

using Microsoft.Extensions.DependencyInjection;

namespace IPConfig.Views;

/// <summary>
/// IPConfigListSelectionCounterView.xaml 的交互逻辑
/// </summary>
public partial class IPConfigListSelectionCounterView : UserControl
{
    public IPConfigListSelectionCounterView()
    {
        InitializeComponent();

        if (App.IsInDesignMode)
        {
            return;
        }

        DataContext = App.Current.Services.GetRequiredService<IPConfigListViewModel>();
    }
}
