using Keylegend.Core.Devices;
using Keylegend.Core.Input;

namespace Keylegend.Core.Lighting.Effects;

/// <summary>
/// The effects that need nothing but the key itself: no geometry, no neighbours, no waves.
/// </summary>
/// <remarks>
/// Each is a curve over the time since a press or a release, and nothing else. They share this
/// file because they share that shape — read side by side, it is plain that "Faden" and
/// "Nachleuchten" are one idea pointed in opposite directions.
/// </remarks>
public abstract class PerKeyEffect : IKeyEffect
{
    private KeyActivity? _activity;

    public bool Animating { get; private set; }

    public void Advance(KeyActivity activity, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(activity);

        _activity = activity;
        Animating = false;

        foreach (var id in activity.Known)
        {
            if (StillMoving(activity, id, now))
            {
                Animating = true;
                return;
            }
        }
    }

    public KeyTint TintFor(KeyDefinition key, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(key);

        return _activity is { } activity ? Tint(activity, key.Id, now) : KeyTint.Untouched;
    }

    public void Reset()
    {
        _activity = null;
        Animating = false;
    }

    /// <summary>Whether this one key still has something happening to it.</summary>
    protected abstract bool StillMoving(KeyActivity activity, string keyId, DateTimeOffset now);

    protected abstract KeyTint Tint(KeyActivity activity, string keyId, DateTimeOffset now);

    /// <summary>How far through a span a moment is, or <c>null</c> if it is outside it.</summary>
    protected static double? Through(DateTimeOffset? from, DateTimeOffset now, TimeSpan span)
    {
        if (from is not { } start)
        {
            return null;
        }

        var elapsed = now - start;

        return elapsed >= TimeSpan.Zero && elapsed < span ? elapsed / span : null;
    }
}

/// <summary>
/// Press and the light goes out; let go and it comes back over a second.
/// </summary>
/// <remarks>
/// The key is dark for exactly as long as it is held, however long that is — a held key is one
/// whose light is off, not one part-way through a fade. Only the release starts the clock.
/// </remarks>
public sealed class FadeEffect(TimeSpan? duration = null) : PerKeyEffect
{
    private readonly TimeSpan _duration = duration ?? TimeSpan.FromSeconds(1);

    protected override bool StillMoving(KeyActivity activity, string keyId, DateTimeOffset now)
        => activity.IsDown(keyId) || Through(activity.ReleasedAt(keyId), now, _duration) is not null;

    protected override KeyTint Tint(KeyActivity activity, string keyId, DateTimeOffset now)
    {
        if (activity.IsDown(keyId))
        {
            return KeyTint.Dimmed(0);
        }

        return Through(activity.ReleasedAt(keyId), now, _duration) is { } through
            ? KeyTint.Dimmed(through)
            : KeyTint.Untouched;
    }
}

/// <summary>
/// The struck key goes white at full brightness and falls straight back into its own colour.
/// Short and sharp — the counterpart to the fade.
/// </summary>
/// <remarks>
/// White, and not "its own colour, brighter". A key showing pure blue is already at the ceiling
/// on the only channel it uses, so there is no brighter blue to go to; white is the one thing
/// that reads as brighter than a saturated colour.
/// </remarks>
public sealed class FlashEffect(TimeSpan? duration = null, double peak = 1.0) : PerKeyEffect
{
    private readonly TimeSpan _duration = duration ?? TimeSpan.FromSeconds(0.18);

    protected override bool StillMoving(KeyActivity activity, string keyId, DateTimeOffset now)
        => Through(activity.PressedAt(keyId), now, _duration) is not null;

    protected override KeyTint Tint(KeyActivity activity, string keyId, DateTimeOffset now)
        // Brightest at the stroke and straight back down: the light answers the finger, it does
        // not swell up behind it.
        => Through(activity.PressedAt(keyId), now, _duration) is { } through
            ? KeyTint.Lit(peak * (1 - through))
            : KeyTint.Untouched;
}

/// <summary>
/// The struck key stays bright while it is held and dies away once it is let go — the trail
/// typing leaves behind.
/// </summary>
public sealed class AfterglowEffect(TimeSpan? duration = null, double peak = 0.7) : PerKeyEffect
{
    private readonly TimeSpan _duration = duration ?? TimeSpan.FromSeconds(0.8);

    protected override bool StillMoving(KeyActivity activity, string keyId, DateTimeOffset now)
        => activity.IsDown(keyId) || Through(activity.ReleasedAt(keyId), now, _duration) is not null;

    protected override KeyTint Tint(KeyActivity activity, string keyId, DateTimeOffset now)
    {
        if (activity.IsDown(keyId))
        {
            return KeyTint.Lit(peak);
        }

        return Through(activity.ReleasedAt(keyId), now, _duration) is { } through
            ? KeyTint.Lit(peak * (1 - through))
            : KeyTint.Untouched;
    }
}
