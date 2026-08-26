using System.Text;
using Keylegend.Chroma;
using Keylegend.Core.Devices;

namespace Keylegend.Core.Tests.Chroma;

/// <summary>
/// Checks that each printed legend is matched to the key it belongs to. Skips without the
/// vendor's software, like the rest of the drawing tests — see <see cref="VendorFiles"/>.
/// </summary>
public class LegendPlacementTests
{
    /// <summary>
    /// The attached keyboard as this machine's drawing describes it. Skips the calling test if
    /// there is no drawing here.
    /// </summary>
    private static (AttachedKeyboard Composed, SdkDeviceDescription Device) Attached()
    {
        var device = DeathStalker();
        var drawing = SvgLayoutSource.Find(device);

        Assert.SkipWhen(drawing is null, VendorFiles.NoDrawingForTheDevice);

        var built = AttachedKeyboardBuilder.FromDrawing(device, drawing);

        Assert.NotNull(built);

        return (built, device);
    }

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

    [Fact]
    public void TheGermanDrawingIsTheOneChosen()
    {
        var attached = Attached();

        var drawing = SvgLayoutSource.Find(DeathStalker());

        Assert.NotNull(drawing);
        Assert.Equal(3, drawing.LayoutId);
        Assert.Equal(0x0295, drawing.ProductId);
    }

    [Fact]
    public void EveryKeyThatCanBeMatchedIsMatched()
    {
        var attached = Attached();

        var drawn = attached.Composed.Legend?.DrawnKeys;

        Assert.NotNull(drawn);

        var missing = attached.Composed.Keys
            .Where(k => !drawn.ContainsKey(k.Id))
            .Select(k => k.Id)
            .ToArray();

        Assert.True(
            missing.Length == 0,
            $"No drawn counterpart for: {string.Join(", ", missing)}");
    }

    /// <summary>
    /// The one that matters for what is shown: a legend must sit on its own key. Measured as the
    /// distance between the two centres after the board-wide mapping — anything approaching a key
    /// height means the legend of the neighbouring row is what gets drawn.
    /// </summary>
    [Fact]
    public void NoLegendIsMatchedToTheWrongRow()
    {
        var attached = Attached();

        var legend = attached.Composed.Legend;

        Assert.NotNull(legend);
        Assert.NotNull(legend.DrawnKeys);

        var report = new StringBuilder();

        foreach (var key in attached.Composed.Keys)
        {
            if (!legend.DrawnKeys.TryGetValue(key.Id, out var drawn))
            {
                continue;
            }

            var mappedCentreX = ((drawn.X + (drawn.Width / 2)) * legend.ScaleX) + legend.OffsetX;
            var mappedCentreY = ((drawn.Y + (drawn.Height / 2)) * legend.ScaleY) + legend.OffsetY;

            // Compared over everything the key covers. An L-shaped Enter gets one outline across
            // both halves, so its centre sits between them and never over the upper half alone.
            double left = double.MaxValue, top = double.MaxValue;
            double right = double.MinValue, bottom = double.MinValue;

            foreach (var part in key.Areas())
            {
                left = Math.Min(left, part.X);
                top = Math.Min(top, part.Y);
                right = Math.Max(right, part.X + part.Width);
                bottom = Math.Max(bottom, part.Y + part.Height);
            }

            var dx = Math.Abs(((left + right) / 2) - mappedCentreX);
            var dy = Math.Abs(((top + bottom) / 2) - mappedCentreY);

            // Half a key is the point at which a nudge starts pulling a neighbour's legend in.
            if (dx > (right - left) / 2 || dy > (bottom - top) / 2)
            {
                report.AppendLine(
                    $"  {key.Id}: off by {dx:N1} x {dy:N1} "
                    + $"(key covers {right - left:N1} x {bottom - top:N1})");
            }
        }

        Assert.True(report.Length == 0, $"Legends matched too far from their key:{Environment.NewLine}{report}");
    }
}
