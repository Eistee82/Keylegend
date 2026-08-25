using Keylegend.Core.Input;
using Keylegend.Core.Shortcuts;

namespace Keylegend.Core.Lighting;

/// <summary>A lock key's two states.</summary>
public sealed record LockColours(RgbColor On, RgbColor Off);

/// <summary>
/// Every colour the composer can produce. Kept as data so the interface can edit it and so
/// the composer stays a pure function of its inputs.
/// </summary>
public sealed record ColourScheme
{
    public required IReadOnlyDictionary<KeyCategory, RgbColor> Categories { get; init; }

    public required IReadOnlyDictionary<FunctionGroup, RgbColor> Groups { get; init; }

    public required LockColours NumLock { get; init; }

    public required LockColours CapsLock { get; init; }

    public required LockColours ScrollLock { get; init; }

    /// <summary>Global brightness from 0 to 1, applied to every colour as the frame is built.</summary>
    public double Brightness { get; init; } = 1.0;

    public RgbColor For(KeyCategory category)
        => Categories.TryGetValue(category, out var colour) ? colour : RgbColor.Off;

    public RgbColor For(FunctionGroup group)
        => Groups.TryGetValue(group, out var colour) ? colour : RgbColor.Off;

    /// <summary>
    /// A readable starting point: cool colours for what you type, warm ones for what controls
    /// the machine, and distinctly separated hues per function group so that a shortcut layer
    /// reads as blocks rather than as noise.
    /// </summary>
    /// <remarks>
    /// Deliberately bright. An earlier, more tasteful palette turned out to be invisible in
    /// practice — against a vivid vendor effect, a muted blue simply does not register as a
    /// change. Legibility beats subtlety here, and anyone who disagrees can dim it.
    /// </remarks>
    /// <remarks>
    /// Every colour here runs at least one channel at 255, so the LEDs are driven as brightly
    /// as the hardware allows and hues stay fully saturated. Only three things are dim on
    /// purpose: unassigned keys (off), and the "off" state of the three lock keys, where the
    /// dimness *is* the information. <see cref="RunsAtFullBrightness"/> holds that rule, and a
    /// test enforces it — a palette that quietly loses brightness is hard to notice and easy to
    /// introduce.
    /// </remarks>
    public static ColourScheme Default { get; } = new()
    {
        Categories = new Dictionary<KeyCategory, RgbColor>
        {
            [KeyCategory.Unassigned] = RgbColor.Off,
            [KeyCategory.Digit] = new(0, 255, 255),        // cyan
            [KeyCategory.Lowercase] = new(0, 0, 255),      // blue
            [KeyCategory.Uppercase] = new(0, 255, 0),      // green — Shift changes the hue, not the shade
            [KeyCategory.Symbol] = new(255, 255, 0),       // yellow
            [KeyCategory.Control] = new(200, 0, 255),      // violet
            [KeyCategory.DeadKey] = new(255, 80, 0),       // orange
            [KeyCategory.FunctionKey] = new(255, 255, 255) // white — the top row reads as its own band
        },
        Groups = new Dictionary<FunctionGroup, RgbColor>
        {
            [FunctionGroup.Edit] = new(0, 255, 0),         // green
            [FunctionGroup.File] = new(0, 0, 255),         // blue
            [FunctionGroup.Search] = new(255, 255, 0),     // yellow
            [FunctionGroup.View] = new(255, 0, 255),       // magenta
            [FunctionGroup.Window] = new(0, 255, 255),     // cyan
            [FunctionGroup.System] = new(255, 0, 0),       // red
            // Orange, and it has to be a saturated one. Lavender was tried first, on the
            // reasoning that orange sits between red and yellow and would be confused with both.
            // On the hardware it turned out worse than that: lavender is a tinted white, and it
            // is white the eye compares it to — the function row is white, Navigation is white,
            // and a Tools key beside them read as "white, maybe". Saturated orange is told apart
            // from red and yellow at a glance; a pale blue is not told apart from white at all.
            [FunctionGroup.Tools] = new(255, 140, 0),
            [FunctionGroup.Navigation] = new(255, 255, 255) // white
        },
        NumLock = new LockColours(new RgbColor(0, 255, 0), new RgbColor(30, 30, 30)),
        CapsLock = new LockColours(new RgbColor(255, 0, 0), new RgbColor(30, 30, 30)),
        ScrollLock = new LockColours(new RgbColor(255, 255, 0), new RgbColor(30, 30, 30))
    };

    /// <summary>
    /// How far apart two colours are, as a plain distance in RGB. Crude compared with a
    /// perceptual measure, but enough to catch the failure that matters here: two categories
    /// that a person cannot tell apart on a keyboard from arm's length.
    /// </summary>
    public static double Distance(RgbColor a, RgbColor b)
    {
        double dr = a.R - b.R;
        double dg = a.G - b.G;
        double db = a.B - b.B;

        return Math.Sqrt(dr * dr + dg * dg + db * db);
    }

    /// <summary>
    /// The distance two category colours must keep. Reached by trying it: the first palette
    /// had digits, lowercase and uppercase all as shades of blue, roughly 85 apart, and they
    /// were indistinguishable in use.
    /// </summary>
    public const double MinimumCategoryDistance = 150;

    /// <summary>
    /// Whether a colour drives at least one channel to full. Used to keep the palette honest:
    /// anything meant to be visible should be as bright as the hardware permits, since global
    /// dimming is what <see cref="Brightness"/> is for.
    /// </summary>
    public static bool RunsAtFullBrightness(RgbColor colour)
        => colour.R == 255 || colour.G == 255 || colour.B == 255;

    /// <summary>How far a colour is from grey, 0 to 1.</summary>
    /// <remarks>
    /// The measure <see cref="Distance"/> lacks, and the reason a palette can pass that check and
    /// still be unreadable on the hardware. A pale blue sits 180 from white by distance — well
    /// past the minimum — and on a lit keycap it is simply a tinted white, because what the eye
    /// compares against white is saturation and not the size of a channel difference.
    /// </remarks>
    public static double Saturation(RgbColor colour)
    {
        var peak = Math.Max(colour.R, Math.Max(colour.G, colour.B));

        if (peak == 0)
        {
            return 0;
        }

        var floor = Math.Min(colour.R, Math.Min(colour.G, colour.B));

        return (peak - floor) / (double)peak;
    }

    /// <summary>The hue of a colour in degrees, 0 to 360. Meaningless for a grey.</summary>
    public static double Hue(RgbColor colour)
    {
        double r = colour.R / 255.0, g = colour.G / 255.0, b = colour.B / 255.0;

        var peak = Math.Max(r, Math.Max(g, b));
        var floor = Math.Min(r, Math.Min(g, b));
        var span = peak - floor;

        if (span < 0.0001)
        {
            return 0;
        }

        var hue = peak == r
            ? ((g - b) / span) % 6
            : peak == g
                ? ((b - r) / span) + 2
                : ((r - g) / span) + 4;

        hue *= 60;

        return hue < 0 ? hue + 360 : hue;
    }

    /// <summary>The shorter way round the colour wheel between two hues, in degrees.</summary>
    public static double HueDistance(RgbColor a, RgbColor b)
    {
        var difference = Math.Abs(Hue(a) - Hue(b)) % 360;

        return difference > 180 ? 360 - difference : difference;
    }

    /// <summary>
    /// The saturation a colour needs to read as a colour rather than as a tinted white.
    /// </summary>
    /// <remarks>
    /// Found at the keyboard rather than reasoned out: a group colour at 0.5 was reported as
    /// looking white beside the function row, which is white, and beside Navigation, which is also
    /// white. Everything meant to be a hue is at or near 1.0 now, and white is left to mean white.
    /// </remarks>
    public const double MinimumSaturation = 0.85;

    /// <summary>
    /// The hue separation two saturated colours need. Red, orange and yellow sit 27 to 33 degrees
    /// apart and are told apart at a glance; this is set just under that.
    /// </summary>
    public const double MinimumHueSeparation = 25;
}
