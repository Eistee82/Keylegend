using Keylegend.Core.Input;

namespace Keylegend.Windows;

/// <summary>
/// Reads which modifiers are held and which locks are on.
/// </summary>
/// <remarks>
/// This is a poll, not a hook. No keystroke is intercepted, forwarded, logged or stored — the
/// reader only ever asks "is this key down right now". That keeps the application out of the
/// input chain, which matters both for privacy and because anti-cheat systems object to
/// keyboard hooks.
/// </remarks>
public sealed class KeyboardStateReader
{
    /// <summary>Takes a snapshot of the current state.</summary>
    public KeyboardState Read()
    {
        var modifiers = ModifierKeys.None;

        if (NativeMethods.IsDown(NativeMethods.VK_LSHIFT)) { modifiers |= ModifierKeys.LeftShift; }
        if (NativeMethods.IsDown(NativeMethods.VK_RSHIFT)) { modifiers |= ModifierKeys.RightShift; }
        if (NativeMethods.IsDown(NativeMethods.VK_LCONTROL)) { modifiers |= ModifierKeys.LeftCtrl; }
        if (NativeMethods.IsDown(NativeMethods.VK_RCONTROL)) { modifiers |= ModifierKeys.RightCtrl; }
        if (NativeMethods.IsDown(NativeMethods.VK_LMENU)) { modifiers |= ModifierKeys.LeftAlt; }
        if (NativeMethods.IsDown(NativeMethods.VK_RMENU)) { modifiers |= ModifierKeys.RightAlt; }
        if (NativeMethods.IsDown(NativeMethods.VK_LWIN)) { modifiers |= ModifierKeys.LeftWin; }
        if (NativeMethods.IsDown(NativeMethods.VK_RWIN)) { modifiers |= ModifierKeys.RightWin; }

        var locks = new LockStates(
            NumLock: NativeMethods.IsToggled(NativeMethods.VK_NUMLOCK),
            CapsLock: NativeMethods.IsToggled(NativeMethods.VK_CAPITAL),
            ScrollLock: NativeMethods.IsToggled(NativeMethods.VK_SCROLL));

        return new KeyboardState(modifiers, locks);
    }
}
