using System.Collections.Immutable;
using System.Reflection;
using System.Text.Json;

namespace Keylegend.Core.Profiles;

/// <summary>
/// The profiles shipped with the application, read from resources embedded in this assembly.
/// </summary>
/// <remarks>
/// <para>
/// Embedded rather than loaded from a folder beside the executable. Three reasons, and all of
/// them matter. A single-file release carries them with no loose folder to
/// lose. Nothing on disk can be edited by accident, which is what makes "reset to shipped" mean
/// something. And a missing file becomes a build error rather than a program that silently has
/// no profiles.
/// </para>
/// <para>
/// Adding a profile is therefore adding a JSON file under <c>profiles/</c> — no code, matching
/// the rule that device support is data.
/// </para>
/// </remarks>
public static class ShippedProfiles
{
    private const string Prefix = "keylegend.profiles.";

    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    private static readonly Lazy<Loaded> Cache = new(Read, isThreadSafe: true);

    /// <summary>Every shipped profile, ordered by kind then name.</summary>
    public static ImmutableArray<ApplicationProfile> All => Cache.Value.Profiles;

    /// <summary>
    /// Anything that would not parse. Empty in a healthy build — a test fails on any entry, so
    /// a broken profile cannot reach a release.
    /// </summary>
    public static ImmutableArray<string> Problems => Cache.Value.Problems;

    public static ApplicationProfile? ById(string id)
        => All.FirstOrDefault(p => string.Equals(p.Id, id, StringComparison.Ordinal));

    /// <summary>The catalogue used when the user has configured nothing.</summary>
    public static ProfileCatalogue Create() => new([.. All]);

    private static Loaded Read()
    {
        var assembly = typeof(ShippedProfiles).Assembly;
        var problems = new List<string>();
        var profiles = new List<ApplicationProfile>();

        foreach (var name in assembly.GetManifestResourceNames().Order(StringComparer.Ordinal))
        {
            if (!name.StartsWith(Prefix, StringComparison.Ordinal))
            {
                continue;
            }

            if (ReadOne(assembly, name, problems) is { } profile)
            {
                profiles.Add(profile);
            }
        }

        // Two profiles claiming one id would make overrides ambiguous — whichever loaded second
        // would silently take the other's saved edits. Drop the later one and say so.
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var unique = new List<ApplicationProfile>(profiles.Count);

        foreach (var profile in profiles)
        {
            if (seen.Add(profile.Id))
            {
                unique.Add(profile);
            }
            else
            {
                problems.Add($"{profile.Id}: more than one shipped profile claims this id.");
            }
        }

        return new Loaded(
            [.. unique.OrderBy(p => p.Kind).ThenBy(p => p.Name, StringComparer.CurrentCultureIgnoreCase)],
            [.. problems]);
    }

    private static ApplicationProfile? ReadOne(Assembly assembly, string name, List<string> problems)
    {
        try
        {
            using var stream = assembly.GetManifestResourceStream(name);

            if (stream is null)
            {
                problems.Add($"{name}: the embedded resource could not be opened.");
                return null;
            }

            var document = JsonSerializer.Deserialize<ProfileDocument>(stream, Options);

            if (document is null)
            {
                problems.Add($"{name}: the profile is empty.");
                return null;
            }

            if (document.FormatVersion > ProfileDocument.SupportedFormatVersion)
            {
                problems.Add(
                    $"{name}: format version {document.FormatVersion} is newer than this build understands.");
                return null;
            }

            if (string.IsNullOrWhiteSpace(document.Id))
            {
                problems.Add($"{name}: the profile has no id.");
                return null;
            }

            return document.ToRuntime(ProfileSource.Shipped, problems);
        }
        catch (JsonException ex)
        {
            problems.Add($"{name}: {ex.Message}");
            return null;
        }
    }

    private sealed record Loaded(
        ImmutableArray<ApplicationProfile> Profiles,
        ImmutableArray<string> Problems);
}
