namespace IPConfig.Models.Messages;

public class ChangeSelectionMessage<T>(object sender, T selection) : ISender
{
    public T Selection { get; } = selection;

    public object Sender { get; } = sender;
}
