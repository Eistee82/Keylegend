namespace Keylegend.Core.Devices;

/// <summary>
/// Checks a device profile for the mistakes that actually happen when someone contributes
/// a new keyboard by hand: duplicate keys, two keys driving the same LED, cells outside the
/// matrix, or geometry that has drifted off the canvas.
/// </summary>
public static class DeviceProfileValidator
{
    /// <summary>
    /// Returns one message per problem found. An empty result means the profile is usable.
    /// </summary>
    public static IReadOnlyList<string> Validate(DeviceProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);

        var problems = new List<string>();

        if (profile.FormatVersion is < 1 or > DeviceProfile.SupportedFormatVersion)
        {
            problems.Add(
                $"formatVersion {profile.FormatVersion} is not supported " +
                $"(this build understands 1..{DeviceProfile.SupportedFormatVersion}).");
        }

        if (string.IsNullOrWhiteSpace(profile.Name))
        {
            problems.Add("name must not be empty.");
        }

        if (profile.Canvas.Width <= 0 || profile.Canvas.Height <= 0)
        {
            problems.Add("canvas width and height must both be positive.");
        }

        if (profile.Matrix.Rows <= 0 || profile.Matrix.Columns <= 0)
        {
            problems.Add("matrix rows and columns must both be positive.");
        }

        if (profile.Keys.Count == 0)
        {
            problems.Add("profile contains no keys.");
            return problems;
        }

        foreach (var duplicate in profile.Keys
                     .GroupBy(k => k.Id, StringComparer.Ordinal)
                     .Where(g => g.Count() > 1))
        {
            problems.Add($"key id '{duplicate.Key}' appears {duplicate.Count()} times; ids must be unique.");
        }

        foreach (var key in profile.Keys)
        {
            if (key.Width <= 0 || key.Height <= 0)
            {
                problems.Add($"key '{key.Id}' has a non-positive size.");
            }

            foreach (var area in key.Areas())
            {
                if (area.X < 0 || area.Y < 0 ||
                    area.X + area.Width > profile.Canvas.Width + Tolerance ||
                    area.Y + area.Height > profile.Canvas.Height + Tolerance)
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

            // A key may legitimately have no cell yet - that is what calibration is for.
            // But half a coordinate is always a mistake.
            if (key.Row.HasValue != key.Column.HasValue)
            {
                problems.Add($"key '{key.Id}' has only one of row/column set; specify both or neither.");
                continue;
            }

            if (key.Row is { } row && (row < 0 || row >= profile.Matrix.Rows))
            {
                problems.Add($"key '{key.Id}' has row {row}, outside 0..{profile.Matrix.Rows - 1}.");
            }

            if (key.Column is { } column && (column < 0 || column >= profile.Matrix.Columns))
            {
                problems.Add($"key '{key.Id}' has column {column}, outside 0..{profile.Matrix.Columns - 1}.");
            }
        }

        // Two keys drawn on top of each other. Always a mistake, and an easy one to make by
        // hand: one wrong width and everything after it on the row slides under its neighbour.
        var rectangles = profile.Keys
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

        foreach (var collision in profile.Keys
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
