using Keylegend.Core.Lighting;
using Keylegend.Core.Lighting.Effects;
using Keylegend.Core.Profiles;
using Keylegend.Core.Shortcuts;

namespace Keylegend.Engine;

/// <summary>
/// Everything about the engine's behaviour the user can change while it runs.
/// </summary>
public sealed record EngineSettings
{
    /// <summary>
    /// How long the keyboard may stay quiet before the lighting is handed back.
    /// </summary>
    /// <remarks>
    /// Sixty seconds by default. Reclaiming the lighting from the vendor effect costs one to
    /// two seconds, so a short timeout turns that cost into a constant nuisance; an hour of
    /// ordinary typing, pauses included, now stays within a single session.
    /// </remarks>
    public TimeSpan IdleTimeout { get; init; } = TimeSpan.FromSeconds(60);

    public ColourScheme Scheme { get; init; } = ColourScheme.Default;

    public ShortcutCatalogue Shortcuts { get; init; } = DefaultShortcuts.Create();

    /// <summary>
    /// Profiles bound to particular applications. Defaults to the ones shipped with the build;
    /// the application replaces this with the user's library once settings are loaded.
    /// </summary>
    public ProfileCatalogue Profiles { get; init; } = ShippedProfiles.Create();

    /// <summary>Whether to watch which application is in front at all.</summary>
    public bool UseApplicationProfiles { get; init; } = true;

    /// <summary>
    /// How the lighting answers the typing, if at all.
    /// </summary>
    /// <remarks>
    /// <see cref="KeyEffectKind.None"/> is not merely an effect that does nothing: it is the one
    /// state in which the engine never asks which individual keys are down. Everything a
    /// keystroke effect needs to know is asked for only while one is chosen.
    /// </remarks>
    public KeyEffectKind Effect { get; init; } = KeyEffectKind.None;

    /// <summary>
    /// A state to display instead of the live keyboard, or <c>null</c> to follow the keyboard.
    /// Used by the window to preview a modifier layer without the user holding it down.
    /// </summary>
    public Core.Input.KeyboardState? OverrideState { get; init; }
}
