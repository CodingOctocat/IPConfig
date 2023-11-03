using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;

using HandyControl.Controls;
using HandyControl.Data;

using IPConfig.Extensions;
using IPConfig.Helpers;
using IPConfig.Languages;
using IPConfig.Models;
using IPConfig.Models.Messages;
using IPConfig.Properties;

using HcMessageBox = HandyControl.Controls.MessageBox;

namespace IPConfig.ViewModels;

public partial class MainViewModel : ObservableRecipient
{
    #region Fields

    private GithubReleaseInfo? _githubReleaseInfo;

    #endregion Fields

    #region Observable Properties

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(NewVersionAvailableToolTip))]
    private Exception? _checkUpdateError;

    [ObservableProperty]
    private SkinType? _currentSkinType;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(NewVersionAvailableToolTip))]
    private bool _hasNewVersion;

    [ObservableProperty]
    private bool _isInNicConfigDetailView;

    [ObservableProperty]
    private ObservableCollection<CultureInfo> _languages = null!;

    [ObservableProperty]
    private bool _topmost = Settings.Default.Topmost;

    #endregion Observable Properties

    #region Properties

    public string NewVersionAvailableToolTip
    {
        get
        {
            if (CheckUpdateError is not null)
            {
                return $"{Lang.CheckUpdateFailed}\n\n{CheckUpdateError.Message}";
            }

            if (HasNewVersion)
            {
                return Lang.NewVersionAvailable_Format_ToolTip.Format(_githubReleaseInfo?.Name ?? "<?.?.?>",
                    App.VersionString, _githubReleaseInfo?.TagName ?? "<?.?.?>");
            }
            else
            {
                return Lang.YouAreUpToDate;
            }
        }
    }

    #endregion Properties

    #region Constructors & Recipients

    public MainViewModel()
    {
        IsActive = true;

        // 更新 ToolTip 信息。
        LangSource.Instance.LanguageChanged += (s, e) => OnPropertyChanged(nameof(NewVersionAvailableToolTip));
    }

    protected override void OnActivated()
    {
        base.OnActivated();

        Messenger.Register<MainViewModel, ValueChangedMessage<bool>, string>(this, "IsInNicConfigDetailView",
            (r, m) => IsInNicConfigDetailView = m.Value);
    }

    #endregion Constructors & Recipients

    #region Relay Commands

    [RelayCommand]
    private static void ChangeLanguage(string name)
    {
        LangSource.Instance.SetLanguage(name);
    }

    [RelayCommand]
    private void ChangeTheme(SkinType? skin)
    {
        if (CurrentSkinType == skin)
        {
            return;
        }

        CurrentSkinType = skin;

        if (skin is null)
        {
            App.Current.UpdateSkin(ThemeWatcher.GetCurrentWindowsTheme().ToSkinType());
        }
        else
        {
            App.Current.UpdateSkin(skin.Value);
        }

        Settings.Default.Theme = CurrentSkinType?.ToString();
        Settings.Default.Save();
    }

    [RelayCommand]
    private void Closing(CancelEventArgs e)
    {
        if (App.IsDbSyncing)
        {
            HcMessageBox.Show(
                Lang.ClosingInfoDbSyncing,
                App.AppName,
                MessageBoxButton.OK,
                MessageBoxImage.Information,
                MessageBoxResult.OK);

            e.Cancel = true;

            return;
        }

        var modifieldIPConfigs = Messenger.Send<RequestMessage<IEnumerable<EditableIPConfigModel>>, string>("ModifieldIPConfigs").Response.ToImmutableArray();

        foreach (var item in modifieldIPConfigs)
        {
            Messenger.Send<ChangeSelectionMessage<EditableIPConfigModel>>(new(this, item));

            var result = HcMessageBox.Show(
                Lang.ClosingSaveAsk_Format.Format(item.Name),
                App.AppName,
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Question,
                MessageBoxResult.Yes);

            if (result == MessageBoxResult.Yes)
            {
                item.ValidateAllProperties();

                if (item.HasErrors)
                {
                    Growl.Error(Lang.SaveFailedValidationError);
                    e.Cancel = true;

                    return;
                }

                Messenger.Send<SaveMessage>(new(this));
            }
            else if (result == MessageBoxResult.No)
            {
                if (item.Order < 0)
                {
                    Messenger.Send<CollectionChangeActionMessage<EditableIPConfigModel>>(new(this, CollectionChangeAction.Remove, item));
                }
                else
                {
                    item.RejectChanges();
                }

                continue;
            }
            else
            {
                e.Cancel = true;

                return;
            }
        }

        var iPConfigList = Messenger.Send<RequestMessage<IEnumerable<EditableIPConfigModel>>, string>("IPConfigList").Response.ToImmutableArray();

        LiteDbHelper.Handle(col => {
            for (int i = 0; i < iPConfigList.Length; i++)
            {
                var item = iPConfigList[i];
                item.Order = i;
                col.Update(item);
            }
        });
    }

    [RelayCommand]
    private async Task LoadedAsync()
    {
        var cultures = LangSource.GetAvailableCultures().OrderBy(x => x.Name);
        Languages = new(cultures);

        if (Settings.Default.UpgradeRequired)
        {
            Settings.Default.Upgrade();
            Settings.Default.UpgradeRequired = false;
            Settings.Default.Save();
        }

        SkinType? skinType;

        if (Enum.TryParse(Settings.Default.Theme, out SkinType skin))
        {
            skinType = skin;
        }
        else
        {
            skinType = null;
        }

        ChangeTheme(skinType);

        await GetLatestReleaseInfoAsync();
    }

    [RelayCommand]
    private void Save()
    {
        Messenger.Send<SaveMessage>(new(this));
    }

    [RelayCommand]
    private async Task ShowUpdateGrowlAsync()
    {
        if (_githubReleaseInfo is null)
        {
            await GetLatestReleaseInfoAsync();
        }

        if (CheckUpdateError is not null)
        {
            string innerExMsg = CheckUpdateError.InnerException == null ? "" : $"\n\n{new string('-', 24)}\n{CheckUpdateError.InnerException.Message}";

            Growl.Error($"{Lang.CheckUpdateFailed}\n\n{CheckUpdateError.Message}{innerExMsg}");

            return;
        }

        if (!HasNewVersion)
        {
            Growl.Info(new() {
                Message = Lang.YouAreUpToDate,
                WaitTime = 2
            });

            return;
        }

        string note = ReleaseNoteFormatRegex().Replace(_githubReleaseInfo!.ReleaseNote, "• ");

        if (String.IsNullOrWhiteSpace(note))
        {
            note = "<null>";
        }

        Growl.Ask(new() {
            ConfirmStr = Lang.JumpToGithub,
            CancelStr = Lang.Cancel,
            Message = $"""
                {Lang.LatestVersion}{_githubReleaseInfo.Name}
                {App.VersionString} -> {_githubReleaseInfo.TagName}
                {_githubReleaseInfo.CreatedAt}

                What's Changed
                {note}
                """,
            ActionBeforeClose = (isConfirm) => {
                if (isConfirm)
                {
                    OpenUri(_githubReleaseInfo.HtmlUrl ?? App.GithubRepositoryUrl);
                }

                return true;
            }
        });
    }

    #endregion Relay Commands

    #region Partial OnPropertyChanged Methods

    partial void OnTopmostChanged(bool value)
    {
        Settings.Default.Topmost = value;
        Settings.Default.Save();
    }

    #endregion Partial OnPropertyChanged Methods

    #region Private Methods

    private static void OpenUri(string uri)
    {
        // HACK: 如果不转换为 AbsoluteUri，那么链接中的 “%20” 将以空格形式传入参数。
        string absoluteUri = new Uri(uri, UriKind.RelativeOrAbsolute).AbsoluteUri;

        // HACK: 如果不替换 “&”，那么 “&” 后面的内容将被截断。
        absoluteUri = absoluteUri.Replace("&", "^&");

        var psi = new ProcessStartInfo {
            FileName = "cmd",
            WindowStyle = ProcessWindowStyle.Hidden,
            UseShellExecute = true,
            CreateNoWindow = true,
            Arguments = $"/c start {absoluteUri}"
        };

        Process.Start(psi);
    }

    [GeneratedRegex("^\\-\\s", RegexOptions.Multiline)]
    private static partial Regex ReleaseNoteFormatRegex();

    private async Task GetLatestReleaseInfoAsync()
    {
        try
        {
            _githubReleaseInfo = await GithubReleaseInfo.GetLatestReleaseInfoAsync();

            if (Version.Parse(_githubReleaseInfo.TagName.TrimStart('v')) > App.Version)
            {
                HasNewVersion = true;
            }
        }
        catch (Exception ex)
        {
            _githubReleaseInfo = null;
            HasNewVersion = false;
            CheckUpdateError = ex;
        }
    }

    #endregion Private Methods
}
