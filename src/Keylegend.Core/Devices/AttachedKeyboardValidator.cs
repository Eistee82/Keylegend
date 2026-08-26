namespace Keylegend.Core.Devices;

/// <summary>
/// Checks a described keyboard for the mistakes a bad reading produces: duplicate keys, two keys
/// driving the same LED, cells outside the matrix, or geometry that has drifted off the canvas.
/// </summary>
/// <remarks>
/// The keyboard is assembled at run time from the vendor's drawing, so nobody types these numbers
/// and nobody proof-reads them either. What this catches is a drawing read wrongly — and it has
/// caught exactly that: keys overlapping on the canvas after the L-shaped Enter kept its own size
/// while its neighbours took the drawing's.
/// </remarks>
public static class AttachedKeyboardValidator
{
    /// <summary>
    /// Returns one message per problem found. An empty result means the keyboard is usable.
    /// </summary>
    public static IReadOnlyList<string> Validate(AttachedKeyboard keyboard)
    {
        ArgumentNullException.ThrowIfNull(keyboard);

        var problems = new List<string>();

        if (string.IsNullOrWhiteSpace(keyboard.Name))
        {
            problems.Add("name must not be empty.");
        }

        if (keyboard.Canvas.Width <= 0 || keyboard.Canvas.Height <= 0)
        {
            problems.Add("canvas width and height must both be positive.");
        }

        if (keyboard.Matrix.Rows <= 0 || keyboard.Matrix.Columns <= 0)
        {
            problems.Add("matrix rows and columns must both be positive.");
        }

        if (keyboard.Keys.Count == 0)
        {
            problems.Add("the keyboard has no keys.");
            return problems;
        }

        foreach (var duplicate in keyboard.Keys
                     .GroupBy(k => k.Id, StringComparer.Ordinal)
                     .Where(g => g.Count() > 1))
        {
            problems.Add($"key id '{duplicate.Key}' appears {duplicate.Count()} times; ids must be unique.");
        }

        foreach (var key in keyboard.Keys)
        {
            if (key.Width <= 0 || key.Height <= 0)
            {
                problems.Add($"key '{key.Id}' has a non-positive size.");
            }

            foreach (var area in key.Areas())
            {
                if (area.X < 0 || area.Y < 0 ||
                    area.X + area.Width > keyboard.Canvas.Width + Tolerance ||
                    area.Y + area.Height > keyboard.Canvas.Height + Tolerance)
                {
                    problems.Add($"key '{key.Id}' lies outside the canvas.");
                    break;
                }
            }

            foreach (var part in key.Parts ?? [])
            {
                if (part.Width <= 0 || part.Height <= 0)
                {
                    problems.Add($"key '{key.Id}' has a part with a non-positive size.");
                    break;
                }
            }

            // A key may legitimately have no cell: the protocol addresses fewer positions than a
            // keyboard has keys. But half a coordinate is always a mistake.
            if (key.Row.HasValue != key.Column.HasValue)
            {
                problems.Add($"key '{key.Id}' has only one of row/column set; specify both or neither.");
                continue;
            }

            if (key.Row is { } row && (row < 0 || row >= keyboard.Matrix.Rows))
            {
                problems.Add($"key '{key.Id}' has row {row}, outside 0..{keyboard.Matrix.Rows - 1}.");
            }

            if (key.Column is { } column && (column < 0 || column >= keyboard.Matrix.Columns))
            {
                problems.Add($"key '{key.Id}' has column {column}, outside 0..{keyboard.Matrix.Columns - 1}.");
            }
        }

        // Two keys drawn on top of each other. Always a mistake, and the one that actually
        // happened: one wrong width and everything after it on the row slides under its
        // neighbour.
        var rectangles = keyboard.Keys
            .SelectMany(key => key.Areas().Select(area => (key.Id, area)))
            .ToList();

        var reported = new HashSet<string>(StringComparer.Ordinal);
        for (var i = 0; i < rectangles.Count; i++)
        {
            for (var j = i + 1; j < rectangles.Count; j++)
            {
                var (firstId, first) = rectangles[i];
                var (secondId, second) = rectangles[j];

                if (string.Equals(firstId, secondId, StringComparison.Ordinal) ||
                    !Overlaps(first, second))
                {
                    continue;
                }

                // One message per pair, whichever of their rectangles happened to collide.
                var pair = string.CompareOrdinal(firstId, secondId) < 0
                    ? $"{firstId}|{secondId}"
                    : $"{secondId}|{firstId}";

                if (reported.Add(pair))
                {
                    problems.Add($"keys '{firstId}' and '{secondId}' overlap on the canvas.");
                }
            }
        }

        foreach (var collision in keyboard.Keys
                     .Where(k => k.Row.HasValue && k.Column.HasValue)
                     .GroupBy(k => (k.Row!.Value, k.Column!.Value))
                     .Where(g => g.Count() > 1))
        {
            var ids = string.Join(", ", collision.Select(k => k.Id));
            problems.Add(
                $"matrix cell ({collision.Key.Item1},{collision.Key.Item2}) is claimed by more than one key: {ids}.");
        }

        return problems;
    }

    /// <summary>
    /// Whether two key rectangles share any area. Keys that merely touch along an edge do not:
    /// that is what every key on a keyboard does to its neighbour.
    /// </summary>
    private static bool Overlaps(KeyArea first, KeyArea second)
        => first.X < second.X + second.Width - Tolerance &&
           second.X < first.X + first.Width - Tolerance &&
           first.Y < second.Y + second.Height - Tolerance &&
           second.Y < first.Y + first.Height - Tolerance;

    /// <summary>Rounding slack so that a key ending exactly at the canvas edge is not flagged.</summary>
    private const double Tolerance = 0.01;
}
