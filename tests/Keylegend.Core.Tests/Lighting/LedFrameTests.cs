using Keylegend.Core.Lighting;

namespace Keylegend.Core.Tests.Lighting;

public class LedFrameTests
{
    [Fact]
    public void StartsCompletelyDark()
    {
        var frame = new LedFrame(6, 22);

        Assert.Equal(RgbColor.Off, frame[0, 0]);
        Assert.Equal(RgbColor.Off, frame[5, 21]);
    }

    [Fact]
    public void StoresAndReturnsColours()
    {
        var frame = new LedFrame(6, 22);

        frame.Set(3, 13, new RgbColor(10, 20, 30));

        Assert.Equal(new RgbColor(10, 20, 30), frame[3, 13]);
    }

    [Fact]
    public void ProducesABgrMatrixOfTheDeclaredShape()
    {
        var frame = new LedFrame(6, 22);
        frame.Set(1, 2, new RgbColor(255, 0, 0));

        var matrix = frame.ToBgrMatrix();

        Assert.Equal(6, matrix.Length);
        Assert.All(matrix, row => Assert.Equal(22, row.Length));
        Assert.Equal(0x0000FF, matrix[1][2]);
        Assert.Equal(0, matrix[0][0]);
    }

    [Fact]
    public void ClearTurnsEverythingOff()
    {
        var frame = new LedFrame(6, 22);
        frame.Set(2, 2, new RgbColor(1, 2, 3));

        frame.Clear();

        Assert.Equal(RgbColor.Off, frame[2, 2]);
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(0, -1)]
    [InlineData(6, 0)]
    [InlineData(0, 22)]
    public void RejectsCellsOutsideTheMatrix(int row, int column)
    {
        var frame = new LedFrame(6, 22);

        Assert.Throws<ArgumentOutOfRangeException>(() => frame.Set(row, column, RgbColor.Off));
    }

    [Fact]
    public void RejectsNonPositiveDimensions()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new LedFrame(0, 22));
        Assert.Throws<ArgumentOutOfRangeException>(() => new LedFrame(6, 0));
    }
}
