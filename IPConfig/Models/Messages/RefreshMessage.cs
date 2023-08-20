namespace IPConfig.Models.Messages;

public class RefreshMessage : ISender
{
    public object Sender { get; }

    public RefreshMessage(object sender)
    {
        Sender = sender;
    }
}
