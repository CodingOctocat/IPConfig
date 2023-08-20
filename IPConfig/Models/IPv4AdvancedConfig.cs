using System.Collections.Immutable;
using System.Linq;

using IPConfig.Extensions;
using IPConfig.Languages;

namespace IPConfig.Models;

/// <summary>
/// 高级 TCP/IPv4 设置属性。
/// </summary>
public record IPv4AdvancedConfig : IPAdvancedConfigBase
{
    #region TCP/IPv4 常规属性

    public ImmutableList<string> AlternateMaskCollection { get; init; } = ImmutableList.Create<string>();

    public required bool IsAutoDns { get; init; }

    public required bool IsDhcpEnabled { get; init; }

    public required string PreferredMask { get; init; }

    public required string WinsServerAddress { get; init; }

    public ImmutableList<string> WinsServerAddressCollection { get; init; } = ImmutableList.Create<string>();

    #endregion TCP/IPv4 常规属性

    /// <summary>
    /// 转换为 <seealso cref="IPv4Config"/>。
    /// </summary>
    /// <returns></returns>
    public IPv4Config ToIPv4Config()
    {
        return new IPv4Config() {
            IsDhcpEnabled = IsDhcpEnabled,
            IP = PreferredIP,
            Mask = PreferredMask,
            Gateway = PreferredGateway,
            IsAutoDns = IsAutoDns,
            Dns1 = PreferredDns,
            Dns2 = AlternateDnsCollection.ElementAtOrDefault(1) ?? ""
        };
    }

    public override string FormatGeneralProperties()
    {
        return $"""
            {Lang.IPv4IsDhcpEnabled}: {IsDhcpEnabled}
            IP: {PreferredIP}{AlternateIPCollection.ToStringWithLeftAlignment("IP")}
            {Lang.IPv4SubnetMask}: {PreferredMask}{AlternateMaskCollection.ToStringWithLeftAlignment(Lang.IPv4SubnetMask)}
            {Lang.DefaultGateway}: {PreferredGateway}{AlternateGatewayCollection.ToStringWithLeftAlignment(Lang.DefaultGateway)}
            {Lang.IPv4IsAutoDns}: {IsAutoDns}
            DNS: {PreferredDns}{AlternateDnsCollection.ToStringWithLeftAlignment("DNS")}
            """;
    }

    public override string ToString()
    {
        return $"""
            [{Lang.IPv4GeneralProperties_Header}]
            {FormatGeneralProperties()}

            [{Lang.IPv4Lifetimes_Header}]
            {FormatLifetimes()}
            """;
    }
}
