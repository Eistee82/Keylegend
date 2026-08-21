using System.Globalization;

namespace Keylegend.Core.Lighting;

/// <summary>
/// A colour as the rest of the program thinks about it: plain red, green and blue.
/// Conversion into the vendor's packing happens at the very edge, in <see cref="ToBgr"/>.
/// </summary>
public readonly record struct RgbColor(byte R, byte G, byte B)
{
    /// <summary>No light at all.</summary>
    public static RgbColor Off => new(0, 0, 0);

    /// <summary>
    /// Packs the colour the way the Chroma SDK expects it: blue in the high byte, red in the
    /// low one. Passing an RGB-packed value instead is the classic cause of swapped colours.
    /// </summary>
    public int ToBgr() => (B << 16) | (G << 8) | R;

    /// <summary>Applies a brightness factor. Values outside 0..1 are clamped.</summary>
    public RgbColor Scale(double factor)
    {
        var clamped = Math.Clamp(factor, 0.0, 1.0);

        return new RgbColor(
            (byte)Math.Round(R * clamped),
            (byte)Math.Round(G * clamped),
            (byte)Math.Round(B * clamped));
    }

    /// <summary>Parses <c>#RRGGBB</c> or <c>RRGGBB</c>.</summary>
    /// <exception cref="FormatException">The text is not a six-digit hex colour.</exception>
    public static RgbColor FromHex(string hex)
    {
        ArgumentNullException.ThrowIfNull(hex);

        var digits = hex.StartsWith('#') ? hex[1..] : hex;

        if (digits.Length != 6)
        {
            throw new FormatException($"Expected six hex digits, got '{hex}'.");
        }

        static byte Component(ReadOnlySpan<char> text) =>
            byte.TryParse(text, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var value)
                ? value
                : throw new FormatException($"'{text}' is not a hex byte.");

        return new RgbColor(
            Component(digits.AsSpan(0, 2)),
            Component(digits.AsSpan(2, 2)),
            Component(digits.AsSpan(4, 2)));
    }

    public override string ToString() => $"#{R:X2}{G:X2}{B:X2}";
}
