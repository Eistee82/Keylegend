using Keylegend.Core.Profiles;

namespace Keylegend.Core.Tests.Profiles;

public class ApplicationProfileTests
{
    private static ApplicationProfile Named(string name, params string[] processes)
        => Blank(name) with { Match = new ProfileMatch(processes) };

    private static ApplicationProfile Games(int priority = 0)
        => Blank("Games") with
        {
            Kind = ProfileKind.Game,
            Match = new ProfileMatch([], AppliesToGames: true, Priority: priority)
        };

    private static ApplicationProfile Blank(string name) => new(
        Id: name.ToLowerInvariant(),
        Name: name,
        Kind: ProfileKind.App,
        Source: ProfileSource.Shipped,
        Match: ProfileMatch.None,
        Highlights: ApplicationProfile.NoHighlights,
        Shortcuts: ApplicationProfile.NoShortcuts);

    [Fact]
    public void MatchesByProcessName()
    {
        var profile = Named("Photoshop", "photoshop");

        Assert.True(profile.Matches(new ForegroundContext("photoshop", "", false)));
        Assert.False(profile.Matches(new ForegroundContext("notepad", "", false)));
    }

    [Fact]
    public void ProcessMatchingIgnoresCase()
    {
        var profile = Named("Photoshop", "Photoshop");

        Assert.True(profile.Matches(new ForegroundContext("photoshop", "", false)));
    }

    [Fact]
    public void TheGameProfileMatchesAnythingDetectedAsAGame()
    {
        // Naming games individually is hopeless - there are too many and new ones appear
        // constantly - so one profile covers them all by detection instead.
        var profile = Games();

        Assert.True(profile.Matches(new ForegroundContext("some-unknown-game", "", LooksLikeGame: true)));
        Assert.False(profile.Matches(new ForegroundContext("notepad", "", LooksLikeGame: false)));
    }

    [Fact]
    public void AProfileNamingTheProcessBeatsTheGeneralGameProfile()
    {
        // A game with its own settings must keep them rather than falling back to the generic
        // one, even though both match.
        var catalogue = new ProfileCatalogue([Games(priority: 100), Named("Specific", "thatgame")]);

        var selected = catalogue.Select(new ForegroundContext("thatgame", "", LooksLikeGame: true));

        Assert.Equal("Specific", selected?.Name);
    }

    [Fact]
    public void PriorityDecidesBetweenProfilesOfEqualSpecificity()
    {
        var catalogue = new ProfileCatalogue(
        [
            Named("Low", "app") with { Match = new ProfileMatch(["app"], Priority: 1) },
            Named("High", "app") with { Match = new ProfileMatch(["app"], Priority: 2) }
        ]);

        Assert.Equal("High", catalogue.Select(new ForegroundContext("app", "", false))?.Name);
    }

    [Fact]
    public void NothingMatchingMeansDefaultBehaviour()
    {
        var catalogue = new ProfileCatalogue([Games(), Named("Photoshop", "photoshop")]);

        Assert.Null(catalogue.Select(new ForegroundContext("notepad", "", false)));
    }

    [Fact]
    public void TheShippedGameProfileHighlightsTheMovementKeys()
    {
        var game = ShippedProfiles.ById("_generic");

        Assert.NotNull(game);

        foreach (var key in new[] { "Keyboard_W", "Keyboard_A", "Keyboard_S", "Keyboard_D" })
        {
            Assert.True(game.Highlights.ContainsKey(key), $"{key} should be highlighted.");
        }

        // All four in the same colour: they are one cluster, and splitting them would suggest
        // a distinction that does not exist.
        var colours = new[] { "Keyboard_W", "Keyboard_A", "Keyboard_S", "Keyboard_D" }
            .Select(k => game.Highlights[k].Colour)
            .Distinct()
            .ToArray();

        Assert.Single(colours);
    }

    [Fact]
    public void TheShippedGameProfileAppliesToGamesWithoutNamingAny()
    {
        var game = ShippedProfiles.ById("_generic");

        Assert.NotNull(game);
        Assert.True(game.Match.AppliesToGames);
        Assert.Empty(game.Match.Processes);
    }

    [Fact]
    public void ExactlyOneShippedProfileClaimsEveryGame()
    {
        // Two would make the choice between them arbitrary, and a named title would still have
        // to outrank both.
        var generic = ShippedProfiles.All.Where(p => p.Match.AppliesToGames).ToArray();

        Assert.Single(generic);
    }
}
