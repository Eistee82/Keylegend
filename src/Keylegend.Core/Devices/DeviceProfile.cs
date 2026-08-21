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
/// The USB identity of a keyboard, as four hex digits each — <c>1532</c> and <c>0295</c> for the
/// Razer DeathStalker V2.
/// </summary>
/// <remarks>
/// This is what makes recognition possible rather than guesswork: Windows reports the same pair
/// for the hardware that is plugged in, so a profile carrying it can be matched to the keyboard
/// on the desk instead of being picked by name order. Find yours with
/// <c>Get-PnpDevice -Class Keyboard</c>; the instance id reads <c>HID\VID_1532&amp;PID_0295\…</c>.
/// <para>
/// A vendor uses one product id across layouts, so a match narrows the choice to a model, not to
/// a layout. Which ISO or ANSI variant of that model is meant is decided afterwards, from the
/// keyboard layout Windows is running.
/// </para>
/// </remarks>
public sealed record UsbId(string VendorId, string ProductId)
{
    /// <summary>Whether two ids name the same hardware, ignoring case and leading zeroes.</summary>
    public bool Matches(UsbId other)
    {
        ArgumentNullException.ThrowIfNull(other);

        return Same(VendorId, other.VendorId) && Same(ProductId, other.ProductId);
    }

    private static bool Same(string first, string second)
        => string.Equals(
            first?.TrimStart('0'),
            second?.TrimStart('0'),
            StringComparison.OrdinalIgnoreCase);
}

/// <summary>
/// Describes one keyboard model in one physical layout. Device support is data, not code:
/// adding a keyboard means adding one of these, never changing the program.
/// </summary>
/// <remarks>
/// <see cref="Image"/> is optional and currently unused: the on-screen preview is drawn from
/// the geometry below, which stays sharp at any size and cannot disagree with the profile.
/// Only attach a picture you took or made yourself — everything in this repository ships under
/// the MIT licence, and a vendor's product render cannot.
/// <para>
/// <see cref="Note"/> is free text for whoever opens the file next: what is still unchecked,
/// which model the profile was written against, what surprised the person who calibrated it.
/// </para>
/// <para>
/// <see cref="Usb"/> is what lets the right profile be recognised rather than guessed. Without
/// it a profile still works — it just has to be chosen instead of found.
/// </para>
/// </remarks>
public sealed record DeviceProfile(
    int FormatVersion,
    string Name,
    string Vendor,
    string Model,
    string PhysicalLayout,
    string? Image,
    Canvas Canvas,
    MatrixSize Matrix,
    bool Verified,
    IReadOnlyList<KeyDefinition> Keys,
    string? Note = null,
    UsbId? Usb = null)
{
    /// <summary>Highest format version this build understands.</summary>
    public const int SupportedFormatVersion = 1;
}
