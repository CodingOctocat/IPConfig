using System;
using System.IO;
using System.Net.NetworkInformation;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Timers;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;

using HandyControl.Controls;

using IPConfig.Extensions;
using IPConfig.Helpers;
using IPConfig.Languages;
using IPConfig.Models;
using IPConfig.Models.Messages;

using Microsoft.Win32;

namespace IPConfig.ViewModels;

public partial class NicConfigDetailViewModel : ObservableRecipient,
    IRecipient<PropertyChangedMessage<Nic>>
{
    #region Fields

    private readonly Timer _updateConfigTimer = new(1000);

    #endregion Fields

    #region Observable Properties

    [ObservableProperty]
    private bool _canUpdate;

    [ObservableProperty]
    private IPv4AdvancedConfig _iPv4AdvancedCofnig = null!;

    [ObservableProperty]
    private IPv4InterfaceStatistics _iPv4InterfaceStatistics = null!;

    [ObservableProperty]
    private IPv6AdvancedConfig _iPv6AdvancedCofnig = null!;

    [ObservableProperty]
    private Nic _nic = null!;

    #endregion Observable Properties

    #region Constructors & Recipients

    public NicConfigDetailViewModel()
    {
        IsActive = true;
    }

    public void Receive(PropertyChangedMessage<Nic> message)
    {
        Nic = message.NewValue;
    }

    #endregion Constructors & Recipients

    #region Relay Commands

    [RelayCommand]
    private async Task CopyAllContentsAsTextAsync()
    {
        string iPv4Header = Nic.SupportsIPv4 ? Lang.TcpIPv4_Header : $"{Lang.AdapterNotSupported} {Lang.TcpIPv4_Header}";
        string iPv6Header = Nic.SupportsIPv6 ? Lang.TcpIPv6_Header : $"{Lang.AdapterNotSupported} {Lang.TcpIPv6_Header}";

        string text = $"""
            {Nic.Name}

            [{Lang.AdapterProperties_Header}]
            {Nic}

            [{iPv4Header}]
            [{Lang.IPv4GeneralProperties_Header}]
            {IPv4AdvancedCofnig.FormatGeneralProperties()}

            [{Lang.IPv4Lifetimes_Header}]
            {IPv4AdvancedCofnig.FormatLifetimes()}

            [{Lang.IPv4Statistics_Header}]
            {FormatIPv4InterfaceStatistics()}

            [{iPv6Header}]
            [{Lang.IPv6GeneralProperties_Header}]
            {IPv6AdvancedCofnig.FormatGeneralProperties()}

            [{Lang.IPv6Lifetimes_Header}]
            {IPv6AdvancedCofnig.FormatLifetimes()}

            [{Lang.IPv6AdvancedProperties_Header}]
            {FormatIPv6InterfaceProperties()}

            [{Lang.IPv6Statistics_Header}]
            {Lang.NotSupported}
            """;

        await ClipboardHelper.SetTextAsync(text);
    }

    [RelayCommand]
    private async Task CopyContentsAsTextAsync(string tag)
    {
        if (tag == Lang.AdapterProperties_Header)
        {
            await ClipboardHelper.SetTextAsync(Nic.ToString());
        }
        else if (tag == Lang.TcpIPv4_Header)
        {
            await ClipboardHelper.SetTextAsync(GetIPv4PropertyContents());
        }
        else if (tag == Lang.IPv4GeneralProperties_Header)
        {
            await ClipboardHelper.SetTextAsync(IPv4AdvancedCofnig.FormatGeneralProperties());
        }
        else if (tag == Lang.IPv4Lifetimes_Header)
        {
            await ClipboardHelper.SetTextAsync(IPv4AdvancedCofnig.FormatLifetimes());
        }
        else if (tag == Lang.IPv4AdvancedProperties_Header)
        {
            await ClipboardHelper.SetTextAsync(FormatIPv4InterfaceProperties());
        }
        else if (tag == Lang.IPv4Statistics_Header)
        {
            await ClipboardHelper.SetTextAsync(FormatIPv4InterfaceStatistics());
        }
        else if (tag == Lang.TcpIPv6_Header)
        {
            await ClipboardHelper.SetTextAsync(GetIPv6PropertyContents());
        }
        else if (tag == Lang.IPv6GeneralProperties_Header)
        {
            await ClipboardHelper.SetTextAsync(IPv6AdvancedCofnig.FormatGeneralProperties());
        }
        else if (tag == Lang.IPv6Lifetimes_Header)
        {
            await ClipboardHelper.SetTextAsync(IPv6AdvancedCofnig.FormatLifetimes());
        }
        else if (tag == Lang.IPv6AdvancedProperties_Header)
        {
            await ClipboardHelper.SetTextAsync(FormatIPv6InterfaceProperties());
        }
        else if (tag == Lang.IPv6Statistics_Header)
        {
            // Not supported.
        }
    }

    [RelayCommand]
    private async Task ExportToCsvAsync()
    {
        string contents = $"""
            [{Lang.AdapterProperties_Header}]
            {GetIPv4PropertyContents()}

            {GetIPv6PropertyContents()}
            """;

        var regex = CsvRegex();
        var sb = new StringBuilder();

        foreach (string line in contents.Split(Environment.NewLine))
        {
            string kv = regex.Replace(line, ",", 1);
            sb.AppendLine(kv);
        }

        string csv = sb.ToString();

        var dialog = new SaveFileDialog() {
            Title = Lang.ExportToCsv_FileDialogTitle,
            Filter = Lang.ExportToCsv_FileDialogFilter
        };

        if (dialog.ShowDialog().GetValueOrDefault())
        {
            string filename = dialog.FileName;

            try
            {
                await File.WriteAllTextAsync(filename, csv, Encoding.UTF8);

                Growl.Success(new() {
                    Message = $"{Lang.ExportSuccessful}\n{filename}",
                    WaitTime = 2
                });
            }
            catch (IOException ex)
            {
                Growl.Error($"{Lang.ExportFailed}\n{ex.Message}");
            }
        }
    }

    [RelayCommand]
    private async Task ExportToPlaintextAsync()
    {
        string contents = $"""
            [{Lang.AdapterProperties_Header}]
            {GetIPv4PropertyContents()}

            {GetIPv6PropertyContents()}
            """;

        var dialog = new SaveFileDialog() {
            Title = Lang.ExportToPlaintext_FileDialogTitle,
            Filter = Lang.ExportToPlaintext_FileDialogFilter
        };

        if (dialog.ShowDialog().GetValueOrDefault())
        {
            string filename = dialog.FileName;

            try
            {
                await File.WriteAllTextAsync(filename, contents, Encoding.UTF8);

                Growl.Success(new() {
                    Message = $"{Lang.ExportSuccessful}\n{filename}",
                    WaitTime = 2
                });
            }
            catch (IOException ex)
            {
                Growl.Error($"{Lang.ExportFailed}\n{ex.Message}");
            }
        }
    }

    [RelayCommand]
    private void GoBack()
    {
        Messenger.Send<GoBackMessage>(new(this));
    }

    [RelayCommand]
    private void Loaded()
    {
        Nic = Messenger.Send<RequestMessage<Nic>, string>("DeferredLoadingForNicConfigDetailView").Response;
    }

    #endregion Relay Commands

    #region Partial OnPropertyChanged Methods

    partial void OnCanUpdateChanged(bool value)
    {
        if (value)
        {
            GetIPAdvancedConfig();

            _updateConfigTimer.Elapsed += UpdateConfigTimer_Elapsed;
            _updateConfigTimer.Start();
        }
        else
        {
            _updateConfigTimer.Stop();
            _updateConfigTimer.Elapsed -= UpdateConfigTimer_Elapsed;
        }
    }

    partial void OnNicChanged(Nic value)
    {
        GetIPAdvancedConfig();
    }

    #endregion Partial OnPropertyChanged Methods

    #region Event Handlers

    private void UpdateConfigTimer_Elapsed(object? sender, ElapsedEventArgs e)
    {
        GetIPAdvancedConfig();
    }

    #endregion Event Handlers

    #region Private Methods

    [GeneratedRegex(@":\s+|^\s+")]
    private static partial Regex CsvRegex();

    private string FormatIPv4InterfaceProperties()
    {
        var p = Nic.IPv4InterfaceProperties;

        return $"""
            {Lang.Index}: {p?.Index}
            {Lang.MTU}: {p?.Mtu}
            {Lang.APIPAActive}: {p?.IsAutomaticPrivateAddressingActive}
            {Lang.APIPAEnabled}: {p?.IsAutomaticPrivateAddressingEnabled}
            {Lang.ForwardingEnabled}: {p?.IsForwardingEnabled}
            {Lang.UsesWINS}: {p?.UsesWins}
            {Lang.IPv4WinsServer}: {IPv4AdvancedCofnig.WinsServerAddress}{IPv4AdvancedCofnig.WinsServerAddressCollection.ToStringWithLeftAlignment(Lang.IPv4WinsServer)}
            {Lang.IsTransient}: {IPv4AdvancedCofnig.IsTransient}{IPv4AdvancedCofnig.IsTransientCollection.ToStringWithLeftAlignment(Lang.IsTransient)}
            {Lang.DnsEligible}: {IPv4AdvancedCofnig.IsDnsEligible}{IPv4AdvancedCofnig.IsDnsEligibleCollcetion.ToStringWithLeftAlignment(Lang.DnsEligible)}
            {Lang.DuplicateAddressDetectionState}: {IPv4AdvancedCofnig.DuplicateAddressDetectionState}{IPv4AdvancedCofnig.DuplicateAddressDetectionStateCollcetion.ToStringWithLeftAlignment(Lang.DuplicateAddressDetectionState)}
            """;
    }

    private string FormatIPv4InterfaceStatistics()
    {
        var stats = IPv4InterfaceStatistics;

        return $"""
            {Lang.IPv4BytesSent}: {stats.BytesSent}
            {Lang.IPv4BytesReceived}: {stats.BytesReceived}
            {Lang.UnicastPacketsSent}: {stats.UnicastPacketsSent}
            {Lang.UnicastPacketsReceived}: {stats.UnicastPacketsReceived}
            {Lang.NonUnicastPacketsSent}: {stats.NonUnicastPacketsSent}
            {Lang.NonUnicastPacketsReceived}: {stats.NonUnicastPacketsReceived}
            {Lang.OutgoingPacketsDiscarded}: {stats.OutgoingPacketsDiscarded}
            {Lang.OutgoingPacketsWithErrors}: {stats.OutgoingPacketsWithErrors}
            {Lang.IncomingPacketsDiscarded}: {stats.IncomingPacketsDiscarded}
            {Lang.IncomingPacketsWithErrors}: {stats.IncomingPacketsWithErrors}
            {Lang.IncomingUnknownProtocolPackets}: {stats.IncomingUnknownProtocolPackets}
            {Lang.OutputQueueLength}: {stats.OutputQueueLength}
            """;
    }

    private string FormatIPv6InterfaceProperties()
    {
        var p = Nic?.IPv6InterfaceProperties;

        return $"""
            {Lang.Index}: {p?.Index}
            {Lang.MTU}: {p?.Mtu}
            {Lang.IsTransient}: {IPv6AdvancedCofnig.IsTransient}{IPv6AdvancedCofnig.IsTransientCollection.ToStringWithLeftAlignment(Lang.IsTransient)}
            {Lang.DnsEligible}: {IPv6AdvancedCofnig.IsDnsEligible}{IPv6AdvancedCofnig.IsDnsEligibleCollcetion.ToStringWithLeftAlignment(Lang.DnsEligible)}
            {Lang.DuplicateAddressDetectionState}: {IPv6AdvancedCofnig.DuplicateAddressDetectionState}{IPv6AdvancedCofnig.DuplicateAddressDetectionStateCollcetion.ToStringWithLeftAlignment(Lang.DuplicateAddressDetectionState)}
            """;
    }

    private void GetIPAdvancedConfig()
    {
        if (Nic is not null)
        {
            IPv4AdvancedCofnig = NetworkManagement.GetIPv4AdvancedConfig(Nic);
            IPv4InterfaceStatistics = Nic.GetIPv4Statistics();
            IPv6AdvancedCofnig = NetworkManagement.GetIPv6AdvancedConfig(Nic);
        }
    }

    private string GetIPv4PropertyContents()
    {
        string header = Nic.SupportsIPv4 ? Lang.TcpIPv4_Header : $"{Lang.AdapterNotSupported} {Lang.TcpIPv4_Header}";

        string text = $"""
            {header}
            [{Lang.IPv4GeneralProperties_Header}]
            {IPv4AdvancedCofnig.FormatGeneralProperties()}

            [{Lang.IPv4Lifetimes_Header}]
            {IPv4AdvancedCofnig.FormatLifetimes()}

            [{Lang.IPv4AdvancedProperties_Header}]
            {FormatIPv4InterfaceProperties()}

            [{Lang.IPv4Statistics_Header}]
            {FormatIPv4InterfaceStatistics()}
            """;

        return text;
    }

    private string GetIPv6PropertyContents()
    {
        string header = Nic.SupportsIPv6 ? Lang.TcpIPv6_Header : $"{Lang.AdapterNotSupported} {Lang.TcpIPv6_Header}";

        string text = $"""
            {header}
            [{Lang.IPv6GeneralProperties_Header}]
            {IPv6AdvancedCofnig.FormatGeneralProperties()}

            [{Lang.IPv6Lifetimes_Header}]
            {IPv6AdvancedCofnig.FormatLifetimes()}

            [{Lang.IPv6AdvancedProperties_Header}]
            {FormatIPv6InterfaceProperties()}

            [{Lang.IPv6Statistics_Header}]
            {Lang.NotSupported}
            """;

        return text;
    }

    #endregion Private Methods
}
