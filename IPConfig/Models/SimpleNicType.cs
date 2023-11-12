using System;
using System.Collections.Generic;

namespace IPConfig.Models;

public enum ConnectionType
{
    Other,

    Ethernet,

    Wlan,
}

public enum SimpleNicType
{
    Unknown,

    Ethernet,

    Wlan,

    Loopback,

    Other
}

public class SimpleNicTypeComparer : IComparer<SimpleNicType>
{
    public static readonly SimpleNicTypeComparer Instance = new();

    public int Compare(SimpleNicType x, SimpleNicType y)
    {
        int xOrder = GetSimpleNicTypeOrder(x);
        int yOrder = GetSimpleNicTypeOrder(y);

        return xOrder.CompareTo(yOrder);
    }

    private static int GetSimpleNicTypeOrder(SimpleNicType simpleNicType)
    {
        return simpleNicType switch {
            SimpleNicType.Ethernet => 0,
            SimpleNicType.Wlan => 1,
            SimpleNicType.Other => 2,
            SimpleNicType.Loopback => 3,
            SimpleNicType.Unknown => 4,
            _ => throw new ArgumentOutOfRangeException(nameof(simpleNicType), simpleNicType, null)
        };
    }
}
