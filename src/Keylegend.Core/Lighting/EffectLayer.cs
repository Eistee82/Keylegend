using Keylegend.Core.Devices;
using Keylegend.Core.Input;

namespace Keylegend.Core.Lighting;

/// <summary>
/// Lays a keystroke effect over the finished frame.
/// </summary>
/// <remarks>
/// <para>
/// After the composer, never inside it. The composer stays a pure function of the keyboard state,
/// which is what keeps the picture in the window and the light on the desk the same thing; an
/// effect is a change over time laid on top, not a second opinion about what a key means.
/// </para>
/// <para>
/// Two arithmetic rules live here rather than in the eight effects, so that they cannot drift
/// apart: dim first and mix the effect's own colour second, and clamp to what the hardware can
/// show exactly once, at the end.
/// </para>
/// </remarks>
public sealed class EffectLayer
{
    private readonly AttachedKeyboard _keyboard;

    private IKeyEffect? _effect;

    public EffectLayer(AttachedKeyboard keyboard)
        => _keyboard = keyboard ?? throw new ArgumentNullException(nameof(keyboard));

    /// <summary>
    /// The effect in force, or <c>null</c> for none. Changing it clears both the one being put
    /// down and the one being taken up, so a half-finished wave cannot run on into the next.
    /// </summary>
    public IKeyEffect? Effect
    {
        get => _effect;
        set
        {
            if (ReferenceEquals(_effect, value))
            {
                return;
            }

            _effect?.Reset();
            _effect = value;
            _effect?.Reset();
        }
    }

    /// <summary>Whether the effect still has something in flight.</summary>
    public bool Animating => _effect?.Animating ?? false;

    /// <summary>
    /// Carries the effect forward, without painting anything.
    /// </summary>
    /// <remarks>
    /// Apart from painting on purpose, and this is the whole reason: <see cref="Animating"/> is
    /// what decides whether a frame is sent at all, so it has to be answerable before anything is
    /// drawn. Folded into the painting, an effect could only ever announce itself on a frame that
    /// was being sent for some other reason — which made a keystroke wait up to three quarters of
    /// a second for the next insurance frame before the lighting answered it.
    /// </remarks>
    public void Advance(KeyActivity activity, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(activity);

        _effect?.Advance(activity, now);
    }

    /// <summary>Paints what the effect currently says into the frame.</summary>
    public void Paint(LedFrame frame, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(frame);

        if (_effect is not { } effect)
        {
            return;
        }

        foreach (var key in _keyboard.Keys)
        {
            // A key the drawing places but the lighting cannot address — the upper half of an
            // ISO Enter has no LED of its own on every board.
            if (key.Row is not { } row || key.Column is not { } column)
            {
                continue;
            }

            var tint = effect.TintFor(key, now);
            var painted = frame[row, column];

            frame.Set(row, column, Blend(painted, tint));
        }
    }

    private static RgbColor Blend(RgbColor painted, KeyTint tint)
    {
        // Only downward. A factor cannot brighten a colour that already runs a channel at 255
        // and the rest at 0 — see KeyTint, where that mistake is written down.
        var factor = Math.Clamp(tint.Factor, 0.0, 1.0);

        var r = painted.R * factor;
        var g = painted.G * factor;
        var b = painted.B * factor;

        if (tint.Colour is { } colour && tint.Mix > 0)
        {
            var mix = Math.Clamp(tint.Mix, 0.0, 1.0);

            r = (r * (1 - mix)) + (colour.R * mix);
            g = (g * (1 - mix)) + (colour.G * mix);
            b = (b * (1 - mix)) + (colour.B * mix);
        }

        return new RgbColor(Channel(r), Channel(g), Channel(b));
    }

    /// <summary>The one place the arithmetic is brought back to what the hardware can show.</summary>
    private static byte Channel(double value)
        => (byte)Math.Clamp(Math.Round(value), 0, 255);
}
