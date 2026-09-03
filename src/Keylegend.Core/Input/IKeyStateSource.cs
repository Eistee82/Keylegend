namespace Keylegend.Core.Input;

/// <summary>
/// Where the keyboard state comes from.
/// </summary>
/// <remarks>
/// Abstracted for two reasons. Tests can drive the engine's timing and session behaviour
/// without a keyboard attached; and the window can feed it a state the user has chosen rather
/// than the live one, which is what makes "show me the AltGr layer" possible without holding
/// AltGr down.
/// </remarks>
public interface IKeyStateSource
{
    /// <summary>The current modifier and lock state.</summary>
    KeyboardState Read();

    /// <summary>Whether any key is held right now, used to wake the lighting.</summary>
    bool AnyKeyDown();

    /// <summary>
    /// Which keys are held right now, by key id — what the keystroke effects are made of.
    /// </summary>
    /// <remarks>
    /// Asked only while an effect is selected. With none the engine never calls it, so a source
    /// that reads real hardware never looks at the individual keys at all.
    /// </remarks>
    IReadOnlyList<string> PressedKeys();
}
