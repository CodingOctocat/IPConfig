namespace IPConfig.Models.Messages;

public class AddUntitledIPConfigMessage(object sender, string name = "") : ISender
{
    public string Name { get; set; } = name;

    public object Sender { get; } = sender;
}
