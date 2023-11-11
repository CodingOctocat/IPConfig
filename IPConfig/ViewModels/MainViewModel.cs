using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;

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
using IPConfig.Properties;

using HcMessageBox = HandyControl.Controls.MessageBox;

namespace IPConfig.ViewModels;

public partial class MainViewModel : ObservableRecipient
{
    #region Observable Properties

    [ObservableProperty]
    private bool _isInNicConfigDetailView;

    [ObservableProperty]
    private ObservableCollection<CultureInfo> _languages = null!;

    [ObservableProperty]
    private bool _topmost = Settings.Default.Topmost;

    #endregion Observable Properties

    #region Constructors & Recipients

    public MainViewModel()
    {
        IsActive = true;
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
    private void Loaded()
    {
        var cultures = LangSource.GetAvailableCultures().OrderBy(x => x.Name);
        Languages = new(cultures);
    }

    [RelayCommand]
    private void Save()
    {
        Messenger.Send<SaveMessage>(new(this));
    }

    #endregion Relay Commands

    #region Partial OnPropertyChanged Methods

    partial void OnTopmostChanged(bool value)
    {
        Settings.Default.Topmost = value;
        Settings.Default.Save();
    }

    #endregion Partial OnPropertyChanged Methods
}
