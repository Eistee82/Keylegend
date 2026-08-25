using System.Globalization;
using System.Text.RegularExpressions;

namespace Keylegend.Core.Devices;

/// <summary>One key as the drawing describes it: where it sits and what it is called.</summary>
/// <param name="Parts">
/// The further rectangles of a key that is not one rectangle, the main one being <see cref="X"/>
/// and its neighbours. Only the ISO Enter has any: the drawing gives it an L-shaped outline, and
/// the outline is split back into rectangles so that the step lands exactly where it is drawn
/// rather than wherever scaling would put it.
/// </param>
public sealed record SvgKey(
    string Name,
    double X,
    double Y,
    double Width,
    double Height,
    string? Group,
    IReadOnlyList<SvgRect>? Parts = null)
{
    /// <summary>Every rectangle this key occupies, the main one first.</summary>
    public IEnumerable<SvgRect> Rectangles()
    {
        yield return new SvgRect(X, Y, Width, Height);

        foreach (var part in Parts ?? [])
        {
            yield return part;
        }
    }
}

/// <summary>A rectangle in the drawing's own coordinates.</summary>
public readonly record struct SvgRect(double X, double Y, double Width, double Height);

/// <summary>How prominent a shape of the casing is, so the program can colour it its own way.</summary>
public enum SvgChassisLayer
{
    /// <summary>The body of the case — the largest shape, furthest back.</summary>
    Body,

    /// <summary>A raised detail: the volume dial, the media strip, the vendor's wordmark.</summary>
    Raised,

    /// <summary>A recessed or shaded detail.</summary>
    Recessed
}

/// <summary>One shape of the keyboard's casing, in the drawing's own coordinates.</summary>
/// <remarks>
/// Kept as an outline and nothing else. The vendor's own fills are read only to tell which shapes
/// sit on top of which — the colours here are the program's, so the keyboard looks like the rest
/// of it rather than like somebody else's software.
/// </remarks>
public sealed record SvgChassisShape(string Path, SvgChassisLayer Layer, SvgRect Bounds);

/// <summary>
/// The keyboard drawing the vendor's own software uses, read for the parts that are facts rather
/// than design: where each key sits, how large it is, and the outlines of the legends printed on
/// it.
/// </summary>
/// <remarks>
/// <para>
/// This is what makes a keyboard that nobody drew by hand appear correctly — with its macro
/// column, its dial, its own key sizes — instead of as a stand-in of the right general shape.
/// The legends come with it, in the right language for the layout, which is otherwise the one
/// thing that cannot be derived: a scan code says which key it is, never what is printed on it.
/// </para>
/// <para>
/// Only measurements and outlines are taken. The vendor's colours, casing and styling are
/// ignored; drawing stays entirely with the program, so the keyboard looks like the rest of it
/// rather than like somebody else's software.
/// </para>
/// </remarks>
public sealed record SvgKeyboardLayout(
    double Width,
    double Height,
    IReadOnlyList<SvgKey> Keys,
    string? Legends,
    int? ProductId = null,
    int? LayoutId = null,
    IReadOnlyList<SvgChassisShape>? Chassis = null)
{
    private static readonly Regex ChassisPattern = new(
        @"<path[^>]*class=""(product[A-Za-z0-9]*)""[^>]*\sd=""([^""]*)""",
        RegexOptions.Compiled);

    /// <summary>
    /// The product and physical layout a drawing belongs to, declared right beside it.
    /// </summary>
    /// <remarks>
    /// The drawings are delivered inside the vendor's own script bundles, and immediately after
    /// the closing <c>svg</c> tag each one is followed by a configuration object naming the
    /// product and the layout: <c>PID:661,EID:128,Layout:3</c>. That is what makes picking the
    /// right drawing exact rather than a guess — the drawings for one keyboard are identical
    /// except for the legend outlines, so nothing inside the picture distinguishes German from
    /// Italian, and matching by shape alone will happily return either.
    /// </remarks>
    private static readonly Regex ConfigPattern = new(
        @"PID:\s*(\d+)\s*,\s*EID:\s*\d+\s*,\s*Layout:\s*(\d+)",
        RegexOptions.Compiled);

    private static readonly Regex ViewBoxPattern = new(
        @"viewBox\s*=\s*""\s*[\d.+-]+\s+[\d.+-]+\s+([\d.+-]+)\s+([\d.+-]+)",
        RegexOptions.Compiled);

    // Most keys are rectangles. The ISO Enter is not: it is L-shaped, so the drawing gives it a
    // path instead, carrying the same data- attributes as every other key. Reading only
    // rectangles lost exactly one key on every ISO keyboard, and losing one key was enough for
    // the geometry hand-off to fall back to the shipped layout every time.
    private static readonly Regex KeyPattern = new(
        @"<(rect|path)\s+id=""led-\d+""([^>]*)>",
        RegexOptions.Compiled);

    private static readonly Regex LegendPattern = new(
        @"<path[^>]*class=""[^""]*characters[^""]*""[^>]*\sd=""([^""]*)""",
        RegexOptions.Compiled);

    /// <summary>
    /// Reads a drawing. Returns <c>null</c> for anything that is not one — the source is another
    /// program's asset and may change shape without warning, and a keyboard drawn from a
    /// half-understood file would be worse than the shipped layout it replaces.
    /// </summary>
    public static SvgKeyboardLayout? Parse(string svg)
    {
        ArgumentNullException.ThrowIfNull(svg);

        var box = ViewBoxPattern.Match(svg);

        if (!box.Success
            || !TryNumber(box.Groups[1].Value, out var width)
            || !TryNumber(box.Groups[2].Value, out var height)
            || width <= 0 || height <= 0)
        {
            return null;
        }

        // Keys live in their own group. The drawing repeats them in a second group for the
        // selection outline, and counting those would double every key.
        var keys = ReadKeys(Section(svg, "LED"));

        if (keys.Count == 0)
        {
            return null;
        }

        var legends = LegendPattern.Match(svg);
        var config = ConfigPattern.Match(svg);

        int? productId = null;
        int? layoutId = null;

        if (config.Success
            && int.TryParse(config.Groups[1].Value, out var pid)
            && int.TryParse(config.Groups[2].Value, out var layout))
        {
            productId = pid;
            layoutId = layout;
        }

        return new SvgKeyboardLayout(
            width,
            height,
            keys,
            legends.Success ? legends.Groups[1].Value : null,
            productId,
            layoutId,
            ReadChassis(Section(svg, "Product")));
    }

    /// <summary>The part of the drawing inside one named group.</summary>
    private static string Section(string svg, string id)
    {
        var start = svg.IndexOf($@"<g id=""{id}""", StringComparison.Ordinal);

        if (start < 0)
        {
            return svg;
        }

        var next = svg.IndexOf(@"<g id=""", start + 1, StringComparison.Ordinal);

        return next < 0 ? svg[start..] : svg[start..next];
    }

    /// <summary>
    /// The shapes of the casing: the body, and the details raised out of it — on this keyboard the
    /// volume dial and the media strip along the top right.
    /// </summary>
    /// <remarks>
    /// The vendor's own class names say which shape belongs on top of which, and that is the only
    /// reason they are read: <c>productfill</c> and the first outline are the body, the rest are
    /// details. Their fills are ignored. A drawing with no casing simply has none, and the preview
    /// then draws what it always drew.
    /// </remarks>
    private static List<SvgChassisShape>? ReadChassis(string section)
    {
        var shapes = new List<SvgChassisShape>();
        var body = 0;

        foreach (Match match in ChassisPattern.Matches(section))
        {
            var kind = match.Groups[1].Value;
            var path = match.Groups[2].Value;

            if (path.Length == 0)
            {
                continue;
            }

            // The body is drawn twice — a fill and an outline over it — and everything after
            // those two is a detail sitting on the case.
            var layer = kind.Equals("productfill", StringComparison.OrdinalIgnoreCase) || body < 2
                ? SvgChassisLayer.Body
                : kind.EndsWith('2')
                    ? SvgChassisLayer.Recessed
                    : SvgChassisLayer.Raised;

            if (layer == SvgChassisLayer.Body)
            {
                body++;
            }

            // The box each shape occupies, so the caller can size a canvas to the case rather
            // than to the whole drawing — the drawing leaves a margin the case does not use, and
            // carrying it through would shrink the keyboard on screen for nothing. A shape whose
            // outline cannot be followed reports an empty box and simply does not count towards
            // the size.
            var bounds = TryOutlineBounds(path, out var bx, out var by, out var bw, out var bh)
                ? new SvgRect(bx, by, bw, bh)
                : default;

            shapes.Add(new SvgChassisShape(path, layer, bounds));
        }

        return shapes.Count > 0 ? shapes : null;
    }

    private static List<SvgKey> ReadKeys(string section)
    {
        var keys = new List<SvgKey>();
        var seen = new HashSet<(double, double)>();

        foreach (Match match in KeyPattern.Matches(section))
        {
            var attributes = match.Groups[2].Value;

            // Initialised because the checks below short-circuit: the later ones do not run once
            // an earlier one fails, and then their out parameters are never written.
            double x = 0, y = 0, w = 0, h = 0;
            bool read;
            List<SvgRect>? parts = null;

            if (match.Groups[1].Value == "path")
            {
                var outline = Attribute(attributes, "d");

                read = TryOutlineBounds(outline, out x, out y, out w, out h);

                // An outline that is more than one rectangle keeps them, so the step of an
                // L-shaped key lands where it is drawn. The largest is treated as the main one,
                // which is where a legend belongs.
                if (read && OutlineRectangles(outline) is { Count: > 1 } pieces)
                {
                    var ordered = pieces
                        .OrderByDescending(r => r.Width * r.Height)
                        .ToList();

                    x = ordered[0].X;
                    y = ordered[0].Y;
                    w = ordered[0].Width;
                    h = ordered[0].Height;
                    parts = [.. ordered.Skip(1)];
                }
            }
            else
            {
                read = TryNumber(Attribute(attributes, "x"), out x)
                    && TryNumber(Attribute(attributes, "y"), out y)
                    && TryNumber(Attribute(attributes, "width"), out w)
                    && TryNumber(Attribute(attributes, "height"), out h);
            }

            if (!read || w <= 0 || h <= 0)
            {
                continue;
            }

            // The same key can appear more than once; position identifies it.
            if (!seen.Add((x, y)))
            {
                continue;
            }

            keys.Add(new SvgKey(
                Attribute(attributes, "data-assumed-key-name") ?? "",
                x, y, w, h,
                Attribute(attributes, "data-selection-group"),
                parts));
        }

        return keys;
    }

    private static string? Attribute(string attributes, string name)
    {
        var match = Regex.Match(attributes, name + @"\s*=\s*""([^""]*)""");

        return match.Success ? match.Groups[1].Value : null;
    }

    private static bool TryNumber(string? text, out double value)
        => double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value);

    private static readonly Regex PathStepPattern = new(
        @"([MmLlHhVvCcSsZz])([^MmLlHhVvCcSsQqTtAaZz]*)",
        RegexOptions.Compiled);

    private static readonly Regex NumberPattern = new(
        @"-?\d*\.?\d+(?:[eE][+-]?\d+)?",
        RegexOptions.Compiled);

    /// <summary>
    /// The box an outline occupies, or <c>false</c> for a path this cannot follow.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Only the points the pen actually reaches are collected, never the control points of a
    /// curve. That is what makes the result exact here rather than approximate: the curves in
    /// these outlines are the rounded corners, and a rounded corner cuts the box in — it never
    /// pushes it out. Taking control points into account would inflate every key by the corner
    /// radius.
    /// </para>
    /// <para>
    /// Arcs and quadratic curves are refused rather than guessed at, and so is a path that starts
    /// anywhere but an absolute move. The caller then keeps the shipped geometry, which is the
    /// right outcome for a drawing this does not fully understand.
    /// </para>
    /// </remarks>
    /// <summary>
    /// Splits an axis-aligned outline into the rectangles it is made of, or an empty list.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The bounding box alone is not enough for the L-shaped Enter. Fitting our own two rectangles
    /// into it looked right and was not: the drawing puts the step at 46.7 % of the key's height
    /// because its lower half is the taller one, while a layout drawn on the standard grid has two
    /// halves of equal height and so puts it at 50 %. The step visibly sat wrong, and the halves
    /// no longer met, which showed as a seam in the glow between Enter and the key beside it.
    /// </para>
    /// <para>
    /// So the outline is taken apart instead. Its own edges form a grid; each cell of that grid is
    /// either wholly inside the shape or wholly outside, and the cells that are inside are merged
    /// back into as few rectangles as possible. Exact for any axis-aligned shape, which is all the
    /// drawings contain — the rounded corners are dropped first, since a 4 px radius is neither a
    /// step nor worth a rectangle.
    /// </para>
    /// </remarks>
    private static List<SvgRect> OutlineRectangles(string? d)
    {
        var empty = new List<SvgRect>();

        if (!TryOutlinePoints(d, out var points) || points.Count < 4)
        {
            return empty;
        }

        // The grid comes from the straight edges themselves, not from clustering the points. A
        // vertical edge is a v command and its x is exactly one grid line; the same for h and y.
        // Clustering was tried first and got the outer edge wrong — it collapsed the right-hand
        // edge into its rounded corner and made the Enter 51 wide instead of 55.
        if (!TryAxisEdges(d, out var xs, out var ys)
            || xs.Count < 2 || ys.Count < 2 || (xs.Count - 1) * (ys.Count - 1) > 64)
        {
            return empty;
        }

        var inside = new bool[xs.Count - 1, ys.Count - 1];

        for (var col = 0; col < xs.Count - 1; col++)
        {
            for (var row = 0; row < ys.Count - 1; row++)
            {
                var centreX = (xs[col] + xs[col + 1]) / 2;
                var centreY = (ys[row] + ys[row + 1]) / 2;

                // Tested at the centre of a cell, which is far enough from any corner that the
                // roundings in the raw outline cannot change the answer.
                inside[col, row] = Contains(points, centreX, centreY);
            }
        }

        // Merge runs of cells along a row, then merge rows that span the same columns.
        var rectangles = new List<SvgRect>();

        for (var row = 0; row < ys.Count - 1; row++)
        {
            var col = 0;

            while (col < xs.Count - 1)
            {
                if (!inside[col, row])
                {
                    col++;
                    continue;
                }

                var last = col;

                while (last + 1 < xs.Count - 1 && inside[last + 1, row])
                {
                    last++;
                }

                var candidate = new SvgRect(
                    xs[col], ys[row], xs[last + 1] - xs[col], ys[row + 1] - ys[row]);

                var above = rectangles.FindIndex(r =>
                    Close(r.X, candidate.X)
                    && Close(r.Width, candidate.Width)
                    && Close(r.Y + r.Height, candidate.Y));

                if (above >= 0)
                {
                    rectangles[above] = rectangles[above] with
                    {
                        Height = rectangles[above].Height + candidate.Height
                    };
                }
                else
                {
                    rectangles.Add(candidate);
                }

                col = last + 1;
            }
        }

        return rectangles;
    }

    private static bool Close(double a, double b) => Math.Abs(a - b) < 0.001;

    /// <summary>
    /// The lines the straight edges of an outline lie on: one x per vertical edge, one y per
    /// horizontal one.
    /// </summary>
    /// <remarks>
    /// Read off the <c>h</c> and <c>v</c> commands, whose whole purpose is to run along an axis.
    /// Where the pen is when such a command starts is the line it draws on, and that is exact —
    /// no rounding, no clustering, no guessing which of two nearby values is the real edge.
    /// </remarks>
    private static bool TryAxisEdges(string? d, out List<double> xs, out List<double> ys)
    {
        xs = [];
        ys = [];

        if (string.IsNullOrWhiteSpace(d))
        {
            return false;
        }

        var vertical = new SortedSet<double>();
        var horizontal = new SortedSet<double>();

        double cx = 0, cy = 0;
        var moved = false;

        foreach (Match step in PathStepPattern.Matches(d))
        {
            var command = step.Groups[1].Value[0];
            var relative = char.IsLower(command);

            var numbers = new List<double>();

            foreach (Match number in NumberPattern.Matches(step.Groups[2].Value))
            {
                if (!TryNumber(number.Value, out var value))
                {
                    return false;
                }

                numbers.Add(value);
            }

            switch (char.ToUpperInvariant(command))
            {
                case 'M':
                case 'L':
                    for (var i = 0; i + 1 < numbers.Count; i += 2)
                    {
                        var nx = relative && moved ? cx + numbers[i] : numbers[i];
                        var ny = relative && moved ? cy + numbers[i + 1] : numbers[i + 1];

                        // A straight segment that happens to run along an axis counts too.
                        if (moved && Math.Abs(nx - cx) < 0.001) { vertical.Add(cx); }
                        if (moved && Math.Abs(ny - cy) < 0.001) { horizontal.Add(cy); }

                        cx = nx;
                        cy = ny;
                        moved = true;
                    }

                    break;

                case 'H':
                    foreach (var value in numbers)
                    {
                        horizontal.Add(cy);
                        cx = relative ? cx + value : value;
                    }

                    break;

                case 'V':
                    foreach (var value in numbers)
                    {
                        vertical.Add(cx);
                        cy = relative ? cy + value : value;
                    }

                    break;

                case 'C':
                    for (var i = 0; i + 5 < numbers.Count; i += 6)
                    {
                        cx = relative ? cx + numbers[i + 4] : numbers[i + 4];
                        cy = relative ? cy + numbers[i + 5] : numbers[i + 5];
                    }

                    break;

                case 'S':
                    for (var i = 0; i + 3 < numbers.Count; i += 4)
                    {
                        cx = relative ? cx + numbers[i + 2] : numbers[i + 2];
                        cy = relative ? cy + numbers[i + 3] : numbers[i + 3];
                    }

                    break;

                case 'Z':
                    break;

                default:
                    return false;
            }
        }

        xs = [.. vertical];
        ys = [.. horizontal];

        return true;
    }

    /// <summary>Whether a point lies inside a rectilinear polygon, by crossing count.</summary>
    private static bool Contains(List<(double X, double Y)> polygon, double x, double y)
    {
        var inside = false;

        for (var i = 0; i < polygon.Count; i++)
        {
            var (ax, ay) = polygon[i];
            var (bx, by) = polygon[(i + 1) % polygon.Count];

            if (ay > y != by > y
                && x < ax + ((y - ay) / (by - ay) * (bx - ax)))
            {
                inside = !inside;
            }
        }

        return inside;
    }

    /// <summary>
    /// The points the pen reaches along an outline, or <c>false</c> for a path this cannot follow.
    /// </summary>
    /// <remarks>
    /// Only the points actually reached, never the control points of a curve. That is what makes
    /// the result exact here rather than approximate: the curves in these outlines are the rounded
    /// corners, and a rounded corner cuts a shape in — it never pushes it out. Taking control
    /// points into account would inflate every key by the corner radius.
    /// </remarks>
    private static bool TryOutlinePoints(string? d, out List<(double X, double Y)> points)
    {
        points = [];

        if (string.IsNullOrWhiteSpace(d) || !d.TrimStart().StartsWith('M'))
        {
            return false;
        }

        double cx = 0, cy = 0;
        var moved = false;

        foreach (Match step in PathStepPattern.Matches(d))
        {
            var command = step.Groups[1].Value[0];
            var relative = char.IsLower(command);

            var numbers = new List<double>();

            foreach (Match number in NumberPattern.Matches(step.Groups[2].Value))
            {
                if (!TryNumber(number.Value, out var value))
                {
                    return false;
                }

                numbers.Add(value);
            }

            switch (char.ToUpperInvariant(command))
            {
                case 'Z':
                    break;

                case 'M':
                case 'L':
                    if (numbers.Count == 0 || numbers.Count % 2 != 0)
                    {
                        return false;
                    }

                    for (var i = 0; i + 1 < numbers.Count; i += 2)
                    {
                        cx = relative && moved ? cx + numbers[i] : numbers[i];
                        cy = relative && moved ? cy + numbers[i + 1] : numbers[i + 1];
                        moved = true;
                        points.Add((cx, cy));
                    }

                    break;

                case 'H':
                case 'V':
                    if (numbers.Count == 0)
                    {
                        return false;
                    }

                    foreach (var value in numbers)
                    {
                        if (char.ToUpperInvariant(command) == 'H')
                        {
                            cx = relative ? cx + value : value;
                        }
                        else
                        {
                            cy = relative ? cy + value : value;
                        }

                        points.Add((cx, cy));
                    }

                    break;

                case 'C':
                    // Six numbers per curve; the last pair is where the pen ends up.
                    if (numbers.Count == 0 || numbers.Count % 6 != 0)
                    {
                        return false;
                    }

                    for (var i = 0; i + 5 < numbers.Count; i += 6)
                    {
                        cx = relative ? cx + numbers[i + 4] : numbers[i + 4];
                        cy = relative ? cy + numbers[i + 5] : numbers[i + 5];
                        points.Add((cx, cy));
                    }

                    break;

                case 'S':
                    if (numbers.Count == 0 || numbers.Count % 4 != 0)
                    {
                        return false;
                    }

                    for (var i = 0; i + 3 < numbers.Count; i += 4)
                    {
                        cx = relative ? cx + numbers[i + 2] : numbers[i + 2];
                        cy = relative ? cy + numbers[i + 3] : numbers[i + 3];
                        points.Add((cx, cy));
                    }

                    break;

                default:
                    return false;
            }
        }

        return moved && points.Count > 0;
    }

    /// <summary>The box an outline occupies, or <c>false</c> for a path this cannot follow.</summary>
    private static bool TryOutlineBounds(
        string? d, out double x, out double y, out double width, out double height)
    {
        x = y = width = height = 0;

        if (!TryOutlinePoints(d, out var points))
        {
            return false;
        }

        var minX = points.Min(p => p.X);
        var minY = points.Min(p => p.Y);
        var maxX = points.Max(p => p.X);
        var maxY = points.Max(p => p.Y);

        if (minX > maxX || minY > maxY)
        {
            return false;
        }

        x = minX;
        y = minY;
        width = maxX - minX;
        height = maxY - minY;

        return true;
    }

}
