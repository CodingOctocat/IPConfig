using System;
using System.Collections.Generic;

using IPConfig.Models;

using LiteDB;

namespace IPConfig.Helpers;

public static class LiteDbHelper
{
    public static void Handle(Action<ILiteCollection<EditableIPConfigModel>> action)
    {
        Handle(col => {
            action(col);

            return true;
        });
    }

    public static T Handle<T>(Func<ILiteCollection<EditableIPConfigModel>, T> func)
    {
        App.IsDbSyncing = true;

        using var db = new LiteDatabase("Filename=ipconfig.db; Connection=Shared;");
        var col = db.GetCollection<EditableIPConfigModel>("ipconfigs");

        var result = func(col);

        App.IsDbSyncing = false;

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
}
