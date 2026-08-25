using Keylegend.Core.Devices;

namespace Keylegend.Core.Tests.Devices;

/// <summary>
/// Guards reading the vendor's keyboard drawing. The sample keeps the shapes that matter: the
/// two groups, a key repeated across both, and a legend outline.
/// </summary>
public class SvgKeyboardLayoutTests
{
    private const string Drawing = """
        <svg width="961" height="361" viewBox="0 0 961 361">
          <style>.led{fill:#FF00FF;}</style>
          <g id="Product">
            <path class="productfill" d="M919,37H175V8Z"/>
          </g>
          <g id="LED">
            <rect id="led-1" class="keyEsc led" x="44.5" y="53.5" width="36" height="35" data-assumed-key-name="Esc" data-col="0" data-row="0"/>
            <rect id="led-2" class="keyF1 led" x="123.5" y="53.5" width="35" height="35" data-assumed-key-name="F1" data-selection-group="functions"/>
            <rect id="led-3" class="keySpace led" x="200" y="300" width="141" height="35" data-assumed-key-name="Space"/>
            <path id="led-4" class="keyEnter led ledkeys" d="M622.5,140.5h-47c-2.21,0-4,1.79-4,4v27c0,2.21,1.79,4,4,4h.78c2.88,0,5.22,2.34,5.22,5.22v30.78c0,2.21,1.79,4,4,4h37c2.21,0,4-1.79,4-4v-67c0-2.21-1.79-4-4-4Z" data-assumed-key-name="Enter" data-col="14" data-row="3" data-selection-group="alphabets"/>
          </g>
          <g id="Selection">
            <rect id="led-1" class="selection" x="44.5" y="53.5" width="36" height="35" data-assumed-key-name="Esc"/>
          </g>
          <path class="characters" d="M695.9,63.8v2.5h-.5v-3.4h.5Z"/>
        </svg>
        """;

    [Fact]
    public void ReadsTheCanvasSize()
    {
        var layout = SvgKeyboardLayout.Parse(Drawing);

        Assert.NotNull(layout);
        Assert.Equal(961, layout.Width);
        Assert.Equal(361, layout.Height);
    }

    [Fact]
    public void ReadsKeyGeometryAndNames()
    {
        var layout = SvgKeyboardLayout.Parse(Drawing);

        var escape = layout!.Keys.Single(k => k.Name == "Esc");

        Assert.Equal(44.5, escape.X);
        Assert.Equal(53.5, escape.Y);
        Assert.Equal(36, escape.Width);
        Assert.Equal(35, escape.Height);
    }

    /// <summary>Wide keys are the ones a stand-in layout gets wrong, so they are worth checking.</summary>
    [Fact]
    public void KeepsTheWidthOfWideKeys()
    {
        var layout = SvgKeyboardLayout.Parse(Drawing);

        Assert.Equal(141, layout!.Keys.Single(k => k.Name == "Space").Width);
    }

    [Fact]
    public void ReadsTheSelectionGroup()
    {
        var layout = SvgKeyboardLayout.Parse(Drawing);

        Assert.Equal("functions", layout!.Keys.Single(k => k.Name == "F1").Group);
    }

    /// <summary>
    /// The drawing repeats every key in a second group for the selection outline. Counting those
    /// would double the keyboard.
    /// </summary>
    [Fact]
    public void CountsEachKeyOnce()
    {
        var layout = SvgKeyboardLayout.Parse(Drawing);

        Assert.Equal(4, layout!.Keys.Count);
        Assert.Single(layout.Keys, k => k.Name == "Esc");
    }

    [Fact]
    public void ReadsTheLegendOutline()
    {
        var layout = SvgKeyboardLayout.Parse(Drawing);

        Assert.NotNull(layout!.Legends);
        Assert.StartsWith("M695.9", layout.Legends);
    }

    /// <summary>
    /// The source belongs to another program and can change without warning. Anything not
    /// understood must be refused, so the shipped layout stays in charge.
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("not xml at all")]
    [InlineData(@"<svg viewBox=""0 0 961 361""><g id=""LED""></g></svg>")]
    [InlineData(@"<svg><g id=""LED""><rect id=""led-1"" x=""1"" y=""1"" width=""2"" height=""2""/></g></svg>")]
    public void RefusesAnythingItCannotRead(string svg)
    {
        Assert.Null(SvgKeyboardLayout.Parse(svg));
    }

    [Fact]
    public void RefusesKeysWithoutSize()
    {
        const string broken = """
            <svg viewBox="0 0 961 361"><g id="LED">
              <rect id="led-1" x="10" y="10" width="0" height="0" data-assumed-key-name="Esc"/>
            </g></svg>
            """;

        Assert.Null(SvgKeyboardLayout.Parse(broken));
    }

    /// <summary>
    /// The ISO Enter is L-shaped, so the drawing gives it a path rather than a rectangle. Reading
    /// only rectangles lost exactly one key on every ISO keyboard — and one missing key was enough
    /// for the geometry hand-off to discard the whole drawing and keep the shipped layout.
    /// </summary>
    [Fact]
    public void ReadsTheLShapedEnterFromItsOutline()
    {
        var layout = SvgKeyboardLayout.Parse(Drawing);

        var enter = Assert.Single(layout!.Keys, k => k.Name == "Enter");

        // Worked through by hand from the path. The outline's own straight edges give the grid
        // x 571.5 | 581.5 | 626.5 and y 140.5 | 175.5 | 215.5, and the shape covers three of the
        // four cells: the upper row entire, and the right-hand cell of the lower one.
        Assert.Equal("alphabets", enter.Group);
        Assert.NotNull(enter.Parts);

        var rectangles = enter.Rectangles().OrderBy(r => r.Y).ToArray();

        Assert.Equal(2, rectangles.Length);

        // Upper half: the wider one, and the larger by area, so it is the main rectangle.
        Assert.Equal(571.5, rectangles[0].X, 3);
        Assert.Equal(140.5, rectangles[0].Y, 3);
        Assert.Equal(55, rectangles[0].Width, 3);
        Assert.Equal(35, rectangles[0].Height, 3);

        // Lower half: narrower and further right — this is the step, and it has to land here
        // rather than wherever proportional scaling would put it.
        Assert.Equal(581.5, rectangles[1].X, 3);
        Assert.Equal(175.5, rectangles[1].Y, 3);
        Assert.Equal(45, rectangles[1].Width, 3);
        Assert.Equal(40, rectangles[1].Height, 3);

        // The main rectangle is the larger by area, which is where a legend belongs.
        Assert.Equal(571.5, enter.X, 3);
        Assert.Equal(55, enter.Width, 3);
    }

    /// <summary>
    /// A rounded corner cuts the box in; it never pushes it out. Taking the control points of the
    /// curves into account would inflate every key by the corner radius, so only the points the
    /// pen reaches are counted.
    /// </summary>
    [Fact]
    public void RoundedCornersDoNotEnlargeTheKey()
    {
        const string rounded = """
            <svg viewBox="0 0 100 100">
              <g id="LED">
                <path id="led-1" d="M10,10h20c2.21,0,4,1.79,4,4v20c0,2.21-1.79,4-4,4H10c-2.21,0-4-1.79-4-4V14c0-2.21,1.79-4,4-4Z" data-assumed-key-name="Rounded"/>
              </g>
            </svg>
            """;

        var layout = SvgKeyboardLayout.Parse(rounded);

        var key = Assert.Single(layout!.Keys);

        // The pen reaches x 6 to 34 and y 10 to 38; the control points sit outside that and are
        // deliberately ignored.
        Assert.Equal(6, key.X, 3);
        Assert.Equal(10, key.Y, 3);
        Assert.Equal(28, key.Width, 3);
        Assert.Equal(28, key.Height, 3);
    }

    /// <summary>
    /// An outline this cannot follow is skipped rather than guessed at — the caller then keeps the
    /// shipped geometry, which is the right outcome for a drawing that is not understood.
    /// </summary>
    [Theory]
    [InlineData("M10,10a5,5 0 0 1 10,10Z")]       // arc
    [InlineData("M10,10q5,5 10,10Z")]             // quadratic curve
    [InlineData("h20v20Z")]                       // no absolute move to start from
    [InlineData("M10,10c1,2,3Z")]                 // a curve missing numbers
    public void SkipsAnOutlineItCannotFollow(string d)
    {
        var svg = $"""
            <svg viewBox="0 0 100 100">
              <g id="LED">
                <rect id="led-1" x="1" y="1" width="9" height="9" data-assumed-key-name="Real"/>
                <path id="led-2" d="{d}" data-assumed-key-name="Unreadable"/>
              </g>
            </svg>
            """;

        var layout = SvgKeyboardLayout.Parse(svg);

        Assert.Single(layout!.Keys);
        Assert.Equal("Real", layout.Keys[0].Name);
    }
}
