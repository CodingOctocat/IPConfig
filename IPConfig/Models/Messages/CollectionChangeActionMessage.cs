using System.ComponentModel;

namespace IPConfig.Models.Messages;

public class CollectionChangeActionMessage<T>(object sender, CollectionChangeAction action, T item) : ISender
{
    public CollectionChangeAction Action { get; } = action;

    public T Item { get; } = item;

    public object Sender { get; } = sender;
}
