using Keylegend.Chroma;
using Keylegend.Core.Devices;

namespace Keylegend.Core.Tests.Chroma;

/// <summary>
/// The keyboard built from the vendor's drawing, against the one measured at real hardware.
/// </summary>
/// <remarks>
/// <para>
/// This is the test the whole approach rests on. The yardstick is the same one as everywhere
/// else: <c>razer-deathstalker-v2-de</c>, measured key by key at the device. If a keyboard
/// assembled from the drawing places every key on the cell that was measured there, then the
/// drawing and the protocol between them say everything a description of a keyboard can say.
/// </para>
/// <para>
/// Skips without the vendor's software, like the other drawing tests — there is nothing to build
/// from on a machine that has none. See <see cref="VendorFiles"/> for why that is a skip and not
/// a pass.
/// </para>
/// </remarks>
public class FromDrawingTests
{
    private static SdkDeviceDescription DeathStalker()
    {
        // The scan codes the hardware reports, taken from the measured key list so that this
        // describes the same board.
        var measured = MeasuredKeys.Load();
        var reported = new List<SdkKey>();

        foreach (var key in measured)
        {
            if (!Core.Input.ScanCodes.TryGet(key.Id, out var code))
            {
                continue;
            }

            if (code == Core.Input.ScanCodes.PauseSequence)
            {
                // The E1 sequence, filed whole and unextended — as the reader files it.
                reported.Add(new SdkKey(code, false, 0, 0));
                continue;
            }

            var extended = (code & Core.Input.ScanCodes.ExtendedPrefix) != 0;
            reported.Add(new SdkKey(code & 0xFF, extended, 0, 0));
        }

        return new SdkDeviceDescription(
            "Razer DeathStalker V2", 0x1532, 0x0295, LayoutId: 3,
            MatrixRows: 6, MatrixColumns: 22, reported);
    }

    /// <summary>
    /// The profile built from the drawing on this machine, beside the measurement it has to
    /// agree with. Skips the calling test if there is no drawing here to build from.
    /// </summary>
    private static (AttachedKeyboard Built, IReadOnlyList<KeyDefinition> Measured) Built()
    {
        var device = DeathStalker();
        var drawing = SvgLayoutSource.Find(device);

        Assert.SkipWhen(drawing is null, VendorFiles.NoDrawingForTheDevice);

        var built = AttachedKeyboardBuilder.FromDrawing(device, drawing);

        // Not a skip: the drawing is here and was read, so a profile has to come out of it.
        Assert.NotNull(built);

        return (built, MeasuredKeys.Load());
    }

    [Fact]
    public void BuildsAProfileFromTheDrawingAlone()
    {
        var pair = Built();

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
        var pair = Built();

        var built = pair.Built.Keys.Select(k => k.Id).ToHashSet(StringComparer.Ordinal);
        var measured = pair.Measured.Select(k => k.Id).ToHashSet(StringComparer.Ordinal);

        var missing = measured.Except(built).Order().ToArray();
        var extra = built.Except(measured).Order().ToArray();

        Assert.True(
            missing.Length == 0 && extra.Length == 0,
            $"missing: {string.Join(", ", missing)}{Environment.NewLine}"
            + $"extra:   {string.Join(", ", extra)}");
    }

    /// <summary>
    /// The decisive one. Every key must land on the cell that was measured at the hardware: this
    /// is what says the cells come from the protocol and not from any per-model knowledge.
    /// </summary>
    [Fact]
    public void PlacesEveryKeyOnTheCellMeasuredAtTheDevice()
    {
        var pair = Built();

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
        var pair = Built();

        var dark = pair.Built.Keys
            .Where(k => k.Row is null || k.Column is null)
            .Select(k => k.Id)
            .ToArray();

        // fn is the exception on these boards: the protocol has no right Windows key, so fn shares
        // that cell — and it is placed, not dark. Anything dark here is a gap in the table.
        Assert.True(dark.Length == 0, $"No cell for: {string.Join(", ", dark)}");
    }

    /// <summary>The result has to survive the validator like any other description would.</summary>
    [Fact]
    public void TheBuiltKeyboardIsValid()
    {
        var pair = Built();

        var problems = AttachedKeyboardValidator.Validate(pair.Built);

        Assert.True(
            problems.Count == 0,
            $"{problems.Count} problem(s):{Environment.NewLine}  " + string.Join(Environment.NewLine + "  ", problems));
    }

    /// <summary>The casing and the printed legends come along, since they are the point.</summary>
    [Fact]
    public void BringsTheCasingAndTheLegends()
    {
        var pair = Built();

        Assert.NotNull(pair.Built.Legend);
        Assert.NotEmpty(pair.Built.Legend.Path);
        Assert.NotNull(pair.Built.Legend.Chassis);
        Assert.NotEmpty(pair.Built.Legend.Chassis);
    }
}
