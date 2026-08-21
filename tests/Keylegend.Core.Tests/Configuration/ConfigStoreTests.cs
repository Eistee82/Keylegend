using Keylegend.Core.Configuration;
using Keylegend.Core.Input;
using Keylegend.Core.Lighting;
using Keylegend.Core.Profiles;

namespace Keylegend.Core.Tests.Configuration;

public class ConfigStoreTests : IDisposable
{
    private readonly string _directory =
        Path.Combine(Path.GetTempPath(), "keylegend-tests", Guid.NewGuid().ToString("N"));

    private string SettingsPath => Path.Combine(_directory, "settings.json");

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    [Fact]
    public void MissingFileYieldsDefaultsWithoutComplaint()
    {
        var (settings, problem) = new ConfigStore(SettingsPath).Load();

        Assert.Null(problem);
        Assert.Equal(60, settings.IdleTimeoutSeconds);
        Assert.Equal(1.0, settings.Brightness);
    }

    [Fact]
    public void SavesAndReloadsEverything()
    {
        var store = new ConfigStore(SettingsPath);
        var scheme = ColourScheme.Default with { Brightness = 0.5 };

        // No shipped profiles: this test is about what a settings file carries, and the
        // embedded set would only add noise it does not store anyway.
        var profiles = new ProfileLibrary([]);

        profiles.Add(new ApplicationProfile(
            Id: "my-games",
            Name: "Games",
            Kind: ProfileKind.Game,
            Source: ProfileSource.User,
            Match: new ProfileMatch([], AppliesToGames: true),
            Highlights: new Dictionary<string, KeyHighlight>
            {
                ["Keyboard_W"] = new(new RgbColor(255, 0, 0), "Forward")
            },
            Shortcuts: ApplicationProfile.NoShortcuts));

        profiles.Add(new ApplicationProfile(
            Id: "editor",
            Name: "Editor",
            Kind: ProfileKind.App,
            Source: ProfileSource.User,
            Match: new ProfileMatch(["devenv", "code"], Priority: 5),
            Highlights: ApplicationProfile.NoHighlights,
            Shortcuts: ApplicationProfile.NoShortcuts));

        Assert.Null(store.Save(StoredSettings.From(
            TimeSpan.FromSeconds(45), scheme, profiles,
            startWithWindows: true, useApplicationProfiles: false)));

        var (loaded, problem) = store.Load();

        Assert.Null(problem);
        Assert.Equal(45, loaded.IdleTimeoutSeconds);
        Assert.True(loaded.StartWithWindows);
        Assert.False(loaded.UseApplicationProfiles);
        Assert.Equal(0.5, loaded.ToColourScheme().Brightness);

        var reloaded = loaded.ToProfileLibrary([]);
        Assert.Equal(2, reloaded.Entries.Count);

        var games = reloaded.Find("my-games")!.Profile;
        Assert.True(games.Match.AppliesToGames);
        Assert.Equal(new RgbColor(255, 0, 0), games.Highlights["Keyboard_W"].Colour);
        Assert.Equal("Forward", games.Highlights["Keyboard_W"].Label);

        var editor = reloaded.Find("editor")!.Profile;
        Assert.Equal(["devenv", "code"], editor.Match.Processes);
        Assert.Equal(5, editor.Match.Priority);
    }

    [Fact]
    public void ColoursSurviveTheRoundTripExactly()
    {
        var store = new ConfigStore(SettingsPath);
        var scheme = ColourScheme.Default with
        {
            Categories = new Dictionary<KeyCategory, RgbColor>(ColourScheme.Default.Categories)
            {
                [KeyCategory.Digit] = new(17, 34, 51)
            }
        };

        store.Save(StoredSettings.From(TimeSpan.FromSeconds(60), scheme, new ProfileLibrary([]), false, true));

        var restored = store.Load().Settings.ToColourScheme();

        Assert.Equal(new RgbColor(17, 34, 51), restored.For(KeyCategory.Digit));
    }

    [Fact]
    public void ADamagedFileYieldsDefaultsAndAnExplanation()
    {
        // Refusing to start because of a broken settings file would be a poor trade.
        Directory.CreateDirectory(_directory);
        File.WriteAllText(SettingsPath, "{ this is not json");

        var (settings, problem) = new ConfigStore(SettingsPath).Load();

        Assert.NotNull(problem);
        Assert.Equal(60, settings.IdleTimeoutSeconds);
    }

    [Fact]
    public void ASingleBadColourCostsThatColourAndNothingElse()
    {
        Directory.CreateDirectory(SettingsPath[..SettingsPath.LastIndexOf(Path.DirectorySeparatorChar)]);
        File.WriteAllText(SettingsPath, """
            {
              "formatVersion": 1,
              "idleTimeoutSeconds": 30,
              "categoryColours": { "Digit": "not-a-colour", "Symbol": "#00FF00" }
            }
            """);

        var (settings, problem) = new ConfigStore(SettingsPath).Load();
        var scheme = settings.ToColourScheme();

        Assert.Null(problem);
        Assert.Equal(30, settings.IdleTimeoutSeconds);
        Assert.Equal(ColourScheme.Default.For(KeyCategory.Digit), scheme.For(KeyCategory.Digit));
        Assert.Equal(new RgbColor(0, 255, 0), scheme.For(KeyCategory.Symbol));
    }

    [Fact]
    public void AFileFromANewerVersionIsLeftAlone()
    {
        // Overwriting it with what this build understands would quietly discard the user's
        // settings from a later version.
        Directory.CreateDirectory(_directory);
        File.WriteAllText(SettingsPath, """{ "formatVersion": 99, "idleTimeoutSeconds": 5 }""");

        var (settings, problem) = new ConfigStore(SettingsPath).Load();

        Assert.NotNull(problem);
        Assert.Contains("newer", problem);
        Assert.Equal(60, settings.IdleTimeoutSeconds);
    }

    [Fact]
    public void SavingLeavesNoTemporaryFileBehind()
    {
        var store = new ConfigStore(SettingsPath);

        store.Save(new StoredSettings());

        Assert.True(File.Exists(SettingsPath));
        Assert.False(File.Exists(SettingsPath + ".tmp"));
    }

    [Fact]
    public void AnInvalidIdleTimeoutFallsBackRatherThanDisablingTheHandBack()
    {
        var settings = new StoredSettings { IdleTimeoutSeconds = 0 };

        Assert.Equal(TimeSpan.FromSeconds(60), settings.ToIdleTimeout());
    }
}
