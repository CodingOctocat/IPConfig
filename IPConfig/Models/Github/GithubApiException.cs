using System;
using System.Runtime.Serialization;

namespace IPConfig.Models.GitHub;

public class GitHubApiException : Exception
{
    public GitHubApiException()
    { }

    public GitHubApiException(string? message) : base(message)
    { }

    public GitHubApiException(string? message, Exception? innerException) : base(message, innerException)
    { }

    protected GitHubApiException(SerializationInfo info, StreamingContext context) : base(info, context)
    { }
}
