using System.ComponentModel;

namespace IPConfig.Models.Messages;

public class CollectionChangeActionMessage<T> : ISender
{
    public CollectionChangeAction Action { get; }

    public T Item { get; }

    public object Sender { get; }

    public CollectionChangeActionMessage(object sender, CollectionChangeAction action, T item)
    {
        Sender = sender;
        Action = action;
        Item = item;
    }
}
