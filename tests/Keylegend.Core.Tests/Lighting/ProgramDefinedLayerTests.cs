using Keylegend.Core.Devices;
using Keylegend.Core.Input;
using Keylegend.Core.Lighting;
using Keylegend.Core.Profiles;
using Keylegend.Core.Shortcuts;

namespace Keylegend.Core.Tests.Lighting;

/// <summary>
/// Layers a program defines for itself: Shift, and no modifier at all.
/// </summary>
/// <remarks>
/// <para>
/// Shift is not a modifier for <em>text</em> — it changes which character a key types, and that is
/// why holding it does not normally dim the keyboard. For <em>functions</em> it plainly is one, and
/// games use it that way: sprint, walk, secondary fire. The same goes for the bare keys, where WASD
/// means direction rather than letters.
/// </para>
/// <para>
/// So the rule is not "Shift filters" but "a layer somebody defined filters". Nothing shipped is
/// keyed on Shift or on no modifier, so nothing changes by default; an application profile that
/// defines one turns it into a function layer while that program is in front.
/// </para>
/// </remarks>
public class ProgramDefinedLayerTests
{
    private sealed class Letters : IKeyResolver
    {
        public KeyMeaning Resolve(string keyId, int? scanCode, KeyboardState state)
            => keyId switch
            {
                "Keyboard_W" => new KeyMeaning(state.Shift ? "W" : "w",
                    state.Shift ? KeyCategory.Uppercase : KeyCategory.Lowercase),
                "Keyboard_A" => new KeyMeaning(state.Shift ? "A" : "a",
                    state.Shift ? KeyCategory.Uppercase : KeyCategory.Lowercase),
                "Keyboard_B" => new KeyMeaning(state.Shift ? "B" : "b",
                    state.Shift ? KeyCategory.Uppercase : KeyCategory.Lowercase),
                _ => KeyMeaning.Unassigned
            };
    }

    private static AttachedKeyboard Board() => new(
        Name: "Test",
        PhysicalLayout: "ISO-DE",
        Canvas: new Canvas(500, 200), Matrix: new MatrixSize(6, 22),
        Keys:
        [
            new KeyDefinition("Keyboard_W", 0, 0, 19, 19, 2, 3),
            new KeyDefinition("Keyboard_A", 20, 0, 19, 19, 3, 2),
            new KeyDefinition("Keyboard_B", 40, 0, 19, 19, 4, 7),
        ]);

    private static KeyboardState State(ModifierKeys modifiers = ModifierKeys.None)
        => new(modifiers, new LockStates(false, false, false));

    private static ShortcutCatalogue With(ModifierKeys layer, params string[] characters)
    {
        var set = new ShortcutSet(
            characters.ToDictionary(
                c => c,
                c => new Shortcut(FunctionGroup.Tools, $"Does something with {c}"),
                StringComparer.OrdinalIgnoreCase),
            new Dictionary<string, Shortcut>(StringComparer.Ordinal));

        return new ShortcutCatalogue(new Dictionary<ModifierKeys, ShortcutSet> { [layer] = set });
    }

    /// <summary>
    /// The behaviour that must not change: with nothing defined for Shift, holding it shows
    /// uppercase rather than dimming the board.
    /// </summary>
    [Fact]
    public void ShiftStillShowsUppercaseWhenNoLayerIsDefined()
    {
        var composer = new FrameComposer(Board(), new Letters());
        var frame = composer.CreateFrame();

        composer.Compose(frame, State(ModifierKeys.LeftShift), ColourScheme.Default,
            DefaultShortcuts.Create());

        Assert.Equal(ColourScheme.Default.For(KeyCategory.Uppercase), frame[2, 3]);
        Assert.Equal(ColourScheme.Default.For(KeyCategory.Uppercase), frame[3, 2]);
    }

    /// <summary>
    /// And with a Shift layer defined, Shift becomes a function layer: only the keys it names
    /// light, in their function colour.
    /// </summary>
    [Fact]
    public void ADefinedShiftLayerFiltersLikeAnyOtherModifier()
    {
        var composer = new FrameComposer(Board(), new Letters());
        var frame = composer.CreateFrame();

        composer.Compose(frame, State(ModifierKeys.LeftShift), ColourScheme.Default,
            With(ModifierKeys.LeftShift, "w", "a"));

        Assert.Equal(ColourScheme.Default.For(FunctionGroup.Tools), frame[2, 3]);
        Assert.Equal(ColourScheme.Default.For(FunctionGroup.Tools), frame[3, 2]);

        // B carries nothing on that layer, so it goes dark like any unassigned key.
        Assert.Equal(RgbColor.Off, frame[4, 7]);
    }

    /// <summary>
    /// A layer for no modifier at all — the keyboard a game gives its own meaning to.
    /// </summary>
    [Fact]
    public void ALayerForNoModifierTakesOverTheRestingKeyboard()
    {
        var composer = new FrameComposer(Board(), new Letters());
        var frame = composer.CreateFrame();

        composer.Compose(frame, State(), ColourScheme.Default,
            With(ModifierKeys.None, "w", "a"));

        Assert.Equal(ColourScheme.Default.For(FunctionGroup.Tools), frame[2, 3]);
        Assert.Equal(RgbColor.Off, frame[4, 7]);
    }

    /// <summary>
    /// Without such a layer the resting keyboard is what it always was: coloured by what each
    /// key types.
    /// </summary>
    [Fact]
    public void TheRestingKeyboardIsUnchangedWithoutSuchALayer()
    {
        var composer = new FrameComposer(Board(), new Letters());
        var frame = composer.CreateFrame();

        composer.Compose(frame, State(), ColourScheme.Default, DefaultShortcuts.Create());

        Assert.Equal(ColourScheme.Default.For(KeyCategory.Lowercase), frame[2, 3]);
        Assert.Equal(ColourScheme.Default.For(KeyCategory.Lowercase), frame[4, 7]);
    }

    /// <summary>
    /// The layer may come from an application profile, which is the point: it applies while that
    /// program is in front and not otherwise.
    /// </summary>
    [Fact]
    public void AProfileCanDefineTheShiftLayerForItsOwnProgram()
    {
        var profile = new ApplicationProfile(
            Id: "test-game",
            Name: "Test game",
            Kind: ProfileKind.Game,
            Source: ProfileSource.Shipped,
            Match: new ProfileMatch(["testgame"], AppliesToGames: false, Priority: 10),
            Highlights: ApplicationProfile.NoHighlights,
            Shortcuts: new Dictionary<ModifierKeys, ShortcutSet>
            {
                [ModifierKeys.LeftShift] = new(
                    new Dictionary<string, Shortcut>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["w"] = new(FunctionGroup.Navigation, "Sprint")
                    },
                    new Dictionary<string, Shortcut>(StringComparer.Ordinal))
            });

        var composer = new FrameComposer(Board(), new Letters());
        var frame = composer.CreateFrame();

        composer.Compose(frame, State(ModifierKeys.LeftShift), ColourScheme.Default,
            DefaultShortcuts.Create(), profile);

        Assert.Equal(ColourScheme.Default.For(FunctionGroup.Navigation), frame[2, 3]);
        Assert.Equal(RgbColor.Off, frame[3, 2]);
    }

    /// <summary>Both layers have to survive being saved and read back.</summary>
    [Theory]
    [InlineData(ModifierKeys.LeftShift, "Shift")]
    [InlineData(ModifierKeys.None, "None")]
    public void BothLayersRoundTripThroughSettings(ModifierKeys layer, string expected)
    {
        Assert.Contains(layer, ModifierCombination.Known);

        var text = ModifierCombination.Format(layer);

        Assert.Equal(expected, text);
        Assert.True(ModifierCombination.TryParse(text, out var parsed));
        Assert.Equal(layer, parsed);
    }
}
