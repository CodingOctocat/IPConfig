using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

using CommunityToolkit.Mvvm.ComponentModel;

using IPConfig.Languages;
using IPConfig.Models.Validations;
using IPConfig.ViewModels;

using LiteDB;

namespace IPConfig.Models;

/// <summary>
/// 表示一个 IP 配置。
/// <para>MVVM Toolkit 源生成器暂不支持 <seealso cref="JsonIgnoreAttribute"/> 等其他特性，可以使用传统的完整属性(SetProperty)代替。</para>
/// </summary>
public partial class IPConfigModel : ObservableValidator, IDeepCloneable<IPConfigModel>, IDeepCloneTo<IPConfigModel>
{
    private IPv4Config _iPv4Config = new();

    private string _name = "";

    private string _remark = "";

    public static IPConfigModel Empty => new("");

    [BsonField("IPv4")]
    [JsonPropertyOrder(1)]
    [JsonPropertyName("IPv4")]
    [ForwardingErrors]
    public IPv4Config IPv4Config
    {
        get => _iPv4Config;
        set
        {
            if (EqualityComparer<IPv4Config>.Default.Equals(_iPv4Config, value))
            {
                return;
            }

            OnPropertyChanging();

            // 注销 oldValue 事件。
            _iPv4Config.ErrorsChanged -= IPv4Config_ErrorsChanged;
            _iPv4Config.PropertyChanged -= IPv4PropertyChanged;
            _iPv4Config.PropertyChanging -= IPv4PropertyChanging;

            // 确保 newValue 注册事件。
            value.PropertyChanging -= IPv4PropertyChanging;
            value.PropertyChanging += IPv4PropertyChanging;
            value.PropertyChanged -= IPv4PropertyChanged;
            value.PropertyChanged += IPv4PropertyChanged;
            value.ErrorsChanged -= IPv4Config_ErrorsChanged;
            value.ErrorsChanged += IPv4Config_ErrorsChanged;

            _iPv4Config = value;

            OnPropertyChanged();

            ValidateProperty(_iPv4Config);
        }
    }

    [BsonId]
    [JsonPropertyOrder(0)]
    [Languages.Required(LangKey.Required)]
    [CustomValidation(typeof(IPConfigListViewModel), nameof(IPConfigListViewModel.ValidateName))]
    public string Name
    {
        get => _name;
        set
        {
            // 修复错误信息更新滞后的问题。
            ClearErrors(nameof(Name));
            SetProperty(ref _name, value.Trim(), true);
        }
    }

    [JsonPropertyOrder(3)]
    public string Remark
    {
        get => _remark;
        set => SetProperty(ref _remark, value);
    }

    [BsonCtor]
    public IPConfigModel(string name)
    {
        Name = name;

        _iPv4Config = new();
        _iPv4Config.PropertyChanging += IPv4PropertyChanging;
        _iPv4Config.PropertyChanged += IPv4PropertyChanged;
        _iPv4Config.ErrorsChanged += IPv4Config_ErrorsChanged;
    }

    public static IPConfigModel GetUntitledWith(int order)
    {
        return new($"{Lang.Untitled}{order}");
    }

    public new void ClearErrors(string? propertyName = null)
    {
        base.ClearErrors(propertyName);
        IPv4Config.ClearErrors(propertyName);
    }

    #region IDeepCloneable

    public IPConfigModel DeepClone()
    {
        var clone = Empty;
        DeepCloneTo(clone);

        return clone;
    }

    #endregion IDeepCloneable

    #region IDeepCloneTo

    public void DeepCloneTo(IPConfigModel other)
    {
        other.Name = Name;
        IPv4Config.DeepCloneTo(other.IPv4Config);
        other.Remark = Remark;
    }

    #endregion IDeepCloneTo

    public bool PropertyEquals(IPConfigModel other)
    {
        bool iPv4ConfigEquals = IPv4Config.PropertyEquals(other.IPv4Config);

        if (iPv4ConfigEquals && Name == other.Name && Remark == other.Remark)
        {
            return true;
        }

        return false;
    }

    public override string ToString()
    {
        string? remark = Remark?.Length > 200
            ? String.Concat(Remark.AsSpan(0, 200), "…")
            : Remark;

        remark = remark?.Trim();

        return $"""
            {Name}

            {IPv4Config}

            {Lang.Remark}: {(String.IsNullOrEmpty(remark) ? Lang.None : remark)}
            """;
    }

    public new void ValidateAllProperties()
    {
        base.ValidateAllProperties();

        IPv4Config.ValidateAllProperties();
    }

    private void IPv4Config_ErrorsChanged(object? sender, DataErrorsChangedEventArgs e)
    {
        ValidateProperty(IPv4Config, nameof(IPv4Config));
    }

    private void IPv4PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        OnPropertyChanged(e.PropertyName);
    }

    private void IPv4PropertyChanging(object? sender, PropertyChangingEventArgs e)
    {
        OnPropertyChanging(e.PropertyName);
    }
}
