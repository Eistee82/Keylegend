using Keylegend.Core.Lighting;

namespace Keylegend.Core.Tests.Lighting;

public class RgbColorTests
{
    [Fact]
    public void ConvertsToBgrIntegerAsChromaExpects()
    {
        // Chroma packs colours as (B << 16) | (G << 8) | R - not RGB.
        Assert.Equal(0x0000FF, new RgbColor(255, 0, 0).ToBgr());   // red
        Assert.Equal(0x00FF00, new RgbColor(0, 255, 0).ToBgr());   // green
        Assert.Equal(0xFF0000, new RgbColor(0, 0, 255).ToBgr());   // blue
        Assert.Equal(0xFFFFFF, new RgbColor(255, 255, 255).ToBgr());
    }

    [Fact]
    public void OffIsBlack() => Assert.Equal(0, RgbColor.Off.ToBgr());

    [Theory]
    [InlineData(1.0, 200)]
    [InlineData(0.5, 100)]
    [InlineData(0.0, 0)]
    public void ScaleAppliesBrightnessFactor(double factor, byte expected)
    {
        var scaled = new RgbColor(200, 200, 200).Scale(factor);

        Assert.Equal(expected, scaled.R);
        Assert.Equal(expected, scaled.G);
        Assert.Equal(expected, scaled.B);
    }

    [Fact]
    public void ScaleClampsOutOfRangeFactors()
    {
        Assert.Equal(new RgbColor(255, 255, 255), new RgbColor(255, 255, 255).Scale(5.0));
        Assert.Equal(RgbColor.Off, new RgbColor(255, 255, 255).Scale(-1.0));
    }

    [Theory]
    [InlineData("#FF8000", 255, 128, 0)]
    [InlineData("FF8000", 255, 128, 0)]
    public void ParsesHexNotation(string hex, byte r, byte g, byte b)
        => Assert.Equal(new RgbColor(r, g, b), RgbColor.FromHex(hex));

    [Theory]
    [InlineData("")]
    [InlineData("#GGGGGG")]
    [InlineData("#FFF")]
    public void RejectsMalformedHex(string hex)
        => Assert.Throws<FormatException>(() => RgbColor.FromHex(hex));
}
