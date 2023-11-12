using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows;

using HandyControl.Controls;
using HandyControl.Data;

using IPConfig.Helpers;
using IPConfig.Languages;
using IPConfig.Models;
using IPConfig.Properties;
using IPConfig.ViewModels;
using IPConfig.Views;

using LiteDB;

using Microsoft.Extensions.DependencyInjection;

using Window = System.Windows.Window;

namespace IPConfig;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    public const string AppName = "IPConfig";

    public static readonly JsonSerializerOptions JsonOptions = new() {
        WriteIndented = true,
        IgnoreReadOnlyProperties = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        Converters = { new JsonStringEnumConverter() }
    };

    private static bool _isDbSyncing;

    public static bool CanForceClose { get; set; }

    /// <summary>
    /// Gets the current <see cref="App"/> instance in use.
    /// </summary>
    public static new App Current => (App)Application.Current;

    public static bool IsDbSyncing
    {
        get => _isDbSyncing;
        set
        {
            if (_isDbSyncing != value)
            {
                _isDbSyncing = value;
                OnStaticPropertyChanged();
            }
        }
    }

    public static bool IsInDesignMode => Application.Current is not App;

    public static Version? Version => Assembly.GetEntryAssembly()?.GetName().Version;

    public static string VersionString => $"v{Version?.Major}.{Version?.Minor}.{Version?.Build}";

    /// <summary>
    /// Gets the <see cref="IServiceProvider"/> instance to resolve application services.
    /// </summary>
    public IServiceProvider Services { get; }

    public App()
    {
        Services = ConfigureServices();

        InitializeComponent();

        BsonMapper.Global.EmptyStringToNull = false;

        BsonMapper.Global.Entity<EditableIPConfigModel>()
            .Ignore(x => x.HasErrors);

        BsonMapper.Global.Entity<IPv4Config>()
            .Ignore(x => x.HasErrors);

        ThemeWatcher.WindowsThemeChanged += ThemeWatcher_WindowsThemeChanged;
        ThemeWatcher.StartThemeWatching();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        ThemeWatcher.WindowsThemeChanged -= ThemeWatcher_WindowsThemeChanged;

        base.OnExit(e);
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // 全局异常处理。
        SetupExceptionHandling();

        FrameworkElement.StyleProperty.OverrideMetadata(typeof(Window), new FrameworkPropertyMetadata {
            DefaultValue = Current.FindResource(typeof(Window))
        });

        if (Settings.Default.UpgradeRequired)
        {
            Settings.Default.Upgrade();
            Settings.Default.UpgradeRequired = false;
            Settings.Default.Save();
        }

        var mainWindow = Services.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }

    /// <summary>
    /// Configures the services for the application.
    /// </summary>
    private static IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        services.AddSingleton<MainViewModel>();
        services.AddSingleton<NicViewModel>();
        services.AddSingleton<IPConfigListViewModel>();
        services.AddSingleton<IPConfigDetailViewModel>();
        services.AddTransient<NicConfigDetailViewModel>();
        services.AddSingleton<ThemeSwitchButtonViewModel>();
        services.AddSingleton<VersionInfoViewModel>();
        services.AddSingleton<StatusBarViewModel>();

        services.AddSingleton<MainWindow>();
        services.AddSingleton<NicSelectorView>();
        services.AddSingleton<NicInfoCardView>();
        services.AddSingleton<IPConfigListView>();
        services.AddTransient<IPConfigDetailView>();
        services.AddTransient<IPv4ConfigView>();
        services.AddTransient<NicConfigDetailView>();
        services.AddSingleton<StatusBarView>();
        services.AddSingleton<IPConfigListSelectionCounterView>();
        services.AddSingleton<NicSpeedMonitorView>();
        services.AddSingleton<ThemeSwitchButtonView>();
        services.AddSingleton<VersionInfoView>();

        return services.BuildServiceProvider();
    }

    private static async Task ShowUnhandledExceptionAsync(Exception exception, string source)
    {
        try
        {
            var err = new Error(exception, source);

            await File.WriteAllTextAsync("error.log", err.ToString());
        }
        catch
        { }

        var sb = new StringBuilder($"{Lang.AppCrashed}\n\n[{source}]\n{exception.Message}");

        var innerEx = exception.InnerException;

        while (innerEx is not null)
        {
            sb.AppendLine(new string('-', 16));
            sb.AppendLine(innerEx.Message);
            innerEx = innerEx.InnerException;
        }

        string msg = sb.ToString();

        Growl.Ask(new GrowlInfo {
            Message = msg,
            ShowDateTime = true,
            ConfirmStr = Lang.RestartApp,
            CancelStr = Lang.Ignore,
            IsCustom = true,
            IconKey = ResourceToken.FatalGeometry,
            IconBrushKey = ResourceToken.DangerBrush,
            ActionBeforeClose = isConfirm => {
                if (isConfirm)
                {
                    var module = Process.GetCurrentProcess().MainModule;

                    if (module is null)
                    {
                        Growl.Error(Lang.RestartFailed);
                    }
                    else
                    {
                        Process.Start(module.FileName);
                        CanForceClose = true;
                        Current?.Shutdown();
                    }
                }

                return true;
            }
        });
    }

    private void SetupExceptionHandling()
    {
        AppDomain.CurrentDomain.UnhandledException += async (s, e) =>
           await ShowUnhandledExceptionAsync((Exception)e.ExceptionObject, nameof(AppDomain.CurrentDomain.UnhandledException));

        DispatcherUnhandledException += async (s, e) => {
            await ShowUnhandledExceptionAsync(e.Exception, nameof(DispatcherUnhandledException));
            e.Handled = true;
        };

        TaskScheduler.UnobservedTaskException += async (s, e) => {
            await ShowUnhandledExceptionAsync(e.Exception, nameof(TaskScheduler.UnobservedTaskException));
            e.SetObserved();
        };
    }

    private void ThemeWatcher_WindowsThemeChanged(object? sender, ThemeWatcher.ThemeChangedArgs e)
    {
        if (ThemeManager.CurrentSkinTypeMode is null)
        {
            ThemeManager.UpdateSkin(null);
        }
    }

    #region Static Properties Change Notification

    public static event EventHandler<PropertyChangedEventArgs> StaticPropertyChanged = delegate { };

    private static void OnStaticPropertyChanged([CallerMemberName] string? staticPropertyName = null)
    {
        StaticPropertyChanged(null, new PropertyChangedEventArgs(staticPropertyName));
    }

    #endregion Static Properties Change Notification
}
