using System;
using System.Collections.Immutable;
using System.Linq;
using System.Management;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

using IPConfig.Languages;
using IPConfig.Models;

using Microsoft.Win32;

namespace IPConfig.Helpers;

/// <summary>
/// 管理网络配置信息的首选项。
/// </summary>
public static class NetworkManagement
{
    public static NetworkInterface? GetActiveNetworkInterface()
    {
        var networks = NetworkInterface.GetAllNetworkInterfaces();

        var activeAdapter = networks.FirstOrDefault(x => x.NetworkInterfaceType != NetworkInterfaceType.Loopback
                            && x.NetworkInterfaceType != NetworkInterfaceType.Tunnel
                            && x.OperationalStatus == OperationalStatus.Up
                            && x.Name.StartsWith("vEthernet") == false);

        return activeAdapter;
    }

    public static IPAddress? GetIPAddress(string nicId)
    {
        foreach (var adapter in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (adapter.Id == nicId)
            {
                foreach (var unicastIPAddressInformation in adapter.GetIPProperties().UnicastAddresses)
                {
                    if (unicastIPAddressInformation.Address.AddressFamily == AddressFamily.InterNetwork)
                    {
                        return unicastIPAddressInformation.Address;
                    }
                }
            }
        }

        return null;
    }

    public static IPInterfaceProperties? GetIPInterfaceProperties(string nicId)
    {
        foreach (var adapter in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (adapter.Id == nicId)
            {
                return adapter.GetIPProperties();
            }
        }

        return null;
    }

    public static IPv4AdvancedConfig GetIPv4AdvancedConfig(NetworkInterface nic)
    {
        var config = GetIPAdvancedConfig(nic, AddressFamily.InterNetwork);

        return (IPv4AdvancedConfig)config;
    }

    public static IPv4Config GetIPv4Config(NetworkInterface nic)
    {
        var props = nic.GetIPProperties();

        var info = props.UnicastAddresses
            .FirstOrDefault(x => x.Address.AddressFamily == AddressFamily.InterNetwork);

        bool isDhcpEnabled = IsDhcpEnabled(nic.Id);

        string ip = info?.Address.ToString() ?? "";
        string mask = info?.IPv4Mask.ToString() ?? "";
        string gateway = props.GatewayAddresses.FirstOrDefault()?.Address.ToString() ?? "";

        bool isAutoDns = IsAutoDns(nic.Id);

        string dns1 = "";
        string dns2 = "";

        if (!isAutoDns)
        {
            if (props.DnsAddresses is [var pref, var alt, ..])
            {
                dns1 = pref.ToString();
                dns2 = alt.ToString();
            }
            else if (props.DnsAddresses is [var p])
            {
                dns1 = p.ToString();
            }
        }

        var cfg = new IPv4Config() {
            IsDhcpEnabled = isDhcpEnabled,
            IP = ip,
            Mask = mask,
            Gateway = gateway,
            IsAutoDns = isAutoDns,
            Dns1 = dns1,
            Dns2 = dns2
        };

        return cfg;
    }

    public static IPv6AdvancedConfig GetIPv6AdvancedConfig(NetworkInterface nic)
    {
        var config = GetIPAdvancedConfig(nic, AddressFamily.InterNetworkV6);

        return (IPv6AdvancedConfig)config;
    }

    public static bool IsAutoDns(string nicId)
    {
        string path = "HKEY_LOCAL_MACHINE\\SYSTEM\\CurrentControlSet\\Services\\Tcpip\\Parameters\\Interfaces\\" + nicId;
        string? ns = Registry.GetValue(path, "NameServer", null) as string;

        // Wi-Fi 可能使用配置文件而不是网卡配置，需要优先判断。
        if (Registry.GetValue(path, "ProfileNameServer", null) is not string pns)
        {
            return String.IsNullOrEmpty(ns);
        }

        return String.IsNullOrEmpty(pns);
    }

    public static bool IsDhcpEnabled(string nicId)
    {
        string path = "HKEY_LOCAL_MACHINE\\SYSTEM\\CurrentControlSet\\Services\\Tcpip\\Parameters\\Interfaces\\" + nicId;
        object? dhcp = Registry.GetValue(path, "EnableDHCP", 0);

        return dhcp is 1;
    }

    public static bool IsPhysicalAdapter(string nicId)
    {
        using var searcher = new ManagementObjectSearcher(@"root\CIMV2",
            @$"SELECT * FROM  Win32_NetworkAdapter WHERE GUID='{nicId}' AND NOT PNPDeviceID LIKE 'ROOT\\%'");

        var managementObject = searcher.Get().OfType<ManagementObject>().FirstOrDefault();
        bool isPhysical = Convert.ToBoolean(managementObject?.Properties["PhysicalAdapter"].Value);

        return isPhysical;
    }

    public static void SetIPv4(string nicId, string ipAddress, string subnetMask, string gateway)
    {
        using var networkConfigMng = new ManagementClass("Win32_NetworkAdapterConfiguration");
        using var networkConfigs = networkConfigMng.GetInstances();

        using var managementObject = networkConfigs
            .Cast<ManagementObject>()
            .FirstOrDefault(objMO => (bool)objMO["IPEnabled"] && objMO["SettingID"].Equals(nicId));

        if (managementObject is null)
        {
            return;
        }

        using var newIP = managementObject.GetMethodParameters("EnableStatic");

        if ((!String.IsNullOrEmpty(ipAddress)) || (!String.IsNullOrEmpty(subnetMask)))
        {
            if (!String.IsNullOrEmpty(ipAddress))
            {
                ConcatManagementBaseObjectValues(newIP, "IPAddress", new[] { ipAddress });
            }

            if (!String.IsNullOrEmpty(subnetMask))
            {
                ConcatManagementBaseObjectValues(newIP, "SubnetMask", new[] { subnetMask });
            }

            managementObject.InvokeMethod("EnableStatic", newIP, null!);
        }

        if (!String.IsNullOrEmpty(gateway))
        {
            using var newGateway = managementObject.GetMethodParameters("SetGateways");

            ConcatManagementBaseObjectValues(newGateway, "DefaultIPGateway", new[] { gateway });
            ConcatManagementBaseObjectValues(newGateway, "GatewayCostMetric", new[] { 1 });
            managementObject.InvokeMethod("SetGateways", newGateway, null!);
        }
    }

    public static void SetIPv4Dhcp(string nicId)
    {
        using var networkConfigMng = new ManagementClass("Win32_NetworkAdapterConfiguration");
        using var networkConfigs = networkConfigMng.GetInstances();

        using var managementObject = networkConfigs
            .Cast<ManagementObject>()
            .FirstOrDefault(objMO => (bool)objMO["IPEnabled"] && objMO["SettingID"].Equals(nicId));

        if (managementObject is null)
        {
            return;
        }

        managementObject.InvokeMethod("SetDNSServerSearchOrder", null!);
        managementObject.InvokeMethod("EnableDHCP", null!);
    }

    public static void SetIPv4Dns(string nicId, string dns1, string dns2 = "")
    {
        using var networkConfigMng = new ManagementClass("Win32_NetworkAdapterConfiguration");
        using var networkConfigs = networkConfigMng.GetInstances();

        using var managementObject = networkConfigs
            .Cast<ManagementObject>()
            .FirstOrDefault(objMO => (bool)objMO["IPEnabled"] && objMO["SettingID"].Equals(nicId));

        if (managementObject is null)
        {
            return;
        }

        SetManagementObjectArrayValue(managementObject,
            "SetDNSServerSearchOrder",
            "DNSServerSearchOrder",
            new[] { dns1, dns2 });
    }

    public static void SetIPv4DnsAuto(string nicId)
    {
        using var networkConfigMng = new ManagementClass("Win32_NetworkAdapterConfiguration");
        using var networkConfigs = networkConfigMng.GetInstances();

        using var managementObject = networkConfigs
            .Cast<ManagementObject>()
            .FirstOrDefault(objMO => (bool)objMO["IPEnabled"] && objMO["SettingID"].Equals(nicId));

        if (managementObject is null)
        {
            return;
        }

        using var dnsObj = managementObject.GetMethodParameters("SetDNSServerSearchOrder");

        if (dnsObj is null)
        {
            return;
        }

        dnsObj["DNSServerSearchOrder"] = null!;
        managementObject.InvokeMethod("SetDNSServerSearchOrder", dnsObj, null!);
    }

    private static void ConcatManagementBaseObjectValues<T>(ManagementBaseObject mbo, string propertyName, T[] newValues)
    {
        if (mbo[propertyName] is T[] oldValues)
        {
            mbo[propertyName] = newValues.Concat(oldValues.Skip(newValues.Length)).ToArray();
        }
        else
        {
            mbo[propertyName] = newValues;
        }
    }

    private static IPAdvancedConfigBase GetIPAdvancedConfig(NetworkInterface nic, AddressFamily addressFamily)
    {
        var props = nic.GetIPProperties();

        var info = props.UnicastAddresses
            .Where(x => x.Address.AddressFamily == addressFamily);

        var iPCollection = info.Select(x => x.Address.ToString());
        var prefixLenghtCollection = info.Select(x => x.PrefixLength);

        var prefixOriginCollection = info.Select(x => x.PrefixOrigin);
        var suffixOriginCollection = info.Select(x => x.SuffixOrigin);

        var validLifetimeCollection = info.Select(x => {
            var when = (DateTime.UtcNow + TimeSpan.FromSeconds(x.AddressValidLifetime)).ToLocalTime();

            return when.ToString(LangSource.Instance.CurrentCulture);
        });

        var prefLifetimeCollection = info.Select(x => {
            var when = (DateTime.UtcNow + TimeSpan.FromSeconds(x.AddressPreferredLifetime)).ToLocalTime();

            return when.ToString(LangSource.Instance.CurrentCulture);
        });

        var dhcpLeaseLifetimeCollection = info.Select(x => {
            var when = (DateTime.UtcNow + TimeSpan.FromSeconds(x.DhcpLeaseLifetime)).ToLocalTime();

            return when.ToString(LangSource.Instance.CurrentCulture);
        });

        var isTransientCollection = info.Select(x => x.IsTransient);
        var dadsCollection = info.Select(x => x.DuplicateAddressDetectionState);
        var isDnsEligibleCollcetion = info.Select(x => x.IsDnsEligible);

        if (addressFamily == AddressFamily.InterNetwork)
        {
            bool isDhcpEnabled = IsDhcpEnabled(nic.Id);
            var maskCollection = info.Select(x => x.IPv4Mask.ToString());

            var gatewayCollection = props.GatewayAddresses
                .Where(x => x.Address.AddressFamily == AddressFamily.InterNetwork)
                .Select(x => x.Address.ToString());

            bool isAutoDns = IsAutoDns(nic.Id);

            var dnsCollection = props.DnsAddresses
                .Where(x => x.AddressFamily == AddressFamily.InterNetwork)
                .Select(x => x.ToString());

            var winsCollection = nic.GetIPProperties().WinsServersAddresses.Select(x => x.ToString());

            var iPv4AdvancedConfig = new IPv4AdvancedConfig() {
                IsDhcpEnabled = isDhcpEnabled,
                PreferredIP = iPCollection.FirstOrDefault(""),
                AlternateIPCollection = iPCollection.Skip(1).ToImmutableList(),
                PreferredMask = maskCollection.FirstOrDefault(""),
                AlternateMaskCollection = maskCollection.Skip(1).ToImmutableList(),
                PreferredGateway = gatewayCollection.FirstOrDefault(""),
                AlternateGatewayCollection = gatewayCollection.Skip(1).ToImmutableList(),
                IsAutoDns = isAutoDns,
                PreferredDns = dnsCollection.FirstOrDefault(""),
                AlternateDnsCollection = dnsCollection.Skip(1).ToImmutableList(),
                WinsServerAddress = winsCollection.FirstOrDefault(""),
                WinsServerAddressCollection = winsCollection.Skip(1).ToImmutableList(),
                ValidLifetime = validLifetimeCollection.FirstOrDefault(""),
                ValidLifetimeCollection = validLifetimeCollection.Skip(1).ToImmutableList(),
                PreferredLifetime = prefLifetimeCollection.FirstOrDefault(""),
                PreferredLifetimeCollection = prefLifetimeCollection.Skip(1).ToImmutableList(),
                DhcpLeaseLifetime = dhcpLeaseLifetimeCollection.FirstOrDefault(""),
                DhcpLeaseLifetimeCollection = dhcpLeaseLifetimeCollection.Skip(1).ToImmutableList(),
                IsTransient = isTransientCollection.FirstOrDefault(),
                IsTransientCollection = isTransientCollection.Skip(1).ToImmutableList(),
                IsDnsEligible = isDnsEligibleCollcetion.FirstOrDefault(),
                IsDnsEligibleCollcetion = isDnsEligibleCollcetion.Skip(1).ToImmutableList(),
                DuplicateAddressDetectionState = dadsCollection.FirstOrDefault(),
                DuplicateAddressDetectionStateCollcetion = dadsCollection.Skip(1).ToImmutableList()
            };

            return iPv4AdvancedConfig;
        }
        else if (addressFamily == AddressFamily.InterNetworkV6)
        {
            var gatewayCollection = props.GatewayAddresses
                .Where(x => x.Address.AddressFamily == AddressFamily.InterNetworkV6)
                .Select(x => x.Address.ToString());

            var dnsCollection = props.DnsAddresses
                .Where(x => x.AddressFamily == AddressFamily.InterNetworkV6)
                .Select(x => x.ToString());

            var config = new IPv6AdvancedConfig() {
                PreferredIP = iPCollection.FirstOrDefault(""),
                AlternateIPCollection = iPCollection.Skip(1).ToImmutableList(),
                PreferredPrefixLength = prefixLenghtCollection.FirstOrDefault(),
                AlternatePrefixLengthCollection = prefixLenghtCollection.Skip(1).ToImmutableList(),
                PreferredGateway = gatewayCollection.FirstOrDefault(""),
                AlternateGatewayCollection = gatewayCollection.Skip(1).ToImmutableList(),
                PrefixOrigin = prefixOriginCollection.FirstOrDefault(),
                PrefixOriginCollection = prefixOriginCollection.Skip(1).ToImmutableList(),
                SuffixOrigin = suffixOriginCollection.FirstOrDefault(),
                SuffixOriginCollection = suffixOriginCollection.Skip(1).ToImmutableList(),
                PreferredDns = dnsCollection.FirstOrDefault(""),
                AlternateDnsCollection = dnsCollection.Skip(1).ToImmutableList(),
                ValidLifetime = validLifetimeCollection.FirstOrDefault(""),
                ValidLifetimeCollection = validLifetimeCollection.Skip(1).ToImmutableList(),
                PreferredLifetime = prefLifetimeCollection.FirstOrDefault(""),
                PreferredLifetimeCollection = prefLifetimeCollection.Skip(1).ToImmutableList(),
                DhcpLeaseLifetime = dhcpLeaseLifetimeCollection.FirstOrDefault(""),
                DhcpLeaseLifetimeCollection = dhcpLeaseLifetimeCollection.Skip(1).ToImmutableList(),
                IsTransient = isTransientCollection.FirstOrDefault(),
                IsTransientCollection = isTransientCollection.Skip(1).ToImmutableList(),
                IsDnsEligible = isDnsEligibleCollcetion.FirstOrDefault(),
                IsDnsEligibleCollcetion = isDnsEligibleCollcetion.Skip(1).ToImmutableList(),
                DuplicateAddressDetectionState = dadsCollection.FirstOrDefault(),
                DuplicateAddressDetectionStateCollcetion = dadsCollection.Skip(1).ToImmutableList()
            };

            return config;
        }

        throw new ArgumentOutOfRangeException(nameof(addressFamily), addressFamily, $"Supports {AddressFamily.InterNetwork} and {AddressFamily.InterNetworkV6} only.");
    }

    private static void SetManagementObjectArrayValue<T>(ManagementObject mo, string methodName, string propertyName, T[] newValues)
    {
        using var mbo = mo.GetMethodParameters(methodName);

        if (mbo is null)
        {
            return;
        }

        var oldValues = mbo[propertyName] as T[] ?? Array.Empty<T>();
        mbo[propertyName] = newValues.Concat(oldValues.Skip(newValues.Length)).ToArray();
        mo.InvokeMethod(methodName, mbo, null!);
    }
}
