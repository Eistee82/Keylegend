using Keylegend.Core.Input;
using Keylegend.Core.Lighting;
using Keylegend.Core.Shortcuts;

namespace Keylegend.Core.Profiles;

/// <summary>What is in front of the user, as far as profile selection is concerned.</summary>
/// <param name="ProcessName">Executable name without extension, lower case.</param>
/// <param name="WindowTitle">Foreground window title.</param>
/// <param name="LooksLikeGame">Whether it appears to be a game or other full-screen application.</param>
public readonly record struct ForegroundContext(string ProcessName, string WindowTitle, bool LooksLikeGame)
{
    public static ForegroundContext None { get; } = new(string.Empty, string.Empty, false);
}

/// <summary>Where a profile came from. Decides whether it can be reset or only deleted.</summary>
public enum ProfileSource
{
    /// <summary>Shipped with the application, read-only, resettable.</summary>
    Shipped,

    /// <summary>Made by the user. There is nothing to reset to, only to delete.</summary>
    User
}

/// <summary>Groups the list in the interface. Carries no behaviour.</summary>
public enum ProfileKind
{
    App,
    Game
}

/// <summary>
/// The three parts of a profile the user can override independently.
/// </summary>
/// <remarks>
/// Overriding is per section rather than per field on purpose. Per field looks tidier but
/// produces states nobody configured: recolour W, and an update that adds Q leaves a mixture
/// the user never built. Per whole profile is the opposite problem — rename one thing and the
/// profile never sees an improvement again. Sections are the granularity at which a change is
/// still explainable: "you edited the highlights, so the highlights are yours now".
/// </remarks>
public enum ProfileSection
{
    Match,
    Highlights,
    Shortcuts
}

/// <summary>A key pinned to a fixed colour, regardless of what it types.</summary>
/// <param name="Label">
/// What the key does — "Forward", "Brush". Optional, and invisible on the hardware; the preview
/// shows it, and it is what makes a shipped profile checkable.
/// </param>
public sealed record KeyHighlight(RgbColor Colour, string? Label = null);

/// <summary>Which applications a profile applies to.</summary>
/// <param name="Processes">
/// Executable names without extension, case-insensitive. Empty means the profile is not
/// selected by process name.
/// </param>
/// <param name="AppliesToGames">
/// Whether this applies to anything detected as a game. Lets one profile cover every game
/// without naming them, which matters because a list of games is never complete.
/// </param>
/// <param name="Priority">
/// Higher wins where several profiles match. Only breaks ties: a profile naming the process
/// already outranks the general game profile.
/// </param>
/// <param name="TitleContains">
/// Additional condition on the window title: the profile applies only if the title contains one
/// of these, case-insensitively. Empty means the process name alone decides.
/// </param>
/// <remarks>
/// The title condition exists for one situation and should not be used outside it: several
/// unrelated programs sharing one executable name. LibreOffice runs Writer and Calc both as
/// <c>soffice</c>, and every Java program is <c>javaw</c>. Without a way to tell them apart,
/// one profile wins arbitrarily and the keyboard confidently shows the wrong shortcuts — a
/// silent wrong answer, which is worse than having no profile at all.
/// <para>
/// It is deliberately a substring test and not a pattern language. Matching on titles is
/// fragile: they are localised, they change with the open document, and a rule nobody can read
/// back is worse than no rule. If a profile needs more than "the title mentions Calc", the
/// profile is trying to solve the wrong problem.
/// </para>
/// </remarks>
public sealed record ProfileMatch(
    IReadOnlyList<string> Processes,
    bool AppliesToGames = false,
    int Priority = 0,
    IReadOnlyList<string>? TitleContains = null)
{
    public static ProfileMatch None { get; } = new([]);

    public bool Matches(ForegroundContext context)
    {
        if (AppliesToGames && context.LooksLikeGame)
        {
            return true;
        }

        return NamesTheProcess(context) && TitleMatches(context);
    }

    public bool NamesTheProcess(ForegroundContext context)
        => Processes.Any(p => string.Equals(p, context.ProcessName, StringComparison.OrdinalIgnoreCase));

    private bool TitleMatches(ForegroundContext context)
        => TitleContains is not { Count: > 0 }
            || TitleContains.Any(t =>
                context.WindowTitle.Contains(t, StringComparison.OrdinalIgnoreCase));
}

/// <summary>
/// A set of lighting rules bound to particular applications.
/// </summary>
/// <param name="Id">
/// Stable identifier. User overrides are keyed on it, so it must never change once shipped —
/// renaming one orphans somebody's edits.
/// </param>
/// <param name="Shortcuts">
/// Replacements for individual modifier layers, keyed by combination. A layer the profile does
/// not mention keeps the general set: Photoshop replaces what Ctrl means, but Win+E still opens
/// Explorer, because that is assigned by Windows and true regardless of what is in front.
/// </param>
public sealed record ApplicationProfile(
    string Id,
    string Name,
    ProfileKind Kind,
    ProfileSource Source,
    ProfileMatch Match,
    IReadOnlyDictionary<string, KeyHighlight> Highlights,
    IReadOnlyDictionary<ModifierKeys, ShortcutSet> Shortcuts)
{
    public static IReadOnlyDictionary<string, KeyHighlight> NoHighlights { get; }
        = new Dictionary<string, KeyHighlight>(StringComparer.Ordinal);

    public static IReadOnlyDictionary<ModifierKeys, ShortcutSet> NoShortcuts { get; }
        = new Dictionary<ModifierKeys, ShortcutSet>();

    /// <summary>Whether this profile applies to the given foreground application.</summary>
    public bool Matches(ForegroundContext context) => Match.Matches(context);

    /// <summary>
    /// Lays this profile's shortcut layers over the general ones. Returns the general catalogue
    /// unchanged when the profile defines none, so the common case allocates nothing.
    /// </summary>
    public ShortcutCatalogue ApplyShortcuts(ShortcutCatalogue general)
    {
        ArgumentNullException.ThrowIfNull(general);

        return Shortcuts.Count == 0 ? general : general.WithOverrides(Shortcuts);
    }
}

/// <summary>
/// All application profiles, and the rule for picking one.
/// </summary>
public sealed class ProfileCatalogue
{
    private readonly IReadOnlyList<ApplicationProfile> _profiles;

    public ProfileCatalogue(IReadOnlyList<ApplicationProfile> profiles)
    {
        _profiles = profiles ?? throw new ArgumentNullException(nameof(profiles));
    }

    public IReadOnlyList<ApplicationProfile> Profiles => _profiles;

    /// <summary>
    /// The profile that applies, or <c>null</c> for the default behaviour. Where several match,
    /// the highest priority wins; a profile naming the process outranks the general game
    /// profile, so a game with dedicated settings keeps them.
    /// </summary>
    public ApplicationProfile? Select(ForegroundContext context)
        => _profiles
            .Where(p => p.Matches(context))
            .OrderByDescending(p => p.Match.NamesTheProcess(context))
            .ThenByDescending(p => p.Match.Priority)
            .FirstOrDefault();

    public ApplicationProfile? ById(string id)
        => _profiles.FirstOrDefault(p => string.Equals(p.Id, id, StringComparison.Ordinal));

    public static ProfileCatalogue Empty { get; } = new([]);
}
