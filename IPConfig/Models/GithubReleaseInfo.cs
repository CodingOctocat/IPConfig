using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace IPConfig.Models;

public record GithubReleaseInfo(string TagName, string Name, string ReleaseNote, string CreatedAt, string HtmlUrl)
{
    private static readonly HttpClient _httpClient = new();

    static GithubReleaseInfo()
    {
        _httpClient.DefaultRequestHeaders.UserAgent.Add(new(new ProductHeaderValue($"{App.Owner}-{App.AppName}", App.VersionString)));
    }

    public static async Task<GithubReleaseInfo> GetLatestReleaseInfoAsync()
    {
        var jObj = await _httpClient.GetFromJsonAsync<JsonObject>($"https://api.github.com/repos/{App.Owner}/{App.AppName}/releases/latest");

        ArgumentNullException.ThrowIfNull(jObj);

        string tagName = jObj["tag_name"]!.ToString();
        string name = jObj["name"]!.ToString();
        string releaseNote = jObj["body"]!.ToString();
        string createdAt = jObj["created_at"]!.ToString();
        string htmlUrl = jObj["html_url"]!.ToString();
        var info = new GithubReleaseInfo(tagName, name, releaseNote, createdAt, htmlUrl);

        return info;
    }
}
