using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

using HandyControl.Data;

using IPConfig.Extensions;

namespace IPConfig.Helpers;

public static class ThemeManager
{
    private static SkinType _actualSkinType;

    private static SkinType? _currentSkinType;

    public static SkinType ActualSkinType
    {
        get => _actualSkinType;
        private set
        {
            _actualSkinType = value;
            OnStaticPropertyChanged();
        }
    }

    /// <summary>
    /// <para><see cref="SkinType.Default"/>: Light</para>
    /// <para><see cref="SkinType.Dark"/>: Dark</para>
    /// <para><see cref="SkinType.Violet"/>: Violet</para>
    /// <para><see langword="null"/>: Use System Setting</para>
    /// </summary>
    public static SkinType? CurrentSkinType
    {
        get => _currentSkinType;
        private set
        {
            _currentSkinType = value;
            OnStaticPropertyChanged();
        }
    }

    public static event EventHandler<SkinType?>? ThemeChanged;

    public static event EventHandler<SkinType?>? ThemeChanging;

    public static void RaiseCurrentSkinTypeChanged()
    {
        OnStaticPropertyChanged(nameof(CurrentSkinType));
    }

    public static void UpdateSkin(SkinType? skin)
    {
        if (skin == CurrentSkinType)
        {
            return;
        }

        ThemeChanging?.Invoke(null, skin);

        SkinType actualSkinType;

        if (skin is null)
        {
            actualSkinType = ThemeWatcher.GetCurrentWindowsTheme().ToSkinType();
        }
        else
        {
            actualSkinType = skin.Value;
        }

        App.Current.Resources.MergedDictionaries.Clear();

        App.Current.Resources.MergedDictionaries.Add(new ResourceDictionary {
            Source = new($"pack://application:,,,/HandyControl;component/Themes/Skin{actualSkinType}.xaml")
        });

        App.Current.Resources.MergedDictionaries.Add(new ResourceDictionary {
            Source = new("pack://application:,,,/HandyControl;component/Themes/Theme.xaml")
        });

        App.Current.Resources.MergedDictionaries.Add(new ResourceDictionary {
            Source = new("/Themes/MyResources.xaml", UriKind.Relative)
        });

        App.Current.Resources.MergedDictionaries.Add(new ResourceDictionary {
            Source = new("/Themes/MyStyles.xaml", UriKind.Relative)
        });

        // 优化显示效果。
        var theme = actualSkinType switch {
            SkinType.Default => new ResourceDictionary {
                Source = new("/Themes/MyLightTheme.xaml", UriKind.Relative)
            },
            SkinType.Dark => new ResourceDictionary {
                Source = new("/Themes/MyDarkTheme.xaml", UriKind.Relative)
            },
            SkinType.Violet => new ResourceDictionary {
                Source = new("/Themes/MyVioletTheme.xaml", UriKind.Relative)
            },
            _ => throw new NotImplementedException()
        };

        App.Current.Resources.MergedDictionaries.Add(theme);
        CurrentSkinType = skin;
        ActualSkinType = actualSkinType;

        ThemeChanged?.Invoke(null, skin);
    }

    #region Static Properties Change Notification

    public static event EventHandler<PropertyChangedEventArgs> StaticPropertyChanged = delegate { };

    private static void OnStaticPropertyChanged([CallerMemberName] string? staticPropertyName = null)
    {
        StaticPropertyChanged(null, new PropertyChangedEventArgs(staticPropertyName));
    }

    #endregion Static Properties Change Notification
}
