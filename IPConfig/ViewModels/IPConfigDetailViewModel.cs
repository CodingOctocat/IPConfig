using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;

using CodingNinja.Wpf.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;

using HandyControl.Controls;

using IPConfig.Extensions;
using IPConfig.Helpers;
using IPConfig.Languages;
using IPConfig.Models;
using IPConfig.Models.Messages;

using LiteDB;

using HcMessageBox = HandyControl.Controls.MessageBox;

namespace IPConfig.ViewModels;

public partial class IPConfigDetailViewModel : ObservableRecipient, IEditableObject, IRevertibleChangeTracking,
    IRecipient<PropertyChangedMessage<Nic?>>,
    IRecipient<PropertyChangedMessage<EditableIPConfigModel?>>
{
    #region Fields

    private readonly WpfObservableRangeCollection<IPv4Dns> _iPv4DnsList = [];

    private readonly WpfObservableRangeCollection<IPv4Mask> _iPv4MaskList = [];

    private EditableIPConfigModel _backup = EditableIPConfigModel.Empty;

    private bool _inTxn;

    private bool _isSaveSuccessful;

    private bool _loaded;

    private Nic? _nic;

    #endregion Fields

    #region Observable Properties

    [ObservableProperty]
    private IPConfigModel _autoComplete = IPConfigModel.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanShowTitleChangedIndicator),
        nameof(CanShowDhcpEnabledChangedIndicator),
        nameof(CanShowIPChangedIndicator),
        nameof(CanShowMaskChangedIndicator),
        nameof(CanShowGatewayChangedIndicator),
        nameof(CanShowAutoDnsChangedIndicator),
        nameof(CanShowDns1ChangedIndicator),
        nameof(CanShowDns2ChangedIndicator),
        nameof(CanShowRemarkChangedIndicator))]
    private bool _canShowChangedIndicator;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanShowTitleChangedIndicator),
        nameof(CanShowDhcpEnabledChangedIndicator),
        nameof(CanShowIPChangedIndicator),
        nameof(CanShowMaskChangedIndicator),
        nameof(CanShowGatewayChangedIndicator),
        nameof(CanShowAutoDnsChangedIndicator),
        nameof(CanShowDns1ChangedIndicator),
        nameof(CanShowDns2ChangedIndicator),
        nameof(CanShowRemarkChangedIndicator))]
    private bool _canShowUnchangedIndicator;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEditingIPConfigModifield))]
    [NotifyCanExecuteChangedFor(nameof(DiscardChangesCommand))]
    private EditableIPConfigModel _editingIPConfig = EditableIPConfigModel.Empty;

    [ObservableProperty]
    private bool _isInContrastView;

    #endregion Observable Properties

    #region Properties

    public bool CanExecuteLoaded => !_loaded;

    public bool? CanShowAutoDnsChangedIndicator => CanShowIPConfigPropertyChangedIndicator(x => x.IPv4Config.IsAutoDns);

    public bool? CanShowDhcpEnabledChangedIndicator => CanShowIPConfigPropertyChangedIndicator(x => x.IPv4Config.IsDhcpEnabled);

    public bool? CanShowDns1ChangedIndicator => CanShowIPConfigPropertyChangedIndicator(x => x.IPv4Config.Dns1);

    public bool? CanShowDns2ChangedIndicator => CanShowIPConfigPropertyChangedIndicator(x => x.IPv4Config.Dns2);

    public bool? CanShowGatewayChangedIndicator => CanShowIPConfigPropertyChangedIndicator(x => x.IPv4Config.Gateway);

    public bool? CanShowIPChangedIndicator => CanShowIPConfigPropertyChangedIndicator(x => x.IPv4Config.IP);

    public bool? CanShowMaskChangedIndicator => CanShowIPConfigPropertyChangedIndicator(x => x.IPv4Config.Mask);

    public bool? CanShowRemarkChangedIndicator => CanShowIPConfigPropertyChangedIndicator(x => x.Remark);

    public bool? CanShowTitleChangedIndicator => CanShowIPConfigPropertyChangedIndicator(x => x.Name);

    public ICollectionView? IPv4DnsListCollectionView { get; }

    public CollectionViewSource? IPv4DnsListCvs { get; }

    public ICollectionView? IPv4MaskListCollectionView { get; }

    public CollectionViewSource? IPv4MaskListCvs { get; }

    public bool IsEditingIPConfigModifield => IsInContrastView || EditingIPConfig.IsChanged;

    public bool IsNicIPConfig { get; private set; }

    public ICommand PrimarySaveOrApplyCommand
    {
        get
        {
            if (IsNicIPConfig)
            {
                return ApplyCommand;
            }

            if (EditingIPConfig.IsChanged)
            {
                return SaveCommand;
            }
            else
            {
                return ApplyCommand;
            }
        }
    }

    public string PrimarySaveOrApplyString
    {
        get
        {
            if (IsNicIPConfig)
            {
                return $"{Lang.Apply_}";
            }

            if (EditingIPConfig.IsChanged)
            {
                return $"{Lang.Save_}";
            }
            else
            {
                return $"{Lang.Apply_}";
            }
        }
    }

    public ICommand SecondarySaveOrApplyCommand
    {
        get
        {
            if (IsNicIPConfig)
            {
                return SaveCommand;
            }

            if (EditingIPConfig.IsChanged)
            {
                return ApplyCommand;
            }
            else
            {
                return SaveCommand;
            }
        }
    }

    public string SecondarySaveOrApplyString
    {
        get
        {
            if (IsNicIPConfig)
            {
                return $"{Lang.Save_}";
            }

            if (EditingIPConfig.IsChanged)
            {
                return $"{Lang.Apply_}";
            }
            else
            {
                return $"{Lang.Save_}";
            }
        }
    }

    #endregion Properties

    #region Constructors & Recipients

    public IPConfigDetailViewModel()
    {
        IsActive = true;

        IPv4MaskListCvs = new();
        IPv4MaskListCvs.GroupDescriptions.Add(new PropertyGroupDescription("Group"));
        IPv4MaskListCvs.Source = _iPv4MaskList;
        IPv4MaskListCollectionView = IPv4MaskListCvs.View;

        IPv4DnsListCvs = new();
        IPv4DnsListCvs.GroupDescriptions.Add(new PropertyGroupDescription("Group"));
        IPv4DnsListCvs.GroupDescriptions.Add(new PropertyGroupDescription("Provider"));
        IPv4DnsListCvs.GroupDescriptions.Add(new PropertyGroupDescription("Filter"));
        IPv4DnsListCvs.Source = _iPv4DnsList;
        IPv4DnsListCollectionView = IPv4DnsListCvs.View;

        LangSource.Instance.LanguageChanged += (s, e) => {
            // 更新数据验证错误信息。
            EditingIPConfig.ClearErrors();
            EditingIPConfig.ValidateAllProperties();

            OnPropertyChanged(nameof(PrimarySaveOrApplyString));
            OnPropertyChanged(nameof(SecondarySaveOrApplyString));

            // 更新 Mask 选项列表。
            string lastMask = EditingIPConfig.IPv4Config.Mask;

            var iPv4MaskList = IPv4Mask.ReadIPv4MaskList();
            _iPv4MaskList.ReplaceRange(iPv4MaskList);

            EditingIPConfig.IPv4Config.Mask = lastMask;

            // 更新 DNS 选项列表。
            string lastDns1 = EditingIPConfig.IPv4Config.Dns1;
            string lastDns2 = EditingIPConfig.IPv4Config.Dns2;

            var iPv4DnsList = ReadIPv4DnsList();
            _iPv4DnsList.ReplaceRange(iPv4DnsList);

            EditingIPConfig.IPv4Config.Dns1 = lastDns1;
            EditingIPConfig.IPv4Config.Dns2 = lastDns2;
        };
    }

    /// <summary>
    /// 接收 <seealso cref="NicViewModel.SelectedNicIPConfig"/> 或 <seealso cref="IPConfigListViewModel.SelectedIPConfig"/>。
    /// </summary>
    /// <param name="message"></param>
    public void Receive(PropertyChangedMessage<EditableIPConfigModel?> message)
    {
        var newIPConfig = message.NewValue;
        var oldIPConfig = message.OldValue;

        if (oldIPConfig is not null)
        {
            oldIPConfig.EditableChanged -= EditingIPConfig_EditableChanged;
            oldIPConfig.PropertyChanged -= EditingIPConfig_PropertyChanged;
            oldIPConfig.IPv4Config.AllowAutoDisableDhcp(false);
            oldIPConfig.IPv4Config.AllowAutoDisableAutoDns(false);
        }

        IsNicIPConfig = message.Sender is NicViewModel;
        OnPropertyChanged(nameof(IsNicIPConfig));

        if (newIPConfig is null)
        {
            EditingIPConfig = EditableIPConfigModel.Empty;
        }
        else
        {
            EditingIPConfig = newIPConfig;
        }

        EditingIPConfig.BeginEdit();
        EditingIPConfig.IPv4Config.AllowAutoDisableDhcp();
        EditingIPConfig.IPv4Config.AllowAutoDisableDhcp();
        EditingIPConfig.EditableChanged += EditingIPConfig_EditableChanged;
        EditingIPConfig.PropertyChanged += EditingIPConfig_PropertyChanged;

        // 初始化页面状态。
        UpdateSaveAndApplyButton();
        CanShowChangedIndicator = false;
        CanShowUnchangedIndicator = false;

        AutoComplete = EditingIPConfig.Backup;
        GetAutoCompleteName();
        GetAutoCompleteGateway();
        GetAutoCompleteDns1();
        GetAutoCompleteDns2();
    }

    public void Receive(PropertyChangedMessage<Nic?> message)
    {
        _nic = message.NewValue;
    }

    protected override void OnActivated()
    {
        base.OnActivated();

        Messenger.Register<IPConfigDetailViewModel, SaveMessage>(this,
            (r, m) => SaveCommand.Execute(null));

        Messenger.Register<IPConfigDetailViewModel, CancelEditMessage>(this,
            (r, m) => DiscardChangesCommand.Execute(m.Ask));
    }

    #endregion Constructors & Recipients

    #region Relay Commands

    [RelayCommand]
    private static async Task CopyDnsAsync(string dns)
    {
        await ClipboardHelper.SetTextAsync(dns);
    }

    [RelayCommand(AllowConcurrentExecutions = true)]
    private static async Task PingDnsAsync(IPv4Dns dns)
    {
        await dns.PingAsync();
    }

    [RelayCommand(AllowConcurrentExecutions = true)]
    private static async Task PingDnsGroupAsync(CollectionViewGroup group)
    {
        var dnsItems = GroupItemHelper.GetGroupItems<IPv4Dns>(group);

        await Task.WhenAll(dnsItems.Select(x => x.PingAsync()));
    }

    [RelayCommand]
    private async Task ApplyAsync()
    {
        if (_nic is null)
        {
            Growl.Error(Lang.ApplyFailed);

            return;
        }

        FormatInput();
        EditingIPConfig.ValidateAllProperties();

        string msg = Lang.ApplyAsk_Format.Format(_nic.Name, _nic.Description);

        var ipv4Config = EditingIPConfig.IPv4Config;

        if (ipv4Config.HasErrors)
        {
            msg = $"{msg}\n\n{Lang.ValidationErrorWarning}";
        }

        var result = HcMessageBox.Show(
            msg,
            App.AppName,
            MessageBoxButton.OKCancel,
            ipv4Config.HasErrors ? MessageBoxImage.Warning : MessageBoxImage.Question,
            MessageBoxResult.OK);

        if (result != MessageBoxResult.OK)
        {
            return;
        }

        await BackupAsync();

        if (ipv4Config.IsDhcpEnabled)
        {
            NetworkManagement.SetIPv4Dhcp(_nic.Id);
        }
        else
        {
            NetworkManagement.SetIPv4(_nic.Id, ipv4Config.IP, ipv4Config.Mask, ipv4Config.Gateway);
        }

        if (ipv4Config.IsAutoDns)
        {
            NetworkManagement.SetIPv4DnsAuto(_nic.Id);
        }
        else
        {
            NetworkManagement.SetIPv4Dns(_nic.Id, ipv4Config.Dns1, ipv4Config.Dns2);
        }

        Messenger.Send<RefreshMessage>(new(this));

        Growl.Success(new() {
            Message = Lang.ApplySuccessful,
            WaitTime = 2
        });
    }

    [RelayCommand(CanExecute = nameof(IsEditingIPConfigModifield))]
    private void DiscardChanges(bool ask = true)
    {
        if (ask)
        {
            var result = HcMessageBox.Show(
                Lang.DiscardAsk,
                App.AppName,
                MessageBoxButton.OKCancel,
                MessageBoxImage.Question,
                MessageBoxResult.OK);

            if (result == MessageBoxResult.OK)
            {
                EditingIPConfig.RejectChanges();
                RejectChanges();
                AutoComplete = EditingIPConfig.Backup;
            }
        }
        else
        {
            EditingIPConfig.RejectChanges();
            RejectChanges();
            AutoComplete = EditingIPConfig.Backup;
        }

        EditingIPConfig.ClearErrors();
    }

    [RelayCommand(CanExecute = nameof(CanExecuteLoaded))]
    private void Loaded()
    {
        _loaded = true;

        var iPv4MaskList = IPv4Mask.ReadIPv4MaskList();
        _iPv4MaskList.AddRange(iPv4MaskList);

        var iPv4DnsList = ReadIPv4DnsList();
        _iPv4DnsList.AddRange(iPv4DnsList);
    }

    [RelayCommand(CanExecute = nameof(IsEditingIPConfigModifield))]
    private void RaiseContrastViewButtonMouseDown()
    {
        IsInContrastView = true;
        CanShowChangedIndicator = false;
        CanShowUnchangedIndicator = true;

        BeginEdit();
        EditingIPConfig.RejectChanges();
    }

    [RelayCommand]
    private void RaiseContrastViewButtonMouseEnter()
    {
        CanShowChangedIndicator = true;
    }

    [RelayCommand]
    private void RaiseContrastViewButtonMouseLeave()
    {
        CanShowChangedIndicator = false;
    }

    [RelayCommand(CanExecute = nameof(IsEditingIPConfigModifield))]
    private void RaiseContrastViewButtonMouseUp()
    {
        IsInContrastView = false;

        RejectChanges();

        CanShowUnchangedIndicator = false;
        CanShowChangedIndicator = true;
    }

    [RelayCommand]
    private void Save()
    {
        FormatInput();

        try
        {
            EditingIPConfig.ValidateAllProperties();

            if (EditingIPConfig.HasErrors)
            {
                Growl.Error(Lang.SaveFailedValidationError);

                _isSaveSuccessful = false;

                return;
            }

            LiteDbHelper.Handle(col => {
                if (EditingIPConfig.IsPropertyChanged(x => x.Name, out string? oldValue))
                {
                    col.Delete(new BsonValue(oldValue));
                }

                col.Upsert(EditingIPConfig);
            });

            if (IsNicIPConfig)
            {
                Messenger.Send(EditingIPConfig, "DuplicateSelectedNicIPConfig");
            }

            Growl.Success(new() {
                Message = Lang.SaveSuccessful,
                WaitTime = 2
            });

            EditingIPConfig.AcceptChanges();
            AutoComplete = EditingIPConfig.Backup;

            _isSaveSuccessful = true;
        }
        catch (Exception ex)
        {
            Growl.Error($"{Lang.SaveFailed}\n\n{ex.Message}");

            _isSaveSuccessful = false;
        }
    }

    [RelayCommand]
    private async Task SaveAndApplyAsync()
    {
        Save();

        if (_isSaveSuccessful)
        {
            await ApplyAsync();
        }
    }

    [RelayCommand]
    private void TrimRemarkText()
    {
        if (EditingIPConfig is not null)
        {
            EditingIPConfig.Remark = EditingIPConfig.Remark.Trim();
        }
    }

    [RelayCommand]
    private void TryAutoCompleteDns1()
    {
        if (String.IsNullOrEmpty(EditingIPConfig.IPv4Config.Dns1))
        {
            EditingIPConfig.IPv4Config.Dns1 = AutoComplete.IPv4Config.Dns1;
        }
    }

    [RelayCommand]
    private void TryAutoCompleteDns2()
    {
        if (String.IsNullOrEmpty(EditingIPConfig.IPv4Config.Dns2))
        {
            EditingIPConfig.IPv4Config.Dns2 = AutoComplete.IPv4Config.Dns2;
        }
    }

    [RelayCommand]
    private void TryAutoCompleteGateway()
    {
        if (String.IsNullOrEmpty(EditingIPConfig.IPv4Config.Gateway))
        {
            EditingIPConfig.IPv4Config.Gateway = AutoComplete.IPv4Config.Gateway;
        }
    }

    [RelayCommand]
    private void TryAutoCompleteIP()
    {
        if (String.IsNullOrEmpty(EditingIPConfig.IPv4Config.IP))
        {
            EditingIPConfig.IPv4Config.IP = AutoComplete.IPv4Config.IP;
        }
    }

    [RelayCommand]
    private void TryAutoCompleteMask()
    {
        if (String.IsNullOrEmpty(EditingIPConfig.IPv4Config.Mask))
        {
            EditingIPConfig.IPv4Config.Mask = AutoComplete.IPv4Config.Mask;
        }
    }

    [RelayCommand]
    private void TryAutoCompleteName()
    {
        if (String.IsNullOrEmpty(EditingIPConfig.Name))
        {
            EditingIPConfig.Name = AutoComplete.Name;
        }
    }

    [RelayCommand]
    private void TryAutoCompleteRemark()
    {
        if (String.IsNullOrEmpty(EditingIPConfig.Remark))
        {
            EditingIPConfig.Remark = AutoComplete.Remark;
        }
    }

    [RelayCommand]
    private void Validate()
    {
        EditingIPConfig.ValidateAllProperties();

        if (EditingIPConfig.HasErrors)
        {
            Growl.Error(Lang.ValidationError);
        }
        else
        {
            Growl.Success(new() {
                Message = Lang.ValidateSuccessful,
                WaitTime = 2
            });
        }
    }

    #endregion Relay Commands

    #region Event Handlers

    private void EditingIPConfig_EditableChanged(object? sender, bool e)
    {
        UpdateSaveAndApplyButton();
    }

    private void EditingIPConfig_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        OnPropertyChanged(nameof(IsEditingIPConfigModifield));
        DiscardChangesCommand.NotifyCanExecuteChanged();

        if (e.PropertyName == nameof(IPConfigModel.Name))
        {
            GetAutoCompleteName();
        }
        else if (e.PropertyName is nameof(IPv4Config.IP) or nameof(IPv4Config.Mask))
        {
            GetAutoCompleteGateway();
        }
        else if (e.PropertyName == nameof(IPv4Config.Dns1))
        {
            GetAutoCompleteDns2();
        }
        else if (e.PropertyName == nameof(IPv4Config.Dns2))
        {
            GetAutoCompleteDns1();
        }
    }

    private void UpdateSaveAndApplyButton()
    {
        OnPropertyChanged(nameof(PrimarySaveOrApplyCommand));
        OnPropertyChanged(nameof(PrimarySaveOrApplyString));
        OnPropertyChanged(nameof(SecondarySaveOrApplyCommand));
        OnPropertyChanged(nameof(SecondarySaveOrApplyString));
    }

    #endregion Event Handlers

    #region Private Methods

    private static IEnumerable<IPv4Dns> ReadIPv4DnsList()
    {
        var iPv4Dns1List = IPv4Dns.ReadIPv4DnsList();

        var iPv4Dns2List = iPv4Dns1List
            .Where(x => x.Dns2 is not null)
            .Select(x => new IPv4Dns(x.Provider, x.Filter, x.Dns2!, x.Dns1, x.Group, x.Description));

        var iPv4DnsList = iPv4Dns1List.Concat(iPv4Dns2List);

        return iPv4DnsList;
    }

    private async Task BackupAsync()
    {
        if (_nic is null)
        {
            return;
        }

        try
        {
            var current = NetworkManagement.GetIPv4Config(_nic);

            if (current.PropertyEquals(EditingIPConfig.IPv4Config))
            {
                return;
            }

            await LastUsedIPv4Config.BackupAsync(_nic.Id, current);
        }
        catch
        { }
    }

    private bool? CanShowIPConfigPropertyChangedIndicator<T>(Func<IPConfigModel, T> property) where T : notnull
    {
        if (EditingIPConfig.IsPropertyChanged(property))
        {
            if (CanShowChangedIndicator)
            {
                return true;
            }

            if (CanShowUnchangedIndicator)
            {
                return false;
            }
        }

        return null;
    }

    private void FormatInput()
    {
        if (EditingIPConfig.IsChanged)
        {
            EditingIPConfig.IPv4Config.FormatProperties();
        }
    }

    private void GetAutoCompleteDns1()
    {
        if (!String.IsNullOrEmpty(EditingIPConfig.IPv4Config.Dns1))
        {
            return;
        }

        // 查找 DNS2(备用) 是否正在使用 DNS 列表中的选项。
        var dns = _iPv4DnsList.FirstOrDefault(x => EditingIPConfig.IPv4Config.Dns2 == x.Dns1 || EditingIPConfig.IPv4Config.Dns2 == x.Dns2);

        if (dns is null)
        {
            AutoComplete.IPv4Config.Dns1 = EditingIPConfig.Backup.IPv4Config.Dns1;
        }
        else
        {
            string? dns1 = EditingIPConfig.IPv4Config.Dns2 == dns.Dns1 ? dns.Dns2 : dns.Dns1;
            AutoComplete.IPv4Config.Dns1 = dns1 ?? EditingIPConfig.Backup.IPv4Config.Dns1;
        }
    }

    private void GetAutoCompleteDns2()
    {
        if (!String.IsNullOrEmpty(EditingIPConfig.IPv4Config.Dns2))
        {
            return;
        }

        var dns = _iPv4DnsList.FirstOrDefault(x => EditingIPConfig.IPv4Config.Dns1 == x.Dns1 || EditingIPConfig.IPv4Config.Dns1 == x.Dns2);

        if (dns is null)
        {
            AutoComplete.IPv4Config.Dns2 = EditingIPConfig.Backup.IPv4Config.Dns2;
        }
        else
        {
            string? dns2 = EditingIPConfig.IPv4Config.Dns1 == dns.Dns1 ? dns.Dns2 : dns.Dns1;
            AutoComplete.IPv4Config.Dns2 = dns2 ?? EditingIPConfig.Backup.IPv4Config.Dns2;
        }
    }

    private void GetAutoCompleteGateway()
    {
        if (!String.IsNullOrEmpty(EditingIPConfig.IPv4Config.Gateway))
        {
            return;
        }

        bool isIPValid = IPAddress.TryParse(EditingIPConfig.IPv4Config.IP, out var ip);
        bool isMaskValid = IPAddress.TryParse(EditingIPConfig.IPv4Config.Mask, out var mask);

        var gatewaySegments = new List<int>();

        if (isIPValid && isMaskValid)
        {
            byte[] ipSegments = ip!.GetAddressBytes();
            byte[] maskSegments = mask!.GetAddressBytes();

            for (int i = 0; i < Math.Min(ipSegments.Length, maskSegments.Length); i++)
            {
                int gatewayGroup = ipSegments[i] & maskSegments[i];
                gatewaySegments.Add(gatewayGroup);
            }
        }

        if (gatewaySegments is [.., 0])
        {
            gatewaySegments[^1] = 1;
        }

        if (gatewaySegments is [])
        {
            AutoComplete.IPv4Config.Gateway = EditingIPConfig.Backup.IPv4Config.Gateway;
        }
        else
        {
            AutoComplete.IPv4Config.Gateway = String.Join(".", gatewaySegments);
        }
    }

    private void GetAutoCompleteName()
    {
        if (String.IsNullOrEmpty(EditingIPConfig.Name))
        {
            if (String.IsNullOrEmpty(AutoComplete.Name))
            {
                AutoComplete.Name = Lang.NameMaxLength50;
            }
        }
    }

    #endregion Private Methods

    #region IEditableObject

    public void BeginEdit()
    {
        if (!_inTxn)
        {
            _backup = EditingIPConfig.DeepClone();
            _inTxn = true;
        }
    }

    public void CancelEdit()
    {
        if (_inTxn)
        {
            _backup.DeepCloneTo(EditingIPConfig);
            _inTxn = false;
        }
    }

    public void EndEdit()
    {
        if (_inTxn)
        {
            _backup = EditableIPConfigModel.Empty;
            _inTxn = false;
        }
    }

    #endregion IEditableObject

    #region IRevertibleChangeTracking

    public bool IsChanged => !EditingIPConfig.PropertyEquals(_backup);

    public void AcceptChanges()
    {
        EndEdit();
    }

    public void RejectChanges()
    {
        CancelEdit();
    }

    #endregion IRevertibleChangeTracking
}
