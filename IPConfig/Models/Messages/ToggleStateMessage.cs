namespace IPConfig.Models.Messages;

public class ToggleStateMessage<T> : ISender
{
    public T NewValue { get; }

    public object Sender { get; }

    public ToggleStateMessage(object sender, T newValue)
    {
        Sender = sender;
        NewValue = newValue;
    }
}
