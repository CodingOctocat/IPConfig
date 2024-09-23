using System.ComponentModel;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

using CommunityToolkit.Mvvm.ComponentModel;

namespace IPConfig.Models;

public abstract partial class IPConfigBase<T> : ObservableValidator,
    IDeepCloneable<T>, IDeepCloneTo<T> where T : IPConfigBase<T>, new()
{
    protected bool _allowAutoDisableAutoDns;

    protected bool _allowAutoDisableDhcp;

    protected string _dns1 = "";

    protected string _dns2 = "";

    protected string _gateway = "";

    protected string _iP = "";

    protected bool _isAutoDns;

    protected bool _isDhcpEnabled;

    protected string _mask = "";

    [JsonPropertyOrder(5)]
    public virtual string Dns1
    {
        get => _dns1;
        set
        {
            if (TryNormalizeIPAddress(value, out string dns))
            {
                SetProperty(ref _dns1, dns, true);
            }
        }
    }

    [JsonPropertyOrder(6)]
    public virtual string Dns2
    {
        get => _dns2;
        set
        {
            if (TryNormalizeIPAddress(value, out string dns))
            {
                SetProperty(ref _dns2, dns, true);
            }
        }
    }

    [JsonPropertyOrder(3)]
    public virtual string Gateway
    {
        get => _gateway;
        set
        {
            if (TryNormalizeIPAddress(value, out string gateway))
            {
                SetProperty(ref _gateway, gateway, true);
            }
        }
    }

    [JsonPropertyOrder(1)]
    public virtual string IP
    {
        get => _iP;
        set
        {
            if (TryNormalizeIPAddress(value, out string ip))
            {
                SetProperty(ref _iP, ip, true);
            }
        }
    }

    [JsonPropertyOrder(4)]
    public virtual bool IsAutoDns
    {
        get => _isAutoDns;
        set
        {
            if (!IsDhcpEnabled)
            {
                value = false;
            }

            SetProperty(ref _isAutoDns, value);
        }
    }

    [JsonPropertyOrder(0)]
    public virtual bool IsDhcpEnabled
    {
        get => _isDhcpEnabled;
        set
        {
            SetProperty(ref _isDhcpEnabled, value);

            if (!_isDhcpEnabled)
            {
                IsAutoDns = false;
            }
        }
    }

    [JsonPropertyOrder(2)]
    public virtual string Mask
    {
        get => _mask;
        set
        {
            if (TryNormalizeIPAddress(value, out string mask))
            {
                SetProperty(ref _mask, mask, true);
            }
        }
    }

    public void AllowAutoDisableAutoDns(bool allow = true)
    {
        _allowAutoDisableAutoDns = allow;
    }

    public void AllowAutoDisableDhcp(bool allow = true)
    {
        _allowAutoDisableDhcp = allow;
    }

    public virtual T DeepClone()
    {
        T clone = new();
        DeepCloneTo(clone);

        return clone;
    }

    public virtual void DeepCloneTo(T other)
    {
        other.IP = IP;
        other.IsDhcpEnabled = IsDhcpEnabled;
        other.Mask = Mask;
        other.Gateway = Gateway;
        other.IsAutoDns = IsAutoDns;
        other.Dns1 = Dns1;
        other.Dns2 = Dns2;
    }

    public virtual void FormatProperties()
    {
        AllowAutoDisableDhcp(false);
        AllowAutoDisableAutoDns(false);

        InnerFormatProperties();
    }

    protected abstract void InnerFormatProperties();

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        if (_allowAutoDisableDhcp && e.PropertyName is nameof(IP) or nameof(Mask) or nameof(Gateway))
        {
            IsDhcpEnabled = false;
        }
        else if (_allowAutoDisableAutoDns && e.PropertyName is nameof(Dns1) or nameof(Dns2))
        {
            IsAutoDns = false;
        }
    }

    protected bool TryNormalizeIPAddress(string ip, out string normalizedIP)
    {
        normalizedIP = AutoPickupRegex().Replace(ip, ".");

        return PendingIPAddressRegex().IsMatch(normalizedIP);
    }

    [GeneratedRegex(@"[-`=\[\]\\;',/·【】、；‘’，。/]")]
    private static partial Regex AutoPickupRegex();

    [GeneratedRegex(@"^\d{0,3}\.?\d{0,3}\.?\d{0,3}\.?\d{0,3}$")]
    private static partial Regex PendingIPAddressRegex();
}
