namespace IPConfig.Models.Messages;

public class SaveMessage : ISender
{
    public object Sender { get; }

    public SaveMessage(object sender)
    {
        Sender = sender;
    }
}
