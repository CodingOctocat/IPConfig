using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

using IPConfig.Languages;

namespace IPConfig.Models;

public partial class LastUsedIPv4Config : IPv4Config
{
    public static readonly Dictionary<string, LastUsedIPv4Config> Backups = new();

    public string FormatedLastUsedTime => LastUsedTime.ToString(LangSource.Instance.CurrentCulture);

    public DateTime LastUsedTime { get; }

    public LastUsedIPv4Config(DateTime lastUsedTime)
    {
        LastUsedTime = lastUsedTime;
    }

    public static async Task BackupAsync(string nicId, IPv4Config value)
    {
        string path = $"backup/{nicId}.bak";
        string json = JsonSerializer.Serialize(value, App.JsonOptions);
        Directory.CreateDirectory("backup");
        await File.WriteAllTextAsync(path, json);
        var backup = new LastUsedIPv4Config(DateTime.Now);
        value.DeepCloneTo(backup);
        Backups[nicId] = backup;
    }

    public static async Task<LastUsedIPv4Config?> ReadAsync(string nicId)
    {
        if (Backups.TryGetValue(nicId, out var cache))
        {
            return cache;
        }

        string path = $"backup/{nicId}.bak";

        if (!File.Exists(path))
        {
            return null;
        }

        try
        {
            string json = await File.ReadAllTextAsync(path);
            var backup = JsonSerializer.Deserialize<IPv4Config>(json);

            if (backup is null)
            {
                return null;
            }

            var lastUsedIPv4Config = new LastUsedIPv4Config(File.GetLastWriteTime(path));
            backup.DeepCloneTo(lastUsedIPv4Config);
            Backups[nicId] = lastUsedIPv4Config;

            return lastUsedIPv4Config;
        }
        catch
        {
            return null;
        }
    }
}
