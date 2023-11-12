using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using HandyControl.Controls;

using IPConfig.Extensions;
using IPConfig.Helpers;
using IPConfig.Languages;
using IPConfig.Models.GitHub;

namespace IPConfig.ViewModels;

public partial class VersionInfoViewModel : ObservableObject
{
    #region Fields

    private GitHubReleaseInfo? _githubReleaseInfo;

    #endregion Fields

    #region Observable Properties

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(NewVersionAvailableToolTip))]
    private Exception? _checkUpdateError;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(NewVersionAvailableToolTip))]
    private bool _hasNewVersion;

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

    public VersionInfoViewModel()
    {
        // 更新 ToolTip 信息。
        LangSource.Instance.LanguageChanged += (s, e) => OnPropertyChanged(nameof(NewVersionAvailableToolTip));
    }

    #endregion Constructors & Recipients

    #region Relay Commands

    [RelayCommand]
    private async Task LoadedAsync()
    {
        await GetLatestReleaseInfoAsync();
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
            ConfirmStr = Lang.JumpToGitHub,
            CancelStr = Lang.Cancel,
            Message = $"""
                {Lang.LatestVersion}{_githubReleaseInfo.Name}
                {App.VersionString} -> {_githubReleaseInfo.TagName}
                {_githubReleaseInfo.CreatedAt.ToLocalTime():yyyy/MM/dd HH:mm:ss 'GMT'z}

                What's Changed
                {note}
                """,
            ActionBeforeClose = (isConfirm) => {
                if (isConfirm)
                {
                    UriHelper.OpenUri(GitHubApi.ReleasesUrl);
                }

                return true;
            }
        });
    }

    #endregion Relay Commands

    #region Private Methods

    [GeneratedRegex("^\\-\\s", RegexOptions.Multiline)]
    private static partial Regex ReleaseNoteFormatRegex();

    private async Task GetLatestReleaseInfoAsync()
    {
        try
        {
            _githubReleaseInfo = await GitHubApi.GetLatestReleaseInfoAsync();

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
