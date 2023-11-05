using System;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Data;

using CodingNinja.Wpf.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;

using IPConfig.Helpers;
using IPConfig.Languages;
using IPConfig.Models;
using IPConfig.Models.Messages;

namespace IPConfig.ViewModels;

public partial class NicViewModel : ObservableRecipient,
    IRecipient<PropertyChangedMessage<EditableIPConfigModel?>>
{
    #region Fields

    private readonly Timer _realTimeNicSpeedMonitor = new(1000);

    private readonly object _syncLock = new();

    private bool _clicked = true;

    private long _lastBytesReceived = 0;

    private long _lastBytesSent = 0;

    private Nic? _lastNic;

    private EditableIPConfigModel? _lastSelectedIPConfig;

    private IPv4Config? _selectedNicIPv4Config;

    #endregion Fields

    #region Observable Properties

    [ObservableProperty]
    private WpfObservableRangeCollection<Nic> _allNics = new();

    [ObservableProperty]
    private bool _isInNicConfigDetailView;

    [ObservableProperty]
    private bool _isSelectedNicIPConfigChecked;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanLoadLastUsedIPv4Config))]
    [NotifyCanExecuteChangedFor(nameof(LoadLastUsedIPv4ConfigCommand))]
    private LastUsedIPv4Config? _lastUsedIPv4Config;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    [NotifyPropertyChangedFor(nameof(IsSelectedNicNotNull))]
    [NotifyCanExecuteChangedFor(nameof(CopySelectedNicIPConfigAsTextCommand),
        nameof(MakeSelectedNicIPConfigCopyCommand),
        nameof(ViewToNicConfigDetailCommand))]
    private Nic? _selectedNic;

    [ObservableProperty]
    private string _selectedNicDownloadSpeed = "…B/s";

    [ObservableProperty]
    private EditableIPConfigModel? _selectedNicIPConfig;

    [ObservableProperty]
    private string _selectedNicUploadSpeed = "…B/s";

    #endregion Observable Properties

    #region Properties

    public bool CanLoadLastUsedIPv4Config => LastUsedIPv4Config is not null;

    public bool IsSelectedNicNotNull => SelectedNic is not null;

    public string NicSelectorPlaceholder { get; private set; } = Lang.AdapterNotFound;

    #endregion Properties

    #region Constructors & Recipients

    public NicViewModel()
    {
        IsActive = true;

        BindingOperations.EnableCollectionSynchronization(AllNics, _syncLock);

        LangSource.Instance.LanguageChanged += (s, e) => {
            // 更新 ToolTip 信息。
            OnPropertyChanged(nameof(SelectedNic));
            OnPropertyChanged(nameof(SelectedNicIPConfig));
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

        Messenger.Register<NicViewModel, RefreshMessage>(this,
            (r, m) => RefreshCommand.Execute(null));

        Messenger.Register<NicViewModel, GoBackMessage>(this,
            (r, m) => GoBack());

        Messenger.Register<NicViewModel, ToggleStateMessage<bool>, string>(this, "ToggleNicInfoCard",
            (r, m) => IsSelectedNicIPConfigChecked = m.NewValue);

        Messenger.Register<NicViewModel, RequestMessage<Nic>, string>(this, "DeferredLoadingForNicConfigDetailView",
            (r, m) => m.Reply(SelectedNic!));
    }

    #endregion Constructors & Recipients

    #region Relay Commands

    [RelayCommand(CanExecute = nameof(IsSelectedNicNotNull))]
    private async Task CopySelectedNicIPConfigAsTextAsync()
    {
        string text = $"""
            {SelectedNic}

            {SelectedNicIPConfig}
            """;

        await ClipboardHelper.SetTextAsync(text.Trim());
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
        AllNics.CollectionChanged += (_, _) => NicSelectorPlaceholder = AllNics.Count > 0 ? Lang.SelectAdapter_ToolTip : Lang.AdapterNotFound;
    }

    [RelayCommand]
    private void Loaded()
    {
        GetAllNics();
        SelectedNic = AllNics.FirstOrDefault();

        _realTimeNicSpeedMonitor.Elapsed += RealTimeNicSpeedMonitor_Elapsed;
        ResetNicSpeedDisplay();
        _realTimeNicSpeedMonitor.Start();
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

    [RelayCommand]
    private void RaiseNicViewCardClick()
    {
        if (_clicked)
        {
            _lastSelectedIPConfig = null;
        }

        _clicked = true;
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
        GetAllNics();
        SelectedNic = AllNics.FirstOrDefault(x => x.Id == _lastNic?.Id);
    }

    [RelayCommand]
    private void SelectedNicIPConfigChecked()
    {
        Messenger.Send<EmptyMessage, string>(new(this), "SelectedNicIPConfigChecked");

        Broadcast(SelectedNicIPConfig, SelectedNicIPConfig, nameof(SelectedNicIPConfig));
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
            if (IsInNicConfigDetailView)
            {
                IsInNicConfigDetailView = false;

                if (_lastSelectedIPConfig is not null)
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
        IsInNicConfigDetailView = true;
    }

    [RelayCommand(CanExecute = nameof(IsSelectedNicNotNull))]
    private void ViewToNicConfigDetailByDoubleClick(RoutedEventArgs e)
    {
        e.Handled = true;
        IsSelectedNicIPConfigChecked = true;
        IsInNicConfigDetailView = true;
    }

    #endregion Relay Commands

    #region Partial OnPropertyChanged Methods

    partial void OnIsInNicConfigDetailViewChanged(bool value)
    {
        Messenger.Send<ValueChangedMessage<bool>, string>(new(value), "IsInNicConfigDetailView");
    }

    partial void OnIsSelectedNicIPConfigCheckedChanged(bool value)
    {
        if (!value)
        {
            _clicked = false;
        }
    }

    partial void OnSelectedNicChanged(Nic? oldValue, Nic? newValue)
    {
        _lastNic = oldValue;

        if (oldValue is not null && newValue is null)
        {
            NicSelectorPlaceholder = $"[{Lang.Disabled}] {oldValue.Name} - {oldValue.Description}";
        }
        else
        {
            NicSelectorPlaceholder = Lang.AdapterNotFound;
        }

        OnPropertyChanged(nameof(NicSelectorPlaceholder));
    }

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

    #endregion Partial OnPropertyChanged Methods

    #region Event Handlers

    private void RealTimeNicSpeedMonitor_Elapsed(object? sender, ElapsedEventArgs e)
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

    private void GoBack()
    {
        ViewNicIPConfig(false);
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
