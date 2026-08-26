using Keylegend.Core.Devices;
using Keylegend.Core.Input;
using Keylegend.Core.Lighting;
using Keylegend.Core.Profiles;
using Keylegend.Core.Shortcuts;

namespace Keylegend.Core.Tests.Shortcuts;

/// <summary>
/// The taskbar-position family and the Microsoft 365 layer. Both were missing, and the taskbar
/// digits were listed the wrong way; these tests are what keeps them right.
/// </summary>
public class TaskbarAndOfficeShortcutTests
{
    private static readonly ModifierKeys Win = ModifierKeys.LeftWin;
    private static readonly ModifierKeys WinShift = ModifierKeys.LeftWin | ModifierKeys.LeftShift;
    private static readonly ModifierKeys WinCtrl = ModifierKeys.LeftWin | ModifierKeys.LeftCtrl;
    private static readonly ModifierKeys WinAlt = ModifierKeys.LeftWin | ModifierKeys.LeftAlt;

    private static readonly ModifierKeys WinCtrlShift =
        ModifierKeys.LeftWin | ModifierKeys.LeftCtrl | ModifierKeys.LeftShift;

    private static readonly ModifierKeys OfficeKey =
        ModifierKeys.LeftWin | ModifierKeys.LeftCtrl | ModifierKeys.LeftAlt | ModifierKeys.LeftShift;

    private static readonly string[] TopRowDigits =
    [
        "Keyboard_1", "Keyboard_2", "Keyboard_3", "Keyboard_4", "Keyboard_5",
        "Keyboard_6", "Keyboard_7", "Keyboard_8", "Keyboard_9", "Keyboard_0"
    ];

    private static ShortcutSet SetFor(ModifierKeys modifiers)
    {
        var catalogue = DefaultShortcuts.Create();

        Assert.True(
            catalogue.Sets.TryGetValue(modifiers, out var set),
            $"No shortcut set is shipped for {ModifierCombination.Format(modifiers)}.");

        return set;
    }

    /// <summary>
    /// Windows binds all five taskbar layers, not just Win+digit. The layer that was documented
    /// in this file as carrying "exactly one command" carries eleven.
    /// </summary>
    [Theory]
    [InlineData("Win")]
    [InlineData("Win+Shift")]
    [InlineData("Win+Ctrl")]
    [InlineData("Win+Alt")]
    [InlineData("Win+Ctrl+Shift")]
    public void EveryTaskbarLayerCoversAllTenPositions(string layer)
    {
        var modifiers = layer switch
        {
            "Win" => Win,
            "Win+Shift" => WinShift,
            "Win+Ctrl" => WinCtrl,
            "Win+Alt" => WinAlt,
            _ => WinCtrlShift
        };

        var set = SetFor(modifiers);

        foreach (var keyId in TopRowDigits)
        {
            Assert.True(set.TryGet(keyId, out var shortcut), $"{layer}+{keyId} carries no command.");
            Assert.False(string.IsNullOrWhiteSpace(shortcut.Label), $"{layer}+{keyId} has no label.");
        }
    }

    /// <summary>
    /// The correction that matters: these commands are bound to the virtual key, so they must be
    /// listed by position. Listed by character they would miss the key on AZERTY, where the
    /// top-row key types "&amp;" unmodified.
    /// </summary>
    [Fact]
    public void TaskbarPositionsAreListedByPositionAndNotByCharacter()
    {
        foreach (var modifiers in new[] { Win, WinShift, WinCtrl, WinAlt, WinCtrlShift })
        {
            var set = SetFor(modifiers);

            for (var digit = 0; digit <= 9; digit++)
            {
                Assert.False(
                    set.TryGetByCharacter(digit.ToString(), out _),
                    $"{ModifierCombination.Format(modifiers)}+{digit} is listed by character; " +
                    "the taskbar commands are bound to the virtual key and belong under Keys.");
            }
        }
    }

    [Fact]
    public void AdministratorLayerIsMoreThanTheGraphicsDriver()
    {
        var set = SetFor(WinCtrlShift);

        Assert.True(set.TryGetByCharacter("b", out var driver));
        Assert.Equal("Restart the graphics driver", driver.Label);

        Assert.True(set.TryGet("Keyboard_1", out var admin));
        Assert.Contains("administrator", admin.Label!, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("w", "Word")]
    [InlineData("x", "Excel")]
    [InlineData("p", "PowerPoint")]
    [InlineData("n", "OneNote")]
    [InlineData("o", "Outlook")]
    [InlineData("t", "Teams")]
    [InlineData("d", "OneDrive")]
    public void TheOfficeKeyLayerOpensTheMicrosoftApps(string character, string application)
    {
        var set = SetFor(OfficeKey);

        Assert.True(set.TryGetByCharacter(character, out var shortcut));
        Assert.Contains(application, shortcut.Label!, StringComparison.Ordinal);
    }

    /// <summary>
    /// A layer the interface cannot offer is a layer nobody can edit, so the four-modifier
    /// combination has to survive the round trip through settings.
    /// </summary>
    [Fact]
    public void TheOfficeKeyLayerIsOfferedAndSurvivesBeingSaved()
    {
        Assert.Contains(OfficeKey, ModifierCombination.Known);

        var text = ModifierCombination.Format(OfficeKey);

        Assert.Equal("Win+Ctrl+Alt+Shift", text);
        Assert.True(ModifierCombination.TryParse(text, out var parsed));
        Assert.Equal(text, ModifierCombination.Format(parsed));
    }

    // ------------------------------------------------------------------ at the keyboard

    private sealed class DigitResolver : IKeyResolver
    {
        /// <summary>
        /// Both digit keys type "1" once Num Lock is on — which is exactly the trap. Only the
        /// top-row key opens a taskbar app.
        /// </summary>
        public KeyMeaning Resolve(string keyId, int? scanCode, KeyboardState state)
            => keyId switch
            {
                "Keyboard_1" => new KeyMeaning("1", KeyCategory.Digit),
                "Keyboard_Num1" => state.NumpadDigitsActive
                    ? new KeyMeaning("1", KeyCategory.Digit)
                    : KeyMeaning.Unassigned,
                _ => KeyMeaning.Unassigned
            };
    }

    /// <summary>
    /// The bug the position change fixes: with Num Lock on, the num pad's 1 types the same
    /// character as the top row while opening nothing, so a lookup by character lit a key that
    /// has no command.
    /// </summary>
    [Fact]
    public void TheNumPadStaysDarkOnTheTaskbarLayerWithNumLockOn()
    {
        var profile = new AttachedKeyboard(
            Name: "Test",
            PhysicalLayout: "ISO-DE",
            Canvas: new Canvas(500, 200), Matrix: new MatrixSize(6, 22),
            Keys:
            [
                new KeyDefinition("Keyboard_1", 0, 0, 19, 19, 1, 2),
                new KeyDefinition("Keyboard_Num1", 30, 0, 19, 19, 4, 18)
            ]);

        var composer = new FrameComposer(profile, new DigitResolver());
        var frame = composer.CreateFrame();
        var state = new KeyboardState(Win, new LockStates(NumLock: true, CapsLock: false, ScrollLock: false));

        composer.Compose(frame, state, ColourScheme.Default, DefaultShortcuts.Create());

        var topRow = frame[1, 2];
        var numPad = frame[4, 18];

        Assert.NotEqual(numPad, topRow);
    }
}
