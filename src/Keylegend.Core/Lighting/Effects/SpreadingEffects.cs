using Keylegend.Core.Devices;
using Keylegend.Core.Input;

namespace Keylegend.Core.Lighting.Effects;

/// <summary>
/// The effects that travel: something begins where a key was struck and moves outward.
/// </summary>
/// <remarks>
/// Each press starts one of these and it lives for a fixed span, so what a key shows is the sum
/// of whatever is passing over it. Sums rather than a winner-takes-all, because two keys struck
/// in quick succession should read as two strokes crossing, not as the later one erasing the
/// earlier.
/// </remarks>
public abstract class SpreadingEffect : IKeyEffect
{
    private readonly List<(double X, double Y, DateTimeOffset At)> _started = [];

    private readonly KeyGeometry _geometry;

    protected SpreadingEffect(AttachedKeyboard keyboard)
        => _geometry = new KeyGeometry(keyboard);

    /// <summary>How far it is from one corner of the board to the other, in key heights.</summary>
    protected double Span => _geometry.Span;

    /// <summary>How long one stroke's worth of movement lasts.</summary>
    protected abstract TimeSpan Lasts { get; }

    public bool Animating => _started.Count > 0;

    public void Advance(KeyActivity activity, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(activity);

        _started.RemoveAll(s => now - s.At >= Lasts || now < s.At);

        foreach (var id in activity.JustPressed)
        {
            if (_geometry.Centre(id) is { } centre)
            {
                _started.Add((centre.X, centre.Y, now));
            }
        }
    }

    public KeyTint TintFor(KeyDefinition key, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(key);

        if (_started.Count == 0 || _geometry.Centre(key.Id) is not { } here)
        {
            return KeyTint.Untouched;
        }

        var total = 0.0;

        foreach (var (x, y, at) in _started)
        {
            var elapsed = now - at;

            if (elapsed < TimeSpan.Zero || elapsed >= Lasts)
            {
                continue;
            }

            total += Amount(_geometry.Distance(here, (x, y)), elapsed / Lasts);
        }

        return total <= 0 ? KeyTint.Untouched : Paint(total);
    }

    public void Reset() => _started.Clear();

    /// <summary>How strongly one stroke touches a key this far away, this far through its life.</summary>
    protected abstract double Amount(double distance, double through);

    /// <summary>What that strength looks like.</summary>
    protected abstract KeyTint Paint(double amount);
}

/// <summary>
/// A ring runs outward from the struck key and fades as it goes — a drop falling into the board.
/// </summary>
public abstract class WaveEffect(AttachedKeyboard keyboard) : SpreadingEffect(keyboard)
{
    /// <summary>
    /// How wide the ring is, in key heights. Narrow on purpose: at a key and a half the ring
    /// covered a third of the board at once and read as the whole keyboard breathing rather than
    /// as something travelling across it.
    /// </summary>
    private const double Width = 0.7;

    protected override TimeSpan Lasts => TimeSpan.FromSeconds(0.9);

    protected override double Amount(double distance, double through)
    {
        // The ring reaches the far corner exactly as its life ends, on any keyboard. A fixed
        // speed cannot: the same six key heights a second crosses a sixty-percent board and dies
        // half-way over a full-size one, which is what "it does not reach across the keyboard"
        // looked like.
        var radius = Span * through;
        var offset = (distance - radius) / Width;

        // A bell around the ring rather than a hard edge: the keys are far enough apart that a
        // sharp ring skips half of them and reads as flickering, not as travel.
        return Math.Exp(-0.5 * offset * offset) * (1 - (0.35 * through));
    }
}

/// <summary>The ring is light — white, because a key already at full colour has no brighter shade.</summary>
public sealed class RippleEffect(AttachedKeyboard keyboard) : WaveEffect(keyboard)
{
    protected override KeyTint Paint(double amount) => KeyTint.Lit(amount);
}

/// <summary>
/// The same ring, dark: the board parts around the stroke instead of lighting up with it.
/// </summary>
public sealed class DarkWaveEffect(AttachedKeyboard keyboard) : WaveEffect(keyboard)
{
    protected override KeyTint Paint(double amount) => KeyTint.Dimmed(1 - amount);
}

/// <summary>
/// The struck key flares and the keys around it answer a moment later — as though the stroke
/// shook the board.
/// </summary>
/// <remarks>
/// The near cousin of the ripple, and deliberately much smaller: it reaches two keys and is over
/// in a fifth of a second. That is what makes it the one effect that stays bearable at typing
/// speed, where a ripple per keystroke turns the board into weather.
/// </remarks>
public sealed class ImpactEffect(AttachedKeyboard keyboard) : SpreadingEffect(keyboard)
{
    /// <summary>Key heights the shock reaches. Beyond this a stroke is simply not felt.</summary>
    private const double Reach = 2.5;

    /// <summary>Seconds of delay per key height, so the answer travels outward rather than at once.</summary>
    private const double Delay = 0.02;

    protected override TimeSpan Lasts => TimeSpan.FromSeconds(0.2);

    protected override double Amount(double distance, double through)
    {
        if (distance > Reach)
        {
            return 0;
        }

        var elapsed = through * Lasts.TotalSeconds;
        var since = elapsed - (distance * Delay);

        if (since < 0)
        {
            return 0;
        }

        var window = Lasts.TotalSeconds - (Reach * Delay);
        var left = 1 - (since / window);

        return left <= 0 ? 0 : left / (1 + (1.5 * distance));
    }

    protected override KeyTint Paint(double amount) => KeyTint.Lit(amount);
}
