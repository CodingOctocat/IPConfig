using System.Collections.Generic;

namespace IPConfig.Helpers;

public static class FileOrderHelper
{
    public static int GetOrder(IEnumerable<int> orders, int start = 1)
    {
        HashSet<int> uniqueOrders = new(orders);

        for (int i = start; i < uniqueOrders.Count + 1; i++)
        {
            if (!uniqueOrders.Contains(i))
            {
                return i;
            }
        }

        return uniqueOrders.Count + 1;
    }
}
