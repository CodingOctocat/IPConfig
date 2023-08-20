namespace IPConfig.Models.Messages;

public class ChangeSelectionMessage<T> : ISender
{
    public T Selection { get; }

    public object Sender { get; }

    public ChangeSelectionMessage(object sender, T selection)
    {
        Sender = sender;
        Selection = selection;
    }
}
