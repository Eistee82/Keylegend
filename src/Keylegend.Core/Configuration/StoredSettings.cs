using Keylegend.Core.Input;
using Keylegend.Core.Lighting;
using Keylegend.Core.Profiles;
using Keylegend.Core.Shortcuts;

namespace Keylegend.Core.Configuration;

/// <summary>
/// The persisted form of everything the user can change.
/// </summary>
/// <remarks>
/// Deliberately separate from the runtime types. Colours are stored as <c>#RRGGBB</c> and
/// enums by name, so a settings file stays readable and editable by hand, and renaming an
/// enum member or reordering a record's parameters does not silently invalidate saved files.
/// </remarks>
public sealed record StoredSettings
{
    public int FormatVersion { get; init; } = 3;

    public int IdleTimeoutSeconds { get; init; } = 60;

    /// <summary>
    /// Whether the lighting is handed back after <see cref="IdleTimeoutSeconds"/> of quiet.
    /// Switched off, Keylegend keeps the keyboard until it is paused or closed.
    /// </summary>
    /// <remarks>
    /// A flag of its own rather than a special value in the seconds, so that switching the
    /// hand-back off and on again returns the period the user had set instead of the default.
    /// It also keeps an existing settings file readable: the seconds still say what they say.
    /// </remarks>
    public bool HandBackWhenIdle { get; init; } = true;

    public double Brightness { get; init; } = 1.0;

    public bool StartWithWindows { get; init; }

    public bool UseApplicationProfiles { get; init; } = true;

    /// <summary>
    /// Interface language: <c>Automatic</c>, <c>English</c> or <c>German</c>. Stored by name so
    /// the file stays readable and adding a language does not renumber the existing ones.
    /// </summary>
    /// <remarks>
    /// <c>Automatic</c> follows Windows. It is the default because a program that ignores the
    /// system language for no reason is a nuisance — the setting exists for the cases where the
    /// two should differ, such as taking English screenshots on a German machine.
    /// </remarks>
    public string Language { get; init; } = "Automatic";

    /// <summary>Category name → <c>#RRGGBB</c>.</summary>
    public Dictionary<string, string> CategoryColours { get; init; } = [];

    /// <summary>Function group name → <c>#RRGGBB</c>.</summary>
    public Dictionary<string, string> GroupColours { get; init; } = [];

    /// <summary>Lock key name → on/off colours.</summary>
    public Dictionary<string, StoredLockColours> LockColours { get; init; } = [];

    /// <summary>
    /// Profiles the user made from scratch, stored whole because there is nothing to compare
    /// them against.
    /// </summary>
    public List<ProfileDocument> UserProfiles { get; init; } = [];

    /// <summary>
    /// Changes to shipped profiles, keyed by profile id. Only the sections the user touched are
    /// present — the rest keeps following the shipped file, so an updated build still improves
    /// a profile that has been partly edited.
    /// </summary>
    public List<ProfileOverride> ProfileOverrides { get; init; } = [];

    /// <summary>
    /// Shipped profiles the user hid. Hidden rather than deleted: the file is embedded in the
    /// program, so deleting one would only last until the next start.
    /// </summary>
    public List<string> HiddenProfiles { get; init; } = [];

    /// <summary>
    /// Format 1 only. Profiles used to be stored whole, with no way to tell a shipped one from
    /// a hand-made one. On load they all become user profiles: keeping someone's work matters
    /// more than guessing which entries were once shipped and silently discarding them.
    /// </summary>
    public List<StoredProfile> Profiles { get; init; } = [];

    /// <summary>
    /// Shortcut sets, keyed by combination as text (<c>Win+Shift</c>). Empty means the shipped
    /// sets are used; a saved file therefore picks up improvements to them until the user has
    /// edited something.
    /// </summary>
    public List<StoredShortcutSet> Shortcuts { get; init; } = [];

    /// <summary>Highest version this build understands.</summary>
    public const int SupportedFormatVersion = 3;

    /// <summary>Captures the current runtime state for saving.</summary>
    public static StoredSettings From(
        TimeSpan idleTimeout,
        ColourScheme scheme,
        ProfileLibrary profiles,
        bool startWithWindows,
        bool useApplicationProfiles,
        ShortcutCatalogue? shortcuts = null,
        string? language = null,
        bool handBackWhenIdle = true)
    {
        ArgumentNullException.ThrowIfNull(scheme);
        ArgumentNullException.ThrowIfNull(profiles);

        return new StoredSettings
        {
            // Never is not a number of seconds; the flag below carries that, and the period keeps
            // whatever the user had set so switching the hand-back back on restores it.
            IdleTimeoutSeconds = idleTimeout == Session.SessionManager.Never
                ? 60
                : (int)idleTimeout.TotalSeconds,
            HandBackWhenIdle = handBackWhenIdle && idleTimeout != Session.SessionManager.Never,
            Brightness = scheme.Brightness,
            StartWithWindows = startWithWindows,
            UseApplicationProfiles = useApplicationProfiles,
            Language = language ?? "Automatic",
            CategoryColours = Changed(scheme.Categories, ColourScheme.Default.Categories),
            GroupColours = Changed(scheme.Groups, ColourScheme.Default.Groups),
            LockColours = new Dictionary<string, StoredLockColours>
            {
                ["NumLock"] = StoredLockColours.From(scheme.NumLock),
                ["CapsLock"] = StoredLockColours.From(scheme.CapsLock),
                ["ScrollLock"] = StoredLockColours.From(scheme.ScrollLock)
            },
            UserProfiles = [.. profiles.UserProfiles],
            ProfileOverrides = [.. profiles.Overrides],
            HiddenProfiles = [.. profiles.Hidden],
            Shortcuts = shortcuts is null ? [] : Changed(shortcuts)
        };
    }

    /// <summary>
    /// The shortcut layers that differ from the shipped ones — the only ones worth writing down.
    /// </summary>
    /// <remarks>
    /// Saving every layer, which is what this used to do, quietly froze the untouched ones: a
    /// file listing Win+Shift exactly as it shipped is indistinguishable from one where somebody
    /// meant it that way, so later additions to that layer could never appear. Writing only what
    /// was actually changed keeps the rest open to improvement.
    /// </remarks>
    /// <summary>
    /// The colours that differ from the shipped palette, and only those.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Writing all of them was the same fault the shortcut sets had, left in one place after being
    /// fixed in the other, and it has the same consequence: a file that states every colour states
    /// the shipped ones too, so it pins them. Improving the palette then reaches nobody who has
    /// ever run the program — their settings quietly override it with what the palette used to be.
    /// </para>
    /// <para>
    /// It surfaced when the Tools colour was changed from a pale blue to orange because the pale
    /// one was reported as looking white on the hardware. The new colour did not appear. The
    /// setting was not wrong, and neither was the palette; the file simply remembered a default
    /// as though it were a decision.
    /// </para>
    /// </remarks>
    private static Dictionary<string, string> Changed<TKey>(
        IReadOnlyDictionary<TKey, RgbColor> current,
        IReadOnlyDictionary<TKey, RgbColor> shipped)
        where TKey : notnull
    {
        var changed = new Dictionary<string, string>();

        foreach (var (key, colour) in current)
        {
            if (!shipped.TryGetValue(key, out var original) || original != colour)
            {
                changed[key.ToString()!] = colour.ToString();
            }
        }

        return changed;
    }

    private static List<StoredShortcutSet> Changed(ShortcutCatalogue shortcuts)
    {
        var shipped = DefaultShortcuts.Create().Sets;
        var changed = new List<StoredShortcutSet>();

        foreach (var (modifiers, set) in shortcuts.Sets)
        {
            if (!shipped.TryGetValue(modifiers, out var original) || !Same(original, set))
            {
                changed.Add(StoredShortcutSet.From(modifiers, set));
            }
        }

        return changed;
    }

    /// <summary>Whether two shortcut sets say the same thing.</summary>
    private static bool Same(ShortcutSet first, ShortcutSet second)
        => Same(first.Characters, second.Characters) && Same(first.Keys, second.Keys);

    private static bool Same(
        IReadOnlyDictionary<string, Shortcut> first,
        IReadOnlyDictionary<string, Shortcut> second)
    {
        if (first.Count != second.Count)
        {
            return false;
        }

        foreach (var (key, value) in first)
        {
            if (!second.TryGetValue(key, out var other) || other != value)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Rebuilds the shortcut sets: the shipped ones, with any layer the user changed laid over
    /// them.
    /// </summary>
    /// <remarks>
    /// The saved layers are an overlay, not a replacement. Replacing was what this used to do,
    /// and it meant that anyone who had ever saved settings was frozen at the shortcuts of that
    /// day: a layer added later — Win+Alt, say — could never reach them, because their file said
    /// nothing about it and the shipped sets were discarded wholesale. What somebody edited
    /// stays edited; everything else keeps improving with the program.
    /// </remarks>
    public ShortcutCatalogue ToShortcutCatalogue()
    {
        var shipped = DefaultShortcuts.Create().Sets;
        var sets = new Dictionary<ModifierKeys, ShortcutSet>(shipped);

        foreach (var stored in Shortcuts)
        {
            if (stored.ToRuntime() is { } entry)
            {
                sets[entry.Modifiers] = shipped.TryGetValue(entry.Modifiers, out var original)
                    ? WithLabelsFrom(entry.Set, original)
                    : entry.Set;
            }
        }

        return new ShortcutCatalogue(sets);
    }

    /// <summary>
    /// Fills in labels a saved set is missing from the shipped one.
    /// </summary>
    /// <remarks>
    /// Older files recorded only which group a shortcut belonged to, not what it does. Reading
    /// one back therefore produced a keyboard that lit correctly but could no longer say what
    /// any of it meant, and saving again wrote the emptiness back — the text was gone for good.
    /// Where the shipped set still knows the wording for a shortcut, it is used; anything the
    /// user actually typed wins over it.
    /// </remarks>
    private static ShortcutSet WithLabelsFrom(ShortcutSet saved, ShortcutSet shipped)
        => new(
            Merge(saved.Characters, shipped.Characters, StringComparer.OrdinalIgnoreCase),
            Merge(saved.Keys, shipped.Keys, StringComparer.Ordinal));

    private static Dictionary<string, Shortcut> Merge(
        IReadOnlyDictionary<string, Shortcut> saved,
        IReadOnlyDictionary<string, Shortcut> shipped,
        StringComparer comparer)
    {
        var merged = new Dictionary<string, Shortcut>(comparer);

        foreach (var (key, shortcut) in saved)
        {
            merged[key] = string.IsNullOrEmpty(shortcut.Label)
                && shipped.TryGetValue(key, out var known)
                && known.Group == shortcut.Group
                    ? shortcut with { Label = known.Label }
                    : shortcut;
        }

        return merged;
    }

    /// <summary>
    /// Rebuilds the colour scheme, falling back to the default for anything missing or
    /// unreadable. A settings file that has lost a colour should cost that one colour, not the
    /// whole configuration.
    /// </summary>
    public ColourScheme ToColourScheme()
    {
        var categories = new Dictionary<KeyCategory, RgbColor>(ColourScheme.Default.Categories);
        var groups = new Dictionary<FunctionGroup, RgbColor>(ColourScheme.Default.Groups);

        // A file written before format 3 listed every colour, the untouched ones included, so it
        // cannot say which were decisions. What it can be compared against is the palette of the
        // time: an entry matching that was a default being echoed back, not a choice, and
        // honouring it would pin the old palette forever. Entries that differ are kept, because
        // those are the ones somebody actually set.
        var echoesDefaults = FormatVersion < 3;

        foreach (var (name, hex) in CategoryColours)
        {
            if (Enum.TryParse<KeyCategory>(name, out var category)
                && TryColour(hex, out var colour)
                && !(echoesDefaults && PaletteBeforeFormat3.WasCategoryDefault(category, colour)))
            {
                categories[category] = colour;
            }
        }

        foreach (var (name, hex) in GroupColours)
        {
            if (Enum.TryParse<FunctionGroup>(name, out var group)
                && TryColour(hex, out var colour)
                && !(echoesDefaults && PaletteBeforeFormat3.WasGroupDefault(group, colour)))
            {
                groups[group] = colour;
            }
        }

        return new ColourScheme
        {
            Categories = categories,
            Groups = groups,
            NumLock = Lock("NumLock", ColourScheme.Default.NumLock),
            CapsLock = Lock("CapsLock", ColourScheme.Default.CapsLock),
            ScrollLock = Lock("ScrollLock", ColourScheme.Default.ScrollLock),
            Brightness = Brightness is > 0 and <= 1 ? Brightness : 1.0
        };

        LockColours Lock(string name, LockColours fallback)
            => LockColours.TryGetValue(name, out var stored) ? stored.ToRuntime(fallback) : fallback;
    }

    /// <summary>
    /// Rebuilds the profile library: the shipped profiles, plus this file's overrides, hidden
    /// entries and hand-made profiles.
    /// </summary>
    /// <param name="shipped">
    /// The shipped profiles, or <c>null</c> for the ones embedded in the build. Passed in only
    /// by tests, which need a known set rather than whatever the build happens to carry.
    /// </param>
    /// <remarks>
    /// A format 1 file is migrated here rather than rewritten on disk: its whole-profile list
    /// becomes user profiles. Nothing is lost, and nothing is guessed — a format 1 file cannot
    /// say which of its entries came from the shipped set, so treating them all as the user's
    /// is the only reading that never discards work. The shipped profiles then appear alongside
    /// them, which may mean two entries for one program until the user removes one.
    /// </remarks>
    public ProfileLibrary ToProfileLibrary(IEnumerable<ApplicationProfile>? shipped = null)
    {
        var migrated = FormatVersion < 2
            ? Profiles.Select(p => p.ToDocument())
            : [];

        return new ProfileLibrary(
            shipped,
            [.. UserProfiles, .. migrated],
            ProfileOverrides,
            HiddenProfiles);
    }

    /// <summary>
    /// The timeout the engine runs with — <see cref="Session.SessionManager.Never"/> when the
    /// hand-back is switched off.
    /// </summary>
    public TimeSpan ToIdleTimeout()
        => HandBackWhenIdle ? ToIdlePeriod() : Session.SessionManager.Never;

    /// <summary>
    /// The configured period, regardless of whether the hand-back is switched on. The interface
    /// shows this, so that turning the hand-back back on restores the period rather than the
    /// default.
    /// </summary>
    public TimeSpan ToIdlePeriod()
        => IdleTimeoutSeconds > 0 ? TimeSpan.FromSeconds(IdleTimeoutSeconds) : TimeSpan.FromSeconds(60);

    private static bool TryColour(string hex, out RgbColor colour)
    {
        try
        {
            colour = RgbColor.FromHex(hex);
            return true;
        }
        catch (Exception ex) when (ex is FormatException or ArgumentNullException)
        {
            colour = RgbColor.Off;
            return false;
        }
    }
}

/// <summary>Persisted form of one shortcut set.</summary>
public sealed record StoredShortcutSet
{
    /// <summary>Modifier combination as text, e.g. <c>Win+Shift</c>.</summary>
    public string Modifiers { get; init; } = string.Empty;

    /// <summary>Key id → command, for keys that type nothing.</summary>
    public Dictionary<string, ShortcutDocument> Keys { get; init; } = [];

    /// <summary>
    /// Typed character → command. Kept apart from <see cref="Keys"/> because Ctrl+Z means the
    /// key that types z, which is a different position on a German keyboard than on a US one.
    /// </summary>
    public Dictionary<string, ShortcutDocument> Characters { get; init; } = [];

    public static StoredShortcutSet From(ModifierKeys modifiers, ShortcutSet set) => new()
    {
        Modifiers = ModifierCombination.Format(modifiers),
        Keys = set.Keys.ToDictionary(
            e => e.Key,
            e => new ShortcutDocument { Group = e.Value.Group.ToString(), Label = e.Value.Label }),
        Characters = set.Characters.ToDictionary(
            e => e.Key,
            e => new ShortcutDocument { Group = e.Value.Group.ToString(), Label = e.Value.Label })
    };

    /// <summary>
    /// Rebuilds the set, or <c>null</c> if the combination is unreadable. Individual keys with
    /// an unknown group are skipped rather than losing the whole set.
    /// </summary>
    public (ModifierKeys Modifiers, ShortcutSet Set)? ToRuntime()
    {
        if (!ModifierCombination.TryParse(Modifiers, out var modifiers))
        {
            return null;
        }

        var keys = new Dictionary<string, Shortcut>(StringComparer.Ordinal);

        foreach (var (keyId, stored) in Keys)
        {
            if (stored.TryToRuntime(out var shortcut))
            {
                keys[keyId] = shortcut;
            }
        }

        var characters = new Dictionary<string, Shortcut>(StringComparer.OrdinalIgnoreCase);

        foreach (var (character, stored) in Characters)
        {
            if (stored.TryToRuntime(out var shortcut))
            {
                characters[character] = shortcut;
            }
        }

        return (modifiers, new ShortcutSet(characters, keys));
    }
}

/// <summary>Persisted form of a lock key's two colours.</summary>
public sealed record StoredLockColours(string On, string Off)
{
    public static StoredLockColours From(Lighting.LockColours colours)
        => new(colours.On.ToString(), colours.Off.ToString());

    public Lighting.LockColours ToRuntime(Lighting.LockColours fallback)
    {
        var on = Parse(On) ?? fallback.On;
        var off = Parse(Off) ?? fallback.Off;

        return new Lighting.LockColours(on, off);
    }

    private static RgbColor? Parse(string hex)
    {
        try
        {
            return RgbColor.FromHex(hex);
        }
        catch (Exception ex) when (ex is FormatException or ArgumentNullException)
        {
            return null;
        }
    }
}

/// <summary>
/// Format 1's whole-profile entry. Kept only so those files can still be read.
/// </summary>
/// <remarks>
/// Note what is missing: an id, and any record of where the profile came from. That is exactly
/// why the format changed — without an id there is nothing for an override to attach to, and
/// without provenance there is no such thing as resetting to the shipped version.
/// </remarks>
public sealed record StoredProfile
{
    public string Name { get; init; } = "Profile";

    public List<string> Processes { get; init; } = [];

    public bool AppliesToGames { get; init; }

    /// <summary>Key id → <c>#RRGGBB</c>.</summary>
    public Dictionary<string, string> KeyHighlights { get; init; } = [];

    public int Priority { get; init; }

    /// <summary>Converts to the current format, as a user profile with an id made from the name.</summary>
    public ProfileDocument ToDocument() => new()
    {
        Id = Slug(Name),
        Name = Name,
        Kind = AppliesToGames ? "game" : "app",
        Match = new ProfileMatchDocument
        {
            Processes = [.. Processes],
            AppliesToGames = AppliesToGames,
            Priority = Priority
        },
        Highlights = KeyHighlights.ToDictionary(
            e => e.Key,
            e => new HighlightDocument { Colour = e.Value })
    };

    private static string Slug(string name)
    {
        var slug = new string([.. (name ?? string.Empty)
            .ToLowerInvariant()
            .Select(c => char.IsAsciiLetterOrDigit(c) ? c : '-')])
            .Trim('-');

        while (slug.Contains("--", StringComparison.Ordinal))
        {
            slug = slug.Replace("--", "-", StringComparison.Ordinal);
        }

        return slug.Length == 0 ? "profile" : slug;
    }
}
