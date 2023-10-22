using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Data;

using CodingNinja.Wpf.ObjectModel;

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

public partial class MainViewModel : ObservableRecipient,
    IRecipient<PropertyChangedMessage<EditableIPConfigModel?>>
{
    #region Fields

    private readonly Timer _realTimeNicSpeed = new(1000);

    private readonly object _syncLock = new();

    private GithubReleaseInfo? _githubReleaseInfo;

    private long _lastBytesReceived = 0;

    private long _lastBytesSent = 0;

    private EditableIPConfigModel? _lastSelectedIPConfig;

    private IPv4Config? _selectedNicIPv4Config;

    #endregion Fields

    #region ObservableProperties

    [ObservableProperty]
    private WpfObservableRangeCollection<Nic> _allNics = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(NewVersionAvailableToolTip))]
    private Exception? _checkUpdateError;

    [ObservableProperty]
    private SkinType? _currentSkinType;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(NewVersionAvailableToolTip))]
    private bool _hasNewVersion;

    [ObservableProperty]
    private bool _isSelectedNicIPConfigChecked;

    [ObservableProperty]
    private bool _isViewToNicConfigDetail;

    [ObservableProperty]
    private ObservableCollection<CultureInfo> _languages = null!;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanLoadLastUsedIPv4Config))]
    [NotifyCanExecuteChangedFor(nameof(LoadLastUsedIPv4ConfigCommand))]
    private LastUsedIPv4Config? _lastUsedIPv4Config;

    [ObservableProperty]
    private int _selectedIPConfigsCount;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    [NotifyPropertyChangedFor(nameof(IsSelectedNicNotNull))]
    [NotifyCanExecuteChangedFor(nameof(CopySelectedNicIPConfigAsTextCommand),
        nameof(MakeSelectedNicIPConfigCopyCommand),
        nameof(ViewToNicConfigDetailCommand),
        nameof(ReadLastUsedIPv4ConfigCommand))]
    private Nic? _selectedNic;

    [ObservableProperty]
    private string _selectedNicDownloadSpeed = "…B/s";

    [ObservableProperty]
    private EditableIPConfigModel? _selectedNicIPConfig;

    [ObservableProperty]
    private string _selectedNicUploadSpeed = "…B/s";

    [ObservableProperty]
    private bool _topmost = Settings.Default.Topmost;

    [ObservableProperty]
    private int _totalIPConfigsCount;

    #endregion ObservableProperties

    #region Properties

    public bool CanLoadLastUsedIPv4Config => LastUsedIPv4Config is not null;

    public string GetNicsToolTip { get; private set; } = Lang.AdapterNotFound;

    public bool IsSelectedNicNotNull => SelectedNic is not null;

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

        BindingOperations.EnableCollectionSynchronization(AllNics, _syncLock);

        LangSource.Instance.LanguageChanged += (s, e) => {
            // 更新 ToolTip 信息。
            OnPropertyChanged(nameof(SelectedNic));
            OnPropertyChanged(nameof(SelectedNicIPConfig));
            OnPropertyChanged(nameof(NewVersionAvailableToolTip));
        };
    }

    public void Receive(PropertyChangedMessage<EditableIPConfigModel?> message)
    {
        if (message.Sender is IPConfigListViewModel)
        {
            if (message.NewValue is not null)
            {
                _lastSelectedIPConfig = message.NewValue;
            }

            IsSelectedNicIPConfigChecked = message.NewValue is null;
        }
    }

    protected override void OnActivated()
    {
        base.OnActivated();

        Messenger.Register<MainViewModel, ValueChangedMessage<int>, string>(this, "SelectedIPConfigsCount",
            (r, m) => SelectedIPConfigsCount = m.Value);

        Messenger.Register<MainViewModel, ValueChangedMessage<int>, string>(this, "TotalIPConfigsCount",
            (r, m) => TotalIPConfigsCount = m.Value);

        Messenger.Register<MainViewModel, GoBackMessage>(this,
            (r, m) => GoBack());

        Messenger.Register<MainViewModel, RequestMessage<Nic?>>(this,
            (r, m) => m.Reply(SelectedNic));

        Messenger.Register<MainViewModel, RefreshMessage>(this,
            (r, m) => RefreshCommand.Execute(null));
    }

    #endregion Constructors & Recipients

    #region Relay Commands

    [RelayCommand]
    private static void ChangeLanguage(string name)
    {
        LangSource.Instance.SetLanguage(name);
    }

    [RelayCommand]
    private void AddUntitledIPConfig()
    {
        Messenger.Send<AddUntitledIPConfigMessage>(new(this));
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

    [RelayCommand(CanExecute = nameof(IsSelectedNicNotNull))]
    private async Task CopySelectedNicIPConfigAsTextAsync()
    {
        string text = $"{SelectedNic}\n\n{SelectedNicIPConfig}".Trim();
        await ClipboardHelper.SetTextAsync(text);
    }

    [RelayCommand]
    private void GetAllNics()
    {
        var nics = NetworkInterface.GetAllNetworkInterfaces()
            .Select(x => new Nic(x))
            .OrderBy(x => x.OperationalStatus is OperationalStatus.Up
                    && x.NetworkInterfaceType is not NetworkInterfaceType.Loopback or NetworkInterfaceType.Tunnel
                    ? (int)x.OperationalStatus
                    : Int32.MaxValue)
            .ThenBy(x => x.SimpleNicType)
            .ThenBy(x => x.Name);

        AllNics = new(nics);
        AllNics.CollectionChanged += (_, _) => GetNicsToolTip = AllNics.Count > 0 ? Lang.SelectAdapter_ToolTip : Lang.AdapterNotFound;
    }

    [RelayCommand]
    private void GoBack()
    {
        IsViewToNicConfigDetail = false;
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

        GetAllNics();
        SelectedNic = AllNics.FirstOrDefault();

        _realTimeNicSpeed.Elapsed += RealTimeNicSpeed_Elapsed;
        ResetNicSpeedDisplay();
        _realTimeNicSpeed.Start();

        await GetLatestReleaseInfoAsync();
    }

    [RelayCommand(CanExecute = nameof(CanLoadLastUsedIPv4Config))]
    private void LoadLastUsedIPv4Config()
    {
        LastUsedIPv4Config!.DeepCloneTo(SelectedNicIPConfig!.IPv4Config);
        SelectedNicIPConfigChecked();
        GoBack();
    }

    [RelayCommand(CanExecute = nameof(IsSelectedNicNotNull))]
    private void MakeSelectedNicIPConfigCopy()
    {
        Messenger.Send(SelectedNicIPConfig!, "MakeSelectedNicIPConfigCopy");
    }

    [RelayCommand(CanExecute = nameof(IsSelectedNicNotNull))]
    private async Task ReadLastUsedIPv4ConfigAsync()
    {
        var backup = await LastUsedIPv4Config.ReadAsync(SelectedNic!.Id);

        if (backup is null || backup.PropertyEquals(_selectedNicIPv4Config!))
        {
            LastUsedIPv4Config = null;
        }
        else
        {
            LastUsedIPv4Config = backup;
        }
    }

    [RelayCommand]
    private void Refresh()
    {
        string? id = SelectedNic?.Id;
        GetAllNics();
        SelectedNic = AllNics.FirstOrDefault(x => x.Id == id);
    }

    [RelayCommand]
    private void Save()
    {
        Messenger.Send<SaveMessage>(new(this));
    }

    [RelayCommand]
    private void SelectedNicIPConfigChecked()
    {
        Messenger.Send<ValueChangedMessage<bool>, string>(new(true), "SelectedNicIPConfigChecked");

        Broadcast(SelectedNicIPConfig, SelectedNicIPConfig, nameof(SelectedNicIPConfig));
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

    [RelayCommand]
    private void ViewNicIPConfig(bool enter = true)
    {
        if (enter)
        {
            IsSelectedNicIPConfigChecked = true;
        }
        else
        {
            if (IsViewToNicConfigDetail)
            {
                if (_lastSelectedIPConfig is null)
                {
                    IsViewToNicConfigDetail = false;
                }
                else
                {
                    Messenger.Send<ChangeSelectionMessage<EditableIPConfigModel>>(new(this, _lastSelectedIPConfig));
                }
            }
        }
    }

    [RelayCommand(CanExecute = nameof(IsSelectedNicNotNull))]
    private void ViewToNicConfigDetail()
    {
        _lastSelectedIPConfig = IsSelectedNicIPConfigChecked ? null : _lastSelectedIPConfig;

        IsSelectedNicIPConfigChecked = true;
        IsViewToNicConfigDetail = true;
    }

    #endregion Relay Commands

    #region Partial OnPropertyChanged Methods

    partial void OnSelectedNicChanged(Nic? value)
    {
        ResetNicSpeedDisplay();

        var nic = value;

        if (nic is null)
        {
            SelectedNicIPConfig = null;

            return;
        }

        var oldSelectedNicIPConfig = SelectedNicIPConfig;
        _selectedNicIPv4Config = NetworkManagement.GetIPv4Config(nic);

        SelectedNicIPConfig = new($"{nic.Name} - {nic.Description}") {
            IPv4Config = _selectedNicIPv4Config.DeepClone()
        };

        SelectedNicIPConfig.BeginEdit();

        IsSelectedNicIPConfigChecked = true;
        Broadcast(oldSelectedNicIPConfig, SelectedNicIPConfig, nameof(SelectedNicIPConfig));
    }

    partial void OnTopmostChanged(bool value)
    {
        Settings.Default.Topmost = value;
        Settings.Default.Save();
    }

    #endregion Partial OnPropertyChanged Methods

    #region Event Handlers

    private void RealTimeNicSpeed_Elapsed(object? sender, ElapsedEventArgs e)
    {
        var stats = SelectedNic?.GetIPStatistics();

        if (stats is null)
        {
            SelectedNicUploadSpeed = "N/A";
            SelectedNicDownloadSpeed = "N/A";

            return;
        }

        if (_lastBytesSent < 0)
        {
            SelectedNicUploadSpeed = "?/s";
        }
        else
        {
            long deltaSent = stats.BytesSent - _lastBytesSent;
            SelectedNicUploadSpeed = BytesFormatter.ToNetSpeed(deltaSent);
        }

        _lastBytesSent = stats.BytesSent;

        if (_lastBytesReceived < 0)
        {
            SelectedNicDownloadSpeed = "?/s";
        }
        else
        {
            long deltaRecv = stats.BytesReceived - _lastBytesReceived;
            SelectedNicDownloadSpeed = BytesFormatter.ToNetSpeed(deltaRecv);
        }

        _lastBytesReceived = stats.BytesReceived;
    }

    #endregion Event Handlers

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

    [GeneratedRegex("^[0-9A-Z]{8}-[0-9A-Z]{4}-[0-9A-Z]{4}-[0-9A-Z]{4}-[0-9A-Z]{12}.json$", RegexOptions.IgnoreCase)]
    private static partial Regex TempFileNameRegex();

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

    private void ResetNicSpeedDisplay()
    {
        _lastBytesSent = 0;
        _lastBytesReceived = 0;
        SelectedNicUploadSpeed = "…B/s";
        SelectedNicDownloadSpeed = "…B/s";

        var stats = SelectedNic?.GetIPStatistics();

        if (stats is null)
        {
            SelectedNicUploadSpeed = "N/A";
            SelectedNicDownloadSpeed = "N/A";

            return;
        }

        _lastBytesSent = stats.BytesSent;
        _lastBytesReceived = stats.BytesReceived;
    }

    #endregion Private Methods
}
