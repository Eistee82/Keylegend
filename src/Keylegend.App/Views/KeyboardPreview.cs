using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using Keylegend.App.Localisation;
using Keylegend.Core.Devices;
using Keylegend.Core.Lighting;

namespace Keylegend.App.Views;

/// <summary>
/// Draws the keyboard from a device profile and fills it from an <see cref="LedFrame"/>.
/// </summary>
/// <remarks>
/// Nothing here knows about any particular keyboard: geometry and colours both come from data,
/// so a contributed device profile shows up correctly without a line of code. And because the
/// frame it displays is produced by the same composer that drives the hardware, the preview
/// cannot drift out of step with what actually lights up.
/// </remarks>
public sealed class KeyboardPreview : FrameworkElement
{
    private static readonly Typeface LabelFace = new("Segoe UI");

    // Dark board, dark keycaps: the colour in this control comes from the lighting, not from
    // the furniture, exactly as on the keyboard itself.
    private static readonly Brush Background = new SolidColorBrush(Color.FromRgb(10, 10, 13));
    private static readonly Brush KeycapFill = new LinearGradientBrush(
        Color.FromRgb(42, 42, 48), Color.FromRgb(26, 26, 31), 90);
    private static readonly Pen KeycapEdge = new(new SolidColorBrush(Color.FromRgb(58, 58, 66)), 1);
    private static readonly Pen UnmappedPen = new(new SolidColorBrush(Color.FromRgb(90, 90, 100)), 1.4)
    {
        DashStyle = DashStyles.Dash
    };

    /// <summary>
    /// Prints the control's measured size and scale over the drawing. Off by default; switched
    /// on with the KEYLEGEND_LAYOUT_DEBUG environment variable when a layout misbehaves.
    /// </summary>
    private static readonly bool ShowLayoutDiagnostics =
        Environment.GetEnvironmentVariable("KEYLEGEND_LAYOUT_DEBUG") == "1";

    private DeviceProfile? _profile;
    private LedFrame? _frame;
    private readonly Dictionary<string, KeyLegend> _labels = [];
    private readonly Dictionary<string, string> _tips = [];
    private readonly ToolTip _tip = new()
    {
        Placement = PlacementMode.Mouse,
        StaysOpen = true,
        Background = new SolidColorBrush(Color.FromRgb(0x24, 0x24, 0x2C)),
        Foreground = new SolidColorBrush(Color.FromRgb(0xE8, 0xE8, 0xEE)),
        BorderBrush = new SolidColorBrush(Color.FromRgb(0x3A, 0x3A, 0x46)),
        FontFamily = new FontFamily("Segoe UI"),
        Padding = new Thickness(8, 4, 8, 4)
    };

    private string? _hovered;

    static KeyboardPreview()
    {
        Background.Freeze();
        KeycapFill.Freeze();
        KeycapEdge.Freeze();
        UnmappedPen.Freeze();
    }

    public KeyboardPreview()
    {
        _tip.PlacementTarget = this;
    }

    /// <summary>The keyboard to draw.</summary>
    public DeviceProfile? Profile
    {
        get => _profile;
        set
        {
            _profile = value;
            InvalidateVisual();
        }
    }

    /// <summary>Raised when a key is clicked, used to assign a highlight.</summary>
    public event Action<KeyDefinition>? KeyClicked;

    /// <summary>Raised when a key is right-clicked, used to clear a highlight.</summary>
    public event Action<KeyDefinition>? KeyRightClicked;

    /// <summary>
    /// Raised as the pointer moves onto a key, and with <c>null</c> when it leaves the keys.
    /// </summary>
    public event Action<KeyDefinition?>? KeyHovered;

    /// <summary>
    /// Sets what each key means in the view being shown, keyed by key id.
    /// </summary>
    /// <remarks>
    /// Shown as a tooltip. The LEDs can only say <em>that</em> a key carries a command, never
    /// what it does; the labels a profile carries are the answer to that, and this is where they
    /// become visible.
    /// </remarks>
    public void SetTips(IReadOnlyDictionary<string, string> tips)
    {
        _tips.Clear();

        foreach (var (id, tip) in tips)
        {
            _tips[id] = tip;
        }

        // What is under the pointer may well have just changed its meaning.
        _hovered = null;
        _tip.IsOpen = false;
    }

    /// <summary>Shows a frame. Cheap enough to call at the engine's frame rate.</summary>
    public void Update(LedFrame frame)
    {
        _frame = frame;
        InvalidateVisual();
    }

    /// <summary>
    /// Sets what is printed on each key, keyed by key id.
    /// </summary>
    public void SetLabels(IReadOnlyDictionary<string, KeyLegend> legends)
    {
        _labels.Clear();

        foreach (var (id, legend) in legends)
        {
            _labels[id] = legend;
        }

        InvalidateVisual();
    }

    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonUp(e);
        Hit(e, KeyClicked);
    }

    protected override void OnMouseRightButtonUp(MouseButtonEventArgs e)
    {
        base.OnMouseRightButtonUp(e);
        Hit(e, KeyRightClicked);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);

        var key = KeyAt(e.GetPosition(this));

        if (string.Equals(key?.Id, _hovered, StringComparison.Ordinal))
        {
            return;      // still the same key; reopening the tooltip would only make it flicker
        }

        _hovered = key?.Id;
        KeyHovered?.Invoke(key);

        // Closed and reopened rather than left to the tooltip service: the service knows only
        // about the control as a whole, and would keep showing the first key's text while the
        // pointer moved across the rest of the keyboard.
        _tip.IsOpen = false;

        if (key is not null && _tips.TryGetValue(key.Id, out var text))
        {
            _tip.Content = text;
            _tip.IsOpen = true;
        }
    }

    protected override void OnMouseLeave(MouseEventArgs e)
    {
        base.OnMouseLeave(e);

        _hovered = null;
        _tip.IsOpen = false;
        KeyHovered?.Invoke(null);
    }

    private void Hit(MouseButtonEventArgs e, Action<KeyDefinition>? notify)
    {
        if (notify is null || KeyAt(e.GetPosition(this)) is not { } key)
        {
            return;
        }

        notify(key);
    }

    private KeyDefinition? KeyAt(Point position)
    {
        if (_profile is null)
        {
            return null;
        }

        var (scale, offsetX, offsetY) = Layout(_profile);

        foreach (var key in _profile.Keys)
        {
            // Every area counts, so clicking either half of an L-shaped key hits it.
            foreach (var area in key.Areas())
            {
                var rect = new Rect(
                    offsetX + area.X * scale,
                    offsetY + area.Y * scale,
                    area.Width * scale,
                    area.Height * scale);

                if (rect.Contains(position))
                {
                    return key;
                }
            }
        }

        return null;
    }

    protected override void OnRender(DrawingContext context)
    {
        context.DrawRectangle(Background, pen: null, new Rect(RenderSize));

        if (_profile is null)
        {
            DrawCentredNotice(context, Texts.Get("KeyboardNoProfile"));
            return;
        }

        var (scale, offsetX, offsetY) = Layout(_profile);
        var radius = Math.Max(2, 3 * scale / 5);
        var type = TypeSizes(_profile, scale);

        if (ShowLayoutDiagnostics)
        {
            var report = new FormattedText(
                $"render {RenderSize.Width:N0}x{RenderSize.Height:N0} · scale {scale:N2} · " +
                $"keyboard {_profile.Canvas.Width * scale:N0} wide",
                CultureInfo.InvariantCulture, FlowDirection.LeftToRight, LabelFace, 12,
                Brushes.Yellow, VisualTreeHelper.GetDpi(this).PixelsPerDip);

            context.DrawText(report, new Point(6, 4));
        }

        // Two passes: every glow first, then every keycap. Otherwise a bright key's halo would
        // be painted over the neighbour drawn after it, and the light would look clipped.
        foreach (var key in _profile.Keys)
        {
            var colour = ColourOf(key);

            if (colour != RgbColor.Off)
            {
                DrawGlow(context, OutlineOf(key, scale, offsetX, offsetY, radius), colour);
            }
        }

        foreach (var key in _profile.Keys)
        {
            var mapped = key.Row.HasValue && key.Column.HasValue;
            var colour = ColourOf(key);
            var geometry = OutlineOf(key, scale, offsetX, offsetY, radius);

            // The keycap itself stays dark, as it physically is: the LED sits underneath and
            // shines around the cap and through the legend. Filling the whole key with the LED
            // colour - which this did at first - looks nothing like the keyboard.
            context.DrawGeometry(KeycapFill, mapped ? KeycapEdge : UnmappedPen, geometry);

            // Labels go on the main area; on an L-shaped key that is the part with the legend.
            DrawLabel(
                context, key,
                Scaled(new KeyArea(key.X, key.Y, key.Width, key.Height), scale, offsetX, offsetY),
                colour, type);
        }

        RgbColor ColourOf(KeyDefinition key)
            => key.Row is { } row && key.Column is { } column && _frame is not null
                ? _frame[row, column]
                : RgbColor.Off;
    }

    /// <summary>
    /// Paints the light spilling out from under a keycap.
    /// </summary>
    /// <remarks>
    /// Three expanding outlines at falling opacity rather than a blur effect: a blur on a
    /// hundred keys at twenty frames a second is not worth its cost, and at this size the
    /// stepped version is indistinguishable from a soft one.
    /// </remarks>
    private static void DrawGlow(DrawingContext context, Geometry outline, RgbColor colour)
    {
        var bounds = outline.Bounds;

        if (bounds.IsEmpty)
        {
            return;
        }

        ReadOnlySpan<(double Spread, byte Alpha)> layers =
        [
            (5.0, 26),
            (2.5, 46),
            (0.8, 92)
        ];

        foreach (var (spread, alpha) in layers)
        {
            var brush = new SolidColorBrush(Color.FromArgb(alpha, colour.R, colour.G, colour.B));
            brush.Freeze();

            var halo = new Rect(
                bounds.X - spread, bounds.Y - spread,
                bounds.Width + spread * 2, bounds.Height + spread * 2);

            context.DrawRoundedRectangle(brush, pen: null, halo, 4 + spread, 4 + spread);
        }
    }

    /// <summary>The three type sizes used across the whole keyboard.</summary>
    private readonly record struct TypeScale(double Glyph, double Word, double Small);

    /// <summary>
    /// Works out the type sizes once for the entire keyboard, from the size of an ordinary key.
    /// </summary>
    /// <remarks>
    /// Deriving the size from each key's own dimensions - as this did at first - makes the
    /// space bar shout and the arrow keys whisper, which looks nothing like a keyboard. Real
    /// keycaps are printed at one size regardless of how wide the key is, with words set
    /// smaller than single characters. The only per-key adjustment left is shrinking a legend
    /// that genuinely will not fit.
    /// </remarks>
    private static TypeScale TypeSizes(DeviceProfile profile, double scale)
    {
        // An ordinary key: the shortest one, since every key is at least one unit tall.
        var unit = profile.Keys.Count > 0 ? profile.Keys.Min(k => k.Height) : 19;
        var glyph = unit * scale * 0.46;

        // The ratios come from measuring the product photograph against screenshots of this
        // control. A first attempt at 0.55 for secondary characters left the shifted symbols
        // barely visible, which defeats the point of printing them at all.
        return new TypeScale(glyph, glyph * 0.62, glyph * 0.64);
    }

    /// <summary>
    /// Builds the outline of a key, merging its areas into a single shape.
    /// </summary>
    /// <remarks>
    /// Keys are not always rectangles — the ISO Enter is one key covering two rows. Drawing the
    /// areas separately would show a seam across the middle of a key that has none, so they are
    /// combined into one outline and stroked once. The areas are grown slightly before merging,
    /// otherwise two rectangles that merely touch leave a hairline where they meet.
    /// </remarks>
    private static Geometry OutlineOf(
        KeyDefinition key, double scale, double offsetX, double offsetY, double radius)
    {
        var areas = key.Areas().Select(a => Scaled(a, scale, offsetX, offsetY)).ToArray();

        if (areas.Length == 1)
        {
            return new RectangleGeometry(areas[0], radius, radius);
        }

        // Two stacked areas is the ISO Enter, and the only composed shape in practice. Tracing
        // its outline gives a proper stepped key with a sharp inner corner; merging two rounded
        // rectangles instead leaves that corner bulging outwards, which no keycap does.
        if (areas.Length == 2 && TryStepOutline(areas[0], areas[1], radius) is { } stepped)
        {
            return stepped;
        }

        // Anything more unusual falls back to a union. Not beautiful, but correct, and it means
        // an exotic contributed profile still draws rather than failing.
        Geometry? combined = null;

        foreach (var rect in areas)
        {
            var inflated = rect;
            inflated.Inflate(0, 1.0);

            var piece = new RectangleGeometry(inflated, radius, radius);
            combined = combined is null
                ? piece
                : Geometry.Combine(combined, piece, GeometryCombineMode.Union, transform: null);
        }

        return combined ?? Geometry.Empty;
    }

    /// <summary>
    /// Traces the outline of two vertically adjacent rectangles as one six-cornered shape,
    /// rounding every corner - including the inner one, which curves the other way.
    /// </summary>
    /// <returns><c>null</c> if the two are not simply stacked.</returns>
    private static Geometry? TryStepOutline(Rect a, Rect b, double radius)
    {
        var (upper, lower) = a.Y <= b.Y ? (a, b) : (b, a);

        // They must meet, and the step must be sideways - otherwise this is not the shape.
        if (Math.Abs(lower.Y - upper.Bottom) > 4 || Math.Abs(upper.X - lower.X) < 0.5)
        {
            return null;
        }

        var y = (upper.Bottom + lower.Y) / 2;      // one shared edge, not two with a gap

        Point[] corners = upper.X < lower.X
            ? // wider at the top, stepping in on the left - the ISO Enter
            [
                new(upper.X, upper.Y),
                new(Math.Max(upper.Right, lower.Right), upper.Y),
                new(Math.Max(upper.Right, lower.Right), lower.Bottom),
                new(lower.X, lower.Bottom),
                new(lower.X, y),
                new(upper.X, y)
            ]
            : // wider at the bottom
            [
                new(upper.X, upper.Y),
                new(upper.Right, upper.Y),
                new(upper.Right, y),
                new(lower.Right, y),
                new(lower.Right, lower.Bottom),
                new(lower.X, lower.Bottom)
            ];

        return RoundedPolygon(corners, radius);
    }

    /// <summary>
    /// Builds a closed outline through the given corners, rounding each one.
    /// </summary>
    /// <remarks>
    /// The turn direction is worked out per corner from the cross product, so an inner corner
    /// curves inwards and an outer one outwards. Rounding everything the same way is what makes
    /// a stepped shape look inflated at the notch.
    /// </remarks>
    private static Geometry RoundedPolygon(IReadOnlyList<Point> corners, double radius)
    {
        var geometry = new StreamGeometry();

        using (var context = geometry.Open())
        {
            var count = corners.Count;
            var started = false;

            for (var i = 0; i < count; i++)
            {
                var previous = corners[(i - 1 + count) % count];
                var current = corners[i];
                var next = corners[(i + 1) % count];

                // Never round more than half of the shorter adjoining edge, or the curves of
                // two close corners would overlap and the outline would fold in on itself.
                var r = Math.Min(radius, Math.Min(Distance(previous, current), Distance(current, next)) / 2);

                var entry = Towards(current, previous, r);
                var exit = Towards(current, next, r);

                if (!started)
                {
                    context.BeginFigure(entry, isFilled: true, isClosed: true);
                    started = true;
                }
                else
                {
                    context.LineTo(entry, isStroked: true, isSmoothJoin: true);
                }

                if (r > 0.01)
                {
                    var cross = ((current.X - previous.X) * (next.Y - current.Y))
                              - ((current.Y - previous.Y) * (next.X - current.X));

                    context.ArcTo(
                        exit,
                        new Size(r, r),
                        rotationAngle: 0,
                        isLargeArc: false,
                        cross > 0 ? SweepDirection.Clockwise : SweepDirection.Counterclockwise,
                        isStroked: true,
                        isSmoothJoin: true);
                }
            }
        }

        geometry.Freeze();

        return geometry;

        static double Distance(Point from, Point to)
            => Math.Sqrt(((to.X - from.X) * (to.X - from.X)) + ((to.Y - from.Y) * (to.Y - from.Y)));

        static Point Towards(Point from, Point to, double distance)
        {
            var length = Distance(from, to);

            return length < 0.001
                ? from
                : new Point(
                    from.X + ((to.X - from.X) / length * distance),
                    from.Y + ((to.Y - from.Y) / length * distance));
        }
    }

    /// <summary>
    /// Where a single character sits vertically, as a fraction of the key's height. One value
    /// for every key, so a letter with an AltGr character lines up with the letters next to it.
    /// </summary>
        /// <summary>Baseline of the main character on a key carrying secondary legends.</summary>
    private const double MainBaseline = 0.47;

    /// <summary>Baseline of the secondary legends in the lower corners.</summary>
    private const double SecondaryBaseline = 0.90;

    private static readonly Brush DarkInk = Frozen(Color.FromRgb(96, 96, 106));
    private static readonly Brush FaintDarkInk = Frozen(Color.FromRgb(70, 70, 78));

    private static Brush Frozen(Color colour)
    {
        var brush = new SolidColorBrush(colour);
        brush.Freeze();

        return brush;
    }

    /// <summary>
    /// The colour a lit legend is drawn in: the key's own colour, lifted towards white so it
    /// reads as light passing through the cap rather than as paint on top of it.
    /// </summary>
    private static Brush Glow(RgbColor colour, double strength)
    {
        static byte Lift(byte channel, double amount)
            => (byte)Math.Clamp(channel + (255 - channel) * amount, 0, 255);

        var lifted = Color.FromRgb(
            Lift(colour.R, 0.55 * strength),
            Lift(colour.G, 0.55 * strength),
            Lift(colour.B, 0.55 * strength));

        var brush = new SolidColorBrush(strength < 1.0
            ? Color.FromArgb(205, lifted.R, lifted.G, lifted.B)
            : lifted);

        brush.Freeze();

        return brush;
    }

    private static Rect Scaled(KeyArea area, double scale, double offsetX, double offsetY)
        => new(
            offsetX + area.X * scale + 1,
            offsetY + area.Y * scale + 1,
            Math.Max(1, area.Width * scale - 2),
            Math.Max(1, area.Height * scale - 2));

    /// <summary>
    /// Draws a key's legend the way a real keycap carries it: the unmodified character large,
    /// the shifted one small above it, and the AltGr one small at the bottom right — which is
    /// exactly where they sit on a German keyboard.
    /// </summary>
    private void DrawLabel(
        DrawingContext context, KeyDefinition key, Rect rect, RgbColor colour, TypeScale type)
    {
        if (!_labels.TryGetValue(key.Id, out var legend) || legend.IsEmpty)
        {
            return;
        }

        // The legend is lit from below, so it carries the key's colour rather than being black
        // or white on top of it - which is what makes a backlit keyboard look the way it does.
        // Unlit keys keep a faint grey legend, as an unlit keycap still shows its printing.
        var lit = colour != RgbColor.Off;
        var ink = lit ? Glow(colour, 1.0) : DarkInk;
        var faint = lit ? Glow(colour, 0.62) : FaintDarkInk;

        var dpi = VisualTreeHelper.GetDpi(this).PixelsPerDip;

        // Secondary characters only where there is room for them. On a small key three legends
        // would each be too small to read, and one legible character beats three illegible ones.
        var roomForSecondary = rect.Height >= 26 && rect.Width >= 24;
        var hasSecondary = roomForSecondary && (legend.Shifted is not null || legend.AltGr is not null);

        // One size for the whole keyboard: words smaller than single characters, exactly as
        // keycaps are printed. Only the fitting below varies it, and only where a legend would
        // otherwise run off the key.
        var mainSize = legend.Main.Length > 1 ? type.Word : type.Glyph;
        if (mainSize < 5.5)
        {
            return;      // too small to read; drawing it would only be noise
        }

        if (!hasSecondary)
        {
            // Word labels such as "pause" or "pos 1" are wider than a single character, so the
            // size is reduced until it fits rather than being allowed to run over the edge.
            var only = Fit(legend.Main, mainSize, rect.Width * 0.86, ink);

            // A character sits on the same baseline as on every other key, whether or not this
            // one happens to carry a second legend. Otherwise Q, E and M - the letters with an
            // AltGr character - would ride higher than the letters beside them. Words are
            // centred, having nothing to line up with.
            var y = legend.Main.Length > 1
                ? rect.Y + (rect.Height - only.Height) / 2
                : rect.Y + rect.Height * MainBaseline - only.Baseline;

            context.DrawText(only, new Point(rect.X + (rect.Width - only.Width) / 2, y));

            return;
        }

        var smallSize = type.Small;

        // Punctuation keys print their two characters beside each other - ", ;" and "# '" -
        // rather than stacked, both at the same size.
        if (legend.SideBySide && legend.Shifted is { } beside)
        {
            // Both at the word size: on the keyboard these two are printed the same size as
            // each other and larger than a corner legend, since neither is subordinate.
            var left = Fit(legend.Main, type.Word, rect.Width * 0.4, ink);
            var right = Fit(beside, type.Word, rect.Width * 0.4, faint);
            var gap = rect.Width * 0.12;
            var total = left.Width + gap + right.Width;
            var x = rect.X + (rect.Width - total) / 2;
            var baseline = rect.Y + (rect.Height - left.Height) / 2;

            context.DrawText(left, new Point(x, baseline));
            context.DrawText(right, new Point(x + left.Width + gap, baseline));

            return;
        }

        // Two stacked lines: "druck" over "s-abf", or a number pad digit over its navigation
        // name.
        if (legend.AltGr is null && (legend.Main.Length > 1 || legend.Shifted!.Length > 1))
        {
            var upperWord = Fit(legend.Main, mainSize, rect.Width * 0.88, ink);
            var lowerWord = Fit(legend.Shifted!, smallSize, rect.Width * 0.88, faint);
            var stack = upperWord.Height + lowerWord.Height;
            var top = rect.Y + (rect.Height - stack) / 2;

            // Two words are centred; a digit with a word beneath it is set to the left, which
            // is how the number pad is printed - "7" over "pos 1" both aligned to the left edge
            // rather than floating in the middle of the key.
            var leftAligned = legend.Main.Length == 1;
            var inset = rect.Width * 0.16;

            var upperX = leftAligned
                ? rect.X + inset
                : rect.X + (rect.Width - upperWord.Width) / 2;
            var lowerX = leftAligned
                ? rect.X + inset
                : rect.X + (rect.Width - lowerWord.Width) / 2;

            context.DrawText(upperWord, new Point(upperX, top));
            context.DrawText(lowerWord, new Point(lowerX, top + upperWord.Height));

            return;
        }

        var half = rect.Width * 0.44;

        // Positioned by baseline rather than by the text block's top edge. A FormattedText's
        // Height is the full line height, most of which is empty space above and below the
        // glyph, so placing blocks by it made the main and secondary characters collide as soon
        // as the type got larger.
        var mainBaseline = rect.Y + rect.Height * MainBaseline;
        var lowerBaseline = rect.Y + rect.Height * SecondaryBaseline;

        // The main character is centred - on these keycaps even the digits are, with only the
        // secondary characters pushed into the lower corners: 7 sits centred above / and {.
        var main = Fit(legend.Main, mainSize, rect.Width * 0.6, ink);
        context.DrawText(main, new Point(
            rect.X + (rect.Width - main.Width) / 2,
            mainBaseline - main.Baseline));

        if (legend.Shifted is { } shifted)
        {
            var lower = Fit(shifted, smallSize, half, faint);
            context.DrawText(lower, new Point(
                rect.X + rect.Width * 0.16,
                lowerBaseline - lower.Baseline));
        }

        if (legend.AltGr is { } altGr)
        {
            var corner = Fit(altGr, smallSize, half, faint);

            // The right-hand corner is where AltGr goes when a shifted character occupies the
            // left one - as on the 7, carrying / and {. With no shifted character there is
            // nothing to sit beside, and the keyboard prints it on the left instead: Q with @,
            // E with the euro sign.
            var x = legend.Shifted is null
                ? rect.X + rect.Width * 0.16
                : rect.X + rect.Width - corner.Width - rect.Width * 0.16;

            context.DrawText(corner, new Point(x, lowerBaseline - corner.Baseline));
        }

        /// <summary>Lays out text, shrinking it if it would exceed the allowed width.</summary>
        FormattedText Fit(string value, double size, double maxWidth, Brush brush)
        {
            var text = Build(value, size, brush);

            if (text.Width <= maxWidth || text.Width <= 0)
            {
                return text;
            }

            var reduced = Math.Max(5.0, size * maxWidth / text.Width);

            return Build(value, reduced, brush);
        }

        FormattedText Build(string value, double size, Brush brush) => new(
            value, CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
            LabelFace, size, brush, dpi);
    }

    private void DrawCentredNotice(DrawingContext context, string message)
    {
        var text = new FormattedText(
            message,
            CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            LabelFace,
            14,
            Brushes.Gray,
            VisualTreeHelper.GetDpi(this).PixelsPerDip);

        context.DrawText(
            text,
            new Point((RenderSize.Width - text.Width) / 2, (RenderSize.Height - text.Height) / 2));
    }

    /// <summary>Fits the profile's canvas into the control, keeping its proportions.</summary>
    private (double Scale, double OffsetX, double OffsetY) Layout(DeviceProfile profile)
    {
        if (profile.Canvas.Width <= 0 || profile.Canvas.Height <= 0)
        {
            return (1, 0, 0);
        }

        const double margin = 8;
        var available = new Size(
            Math.Max(1, RenderSize.Width - 2 * margin),
            Math.Max(1, RenderSize.Height - 2 * margin));

        var scale = Math.Min(
            available.Width / profile.Canvas.Width,
            available.Height / profile.Canvas.Height);

        var offsetX = (RenderSize.Width - profile.Canvas.Width * scale) / 2;
        var offsetY = (RenderSize.Height - profile.Canvas.Height * scale) / 2;

        return (scale, offsetX, offsetY);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        // Ask for nothing and take what the layout gives. Reporting a desired size - even the
        // size just offered - lets this control drive its container's size, and the container
        // then grew wider and taller than the window: the number pad ended up outside the right
        // edge and the modifier bar below the bottom one. Proportions are preserved when
        // drawing, which is where that belongs.
        return new Size(0, 0);
    }
}

