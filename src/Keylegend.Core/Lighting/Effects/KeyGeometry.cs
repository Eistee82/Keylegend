using Keylegend.Core.Devices;

namespace Keylegend.Core.Lighting.Effects;

/// <summary>
/// Where the keys sit, in the only unit an effect can sensibly use.
/// </summary>
/// <remarks>
/// Distances are measured in <em>key heights</em>, not in the drawing's own units. A ripple that
/// travels "six units a second" then looks the same on a full-size board and on a sixty-percent
/// one, where the same drawing distance is a different number of keys. This is the same choice
/// the preview makes when it sizes what has to look alike at every window size.
/// </remarks>
internal sealed class KeyGeometry
{
    private readonly Dictionary<string, (double X, double Y)> _centres = new(StringComparer.Ordinal);
    private readonly double _unit;

    public KeyGeometry(AttachedKeyboard keyboard)
    {
        ArgumentNullException.ThrowIfNull(keyboard);

        foreach (var key in keyboard.Keys)
        {
            _centres[key.Id] = (key.X + (key.Width / 2), key.Y + (key.Height / 2));
        }

        var smallest = keyboard.Keys.Count > 0 ? keyboard.Keys.Min(k => k.Height) : 0;
        _unit = smallest > 0 ? smallest : 1;

        // How far it is from one corner of the board to the other, in key heights. A wave is
        // given this to cross, so it sweeps the whole keyboard whatever keyboard it is — a
        // fixed speed crossed a tenkeyless and died half-way over a full-size one.
        var left = keyboard.Keys.Count > 0 ? keyboard.Keys.Min(k => k.X) : 0;
        var right = keyboard.Keys.Count > 0 ? keyboard.Keys.Max(k => k.X + k.Width) : 0;
        var top = keyboard.Keys.Count > 0 ? keyboard.Keys.Min(k => k.Y) : 0;
        var bottom = keyboard.Keys.Count > 0 ? keyboard.Keys.Max(k => k.Y + k.Height) : 0;

        var width = (right - left) / _unit;
        var height = (bottom - top) / _unit;

        Span = Math.Max(1, Math.Sqrt((width * width) + (height * height)));
    }

    /// <summary>Corner to corner, in key heights.</summary>
    public double Span { get; }

    /// <summary>The centre of a key, or <c>null</c> for a key this board does not have.</summary>
    public (double X, double Y)? Centre(string keyId)
        => _centres.TryGetValue(keyId, out var centre) ? centre : null;

    /// <summary>Every key on the board, by id and centre.</summary>
    public IEnumerable<KeyValuePair<string, (double X, double Y)>> All => _centres;

    /// <summary>How far apart two points are, in key heights.</summary>
    public double Distance((double X, double Y) from, (double X, double Y) to)
    {
        var dx = (from.X - to.X) / _unit;
        var dy = (from.Y - to.Y) / _unit;

        return Math.Sqrt((dx * dx) + (dy * dy));
    }
}
