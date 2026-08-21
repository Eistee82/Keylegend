using Keylegend.Core.Configuration;
using Keylegend.Core.Input;
using Keylegend.Core.Lighting;
using Keylegend.Core.Profiles;
using Keylegend.Core.Shortcuts;

namespace Keylegend.Core.Tests.Shortcuts;

public class ModifierCombinationTests
{
    [Theory]
    [InlineData(ModifierKeys.LeftWin, "Win")]
    [InlineData(ModifierKeys.LeftCtrl, "Ctrl")]
    [InlineData(ModifierKeys.LeftAlt, "Alt")]
    [InlineData(ModifierKeys.LeftWin | ModifierKeys.LeftShift, "Win+Shift")]
    [InlineData(ModifierKeys.LeftCtrl | ModifierKeys.LeftAlt, "Ctrl+Alt")]
    [InlineData(ModifierKeys.None, "None")]
    public void FormatsCombinationsReadably(ModifierKeys modifiers, string expected)
        => Assert.Equal(expected, ModifierCombination.Format(modifiers));

    [Fact]
    public void AltGrIsItsOwnLayerRatherThanCtrlPlusAlt()
    {
        // Windows reports AltGr as Ctrl plus right Alt. Writing that out as "Ctrl+AltGr" would
        // describe a combination nobody can deliberately press.
        Assert.Equal("AltGr", ModifierCombination.Format(ModifierKeys.RightAlt | ModifierKeys.LeftCtrl));
    }

    [Theory]
    [InlineData("Win")]
    [InlineData("Ctrl+Shift")]
    [InlineData("Ctrl+Alt")]
    [InlineData("Win+Ctrl")]
    public void ParsingIsTheInverseOfFormatting(string text)
    {
        Assert.True(ModifierCombination.TryParse(text, out var modifiers));
        Assert.Equal(text, ModifierCombination.Format(modifiers));
    }

    [Theory]
    [InlineData("strg")]        // German
    [InlineData("umschalt")]
    [InlineData("CTRL")]
    [InlineData(" ctrl ")]
    public void ParsingAcceptsReasonableVariations(string text)
        => Assert.True(ModifierCombination.TryParse(text, out _));

    [Fact]
    public void AnUnknownWordCostsThatWordAndNothingElse()
    {
        // A typo in a hand-edited settings file should not discard the whole layer.
        Assert.True(ModifierCombination.TryParse("Ctrl+Wibble", out var modifiers));
        Assert.Equal("Ctrl", ModifierCombination.Format(modifiers));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("nonsense")]
    public void NothingRecognisableFails(string? text)
        => Assert.False(ModifierCombination.TryParse(text, out _));

    [Fact]
    public void EveryOfferedLayerFormatsAndParsesBack()
    {
        // The interface lists these, so a round-trip failure would mean an unsaveable layer.
        foreach (var layer in ModifierCombination.Known)
        {
            var text = ModifierCombination.Format(layer);

            Assert.True(ModifierCombination.TryParse(text, out var parsed), text);
            Assert.Equal(text, ModifierCombination.Format(parsed));
        }
    }

    [Fact]
    public void SavedShortcutsAreFoundAgainWhenTheModifierIsPressed()
    {
        // The real risk: a set could be stored under a key that lookup never produces, leaving
        // the user's edits invisible. Runs the saved catalogue through the actual lookup.
        var sets = new Dictionary<ModifierKeys, ShortcutSet>
        {
            [ModifierKeys.LeftWin] = new(
                new Dictionary<string, Shortcut>(StringComparer.OrdinalIgnoreCase)
                {
                    ["e"] = new(FunctionGroup.File, "Open Explorer")
                },
                new Dictionary<string, Shortcut>()),
            [ModifierKeys.LeftCtrl | ModifierKeys.LeftShift] = new(
                new Dictionary<string, Shortcut>(StringComparer.OrdinalIgnoreCase),
                new Dictionary<string, Shortcut>
                {
                    ["Keyboard_Escape"] = new(FunctionGroup.System, "Task manager")
                })
        };

        var stored = StoredSettings.From(
            TimeSpan.FromSeconds(60), ColourScheme.Default, new ProfileLibrary([]),
            startWithWindows: false, useApplicationProfiles: true,
            shortcuts: new ShortcutCatalogue(sets));

        var reloaded = stored.ToShortcutCatalogue();

        // Pressing the right-hand modifiers must find sets saved from the left-hand ones.
        var winState = new KeyboardState(ModifierKeys.RightWin, new LockStates(false, false, false));
        Assert.True(reloaded.TryGetSet(winState, out var winSet));
        Assert.True(winSet.TryGetByCharacter("e", out var shortcut));
        Assert.Equal(FunctionGroup.File, shortcut.Group);

        // The label has to survive too, or editing one layer would silently strip the names
        // from every shortcut in it.
        Assert.Equal("Open Explorer", shortcut.Label);

        var ctrlShift = new KeyboardState(
            ModifierKeys.RightCtrl | ModifierKeys.RightShift, new LockStates(false, false, false));
        Assert.True(reloaded.TryGetSet(ctrlShift, out var ctrlSet));
        Assert.True(ctrlSet.TryGet("Keyboard_Escape", out _));
    }

    [Fact]
    public void NoSavedShortcutsMeansTheShippedOnes()
    {
        // So that improvements to the shipped sets reach users who never edited them.
        var catalogue = new StoredSettings().ToShortcutCatalogue();
        var state = new KeyboardState(ModifierKeys.LeftWin, new LockStates(false, false, false));

        Assert.True(catalogue.TryGetSet(state, out var set));
        Assert.True(set.TryGetByCharacter("e", out _));
    }

    [Fact]
    public void LetterShortcutsAreStoredByCharacterNotByPosition()
    {
        // The bug this guards against: Ctrl+Z listed as "Keyboard_Z" lights the key printed Y
        // on a German keyboard, because the German Z sits where the US layout has Y. Undo and
        // redo would appear swapped on every QWERTZ board.
        var catalogue = DefaultShortcuts.Create();
        var ctrl = new KeyboardState(ModifierKeys.LeftCtrl, new LockStates(false, false, false));

        Assert.True(catalogue.TryGetSet(ctrl, out var set));

        Assert.True(set.TryGetByCharacter("z", out var undo));
        Assert.Equal(FunctionGroup.Edit, undo.Group);

        // No letter may be listed by position - that is what makes it layout dependent.
        Assert.DoesNotContain(set.Keys.Keys, id =>
            id.Length == "Keyboard_X".Length && id.StartsWith("Keyboard_", StringComparison.Ordinal)
            && char.IsAsciiLetter(id[^1]));
    }

    [Fact]
    public void CharacterLookupIgnoresCase()
    {
        var catalogue = DefaultShortcuts.Create();
        var ctrl = new KeyboardState(ModifierKeys.LeftCtrl, new LockStates(false, false, false));

        Assert.True(catalogue.TryGetSet(ctrl, out var set));
        Assert.True(set.TryGetByCharacter("C", out _));
        Assert.True(set.TryGetByCharacter("c", out _));
    }

    [Fact]
    public void AnEmptiedLayerStaysEmptyRatherThanReturningToTheDefault()
    {
        // Clearing a layer is a decision and must survive a restart.
        var sets = new Dictionary<ModifierKeys, ShortcutSet> { [ModifierKeys.LeftWin] = ShortcutSet.Empty };

        var stored = StoredSettings.From(
            TimeSpan.FromSeconds(60), ColourScheme.Default, new ProfileLibrary([]),
            false, true, new ShortcutCatalogue(sets));

        var reloaded = stored.ToShortcutCatalogue();
        var state = new KeyboardState(ModifierKeys.LeftWin, new LockStates(false, false, false));

        Assert.True(reloaded.TryGetSet(state, out var set));
        Assert.Empty(set.Keys);
    }
}
