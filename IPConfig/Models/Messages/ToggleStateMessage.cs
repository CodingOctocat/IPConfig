namespace IPConfig.Models.Messages;

public class ToggleStateMessage<T>(object sender, T newValue) : ISender
{
    public T NewValue { get; } = newValue;

    public object Sender { get; } = sender;
}
