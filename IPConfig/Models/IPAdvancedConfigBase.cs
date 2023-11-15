using System.Collections.Immutable;
using System.Net.NetworkInformation;

using IPConfig.Extensions;
using IPConfig.Languages;

namespace IPConfig.Models;

/// <summary>
/// 提供 TCP/IPv4 和 TCP/IPv6 高级设置的公共属性部分。
/// </summary>
public abstract record IPAdvancedConfigBase
{
    #region TCP/IPv4 和 TCP/IPv6 常规属性

    public ImmutableList<string> AlternateDnsCollection { get; init; } = [];

    public ImmutableList<string> AlternateGatewayCollection { get; init; } = [];

    public ImmutableList<string> AlternateIPCollection { get; init; } = [];

    public required string DhcpLeaseLifetime { get; init; }

    public ImmutableList<string> DhcpLeaseLifetimeCollection { get; init; } = [];

    public required string PreferredDns { get; init; }

    public required string PreferredGateway { get; init; }

    public required string PreferredIP { get; init; }

    public required string PreferredLifetime { get; init; }

    public ImmutableList<string> PreferredLifetimeCollection { get; init; } = [];

    public required string ValidLifetime { get; init; }

    public ImmutableList<string> ValidLifetimeCollection { get; init; } = [];

    #endregion TCP/IPv4 和 TCP/IPv6 常规属性

    #region TCP/IPv4 和 TCP/IPv6 高级信息

    public required DuplicateAddressDetectionState DuplicateAddressDetectionState { get; init; }

    public ImmutableList<DuplicateAddressDetectionState> DuplicateAddressDetectionStateCollcetion { get; init; } = [];

    public required bool IsDnsEligible { get; init; }

    public ImmutableList<bool> IsDnsEligibleCollcetion { get; init; } = [];

    public required bool IsTransient { get; init; }

    public ImmutableList<bool> IsTransientCollection { get; init; } = [];

    #endregion TCP/IPv4 和 TCP/IPv6 高级信息

    public abstract string FormatGeneralProperties();

    public virtual string FormatLifetimes()
    {
        return $"""
            {Lang.ValidLifetime}: {ValidLifetime}{ValidLifetimeCollection.ToStringWithLeftAlignment(Lang.ValidLifetime)}
            {Lang.PreferredLifetime}: {PreferredLifetime}{PreferredLifetimeCollection.ToStringWithLeftAlignment(Lang.PreferredLifetime)}
            {Lang.DhcpLeaseLifetime}: {DhcpLeaseLifetime}{DhcpLeaseLifetimeCollection.ToStringWithLeftAlignment(Lang.DhcpLeaseLifetime)}
            """;
    }
}
