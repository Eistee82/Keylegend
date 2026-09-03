using Keylegend.Core.Devices;

namespace Keylegend.Core.Lighting.Effects;

/// <summary>
/// Which keystroke effect the lighting answers with, if any.
/// </summary>
/// <remarks>
/// The names are what lands in the settings file, spelled out rather than numbered — a
/// hand-edited <c>"Effect": "Ripple"</c> says what it means, and a number would not. Exactly one
/// is in force at a time, which is why there is a single choice here rather than a set.
/// </remarks>
public enum KeyEffectKind
{
    /// <summary>
    /// The lighting says nothing about the typing. The default, and the one state in which the
    /// individual keys are never polled at all.
    /// </summary>
    None,

    /// <summary>The struck key goes dark and comes back over a second.</summary>
    Fade,

    /// <summary>The struck key flares and falls straight back.</summary>
    Flash,

    /// <summary>The struck key stays bright and dies away after the release.</summary>
    Afterglow,

    /// <summary>A bright ring runs outward from the stroke.</summary>
    Ripple,

    /// <summary>A dark ring runs outward from the stroke.</summary>
    DarkWave,

    /// <summary>The stroke flares and shakes the keys around it.</summary>
    Impact,

    /// <summary>The stroke throws warm sparks onto keys nearby.</summary>
    Sparks,

    /// <summary>Keys warm as they are used and cool down again.</summary>
    Heat
}

/// <summary>Makes the effect a choice stands for.</summary>
public static class KeyEffects
{
    /// <summary>
    /// The effect for a choice, or <c>null</c> for <see cref="KeyEffectKind.None"/> — and
    /// <c>null</c> is what tells the engine not to poll the individual keys.
    /// </summary>
    /// <param name="keyboard">
    /// The attached keyboard. The travelling effects need to know where the keys sit; the others
    /// ignore it.
    /// </param>
    /// <param name="random">
    /// Where chance comes from, for the one effect that uses it. Handed in so a test can repeat
    /// itself.
    /// </param>
    public static IKeyEffect? Create(
        KeyEffectKind kind, AttachedKeyboard keyboard, Random? random = null)
    {
        ArgumentNullException.ThrowIfNull(keyboard);

        return kind switch
        {
            KeyEffectKind.Fade => new FadeEffect(),
            KeyEffectKind.Flash => new FlashEffect(),
            KeyEffectKind.Afterglow => new AfterglowEffect(),
            KeyEffectKind.Ripple => new RippleEffect(keyboard),
            KeyEffectKind.DarkWave => new DarkWaveEffect(keyboard),
            KeyEffectKind.Impact => new ImpactEffect(keyboard),
            KeyEffectKind.Sparks => new SparkEffect(keyboard, random),
            KeyEffectKind.Heat => new HeatEffect(),
            _ => null
        };
    }
}
