using System;
using System.Windows.Controls;
using System.Windows.Threading;

using IPConfig.ViewModels;

using Microsoft.Extensions.DependencyInjection;

namespace IPConfig.Views;

/// <summary>
/// IPv4ConfigView.xaml 的交互逻辑
/// </summary>
public partial class IPv4ConfigView : UserControl
{
    private readonly DispatcherTimer _updateTargetTimer = new();

    private MenuItem? _pingDnsGroupMenuItem;

    public IPConfigDetailViewModel ViewModel => (IPConfigDetailViewModel)DataContext;

    public IPv4ConfigView()
    {
        InitializeComponent();

        if (App.IsInDesignMode)
        {
            return;
        }

        DataContext = App.Current.Services.GetService<IPConfigDetailViewModel>();

        _updateTargetTimer.Interval = TimeSpan.FromMicroseconds(100);
        _updateTargetTimer.Tick += DispatcherTimer_Tick;
    }

    private void DispatcherTimer_Tick(object? sender, EventArgs e)
    {
        _pingDnsGroupMenuItem?.GetBindingExpression(MenuItem.IsEnabledProperty).UpdateTarget();
    }

    private void TxtPindDnsGroup_ContextMenuClosing(object sender, ContextMenuEventArgs e)
    {
        _updateTargetTimer.Stop();
    }

    private void TxtPingDnsGroup_ContextMenuOpening(object sender, ContextMenuEventArgs e)
    {
        // 如果 DNS 分组中存在尚未完成的 ping 动作，应该禁用上下文菜单项。
        // <MenuItem IsEnabled="{Binding Converter={StaticResource PingDnsGroupIsEnabledConverter}}" .../>
        // 因为 CollectionViewGroup 不是 BindingList，所以每次打开菜单时都需要强制更新数据源以便重新计算菜单项的可用性。
        var txt = (TextBlock)sender;
        _pingDnsGroupMenuItem = txt.ContextMenu.Items[0] as MenuItem;

        _updateTargetTimer.Start();
    }
}
