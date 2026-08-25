namespace Keylegend.Core.Devices;

/// <summary>
/// A single key: where it sits on the keyboard and which Chroma matrix cell drives its LED.
/// </summary>
/// <param name="Id">Layout-wide unique key identifier, e.g. <c>Keyboard_Enter</c>.</param>
/// <param name="X">Left edge on the canvas, in the same units as <see cref="Canvas"/>.</param>
/// <param name="Y">Top edge on the canvas.</param>
/// <param name="Width">Key width on the canvas.</param>
/// <param name="Height">Key height on the canvas.</param>
/// <param name="Row">Chroma matrix row, or <c>null</c> while the mapping is still unknown.</param>
/// <param name="Column">Chroma matrix column, or <c>null</c> while the mapping is still unknown.</param>
/// <param name="ScanCode">
/// Overrides the standard scan code for this key id. Needed where a physical layout disagrees
/// with the US-based naming — on ISO keyboards the tall Enter covers the position ANSI uses for
/// backslash, so its upper LED must report the Enter scan code rather than the backslash one.
/// <c>null</c> means "use the standard table".
/// </param>
/// <param name="Parts">
/// Additional rectangles belonging to the same key, for keys that are not rectangular. The ISO
/// Enter is the standard case: one key, two areas. <c>null</c> for ordinary keys.
/// </param>
/// <param name="Label">
/// What is printed on the key, for keys that type nothing — <c>strg</c>, <c>entf</c>, <c>pos 1</c>.
/// It belongs to the keyboard rather than to the program's language: a German keyboard says
/// "Strg" whatever language the software speaks. Keys that type a character need no label; what
/// they print is asked from the layout instead.
/// </param>
/// <param name="LabelSecondary">
/// A second line printed below the first — <c>s-abf</c> under <c>druck</c>, or the navigation
/// name under a number pad digit. May be given on its own, leaving the main legend to come from
/// the layout as usual.
/// </param>
public sealed record KeyDefinition(
    string Id,
    double X,
    double Y,
    double Width,
    double Height,
    int? Row,
    int? Column,
    int? ScanCode = null,
    IReadOnlyList<KeyArea>? Parts = null,
    string? Label = null,
    string? LabelSecondary = null)
{
    /// <summary>Every rectangle this key occupies, the main one first.</summary>
    public IEnumerable<KeyArea> Areas()
    {
        yield return new KeyArea(X, Y, Width, Height);

        foreach (var part in Parts ?? [])
        {
            yield return part;
        }
    }
}

/// <summary>A rectangle belonging to a key.</summary>
public sealed record KeyArea(double X, double Y, double Width, double Height);

/// <summary>Drawing surface the key coordinates refer to.</summary>
public sealed record Canvas(double Width, double Height);

/// <summary>Dimensions of the vendor LED matrix. Razer keyboards are 6 x 22.</summary>
public sealed record MatrixSize(int Rows, int Columns);

/// <summary>
/// The outlines of the legends printed on a keyboard, as one shape for the whole board.
/// </summary>
/// <param name="Path">
/// The outline itself, in SVG path syntax, in the coordinates of the drawing it came from.
/// </param>
/// <param name="ScaleX">Multiply a drawing x by this to reach a profile x.</param>
/// <param name="ScaleY">Multiply a drawing y by this to reach a profile y.</param>
/// <param name="OffsetX">Then add this.</param>
/// <param name="OffsetY">Then add this.</param>
/// <param name="DrawnKeys">
/// Where the drawing puts each key, by our key id, in the drawing's own coordinates. This is what
/// lets a legend be placed on the key it belongs to rather than merely near it: the two sides do
/// not agree on where a block of keys sits — the navigation block is the worst of them — so a
/// single mapping for the whole board leaves a legend sitting over its neighbour. With this, each
/// legend is nudged onto the centre of its own key.
/// </param>
/// <remarks>
/// <para>
/// One shape rather than one per key, because that is how the drawing gives it: a single path
/// holding every character on the board. There is no per-key division in it to recover, and none
/// is needed — the path already sits at the right places in the drawing's own coordinates, so the
/// same mapping that carries the key geometry across carries the legends with it. That mapping is
/// the four numbers here, so that whoever draws this needs nothing but the profile.
/// </para>
/// <para>
/// This is never written to a file. It is read from the vendor's own installation at runtime and
/// held for as long as the program runs, which is what keeps it clear of the MIT licence: nothing
/// is copied into this repository. It follows that a profile on disk never has one, and that
/// everything downstream must work without it.
/// </para>
/// </remarks>
public sealed record LegendDrawing(
    string Path,
    double ScaleX,
    double ScaleY,
    double OffsetX,
    double OffsetY,
    IReadOnlyDictionary<string, KeyArea>? DrawnKeys = null,
    IReadOnlyList<ChassisShape>? Chassis = null);

/// <summary>How prominent a shape of the casing is.</summary>
public enum ChassisLayer
{
    /// <summary>The body of the case.</summary>
    Body,

    /// <summary>A raised detail — a dial, a media strip, a wordmark.</summary>
    Raised,

    /// <summary>A recessed or shaded detail.</summary>
    Recessed
}

/// <summary>
/// One shape of the keyboard's casing, in the same coordinates as the legend outline.
/// </summary>
/// <remarks>
/// An outline and a layer, nothing more. What it is drawn in is the program's business: the
/// vendor's own greys are read only to work out which shape sits on top of which. This is how the
/// dial and the media strip along the top right of a board come to appear at all — they carry no
/// addressable lighting, so no profile ever described them.
/// </remarks>
public sealed record ChassisShape(string Path, ChassisLayer Layer);

/// <summary>
/// The keyboard that is plugged in: what it is called, how it is laid out, and where every key
/// sits.
/// </summary>
/// <remarks>
/// Assembled while the program runs, from what the lighting service reports about the device and
/// what the vendor's drawing of it measures. Every field is therefore something one of those two
/// states — there is no version, no origin and no flag for how much of it to trust, because there
/// is no file and nobody writing one by hand.
/// </remarks>
public sealed record DeviceProfile(
    string Name,
    string PhysicalLayout,
    Canvas Canvas,
    MatrixSize Matrix,
    IReadOnlyList<KeyDefinition> Keys,
    LegendDrawing? Legend = null);
