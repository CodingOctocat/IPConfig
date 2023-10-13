using System;
using System.IO;
using System.Windows;
using System.Windows.Input;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;

using IPConfig.Languages;
using IPConfig.Models.Messages;
using IPConfig.ViewModels;

using Microsoft.Extensions.DependencyInjection;

using HcWindow = HandyControl.Controls.Window;

namespace IPConfig.Views;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
[INotifyPropertyChanged]
public partial class MainWindow : HcWindow
{
    public static Version? Version => App.Version;

    public static string WindowTitle1 => $": TCP/IPv4 {Lang.ConfigTool}";

    public static string WindowTitle2 => $"{WindowTitle1} by CodingNinja";

    public string DisplayTitle { get; private set; } = WindowTitle1;

    public string ReleasedLongTime => File.GetLastWriteTime(GetType().Assembly.Location).ToString(LangSource.Instance.CurrentCulture);

    public MainViewModel ViewModel => (MainViewModel)DataContext;

    public string WindowTitle3 => $"{WindowTitle2} / Released on {ReleasedLongTime}";

    public MainWindow()
    {
        InitializeComponent();

        if (App.IsInDesignMode)
        {
            return;
        }

        DataContext = App.Current.Services.GetRequiredService<MainViewModel>();

        LangSource.Instance.LanguageChanged += (s, e) => UpdateTitle();
    }

    private void UpdateTitle()
    {
        DisplayTitle = ActualWidth switch {
            < 640 => WindowTitle1,
            < 960 => WindowTitle2,
            _ => WindowTitle3
        };

        txtTitle.Text = DisplayTitle;
    }

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        WeakReferenceMessenger.Default.Send<KeyPressMessage>(new(this, e));
    }

    private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        UpdateTitle();
    }
}
