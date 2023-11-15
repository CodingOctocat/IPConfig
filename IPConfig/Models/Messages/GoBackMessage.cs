namespace IPConfig.Models.Messages;

public class GoBackMessage(object sender) : ISender
{
    public object Sender { get; } = sender;
}
