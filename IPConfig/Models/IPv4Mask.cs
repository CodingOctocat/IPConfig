using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;

using CsvHelper;
using CsvHelper.Configuration;

using IPConfig.Languages;

namespace IPConfig.Models;

public record IPv4Mask(string Mask, int CIDR, string Group)
{
    public static List<IPv4Mask> ReadIPv4MaskList()
    {
        string[] files = Directory.GetFiles(@".\.mask", "*.csv");

        var files1 = files.Where(x => x.EndsWith($".{LangSource.Instance.CurrentCulture.Name}.csv"));
        var files2 = files.Where(x => !Path.GetFileNameWithoutExtension(x).Contains('.'));

        var list = new List<IPv4Mask>();

        foreach (string file in files1.Concat(files2))
        {
            using var reader = new StreamReader(file);

            var config = new CsvConfiguration(CultureInfo.InvariantCulture) {
                TrimOptions = TrimOptions.Trim
            };

            using var csv = new CsvReader(reader, config);
            csv.Context.TypeConverterOptionsCache.GetOptions<string?>().NullValues.AddRange(new[] { "", "NULL" });
            var records = csv.GetRecords<IPv4Mask>();
            list.AddRange(records);
        }

        return list;
    }

    public override string ToString()
    {
        return Mask;
    }
}

public enum IPv4MaskClass
{
    [LocalizedDescription(LangKey.Default)]
    Default = 1,

    [Description("/0 ~ /8")]
    A = 2,

    [Description("/9 ~ /16")]
    B = 4,

    [Description("/17 ~ /24")]
    C = 8,

    [Description("/25 ~ /32")]
    D = 16
}
