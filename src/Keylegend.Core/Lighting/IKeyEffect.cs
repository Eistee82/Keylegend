using Keylegend.Core.Devices;
using Keylegend.Core.Input;

namespace Keylegend.Core.Lighting;

/// <summary>
/// What an effect does to one key at one moment.
/// </summary>
/// <param name="Factor">
/// Brightness applied to the colour the key already has, from 0 to 1. It can only take light
/// away.
/// </param>
/// <param name="Colour">
/// A colour of the effect's own, or <c>null</c> where the effect only dims.
/// </param>
/// <param name="Mix">How much of <paramref name="Colour"/> to lay over the result, from 0 to 1.</param>
/// <remarks>
/// <para>
/// <strong>Brightening is not a factor, and cannot be.</strong> The shipped palette runs at least
/// one channel at 255 on every colour — that is deliberate, so the LEDs are driven as hard as the
/// hardware allows — and the other channels are usually at 0. Multiplying such a colour does
/// nothing whatever: 255 is already the ceiling and 0 stays 0 however large the factor. The first
/// version of these effects did exactly that, and four of the eight were invisible on a lit
/// keyboard.
/// </para>
/// <para>
/// So an effect that wants a key <em>brighter</em> mixes white into it: <see cref="Lit"/>. At full
/// strength the key is white at full brightness, which is the only thing that reads as brighter
/// than a saturated colour. What the key means is still legible either side of the flash, because
/// the mix falls back to nothing.
/// </para>
/// </remarks>
public readonly record struct KeyTint(double Factor, RgbColor? Colour, double Mix)
{
    /// <summary>White at full brightness — as bright as a key can be.</summary>
    public static RgbColor Brightest => new(255, 255, 255);

    /// <summary>The key as the composer left it.</summary>
    public static KeyTint Untouched => new(1.0, null, 0);

    /// <summary>
    /// Brighter, by mixing white in — the only way that shows on a key already at full colour.
    /// </summary>
    /// <param name="amount">0 leaves the key alone, 1 makes it white at full brightness.</param>
    public static KeyTint Lit(double amount)
    {
        var mix = Math.Clamp(amount, 0.0, 1.0);

        return mix <= 0 ? Untouched : new KeyTint(1.0, Brightest, mix);
    }

    /// <summary>Darker, by taking light away.</summary>
    public static KeyTint Dimmed(double factor) => new(Math.Clamp(factor, 0.0, 1.0), null, 0);
}

/// <summary>
/// A keystroke effect: how the lighting answers the typing, over time.
/// </summary>
/// <remarks>
/// <para>
/// Deliberately a pure function of events, geometry and time. An effect knows nothing about
/// Chroma, about the window, or about the clock of the real world — the moment is handed to it.
/// That is what makes all of them testable without hardware and without waiting: set the time,
/// ask for the value.
/// </para>
/// <para>
/// One effect runs at a time. There is no ordering between effects and no rule for blending two,
/// because there are never two.
/// </para>
/// </remarks>
public interface IKeyEffect
{
    /// <summary>
    /// Carries the effect's own state forward: takes in new presses, moves waves along, lets
    /// heat cool. Called once per frame, before any key is asked.
    /// </summary>
    void Advance(KeyActivity activity, DateTimeOffset now);

    /// <summary>What this key should look like now.</summary>
    KeyTint TintFor(KeyDefinition key, DateTimeOffset now);

    /// <summary>
    /// Whether anything is still moving. This alone decides whether frames keep being sent —
    /// an effect that has finished must say so, or the lighting would never go quiet again.
    /// </summary>
    bool Animating { get; }

    /// <summary>Throws away everything in flight, for a change of selection.</summary>
    void Reset();
}
