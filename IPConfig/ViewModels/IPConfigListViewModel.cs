using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.Specialized;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

using CodingNinja.Wpf.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;

using IPConfig.Extensions;
using IPConfig.Helpers;
using IPConfig.Languages;
using IPConfig.Models;
using IPConfig.Models.Messages;

using LiteDB;

using Microsoft.Extensions.DependencyInjection;

using HcMessageBox = HandyControl.Controls.MessageBox;

namespace IPConfig.ViewModels;

public partial class IPConfigListViewModel : ObservableRecipient
{
    #region Fields

    private readonly object _syncLock = new();

    private EditableIPConfigModel? _selectedIPConfigBeforeSearch;

    #endregion Fields

    #region ObservableProperties

    [ObservableProperty]
    private string _searchKeyword = String.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(MoveToTopCommand), nameof(MoveToBottomCommand))]
    private EditableIPConfigModel? _selectedIPConfig;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedIPConfigsCountString))]
    private int _selectedIPConfigsCount;

    #endregion ObservableProperties

    #region Properties

    public bool CanDeleteIPConfig => HasAnyIPConfigSelected;

    public bool CanMoveToBottom => SelectedIPConfigIndex < IPConfigList.Count - 1;

    public bool CanMoveToTop => SelectedIPConfigIndex > 0;

    public bool HasAnyIPConfigSelected => SelectedIPConfigsCount > 0;

    public WpfObservableRangeCollection<EditableIPConfigModel> IPConfigList { get; } = new();

    public ICollectionView IPConfigListCollectionView { get; }

    public CollectionViewSource IPConfigListCvs { get; }

    public EditableIPConfigModel? PrimarySelectedIPConfig => SelectedIPConfigs.FirstOrDefault();

    public int SelectedIPConfigIndex => IPConfigList.IndexOf(SelectedIPConfig!);

    public WpfObservableRangeCollection<EditableIPConfigModel> SelectedIPConfigs { get; } = new();

    public string SelectedIPConfigsCountString => SelectedIPConfigsCount > 1 ? SelectedIPConfigsCount.ToString() : "";

    #endregion Properties

    #region Constructors & Recipients

    public IPConfigListViewModel()
    {
        IsActive = true;

        IPConfigList.CollectionChanged += IPConfigList_CollectionChanged;

        IPConfigListCvs = new() {
            IsLiveFilteringRequested = true,
            IsLiveGroupingRequested = true,
            IsLiveSortingRequested = true,
            Source = IPConfigList,
        };

        IPConfigListCvs.Filter += IPConfigListCvs_Filter;

        IPConfigListCollectionView = IPConfigListCvs.View;

        SelectedIPConfig = null;

        SelectedIPConfigs.CollectionChanged += SelectedIPConfigs_CollectionChanged;

        BindingOperations.EnableCollectionSynchronization(IPConfigList, _syncLock);
        BindingOperations.EnableCollectionSynchronization(SelectedIPConfigs, _syncLock);
    }

    protected override void OnActivated()
    {
        base.OnActivated();

        Messenger.Register<IPConfigListViewModel, ValueChangedMessage<bool>, string>(this, "SelectedNicIPConfigChecked",
            (r, m) => SelectedIPConfig = null);

        Messenger.Register<IPConfigListViewModel, EditableIPConfigModel, string>(this, "MakeSelectedNicIPConfigCopy",
            (r, m) => MakeSelectedIPConfigCopy(m));

        Messenger.Register<IPConfigListViewModel, RequestMessage<IEnumerable<EditableIPConfigModel>>, string>(this, "IPConfigList",
            (r, m) => m.Reply(IPConfigList));

        Messenger.Register<IPConfigListViewModel, RequestMessage<IEnumerable<EditableIPConfigModel>>, string>(this, "ModifieldIPConfigs",
            (r, m) => m.Reply(IPConfigList.Where(x => x.IsChanged)));

        Messenger.Register<IPConfigListViewModel, AddUntitledIPConfigMessage>(this,
            (r, m) => AddUntitledIPConfig());

        Messenger.Register<IPConfigListViewModel, RequestMessage<EditableIPConfigModel?>, string>(this, "SelectedIPConfig",
            (r, m) => m.Reply(SelectedIPConfig));

        Messenger.Register<IPConfigListViewModel, ChangeSelectionMessage<EditableIPConfigModel>>(this,
            (r, m) => SelectedIPConfig = m.Selection);

        Messenger.Register<IPConfigListViewModel, CollectionChangeActionMessage<EditableIPConfigModel>>(this,
            (r, m) => {
                switch (m.Action)
                {
                    case CollectionChangeAction.Add:
                        IPConfigList.Add(m.Item);
                        break;

                    case CollectionChangeAction.Remove:
                        IPConfigList.Remove(m.Item);
                        break;

                    case CollectionChangeAction.Refresh:
                        IPConfigListCollectionView.Refresh();
                        break;

                    default:
                        break;
                }
            });
    }

    #endregion Constructors & Recipients

    #region Public Methods

    public static ValidationResult ValidateName(string name)
    {
        var instance = App.Current.Services.GetRequiredService<IPConfigListViewModel>();
        bool isValid = instance.IPConfigList.Count(x => x.Name == name) <= 1;

        if (isValid)
        {
            return ValidationResult.Success!;
        }

        return new(Lang.DuplicateIPConfigName);
    }

    #endregion Public Methods

    #region Relay Commands

    [RelayCommand]
    private void AddUntitledIPConfig(string name = "")
    {
        EditableIPConfigModel iPConfig;

        if (String.IsNullOrWhiteSpace(name))
        {
            var orders = from x in IPConfigList
                         let match = Regex.Match(x.Name, @$"^{Lang.Untitled}(\d+)$")
                         where match.Success
                         select Convert.ToInt32(match.Groups[1].Value);

            int order = FileOrderHelper.GetOrder(orders);
            iPConfig = IPConfigModel.GetUntitledWith(order).AsEditable();
        }
        else
        {
            iPConfig = new(name);
        }

        InsertNewIPConfig(iPConfig);
    }

    [RelayCommand]
    private async Task CopySelectedIPConfigsAsTextAsync()
    {
        string sep = new('=', 64);
        var sb = new StringBuilder();
        sb.AppendLine();

        foreach (var item in SelectedIPConfigs)
        {
            sb.AppendLine($"{item}{Environment.NewLine}{sep}");
        }

        string text = sb.ToString().Trim()[..^64];
        await ClipboardHelper.SetTextAsync(text);
    }

    [RelayCommand(CanExecute = nameof(CanDeleteIPConfig))]
    private void DeleteIPConfig()
    {
        var toBeDeleted = SelectedIPConfigs.ToImmutableArray();
        int idx = SelectedIPConfigIndex;

        if (toBeDeleted.Length > 0)
        {
            var result = HcMessageBox.Show(
                Lang.DeleteAsk_Format.Format(toBeDeleted.Length),
                App.AppName,
                MessageBoxButton.OKCancel,
                MessageBoxImage.Question,
                MessageBoxResult.OK);

            if (result != MessageBoxResult.OK)
            {
                return;
            }
        }

        var unremoved = new List<EditableIPConfigModel>();

        LiteDbHelper.Handle(col => {
            foreach (var item in toBeDeleted)
            {
                string name = item.Name;

                if (item.IsPropertyChanged(x => x.Name, out string? oldValue))
                {
                    name = oldValue;
                }

                col.Delete(new(name));
            }
        });

        if (toBeDeleted.Length == IPConfigList.Count)
        {
            IPConfigList.Clear();
        }
        else
        {
            IPConfigList.RemoveRange(toBeDeleted);
        }

        SelectedIPConfig = IPConfigList.ElementAtOrDefault(idx) ?? IPConfigList.LastOrDefault();
    }

    [RelayCommand]
    private void Loaded()
    {
        var col = LiteDbHelper.Query();
        IPConfigList.AddRange(col.OrderBy(x => x.Order).ToEnumerable());
    }

    [RelayCommand(CanExecute = nameof(HasAnyIPConfigSelected))]
    private void MakeSelectedIPConfigCopy()
    {
        MakeSelectedIPConfigCopy(SelectedIPConfig);
    }

    [RelayCommand(CanExecute = nameof(CanMoveToBottom))]
    private void MoveToBottom()
    {
        IPConfigList.Move(SelectedIPConfigIndex, IPConfigList.Count - 1);
    }

    [RelayCommand(CanExecute = nameof(CanMoveToTop))]
    private void MoveToTop()
    {
        IPConfigList.Move(SelectedIPConfigIndex, 0);
    }

    [RelayCommand]
    private void Search()
    {
        if (!String.IsNullOrEmpty(SearchKeyword))
        {
            _selectedIPConfigBeforeSearch = SelectedIPConfig;
        }

        using (IPConfigListCvs.DeferRefresh())
        {
            IPConfigListCollectionView.Refresh();
        }

        if (String.IsNullOrEmpty(SearchKeyword))
        {
            SelectedIPConfig = _selectedIPConfigBeforeSearch;
        }
        else
        {
            SelectedIPConfig = IPConfigListCollectionView.Cast<EditableIPConfigModel>().FirstOrDefault();
        }
    }

    #endregion Relay Commands

    #region Partial OnPropertyChanged Methods

    partial void OnSearchKeywordChanged(string value)
    {
        SearchCommand.Execute(value);
    }

    partial void OnSelectedIPConfigChanged(EditableIPConfigModel? oldValue, EditableIPConfigModel? newValue)
    {
        if (newValue is not null)
        {
            Messenger.Send<GoBackMessage>(new(this));
        }

        Messenger.Send<PropertyChangedMessage<EditableIPConfigModel?>>(new(this, nameof(SelectedIPConfig), oldValue, newValue));
    }

    #endregion Partial OnPropertyChanged Methods

    #region Event Handlers

    private void IPConfigList_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        MoveToTopCommand.NotifyCanExecuteChanged();
        MoveToBottomCommand.NotifyCanExecuteChanged();

        Messenger.Send<ValueChangedMessage<int>, string>(new(IPConfigList.Count), "TotalIPConfigsCount");
    }

    private void IPConfigListCvs_Filter(object sender, FilterEventArgs e)
    {
        if (String.IsNullOrWhiteSpace(SearchKeyword))
        {
            e.Accepted = true;

            return;
        }

        var item = (IPConfigModel)e.Item;
        string keyword = SearchKeyword.ToLower();

        void Contains(Func<IPConfigModel, string> property)
        {
            if (property(item).ToLower().Contains(keyword))
            {
                e.Accepted = true;

                throw new Exception("return");
            }
            else
            {
                e.Accepted = false;
            }
        }

        try
        {
            Contains(x => x.Name);
            Contains(x => x.IPv4Config.IP);
            Contains(x => x.IPv4Config.Mask);
            Contains(x => x.IPv4Config.Gateway);
            Contains(x => x.IPv4Config.Dns1);
            Contains(x => x.IPv4Config.Dns2);
            Contains(x => x.Remark);
        }
        catch (Exception ex) when (ex.Message == "return")
        {
            return;
        }
    }

    private void SelectedIPConfigs_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        SelectedIPConfigsCount = SelectedIPConfigs.Count;
        DeleteIPConfigCommand.NotifyCanExecuteChanged();
        OnPropertyChanged(nameof(PrimarySelectedIPConfig));
        Messenger.Send<ValueChangedMessage<int>, string>(new(SelectedIPConfigsCount), "SelectedIPConfigsCount");
    }

    #endregion Event Handlers

    #region Private Methods

    private void InsertNewIPConfig(EditableIPConfigModel iPConfig)
    {
        IPConfigList.Insert(SelectedIPConfigIndex + 1, iPConfig);
        SelectedIPConfig = iPConfig;
    }

    private void MakeSelectedIPConfigCopy(EditableIPConfigModel? original)
    {
        if (original is null)
        {
            return;
        }

        var orders = from x in IPConfigList
                     let match = Regex.Match(x.Name, @$"^{Lang.Copy_Noun}(\d+)-{Regex.Escape(original.Name)}$")
                     where match.Success
                     select Convert.ToInt32(match.Groups[1].Value);

        int order = FileOrderHelper.GetOrder(orders);
        var copy = original.DeepClone();
        copy.Name = $"{Lang.Copy_Noun}{order}-{original.Name}";
        copy.BeginEdit();
        InsertNewIPConfig(copy);

        copy.ValidateAllProperties();

        if (!copy.HasErrors)
        {
            LiteDbHelper.Handle(col => col.Insert(copy));
        }
    }

    #endregion Private Methods
}
