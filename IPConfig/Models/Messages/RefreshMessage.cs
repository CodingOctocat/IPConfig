namespace IPConfig.Models.Messages;

public class RefreshMessage(object sender) : ISender
{
    public object Sender { get; } = sender;
}
