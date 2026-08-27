using Keylegend.Chroma;
using Keylegend.Core.Devices;

namespace Keylegend.Core.Tests.Chroma;

/// <summary>
/// Assembling the attached keyboard's profile, with a drawing written for the purpose.
/// </summary>
/// <remarks>
/// <para>
/// A drawing built here rather than read off this machine, so these run anywhere. What they pin
/// down is the reasoning: which keys are kept, what the model is called, where fn is. The other
/// half — that the result agrees with hardware — needs a real drawing and lives in
/// <see cref="FromDrawingTests"/>.
/// </para>
/// </remarks>
public class AttachedKeyboardBuilderTests
{
    /// <summary>A drawing of a small keyboard, named the way the vendor names keys.</summary>
    private static SvgKeyboardLayout Drawing(params string[] names)
    {
        var keys = new List<SvgKey>();
        double x = 10;

        foreach (var name in names)
        {
            keys.Add(new SvgKey(name, x, 10, 35, 35, "alphabets"));
            x += 39;
        }

        return new SvgKeyboardLayout(
            Width: x + 10,
            Height: 100,
            Keys: keys,
            Legends: "M10,10h5v5h-5Z",
            ProductId: 0x0295,
            LayoutId: 3,
            Chassis: [new SvgChassisShape("M0,0h100v60h-100Z", SvgChassisLayer.Body, new SvgRect(0, 0, 100, 60))]);
    }

    /// <summary>A device reporting the scan codes of the ids given.</summary>
    private static SdkDeviceDescription Device(
        IEnumerable<string> ids, string name = "Razer DeathStalker V2", int silent = 0)
    {
        var reported = new List<SdkKey>();

        foreach (var id in ids)
        {
            if (Core.Input.ScanCodes.TryGet(id, out var code))
            {
                var extended = (code & Core.Input.ScanCodes.ExtendedPrefix) != 0;
                reported.Add(new SdkKey(code & 0xFF, extended, 0, 0));
            }
        }

        return new SdkDeviceDescription(
            name, 0x1532, 0x0295, LayoutId: 3,
            MatrixRows: 6, MatrixColumns: 22, reported, SilentKeys: silent);
    }

    [Fact]
    public void TakesTheModelNameFromTheAttachedDevice()
    {
        var drawing = Drawing("Esc", "A", "B");
        var device = Device(["Keyboard_Escape", "Keyboard_A", "Keyboard_B"]);

        var built = AttachedKeyboardBuilder.FromDrawing(device, drawing);

        Assert.NotNull(built);
        Assert.Equal("Razer DeathStalker V2", built.Name);
        Assert.Equal("ISO-DE", built.PhysicalLayout);
    }

    /// <summary>The geometry and the printed legends are the drawing's, which is the point.</summary>
    [Fact]
    public void TakesGeometryAndLegendsFromTheDrawing()
    {
        var drawing = Drawing("Esc", "A");
        var device = Device(["Keyboard_Escape", "Keyboard_A"]);

        var built = AttachedKeyboardBuilder.FromDrawing(device, drawing);

        Assert.NotNull(built);

        var escape = built.Keys.Single(k => k.Id == "Keyboard_Escape");

        Assert.Equal(35, escape.Width, 3);
        Assert.Equal(35, escape.Height, 3);
        Assert.NotNull(built.Legend);
        Assert.NotEmpty(built.Legend.Path);
    }

    /// <summary>
    /// The key right of the right Alt is fn on these boards, and the protocol has no right Windows
    /// key at all — so a layout that draws one there would label it with the wrong symbol.
    /// </summary>
    [Fact]
    public void LabelsTheRightWindowsKeyAsFn()
    {
        var drawing = Drawing("Esc", "Function");
        var device = Device(["Keyboard_Escape", "Keyboard_RightGui"]);

        var built = AttachedKeyboardBuilder.FromDrawing(device, drawing);

        Assert.NotNull(built);
        Assert.Equal("fn", built.Keys.Single(k => k.Id == "Keyboard_RightGui").Label);
    }

    /// <summary>
    /// A drawing describes a model; the hardware in front of you may be a variant of it. Keys the
    /// device does not report are dropped.
    /// </summary>
    [Fact]
    public void DropsKeysTheDeviceDoesNotReport()
    {
        var drawing = Drawing("Esc", "F7", "A");
        var device = Device(["Keyboard_Escape", "Keyboard_A"]);

        var built = AttachedKeyboardBuilder.FromDrawing(device, drawing);

        Assert.NotNull(built);
        Assert.DoesNotContain("Keyboard_F7", built.Keys.Select(k => k.Id));
        Assert.Contains("Keyboard_Escape", built.Keys.Select(k => k.Id));
    }

    /// <summary>
    /// Two cases look alike and must not be treated alike: a tenkeyless board does not report a
    /// number pad because it has none, while fn is not reported because it sends nothing — it is
    /// right there under your finger. The device says how many such silent keys it carries.
    /// </summary>
    [Fact]
    public void KeepsSilentKeysTheDeviceDeclares()
    {
        var drawing = Drawing("Esc", "Function", "A");
        var device = Device(["Keyboard_Escape", "Keyboard_A"], silent: 1);

        var built = AttachedKeyboardBuilder.FromDrawing(device, drawing);

        Assert.NotNull(built);
        Assert.Contains("Keyboard_RightGui", built.Keys.Select(k => k.Id));
    }

    [Fact]
    public void EveryKeptKeyCarriesItsMatrixCell()
    {
        var drawing = Drawing("Esc", "A", "Space", "NumPad7");
        var device = Device(["Keyboard_Escape", "Keyboard_A", "Keyboard_Space", "Keyboard_Num7"]);

        var built = AttachedKeyboardBuilder.FromDrawing(device, drawing);

        Assert.NotNull(built);
        Assert.All(built.Keys, k =>
        {
            Assert.NotNull(k.Row);
            Assert.NotNull(k.Column);
        });
    }

    /// <summary>A drawing whose keys mean nothing here is refused rather than half-read.</summary>
    [Fact]
    public void RefusesADrawingItCannotName()
    {
        var drawing = Drawing("Nonsense1", "Nonsense2");
        var device = Device(["Keyboard_Escape"]);

        Assert.Null(AttachedKeyboardBuilder.FromDrawing(device, drawing));
    }
}
