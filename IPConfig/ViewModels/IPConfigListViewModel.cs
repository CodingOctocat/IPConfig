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

    #region Observable Properties

    [ObservableProperty]
    private string _searchKeyword = String.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    [NotifyCanExecuteChangedFor(nameof(MoveToTopCommand), nameof(MoveToBottomCommand))]
    private EditableIPConfigModel? _selectedIPConfig;

    #endregion Observable Properties

    #region Properties

    public bool CanDeleteIPConfig => HasSelected;

    public bool CanMoveToBottom => SelectedIndex < IPConfigList.Count - 1;

    public bool CanMoveToTop => SelectedIndex > 0;

    public bool HasSelected => SelectedCount > 0;

    public int IPConfigCount => IPConfigList.Count;

    public WpfObservableRangeCollection<EditableIPConfigModel> IPConfigList { get; } = new();

    public ICollectionView IPConfigListCollectionView { get; }

    public CollectionViewSource IPConfigListCvs { get; }

    public string MultiSelectedCountString => SelectedCount > 1 ? SelectedCount.ToString() : "";

    public EditableIPConfigModel? PrimarySelectedIPConfig => SelectedIPConfigs.FirstOrDefault();

    public int SelectedCount => SelectedIPConfigs.Count;

    public int SelectedIndex => IPConfigList.IndexOf(SelectedIPConfig!);

    public WpfObservableRangeCollection<EditableIPConfigModel> SelectedIPConfigs { get; } = new();

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

        Messenger.Register<IPConfigListViewModel, EmptyMessage, string>(this, "SelectedNicIPConfigChecked",
            (r, m) => SelectedIPConfig = null);

        Messenger.Register<IPConfigListViewModel, EditableIPConfigModel, string>(this, "MakeSelectedNicIPConfigCopy",
            (r, m) => MakeSelectedIPConfigCopy(m));

        Messenger.Register<IPConfigListViewModel, RequestMessage<IEnumerable<EditableIPConfigModel>>, string>(this, "IPConfigList",
            (r, m) => m.Reply(IPConfigList));

        Messenger.Register<IPConfigListViewModel, RequestMessage<IEnumerable<EditableIPConfigModel>>, string>(this, "ModifieldIPConfigs",
            (r, m) => m.Reply(IPConfigList.Where(x => x.IsChanged)));

        Messenger.Register<IPConfigListViewModel, AddUntitledIPConfigMessage>(this,
            (r, m) => AddUntitledIPConfig());

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
                         let match = Regex.Match(x.Name, $@"^{Lang.Untitled}(\d+)$")
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
        int idx = SelectedIndex;

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
    private async Task LoadedAsync()
    {
        var col = await Task.Run(() => {
            LiteDbHelper.Handle(col => col.EnsureIndex(x => x.Order));

            return LiteDbHelper.Query().OrderBy(x => x.Order).ToList();
        });

        IPConfigList.AddRange(col);
    }

    [RelayCommand(CanExecute = nameof(HasSelected))]
    private void MakeSelectedIPConfigCopy()
    {
        MakeSelectedIPConfigCopy(SelectedIPConfig);
    }

    [RelayCommand(CanExecute = nameof(CanMoveToBottom))]
    private void MoveToBottom()
    {
        IPConfigList.Move(SelectedIndex, IPConfigList.Count - 1);
    }

    [RelayCommand(CanExecute = nameof(CanMoveToTop))]
    private void MoveToTop()
    {
        IPConfigList.Move(SelectedIndex, 0);
    }

    [RelayCommand]
    private void Search(string keyword)
    {
        if (!String.IsNullOrEmpty(keyword))
        {
            _selectedIPConfigBeforeSearch = SelectedIPConfig;
        }

        using (IPConfigListCvs.DeferRefresh())
        {
            IPConfigListCollectionView.Refresh();
        }

        if (String.IsNullOrEmpty(keyword))
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
            Messenger.Send<ToggleStateMessage<bool>, string>(new(this, false), "ToggleNicInfoCard");
        }
    }

    #endregion Partial OnPropertyChanged Methods

    #region Event Handlers

    private void IPConfigList_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        OnPropertyChanged(nameof(IPConfigCount));
        MoveToTopCommand.NotifyCanExecuteChanged();
        MoveToBottomCommand.NotifyCanExecuteChanged();

        if (IPConfigCount == 0)
        {
            Messenger.Send<ToggleStateMessage<bool>, string>(new(this, true), "ToggleNicInfoCard");
        }
    }

    private void IPConfigListCvs_Filter(object sender, FilterEventArgs e)
    {
        if (String.IsNullOrWhiteSpace(SearchKeyword))
        {
            e.Accepted = true;

            return;
        }

        var item = (IPConfigModel)e.Item;

        void Contains(Func<IPConfigModel, string> property)
        {
            if (property(item).Contains(SearchKeyword, StringComparison.OrdinalIgnoreCase))
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
        OnPropertyChanged(nameof(SelectedCount));
        OnPropertyChanged(nameof(MultiSelectedCountString));
        OnPropertyChanged(nameof(PrimarySelectedIPConfig));
        DeleteIPConfigCommand.NotifyCanExecuteChanged();
    }

    #endregion Event Handlers

    #region Private Methods

    private void InsertNewIPConfig(EditableIPConfigModel iPConfig)
    {
        IPConfigList.Insert(SelectedIndex + 1, iPConfig);
        SelectedIPConfig = iPConfig;
    }

    private void MakeSelectedIPConfigCopy(EditableIPConfigModel? original)
    {
        if (original is null)
        {
            return;
        }

        var orders = from x in IPConfigList
                     let match = Regex.Match(x.Name, $@"^{Lang.Copy_Noun}(\d+)-{Regex.Escape(original.Name)}$")
                     where match.Success
                     select Convert.ToInt32(match.Groups[1].Value);

        int order = FileOrderHelper.GetOrder(orders);
        var copy = original.DeepClone();
        copy.Name = $"{Lang.Copy_Noun}{order}-{original.Name}";
        copy.AcceptChanges();
        InsertNewIPConfig(copy);

        copy.ValidateAllProperties();

        if (!copy.HasErrors)
        {
            LiteDbHelper.Handle(col => col.Insert(copy));
        }
    }

    #endregion Private Methods
}
