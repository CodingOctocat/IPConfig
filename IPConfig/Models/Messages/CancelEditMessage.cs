namespace IPConfig.Models.Messages;

public class CancelEditMessage(object sender, bool ask) : ISender
{
    public bool Ask { get; } = ask;

    public object Sender { get; } = sender;
}
