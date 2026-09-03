using Keylegend.Core.Input;

namespace Keylegend.Core.Tests.Input;

/// <summary>
/// What the lighting effects are driven by: which keys are down, and since when.
/// </summary>
/// <remarks>
/// <para>
/// Derived from the set of keys that are down right now, never from edges. A program that counts
/// presses and releases has to see every one of them, and this one cannot: it polls, so a release
/// that happens while the screen is locked or the foreground changes is simply never observed. An
/// effect built on counted edges would leave that key dark for ever. A key that is not in the set
/// is up — and the next poll repairs whatever the last one missed.
/// </para>
/// <para>
/// It is asked for at all only while an effect is selected. With none, nothing calls it and
/// nothing polls the individual keys.
/// </para>
/// </remarks>
public class KeyActivityTests
{
    private static readonly DateTimeOffset T0 = new(2026, 8, 28, 12, 0, 0, TimeSpan.Zero);

    private static DateTimeOffset At(double seconds) => T0.AddSeconds(seconds);

    [Fact]
    public void NotesWhenAKeyWentDown()
    {
        var activity = new KeyActivity();

        activity.Observe(["Keyboard_A"], T0);

        Assert.True(activity.IsDown("Keyboard_A"));
        Assert.Equal(T0, activity.PressedAt("Keyboard_A"));
    }

    /// <summary>Held down is still the same press, so the moment it began must not drift.</summary>
    [Fact]
    public void KeepsTheMomentAPressBeganWhileTheKeyStaysDown()
    {
        var activity = new KeyActivity();

        activity.Observe(["Keyboard_A"], T0);
        activity.Observe(["Keyboard_A"], At(0.5));
        activity.Observe(["Keyboard_A"], At(1.0));

        Assert.Equal(T0, activity.PressedAt("Keyboard_A"));
    }

    [Fact]
    public void NotesWhenAKeyCameUp()
    {
        var activity = new KeyActivity();

        activity.Observe(["Keyboard_A"], T0);
        activity.Observe([], At(0.25));

        Assert.False(activity.IsDown("Keyboard_A"));
        Assert.Equal(At(0.25), activity.ReleasedAt("Keyboard_A"));
    }

    /// <summary>A key that is down has not been released, whatever it did before.</summary>
    [Fact]
    public void SaysAKeyThatIsDownHasNoReleaseBehindIt()
    {
        var activity = new KeyActivity();

        activity.Observe(["Keyboard_A"], T0);
        activity.Observe([], At(0.25));
        activity.Observe(["Keyboard_A"], At(0.5));

        Assert.Null(activity.ReleasedAt("Keyboard_A"));
        Assert.Equal(At(0.5), activity.PressedAt("Keyboard_A"));
    }

    [Fact]
    public void SaysNothingAboutAKeyItHasNeverSeen()
    {
        var activity = new KeyActivity();

        activity.Observe(["Keyboard_A"], T0);

        Assert.False(activity.IsDown("Keyboard_Z"));
        Assert.Null(activity.PressedAt("Keyboard_Z"));
        Assert.Null(activity.ReleasedAt("Keyboard_Z"));
    }

    /// <summary>
    /// What an effect spawns from: the presses that began in this round, not the ones still being
    /// held. A water drop falls once per press, not once per frame the key is down.
    /// </summary>
    [Fact]
    public void ListsOnlyThePressesThatBeganThisRound()
    {
        var activity = new KeyActivity();

        activity.Observe(["Keyboard_A"], T0);
        Assert.Equal(["Keyboard_A"], activity.JustPressed);

        activity.Observe(["Keyboard_A", "Keyboard_B"], At(0.1));
        Assert.Equal(["Keyboard_B"], activity.JustPressed);

        activity.Observe(["Keyboard_A", "Keyboard_B"], At(0.2));
        Assert.Empty(activity.JustPressed);
    }

    /// <summary>
    /// The self-healing property, and the reason this is derived from the set rather than from
    /// edges: a release nobody observed still ends the press.
    /// </summary>
    [Fact]
    public void TreatsAKeyMissingFromTheSetAsUp()
    {
        var activity = new KeyActivity();

        activity.Observe(["Keyboard_A", "Keyboard_B"], T0);

        // B vanishes without any release ever being seen — the screen locked, the foreground
        // changed, the poll landed badly.
        activity.Observe(["Keyboard_A"], At(5));

        Assert.False(activity.IsDown("Keyboard_B"));
        Assert.Equal(At(5), activity.ReleasedAt("Keyboard_B"));
    }

    /// <summary>
    /// Bounded on purpose. Nothing here is written anywhere, and nothing outlives the longest
    /// effect that could still be using it.
    /// </summary>
    [Fact]
    public void ForgetsKeysNothingHasTouchedForLongerThanItRemembers()
    {
        var activity = new KeyActivity(remembers: TimeSpan.FromSeconds(2));

        activity.Observe(["Keyboard_A"], T0);
        activity.Observe([], At(0.1));
        activity.Observe(["Keyboard_B"], At(9));

        Assert.Null(activity.PressedAt("Keyboard_A"));
        Assert.Null(activity.ReleasedAt("Keyboard_A"));
        Assert.Equal(At(9), activity.PressedAt("Keyboard_B"));
    }

    /// <summary>
    /// What an effect walks to decide whether anything is still moving. Without it, an effect
    /// would have to be told the whole keyboard just to ask "is anyone still fading?".
    /// </summary>
    [Fact]
    public void NamesTheKeysItIsCurrentlyHolding()
    {
        var activity = new KeyActivity();

        activity.Observe(["Keyboard_A", "Keyboard_B"], T0);
        activity.Observe(["Keyboard_A"], At(0.1));

        Assert.Equal(
            ["Keyboard_A", "Keyboard_B"],
            activity.Known.Order(StringComparer.Ordinal));
    }

    /// <summary>A key still held is never forgotten, however long it is held.</summary>
    [Fact]
    public void NeverForgetsAKeyThatIsStillDown()
    {
        var activity = new KeyActivity(remembers: TimeSpan.FromSeconds(2));

        activity.Observe(["Keyboard_A"], T0);
        activity.Observe(["Keyboard_A"], At(30));

        Assert.True(activity.IsDown("Keyboard_A"));
        Assert.Equal(T0, activity.PressedAt("Keyboard_A"));
    }
}
