using System;

namespace IPConfig.Extensions;

public static class StringExtensions
{
    public static string Format(this string format, params object[] args)
    {
        return String.Format(format, args);
    }
}
