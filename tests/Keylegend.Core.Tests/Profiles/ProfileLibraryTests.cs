using Keylegend.Core.Configuration;
using Keylegend.Core.Input;
using Keylegend.Core.Lighting;
using Keylegend.Core.Profiles;
using Keylegend.Core.Shortcuts;

namespace Keylegend.Core.Tests.Profiles;

/// <summary>
/// The rules for shipped profiles, user changes and resetting.
/// </summary>
public class ProfileLibraryTests
{
    // ---------------------------------------------------------------- untouched

    [Fact]
    public void AShippedProfileIsUnchangedAndHasNothingToReset()
    {
        var library = new ProfileLibrary([Shipped()]);

        var entry = library.Find("example");

        Assert.NotNull(entry);
        Assert.Equal(ProfileSource.Shipped, entry.Source);
        Assert.Empty(entry.Overridden);
        Assert.False(entry.CanReset);
        Assert.Equal(new RgbColor(255, 0, 0), entry.Profile.Highlights["Keyboard_W"].Colour);
    }

    [Fact]
    public void AnUntouchedLibrarySavesNothing()
    {
        var library = new ProfileLibrary([Shipped()]);

        Assert.Empty(library.Overrides);
        Assert.Empty(library.UserProfiles);
        Assert.Empty(library.Hidden);
    }

    // ---------------------------------------------------------------- editing

    [Fact]
    public void EditingOneSectionFreezesOnlyThatSection()
    {
        var library = new ProfileLibrary([Shipped()]);
        var profile = library.Find("example")!.Profile;

        library.Edit(
            "example",
            profile with { Highlights = Highlights(("Keyboard_W", new RgbColor(0, 255, 0))) },
            ProfileSection.Highlights);

        var entry = library.Find("example")!;

        Assert.True(entry.IsOverridden(ProfileSection.Highlights));
        Assert.False(entry.IsOverridden(ProfileSection.Match));
        Assert.False(entry.IsOverridden(ProfileSection.Shortcuts));
        Assert.Equal(new RgbColor(0, 255, 0), entry.Profile.Highlights["Keyboard_W"].Colour);
    }

    /// <summary>
    /// The reason overriding is per section rather than per profile. A user who recoloured the
    /// highlights should still get a corrected shortcut in the next release.
    /// </summary>
    [Fact]
    public void AnUpdatedShippedProfileStillImprovesTheSectionsTheUserDidNotTouch()
    {
        var library = new ProfileLibrary([Shipped()]);

        library.Edit(
            "example",
            library.Find("example")!.Profile with
            {
                Highlights = Highlights(("Keyboard_W", new RgbColor(0, 255, 0)))
            },
            ProfileSection.Highlights);

        // The next release corrects the shortcut and adds a process. The user's saved file is
        // unchanged; only the shipped profile is newer.
        var updated = Shipped() with
        {
            Match = new ProfileMatch(["example", "example-beta"]),
            Shortcuts = Shortcuts(("z", FunctionGroup.Edit, "Undo"))
        };

        var reopened = new ProfileLibrary([updated], library.UserProfiles, library.Overrides, library.Hidden);
        var entry = reopened.Find("example")!;

        Assert.Equal(new RgbColor(0, 255, 0), entry.Profile.Highlights["Keyboard_W"].Colour);
        Assert.Equal(["example", "example-beta"], entry.Profile.Match.Processes);
        Assert.Equal(
            "Undo",
            entry.Profile.Shortcuts[ModifierKeys.LeftCtrl].Characters["z"].Label);
    }

    [Fact]
    public void RenamingIsTrackedApartFromTheSections()
    {
        var library = new ProfileLibrary([Shipped()]);

        library.Rename("example", "My name for it");

        var entry = library.Find("example")!;

        Assert.Equal("My name for it", entry.Profile.Name);
        Assert.True(entry.Renamed);
        Assert.Empty(entry.Overridden);
        Assert.True(entry.CanReset);
    }

    /// <summary>
    /// The failure this guards against was real and nearly shipped: the editor called
    /// <see cref="ProfileLibrary.Edit"/> whenever a field lost focus, so merely clicking through
    /// the list marked every profile "edited by you" and froze it. Nothing had been changed, and
    /// none of those profiles would ever have received an improvement again.
    /// </summary>
    [Fact]
    public void EditingWithNoActualChangeFreezesNothing()
    {
        var library = new ProfileLibrary([Shipped()]);
        var unchanged = library.Find("example")!.Profile;

        library.Edit("example", unchanged, ProfileSection.Match);
        library.Edit("example", unchanged, ProfileSection.Highlights);
        library.Edit("example", unchanged, ProfileSection.Shortcuts);

        var entry = library.Find("example")!;

        Assert.Empty(entry.Overridden);
        Assert.False(entry.CanReset);
        Assert.Empty(library.Overrides);
    }

    [Fact]
    public void EditingAValueBackToTheShippedOneUndoesTheOverride()
    {
        var library = new ProfileLibrary([Shipped()]);
        var original = library.Find("example")!.Profile;

        library.Edit(
            "example",
            original with { Highlights = Highlights(("Keyboard_W", new RgbColor(0, 255, 0))) },
            ProfileSection.Highlights);

        Assert.True(library.Find("example")!.IsOverridden(ProfileSection.Highlights));

        library.Edit("example", original, ProfileSection.Highlights);

        Assert.Empty(library.Find("example")!.Overridden);
        Assert.Empty(library.Overrides);
    }

    [Fact]
    public void ReorderingHighlightsIsNotAChange()
    {
        // Rebuilding a dictionary can change its order. If that counted, the editor would
        // freeze a section for doing nothing.
        var library = new ProfileLibrary([Shipped() with
        {
            Highlights = Highlights(
                ("Keyboard_W", new RgbColor(255, 0, 0)),
                ("Keyboard_A", new RgbColor(255, 0, 0)))
        }]);

        var reordered = library.Find("example")!.Profile with
        {
            Highlights = Highlights(
                ("Keyboard_A", new RgbColor(255, 0, 0)),
                ("Keyboard_W", new RgbColor(255, 0, 0)))
        };

        library.Edit("example", reordered, ProfileSection.Highlights);

        Assert.Empty(library.Overrides);
    }

    // ---------------------------------------------------------------- resetting

    [Fact]
    public void ResettingASectionGivesItBackToTheShippedVersion()
    {
        var library = new ProfileLibrary([Shipped()]);

        library.Edit(
            "example",
            library.Find("example")!.Profile with
            {
                Highlights = Highlights(("Keyboard_W", new RgbColor(0, 255, 0)))
            },
            ProfileSection.Highlights);

        library.Reset("example", ProfileSection.Highlights);

        var entry = library.Find("example")!;

        Assert.Empty(entry.Overridden);
        Assert.Equal(new RgbColor(255, 0, 0), entry.Profile.Highlights["Keyboard_W"].Colour);
    }

    [Fact]
    public void ResettingTheLastSectionLeavesNoResidueInTheSavedFile()
    {
        var library = new ProfileLibrary([Shipped()]);

        library.Edit(
            "example",
            library.Find("example")!.Profile with { Highlights = ApplicationProfile.NoHighlights },
            ProfileSection.Highlights);

        library.Reset("example", ProfileSection.Highlights);

        Assert.Empty(library.Overrides);
    }

    [Fact]
    public void ResettingEverythingAlsoUndoesRenamingAndHiding()
    {
        var library = new ProfileLibrary([Shipped()]);

        library.Rename("example", "Mine");
        library.Hide("example");
        library.Edit(
            "example",
            library.Find("example")!.Profile with { Match = new ProfileMatch(["other"]) },
            ProfileSection.Match);

        library.ResetAll("example");

        var entry = library.Find("example")!;

        Assert.Equal("Example", entry.Profile.Name);
        Assert.False(entry.Hidden);
        Assert.Empty(entry.Overridden);
        Assert.Empty(library.Overrides);
    }

    // ---------------------------------------------------------------- hiding

    [Fact]
    public void AHiddenProfileStaysInTheListButIsNotSelected()
    {
        var library = new ProfileLibrary([Shipped()]);

        library.Hide("example");

        Assert.True(library.Find("example")!.Hidden);
        Assert.Empty(library.Catalogue().Profiles);
        Assert.Single(library.Entries);
    }

    [Fact]
    public void RemovingAShippedProfileDoesNothingBecauseTheFileWouldComeBack()
    {
        var library = new ProfileLibrary([Shipped()]);

        library.Remove("example");

        Assert.NotNull(library.Find("example"));
    }

    // ---------------------------------------------------------------- user profiles

    [Fact]
    public void AUserProfileIsStoredWholeAndCanBeRemoved()
    {
        var library = new ProfileLibrary([]);

        library.Add(Shipped() with { Id = "mine", Name = "Mine", Source = ProfileSource.User });

        var entry = library.Find("mine")!;

        Assert.Equal(ProfileSource.User, entry.Source);
        Assert.False(entry.CanReset);
        Assert.Single(library.UserProfiles);

        library.Remove("mine");

        Assert.Null(library.Find("mine"));
    }

    [Fact]
    public void NewIdDoesNotCollide()
    {
        var library = new ProfileLibrary([Shipped()]);

        Assert.Equal("example-2", library.NewId("Example"));
        Assert.Equal("my-profile", library.NewId("My Profile!"));
        Assert.Equal("profile", library.NewId("   "));
    }

    // ---------------------------------------------------------------- selection

    [Fact]
    public void AProfileNamingTheProcessOutranksTheGenericGameProfile()
    {
        var generic = Shipped() with
        {
            Id = "_generic",
            Name = "Games",
            Match = new ProfileMatch([], AppliesToGames: true)
        };

        var specific = Shipped() with { Id = "some-game", Match = new ProfileMatch(["somegame"]) };

        var catalogue = new ProfileLibrary([generic, specific]).Catalogue();

        var selected = catalogue.Select(new ForegroundContext("somegame", string.Empty, LooksLikeGame: true));

        Assert.Equal("some-game", selected?.Id);
    }

    [Fact]
    public void ATitleConditionSeparatesProgramsSharingAnExecutable()
    {
        // LibreOffice runs both modules as soffice. Without the title there is no way to tell
        // a spreadsheet from a letter, and one profile would win arbitrarily.
        var writer = Shipped() with
        {
            Id = "writer",
            Match = new ProfileMatch(["soffice"], TitleContains: ["LibreOffice Writer"])
        };

        var calc = Shipped() with
        {
            Id = "calc",
            Match = new ProfileMatch(["soffice"], TitleContains: ["LibreOffice Calc"])
        };

        var catalogue = new ProfileLibrary([writer, calc]).Catalogue();

        Assert.Equal("calc", catalogue.Select(new ForegroundContext("soffice", "Budget.ods — LibreOffice Calc", false))?.Id);
        Assert.Equal("writer", catalogue.Select(new ForegroundContext("soffice", "Letter.odt — LibreOffice Writer", false))?.Id);

        // A title matching neither means no profile at all, which is better than guessing.
        Assert.Null(catalogue.Select(new ForegroundContext("soffice", "Start Centre", false)));
    }

    [Fact]
    public void NoTitleConditionMeansTheProcessNameAloneDecides()
    {
        var catalogue = new ProfileLibrary([Shipped()]).Catalogue();

        Assert.NotNull(catalogue.Select(new ForegroundContext("example", "anything at all", false)));
    }

    // ---------------------------------------------------------------- migration

    /// <summary>
    /// Format 1 stored profiles whole, with no id and no record of where they came from — so
    /// there is no way to tell which entries were once shipped. Treating them all as the user's
    /// is the only reading that never throws away work.
    /// </summary>
    [Fact]
    public void AFormatOneFileBecomesUserProfilesAndKeepsItsHighlights()
    {
        var settings = new StoredSettings
        {
            FormatVersion = 1,
            Profiles =
            [
                new StoredProfile
                {
                    Name = "My Game Setup",
                    Processes = ["somegame"],
                    AppliesToGames = true,
                    Priority = 3,
                    KeyHighlights = new Dictionary<string, string> { ["Keyboard_W"] = "#00FF00" }
                }
            ]
        };

        var library = settings.ToProfileLibrary([Shipped()]);
        var migrated = library.Find("my-game-setup");

        Assert.NotNull(migrated);
        Assert.Equal(ProfileSource.User, migrated.Source);
        Assert.Equal("My Game Setup", migrated.Profile.Name);
        Assert.Equal(["somegame"], migrated.Profile.Match.Processes);
        Assert.Equal(3, migrated.Profile.Match.Priority);
        Assert.Equal(new RgbColor(0, 255, 0), migrated.Profile.Highlights["Keyboard_W"].Colour);

        // The shipped profiles appear alongside it rather than being replaced.
        Assert.NotNull(library.Find("example"));
    }

    [Fact]
    public void AFormatTwoFileDoesNotMigrateItsLegacyList()
    {
        var settings = new StoredSettings
        {
            FormatVersion = 2,
            Profiles = [new StoredProfile { Name = "Left over" }]
        };

        Assert.Null(settings.ToProfileLibrary([Shipped()]).Find("left-over"));
    }

    [Fact]
    public void OverridesSurviveASaveAndLoadRound()
    {
        var library = new ProfileLibrary([Shipped()]);

        library.Edit(
            "example",
            library.Find("example")!.Profile with
            {
                Highlights = Highlights(("Keyboard_A", new RgbColor(1, 2, 3)))
            },
            ProfileSection.Highlights);
        library.Hide("example");

        var saved = StoredSettings.From(
            TimeSpan.FromSeconds(60),
            ColourScheme.Default,
            library,
            startWithWindows: false,
            useApplicationProfiles: true);

        var reloaded = saved.ToProfileLibrary([Shipped()]).Find("example")!;

        Assert.True(reloaded.Hidden);
        Assert.True(reloaded.IsOverridden(ProfileSection.Highlights));
        Assert.Equal(new RgbColor(1, 2, 3), reloaded.Profile.Highlights["Keyboard_A"].Colour);
    }

    // ---------------------------------------------------------------- shortcut layering

    /// <summary>
    /// A profile says what Ctrl also means inside that program. It has nothing to say about
    /// Win+E, which Windows assigns and which stays true whatever is in front — so layers the
    /// profile does not mention must survive whole.
    /// </summary>
    [Fact]
    public void LayersAProfileDoesNotMentionSurviveUntouched()
    {
        var general = new ShortcutCatalogue(new Dictionary<ModifierKeys, ShortcutSet>
        {
            [ModifierKeys.LeftWin] = Set(("e", FunctionGroup.File, "Open Explorer")),
            [ModifierKeys.LeftCtrl] = Set(("s", FunctionGroup.File, "Save"))
        });

        var profile = Shipped() with { Shortcuts = Shortcuts(("s", FunctionGroup.File, "Save document")) };

        var effective = profile.ApplyShortcuts(general);

        Assert.Equal("Save document", effective.Sets[ModifierKeys.LeftCtrl].Characters["s"].Label);
        Assert.Equal("Open Explorer", effective.Sets[ModifierKeys.LeftWin].Characters["e"].Label);
    }

    /// <summary>
    /// A profile naming Ctrl does not mean "Ctrl means nothing else here". Everything it does not
    /// name has to show through.
    /// </summary>
    /// <remarks>
    /// This is the one that was wrong, and the clipboard is how it showed: a browser profile names
    /// Ctrl for its tabs and its address bar and says nothing about Ctrl+C, so copy, paste, cut,
    /// undo, redo and select-all went dark in a program one does little but type and paste in.
    /// </remarks>
    [Fact]
    public void AProfileDoesNotBlankTheEntriesItLeavesUnmentioned()
    {
        var general = new ShortcutCatalogue(new Dictionary<ModifierKeys, ShortcutSet>
        {
            [ModifierKeys.LeftCtrl] = Set(
                ("c", FunctionGroup.Edit, "Copy"),
                ("v", FunctionGroup.Edit, "Paste"),
                ("s", FunctionGroup.File, "Save"))
        });

        var profile = Shipped() with
        {
            Shortcuts = Shortcuts(("t", FunctionGroup.Window, "New tab"))
        };

        var effective = profile.ApplyShortcuts(general).Sets[ModifierKeys.LeftCtrl];

        Assert.Equal("New tab", effective.Characters["t"].Label);
        Assert.Equal("Copy", effective.Characters["c"].Label);
        Assert.Equal("Paste", effective.Characters["v"].Label);
        Assert.Equal("Save", effective.Characters["s"].Label);
    }

    /// <summary>
    /// And it keeps what a profile is for: naming a key changes what it means in that program.
    /// </summary>
    [Fact]
    public void AProfileStillChangesTheEntriesItDoesName()
    {
        var general = new ShortcutCatalogue(new Dictionary<ModifierKeys, ShortcutSet>
        {
            [ModifierKeys.LeftCtrl] = Set(("d", FunctionGroup.View, "Bookmark or duplicate"))
        });

        var profile = Shipped() with
        {
            Shortcuts = Shortcuts(("d", FunctionGroup.File, "Bookmark this page"))
        };

        var effective = profile.ApplyShortcuts(general).Sets[ModifierKeys.LeftCtrl];

        Assert.Equal("Bookmark this page", effective.Characters["d"].Label);
        Assert.Equal(FunctionGroup.File, effective.Characters["d"].Group);
    }

    /// <summary>Keys addressed by position layer the same way as characters.</summary>
    [Fact]
    public void KeysByPositionLayerToo()
    {
        var general = new ShortcutCatalogue(new Dictionary<ModifierKeys, ShortcutSet>
        {
            [ModifierKeys.LeftCtrl] = new(
                new Dictionary<string, Shortcut>(StringComparer.OrdinalIgnoreCase),
                new Dictionary<string, Shortcut>(StringComparer.Ordinal)
                {
                    ["Keyboard_Home"] = new(FunctionGroup.Navigation, "Start of document"),
                    ["Keyboard_End"] = new(FunctionGroup.Navigation, "End of document")
                })
        });

        var profile = Shipped() with
        {
            Shortcuts = new Dictionary<ModifierKeys, ShortcutSet>
            {
                [ModifierKeys.LeftCtrl] = new(
                    new Dictionary<string, Shortcut>(StringComparer.OrdinalIgnoreCase),
                    new Dictionary<string, Shortcut>(StringComparer.Ordinal)
                    {
                        ["Keyboard_Tab"] = new(FunctionGroup.Window, "Next tab")
                    })
            }
        };

        var effective = profile.ApplyShortcuts(general).Sets[ModifierKeys.LeftCtrl];

        Assert.Equal("Next tab", effective.Keys["Keyboard_Tab"].Label);
        Assert.Equal("Start of document", effective.Keys["Keyboard_Home"].Label);
        Assert.Equal("End of document", effective.Keys["Keyboard_End"].Label);
    }

    [Fact]
    public void AProfileWithNoShortcutsLeavesTheGeneralCatalogueAlone()
    {
        var general = new ShortcutCatalogue(new Dictionary<ModifierKeys, ShortcutSet>
        {
            [ModifierKeys.LeftWin] = Set(("e", FunctionGroup.File, "Open Explorer"))
        });

        Assert.Same(general, Shipped().ApplyShortcuts(general));
    }

    // ---------------------------------------------------------------- helpers

    private static ApplicationProfile Shipped() => new(
        Id: "example",
        Name: "Example",
        Kind: ProfileKind.App,
        Source: ProfileSource.Shipped,
        Match: new ProfileMatch(["example"]),
        Highlights: Highlights(("Keyboard_W", new RgbColor(255, 0, 0))),
        Shortcuts: ApplicationProfile.NoShortcuts);

    private static IReadOnlyDictionary<string, KeyHighlight> Highlights(
        params (string KeyId, RgbColor Colour)[] entries)
        => entries.ToDictionary(e => e.KeyId, e => new KeyHighlight(e.Colour), StringComparer.Ordinal);

    private static IReadOnlyDictionary<ModifierKeys, ShortcutSet> Shortcuts(
        params (string Character, FunctionGroup Group, string Label)[] entries)
        => new Dictionary<ModifierKeys, ShortcutSet> { [ModifierKeys.LeftCtrl] = Set(entries) };

    private static ShortcutSet Set(params (string Character, FunctionGroup Group, string Label)[] entries)
        => new(
            entries.ToDictionary(
                e => e.Character,
                e => new Shortcut(e.Group, e.Label),
                StringComparer.OrdinalIgnoreCase),
            new Dictionary<string, Shortcut>(StringComparer.Ordinal));
}
