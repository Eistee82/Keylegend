using Keylegend.Core.Session;

namespace Keylegend.Core.Tests.Session;

public class SessionManagerTests
{
    private sealed class TestClock
    {
        public DateTimeOffset Now { get; private set; } = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

        public void Advance(TimeSpan by) => Now += by;

        public Func<DateTimeOffset> Read => () => Now;
    }

    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    [Fact]
    public void StartsIdle()
    {
        var clock = new TestClock();

        Assert.Equal(LightingState.Idle, new SessionManager(Timeout, clock.Read).State);
    }

    [Fact]
    public void ActivityTakesTheLightingOver()
    {
        var clock = new TestClock();
        var manager = new SessionManager(Timeout, clock.Read);
        var transitions = new List<LightingState>();
        manager.StateChanged += transitions.Add;

        manager.NoteActivity();

        Assert.Equal(LightingState.Active, manager.State);
        Assert.Equal([LightingState.Active], transitions);
    }

    [Fact]
    public void ContinuedTypingRaisesNoFurtherEvents()
    {
        var clock = new TestClock();
        var manager = new SessionManager(Timeout, clock.Read);
        manager.NoteActivity();
        var transitions = new List<LightingState>();
        manager.StateChanged += transitions.Add;

        manager.NoteActivity();
        manager.NoteActivity();

        Assert.Empty(transitions);
    }

    [Fact]
    public void StaysActiveThroughShortPauses()
    {
        var clock = new TestClock();
        var manager = new SessionManager(Timeout, clock.Read);
        manager.NoteActivity();

        clock.Advance(TimeSpan.FromSeconds(9));

        Assert.Equal(LightingState.Active, manager.Advance());
    }

    [Fact]
    public void HandsTheLightingBackAfterTheIdleTimeout()
    {
        var clock = new TestClock();
        var manager = new SessionManager(Timeout, clock.Read);
        manager.NoteActivity();

        clock.Advance(TimeSpan.FromSeconds(10));

        Assert.Equal(LightingState.Idle, manager.Advance());
    }

    [Fact]
    public void TypingResetsTheIdleCountdown()
    {
        var clock = new TestClock();
        var manager = new SessionManager(Timeout, clock.Read);
        manager.NoteActivity();

        clock.Advance(TimeSpan.FromSeconds(9));
        manager.NoteActivity();
        clock.Advance(TimeSpan.FromSeconds(9));

        Assert.Equal(LightingState.Active, manager.Advance());
    }

    [Fact]
    public void PausedStaysPausedDespiteActivity()
    {
        var clock = new TestClock();
        var manager = new SessionManager(Timeout, clock.Read);
        manager.Pause();

        manager.NoteActivity();

        Assert.Equal(LightingState.Paused, manager.State);
    }

    [Fact]
    public void ResumeReturnsToIdleNotActive()
    {
        // Resuming should not light the keyboard up on its own; the next keypress does.
        var clock = new TestClock();
        var manager = new SessionManager(Timeout, clock.Read);
        manager.NoteActivity();
        manager.Pause();

        manager.Resume();

        Assert.Equal(LightingState.Idle, manager.State);
    }

    [Fact]
    public void RejectsANonPositiveTimeout()
    {
        var clock = new TestClock();

        Assert.Throws<ArgumentOutOfRangeException>(() => new SessionManager(TimeSpan.Zero, clock.Read));
    }

    [Fact]
    public void NeverKeepsTheLightingHoweverLongTheKeyboardIsQuiet()
    {
        var clock = new TestClock();
        var manager = new SessionManager(SessionManager.Never, clock.Read);

        manager.NoteActivity();
        clock.Advance(TimeSpan.FromDays(3));

        Assert.Equal(LightingState.Active, manager.Advance());
        Assert.False(manager.HandsBack);
    }

    [Fact]
    public void NeverTakesTheLightingWithoutWaitingForAKeypress()
    {
        // Otherwise the keyboard sits dark after every start until the user happens to type -
        // the opposite of what switching the hand-back off is for.
        var clock = new TestClock();
        var manager = new SessionManager(SessionManager.Never, clock.Read);

        Assert.Equal(LightingState.Active, manager.Advance());
    }

    [Fact]
    public void NeverTakesTheLightingBackAfterResuming()
    {
        var clock = new TestClock();
        var manager = new SessionManager(SessionManager.Never, clock.Read);
        manager.Advance();
        manager.Pause();

        manager.Resume();

        Assert.Equal(LightingState.Active, manager.Advance());
    }

    [Fact]
    public void NeverStillYieldsToPausing()
    {
        // Switching the hand-back off means "do not let go on your own", not "hold it whatever
        // happens" — pausing and quitting must still release the keyboard.
        var clock = new TestClock();
        var manager = new SessionManager(SessionManager.Never, clock.Read);
        manager.NoteActivity();

        manager.Pause();

        Assert.Equal(LightingState.Paused, manager.State);
    }
}
