using System;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using HandyControl.Data;

using IPConfig.Helpers;
using IPConfig.Languages;
using IPConfig.Properties;

namespace IPConfig.ViewModels;

public partial class ThemeSwitchButtonViewModel : ObservableObject
{
    #region Constructors & Recipients

    public ThemeSwitchButtonViewModel()
    {
        // 更新 ToolTip 信息。
        LangSource.Instance.LanguageChanged += (s, e) => ThemeManager.RaiseCurrentSkinTypeChanged();
    }

    #endregion Constructors & Recipients

    #region Relay Commands

    [RelayCommand]
    private static void ChangeTheme(SkinType? skin)
    {
        ThemeManager.UpdateSkin(skin);

        Settings.Default.Theme = ThemeManager.CurrentSkinType?.ToString();
        Settings.Default.Save();
    }

    [RelayCommand]
    private static void Loaded()
    {
        SkinType? skinType = null;

        if (Enum.TryParse(Settings.Default.Theme, out SkinType skin))
        {
            skinType = skin;
        }

        ChangeTheme(skinType);
    }

    #endregion Relay Commands
}
