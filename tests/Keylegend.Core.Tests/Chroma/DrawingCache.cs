using System.Globalization;
using System.Text;

namespace Keylegend.Core.Tests.Chroma;

/// <summary>
/// A stand-in for the vendor's drawing cache: a directory holding files shaped like the ones
/// Synapse leaves behind, written for a test and thrown away with it.
/// </summary>
/// <remarks>
/// <para>
/// Why this exists rather than a test that steps aside when Synapse is absent: everything
/// <c>SvgLayoutSource</c> does is decide which file to believe. It walks a directory, skips what
/// cannot be a drawing, reads the ones that can, and picks the one whose product and layout match
/// the attached device. Not one of those steps needs the vendor's own bytes — they need files that
/// look like them, and a test can write those.
/// </para>
/// <para>
/// What this cannot prove is that the vendor's real files look the way this one does. That is what
/// the tests reading the actual installation are for, and it is why they stay: this checks the
/// decisions, they check the assumption.
/// </para>
/// </remarks>
internal sealed class DrawingCache : IDisposable
{
    private DrawingCache(string directory) => Directory = directory;

    public string Directory { get; }

    /// <summary>The directories to hand <c>SvgLayoutSource.Find</c>.</summary>
    public IEnumerable<string> Directories => [Directory];

    public static DrawingCache Create()
    {
        var path = Path.Combine(
            Path.GetTempPath(), "keylegend-drawings-" + Guid.NewGuid().ToString("N")[..12]);

        System.IO.Directory.CreateDirectory(path);

        return new DrawingCache(path);
    }

    /// <summary>
    /// Writes a drawing for one product and physical layout, in the shape the parser expects: a
    /// keyed LED group, and the configuration object that follows the picture in the vendor's
    /// bundles.
    /// </summary>
    /// <param name="fileName">
    /// Deliberately meaningless, as the real ones are — cache entries are named by hash, so the
    /// name can say nothing about what is inside.
    /// </param>
    public string Write(
        string fileName,
        int productId,
        int layoutId,
        IEnumerable<string> keyNames,
        bool includeLedGroup = true,
        int padToBytes = 24_000)
    {
        var svg = new StringBuilder();

        svg.AppendLine("""<svg width="961" height="361" viewBox="0 0 961 361">""");
        svg.AppendLine("""  <g id="Product"><path class="productfill" d="M0,0h961v361h-961Z"/></g>""");

        // The group id is what marks a file as a drawing at all. A file without it must be
        // skipped, so a test needs to be able to leave it out.
        svg.AppendLine(includeLedGroup ? """  <g id="LED">""" : """  <g id="NotLed">""");

        var index = 0;
        double x = 10;
        double y = 10;

        foreach (var name in keyNames)
        {
            index++;

            svg.AppendLine(string.Create(
                CultureInfo.InvariantCulture,
                $"""    <rect id="led-{index}" class="key{name} led" x="{x}" y="{y}" width="35" height="35" data-assumed-key-name="{name}" data-col="{index % 22}" data-row="{index / 22}" data-selection-group="alphabets"/>"""));

            x += 39;

            if (x > 900)
            {
                x = 10;
                y += 39;
            }
        }

        svg.AppendLine("  </g>");
        svg.AppendLine("""  <path class="characters" d="M10,10h5v5h-5Z"/>""");
        svg.AppendLine("</svg>");

        // What makes the choice exact: the product and the layout, stated beside the picture.
        svg.AppendLine(string.Create(
            CultureInfo.InvariantCulture,
            $"DEVICECONFIG={{Version:2,VID:5426,PID:{productId},EID:128,Layout:{layoutId}}}"));

        // A real cache entry is a script bundle with a drawing somewhere inside it, so it is far
        // larger than the picture. The size filter in the reader exists because of that, and a
        // file below it would be skipped no matter what it contains.
        var text = svg.ToString();

        if (text.Length < padToBytes)
        {
            text += "\n<!-- " + new string('.', padToBytes - text.Length) + " -->\n";
        }

        var path = Path.Combine(Directory, fileName);
        File.WriteAllText(path, text, Encoding.UTF8);

        return path;
    }

    /// <summary>The 105 keys of a full-size ISO keyboard, named as the drawings name them.</summary>
    public static IReadOnlyList<string> FullSizeIsoKeys { get; } =
    [
        "Esc", "F1", "F2", "F3", "F4", "F5", "F6", "F7", "F8", "F9", "F10", "F11", "F12",
        "PrintScreen", "ScrollLock", "PauseBreak",
        "Tilde", "1", "2", "3", "4", "5", "6", "7", "8", "9", "0", "Dash", "Equal", "Backspace",
        "Insert", "Home", "PageUp",
        "Tab", "Q", "W", "E", "R", "T", "Y", "U", "I", "O", "P",
        "StartSquareBracket", "EndSquareBracket", "Enter",
        "Delete", "End", "PageDown",
        "Caps", "A", "S", "D", "F", "G", "H", "J", "K", "L", "SemiColon", "Apostrophe", "Extra1",
        "LeftShift", "Backslash", "Z", "X", "C", "V", "B", "N", "M",
        "Comma", "Period", "ForwardSlash", "RightShift",
        "UpArrow",
        "LeftCtrl", "LeftWindows", "LeftAlt", "Space", "RightAlt", "Function", "Menu", "RightCtrl",
        "LeftArrow", "DownArrow", "RightArrow",
        "NumPad", "NumPadForwardSlash", "NumPadMultiply", "NumPadDash",
        "NumPad7", "NumPad8", "NumPad9", "NumPadPlus",
        "NumPad4", "NumPad5", "NumPad6",
        "NumPad1", "NumPad2", "NumPad3", "NumPadEnter",
        "NumPad0", "NumPadPeriod"
    ];

    public void Dispose()
    {
        try
        {
            if (System.IO.Directory.Exists(Directory))
            {
                System.IO.Directory.Delete(Directory, recursive: true);
            }
        }
        catch (IOException)
        {
            // A temporary directory that outlives one test run is not worth failing over.
        }
    }
}
