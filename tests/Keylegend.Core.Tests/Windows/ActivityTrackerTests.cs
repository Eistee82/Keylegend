using Keylegend.Core.Devices;
using Keylegend.Windows;

namespace Keylegend.Core.Tests.Windows;

/// <summary>
/// The one place that asks Windows what the keyboard is doing.
/// </summary>
/// <remarks>
/// <para>
/// It polls: it asks whether a key is down at this moment and nothing else. No hook is installed,
/// no keystroke is intercepted, forwarded or stored. That matters for privacy and because
/// anti-cheat systems object to keyboard hooks — and it is why naming the keys that are down is
/// not a change of character, only of detail.
/// </para>
/// <para>
/// The asking itself cannot be tested — it is the operating system — so it is handed in, and what
/// is under test is the part with a decision in it: turning what the board reports into the key
/// ids the rest of the program speaks.
/// </para>
/// </remarks>
public class ActivityTrackerTests
{
    /// <summary>Three keys whose scan codes the shipped table knows.</summary>
    private static AttachedKeyboard Keyboard() => new(
        "Test", "ISO-DE", new Canvas(100, 40), new MatrixSize(6, 22),
        [
            new KeyDefinition("Keyboard_A", 0, 0, 10, 10, Row: 3, Column: 2),
            new KeyDefinition("Keyboard_B", 10, 0, 10, 10, Row: 4, Column: 7),
            new KeyDefinition("Keyboard_C", 20, 0, 10, 10, Row: 4, Column: 5)
        ]);

    /// <summary>The virtual key a key id ends up watched under, as the tracker works it out.</summary>
    private static int VirtualKeyOf(string id)
    {
        var seen = new List<int>();
        var tracker = new ActivityTracker(
            new AttachedKeyboard(
                "Test", "ISO-DE", new Canvas(10, 10), new MatrixSize(6, 22),
                [new KeyDefinition(id, 0, 0, 10, 10, Row: 0, Column: 0)]),
            down: key => { seen.Add(key); return false; });

        tracker.AnyKeyDown();

        return Assert.Single(seen);
    }

    [Fact]
    public void WatchesOnlyTheKeysTheAttachedBoardActuallyHas()
    {
        var asked = new List<int>();
        var tracker = new ActivityTracker(Keyboard(), down: key => { asked.Add(key); return false; });

        tracker.AnyKeyDown();

        Assert.Equal(3, asked.Distinct().Count());
    }

    [Fact]
    public void SaysNobodyIsTypingWhenNoWatchedKeyIsDown()
    {
        var tracker = new ActivityTracker(Keyboard(), down: _ => false);

        Assert.False(tracker.AnyKeyDown());
        Assert.Empty(tracker.PressedKeys());
    }

    /// <summary>
    /// The detail the effects need, and the only thing that is new here: which key, not merely
    /// whether any.
    /// </summary>
    [Fact]
    public void NamesTheKeysThatAreDown()
    {
        var b = VirtualKeyOf("Keyboard_B");
        var tracker = new ActivityTracker(Keyboard(), down: key => key == b);

        Assert.True(tracker.AnyKeyDown());
        Assert.Equal(["Keyboard_B"], tracker.PressedKeys());
    }

    [Fact]
    public void NamesEveryKeyThatIsDownAtOnce()
    {
        var a = VirtualKeyOf("Keyboard_A");
        var c = VirtualKeyOf("Keyboard_C");
        var tracker = new ActivityTracker(Keyboard(), down: key => key == a || key == c);

        Assert.Equal(
            ["Keyboard_A", "Keyboard_C"],
            tracker.PressedKeys().Order(StringComparer.Ordinal));
    }

    /// <summary>
    /// Pause and Num Lock both send scan code <c>0x45</c>; only Pause's <c>E1</c> prefix tells
    /// them apart. Filed under the same code, both landed on Num Lock's virtual key, the second
    /// one named was dropped, and Pause lit up whenever Num Lock was pressed.
    /// </summary>
    [Fact]
    public void TellsPauseFromNumLock()
    {
        var keyboard = new AttachedKeyboard(
            "Test", "ISO-DE", new Canvas(100, 40), new MatrixSize(6, 22),
            [
                new KeyDefinition("Keyboard_PauseBreak", 0, 0, 10, 10, Row: 0, Column: 17),
                new KeyDefinition("Keyboard_NumLock", 10, 0, 10, 10, Row: 1, Column: 18)
            ]);

        var pause = VirtualKeyOf("Keyboard_PauseBreak");
        var numLock = VirtualKeyOf("Keyboard_NumLock");
        Assert.NotEqual(pause, numLock);

        var tracker = new ActivityTracker(keyboard, down: key => key == numLock);
        Assert.Equal(["Keyboard_NumLock"], tracker.PressedKeys());

        tracker = new ActivityTracker(keyboard, down: key => key == pause);
        Assert.Equal(["Keyboard_PauseBreak"], tracker.PressedKeys());
    }

    /// <summary>
    /// A key the board reports but the shipped scan-code table cannot name is simply not watched,
    /// rather than watched under a wrong number.
    /// </summary>
    [Fact]
    public void PassesOverKeysItCannotPlace()
    {
        var keyboard = new AttachedKeyboard(
            "Test", "ISO-DE", new Canvas(100, 40), new MatrixSize(6, 22),
            [
                new KeyDefinition("Keyboard_A", 0, 0, 10, 10, Row: 3, Column: 2),
                new KeyDefinition("Nothing_The_Table_Knows", 10, 0, 10, 10, Row: 3, Column: 3)
            ]);

        var tracker = new ActivityTracker(keyboard, down: _ => true);

        Assert.Equal(["Keyboard_A"], tracker.PressedKeys());
    }
}
