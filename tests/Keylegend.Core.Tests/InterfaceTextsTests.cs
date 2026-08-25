using System.Xml.Linq;

namespace Keylegend.Core.Tests;

/// <summary>
/// The interface texts, across all eleven languages.
/// </summary>
/// <remarks>
/// <para>
/// Read from the resource files rather than through the compiled program, and that is the point: a
/// lookup at run time falls back to English when a translation is missing, which is right for a
/// user and useless for finding the gap. Here a missing entry is a missing entry.
/// </para>
/// <para>
/// This is the failure these tests exist for: adding a text to the English file and forgetting the
/// other ten. Nothing breaks, nothing warns, and ten languages quietly show an English sentence in
/// the middle of their own.
/// </para>
/// </remarks>
public class InterfaceTextsTests
{
    private static readonly string Directory = Path.Combine(
        TestPaths.RepositoryRoot, "src", "Keylegend.App", "Localisation");

    /// <summary>The neutral file, which is English and the reference for every other.</summary>
    private const string Neutral = "Strings.resx";

    private static IReadOnlyDictionary<string, string> Read(string fileName)
    {
        var path = Path.Combine(Directory, fileName);

        return XDocument.Load(path)
            .Root!
            .Elements("data")
            .ToDictionary(
                d => d.Attribute("name")!.Value,
                d => d.Element("value")?.Value ?? string.Empty,
                StringComparer.Ordinal);
    }

    private static IEnumerable<string> Translations()
        => System.IO.Directory
            .EnumerateFiles(Directory, "Strings.*.resx")
            .Select(Path.GetFileName)
            .Select(name => name!)
            .Order(StringComparer.Ordinal);

    /// <summary>
    /// Eleven languages: the neutral file plus ten translations. Named in the interface and in
    /// every README, so a language that quietly stops shipping is a promise broken.
    /// </summary>
    [Fact]
    public void AllElevenLanguagesArePresent()
    {
        var translations = Translations().ToArray();

        Assert.Equal(10, translations.Length);
        Assert.True(File.Exists(Path.Combine(Directory, Neutral)));
    }

    /// <summary>The decisive one: every language says everything the English one says.</summary>
    [Fact]
    public void EveryLanguageCarriesEveryText()
    {
        var reference = Read(Neutral);
        var gaps = new List<string>();

        foreach (var file in Translations())
        {
            var texts = Read(file);

            foreach (var key in reference.Keys.Where(k => !texts.ContainsKey(k)).Order(StringComparer.Ordinal))
            {
                gaps.Add($"{file}: {key} is missing");
            }
        }

        Assert.Empty(gaps);
    }

    /// <summary>
    /// And nothing beyond it. A key only one language has is either a leftover or a text the
    /// interface never asks for — both are dead weight that reads as a translation.
    /// </summary>
    [Fact]
    public void NoLanguageCarriesATextTheEnglishOneDoesNot()
    {
        var reference = Read(Neutral);
        var extra = new List<string>();

        foreach (var file in Translations())
        {
            foreach (var key in Read(file).Keys.Where(k => !reference.ContainsKey(k)).Order(StringComparer.Ordinal))
            {
                extra.Add($"{file}: {key} is not in {Neutral}");
            }
        }

        Assert.Empty(extra);
    }

    /// <summary>
    /// An empty value is worse than a missing one: a missing text falls back to English, an empty
    /// one paints a blank button.
    /// </summary>
    [Fact]
    public void NoTextIsEmpty()
    {
        var blanks = new List<string>();

        foreach (var file in Translations().Prepend(Neutral))
        {
            foreach (var (key, value) in Read(file).Where(e => string.IsNullOrWhiteSpace(e.Value)))
            {
                blanks.Add($"{file}: {key} is blank");
            }
        }

        Assert.Empty(blanks);
    }

    /// <summary>
    /// A placeholder that differs between languages throws at run time rather than showing wrong
    /// text — <c>string.Format</c> with a <c>{1}</c> nobody passed is an exception, in whichever
    /// language happens to be selected.
    /// </summary>
    [Fact]
    public void PlaceholdersMatchTheEnglishText()
    {
        var reference = Read(Neutral);
        var mismatches = new List<string>();

        foreach (var file in Translations())
        {
            foreach (var (key, value) in Read(file))
            {
                if (!reference.TryGetValue(key, out var english))
                {
                    continue;
                }

                if (Placeholders(english) != Placeholders(value))
                {
                    mismatches.Add($"{file}: {key} uses {Placeholders(value)} where English uses {Placeholders(english)}");
                }
            }
        }

        Assert.Empty(mismatches);
    }

    /// <summary>How many numbered placeholders a text expects, as the highest index plus one.</summary>
    private static int Placeholders(string text)
    {
        var highest = -1;

        foreach (var match in System.Text.RegularExpressions.Regex.Matches(text, @"\{(\d+)"))
        {
            highest = Math.Max(highest, int.Parse(
                ((System.Text.RegularExpressions.Match)match).Groups[1].Value));
        }

        return highest + 1;
    }
}
