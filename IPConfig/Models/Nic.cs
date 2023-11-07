using System;
using System.Net.NetworkInformation;

using CommunityToolkit.Mvvm.ComponentModel;

using IPConfig.Helpers;
using IPConfig.Languages;

namespace IPConfig.Models;

[INotifyPropertyChanged]
public partial class Nic : NetworkInterface
{
    public override string Description => Instance.Description;

    public string FormatedMacAddress
    {
        get
        {
            if (String.IsNullOrEmpty(MacAddress))
            {
                return "N/A";
            }

            return BitConverter.ToString(Instance.GetPhysicalAddress().GetAddressBytes());
        }
    }

    public string FormatedSpeed => BytesFormatter.ToNetBand(Speed);

    public override string Id => Instance.Id;

    public NetworkInterface Instance { get; }

    public IPv4InterfaceProperties? IPv4InterfaceProperties
    {
        get
        {
            if (SupportsIPv4)
            {
                return Instance.GetIPProperties().GetIPv4Properties();
            }

            return null;
        }
    }

    public IPv6InterfaceProperties? IPv6InterfaceProperties
    {
        get
        {
            if (SupportsIPv6)
            {
                return Instance.GetIPProperties().GetIPv6Properties();
            }

            return null;
        }
    }

    public override bool IsReceiveOnly => Instance.IsReceiveOnly;

    public string MacAddress => Instance.GetPhysicalAddress().ToString();

    public override string Name => Instance.Name;

    public override NetworkInterfaceType NetworkInterfaceType
    {
        get
        {
            // HACK: 修复 Clash 虚拟网卡返回无效枚举值(53) 的问题。
            if (((int)Instance.NetworkInterfaceType) == 53)
            {
                return NetworkInterfaceType.Tunnel;
            }

            return Instance.NetworkInterfaceType;
        }
    }

    public override OperationalStatus OperationalStatus => Instance.OperationalStatus;

    public SimpleNicType SimpleNicType
    {
        get
        {
            string name = Name.ToLower();
            string description = Description.ToLower();

            if (name == "wlan" || description.Contains("wireless"))
            {
                return SimpleNicType.Wlan;
            }

            if (name.Contains("vmware") || name.Contains("hyper-v") || name.Contains("vethernet")
                || description.Contains("vmware") || description.Contains("virtual")
                || description.Contains("tap") || description.Contains("tun"))
            {
                return SimpleNicType.Vm;
            }

            if (name.Contains(Lang.Ethernet_Lower) || name.Contains("ethernet") || description.Contains("ethernet"))
            {
                return SimpleNicType.Ethernet;
            }
            else
            {
                return SimpleNicType.Other;
            }
        }
    }

    public override long Speed => Instance.Speed;

    public bool SupportsIPv4 => Supports(NetworkInterfaceComponent.IPv4);

    public bool SupportsIPv6 => Supports(NetworkInterfaceComponent.IPv6);

    public override bool SupportsMulticast => Instance.SupportsMulticast;

    public Nic(NetworkInterface nic)
    {
        Instance = nic;

        LangSource.Instance.LanguageChanged += (s, e) => OnPropertyChanged(nameof(OperationalStatus));
    }

    public override IPInterfaceProperties GetIPProperties()
    {
        return Instance.GetIPProperties();
    }

    public override IPInterfaceStatistics GetIPStatistics()
    {
        return Instance.GetIPStatistics();
    }

    public override IPv4InterfaceStatistics GetIPv4Statistics()
    {
        return Instance.GetIPv4Statistics();
    }

    public override PhysicalAddress GetPhysicalAddress()
    {
        return Instance.GetPhysicalAddress();
    }

    public override bool Supports(NetworkInterfaceComponent networkInterfaceComponent)
    {
        return Instance.Supports(networkInterfaceComponent);
    }

    public override string ToString()
    {
        return $"""
            {Lang.AdapterName}: {Name}
            {Lang.AdapterDescription}: {Description}
            {Lang.AdapterMAC}: {FormatedMacAddress}
            {Lang.AdapterType}: {NetworkInterfaceType}
            {Lang.AdapterOperationalStatus}: {OperationalStatus}
            {Lang.AdapterLinkSpeed}: {FormatedSpeed}
            {Lang.AdapterSupportsMulticast}: {SupportsMulticast}
            {Lang.AdapterId}: {Id}
            {Lang.AdapterIsReceiveOnly}: {IsReceiveOnly}
            {Lang.AdapterSupportsIPv4}: {SupportsIPv4}
            {Lang.AdapterSupportsIPv6}: {SupportsIPv6}
            """;
    }
}
