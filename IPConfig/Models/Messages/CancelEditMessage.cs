namespace IPConfig.Models.Messages;

public class CancelEditMessage : ISender
{
    public bool Ask { get; }

    public object Sender { get; }

    public CancelEditMessage(object sender, bool ask)
    {
        Sender = sender;
        Ask = ask;
    }
}
