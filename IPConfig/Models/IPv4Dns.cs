using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading.Tasks;

using CommunityToolkit.Mvvm.ComponentModel;

using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.Configuration.Attributes;

using IPConfig.Languages;

namespace IPConfig.Models;

public partial class IPv4Dns : ObservableObject
{
    private bool _isRunning;

    private PingReply? _pingReply;

    public string? Description { get; set; }

    public string Dns1 { get; set; }

    public string? Dns2 { get; set; }

    public string? Filter { get; set; }

    public string Group { get; set; }

    [Ignore]
    public bool IsRunning
    {
        get => _isRunning;
        private set => SetProperty(ref _isRunning, value);
    }

    [Ignore]
    public PingReply? PingReply
    {
        get => _pingReply;
        set => SetProperty(ref _pingReply, value);
    }

    public string Provider { get; set; }

    public IPv4Dns([Name(nameof(Provider))] string provider,
        [Name(nameof(Filter))] string? filter,
        [Name(nameof(Dns1))] string dns1,
        [Name(nameof(Dns2))] string? dns2,
        [Name(nameof(Group))] string group,
        [Name(nameof(Description))] string? description)
    {
        Provider = provider;
        Filter = filter;
        Dns1 = dns1;
        Dns2 = dns2;
        Group = group;
        Description = description;
    }

    public static List<IPv4Dns> ReadIPv4DnsList()
    {
        string[] files = Directory.GetFiles(@".\.dns", "*.csv");

        var files1 = files.Where(x => x.EndsWith($".{LangSource.Instance.CurrentCulture.Name}.csv"));
        var files2 = files.Where(x => !Path.GetFileNameWithoutExtension(x).Contains('.'));

        var list = new List<IPv4Dns>();

        foreach (string file in files1.Concat(files2))
        {
            using var reader = new StreamReader(file);

            var config = new CsvConfiguration(CultureInfo.InvariantCulture) {
                TrimOptions = TrimOptions.Trim
            };

            using var csv = new CsvReader(reader, config);
            csv.Context.TypeConverterOptionsCache.GetOptions<string?>().NullValues.AddRange(new[] { "", "NULL" });
            var records = csv.GetRecords<IPv4Dns>();
            list.AddRange(records);
        }

        return list;
    }

    public async Task PingAsync()
    {
        if (!IsRunning)
        {
            IsRunning = true;
            PingReply = null;

            using var ping = new Ping();

            try
            {
                PingReply = await ping.SendPingAsync(Dns1);
            }
            catch
            {
                PingReply = null;
            }

            IsRunning = false;
        }
    }

    public override string ToString()
    {
        return Dns1;
    }
}
