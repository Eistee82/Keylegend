using Keylegend.Core.Devices;

namespace Keylegend.Chroma;

/// <summary>
/// Finds the vendor's own drawing of the attached keyboard among the files its software keeps on
/// disk, so that a keyboard nobody drew by hand still appears in its real shape.
/// </summary>
/// <remarks>
/// <para>
/// The vendor's interface is a web application, and the drawings it loads for the attached device
/// stay in its cache. They are read there because the alternative — asking its service over the
/// socket the interface itself uses — speaks a binary protocol that is not documented.
/// </para>
/// <para>
/// That makes this a convenience, never a dependency: everything here may fail, and the shipped
/// layout takes over. It is also why nothing is copied out of those files except measurements.
/// </para>
/// <para>
/// One thing makes the search practical. The cache holds a drawing per physical layout — German,
/// French, Spanish and so on — and they cannot be told apart from the outside, because they
/// differ only in the outlines of the printed legends. But their <em>geometry is identical</em>:
/// all eighteen layouts checked carried byte-identical key rectangles. So the right shape can be
/// had without knowing which language a drawing is for — and the language is then settled
/// separately, from the layout number the service states, because the shape alone cannot settle
/// it. Matching on shape and stopping there puts Italian legends on a German board.
/// </para>
/// </remarks>
public static class SvgLayoutSource
{
    /// <summary>Where the vendor's interface keeps what it has downloaded.</summary>
    public static IEnumerable<string> DefaultDirectories()
    {
        var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        if (!string.IsNullOrEmpty(local))
        {
            yield return Path.Combine(local, "Razer", "RazerAppEngine", "User Data", "Default",
                "Service Worker", "CacheStorage");
        }
    }

    /// <summary>
    /// The drawing that matches the attached keyboard, or <c>null</c> if none does.
    /// </summary>
    /// <param name="device">The attached keyboard, to tell one physical layout from another.</param>
    /// <param name="directories">Where to look; the vendor's cache by default.</param>
    /// <param name="fileLimit">
    /// How many files to open at most. The cache holds thousands, most of them not drawings, and
    /// a start-up must not turn into a disk scan.
    /// </param>
    public static SvgKeyboardLayout? Find(
        SdkDeviceDescription device,
        IEnumerable<string>? directories = null,
        int fileLimit = 4000)
    {
        ArgumentNullException.ThrowIfNull(device);

        var wanted = ShapeOf(device);
        var opened = 0;

        // The shape only narrows a drawing down to a keyboard, never to a language, so a match on
        // it is kept as a fallback while the walk continues looking for the exact one.
        SvgKeyboardLayout? sameShape = null;

        foreach (var directory in directories ?? DefaultDirectories())
        {
            if (!Directory.Exists(directory))
            {
                continue;
            }

            foreach (var file in Enumerate(directory))
            {
                if (++opened > fileLimit)
                {
                    return sameShape;
                }

                var layout = ReadDrawing(file);

                if (layout is null)
                {
                    continue;
                }

                if (IsFor(layout, device))
                {
                    return layout;
                }

                if (sameShape is null && Matches(ShapeOf(layout), wanted))
                {
                    sameShape = layout;
                }
            }
        }

        return sameShape;
    }

    /// <summary>
    /// Whether a drawing declares itself to be for this device in this physical layout.
    /// </summary>
    /// <remarks>
    /// This is the difference between the right legends and somebody else's. The drawings for one
    /// keyboard are identical except for the outlines of the printed characters, so matching on
    /// the picture returns whichever language happened to be read first — a German board was
    /// showing <c>invio</c>, <c>canc</c> and <c>ò @ ç</c>, because an Italian drawing has exactly
    /// the same key rectangles. What does distinguish them sits just past the closing tag, in the
    /// configuration object the vendor's bundle carries beside each drawing, and the layout id
    /// there is the same number the lighting service reports for the attached keyboard.
    /// </remarks>
    private static bool IsFor(SvgKeyboardLayout layout, SdkDeviceDescription device)
        => layout.ProductId is { } product
            && layout.LayoutId is { } id
            && product == device.ProductId
            && id == device.LayoutId;

    private static IEnumerable<string> Enumerate(string directory)
    {
        // A cache is another program's business: it may be locked, renamed or gone mid-walk.
        IEnumerable<string> files;

        try
        {
            files = Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            yield break;
        }

        foreach (var file in files)
        {
            yield return file;
        }
    }

    private static SvgKeyboardLayout? ReadDrawing(string file)
    {
        try
        {
            var info = new FileInfo(file);

            // A drawing of a keyboard is tens of kilobytes. Skipping the rest keeps this cheap.
            if (info.Length is < 20_000 or > 4_000_000)
            {
                return null;
            }

            var text = File.ReadAllText(file);

            return text.Contains("<g id=\"LED\"", StringComparison.Ordinal)
                ? SvgKeyboardLayout.Parse(text)
                : null;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException)
        {
            return null;
        }
    }

    /// <summary>
    /// Whether a drawing depicts the keyboard that is attached.
    /// </summary>
    /// <remarks>
    /// The key counts are compared loosely on purpose. The two sides count differently — the
    /// device lists the media keys it can address, the drawing only draws the ones that have a
    /// key cap of their own — and on the keyboard measured here that is a difference of three.
    /// Anything within a handful is the same size of keyboard; the sizes themselves are far
    /// apart, a tenkeyless having some twenty keys fewer than a full-size and a sixty-percent
    /// forty fewer again. Physical layout has to agree exactly, because an ISO board drawn as
    /// ANSI is missing a key where the user is looking.
    /// </remarks>
    private static bool Matches((int Keys, bool Iso, bool Japanese) drawing, (int Keys, bool Iso, bool Japanese) device)
        => drawing.Iso == device.Iso
        && drawing.Japanese == device.Japanese
        && Math.Abs(drawing.Keys - device.Keys) <= 6;

    /// <summary>
    /// What kind of keyboard a drawing depicts: how many keys, and whether it carries the extra
    /// keys that mark an ISO or Japanese board. Enough to tell the drawings apart; deliberately
    /// not enough to tell German from French, which no drawing states.
    /// </summary>
    private static (int Keys, bool Iso, bool Japanese) ShapeOf(SvgKeyboardLayout layout)
        => (layout.Keys.Count,
            layout.Keys.Any(k => k.Name == "Extra1"),
            layout.Keys.Any(k => k.Name == "Extra3"));

    private static (int Keys, bool Iso, bool Japanese) ShapeOf(SdkDeviceDescription device)
    {
        // The drawing counts every key including the silent ones; the device reports those
        // separately.
        var keys = device.Keys.Count + device.SilentKeys;

        var iso = device.Keys.Any(k => !k.Extended && k.Scancode == 0x56);
        var japanese = device.Keys.Any(k => !k.Extended && k.Scancode is 0x70 or 0x7B or 0x7D);

        return (keys, iso || japanese, japanese);
    }
}
