using System.Windows.Controls;

using IPConfig.ViewModels;

using Microsoft.Extensions.DependencyInjection;

namespace IPConfig.Views;

/// <summary>
/// NicInfoCardView.xaml 的交互逻辑
/// </summary>
public partial class NicInfoCardView : UserControl
{
    public NicInfoCardView()
    {
        InitializeComponent();

        if (App.IsInDesignMode)
        {
            return;
        }

        DataContext = App.Current.Services.GetRequiredService<NicViewModel>();
    }
}
