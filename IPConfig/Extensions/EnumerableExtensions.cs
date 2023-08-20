using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IPConfig.Extensions;

public static class EnumerableExtensions
{
    public static int Offset { get; set; } = 2;

    public static string ToStringWithLeftAlignment<T>(this IEnumerable<T> collection, int leftAlign)
    {
        if (!collection.Any())
        {
            return String.Empty;
        }

        var sb = new StringBuilder(Environment.NewLine);
        string left = new(' ', leftAlign + Offset);

        foreach (var item in collection)
        {
            sb.AppendLine($"{left}{item?.ToString()}");
        }

        return sb.ToString().TrimEnd();
    }

    public static string ToStringWithLeftAlignment<T>(this IEnumerable<T> collection, string leftAlign)
    {
        int len = leftAlign.Length;
        int byteCount = Encoding.UTF8.GetByteCount(leftAlign);
        int align = (len + byteCount) / 2;

        return ToStringWithLeftAlignment(collection, align);
    }
}
