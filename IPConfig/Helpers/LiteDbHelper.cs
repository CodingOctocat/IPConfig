using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

using IPConfig.Models;

using LiteDB;

namespace IPConfig.Helpers;

public static class LiteDbHelper
{
    private static bool _isDbBusy;

    public static bool IsDbBusy
    {
        get => _isDbBusy;
        private set
        {
            if (_isDbBusy != value)
            {
                _isDbBusy = value;
                OnStaticPropertyChanged();
            }
        }
    }

    public static void Handle(Action<ILiteCollection<EditableIPConfigModel>> action)
    {
        Handle(col => {
            action(col);

            return true;
        });
    }

    public static T Handle<T>(Func<ILiteCollection<EditableIPConfigModel>, T> func)
    {
        IsDbBusy = true;

        using var db = new LiteDatabase("Filename=ipconfig.db; Connection=Shared;");
        var col = db.GetCollection<EditableIPConfigModel>("ipconfigs");

        var result = func(col);

        IsDbBusy = false;

        return result;
    }

    public static ILiteQueryable<EditableIPConfigModel> Query()
    {
        return Handle(col => {
            var result = col.Query();

            return result;
        });
    }

    public static void Query(Action<IEnumerable<EditableIPConfigModel>> action)
    {
        Handle(col => {
            var result = col.Query().ToEnumerable();
            action(result);
        });
    }

    public static T Query<T>(Func<IEnumerable<EditableIPConfigModel>, T> func)
    {
        return Handle(col => {
            var enumerable = col.Query().ToEnumerable();
            var result = func(enumerable);

            return result;
        });
    }

    #region Static Properties Change Notification

    public static event EventHandler<PropertyChangedEventArgs> StaticPropertyChanged = delegate { };

    private static void OnStaticPropertyChanged([CallerMemberName] string? staticPropertyName = null)
    {
        StaticPropertyChanged(null, new PropertyChangedEventArgs(staticPropertyName));
    }

    #endregion Static Properties Change Notification
}
