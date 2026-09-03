using Keylegend.Core.Devices;
using Keylegend.Core.Input;

namespace Keylegend.Core.Lighting.Effects;

/// <summary>
/// A stroke throws a few sparks onto keys nearby, which glow warm and go out.
/// </summary>
/// <remarks>
/// <para>
/// The one effect that brings a colour of its own, and the one with chance in it. The source of
/// that chance is handed in rather than made here, which is the only thing that makes it
/// testable at all: the same seed has to throw the same sparks.
/// </para>
/// <para>
/// Sparks land near the stroke and never on the struck key itself — the finger is already there,
/// and a spark under it would read as a flash rather than as something thrown.
/// </para>
/// </remarks>
public sealed class SparkEffect : IKeyEffect
{
    /// <summary>Key heights a spark can be thrown.</summary>
    private const double Reach = 2.5;

    private static readonly RgbColor Ember = new(255, 225, 170);

    private readonly KeyGeometry _geometry;
    private readonly Random _random;
    private readonly TimeSpan _life;
    private readonly int _most;

    private readonly List<(string Key, DateTimeOffset At)> _sparks = [];

    public SparkEffect(
        AttachedKeyboard keyboard,
        Random? random = null,
        TimeSpan? life = null,
        int most = 3)
    {
        _geometry = new KeyGeometry(keyboard);
        _random = random ?? new Random();
        _life = life ?? TimeSpan.FromSeconds(0.45);
        _most = most;
    }

    public bool Animating => _sparks.Count > 0;

    public void Advance(KeyActivity activity, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(activity);

        _sparks.RemoveAll(s => now - s.At >= _life || now < s.At);

        foreach (var id in activity.JustPressed)
        {
            if (_geometry.Centre(id) is not { } struck)
            {
                continue;
            }

            var within = _geometry.All
                .Where(k => !string.Equals(k.Key, id, StringComparison.Ordinal))
                .Where(k => _geometry.Distance(struck, k.Value) <= Reach)
                .Select(k => k.Key)
                .Order(StringComparer.Ordinal)
                .ToList();

            // Ordered before drawing so that the same seed lands the same sparks: a dictionary's
            // own order is not promised and would make this untestable.
            for (var thrown = 0; thrown < _most && within.Count > 0; thrown++)
            {
                var index = _random.Next(within.Count);
                _sparks.Add((within[index], now));
                within.RemoveAt(index);
            }
        }
    }

    public KeyTint TintFor(KeyDefinition key, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(key);

        var strongest = 0.0;

        foreach (var (id, at) in _sparks)
        {
            if (!string.Equals(id, key.Id, StringComparison.Ordinal))
            {
                continue;
            }

            var elapsed = now - at;

            if (elapsed < TimeSpan.Zero || elapsed >= _life)
            {
                continue;
            }

            strongest = Math.Max(strongest, 1 - (elapsed / _life));
        }

        // Brightest spark wins rather than the sum: two sparks on one key is one brighter spark,
        // not a key painted twice over.
        return strongest <= 0
            ? KeyTint.Untouched
            : new KeyTint(1.0, Ember, strongest);
    }

    public void Reset() => _sparks.Clear();
}

/// <summary>
/// Keys warm up as they are used and cool down again — the board slowly shows how you type.
/// </summary>
/// <remarks>
/// <para>
/// The only effect with a memory. It keeps one decaying number per key, so for a few seconds
/// there is a trace in memory of what was typed. Nothing is written anywhere and nothing outlives
/// the process, but it is more than "is this key down right now", and that is worth saying plainly
/// rather than leaving to be discovered.
/// </para>
/// <para>
/// Heat is mixed in as colour rather than brightness alone, because warmth that is only brighter
/// reads as a key that means something — and meaning is what the colours are for.
/// </para>
/// </remarks>
public sealed class HeatEffect : IKeyEffect
{
    private static readonly RgbColor Glow = new(255, 90, 0);

    /// <summary>Below this a key counts as cold, so the lighting can go quiet again.</summary>
    private const double Cold = 0.02;

    private readonly Dictionary<string, double> _heat = new(StringComparer.Ordinal);
    private readonly TimeSpan _halfLife;
    private readonly double _perStroke;

    private DateTimeOffset? _last;

    public HeatEffect(TimeSpan? halfLife = null, double perStroke = 0.34)
    {
        _halfLife = halfLife ?? TimeSpan.FromSeconds(4);
        _perStroke = perStroke;
    }

    public bool Animating => _heat.Count > 0;

    public void Advance(KeyActivity activity, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(activity);

        if (_last is { } last && now > last)
        {
            var kept = Math.Pow(0.5, (now - last) / _halfLife);

            foreach (var id in _heat.Keys.ToArray())
            {
                var left = _heat[id] * kept;

                if (left < Cold)
                {
                    _heat.Remove(id);
                }
                else
                {
                    _heat[id] = left;
                }
            }
        }

        _last = now;

        foreach (var id in activity.JustPressed)
        {
            _heat[id] = Math.Min(1.0, _heat.GetValueOrDefault(id) + _perStroke);
        }
    }

    public KeyTint TintFor(KeyDefinition key, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(key);

        return _heat.TryGetValue(key.Id, out var heat) && heat >= Cold
            ? new KeyTint(1.0, Glow, 0.8 * heat)
            : KeyTint.Untouched;
    }

    public void Reset()
    {
        _heat.Clear();
        _last = null;
    }
}
