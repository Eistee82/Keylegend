namespace Keylegend.Core.Input;

/// <summary>
/// Which keys are down, and since when — the ground the lighting effects stand on.
/// </summary>
/// <remarks>
/// <para>
/// Fed the set of keys that are down at this moment, and derives everything from that. Never from
/// counted edges: this program polls rather than hooking, so a release that happens while the
/// screen is locked or the foreground changes is simply never observed. An effect built on
/// counted presses and releases would leave such a key dark for ever; here a key that is not in
/// the set is up, and the next poll repairs whatever the last one missed.
/// </para>
/// <para>
/// Nothing here is written anywhere, and nothing outlives <see cref="Remembers"/> — long enough
/// for the slowest effect to finish with it, and no longer. It is asked for at all only while an
/// effect is selected; with none, nothing polls the individual keys in the first place.
/// </para>
/// </remarks>
public sealed class KeyActivity
{
    private sealed class Entry
    {
        public DateTimeOffset PressedAt;
        public DateTimeOffset? ReleasedAt;
        public DateTimeOffset Touched;
    }

    private readonly Dictionary<string, Entry> _keys = new(StringComparer.Ordinal);
    private readonly List<string> _justPressed = [];

    /// <param name="remembers">
    /// How long a key that is neither down nor recently released is kept. The default outlasts
    /// the longest shipped effect.
    /// </param>
    public KeyActivity(TimeSpan? remembers = null)
        => Remembers = remembers ?? TimeSpan.FromSeconds(10);

    /// <summary>How long a key nothing has touched is kept.</summary>
    public TimeSpan Remembers { get; }

    /// <summary>The presses that began in the latest round, and only those.</summary>
    /// <remarks>
    /// What an effect spawns from. A water drop falls once per press, not once per frame the key
    /// happens to be held.
    /// </remarks>
    public IReadOnlyList<string> JustPressed => _justPressed;

    /// <summary>Takes in the keys that are down right now.</summary>
    public void Observe(IReadOnlyCollection<string> down, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(down);

        _justPressed.Clear();

        foreach (var id in down)
        {
            if (_keys.TryGetValue(id, out var entry) && entry.ReleasedAt is null)
            {
                // Still the same press. The moment it began must not drift.
                entry.Touched = now;
                continue;
            }

            _keys[id] = new Entry { PressedAt = now, ReleasedAt = null, Touched = now };
            _justPressed.Add(id);
        }

        foreach (var (id, entry) in _keys)
        {
            if (entry.ReleasedAt is null && !down.Contains(id))
            {
                entry.ReleasedAt = now;
                entry.Touched = now;
            }
        }

        Forget(now);
    }

    /// <summary>
    /// Every key still within living memory, held or lately let go.
    /// </summary>
    /// <remarks>
    /// What an effect walks to decide whether anything is still moving, without having to be
    /// handed the whole keyboard for the question.
    /// </remarks>
    public IReadOnlyCollection<string> Known => _keys.Keys;

    /// <summary>Whether this key is down at the moment.</summary>
    public bool IsDown(string keyId)
        => _keys.TryGetValue(keyId, out var entry) && entry.ReleasedAt is null;

    /// <summary>
    /// When the current press began, or the last one if the key is up again. <c>null</c> for a
    /// key that has not been touched within living memory.
    /// </summary>
    public DateTimeOffset? PressedAt(string keyId)
        => _keys.TryGetValue(keyId, out var entry) ? entry.PressedAt : null;

    /// <summary>
    /// When the key last came up. <c>null</c> while it is down, and for a key never seen.
    /// </summary>
    public DateTimeOffset? ReleasedAt(string keyId)
        => _keys.TryGetValue(keyId, out var entry) ? entry.ReleasedAt : null;

    /// <summary>Starts again, for a change of effect.</summary>
    public void Clear()
    {
        _keys.Clear();
        _justPressed.Clear();
    }

    private void Forget(DateTimeOffset now)
    {
        // A key still down is never forgotten, however long it is held.
        foreach (var id in _keys
                     .Where(k => k.Value.ReleasedAt is not null && now - k.Value.Touched > Remembers)
                     .Select(k => k.Key)
                     .ToArray())
        {
            _keys.Remove(id);
        }
    }
}
