namespace IPConfig.Models.Messages;

public class EmptyMessage(object sender) : ISender
{
    public object Sender { get; } = sender;
}
