using System.Windows.Controls;

using IPConfig.Languages;
using IPConfig.ViewModels;

using Microsoft.Extensions.DependencyInjection;

namespace IPConfig.Views;

/// <summary>
/// ThemeSwitchButtonView.xaml 的交互逻辑
/// </summary>
public partial class ThemeSwitchButtonView : UserControl
{
    public ThemeSwitchButtonView()
    {
        InitializeComponent();

        if (App.IsInDesignMode)
        {
            return;
        }

        DataContext = App.Current.Services.GetRequiredService<ThemeSwitchButtonViewModel>();

        LangSource.Instance.LanguageChanged += (s, e) => btnThemeSwitcher.GetBindingExpression(ToolTipProperty).UpdateTarget();
    }
}
