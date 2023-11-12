using System;

namespace IPConfig.Models.Github;

public record GithubReleaseInfo(string TagName, string Name, string ReleaseNote, DateTimeOffset CreatedAt, string HtmlUrl)
{ }
