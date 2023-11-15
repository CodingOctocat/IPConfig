using System.Collections.Immutable;
using System.Net.NetworkInformation;

using IPConfig.Extensions;
using IPConfig.Languages;

namespace IPConfig.Models;

/// <summary>
/// 高级 TCP/IPv6 设置属性。
/// </summary>
public record IPv6AdvancedConfig : IPAdvancedConfigBase
{
    #region TCP/IPv6 常规属性

    public virtual ImmutableList<int> AlternatePrefixLengthCollection { get; init; } = [];

    public virtual int PreferredPrefixLength { get; init; }

    public virtual PrefixOrigin PrefixOrigin { get; init; }

    public virtual ImmutableList<PrefixOrigin> PrefixOriginCollection { get; init; } = [];

    public virtual SuffixOrigin SuffixOrigin { get; init; }

    public virtual ImmutableList<SuffixOrigin> SuffixOriginCollection { get; init; } = [];

    #endregion TCP/IPv6 常规属性

    public override string FormatGeneralProperties()
    {
        return $"""
            IP: {PreferredIP}{AlternateIPCollection.ToStringWithLeftAlignment("IP")}
            {Lang.IPv6PrefixLength}: {PreferredPrefixLength}{AlternatePrefixLengthCollection.ToStringWithLeftAlignment(Lang.IPv6PrefixLength)}
            {Lang.IPv6PrefixOrigin}: {PrefixOrigin}{PrefixOriginCollection.ToStringWithLeftAlignment(Lang.IPv6PrefixOrigin)}
            {Lang.IPv6SuffixOrigin}: {SuffixOrigin}{SuffixOriginCollection.ToStringWithLeftAlignment(Lang.IPv6SuffixOrigin)}
            {Lang.DefaultGateway}: {PreferredGateway}{AlternateGatewayCollection.ToStringWithLeftAlignment(Lang.DefaultGateway)}
            DNS: {PreferredDns}{AlternateDnsCollection.ToStringWithLeftAlignment("DNS")}
            """;
    }

    public override string ToString()
    {
        return $"""
            [{Lang.IPv6GeneralProperties_Header}]
            {FormatGeneralProperties()}

            [{Lang.IPv6Lifetimes_Header}]
            {FormatLifetimes()}
            """;
    }
}
