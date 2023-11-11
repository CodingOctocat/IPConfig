using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Text.Json.Serialization;

using IPConfig.Helpers;

using LiteDB;

namespace IPConfig.Models;

public partial class EditableIPConfigModel : IPConfigModel,
    IDeepCloneable<EditableIPConfigModel>, IDeepCloneTo<EditableIPConfigModel>,
    IEditableObject, IRevertibleChangeTracking
{
    private IPConfigModel _backup = IPConfigModel.Empty;

    private bool _inTxn = false;

    public static new EditableIPConfigModel Empty => new("");

    /// <summary>
    /// 获取备份对象的副本。
    /// </summary>
    public IPConfigModel Backup => _backup.DeepClone();

    [JsonIgnore]
    public int Order { get; set; } = -1;

    public event EventHandler<bool>? EditableChanged;

    public event PropertyChangedEventHandler? EditablePropertyChanged;

    [BsonCtor]
    public EditableIPConfigModel(string name) : base(name)
    { }

    public bool IsPropertyChanged<T>(Func<IPConfigModel, T> property) where T : notnull
    {
        return IsPropertyChanged(property, out _);
    }

    public bool IsPropertyChanged<T>(Func<IPConfigModel, T> property, [NotNullWhen(true)] out T? oldValue) where T : notnull
    {
        var prop = property(this);

        if (!_inTxn)
        {
            oldValue = prop;

            return false;
        }

        var backup = property(_backup);

        if (EqualityComparer<T>.Default.Equals(prop, backup))
        {
            oldValue = prop;

            return false;
        }

        oldValue = backup;

        return true;
    }

    public bool IsPropertyError<T>(Expression<Func<IPConfigModel, T>> property, out IEnumerable<ValidationResult> validationResults)
    {
        string propertyName = NameOfHelper.NameOf(property);

        if (propertyName.StartsWith($"{nameof(IPv4Config)}."))
        {
            validationResults = IPv4Config.GetErrors(propertyName[$"{nameof(IPv4Config)}.".Length..]);
        }
        else
        {
            validationResults = GetErrors();
        }

        return validationResults.Any();
    }

    public bool IsPropertyError<T>(Expression<Func<IPConfigModel, T>> property)
    {
        return IsPropertyError(property, out _);
    }

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        if (e.PropertyName != nameof(IsChanged))
        {
            OnPropertyChanged(nameof(IsChanged));
            EditablePropertyChanged?.Invoke(this, e);
        }
        else
        {
            EditableChanged?.Invoke(this, IsChanged);
        }
    }

    #region IDeepCloneable

    public new EditableIPConfigModel DeepClone()
    {
        var clone = Empty;
        DeepCloneTo(clone);

        return clone;
    }

    #endregion IDeepCloneable

    #region IDeepCloneTo

    public void DeepCloneTo(EditableIPConfigModel other)
    {
        base.DeepCloneTo(other);
        _backup.DeepCloneTo(other._backup);
        other._inTxn = _inTxn;
        other.Order = Order;
    }

    #endregion IDeepCloneTo

    #region IEditableObject

    public void BeginEdit()
    {
        if (!_inTxn)
        {
            _backup = DeepClone();
            OnPropertyChanged(nameof(Backup));
            _inTxn = true;
            OnPropertyChanged(nameof(IsChanged));
        }
    }

    public void CancelEdit()
    {
        if (_inTxn)
        {
            _backup.DeepCloneTo(this);
            _inTxn = false;
            OnPropertyChanged(nameof(IsChanged));
        }
    }

    public void EndEdit()
    {
        if (_inTxn)
        {
            _backup = Empty;
            OnPropertyChanged(nameof(Backup));
            _inTxn = false;
            OnPropertyChanged(nameof(IsChanged));
        }
    }

    #endregion IEditableObject

    #region IRevertibleChangeTracking

    [BsonIgnore]
    [JsonIgnore]
    public bool IsChanged => _inTxn && !PropertyEquals(_backup);

    public void AcceptChanges()
    {
        EndEdit();
        BeginEdit();
    }

    public void RejectChanges()
    {
        CancelEdit();
        BeginEdit();
    }

    #endregion IRevertibleChangeTracking
}
