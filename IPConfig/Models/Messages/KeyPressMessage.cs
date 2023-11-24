using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Windows.Input;

namespace IPConfig.Models.Messages;

public class KeyPressMessage : ISender
{
    public KeyEventArgs Args { get; }

    public string Gesture { get; private set; }

    public object Sender { get; }

    public KeyPressMessage(object sender, KeyEventArgs args)
    {
        Sender = sender;
        Args = args;

        GetGesture();
    }

    public bool GestureEquals(string gestureString)
    {
        var gestureConverter = new KeyGestureConverter();
        var keyGesture = gestureConverter.ConvertFromInvariantString(gestureString) as KeyGesture;
        string? normalizedGestureString = keyGesture?.GetDisplayStringForCulture(CultureInfo.InvariantCulture);

        if (normalizedGestureString is not null)
        {
            return normalizedGestureString.Equals(Gesture, StringComparison.OrdinalIgnoreCase);
        }

        return false;
    }

    [MemberNotNull(nameof(Gesture))]
    private void GetGesture()
    {
        try
        {
            var gesture = new KeyGesture(Args.Key, Args.KeyboardDevice.Modifiers);
            Gesture = gesture.GetDisplayStringForCulture(CultureInfo.InvariantCulture);
        }
        catch (NotSupportedException)
        {
            Gesture = Args.Key.ToString();
        }
    }
}
