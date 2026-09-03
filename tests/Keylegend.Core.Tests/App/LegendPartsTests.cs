using System.Windows;
using System.Windows.Media;
using Keylegend.App.Views;
using Keylegend.Core.Devices;

namespace Keylegend.Core.Tests.App;

/// <summary>
/// Cutting the board-wide outline of the printed legends into one shape per key.
/// </summary>
/// <remarks>
/// <para>
/// Why it is cut at all. The drawing carries every character on the keyboard as a single path,
/// and the preview used to hand that whole path to every key in turn, clipped to the key. A clip
/// saves the renderer no work — the path is rasterised in full and the result is thrown away
/// outside the clip — so a hundred and five keys meant a hundred and five passes over tens of
/// thousands of segments, and twice that for lit keys, whose glow is a second pass with a wide
/// pen. Measured: three hundred and fifty milliseconds from a keypress to the picture changing,
/// against thirty-four with the legends left out entirely.
/// </para>
/// <para>
/// The cut has to be exact, not approximate: a character that reaches into a key must reach into
/// it afterwards too, because the clip that used to decide this is kept and would show it. So a
/// figure belongs to every key it touches, not to the nearest one.
/// </para>
/// </remarks>
public class LegendPartsTests
{
    /// <summary>Two ten-by-ten squares, a hundred units apart.</summary>
    private const string TwoSquares =
        "M 0 0 L 10 0 L 10 10 L 0 10 Z M 100 0 L 110 0 L 110 10 L 100 10 Z";

    private static Geometry Parse(string path) => Geometry.Parse(path);

    [Fact]
    public void GivesEachKeyOnlyWhatIsPrintedOnIt()
    {
        var parts = LegendParts.SplitByKey(
            Parse(TwoSquares),
            new Dictionary<string, KeyArea>
            {
                ["Keyboard_A"] = new(0, 0, 20, 20),
                ["Keyboard_B"] = new(95, 0, 20, 20)
            });

        Assert.Equal(new Rect(0, 0, 10, 10), parts["Keyboard_A"].Bounds);
        Assert.Equal(new Rect(100, 0, 10, 10), parts["Keyboard_B"].Bounds);
    }

    /// <summary>
    /// A character straddling two caps is drawn for both, and each one's clip cuts it. Handing it
    /// to the nearer key alone would rub out the half that hangs over.
    /// </summary>
    [Fact]
    public void GivesAKeyEveryCharacterThatReachesIntoIt()
    {
        var parts = LegendParts.SplitByKey(
            Parse("M 18 2 L 22 2 L 22 6 L 18 6 Z"),
            new Dictionary<string, KeyArea>
            {
                ["Keyboard_A"] = new(0, 0, 20, 20),
                ["Keyboard_B"] = new(20, 0, 20, 20)
            });

        Assert.False(parts["Keyboard_A"].IsEmpty());
        Assert.False(parts["Keyboard_B"].IsEmpty());
    }

    /// <summary>
    /// A key the drawing names but prints nothing on still gets an entry, and an empty one. The
    /// preview decides by the drawing whether a key is lettered from the outline or from the
    /// text labels, and an absent entry would send a blank key down the other path and print a
    /// fallback name on a cap that carries none.
    /// </summary>
    [Fact]
    public void GivesABlankKeyAnEntryOfItsOwn()
    {
        var parts = LegendParts.SplitByKey(
            Parse(TwoSquares),
            new Dictionary<string, KeyArea>
            {
                ["Keyboard_A"] = new(0, 0, 20, 20),
                ["Keyboard_Space"] = new(40, 40, 30, 20)
            });

        Assert.True(parts.ContainsKey("Keyboard_Space"));
        Assert.True(parts["Keyboard_Space"].IsEmpty());
    }

    [Fact]
    public void NamesExactlyTheKeysTheDrawingDoes()
    {
        var parts = LegendParts.SplitByKey(
            Parse(TwoSquares),
            new Dictionary<string, KeyArea> { ["Keyboard_A"] = new(0, 0, 20, 20) });

        Assert.Equal(["Keyboard_A"], parts.Keys.Order(StringComparer.Ordinal));
    }

    /// <summary>
    /// Frozen, because these are handed to the renderer on every frame and a thawed Freezable
    /// carries change tracking that costs on every one of them.
    /// </summary>
    [Fact]
    public void HandsBackFrozenShapes()
    {
        var parts = LegendParts.SplitByKey(
            Parse(TwoSquares),
            new Dictionary<string, KeyArea> { ["Keyboard_A"] = new(0, 0, 20, 20) });

        Assert.True(parts["Keyboard_A"].IsFrozen);
    }

    /// <summary>
    /// The whole point of the exercise: what a key is handed is its own share, not the board.
    /// </summary>
    [Fact]
    public void HandsEachKeyFarLessThanTheWholeBoard()
    {
        var whole = PathGeometry.CreateFromGeometry(Parse(TwoSquares));

        var parts = LegendParts.SplitByKey(
            Parse(TwoSquares),
            new Dictionary<string, KeyArea> { ["Keyboard_A"] = new(0, 0, 20, 20) });

        var share = PathGeometry.CreateFromGeometry(parts["Keyboard_A"]);

        Assert.Equal(2, whole.Figures.Count);
        Assert.Single(share.Figures);
    }
}
