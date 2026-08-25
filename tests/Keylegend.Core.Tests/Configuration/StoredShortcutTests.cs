using Keylegend.Core.Configuration;
using Keylegend.Core.Input;
using Keylegend.Core.Lighting;
using Keylegend.Core.Profiles;
using Keylegend.Core.Shortcuts;

namespace Keylegend.Core.Tests.Configuration;

/// <summary>
/// Guards how saved shortcuts meet the shipped ones. The rule these enforce is the one that was
/// broken: what somebody edited stays edited, and everything else keeps improving with the
/// program. Getting this wrong is invisible — the settings file looks reasonable, the shortcuts
/// simply stop changing — so it is worth pinning down.
/// </summary>
public class StoredShortcutTests
{
    private static ShortcutSet Set(params (string Character, string Label)[] entries)
        => new(
            entries.ToDictionary(e => e.Character, e => new Shortcut(FunctionGroup.Tools, e.Label),
                StringComparer.OrdinalIgnoreCase),
            new Dictionary<string, Shortcut>(StringComparer.Ordinal));

    [Fact]
    public void WithoutSavedShortcutsTheShippedOnesAreUsed()
    {
        var restored = new StoredSettings().ToShortcutCatalogue();

        Assert.Equal(DefaultShortcuts.Create().Sets.Count, restored.Sets.Count);
    }

    /// <summary>
    /// The failure that prompted these tests: a file saved before a layer existed must not hide
    /// that layer forever.
    /// </summary>
    [Fact]
    public void ALayerAddedLaterStillReachesSomebodyWhoSavedEarlier()
    {
        // A file from a day when only Win+Shift was known about.
        var old = new StoredSettings
        {
            Shortcuts =
            [
                StoredShortcutSet.From(ModifierKeys.LeftWin | ModifierKeys.LeftShift,
                    Set(("s", "Snip part of the screen")))
            ]
        };

        var restored = old.ToShortcutCatalogue();

        foreach (var (modifiers, _) in DefaultShortcuts.Create().Sets)
        {
            Assert.True(restored.Sets.ContainsKey(modifiers),
                $"{ModifierCombination.Format(modifiers)} disappeared for anyone with saved settings.");
        }
    }

    [Fact]
    public void AnEditedLayerKeepsWhatWasEdited()
    {
        var mine = Set(("s", "Meine eigene Belegung"));

        var restored = new StoredSettings
        {
            Shortcuts = [StoredShortcutSet.From(ModifierKeys.LeftWin | ModifierKeys.LeftShift, mine)]
        }.ToShortcutCatalogue();

        var layer = restored.Sets[ModifierKeys.LeftWin | ModifierKeys.LeftShift];

        Assert.Single(layer.Characters);
        Assert.Equal("Meine eigene Belegung", layer.Characters["s"].Label);
    }

    /// <summary>
    /// Nothing that matches the shipped set is written down. Saving it would be indistinguishable
    /// from somebody meaning it that way, and would freeze the layer.
    /// </summary>
    [Fact]
    public void UnchangedLayersAreNotSaved()
    {
        var settings = StoredSettings.From(
            TimeSpan.FromSeconds(60), ColourScheme.Default, new ProfileLibrary([]),
            startWithWindows: false, useApplicationProfiles: true,
            shortcuts: DefaultShortcuts.Create());

        Assert.Empty(settings.Shortcuts);
    }

    [Fact]
    public void OnlyTheChangedLayerIsSaved()
    {
        var changed = DefaultShortcuts.Create()
            .WithOverride(ModifierKeys.LeftWin | ModifierKeys.LeftShift, Set(("s", "Anders")));

        var settings = StoredSettings.From(
            TimeSpan.FromSeconds(60), ColourScheme.Default, new ProfileLibrary([]),
            startWithWindows: false, useApplicationProfiles: true, shortcuts: changed);

        var saved = Assert.Single(settings.Shortcuts);
        Assert.Equal("Win+Shift", saved.Modifiers);
    }

    /// <summary>A round trip must not lose an edit.</summary>
    [Fact]
    public void AnEditSurvivesSavingAndLoading()
    {
        var changed = DefaultShortcuts.Create()
            .WithOverride(ModifierKeys.LeftAlt, Set(("q", "Etwas Eigenes")));

        var restored = StoredSettings.From(
            TimeSpan.FromSeconds(60), ColourScheme.Default, new ProfileLibrary([]),
            startWithWindows: false, useApplicationProfiles: true, shortcuts: changed)
            .ToShortcutCatalogue();

        Assert.Equal("Etwas Eigenes", restored.Sets[ModifierKeys.LeftAlt].Characters["q"].Label);
    }
}
