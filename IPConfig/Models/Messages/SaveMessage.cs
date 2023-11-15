namespace IPConfig.Models.Messages;

public class SaveMessage(object sender) : ISender
{
    public object Sender { get; } = sender;
}
