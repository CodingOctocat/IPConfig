using System;
using System.Globalization;
using System.Management;
using System.Security.Principal;

using Microsoft.Win32;

namespace IPConfig.Helpers;

/// <summary>
/// <see href="https://engy.us/blog/2018/10/20/dark-theme-in-wpf/">Dark Theme in WPF</see>
/// </summary>
public static class ThemeWatcher
{
    public class ThemeChangedArgs
    {
        public WindowsTheme WindowsTheme { set; get; }

        public ThemeChangedArgs(WindowsTheme windowsTheme)
        {
            WindowsTheme = windowsTheme;
        }
    }

    public enum WindowsTheme
    {
        Default = 0,

        Light = 1,

        Dark = 2,
    }

    private const string _registryKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";

    private const string _registryValueName = "AppsUseLightTheme";

    private static WindowsTheme _currentWindowsTheme;

    public static event EventHandler<ThemeChangedArgs>? WindowsThemeChanged;

    public static WindowsTheme GetCurrentWindowsTheme()
    {
        var theme = WindowsTheme.Light;

        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(_registryKeyPath);
            object? registryValueObject = key?.GetValue(_registryValueName);

            if (registryValueObject is null)
            {
                return WindowsTheme.Light;
            }

            int registryValue = (int)registryValueObject;
            theme = registryValue > 0 ? WindowsTheme.Light : WindowsTheme.Dark;

            return theme;
        }
        catch (Exception)
        {
            return theme;
        }
    }

    public static void StartThemeWatching()
    {
        var currentUser = WindowsIdentity.GetCurrent();

        string query = String.Format(
            CultureInfo.InvariantCulture,
            @"SELECT * FROM RegistryValueChangeEvent WHERE Hive = 'HKEY_USERS' AND KeyPath = '{0}\\{1}' AND ValueName = '{2}'",
            currentUser?.User?.Value,
            _registryKeyPath.Replace(@"\", @"\\"),
            _registryValueName);

        try
        {
            _currentWindowsTheme = GetCurrentWindowsTheme();
            var watcher = new ManagementEventWatcher(query);
            watcher.EventArrived += Watcher_EventArrived;

            // Start listening for events
            watcher.Start();
        }
        catch (Exception)
        {
            // This can fail on Windows 7
            _currentWindowsTheme = WindowsTheme.Default;
        }
    }

    private static void Watcher_EventArrived(object sender, EventArrivedEventArgs e)
    {
        var newWindowsTheme = GetCurrentWindowsTheme();

        if (newWindowsTheme != _currentWindowsTheme)
        {
            _currentWindowsTheme = newWindowsTheme;
            WindowsThemeChanged?.Invoke(sender, new(_currentWindowsTheme));
        }
    }
}
