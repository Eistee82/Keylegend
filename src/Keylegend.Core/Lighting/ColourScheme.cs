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
            // Lavender rather than orange: orange sits on the line between red and yellow and
            // was too close to both. With eight groups the pure hues run out, so this one takes
            // a corner of the colour cube the others leave free.
            [FunctionGroup.Tools] = new(128, 128, 255),
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
}
