using Keylegend.Core.Devices;
using Keylegend.Core.Input;

namespace Keylegend.Chroma;

/// <summary>
/// Names the physical layout the attached keyboard has, from the number the lighting service
/// reports.
/// </summary>
/// <remarks>
/// The service states the physical layout outright, as a number, which removes the guesswork the
/// program used to do: it no longer has to infer German from what Windows reports, and it cannot
/// take a German board for a US one. All that is left to do with the number is give it a name —
/// choosing a file by it stopped being necessary once the vendor's drawing supplied the geometry.
/// </remarks>
public static class LayoutTemplates
{
    /// <summary>
    /// Physical layout per layout id, as the vendor numbers them. Verified against the attached
    /// hardware: a German keyboard reports 3.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Twelve of these have a layout of their own. The rest are keyboards whose printed
    /// characters differ but whose keys sit in the same places — Greek, Chinese, Korean, Thai,
    /// Hebrew and Arabic boards are ANSI underneath, because those scripts are typed through an
    /// input method rather than off the caps. They are mapped to the shape they physically have.
    /// </para>
    /// <para>
    /// The grouping is not guesswork: the vendor's own software loads one drawing for United
    /// States, Greek, Traditional Chinese and Korean together, and another for United Kingdom,
    /// Turkish, Swiss, Italian and Portuguese together. Where a layout of ours matches a group
    /// exactly it is used; otherwise the group's shape is.
    /// </para>
    /// <para>
    /// Every id the vendor knows is listed. A keyboard whose layout we cannot name still gets
    /// drawn — but it gets drawn as the wrong shape, and a missing key is worse than a legend in
    /// the wrong alphabet.
    /// </para>
    /// </remarks>
    private static readonly Dictionary<int, string> Layouts = new()
    {
        [1] = "ANSI-US",
        [2] = "ANSI-US",      // Greek — ANSI board, script via input method
        [3] = "ISO-DE",
        [4] = "ISO-FR",
        [5] = "ISO-RU",
        [6] = "ISO-UK",
        [7] = "ISO-NORDIC",
        [8] = "ANSI-US",      // Traditional Chinese — grouped with US by the vendor
        [9] = "ANSI-US",      // Korean — likewise
        [10] = "ISO-UK",      // Turkish — grouped with UK by the vendor
        [11] = "ANSI-US",     // Thai
        [12] = "JIS-JP",
        [13] = "ABNT2-BR",
        [14] = "ANSI-US",     // Latin American Spanish
        [15] = "ISO-CH",
        [16] = "ISO-ES",
        [17] = "ISO-IT",
        [18] = "ISO-PT",
        [19] = "ANSI-US",     // Hebrew
        [20] = "ANSI-US",     // Arabic
        [21] = "ISO-RU",      // Ukrainian — Cyrillic, same shape as Russian
    };

    /// <summary>The physical layout for a layout id, or <c>null</c> if it is not one we draw.</summary>
    private static string? PhysicalLayout(int layoutId)
        => Layouts.TryGetValue(layoutId, out var layout) ? layout : null;

    /// <summary>
    /// The physical layout for a layout id, falling back to what the drawing itself shows.
    /// </summary>
    /// <remarks>
    /// A profile's <c>physicalLayout</c> is descriptive — it says what shape of board this is —
    /// so an unknown layout id is no reason to give up. The drawing settles the shape either way:
    /// an ISO board carries the extra key beside Enter, a JIS board carries the Japanese ones.
    /// </remarks>
    public static string NameOf(int layoutId, bool iso, bool japanese)
        => PhysicalLayout(layoutId)
            ?? (japanese ? "JIS-JP" : iso ? "ISO" : "ANSI-US");
}
