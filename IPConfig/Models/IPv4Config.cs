using System.ComponentModel;
using System.Net;
using System.Text.Json.Serialization;

using IPConfig.Languages;
using IPConfig.Models.Validations;

namespace IPConfig.Models;

/// <summary>
/// 具有基本属性的 TCP/IPv4 配置。
/// </summary>
public class IPv4Config : IPConfigBase<IPv4Config>
{
    [JsonPropertyOrder(5)]
    [RequiredIf<bool>(nameof(IsAutoDns), false)]
    [IPValidation(LangKey.InvalidIPv4Dns)]
    public override string Dns1 { get => base.Dns1; set => base.Dns1 = value; }

    [JsonPropertyOrder(6)]
    [IPValidation(LangKey.InvalidIPv4Dns)]
    public override string Dns2 { get => base.Dns2; set => base.Dns2 = value; }

    [JsonPropertyOrder(3)]
    [IPValidation(LangKey.InvalidIPv4DefaultGatewayAddress)]
    public override string Gateway { get => base.Gateway; set => base.Gateway = value; }

    [JsonPropertyOrder(1)]
    [RequiredIf<bool>(nameof(IsDhcpEnabled), false)]
    [IPValidation(LangKey.InvalidIPv4Address)]
    public override string IP { get => base.IP; set => base.IP = value; }

    [JsonPropertyOrder(2)]
    [RequiredIf<bool>(nameof(IsDhcpEnabled), false)]
    [IPValidation(LangKey.InvalidIPv4SubnetMask)]
    public override string Mask { get => _mask; set => SetProperty(ref _mask, value.Trim(), true); }

    public new void ClearErrors(string? propertyName = null)
    {
        base.ClearErrors(propertyName);
    }

    public bool PropertyEquals(IPv4Config other)
    {
        if (IP == other.IP
            && IsDhcpEnabled == other.IsDhcpEnabled
            && Mask == other.Mask
            && Gateway == other.Gateway
            && IsAutoDns == other.IsAutoDns
            && Dns1 == other.Dns1
            && Dns2 == other.Dns2)
        {
            return true;
        }

        return false;
    }

    public override string ToString()
    {
        return $"""
            IPv4: {IP}
            {Lang.IPv4SubnetMask}: {Mask}
            {Lang.DefaultGateway}: {Gateway}
            {Lang.PreferredDns}: {Dns1}
            {Lang.AlternateDns}: {Dns2}
            """;
    }

    public new void ValidateAllProperties()
    {
        base.ValidateAllProperties();
    }

    protected override void InnerFormatProperties()
    {
        if (IPAddress.TryParse(IP, out var ip))
        {
            IP = ip.ToString();
        }

        if (IPAddress.TryParse(Mask, out var mask))
        {
            Mask = mask.ToString();
        }

        if (IPAddress.TryParse(Gateway, out var gateway))
        {
            Gateway = gateway.ToString();
        }

        if (IPAddress.TryParse(Dns1, out var dns1))
        {
            Dns1 = dns1.ToString();
        }

        if (IPAddress.TryParse(Dns2, out var dns2))
        {
            Dns2 = dns2.ToString();
        }
    }

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        // 修复数据验证不会及时更新的问题。
        if (e.PropertyName is nameof(IP) or nameof(IsDhcpEnabled))
        {
            ClearErrors(nameof(IP));
            ValidateProperty(IP, nameof(IP));
        }
        else if (e.PropertyName is nameof(Mask) or nameof(IsDhcpEnabled))
        {
            ClearErrors(nameof(Mask));
            ValidateProperty(Mask, nameof(Mask));
        }
        else if (e.PropertyName is nameof(Dns1) or nameof(IsAutoDns))
        {
            ClearErrors(nameof(Dns1));
            ValidateProperty(Dns1, nameof(Dns1));
        }
    }
}
