namespace Keylegend.App.Views;

/// <summary>
/// What is printed on a keycap.
/// </summary>
/// <param name="Main">The unmodified character, or the key's name where it types nothing.</param>
/// <param name="Shifted">What Shift produces, if different — printed small below or beside.</param>
/// <param name="AltGr">What AltGr produces, if any — printed small at the bottom right.</param>
/// <param name="SideBySide">
/// Whether the two characters are printed next to each other rather than stacked. Punctuation
/// keys are printed this way — <c>, ;</c> and <c>. :</c> and <c># '</c> — presumably because a
/// comma stacked over a semicolon is hard to tell apart at that size.
/// </param>
/// <remarks>
/// Three positions rather than one, because that is how a keycap is actually printed: on a
/// German keyboard the 7 carries 7, / and { in exactly these places. Showing only the
/// unmodified character would make the preview less informative than the keyboard it depicts.
/// </remarks>
public readonly record struct KeyLegend(
    string Main,
    string? Shifted = null,
    string? AltGr = null,
    bool SideBySide = false)
{
    public bool IsEmpty => string.IsNullOrEmpty(Main) && Shifted is null && AltGr is null;
}
