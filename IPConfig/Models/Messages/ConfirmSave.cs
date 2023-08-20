namespace IPConfig.Models.Messages;

public class ConfirmSave : ISender
{
    public IPConfigModel IPConfig { get; }

    public bool IsSaved { get; }

    public object Sender { get; }

    public ConfirmSave(object sender, IPConfigModel iPConfig, bool isSaved)
    {
        Sender = sender;
        IPConfig = iPConfig;
        IsSaved = isSaved;
    }
}
