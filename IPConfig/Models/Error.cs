using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace IPConfig.Models;

public class Error
{
    public class SerializableException
    {
        public Dictionary<string, string?> Datas = [];

        public string? HelpLink { get; }

        public int HResult { get; }

        public SerializableException? InnerException { get; }

        public string Message { get; }

        public string? Source { get; }

        public string? StackTrace { get; }

        public string? TargetSite { get; }

        public string Type { get; }

        public SerializableException(Exception exception)
        {
            HelpLink = exception.HelpLink;
            HResult = exception.HResult;
            Message = exception.Message;
            Source = exception.Source;
            StackTrace = exception.StackTrace;
            TargetSite = exception.TargetSite?.ToString();
            Type = exception.GetType().ToString();

            foreach (DictionaryEntry data in exception.Data)
            {
                Datas.Add($"{data.Key}", data.Value?.ToString());
            }

            if (exception?.InnerException is not null)
            {
                InnerException = new(exception.InnerException);
            }
        }

        public override string? ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"{nameof(Type)}: {Type ?? "null"}");
            sb.AppendLine($"{nameof(Source)}: {Source ?? "null"}");
            sb.AppendLine($"{nameof(Message)}: {Message ?? "null"}");
            sb.AppendLine($"{nameof(StackTrace)}:{Environment.NewLine}{StackTrace ?? "null"}");
            sb.AppendLine($"{nameof(HResult)}: {HResult}");
            sb.AppendLine($"{nameof(HelpLink)}: {HelpLink ?? "null"}");
            sb.AppendLine($"{nameof(InnerException)}: {InnerException?.ToString() ?? "null"}");

            return sb.ToString();
        }
    }

    public SerializableException Exception { get; }

    public string Source { get; }

    public DateTime Timestamp { get; }

    public Error(Exception exception, string source)
    {
        Timestamp = DateTime.Now;
        Exception = new(exception);
        Source = source;
    }

    public Error(Exception exception, string source, DateTime timestamp)
    {
        Timestamp = timestamp;
        Exception = new(exception);
        Source = source;
    }

    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"{nameof(Timestamp)}: {Timestamp.ToString()}");
        sb.AppendLine($"{nameof(Source)}: {Source}");
        sb.AppendLine($"{nameof(Exception)}: {Exception.ToString()}");

        return sb.ToString();
    }
}
