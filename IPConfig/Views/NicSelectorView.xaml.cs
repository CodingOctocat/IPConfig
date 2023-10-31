using System.Windows.Controls;

using IPConfig.ViewModels;

using Microsoft.Extensions.DependencyInjection;

namespace IPConfig.Views;

/// <summary>
/// NicSelectorView.xaml 的交互逻辑
/// </summary>
public partial class NicSelectorView : UserControl
{
    public NicSelectorView()
    {
        InitializeComponent();

        if (App.IsInDesignMode)
        {
            return;
        }

        DataContext = App.Current.Services.GetRequiredService<NicViewModel>();
    }
}
