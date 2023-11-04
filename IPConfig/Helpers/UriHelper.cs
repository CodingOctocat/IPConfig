using System;
using System.Diagnostics;

namespace IPConfig.Helpers;

public static class UriHelper
{
    public static string NormalizeUri(string uri)
    {
        // HACK: 如果不转换为 AbsoluteUri，那么链接中的 “%20” 将以空格形式传入参数。
        string absoluteUri = new Uri(uri, UriKind.RelativeOrAbsolute).AbsoluteUri;

        // HACK: 如果不替换 “&”，那么 “&” 后面的内容将被截断。
        absoluteUri = absoluteUri.Replace("&", "^&");

        return absoluteUri;
    }

    public static void OpenUri(string uri)
    {
        uri = NormalizeUri(uri);

        var psi = new ProcessStartInfo {
            FileName = "cmd",
            WindowStyle = ProcessWindowStyle.Hidden,
            UseShellExecute = true,
            CreateNoWindow = true,
            Arguments = $"/c start {uri}"
        };

        Process.Start(psi);
    }
}
