namespace Keylegend.Core.Input;

/// <summary>What a key produces in a particular keyboard state, and what that makes it.</summary>
/// <param name="Character">The character produced, or <c>null</c> if the key produces none.</param>
/// <param name="Category">The category that follows from it.</param>
public readonly record struct KeyMeaning(string? Character, KeyCategory Category)
{
    /// <summary>The key produces nothing in this state.</summary>
    public static KeyMeaning Unassigned { get; } = new(null, KeyCategory.Unassigned);

    /// <summary>Whether the key carries any meaning at all.</summary>
    public bool IsAssigned => Category != KeyCategory.Unassigned;

    /// <summary>
    /// Whether the key actually types something visible.
    /// </summary>
    /// <remarks>
    /// The distinction from <see cref="IsAssigned"/> matters on the AltGr layer. A key with no
    /// AltGr assignment reports "no character", which is indistinguishable from what Escape or
    /// an arrow key reports — both land in <see cref="KeyCategory.Control"/>. Treating that as
    /// "assigned" lit the whole keyboard instead of just the keys carrying an AltGr character,
    /// which is the opposite of what the layer is for.
    /// </remarks>
    public bool ProducesCharacter =>
        Category is not (KeyCategory.Unassigned or KeyCategory.Control)
        && !string.IsNullOrEmpty(Character);
}

/// <summary>
/// Answers "what does this key mean right now". Abstracted so the colouring rules can be
/// tested without Windows, and so the on-screen preview can drive them with any state.
/// </summary>
public interface IKeyResolver
{
    /// <param name="keyId">Key identifier, as the attached keyboard names it.</param>
    /// <param name="scanCode">Scan code override the key carries, or <c>null</c>.</param>
    /// <param name="state">The keyboard state to evaluate against.</param>
    KeyMeaning Resolve(string keyId, int? scanCode, KeyboardState state);
}
