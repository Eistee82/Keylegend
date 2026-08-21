namespace Keylegend.Core.Input;

/// <summary>
/// What a key means right now. The category follows from the character the key produces in
/// the current keyboard state, which is why Shift and Caps Lock need no special handling:
/// the same key simply reports a different character and lands in a different category.
/// </summary>
public enum KeyCategory
{
    /// <summary>Produces nothing in the current context.</summary>
    Unassigned,

    Digit,
    Lowercase,
    Uppercase,

    /// <summary>Punctuation, currency, mathematical signs, space.</summary>
    Symbol,

    /// <summary>Escape, Tab, Enter, modifiers, navigation.</summary>
    Control,

    /// <summary>Produces a character only in combination with the next keystroke.</summary>
    DeadKey,

    /// <summary>
    /// F1 to F12. Unlike every other category this one cannot be derived from a produced
    /// character — function keys produce none — so it is recognised by key identity instead.
    /// </summary>
    FunctionKey
}
