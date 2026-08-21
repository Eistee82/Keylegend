namespace Keylegend.Core.Session;

/// <summary>Who currently owns the keyboard lighting.</summary>
public enum LightingState
{
    /// <summary>Released. The vendor software drives the lighting.</summary>
    Idle,

    /// <summary>This application holds a session and is painting frames.</summary>
    Active,

    /// <summary>Released and staying released until resumed.</summary>
    Paused
}

/// <summary>
/// Decides when to take the lighting over and when to give it back. Taking over from a
/// running vendor effect costs about half a second, so the session is deliberately kept open
/// through short pauses in typing and only released once the user has really stopped.
/// </summary>
/// <remarks>
/// The clock is injected so that the timing rules can be tested without waiting.
/// </remarks>
public sealed class SessionManager
{
    /// <summary>
    /// An idle timeout of this value keeps the lighting for good: it is never handed back on
    /// its own, only by pausing or quitting.
    /// </summary>
    /// <remarks>
    /// A named value rather than zero or a nullable timeout. Zero already means something in a
    /// settings file — "unset, use the default" — and reusing it for the opposite meaning would
    /// make an old file change behaviour on upgrade.
    /// </remarks>
    public static TimeSpan Never { get; } = TimeSpan.MaxValue;

    private readonly TimeSpan _idleTimeout;
    private readonly Func<DateTimeOffset> _clock;

    private DateTimeOffset _lastActivity;
    private LightingState _state = LightingState.Idle;

    public SessionManager(TimeSpan idleTimeout, Func<DateTimeOffset> clock)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(idleTimeout, TimeSpan.Zero);

        _idleTimeout = idleTimeout;
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _lastActivity = _clock();
    }

    /// <summary>Whether the lighting is kept until the user pauses or quits.</summary>
    public bool HandsBack => _idleTimeout != Never;

    public LightingState State => _state;

    /// <summary>Raised on an actual transition, never on a repeat of the same state.</summary>
    public event Action<LightingState>? StateChanged;

    /// <summary>Reports that the user did something. Wakes the lighting unless paused.</summary>
    public void NoteActivity()
    {
        _lastActivity = _clock();

        if (_state == LightingState.Idle)
        {
            Transition(LightingState.Active);
        }
    }

    /// <summary>Releases the lighting and keeps it released until <see cref="Resume"/>.</summary>
    public void Pause() => Transition(LightingState.Paused);

    /// <summary>Allows activity to take the lighting over again.</summary>
    public void Resume()
    {
        if (_state == LightingState.Paused)
        {
            Transition(LightingState.Idle);
        }
    }

    /// <summary>
    /// Advances time. Call regularly; returns the current state so a caller can act on a
    /// hand-back without subscribing to the event.
    /// </summary>
    public LightingState Advance()
    {
        // With the hand-back switched off, take the lighting as soon as there is anything to
        // take: waiting for a keypress would leave the keyboard dark after every start, which
        // is precisely what switching it off was meant to avoid. Paused is left alone - that is
        // a decision the user made and outranks this.
        if (_idleTimeout == Never)
        {
            if (_state == LightingState.Idle)
            {
                Transition(LightingState.Active);
            }

            return _state;
        }

        if (_state == LightingState.Active && _clock() - _lastActivity >= _idleTimeout)
        {
            Transition(LightingState.Idle);
        }

        return _state;
    }

    private void Transition(LightingState next)
    {
        if (_state == next)
        {
            return;
        }

        _state = next;
        StateChanged?.Invoke(next);
    }
}
