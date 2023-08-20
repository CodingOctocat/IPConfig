using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Windows.Input;

namespace IPConfig.Models.Messages;

public class KeyPressMessage : ISender
{
    public KeyEventArgs? Args { get; }

    public string Gesture { get; private set; }

    public object Sender { get; }

    public KeyPressMessage(object sender, string gesture)
    {
        Sender = sender;
        Gesture = gesture;
    }

    public KeyPressMessage(object sender, KeyEventArgs args)
    {
        Sender = sender;
        Args = args;

        GetGesture();
    }

    [MemberNotNull(nameof(Gesture))]
    private void GetGesture()
    {
        var keys = new List<string>();

        if (Args!.KeyboardDevice.Modifiers.HasFlag(ModifierKeys.Windows))
        {
            keys.Add("Win");
        }

        if (Args.KeyboardDevice.Modifiers.HasFlag(ModifierKeys.Control))
        {
            keys.Add("Ctrl");
        }

        if (Args.KeyboardDevice.Modifiers.HasFlag(ModifierKeys.Alt))
        {
            keys.Add("Alt");
        }

        if (Args.KeyboardDevice.Modifiers.HasFlag(ModifierKeys.Shift))
        {
            keys.Add("Shift");
        }

        if (Args.Key == Key.Escape)
        {
            keys.Add("Esc");
        }
        else
        {
            keys.Add(Args.Key.ToString());
        }

        Gesture = String.Join('+', keys);
    }
}
