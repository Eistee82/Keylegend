using System.Text.Json;
using System.Text.Json.Serialization;
using Keylegend.Core.Input;
using Keylegend.Core.Lighting;
using Keylegend.Core.Shortcuts;

namespace Keylegend.Core.Profiles;

/// <summary>
/// The on-disk form of a shipped application profile — see <c>profiles/FORMAT.md</c>.
/// </summary>
/// <remarks>
/// Kept apart from <see cref="ApplicationProfile"/> for the same reason
/// <see cref="Configuration.StoredSettings"/> is: colours are text, enums are names, and the
/// runtime types stay free to change shape without invalidating eighty shipped files.
/// </remarks>
public sealed record ProfileDocument
{
    [JsonPropertyName("$schema")]
    public string? Schema { get; init; }

    public int FormatVersion { get; init; } = 1;

    public string Id { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    /// <summary><c>app</c> or <c>game</c>.</summary>
    public string Kind { get; init; } = "app";

    public ProfileMatchDocument Match { get; init; } = new();

    /// <summary>Key id → colour and label.</summary>
    public Dictionary<string, HighlightDocument> Highlights { get; init; } = [];

    /// <summary>Modifier combination as text (<c>Ctrl+Shift</c>) → the commands it carries.</summary>
    public Dictionary<string, ShortcutSetDocument> Shortcuts { get; init; } = [];

    public const int SupportedFormatVersion = 1;

    /// <summary>
    /// Converts to the runtime form, collecting anything unreadable rather than throwing.
    /// </summary>
    /// <remarks>
    /// Problems are reported instead of raised because both callers want them: the loader logs
    /// them and carries on with what parsed, and the test over the shipped profiles fails the
    /// build on any of them. A malformed colour costs that one key, not the profile.
    /// </remarks>
    public ApplicationProfile ToRuntime(ProfileSource source, IList<string>? problems = null)
    {
        var highlights = new Dictionary<string, KeyHighlight>(StringComparer.Ordinal);

        foreach (var (keyId, highlight) in Highlights)
        {
            if (RgbColorText.TryParse(highlight.Colour, out var colour))
            {
                highlights[keyId] = new KeyHighlight(colour, highlight.Label);
            }
            else
            {
                problems?.Add($"{Id}: highlight '{keyId}' has an unreadable colour '{highlight.Colour}'.");
            }
        }

        var shortcuts = new Dictionary<ModifierKeys, ShortcutSet>();

        foreach (var (combination, document) in Shortcuts)
        {
            if (!ModifierCombination.TryParse(combination, out var modifiers))
            {
                problems?.Add($"{Id}: '{combination}' is not a modifier combination.");
                continue;
            }

            shortcuts[modifiers] = document.ToRuntime(Id, combination, problems);
        }

        return new ApplicationProfile(
            Id: Id,
            Name: string.IsNullOrWhiteSpace(Name) ? Id : Name,
            Kind: string.Equals(Kind, "game", StringComparison.OrdinalIgnoreCase)
                ? ProfileKind.Game
                : ProfileKind.App,
            Source: source,
            Match: Match.ToRuntime(),
            Highlights: highlights,
            Shortcuts: shortcuts);
    }

    /// <summary>Captures a runtime profile for writing, used by the export command.</summary>
    public static ProfileDocument From(ApplicationProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);

        return new ProfileDocument
        {
            Id = profile.Id,
            Name = profile.Name,
            Kind = profile.Kind == ProfileKind.Game ? "game" : "app",
            Match = new ProfileMatchDocument
            {
                Processes = [.. profile.Match.Processes],
                AppliesToGames = profile.Match.AppliesToGames,
                Priority = profile.Match.Priority,
                TitleContains = [.. profile.Match.TitleContains ?? []]
            },
            // Ordered by key throughout. Two reasons: a saved settings file then produces a
            // readable diff instead of reshuffling itself on every write, and comparing a
            // section against the shipped one becomes a plain comparison rather than something
            // that has to reason about ordering.
            Highlights = profile.Highlights
                .OrderBy(e => e.Key, StringComparer.Ordinal)
                .ToDictionary(
                    e => e.Key,
                    e => new HighlightDocument { Colour = e.Value.Colour.ToString(), Label = e.Value.Label }),
            Shortcuts = profile.Shortcuts
                .Select(e => (Key: ModifierCombination.Format(e.Key), Value: e.Value))
                .OrderBy(e => e.Key, StringComparer.Ordinal)
                .ToDictionary(e => e.Key, e => ShortcutSetDocument.From(e.Value))
        };
    }
}

/// <summary>Which applications a profile applies to, as written down.</summary>
public sealed record ProfileMatchDocument
{
    public List<string> Processes { get; init; } = [];

    public bool AppliesToGames { get; init; }

    public int Priority { get; init; }

    /// <summary>
    /// Only for programs that share an executable name with something unrelated — LibreOffice's
    /// modules, or anything running as <c>javaw</c>. See <see cref="ProfileMatch"/>.
    /// </summary>
    public List<string> TitleContains { get; init; } = [];

    public ProfileMatch ToRuntime()
        => new([.. Processes], AppliesToGames, Priority, TitleContains.Count > 0 ? [.. TitleContains] : null);
}

/// <summary>A pinned key, as written down.</summary>
public sealed record HighlightDocument
{
    public string Colour { get; init; } = string.Empty;

    public string? Label { get; init; }
}

/// <summary>One modifier layer, as written down.</summary>
public sealed record ShortcutSetDocument
{
    /// <summary>The character the key types, lower case. Ctrl+Z is <c>"z"</c>.</summary>
    public Dictionary<string, ShortcutDocument> Characters { get; init; } = [];

    /// <summary>Key id, for keys that type no character.</summary>
    public Dictionary<string, ShortcutDocument> Keys { get; init; } = [];

    public ShortcutSet ToRuntime(string profileId, string combination, IList<string>? problems = null)
    {
        var characters = new Dictionary<string, Shortcut>(StringComparer.OrdinalIgnoreCase);
        var keys = new Dictionary<string, Shortcut>(StringComparer.Ordinal);

        foreach (var (character, shortcut) in Characters)
        {
            if (shortcut.TryToRuntime(out var runtime))
            {
                characters[character] = runtime;
            }
            else
            {
                problems?.Add(
                    $"{profileId}: {combination}+{character} has an unknown group '{shortcut.Group}'.");
            }
        }

        foreach (var (keyId, shortcut) in Keys)
        {
            if (shortcut.TryToRuntime(out var runtime))
            {
                keys[keyId] = runtime;
            }
            else
            {
                problems?.Add(
                    $"{profileId}: {combination}+{keyId} has an unknown group '{shortcut.Group}'.");
            }
        }

        return new ShortcutSet(characters, keys);
    }

    public static ShortcutSetDocument From(ShortcutSet set) => new()
    {
        Characters = set.Characters
            .OrderBy(e => e.Key, StringComparer.Ordinal)
            .ToDictionary(
                e => e.Key,
                e => new ShortcutDocument { Group = e.Value.Group.ToString(), Label = e.Value.Label }),
        Keys = set.Keys
            .OrderBy(e => e.Key, StringComparer.Ordinal)
            .ToDictionary(
                e => e.Key,
                e => new ShortcutDocument { Group = e.Value.Group.ToString(), Label = e.Value.Label })
    };
}

/// <summary>One command, as written down.</summary>
/// <remarks>
/// Reads two shapes. <c>{ "group": "Edit", "label": "Undo" }</c> is what is written now;
/// a bare <c>"Edit"</c> is what format 1 settings files contain, from before shortcuts had
/// labels. Accepting both means an existing settings file keeps working rather than losing
/// every shortcut the user had edited.
/// </remarks>
[JsonConverter(typeof(ShortcutDocumentConverter))]
public sealed record ShortcutDocument
{
    public string Group { get; init; } = string.Empty;

    /// <summary>What the command does — "Duplicate layer", not "Ctrl+J".</summary>
    public string? Label { get; init; }

    public bool TryToRuntime(out Shortcut shortcut)
    {
        if (Enum.TryParse<FunctionGroup>(Group, ignoreCase: true, out var group))
        {
            shortcut = new Shortcut(group, Label);
            return true;
        }

        shortcut = null!;
        return false;
    }
}

/// <summary>Reads a shortcut written either as an object or as a bare group name.</summary>
internal sealed class ShortcutDocumentConverter : JsonConverter<ShortcutDocument>
{
    public override ShortcutDocument Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        // Format 1: "Edit"
        if (reader.TokenType == JsonTokenType.String)
        {
            return new ShortcutDocument { Group = reader.GetString() ?? string.Empty };
        }

        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException("A shortcut must be an object or a group name.");
        }

        var group = string.Empty;
        string? label = null;

        while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
        {
            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                continue;
            }

            var property = reader.GetString();
            reader.Read();

            if (string.Equals(property, "group", StringComparison.OrdinalIgnoreCase))
            {
                group = reader.GetString() ?? string.Empty;
            }
            else if (string.Equals(property, "label", StringComparison.OrdinalIgnoreCase))
            {
                label = reader.GetString();
            }
            else
            {
                reader.Skip();
            }
        }

        return new ShortcutDocument { Group = group, Label = label };
    }

    public override void Write(
        Utf8JsonWriter writer,
        ShortcutDocument value,
        JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString("group", value.Group);

        if (value.Label is not null)
        {
            writer.WriteString("label", value.Label);
        }

        writer.WriteEndObject();
    }
}

/// <summary>Parsing <c>#RRGGBB</c> without letting a bad value throw.</summary>
internal static class RgbColorText
{
    public static bool TryParse(string? hex, out RgbColor colour)
    {
        try
        {
            colour = RgbColor.FromHex(hex!);
            return true;
        }
        catch (Exception ex) when (ex is FormatException or ArgumentNullException)
        {
            colour = RgbColor.Off;
            return false;
        }
    }
}
