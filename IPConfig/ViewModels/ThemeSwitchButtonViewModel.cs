using System;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using HandyControl.Data;

using IPConfig.Helpers;
using IPConfig.Properties;

namespace IPConfig.ViewModels;

public partial class ThemeSwitchButtonViewModel : ObservableObject
{
    #region Relay Commands

    [RelayCommand]
    private static void ChangeTheme(SkinType? skin)
    {
        ThemeManager.UpdateSkin(skin);

        Settings.Default.Theme = ThemeManager.CurrentSkinTypeMode?.ToString();
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
