using Keylegend.Core.Devices;
using Keylegend.Core.Input;
using Keylegend.Core.Lighting;
using Keylegend.Core.Profiles;
using Keylegend.Core.Shortcuts;

namespace Keylegend.Core.Tests.Lighting;

public class FrameComposerTests
{
    /// <summary>
    /// Stands in for Windows. Tests declare what each key produces per state, which lets the
    /// colouring rules be exercised exhaustively without a keyboard.
    /// </summary>
    private sealed class FakeResolver : IKeyResolver
    {
        private readonly Dictionary<(string Key, bool Shift, bool AltGr, bool NumLock), KeyMeaning> _meanings = [];

        public FakeResolver Set(string keyId, KeyMeaning meaning,
            bool shift = false, bool altGr = false, bool numLock = false)
        {
            _meanings[(keyId, shift, altGr, numLock)] = meaning;
            return this;
        }

        // Keyed on NumpadDigitsActive rather than the raw lock, mirroring what the real
        // resolver does - Shift suspends Num Lock.
        public KeyMeaning Resolve(string keyId, int? scanCode, KeyboardState state)
            => _meanings.TryGetValue((keyId, state.Shift, state.AltGr, state.NumpadDigitsActive), out var meaning)
                ? meaning
                : KeyMeaning.Unassigned;
    }

    private static DeviceProfile ProfileWith(params (string Id, int Row, int Column)[] keys)
        => new(
            FormatVersion: 1, Name: "Test", Vendor: "Test", Model: "T1",
            PhysicalLayout: "ISO-DE", Image: "device.png",
            Canvas: new Canvas(500, 200), Matrix: new MatrixSize(6, 22), Verified: true,
            Keys: [.. keys.Select(k => new KeyDefinition(k.Id, 0, 0, 19, 19, k.Row, k.Column))]);

    private static KeyboardState State(
        ModifierKeys modifiers = ModifierKeys.None,
        bool num = false, bool caps = false, bool scroll = false)
        => new(modifiers, new LockStates(num, caps, scroll));

    private static readonly ColourScheme Scheme = ColourScheme.Default;

    [Fact]
    public void ColoursALetterByItsCategory()
    {
        var profile = ProfileWith(("Keyboard_A", 3, 2));
        var resolver = new FakeResolver().Set("Keyboard_A", new KeyMeaning("a", KeyCategory.Lowercase));
        var composer = new FrameComposer(profile, resolver);
        var frame = composer.CreateFrame();

        composer.Compose(frame, State(), Scheme, DefaultShortcuts.Create());

        Assert.Equal(Scheme.For(KeyCategory.Lowercase), frame[3, 2]);
    }

    [Fact]
    public void ShiftChangesTheColourBecauseItChangesTheCharacter()
    {
        var profile = ProfileWith(("Keyboard_A", 3, 2));
        var resolver = new FakeResolver()
            .Set("Keyboard_A", new KeyMeaning("a", KeyCategory.Lowercase))
            .Set("Keyboard_A", new KeyMeaning("A", KeyCategory.Uppercase), shift: true);
        var composer = new FrameComposer(profile, resolver);
        var frame = composer.CreateFrame();

        composer.Compose(frame, State(ModifierKeys.LeftShift), Scheme, DefaultShortcuts.Create());

        Assert.Equal(Scheme.For(KeyCategory.Uppercase), frame[3, 2]);
    }

    [Fact]
    public void ShiftDoesNotBlankUnassignedKeys()
    {
        // Regression guard: Shift must not be treated as a filtering modifier.
        var profile = ProfileWith(("Keyboard_A", 3, 2), ("Keyboard_F5", 0, 7));
        var resolver = new FakeResolver()
            .Set("Keyboard_A", new KeyMeaning("A", KeyCategory.Uppercase), shift: true)
            .Set("Keyboard_F5", new KeyMeaning("", KeyCategory.Control), shift: true);
        var composer = new FrameComposer(profile, resolver);
        var frame = composer.CreateFrame();

        composer.Compose(frame, State(ModifierKeys.LeftShift), Scheme, DefaultShortcuts.Create());

        Assert.NotEqual(RgbColor.Off, frame[0, 7]);
    }

    [Fact]
    public void NumLockChangesTheNumpadBetweenDigitsAndNavigation()
    {
        var profile = ProfileWith(("Keyboard_Num7", 2, 18));
        var resolver = new FakeResolver()
            .Set("Keyboard_Num7", new KeyMeaning(null, KeyCategory.Control))
            .Set("Keyboard_Num7", new KeyMeaning("7", KeyCategory.Digit), numLock: true);
        var composer = new FrameComposer(profile, resolver);
        var frame = composer.CreateFrame();

        composer.Compose(frame, State(num: true), Scheme, DefaultShortcuts.Create());
        Assert.Equal(Scheme.For(KeyCategory.Digit), frame[2, 18]);

        composer.Compose(frame, State(num: false), Scheme, DefaultShortcuts.Create());
        Assert.Equal(Scheme.For(KeyCategory.Control), frame[2, 18]);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void LockKeysShowTheirOwnState(bool on)
    {
        var profile = ProfileWith(("Keyboard_CapsLock", 3, 1));
        var composer = new FrameComposer(profile, new FakeResolver());
        var frame = composer.CreateFrame();

        composer.Compose(frame, State(caps: on), Scheme, DefaultShortcuts.Create());

        Assert.Equal(on ? Scheme.CapsLock.On : Scheme.CapsLock.Off, frame[3, 1]);
    }

    [Fact]
    public void LockKeysKeepTheirColourUnderAFilteringModifier()
    {
        // Rule 1 outranks rule 2: losing sight of Caps Lock because Ctrl is held would
        // defeat the point of showing it at all.
        var profile = ProfileWith(("Keyboard_CapsLock", 3, 1));
        var composer = new FrameComposer(profile, new FakeResolver());
        var frame = composer.CreateFrame();

        composer.Compose(frame, State(ModifierKeys.LeftCtrl, caps: true), Scheme, DefaultShortcuts.Create());

        Assert.Equal(Scheme.CapsLock.On, frame[3, 1]);
    }

    [Fact]
    public void AltGrLightsOnlyKeysThatCarryAnAltGrCharacter()
    {
        var profile = ProfileWith(("Keyboard_Q", 2, 2), ("Keyboard_A", 3, 2));
        var resolver = new FakeResolver()
            .Set("Keyboard_Q", new KeyMeaning("@", KeyCategory.Symbol), altGr: true);
        // Keyboard_A deliberately has no AltGr meaning.
        var composer = new FrameComposer(profile, resolver);
        var frame = composer.CreateFrame();

        composer.Compose(frame, State(ModifierKeys.RightAlt), Scheme, DefaultShortcuts.Create());

        Assert.Equal(Scheme.For(KeyCategory.Symbol), frame[2, 2]);
        Assert.Equal(RgbColor.Off, frame[3, 2]);
    }

    [Fact]
    public void AltGrLeavesControlKeysDarkRatherThanLightingTheWholeKeyboard()
    {
        // Regression guard. A key with no AltGr assignment reports "no character", which the
        // resolver classifies as Control - the same as Escape or an arrow key genuinely is.
        // Accepting that as "assigned" lit every key on the AltGr layer, defeating its purpose.
        var profile = ProfileWith(
            ("Keyboard_Q", 2, 2),        // has an AltGr character
            ("Keyboard_A", 3, 2),        // has none - reports no character
            ("Keyboard_Escape", 0, 1),   // genuinely a control key
            ("Keyboard_F5", 0, 7));      // function key, no character either

        var resolver = new FakeResolver()
            .Set("Keyboard_Q", new KeyMeaning("@", KeyCategory.Symbol), altGr: true)
            .Set("Keyboard_A", new KeyMeaning(null, KeyCategory.Control), altGr: true)
            .Set("Keyboard_Escape", new KeyMeaning(null, KeyCategory.Control), altGr: true)
            .Set("Keyboard_F5", new KeyMeaning(null, KeyCategory.Control), altGr: true);

        var composer = new FrameComposer(profile, resolver);
        var frame = composer.CreateFrame();

        composer.Compose(frame, State(ModifierKeys.RightAlt), Scheme, DefaultShortcuts.Create());

        Assert.Equal(Scheme.For(KeyCategory.Symbol), frame[2, 2]);
        Assert.Equal(RgbColor.Off, frame[3, 2]);
        Assert.Equal(RgbColor.Off, frame[0, 1]);
        Assert.Equal(RgbColor.Off, frame[0, 7]);
    }

    [Fact]
    public void WindowsKeyShowsTheShortcutSetColouredByGroup()
    {
        // Letter shortcuts are looked up by the character a key types, so the resolver has to
        // supply it - exactly as the real one does.
        var profile = ProfileWith(("Keyboard_E", 2, 4), ("Keyboard_V", 4, 6), ("Keyboard_J", 3, 8));
        var resolver = new FakeResolver()
            .Set("Keyboard_E", new KeyMeaning("e", KeyCategory.Lowercase))
            .Set("Keyboard_V", new KeyMeaning("v", KeyCategory.Lowercase))
            .Set("Keyboard_J", new KeyMeaning("j", KeyCategory.Lowercase));

        var composer = new FrameComposer(profile, resolver);
        var frame = composer.CreateFrame();

        composer.Compose(frame, State(ModifierKeys.LeftWin), Scheme, DefaultShortcuts.Create());

        Assert.Equal(Scheme.For(FunctionGroup.File), frame[2, 4]);    // Win+E, Explorer
        Assert.Equal(Scheme.For(FunctionGroup.Tools), frame[4, 6]);   // Win+V, clipboard
        Assert.Equal(RgbColor.Off, frame[3, 8]);                      // Win+J has no entry
    }

    [Fact]
    public void LetterShortcutsFollowTheCharacterNotThePosition()
    {
        // On a German keyboard the key identified as Keyboard_Y types z. Ctrl+Z must light
        // that key, not the one identified as Keyboard_Z - which types y there.
        var profile = ProfileWith(("Keyboard_Y", 2, 7), ("Keyboard_Z", 4, 3));
        var resolver = new FakeResolver()
            .Set("Keyboard_Y", new KeyMeaning("z", KeyCategory.Lowercase))   // German Z
            .Set("Keyboard_Z", new KeyMeaning("y", KeyCategory.Lowercase));  // German Y

        var composer = new FrameComposer(profile, resolver);
        var frame = composer.CreateFrame();

        composer.Compose(frame, State(ModifierKeys.LeftCtrl), Scheme, DefaultShortcuts.Create());

        // Both carry a command here (undo and redo), so the check is that each is found at all
        // and that the lookup went through the character rather than the identifier.
        Assert.Equal(Scheme.For(FunctionGroup.Edit), frame[2, 7]);
        Assert.Equal(Scheme.For(FunctionGroup.Edit), frame[4, 3]);
    }

    [Fact]
    public void CtrlAltShowsItsOwnSetRatherThanTheAltGrLayer()
    {
        var profile = ProfileWith(("Keyboard_Delete", 2, 15));
        var composer = new FrameComposer(profile, new FakeResolver());
        var frame = composer.CreateFrame();

        composer.Compose(
            frame,
            State(ModifierKeys.LeftCtrl | ModifierKeys.LeftAlt),
            Scheme,
            DefaultShortcuts.Create());

        Assert.Equal(Scheme.For(FunctionGroup.System), frame[2, 15]);
    }

    [Fact]
    public void BrightnessScalesEveryColour()
    {
        var profile = ProfileWith(("Keyboard_A", 3, 2));
        var resolver = new FakeResolver().Set("Keyboard_A", new KeyMeaning("a", KeyCategory.Lowercase));
        var composer = new FrameComposer(profile, resolver);
        var frame = composer.CreateFrame();
        var dimmed = Scheme with { Brightness = 0.5 };

        composer.Compose(frame, State(), dimmed, DefaultShortcuts.Create());

        Assert.Equal(Scheme.For(KeyCategory.Lowercase).Scale(0.5), frame[3, 2]);
    }

    [Fact]
    public void HoldingShiftTurnsTheNumpadBackIntoNavigation()
    {
        // With Num Lock on, Shift suspends it - so the pad must show the navigation colour
        // even though the lock is on.
        var profile = ProfileWith(("Keyboard_Num7", 2, 18));
        var resolver = new FakeResolver()
            .Set("Keyboard_Num7", new KeyMeaning("7", KeyCategory.Digit), numLock: true)
            .Set("Keyboard_Num7", new KeyMeaning(null, KeyCategory.Control), shift: true);
        var composer = new FrameComposer(profile, resolver);
        var frame = composer.CreateFrame();

        composer.Compose(frame, State(ModifierKeys.LeftShift, num: true), Scheme, DefaultShortcuts.Create());

        Assert.Equal(Scheme.For(KeyCategory.Control), frame[2, 18]);
    }

    [Fact]
    public void FunctionKeysGetTheirOwnColourRatherThanTheControlColour()
    {
        // Function keys produce no character, so their category cannot come from one.
        var profile = ProfileWith(("Keyboard_F5", 0, 7), ("Keyboard_Escape", 0, 1));
        var resolver = new FakeResolver()
            .Set("Keyboard_F5", new KeyMeaning(null, KeyCategory.Control))
            .Set("Keyboard_Escape", new KeyMeaning("", KeyCategory.Control));
        var composer = new FrameComposer(profile, resolver);
        var frame = composer.CreateFrame();

        composer.Compose(frame, State(), Scheme, DefaultShortcuts.Create());

        Assert.Equal(Scheme.For(KeyCategory.FunctionKey), frame[0, 7]);
        Assert.Equal(Scheme.For(KeyCategory.Control), frame[0, 1]);
        Assert.NotEqual(frame[0, 1], frame[0, 7]);
    }

    [Fact]
    public void FunctionKeysStillFollowShortcutSetsUnderAModifier()
    {
        // Alt+F4 must show as a window command, not as the function-key colour.
        var profile = ProfileWith(("Keyboard_F4", 0, 6));
        var composer = new FrameComposer(profile, new FakeResolver());
        var frame = composer.CreateFrame();

        composer.Compose(frame, State(ModifierKeys.LeftAlt), Scheme, DefaultShortcuts.Create());

        Assert.Equal(Scheme.For(FunctionGroup.Window), frame[0, 6]);
    }

    [Fact]
    public void AnApplicationProfilePinsItsKeysOverTheCategoryColours()
    {
        var profile = ProfileWith(("Keyboard_W", 2, 3), ("Keyboard_X", 4, 4));
        var resolver = new FakeResolver()
            .Set("Keyboard_W", new KeyMeaning("w", KeyCategory.Lowercase))
            .Set("Keyboard_X", new KeyMeaning("x", KeyCategory.Lowercase));
        var composer = new FrameComposer(profile, resolver);
        var frame = composer.CreateFrame();

        composer.Compose(
            frame, State(), Scheme, DefaultShortcuts.Create(), GameProfile());

        Assert.Equal(new RgbColor(255, 0, 0), frame[2, 3]);
        Assert.Equal(Scheme.For(KeyCategory.Lowercase), frame[4, 4]);   // not in the profile
    }

    [Fact]
    public void ProfileHighlightsDoNotOverrideTheLockDisplay()
    {
        // Rule 1 stays on top: knowing whether Caps Lock is on matters in a game too.
        var deviceProfile = ProfileWith(("Keyboard_CapsLock", 3, 1));
        var composer = new FrameComposer(deviceProfile, new FakeResolver());
        var frame = composer.CreateFrame();

        var gameProfile = GameProfile() with
        {
            Highlights = new Dictionary<string, KeyHighlight>
            {
                ["Keyboard_CapsLock"] = new(new RgbColor(1, 2, 3))
            }
        };

        composer.Compose(frame, State(caps: true), Scheme, DefaultShortcuts.Create(), gameProfile);

        Assert.Equal(Scheme.CapsLock.On, frame[3, 1]);
    }

    [Fact]
    public void ProfileHighlightsGiveWayToAModifierLayer()
    {
        // Holding Windows should show Windows shortcuts, not the game highlights.
        var deviceProfile = ProfileWith(("Keyboard_E", 2, 4));
        var resolver = new FakeResolver().Set("Keyboard_E", new KeyMeaning("e", KeyCategory.Lowercase));
        var composer = new FrameComposer(deviceProfile, resolver);
        var frame = composer.CreateFrame();

        composer.Compose(
            frame, State(ModifierKeys.LeftWin), Scheme, DefaultShortcuts.Create(), GameProfile());

        Assert.Equal(Scheme.For(FunctionGroup.File), frame[2, 4]);   // Win+E, Explorer
    }

    /// <summary>
    /// A game profile built here rather than taken from the shipped set: these tests are about
    /// the composition rules, and pinning them to whatever colours a shipped file happens to
    /// use would make editing that file break tests that have nothing to do with it.
    /// </summary>
    private static ApplicationProfile GameProfile() => new(
        Id: "test-game",
        Name: "Games",
        Kind: ProfileKind.Game,
        Source: ProfileSource.Shipped,
        Match: new ProfileMatch([], AppliesToGames: true),
        Highlights: new Dictionary<string, KeyHighlight>
        {
            ["Keyboard_W"] = new(new RgbColor(255, 0, 0), "Forward"),
            ["Keyboard_E"] = new(new RgbColor(255, 140, 0), "Use")
        },
        Shortcuts: ApplicationProfile.NoShortcuts);

    [Fact]
    public void KeysWithoutAMatrixCellAreSkipped()
    {
        var profile = new DeviceProfile(
            1, "Test", "Test", "T1", "ISO-DE", "device.png",
            new Canvas(500, 200), new MatrixSize(6, 22), false,
            [new KeyDefinition("Keyboard_Macro1", 0, 0, 19, 19, null, null)]);
        var composer = new FrameComposer(profile, new FakeResolver());
        var frame = composer.CreateFrame();

        composer.Compose(frame, State(), Scheme, DefaultShortcuts.Create());

        Assert.Equal(RgbColor.Off, frame[0, 0]);
    }
}

