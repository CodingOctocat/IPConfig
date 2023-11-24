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

    public bool GestureEquals(string gesture)
    {
        var gestureConverter = new KeyGestureConverter();
        var keyGesture = gestureConverter.ConvertFromInvariantString(gesture) as KeyGesture;
        string? normalizedGesture = keyGesture?.GetDisplayStringForCulture(CultureInfo.InvariantCulture);

        if (normalizedGesture is not null)
        {
            return normalizedGesture.Equals(Gesture, StringComparison.OrdinalIgnoreCase);
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
