using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
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

    private readonly WpfObservableRangeCollection<IPv4Dns> _iPv4DnsList = new();

    private readonly WpfObservableRangeCollection<IPv4Mask> _iPv4MaskList = new();

    private EditableIPConfigModel _backup = EditableIPConfigModel.Empty;

    private bool _inTxn;

    private bool _loaded;

    private Nic? _nic;

    private bool _saved;

    #endregion Fields

    #region ObservableProperties

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
    private string? _dns1AutoCompletePreview;

    [ObservableProperty]
    private string? _dns2AutoCompletePreview;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEditingIPConfigModifield))]
    [NotifyCanExecuteChangedFor(nameof(DiscardChangesCommand))]
    private EditableIPConfigModel _editingIPConfig = IPConfigModel.Empty.AsEditable();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PrimarySaveOrApplyString),
        nameof(SecondarySaveOrApplyString),
        nameof(PrimarySaveOrApplyCommand),
        nameof(SecondarySaveOrApplyCommand))]
    private object _editingIPConfigSender = null!;

    [ObservableProperty]
    private string? _gatewayAutoCompletePreview;

    [ObservableProperty]
    private bool _isInContrastView;

    #endregion ObservableProperties

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

    public ICommand PrimarySaveOrApplyCommand
    {
        get
        {
            if (EditingIPConfigSender is MainViewModel)
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
            if (EditingIPConfigSender is MainViewModel)
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
            if (EditingIPConfigSender is MainViewModel)
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
            if (EditingIPConfigSender is MainViewModel)
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
    /// 接收 <seealso cref="MainViewModel.SelectedNicIPConfig"/> 或 <seealso cref="IPConfigListViewModel.SelectedIPConfig"/>。
    /// </summary>
    /// <param name="message"></param>
    public void Receive(PropertyChangedMessage<EditableIPConfigModel?> message)
    {
        var newIPConfig = message.NewValue;
        var oldIPConfig = message.OldValue;

        if (oldIPConfig is not null)
        {
            oldIPConfig.EditableChanged -= EditingIPConfig_EditableChanged;
            oldIPConfig.IPv4Config.AllowAutoDisableDhcp(false);
            oldIPConfig.IPv4Config.AllowAutoDisableAutoDns(false);
        }

        EditingIPConfigSender = message.Sender;

        if (newIPConfig is null)
        {
            EditingIPConfig = IPConfigModel.Empty.AsEditable();
        }
        else
        {
            EditingIPConfig = newIPConfig;
        }

        EditingIPConfig.BeginEdit();
        EditingIPConfig.IPv4Config.AllowAutoDisableDhcp();
        EditingIPConfig.IPv4Config.AllowAutoDisableDhcp();
        EditingIPConfig.EditableChanged += EditingIPConfig_EditableChanged;

        // 初始化页面状态。
        CanShowChangedIndicator = false;
        CanShowUnchangedIndicator = false;
    }

    public void Receive(PropertyChangedMessage<Nic?> message)
    {
        _nic = message.NewValue;
    }

    protected override void OnActivated()
    {
        base.OnActivated();

        Messenger.Register<IPConfigDetailViewModel, RequestMessage<ConfirmSave>>(this,
            (r, m) => m.Reply(new(EditingIPConfigSender, EditingIPConfig, !IsEditingIPConfigModifield)));

        Messenger.Register<IPConfigDetailViewModel, SaveMessage>(this,
            (r, m) => SaveCommand.Execute(null));

        Messenger.Register<IPConfigDetailViewModel, CancelEditMessage>(this,
            (r, m) => DiscardChangesCommand.Execute(m.Ask));
    }

    #endregion Constructors & Recipients

    #region IEditableObject

    public void BeginEdit()
    {
        if (!_inTxn)
        {
            EditingIPConfig.DeepCloneTo(_backup);
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

    #region RelayCommands

    [RelayCommand]
    private static void CopyDns(string dns)
    {
        Clipboard.SetText(dns);
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
    private void Apply()
    {
        FormatInput();

        if (_nic is null)
        {
            Growl.Error(Lang.ApplyFailed);

            return;
        }

        var result = HcMessageBox.Show(
            Lang.ApplyAsk_Format.Format(_nic.Name, _nic.Description),
            App.AppName,
            MessageBoxButton.OKCancel,
            MessageBoxImage.Question,
            MessageBoxResult.OK);

        if (result != MessageBoxResult.OK)
        {
            return;
        }

        var ipv4Config = EditingIPConfig.IPv4Config;

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
            }
        }
        else
        {
            EditingIPConfig.RejectChanges();
            RejectChanges();
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

                _saved = false;

                return;
            }

            LiteDbHelper.Handle(col => {
                if (EditingIPConfig.IsPropertyChanged(x => x.Name, out string? oldValue))
                {
                    col.Delete(new BsonValue(oldValue));
                }

                col.Upsert(EditingIPConfig);
            });

            if (EditingIPConfigSender is MainViewModel)
            {
                Messenger.Send<EditableIPConfigModel, string>(EditingIPConfig, "MakeSelectedNicIPConfigCopy");
            }

            Growl.Success(new() {
                Message = Lang.SaveSuccessful,
                WaitTime = 2
            });

            EditingIPConfig.AcceptChanges();

            _saved = true;
        }
        catch (Exception ex)
        {
            Growl.Error($"{Lang.SaveFailed}\n\n{ex.Message}");

            _saved = false;
        }
    }

    [RelayCommand]
    private void SaveAndApply()
    {
        Save();

        if (_saved)
        {
            Apply();
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
        if (!String.IsNullOrEmpty(EditingIPConfig.IPv4Config.Dns1))
        {
            return;
        }

        EditingIPConfig.IPv4Config.Dns1 = GetAutoCompleteDns1();
    }

    [RelayCommand]
    private void TryAutoCompleteDns2()
    {
        if (!String.IsNullOrEmpty(EditingIPConfig.IPv4Config.Dns2))
        {
            return;
        }

        EditingIPConfig.IPv4Config.Dns2 = GetAutoCompleteDns2();
    }

    [RelayCommand]
    private void TryAutoCompleteGateway()
    {
        if (!String.IsNullOrEmpty(EditingIPConfig.IPv4Config.Gateway))
        {
            return;
        }

        EditingIPConfig.IPv4Config.Gateway = GetAutoCompleteGateway();
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

    #endregion RelayCommands

    #region Partial OnPropertyChanged Methods

    partial void OnEditingIPConfigChanged([DisallowNull] EditableIPConfigModel? oldValue, EditableIPConfigModel newValue)
    {
        // 注销旧对象事件。
        oldValue.PropertyChanged -= EditingIPConfig_PropertyChanged;

        // 确保新对象注册事件以实现实时自动完成预览及相关属性通知。
        newValue.PropertyChanged -= EditingIPConfig_PropertyChanged;
        newValue.PropertyChanged += EditingIPConfig_PropertyChanged;

        GatewayAutoCompletePreview = GetAutoCompleteGateway();
        Dns1AutoCompletePreview = GetAutoCompleteDns1();
        Dns2AutoCompletePreview = GetAutoCompleteDns2();
    }

    #endregion Partial OnPropertyChanged Methods

    #region Event Handlers

    private void EditingIPConfig_EditableChanged(object? sender, PropertyChangedEventArgs e)
    {
        OnPropertyChanged(nameof(PrimarySaveOrApplyCommand));
        OnPropertyChanged(nameof(PrimarySaveOrApplyString));
        OnPropertyChanged(nameof(SecondarySaveOrApplyCommand));
        OnPropertyChanged(nameof(SecondarySaveOrApplyString));
    }

    private void EditingIPConfig_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        OnPropertyChanged(nameof(IsEditingIPConfigModifield));
        DiscardChangesCommand.NotifyCanExecuteChanged();

        if (e.PropertyName is nameof(IPv4Config.IP) or nameof(IPv4Config.Mask))
        {
            GatewayAutoCompletePreview = GetAutoCompleteGateway();
        }

        if (e.PropertyName == nameof(IPv4Config.Dns1))
        {
            Dns2AutoCompletePreview = GetAutoCompleteDns2();
        }

        if (e.PropertyName == nameof(IPv4Config.Dns2))
        {
            Dns1AutoCompletePreview = GetAutoCompleteDns1();
        }
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
        EditingIPConfig?.IPv4Config.FormatProperties();
    }

    private string GetAutoCompleteDns1()
    {
        // 查找 DNS2(备用) 是否正在使用 DNS 列表中的选项。
        var dns = _iPv4DnsList.FirstOrDefault(x => EditingIPConfig.IPv4Config.Dns2 == x.Dns1 || EditingIPConfig.IPv4Config.Dns2 == x.Dns2);

        if (dns is null)
        {
            return "";
        }

        string? dns1 = EditingIPConfig.IPv4Config.Dns2 == dns.Dns1 ? dns.Dns2 : dns.Dns1;

        return dns1 ?? "";
    }

    private string GetAutoCompleteDns2()
    {
        var dns = _iPv4DnsList.FirstOrDefault(x => EditingIPConfig.IPv4Config.Dns1 == x.Dns1 || EditingIPConfig.IPv4Config.Dns1 == x.Dns2);

        if (dns is null)
        {
            return "";
        }

        string? dns2 = EditingIPConfig.IPv4Config.Dns1 == dns.Dns1 ? dns.Dns2 : dns.Dns1;

        return dns2 ?? "";
    }

    private string GetAutoCompleteGateway()
    {
        bool isIPValid = IPAddress.TryParse(EditingIPConfig.IPv4Config.IP, out var ip);
        bool isMaskValid = IPAddress.TryParse(EditingIPConfig.IPv4Config.Mask, out var mask);

        var gatewayGroups = new List<int>();

        if (isIPValid && isMaskValid)
        {
            byte[] ipGroups = ip!.GetAddressBytes();
            byte[] maskGroups = mask!.GetAddressBytes();

            for (int i = 0; i < Math.Min(ipGroups.Length, maskGroups.Length); i++)
            {
                int gatewayGroup = ipGroups[i] & maskGroups[i];
                gatewayGroups.Add(gatewayGroup);
            }
        }

        if (gatewayGroups is [.., 0])
        {
            gatewayGroups[^1] = 1;
        }

        string gateway = String.Join(".", gatewayGroups);

        return gateway;
    }

    #endregion Private Methods
}
