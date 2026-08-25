using Keylegend.Chroma;
using Keylegend.Core.Devices;

namespace Keylegend.Core.Tests.Chroma;

/// <summary>
/// The profile built from the vendor's drawing alone, with no shipped layout involved.
/// </summary>
/// <remarks>
/// <para>
/// This is what has to hold before the shipped layouts can go. The yardstick is the same one as
/// everywhere else: <c>razer-deathstalker-v2-de</c>, the only profile ever calibrated against real
/// hardware. If a profile assembled from the drawing places every key on the cell that was
/// measured at the device, then the files were restating what the drawing already says.
/// </para>
/// <para>
/// Skips without the vendor's software, like the other drawing tests — there is nothing to build
/// from on a machine that has none.
/// </para>
/// </remarks>
public class FromDrawingTests
{
    private static SdkDeviceDescription DeathStalker()
    {
        // The scan codes the hardware reports, taken from the calibrated profile's key list so
        // that this describes the same board.
        var measured = MeasuredKeys.Load();
        var reported = new List<SdkKey>();

        foreach (var key in measured)
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

    private static (DeviceProfile Built, IReadOnlyList<KeyDefinition> Measured)? Built()
    {
        var device = DeathStalker();

        if (SvgLayoutSource.Find(device) is not { } drawing)
        {
            return null;
        }

        if (AttachedDeviceProfile.FromDrawing(device, drawing) is not { } built)
        {
            return null;
        }

        return (built, MeasuredKeys.Load());
    }

    [Fact]
    public void BuildsAProfileFromTheDrawingAlone()
    {
        if (Built() is not { } pair)
        {
            return;
        }

        Assert.Equal("Razer DeathStalker V2", pair.Built.Name);
        Assert.Equal("ISO-DE", pair.Built.PhysicalLayout);
        Assert.Equal(6, pair.Built.Matrix.Rows);
        Assert.Equal(22, pair.Built.Matrix.Columns);
    }

    /// <summary>
    /// Every key the hardware has, and no key it does not. The measurement is the board.
    /// </summary>
    [Fact]
    public void CarriesTheSameKeysAsTheMeasuredKeyboard()
    {
        if (Built() is not { } pair)
        {
            return;
        }

        var built = pair.Built.Keys.Select(k => k.Id).ToHashSet(StringComparer.Ordinal);
        var calibrated = pair.Measured.Select(k => k.Id).ToHashSet(StringComparer.Ordinal);

        var missing = calibrated.Except(built).Order().ToArray();
        var extra = built.Except(calibrated).Order().ToArray();

        Assert.True(
            missing.Length == 0 && extra.Length == 0,
            $"missing: {string.Join(", ", missing)}{Environment.NewLine}"
            + $"extra:   {string.Join(", ", extra)}");
    }

    /// <summary>
    /// The decisive one. Every key must land on the cell that was measured at the hardware — if
    /// this holds, the shipped layouts carry nothing the drawing does not.
    /// </summary>
    [Fact]
    public void PlacesEveryKeyOnTheCellMeasuredAtTheDevice()
    {
        if (Built() is not { } pair)
        {
            return;
        }

        var measured = pair.Measured.ToDictionary(k => k.Id, StringComparer.Ordinal);
        var disagreements = new List<string>();

        foreach (var key in pair.Built.Keys)
        {
            if (!measured.TryGetValue(key.Id, out var reference) || reference.Row is null)
            {
                continue;
            }

            if (reference.Row != key.Row || reference.Column != key.Column)
            {
                disagreements.Add(
                    $"{key.Id}: built ({key.Row},{key.Column}) measured ({reference.Row},{reference.Column})");
            }
        }

        Assert.True(
            disagreements.Count == 0,
            $"{disagreements.Count} key(s) on the wrong cell:{Environment.NewLine}  "
            + string.Join(Environment.NewLine + "  ", disagreements));
    }

    [Fact]
    public void EveryKeyHasACellToLight()
    {
        if (Built() is not { } pair)
        {
            return;
        }

        var dark = pair.Built.Keys
            .Where(k => k.Row is null || k.Column is null)
            .Select(k => k.Id)
            .ToArray();

        // fn is the exception on these boards: the protocol has no right Windows key, so fn shares
        // that cell — and it is placed, not dark. Anything dark here is a gap in the table.
        Assert.True(dark.Length == 0, $"No cell for: {string.Join(", ", dark)}");
    }

    /// <summary>The result has to survive the same checks a contributed profile would.</summary>
    [Fact]
    public void TheBuiltProfileIsValid()
    {
        if (Built() is not { } pair)
        {
            return;
        }

        var problems = DeviceProfileValidator.Validate(pair.Built);

        Assert.True(
            problems.Count == 0,
            $"{problems.Count} problem(s):{Environment.NewLine}  " + string.Join(Environment.NewLine + "  ", problems));
    }

    /// <summary>The casing and the printed legends come along, since they are the point.</summary>
    [Fact]
    public void BringsTheCasingAndTheLegends()
    {
        if (Built() is not { } pair)
        {
            return;
        }

        Assert.NotNull(pair.Built.Legend);
        Assert.NotEmpty(pair.Built.Legend.Path);
        Assert.NotNull(pair.Built.Legend.Chassis);
        Assert.NotEmpty(pair.Built.Legend.Chassis);
    }
}
