using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace IPConfig.Helpers;

/// <summary>
/// <see href="https://stackoverflow.com/a/62311137/4380178">How to use nameof for long property-paths to get the "fullnameof"?</see>
/// </summary>
public static class NameOfHelper
{
    public static string NameOf<TObject, TProperty>(Expression<Func<TObject, TProperty>> property)
    {
        var members = new Stack<string>();

        for (var memberExpr = property.Body as MemberExpression; memberExpr is not null; memberExpr = memberExpr.Expression as MemberExpression)
        {
            members.Push(memberExpr.Member.Name);
        }

        return String.Join(".", members);
    }
}
