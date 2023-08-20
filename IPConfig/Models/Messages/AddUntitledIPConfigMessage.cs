namespace IPConfig.Models.Messages;

public class AddUntitledIPConfigMessage : ISender
{
    public string Name { get; set; }

    public object Sender { get; }

    public AddUntitledIPConfigMessage(object sender, string name = "")
    {
        Sender = sender;
        Name = name;
    }
}
