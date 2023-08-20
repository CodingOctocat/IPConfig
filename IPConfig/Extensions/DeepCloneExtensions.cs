using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

using IPConfig.Models;

namespace IPConfig.Extensions;

public static class DeepCloneExtensions
{
    /// <summary>
    /// 基于序列化/反序列化技术的深层克隆。
    /// <para>
    /// 如果副本的嵌套(子)属性发生变化，并且副本赋值回原本后，嵌套(子)属性不会立马引发属性通知。
    /// </para>
    /// <para>
    /// * 解决方法1：对象实现自己的深层克隆(<seealso cref="IDeepCloneTo{T}"/>)。
    /// </para>
    /// <para>
    /// * 解决方法2：将嵌套(子)属性的 PropertyChanged 传递到父对象。
    /// </para>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="original"></param>
    /// <returns></returns>
    [return: NotNullIfNotNull(nameof(original))]
    public static T? DeepClone<T>(this T original)
    {
        string json = JsonSerializer.Serialize(original);
        var copy = JsonSerializer.Deserialize<T>(json);

        return copy;
    }
}
