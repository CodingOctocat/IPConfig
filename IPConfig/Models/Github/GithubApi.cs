using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace IPConfig.Models.GitHub;

public static class GitHubApi
{
    public const string GetLatestReleaseApi = $"https://api.github.com/repos/{Owner}/{RepositoryName}/releases/latest";

    public const string Owner = "CodingOctocat";

    public const string ReleasesUrl = $"{RepositoryUrl}/releases";

    public const string RepositoryName = "IPConfig";

    public const string RepositoryUrl = $"https://github.com/{Owner}/{RepositoryName}";

    private static readonly HttpClient _httpClient = new();

    static GitHubApi()
    {
        _httpClient.DefaultRequestHeaders.UserAgent.Add(new(new ProductHeaderValue($"{Owner}-{RepositoryName}", App.VersionString)));
    }

    public static async Task<GitHubReleaseInfo> GetLatestReleaseInfoAsync()
    {
        var jObj = await _httpClient.GetFromJsonAsync<JsonObject>(GetLatestReleaseApi);

        ArgumentNullException.ThrowIfNull(jObj);

        string tagName = jObj.GetValueEx("tag_name", "");
        string name = jObj.GetValueEx("name", "");
        string releaseNote = jObj.GetValueEx("body", "");
        var createdAt = jObj.GetValueEx("created_at", DateTimeOffset.MinValue);
        string htmlUrl = jObj.GetValueEx("html_url", "");
        var info = new GitHubReleaseInfo(tagName, name, releaseNote, createdAt, htmlUrl);

        return info;
    }

    private static T GetValueEx<T>(this JsonObject self, string propertyName, T defaultValue)
    {
        if (self.TryGetPropertyValue(propertyName, out var node))
        {
            try
            {
                var value = node!.GetValue<T>();

                return value;
            }
            catch (Exception ex)
            {
                throw new GitHubApiException(ex.Message, ex);
            }
        }
        else
        {
            return defaultValue;
        }
    }
}
