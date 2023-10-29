namespace IPConfig.Models.Messages;

public class EmptyMessage : ISender
{
    public object Sender { get; }

    public EmptyMessage(object sender)
    {
        Sender = sender;
    }
}
