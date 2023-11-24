using System.Windows.Controls;

using CommunityToolkit.Mvvm.Messaging;

using IPConfig.Models.Messages;
using IPConfig.ViewModels;

using Microsoft.Extensions.DependencyInjection;

namespace IPConfig.Views;

/// <summary>
/// IPConfigDetailView.xaml 的交互逻辑
/// </summary>
public partial class IPConfigDetailView : UserControl
{
    public IPConfigDetailViewModel ViewModel => (IPConfigDetailViewModel)DataContext;

    public IPConfigDetailView()
    {
        InitializeComponent();

        if (App.IsInDesignMode)
        {
            return;
        }

        DataContext = App.Current.Services.GetRequiredService<IPConfigDetailViewModel>();

        WeakReferenceMessenger.Default.Register<IPConfigDetailView, KeyPressMessage>(this,
            (r, m) => {
                if (m.GestureEquals("F2"))
                {
                    tbIPConfigName.Focus();
                    tbIPConfigName.SelectAll();
                    tbIPConfigName.ScrollToHome();
                }
            });
    }
}
