using IPConfig.Models;

namespace IPConfig.Extensions;

public static class IPConfigModelExtensions
{
    public static EditableIPConfigModel AsEditable(this IPConfigModel source, bool beginEdit = true)
    {
        var target = EditableIPConfigModel.Empty;
        source.DeepCloneTo(target);

        if (beginEdit)
        {
            target.BeginEdit();
        }

        return target;
    }
}
