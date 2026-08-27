using System.Text.Json;
using System.Text.Json.Serialization;
using Keylegend.Core.Devices;

namespace Keylegend.Core.Tests;

/// <summary>
/// The keys of the one keyboard that was measured by hand, as they were measured.
/// </summary>
/// <remarks>
/// <para>
/// This is a table of measurements, not a profile. The program has no notion of a keyboard
/// described in a file — it builds the attached one from the lighting service and the vendor's
/// drawing — so nothing here reaches into the program's own types beyond the key definition it
/// produces.
/// </para>
/// <para>
/// What the measurements are for: every key, on the cell it lights on real hardware, taken over
/// the same path the program lights by. Everything the program derives about a keyboard is checked
/// against them — that the construction from a drawing lands each key on the right cell, that the
/// matrix table is right, that the scan codes resolve. Delete the file and the proof goes with it;
/// there is no second board to compare against.
/// </para>
/// </remarks>
internal static class MeasuredKeys
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString
    };

    /// <summary>The measured keys, read fresh so one test cannot alter them for the next.</summary>
    public static IReadOnlyList<KeyDefinition> Load()
    {
        var path = TestPaths.MeasuredKeys;

        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"The measured keys are missing: {path}", path);
        }

        return JsonSerializer.Deserialize<List<KeyDefinition>>(File.ReadAllText(path), Options)
            ?? throw new InvalidOperationException($"{path} holds no keys.");
    }
}
