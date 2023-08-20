using System.Collections.Immutable;
using System.Windows.Data;

namespace IPConfig.Helpers;

public class GroupItemHelper
{
    public static ImmutableArray<T> GetGroupItems<T>(CollectionViewGroup group)
    {
        var builder = ImmutableArray.CreateBuilder<T>();

        void GetGroupItemsRec(CollectionViewGroup group)
        {
            foreach (object item in group.Items)
            {
                if (group.IsBottomLevel)
                {
                    builder.Add((T)item);
                }
                else
                {
                    GetGroupItemsRec((CollectionViewGroup)item);
                }
            }
        }

        GetGroupItemsRec(group);

        return builder.ToImmutableArray();
    }
}
