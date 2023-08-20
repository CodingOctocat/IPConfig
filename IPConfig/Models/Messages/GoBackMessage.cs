namespace IPConfig.Models.Messages;

public class GoBackMessage : ISender
{
    public object Sender { get; }

    public GoBackMessage(object sender)
    {
        Sender = sender;
    }
}
