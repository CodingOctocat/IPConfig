using System;

namespace IPConfig.Models.GitHub;

public class GitHubApiException : Exception
{
    public GitHubApiException()
    { }

    public GitHubApiException(string? message) : base(message)
    { }

    public GitHubApiException(string? message, Exception? innerException) : base(message, innerException)
    { }
}
