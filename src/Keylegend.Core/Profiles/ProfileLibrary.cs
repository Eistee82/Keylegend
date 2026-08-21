using System.Text.Json;
using Keylegend.Core.Input;
using Keylegend.Core.Shortcuts;

namespace Keylegend.Core.Profiles;

/// <summary>One profile as the interface sees it: resolved, with its provenance.</summary>
/// <param name="Profile">The profile in effect — shipped, with any overrides laid over it.</param>
/// <param name="Overridden">
/// Which sections the user has taken over. A section listed here no longer follows the shipped
/// file and will not pick up improvements to it until reset.
/// </param>
/// <param name="Renamed">Whether the user has changed the name.</param>
/// <param name="Hidden">Whether the user has hidden this shipped profile.</param>
public sealed record ProfileEntry(
    ApplicationProfile Profile,
    IReadOnlySet<ProfileSection> Overridden,
    bool Renamed,
    bool Hidden)
{
    public string Id => Profile.Id;

    public ProfileSource Source => Profile.Source;

    /// <summary>Whether anything can be reset — false for an untouched or user-made profile.</summary>
    public bool CanReset => Source == ProfileSource.Shipped && (Overridden.Count > 0 || Renamed || Hidden);

    public bool IsOverridden(ProfileSection section) => Overridden.Contains(section);
}

/// <summary>
/// The whole set of profiles: the ones shipped with the application, the user's changes to
/// them, and the ones the user made from scratch.
/// </summary>
/// <remarks>
/// <para>
/// The point of this class is that shipped profiles stay readable. A user's edit is stored as
/// an override keyed on the profile id, never as a copy of the profile — which is what makes
/// "reset" possible at all, and what lets an updated build improve a profile the user has
/// partly edited.
/// </para>
/// <para>
/// Overrides are per section. See <see cref="ProfileSection"/> for why that granularity and
/// not per field or per profile.
/// </para>
/// </remarks>
public sealed class ProfileLibrary
{
    private readonly List<ApplicationProfile> _shipped;
    private readonly List<ProfileDocument> _user;
    private readonly Dictionary<string, ProfileOverride> _overrides;
    private readonly HashSet<string> _hidden;

    public ProfileLibrary(
        IEnumerable<ApplicationProfile>? shipped = null,
        IEnumerable<ProfileDocument>? userProfiles = null,
        IEnumerable<ProfileOverride>? overrides = null,
        IEnumerable<string>? hidden = null)
    {
        _shipped = [.. shipped ?? ShippedProfiles.All];
        _user = [.. userProfiles ?? []];
        _overrides = (overrides ?? [])
            .Where(o => !string.IsNullOrWhiteSpace(o.Id))
            .GroupBy(o => o.Id, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.Last(), StringComparer.Ordinal);
        _hidden = new HashSet<string>(hidden ?? [], StringComparer.Ordinal);
    }

    /// <summary>Every profile, shipped first, each resolved against its override.</summary>
    public IReadOnlyList<ProfileEntry> Entries
    {
        get
        {
            var entries = new List<ProfileEntry>(_shipped.Count + _user.Count);

            foreach (var profile in _shipped)
            {
                entries.Add(Resolve(profile));
            }

            foreach (var document in _user)
            {
                entries.Add(new ProfileEntry(
                    document.ToRuntime(ProfileSource.User),
                    new HashSet<ProfileSection>(),
                    Renamed: false,
                    Hidden: false));
            }

            return entries;
        }
    }

    /// <summary>What the engine selects from: everything except the hidden profiles.</summary>
    public ProfileCatalogue Catalogue()
        => new([.. Entries.Where(e => !e.Hidden).Select(e => e.Profile)]);

    public ProfileEntry? Find(string id)
        => Entries.FirstOrDefault(e => string.Equals(e.Id, id, StringComparison.Ordinal));

    // ---------------------------------------------------------------- editing

    /// <summary>
    /// Records a change to one section. For a shipped profile this writes an override and
    /// freezes that section; for a user profile it edits the profile itself.
    /// </summary>
    /// <remarks>
    /// A section that matches the shipped version is <em>not</em> frozen, and an existing
    /// override for it is dropped. Two things depend on this. Editing a value back to what it
    /// was undoes the override, as anyone would expect. And an interface that calls this on
    /// every lost focus — the obvious way to write one — cannot quietly freeze all ninety
    /// profiles while the user browses the list. That is too easy a mistake to leave to the
    /// caller, so the rule lives here.
    /// </remarks>
    public void Edit(string id, ApplicationProfile edited, ProfileSection section)
    {
        ArgumentNullException.ThrowIfNull(edited);

        var index = _user.FindIndex(p => string.Equals(p.Id, id, StringComparison.Ordinal));

        if (index >= 0)
        {
            _user[index] = ProfileDocument.From(edited);
            return;
        }

        var shipped = _shipped.FirstOrDefault(p => string.Equals(p.Id, id, StringComparison.Ordinal));

        if (shipped is null)
        {
            return;
        }

        var existing = _overrides.GetValueOrDefault(id) ?? new ProfileOverride { Id = id };
        var document = ProfileDocument.From(edited);
        var original = ProfileDocument.From(shipped);

        var reduced = section switch
        {
            ProfileSection.Match => existing with
            {
                Match = Same(document.Match, original.Match) ? null : document.Match
            },
            ProfileSection.Highlights => existing with
            {
                Highlights = Same(document.Highlights, original.Highlights) ? null : document.Highlights
            },
            ProfileSection.Shortcuts => existing with
            {
                Shortcuts = Same(document.Shortcuts, original.Shortcuts) ? null : document.Shortcuts
            },
            _ => existing
        };

        Store(id, reduced);
    }

    /// <summary>
    /// Whether two sections say the same thing.
    /// </summary>
    /// <remarks>
    /// Compared as serialised text. The sections are records holding lists and dictionaries, and
    /// a record's generated equality compares those by reference — so the obvious <c>==</c>
    /// reports every section as different and freezes everything. Writing out a field-by-field
    /// comparison instead would be one more place to forget a field when the format grows;
    /// this cannot fall out of step with what is actually stored, because it is what is stored.
    /// </remarks>
    private static bool Same<T>(T left, T right)
        => string.Equals(
            JsonSerializer.Serialize(left),
            JsonSerializer.Serialize(right),
            StringComparison.Ordinal);

    /// <summary>Renames a profile. Tracked apart from the sections, and undone by a full reset.</summary>
    public void Rename(string id, string name)
    {
        var index = _user.FindIndex(p => string.Equals(p.Id, id, StringComparison.Ordinal));

        if (index >= 0)
        {
            _user[index] = _user[index] with { Name = name };
            return;
        }

        if (!_shipped.Any(p => string.Equals(p.Id, id, StringComparison.Ordinal)))
        {
            return;
        }

        var existing = _overrides.GetValueOrDefault(id) ?? new ProfileOverride { Id = id };

        _overrides[id] = existing with { Name = name };
    }

    /// <summary>Gives one section back to the shipped version.</summary>
    public void Reset(string id, ProfileSection section)
    {
        if (!_overrides.TryGetValue(id, out var existing))
        {
            return;
        }

        var reduced = section switch
        {
            ProfileSection.Match => existing with { Match = null },
            ProfileSection.Highlights => existing with { Highlights = null },
            ProfileSection.Shortcuts => existing with { Shortcuts = null },
            _ => existing
        };

        Store(id, reduced);
    }

    /// <summary>Gives the whole profile back to the shipped version, including its name.</summary>
    public void ResetAll(string id)
    {
        _overrides.Remove(id);
        _hidden.Remove(id);
    }

    /// <summary>
    /// Hides a shipped profile. Shipped profiles are hidden rather than deleted — the file is
    /// still there, so "delete" would only last until the next start.
    /// </summary>
    public void Hide(string id)
    {
        if (_shipped.Any(p => string.Equals(p.Id, id, StringComparison.Ordinal)))
        {
            _hidden.Add(id);
        }
    }

    public void Show(string id) => _hidden.Remove(id);

    public void Add(ApplicationProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);

        _user.Add(ProfileDocument.From(profile with { Source = ProfileSource.User }));
    }

    /// <summary>Removes a user profile. Shipped profiles are hidden instead — see <see cref="Hide"/>.</summary>
    public void Remove(string id)
    {
        _user.RemoveAll(p => string.Equals(p.Id, id, StringComparison.Ordinal));
    }

    /// <summary>
    /// An id not yet taken, derived from the name so that a settings file stays readable.
    /// </summary>
    public string NewId(string name)
    {
        var slug = new string([.. (name ?? string.Empty)
            .ToLowerInvariant()
            .Select(c => char.IsAsciiLetterOrDigit(c) ? c : '-')])
            .Trim('-');

        while (slug.Contains("--", StringComparison.Ordinal))
        {
            slug = slug.Replace("--", "-", StringComparison.Ordinal);
        }

        if (slug.Length == 0)
        {
            slug = "profile";
        }

        var taken = Entries.Select(e => e.Id).ToHashSet(StringComparer.Ordinal);

        if (!taken.Contains(slug))
        {
            return slug;
        }

        for (var n = 2; ; n++)
        {
            var candidate = $"{slug}-{n}";

            if (!taken.Contains(candidate))
            {
                return candidate;
            }
        }
    }

    // ---------------------------------------------------------------- saving

    public IReadOnlyList<ProfileDocument> UserProfiles => _user;

    public IReadOnlyList<ProfileOverride> Overrides
        => [.. _overrides.Values.OrderBy(o => o.Id, StringComparer.Ordinal)];

    public IReadOnlyList<string> Hidden => [.. _hidden.Order(StringComparer.Ordinal)];

    // ---------------------------------------------------------------- internals

    private void Store(string id, ProfileOverride reduced)
    {
        // An override that no longer says anything is removed rather than kept as an empty
        // record, so a settings file does not accumulate residue from edits that were undone.
        if (reduced.IsEmpty)
        {
            _overrides.Remove(id);
        }
        else
        {
            _overrides[id] = reduced;
        }
    }

    private ProfileEntry Resolve(ApplicationProfile shipped)
    {
        if (!_overrides.TryGetValue(shipped.Id, out var over))
        {
            return new ProfileEntry(
                shipped,
                new HashSet<ProfileSection>(),
                Renamed: false,
                Hidden: _hidden.Contains(shipped.Id));
        }

        var sections = new HashSet<ProfileSection>();
        var profile = shipped;

        if (over.Match is { } match)
        {
            sections.Add(ProfileSection.Match);
            profile = profile with { Match = match.ToRuntime() };
        }

        if (over.Highlights is { } highlights)
        {
            sections.Add(ProfileSection.Highlights);
            profile = profile with { Highlights = ReadHighlights(highlights) };
        }

        if (over.Shortcuts is { } shortcuts)
        {
            sections.Add(ProfileSection.Shortcuts);
            profile = profile with { Shortcuts = ReadShortcuts(shipped.Id, shortcuts) };
        }

        if (!string.IsNullOrWhiteSpace(over.Name))
        {
            profile = profile with { Name = over.Name };
        }

        return new ProfileEntry(
            profile,
            sections,
            Renamed: !string.IsNullOrWhiteSpace(over.Name),
            Hidden: _hidden.Contains(shipped.Id));
    }

    private static IReadOnlyDictionary<string, KeyHighlight> ReadHighlights(
        Dictionary<string, HighlightDocument> stored)
    {
        var result = new Dictionary<string, KeyHighlight>(StringComparer.Ordinal);

        foreach (var (keyId, highlight) in stored)
        {
            if (RgbColorText.TryParse(highlight.Colour, out var colour))
            {
                result[keyId] = new KeyHighlight(colour, highlight.Label);
            }
        }

        return result;
    }

    private static IReadOnlyDictionary<ModifierKeys, ShortcutSet> ReadShortcuts(
        string id,
        Dictionary<string, ShortcutSetDocument> stored)
    {
        var result = new Dictionary<ModifierKeys, ShortcutSet>();

        foreach (var (combination, document) in stored)
        {
            if (ModifierCombination.TryParse(combination, out var modifiers))
            {
                result[modifiers] = document.ToRuntime(id, combination);
            }
        }

        return result;
    }
}

/// <summary>
/// A user's changes to one shipped profile. A <c>null</c> section follows the shipped file and
/// keeps receiving improvements; a set one is frozen until reset.
/// </summary>
public sealed record ProfileOverride
{
    public string Id { get; init; } = string.Empty;

    /// <summary>Set only if the user renamed the profile.</summary>
    public string? Name { get; init; }

    public ProfileMatchDocument? Match { get; init; }

    public Dictionary<string, HighlightDocument>? Highlights { get; init; }

    public Dictionary<string, ShortcutSetDocument>? Shortcuts { get; init; }

    public bool IsEmpty
        => Name is null && Match is null && Highlights is null && Shortcuts is null;
}
