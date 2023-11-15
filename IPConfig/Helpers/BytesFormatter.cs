using System;

namespace IPConfig.Helpers;

public static class BytesFormatter
{
    private static readonly string[] _bandUnits = ["bps", "Kbps", "Mbps", "Gbps", "Tbps", "Pbps", "Ebps", "Zbps", "Ybps"];

    private static readonly string[] _fileUnits = ["B", "KB", "MB", "GB", "TB", "PB", "EB", "Zb", "Yb"];

    private static readonly string[] _speedUnits = ["B/s", "KB/s", "MB/s", "GB/s", "TB/s", "PB/s", "EB/s", "Zb/s", "Yb/s"];

    public static string ToFileSize(long size, int sys = 1024)
    {
        return Format(size, sys, _fileUnits);
    }

    public static string ToNetBand(long speed)
    {
        return Format(speed, 1000, _bandUnits);
    }

    public static string ToNetSpeed(long speed)
    {
        return Format(speed, 1024, _speedUnits);
    }

    private static string Format(long size, int sys, string[] units)
    {
        decimal rate = size;
        int idx = 0;

        while (rate >= sys)
        {
            rate /= sys;
            idx++;
        }

        return $"{Math.Round(rate, 1)} {units[idx]}";
    }
}
