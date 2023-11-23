using System;

namespace IPConfig.Models.GitHub;

public record GitHubReleaseInfo(string TagName, string Name, string ReleaseNote, DateTimeOffset CreatedAt, string HtmlUrl);
