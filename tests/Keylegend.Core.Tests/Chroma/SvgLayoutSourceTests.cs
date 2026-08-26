using Keylegend.Chroma;
using Keylegend.Core.Devices;

namespace Keylegend.Core.Tests.Chroma;

/// <summary>
/// Reads the vendor's real drawings off this machine and checks that the parser understands them.
/// </summary>
/// <remarks>
/// <para>
/// The unit tests in <see cref="Devices.SvgKeyboardLayoutTests"/> feed the parser drawings written
/// for the purpose, which is what pins its behaviour down. This file does the other half: it
/// proves the parser still fits the files the vendor actually ships, which it cannot do from a
/// fixture, because those files may not be copied into this repository — they are someone else's
/// artwork and the licence here is MIT.
/// </para>
/// <para>
/// Every test therefore skips when the vendor's software is not installed, and says so in the
/// run rather than passing. On such a machine the application has no keyboard to describe and
/// stops with a message, so there is nothing here to check.
/// </para>
/// </remarks>
public class SvgLayoutSourceTests
{
    /// <summary>Every drawing found on this machine, parsed.</summary>
    private static IReadOnlyList<SvgKeyboardLayout> Drawings { get; } = FindAll();

    private static IReadOnlyList<SvgKeyboardLayout> FindAll()
    {
        var found = new List<SvgKeyboardLayout>();

        foreach (var directory in SvgLayoutSource.DefaultDirectories())
        {
            if (!Directory.Exists(directory))
            {
                continue;
            }

            var opened = 0;

            foreach (var file in Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories))
            {
                if (opened++ > 4000)
                {
                    break;
                }

                string text;

                try
                {
                    var info = new FileInfo(file);

                    // A drawing is tens of kilobytes. Skipping the rest keeps this off the
                    // multi-megabyte blobs the cache also holds.
                    if (info.Length is < 20_000 or > 4_000_000)
                    {
                        continue;
                    }

                    text = File.ReadAllText(file);
                }
                catch (IOException)
                {
                    continue;
                }
                catch (UnauthorizedAccessException)
                {
                    continue;
                }

                if (SvgKeyboardLayout.Parse(text) is { } layout)
                {
                    found.Add(layout);
                }
            }
        }

        return found;
    }

    private static bool Installed => Drawings.Count > 0;

    [Fact]
    public void FindsDrawingsWhereTheVendorSoftwareKeepsThem()
    {
        Assert.SkipUnless(Installed, VendorFiles.Absent);

        Assert.NotEmpty(Drawings);
    }

    /// <summary>
    /// A keyboard has tens of keys, not thousands. A count far outside that means the parser has
    /// caught something other than the key group — the failure that made this file necessary.
    /// </summary>
    [Fact]
    public void EveryDrawingHasAPlausibleNumberOfKeys()
    {
        Assert.SkipUnless(Installed, VendorFiles.Absent);

        foreach (var drawing in Drawings)
        {
            Assert.InRange(drawing.Keys.Count, 40, 140);
        }
    }

    /// <summary>
    /// The names are what <see cref="SvgLayoutSource"/> tells one physical layout from another
    /// with, so a drawing whose keys are unnamed would silently break that choice.
    /// </summary>
    [Fact]
    public void EveryKeyIsNamed()
    {
        Assert.SkipUnless(Installed, VendorFiles.Absent);

        foreach (var drawing in Drawings)
        {
            var unnamed = drawing.Keys.Count(k => string.IsNullOrWhiteSpace(k.Name));

            Assert.Equal(0, unnamed);
        }
    }

    [Fact]
    public void EveryKeyHasAPositiveSize()
    {
        Assert.SkipUnless(Installed, VendorFiles.Absent);

        foreach (var drawing in Drawings)
        {
            Assert.All(drawing.Keys, k =>
            {
                Assert.True(k.Width > 0, $"{k.Name} has width {k.Width}.");
                Assert.True(k.Height > 0, $"{k.Name} has height {k.Height}.");
            });
        }
    }

    /// <summary>
    /// The drawing repeats each key in a second group for the selection outline. Counting those
    /// would double every key, so the parser keeps one per position — this checks it did.
    /// </summary>
    [Fact]
    public void NoTwoKeysShareAPosition()
    {
        Assert.SkipUnless(Installed, VendorFiles.Absent);

        foreach (var drawing in Drawings)
        {
            var places = drawing.Keys.Select(k => (k.X, k.Y)).ToHashSet();

            Assert.Equal(drawing.Keys.Count, places.Count);
        }
    }

    [Fact]
    public void KeysStayInsideTheCanvas()
    {
        Assert.SkipUnless(Installed, VendorFiles.Absent);

        foreach (var drawing in Drawings)
        {
            Assert.All(drawing.Keys, k =>
            {
                Assert.InRange(k.X, 0, drawing.Width);
                Assert.InRange(k.Y, 0, drawing.Height);
            });
        }
    }

    /// <summary>
    /// A full-size drawing must carry the keys every keyboard has. If the vendor renames these,
    /// the geometry hand-off in <c>AttachedKeyboardBuilder.WithGeometryOf</c> starts matching by
    /// distance alone, and this is the test that says so.
    /// </summary>
    [Fact]
    public void FullSizeDrawingsCarryTheKeysEveryKeyboardHas()
    {
        Assert.SkipUnless(Installed, VendorFiles.Absent);

        var fullSize = Drawings.Where(d => d.Keys.Count >= 100).ToArray();

        Assert.SkipWhen(
            fullSize.Length == 0,
            "None of the drawings on this machine is of a full-size keyboard, which is what this "
            + "test is about.");

        string[] expected = ["Esc", "F1", "F12", "Space", "Enter", "LeftShift", "LeftCtrl"];

        foreach (var drawing in fullSize)
        {
            var names = drawing.Keys.Select(k => k.Name).ToHashSet(StringComparer.Ordinal);

            foreach (var name in expected)
            {
                Assert.Contains(name, names);
            }
        }
    }

    /// <summary>
    /// The finding that decides how much of a profile a drawing can replace: the drawings differ
    /// only in the outlines of the printed legends, and those come as one path for the whole
    /// keyboard rather than per key. So a drawing supplies geometry, and the legend a silent key
    /// carries — <c>strg</c> rather than <c>Ctrl</c> — has to keep coming from the layout.
    /// </summary>
    [Fact]
    public void ALegendOutlineIsOnePathForTheWholeKeyboard()
    {
        Assert.SkipUnless(Installed, VendorFiles.Absent);

        var withLegends = Drawings.Where(d => d.Legends is not null).ToArray();

        Assert.SkipWhen(
            withLegends.Length == 0,
            "None of the drawings on this machine carries a legend outline, which is what this "
            + "test is about.");

        foreach (var drawing in withLegends)
        {
            // One path, and long, because it holds every character on the keyboard at once.
            // There is no per-key division in it to recover a single key's legend from.
            Assert.True(
                drawing.Legends!.Length > 1000,
                $"The legend outline is {drawing.Legends.Length} characters; a whole keyboard's "
                + "worth of characters is far longer, so this is probably not the right path.");
        }
    }

    /// <summary>
    /// The drawing's own measurements reach the keyboard that gets built. Two ways of losing them
    /// have happened: the reader dropping the L-shaped Enter, and — once that was fixed — the
    /// Enter keeping its drawn size while every neighbour took the drawing's, so they overlapped
    /// and the validator threw the whole measured layout away.
    /// </summary>
    /// <remarks>
    /// The space bar is the witness: the standard 19.05 mm grid makes it 118.75 wide, and the
    /// drawing gives Razer's real 117.2. Anything that breaks the hand-off shows up here as the
    /// grid's number.
    /// </remarks>
    [Fact]
    public void TheDrawingsMeasurementsReachTheKeyboard()
    {
        Assert.SkipUnless(Installed, VendorFiles.Absent);

        var device = DeathStalker();
        var drawing = SvgLayoutSource.Find(device);

        Assert.SkipWhen(drawing is null, VendorFiles.NoDrawingForTheDevice);

        var built = AttachedKeyboardBuilder.FromDrawing(device, drawing);

        Assert.NotNull(built);

        Assert.Contains("Enter", drawing.Keys.Select(k => k.Name));

        // Razer's own space bar, in the drawing's own units: a check that the measurement
        // survives the journey rather than being rounded to something tidy off a standard grid.
        var space = built.Keys.Single(k => k.Id == "Keyboard_Space");
        var drawnSpace = drawing.Keys.Single(k => k.Name == "Space");

        Assert.Equal(drawnSpace.Width, space.Width, 3);
        Assert.InRange(space.Width, 200, 280);

        // And the Enter is still made of more than one rectangle, so it draws as an L rather than
        // being flattened into a bar.
        var enter = built.Keys.Single(k => k.Id == "Keyboard_Enter");

        Assert.NotNull(enter.Parts);
        Assert.NotEmpty(enter.Parts);
    }


    /// <summary>Stands in for the attached DeathStalker V2, German layout.</summary>
    private static SdkDeviceDescription DeathStalker()
    {
        var reported = new List<SdkKey>();

        foreach (var key in MeasuredKeys.Load())
        {
            if (Core.Input.ScanCodes.TryGet(key.Id, out var code))
            {
                var extended = (code & Core.Input.ScanCodes.ExtendedPrefix) != 0;
                reported.Add(new SdkKey(code & 0xFF, extended, 0, 0));
            }
        }

        return new SdkDeviceDescription(
            "Razer DeathStalker V2", 0x1532, 0x0295, LayoutId: 3,
            MatrixRows: 6, MatrixColumns: 22, reported);
    }

    /// <summary>
    /// Geometry is identical across the physical layouts — that is what lets the right shape be
    /// found without knowing which language a drawing is for. It is also why a drawing cannot say
    /// which layout it belongs to.
    /// </summary>
    /// <remarks>
    /// The grouping has to include the ISO key, not only the key count. ANSI and ISO drawings of
    /// the same keyboard carry the same number of keys and differ in three rectangles: ISO gains
    /// the extra key next to the left Shift and pays for it with a shorter left Shift and a
    /// narrower Enter. Grouping by count alone put those together and made this look like a fault
    /// in the reader, which it was not — it is the same distinction
    /// <see cref="SvgLayoutSource"/> itself draws.
    /// </remarks>
    [Fact]
    public void DrawingsOfTheSameShapeAgreeOnGeometry()
    {
        Assert.SkipUnless(Installed, VendorFiles.Absent);

        var groups = Drawings
            .GroupBy(d => (
                d.Keys.Count,
                d.Width,
                d.Height,
                Iso: d.Keys.Any(k => k.Name == "Extra1"),
                Japanese: d.Keys.Any(k => k.Name == "Extra3")))
            .Where(g => g.Count() > 1);

        foreach (var group in groups)
        {
            var first = group.First().Keys.Select(k => (k.X, k.Y, k.Width, k.Height)).ToArray();

            foreach (var other in group.Skip(1))
            {
                var theirs = other.Keys.Select(k => (k.X, k.Y, k.Width, k.Height)).ToArray();

                Assert.Equal(first, theirs);
            }
        }
    }
}
