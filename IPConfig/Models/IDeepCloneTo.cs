namespace IPConfig.Models;

/// <summary>
/// 支持深层克隆。
/// </summary>
/// <typeparam name="T"></typeparam>
public interface IDeepCloneTo<T>
{
    void DeepCloneTo(T other);
}
