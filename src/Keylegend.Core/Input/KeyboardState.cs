namespace Keylegend.Core.Input;

/// <summary>
/// Modifier keys, with the sides kept apart. The distinction is not cosmetic: Windows
/// reports AltGr as Ctrl plus <em>right</em> Alt, so telling the sides apart is the only way
/// to know whether the user wanted the character layer or the Ctrl+Alt shortcut set.
/// </summary>
[Flags]
public enum ModifierKeys
{
    None = 0,
    LeftShift = 1 << 0,
    RightShift = 1 << 1,
    LeftCtrl = 1 << 2,
    RightCtrl = 1 << 3,
    LeftAlt = 1 << 4,
    RightAlt = 1 << 5,
    LeftWin = 1 << 6,
    RightWin = 1 << 7
}

/// <summary>The three toggles that change what keys mean.</summary>
public readonly record struct LockStates(bool NumLock, bool CapsLock, bool ScrollLock);

/// <summary>Everything about the keyboard that affects what its keys currently mean.</summary>
public readonly record struct KeyboardState(ModifierKeys Modifiers, LockStates Locks)
{
    public static KeyboardState Empty { get; } =
        new(ModifierKeys.None, new LockStates(false, false, false));

    public bool Shift => Has(ModifierKeys.LeftShift | ModifierKeys.RightShift);

    public bool Win => Has(ModifierKeys.LeftWin | ModifierKeys.RightWin);

    /// <summary>
    /// Right Alt. Windows synthesises a Ctrl press alongside it, which is why every other
    /// property below has to exclude this case explicitly.
    /// </summary>
    public bool AltGr => Has(ModifierKeys.RightAlt);

    /// <summary>Left Alt only, and only when this is not an AltGr press.</summary>
    public bool Alt => !AltGr && Has(ModifierKeys.LeftAlt);

    /// <summary>Ctrl, unless the Ctrl flag is merely the shadow of an AltGr press.</summary>
    public bool Ctrl => !AltGr && Has(ModifierKeys.LeftCtrl | ModifierKeys.RightCtrl);

    /// <summary>
    /// Whether a modifier is held that blanks unassigned keys. Shift, Caps Lock and Num Lock
    /// are deliberately absent: they change which character a key produces without hiding
    /// anything.
    /// </summary>
    public bool HasFilteringModifier => AltGr || Ctrl || Alt || Win;

    /// <summary>
    /// Whether the number pad currently types digits.
    /// </summary>
    /// <remarks>
    /// Holding Shift suspends Num Lock, but only in that one direction: with Num Lock on,
    /// Shift turns the pad back into navigation keys — with Num Lock off, Shift leaves it as
    /// navigation rather than flipping it to digits. That asymmetry is deliberate on Windows'
    /// part, because it is what allows Shift plus the pad's arrows to select text.
    /// </remarks>
    public bool NumpadDigitsActive => Locks.NumLock && !Shift;

    /// <summary>The modifiers a shortcut set is looked up by.</summary>
    public ModifierKeys FilteringModifiers => Modifiers;

    private bool Has(ModifierKeys mask) => (Modifiers & mask) != 0;
}
