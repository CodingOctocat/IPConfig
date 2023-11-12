using System;
using System.Runtime.Serialization;

namespace IPConfig.Models.Github;

public class GithubApiException : Exception
{
    public GithubApiException()
    { }

    public GithubApiException(string? message) : base(message)
    { }

    public GithubApiException(string? message, Exception? innerException) : base(message, innerException)
    { }

    protected GithubApiException(SerializationInfo info, StreamingContext context) : base(info, context)
    { }
}
