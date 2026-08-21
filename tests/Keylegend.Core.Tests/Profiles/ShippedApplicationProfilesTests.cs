using System.Text.Json;
using Keylegend.Core.Devices;
using Keylegend.Core.Profiles;
using Keylegend.Core.Shortcuts;

namespace Keylegend.Core.Tests.Profiles;

/// <summary>
/// Guards every file under <c>profiles/</c>.
/// </summary>
/// <remarks>
/// <para>
/// This exists because of what a wrong entry costs. A profile that names the wrong key does not
/// fail loudly — it lights a key, and the user has no way to tell that the keyboard is lying to
/// them. At eighty profiles nobody reads them all, so the checks that can be automated must be.
/// </para>
/// <para>
/// Runs against the real files rather than a fixture, so a bad contribution fails the build
/// rather than shipping.
/// </para>
/// </remarks>
public class ShippedApplicationProfilesTests
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    /// <summary>
    /// Every key id any shipped device has. A profile naming something outside this is a typo:
    /// the union is used rather than one device's list because a profile is meant to work on
    /// every keyboard, and a full-size layout's num pad is legitimate even on a build whose only
    /// device is tenkeyless.
    /// </summary>
    private static readonly HashSet<string> KnownKeyIds = LoadKeyIds();

    private static IReadOnlyList<string> AllProfileFiles()
        => [.. Directory
            .EnumerateFiles(TestPaths.ProfilesDirectory, "*.json", SearchOption.AllDirectories)
            .Where(p => !string.Equals(Path.GetFileName(p), "schema.json", StringComparison.Ordinal))
            .Order(StringComparer.Ordinal)];

    public static TheoryData<string> ProfilePaths()
    {
        var data = new TheoryData<string>();

        foreach (var path in AllProfileFiles())
        {
            data.Add(path);
        }

        return data;
    }

    [Fact]
    public void ProfilesAreShipped()
    {
        Assert.NotEmpty(ShippedProfiles.All);
    }

    [Fact]
    public void EveryProfileFileIsEmbedded()
    {
        var onDisk = new HashSet<string>(StringComparer.Ordinal);

        foreach (var path in AllProfileFiles())
        {
            onDisk.Add(Path.GetFileNameWithoutExtension(path));
        }

        var embedded = new HashSet<string>(StringComparer.Ordinal);

        foreach (var profile in ShippedProfiles.All)
        {
            embedded.Add(profile.Id);
        }

        // A file added to profiles/ but not picked up by the csproj glob would silently do
        // nothing, which is the sort of failure that goes unnoticed for months.
        Assert.True(
            onDisk.SetEquals(embedded),
            $"On disk but not embedded: {Join(onDisk.Except(embedded))}. " +
            $"Embedded but not on disk: {Join(embedded.Except(onDisk))}.");
    }

    [Fact]
    public void NothingFailedToLoad()
    {
        Assert.True(
            ShippedProfiles.Problems.Length == 0,
            Environment.NewLine + "  " + string.Join(Environment.NewLine + "  ", ShippedProfiles.Problems));
    }

    /// <summary>
    /// Two profiles claiming one executable name, with nothing to tell them apart, is a silent
    /// wrong answer: one wins arbitrarily and the keyboard shows Calc's shortcuts to somebody
    /// writing a letter. Where an executable really is shared, every profile using it has to
    /// narrow itself by window title.
    /// </summary>
    [Fact]
    public void ProfilesSharingAnExecutableAreToldApartByTitle()
    {
        var byProcess = new Dictionary<string, List<ApplicationProfile>>(StringComparer.OrdinalIgnoreCase);

        foreach (var profile in ShippedProfiles.All)
        {
            foreach (var process in profile.Match.Processes)
            {
                if (!byProcess.TryGetValue(process, out var sharing))
                {
                    byProcess[process] = sharing = [];
                }

                sharing.Add(profile);
            }
        }

        var problems = new List<string>();

        foreach (var (process, sharing) in byProcess.Where(e => e.Value.Count > 1))
        {
            var undistinguished = sharing
                .Where(p => p.Match.TitleContains is not { Count: > 0 })
                .Select(p => p.Id)
                .ToArray();

            if (undistinguished.Length > 0)
            {
                problems.Add(
                    $"'{process}' is claimed by {string.Join(", ", sharing.Select(p => p.Id).Order(StringComparer.Ordinal))}; " +
                    $"{string.Join(" and ", undistinguished.Order(StringComparer.Ordinal))} " +
                    "would win arbitrarily. Add titleContains.");
            }
        }

        Assert.True(
            problems.Count == 0,
            Environment.NewLine + "  " + string.Join(Environment.NewLine + "  ", problems));
    }

    [Theory]
    [MemberData(nameof(ProfilePaths))]
    public void ProfileIsValid(string path)
    {
        var document = JsonSerializer.Deserialize<ProfileDocument>(File.ReadAllText(path), Options);

        Assert.NotNull(document);

        var problems = new List<string>();
        var expectedId = Path.GetFileNameWithoutExtension(path);
        var inGamesFolder = Path.GetFileName(Path.GetDirectoryName(path)) == "games";

        // The id is what user overrides attach to. If it drifted from the file name, a rename
        // would quietly orphan somebody's edits with nothing to warn them.
        if (!string.Equals(document.Id, expectedId, StringComparison.Ordinal))
        {
            problems.Add($"id is '{document.Id}' but the file is named '{expectedId}'.");
        }

        if (string.IsNullOrWhiteSpace(document.Name))
        {
            problems.Add("no name.");
        }

        var expectedKind = inGamesFolder ? "game" : "app";

        if (!string.Equals(document.Kind, expectedKind, StringComparison.Ordinal))
        {
            problems.Add($"kind is '{document.Kind}' but the file is under {expectedKind}s.");
        }

        CheckMatch(document, expectedId, problems);
        CheckHighlights(document, problems);
        CheckShortcuts(document, problems);

        // A profile with nothing in it selects itself over the default behaviour and then shows
        // exactly the default behaviour — so it can only ever be a mistake.
        if (document.Highlights.Count == 0 && document.Shortcuts.Count == 0)
        {
            problems.Add("neither highlights nor shortcuts; the profile would do nothing.");
        }

        Assert.True(
            problems.Count == 0,
            $"{expectedId}:{Environment.NewLine}  " +
            string.Join(Environment.NewLine + "  ", problems));
    }

    private static void CheckMatch(ProfileDocument document, string id, List<string> problems)
    {
        if (document.Match.Processes.Count == 0 && !document.Match.AppliesToGames)
        {
            problems.Add("no processes and not the generic game profile; nothing would select it.");
        }

        foreach (var process in document.Match.Processes)
        {
            if (process.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            {
                problems.Add($"process '{process}' includes the extension; it is matched without one.");
            }

            if (process.Any(char.IsUpper))
            {
                problems.Add($"process '{process}' is not lower case.");
            }

            if (string.IsNullOrWhiteSpace(process))
            {
                problems.Add("a process name is empty.");
            }
        }

        // Exactly one profile may claim every game. A second would make selection between them
        // arbitrary, and both would outrank nothing.
        if (document.Match.AppliesToGames && !string.Equals(id, "_generic", StringComparison.Ordinal))
        {
            problems.Add("appliesToGames is set, which belongs to the generic game profile alone.");
        }
    }

    private static void CheckHighlights(ProfileDocument document, List<string> problems)
    {
        foreach (var (keyId, highlight) in document.Highlights)
        {
            if (!KnownKeyIds.Contains(keyId))
            {
                problems.Add($"highlight names '{keyId}', which is not a key on any shipped device.");
            }

            if (!IsColour(highlight.Colour))
            {
                problems.Add($"highlight '{keyId}' has colour '{highlight.Colour}', which is not #RRGGBB.");
            }
        }
    }

    private static void CheckShortcuts(ProfileDocument document, List<string> problems)
    {
        foreach (var (combination, set) in document.Shortcuts)
        {
            if (!ModifierCombination.TryParse(combination, out var modifiers))
            {
                problems.Add($"'{combination}' is not a modifier combination.");
                continue;
            }

            var canonical = ModifierCombination.Format(modifiers);

            if (!string.Equals(canonical, combination, StringComparison.Ordinal))
            {
                problems.Add($"'{combination}' should be written '{canonical}'.");
            }

            foreach (var (character, shortcut) in set.Characters)
            {
                if (character.Length != 1)
                {
                    problems.Add($"{combination}: '{character}' is not a single character.");
                }

                if (character.Any(char.IsUpper))
                {
                    problems.Add($"{combination}: character '{character}' is not lower case.");
                }

                Check(combination, character, shortcut);
            }

            foreach (var (keyId, shortcut) in set.Keys)
            {
                if (!KnownKeyIds.Contains(keyId))
                {
                    problems.Add($"{combination}: '{keyId}' is not a key on any shipped device.");
                }

                // A letter under "keys" is the mistake this whole split exists to prevent: it
                // would pin the shortcut to a US position, showing undo and redo swapped on a
                // German keyboard.
                if (keyId.Length == "Keyboard_X".Length && char.IsAsciiLetter(keyId[^1]))
                {
                    problems.Add(
                        $"{combination}: '{keyId}' is a letter key and belongs under 'characters', " +
                        "addressed by the character it types.");
                }

                Check(combination, keyId, shortcut);
            }
        }

        void Check(string combination, string key, ShortcutDocument shortcut)
        {
            if (string.IsNullOrWhiteSpace(shortcut.Label))
            {
                problems.Add($"{combination}+{key} has no label.");
            }
            else if (shortcut.Label.Contains("Ctrl", StringComparison.OrdinalIgnoreCase)
                || shortcut.Label.Contains("Shift", StringComparison.OrdinalIgnoreCase))
            {
                // The label says what the command does. Restating the key combination in it is
                // both useless and a sign the entry was filled in mechanically.
                problems.Add($"{combination}+{key}: the label '{shortcut.Label}' names keys instead of the command.");
            }

            if (!Enum.TryParse<FunctionGroup>(shortcut.Group, ignoreCase: true, out _))
            {
                problems.Add($"{combination}+{key} has an unknown group '{shortcut.Group}'.");
            }
        }
    }

    // There is deliberately no check for the same label appearing twice under one modifier.
    // It looked like a way to catch copy-and-paste slips, and it caught real aliases instead:
    // browsers close a tab with both Ctrl+W and Ctrl+F4, and move to the next one with both
    // Ctrl+Tab and Ctrl+PageDown. An alias and a slip are indistinguishable from here, and a
    // check that fires on correct data is worse than no check.

    private static bool IsColour(string? hex)
        => hex is { Length: 7 }
            && hex[0] == '#'
            && hex[1..].All(char.IsAsciiHexDigit);

    private static HashSet<string> LoadKeyIds()
    {
        var ids = new HashSet<string>(StringComparer.Ordinal);

        foreach (var path in Directory.EnumerateFiles(
            TestPaths.DevicesDirectory, "device.json", SearchOption.AllDirectories))
        {
            foreach (var key in DeviceProfileLoader.Load(path).Keys)
            {
                ids.Add(key.Id);
            }
        }

        return ids;
    }

    private static string Join(IEnumerable<string> values)
    {
        var list = values.Order(StringComparer.Ordinal).ToArray();

        return list.Length == 0 ? "none" : string.Join(", ", list);
    }
}
