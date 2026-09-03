using System.Windows;
using System.Windows.Media;
using Keylegend.Core.Devices;

namespace Keylegend.App.Views;

/// <summary>
/// Cuts the board-wide outline of the printed legends into one shape per key.
/// </summary>
/// <remarks>
/// <para>
/// The vendor's drawing carries every character on the keyboard as a single path, and every key
/// has to paint its own characters in its own colour. The preview did that by handing the whole
/// path to each key in turn under a clip — which is correct, and slow in a way that does not show
/// up anywhere one would look for it.
/// </para>
/// <para>
/// A clip saves the renderer no work. The path is rasterised in full and the result discarded
/// outside the clip, so a hundred and five keys meant a hundred and five passes over tens of
/// thousands of segments — and twice that for lit keys, whose glow is a second pass with a wide
/// round pen. None of it appears in the interface thread's own timings, because <c>OnRender</c>
/// only records the instructions; the cost lands on the rendering thread afterwards. Measured on
/// a DeathStalker V2: three hundred and fifty milliseconds from a keypress to the picture
/// changing, fifty with the glow left out, and thirty-four with the legends left out entirely.
/// </para>
/// <para>
/// So the path is cut once instead. Where to cut is not a guess: the clip the preview applies is,
/// in the drawing's own coordinates, exactly the rectangle the drawing gives that key. The same
/// rectangle therefore says which characters belong to it.
/// </para>
/// </remarks>
internal static class LegendParts
{
    /// <summary>
    /// One frozen shape per key the drawing names, carrying only the characters printed on it.
    /// </summary>
    /// <param name="legend">The board-wide outline, in the drawing's own coordinates.</param>
    /// <param name="drawnKeys">Where the drawing puts each key, in those same coordinates.</param>
    public static IReadOnlyDictionary<string, Geometry> SplitByKey(
        Geometry legend,
        IReadOnlyDictionary<string, KeyArea> drawnKeys)
    {
        ArgumentNullException.ThrowIfNull(legend);
        ArgumentNullException.ThrowIfNull(drawnKeys);

        // Figures rather than the geometry as a whole: they are the strokes the path is built
        // from, and a character is some of them. Converting is the one expensive step here, and
        // it happens once per keyboard rather than once per frame.
        var whole = PathGeometry.CreateFromGeometry(legend);

        var figures = new List<(PathFigure Figure, Rect Bounds)>(whole.Figures.Count);

        foreach (var figure in whole.Figures)
        {
            figure.Freeze();

            // Measured on its own, which is what makes a figure placeable at all. The bounds of
            // one figure are cheap; the geometry holding it is thrown away again.
            var single = new PathGeometry();
            single.Figures.Add(figure);

            figures.Add((figure, single.Bounds));
        }

        var parts = new Dictionary<string, Geometry>(drawnKeys.Count, StringComparer.Ordinal);

        foreach (var (id, drawn) in drawnKeys)
        {
            var share = new Rect(drawn.X, drawn.Y, drawn.Width, drawn.Height);
            var mine = new PathGeometry();

            foreach (var (figure, bounds) in figures)
            {
                // Touching, not containing. A character straddling two caps is drawn for both,
                // and each one's clip cuts it where the cap ends — which is what the board-wide
                // path under a clip did, and therefore what has to keep happening. Giving it to
                // the nearer key alone would rub out the half that hangs over.
                if (bounds.IntersectsWith(share))
                {
                    mine.Figures.Add(figure);
                }
            }

            // Even when it is empty. The preview decides from the drawing whether a key is
            // lettered from the outline or from the text labels, and a key the drawing names but
            // prints nothing on must stay on the first path — otherwise a blank cap would be
            // given a fallback name it does not carry.
            mine.Freeze();
            parts[id] = mine;
        }

        return parts;
    }
}
