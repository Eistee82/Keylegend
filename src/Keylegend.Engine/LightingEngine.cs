using Keylegend.Chroma;
using Keylegend.Core.Devices;
using Keylegend.Core.Input;
using Keylegend.Core.Lighting;
using Keylegend.Core.Lighting.Effects;
using Keylegend.Core.Profiles;
using Keylegend.Core.Session;

namespace Keylegend.Engine;

/// <summary>
/// Drives the lighting: watches for activity, holds a Chroma session while the user is
/// working, paints frames, and hands the keyboard back when things go quiet.
/// </summary>
/// <remarks>
/// Lives in its own assembly so that the window and the console driver run identical code.
/// Everything it decides comes from <see cref="FrameComposer"/>, so the on-screen preview and
/// the hardware cannot drift apart.
/// </remarks>
public sealed class LightingEngine
{
    private readonly AttachedKeyboard _keyboard;
    private readonly IChromaClient _chroma;
    private readonly IKeyStateSource _keys;
    private readonly FrameComposer _composer;
    private readonly Func<DateTimeOffset> _clock;
    private readonly LedFrame _frame;
    private readonly Func<ForegroundContext>? _foreground;

    // What the keystroke effects are made of. Both stay empty and untouched while no effect is
    // chosen, which is also when the key state source is never asked to name anything.
    private readonly KeyActivity _activity = new();
    private readonly EffectLayer _effects;

    private SessionManager _session;
    private EngineSettings _settings = new();

    /// <param name="foreground">
    /// Supplies which application is in front, for profile selection. Optional so the engine
    /// can be exercised without a desktop.
    /// </param>
    public LightingEngine(
        AttachedKeyboard keyboard,
        IChromaClient chroma,
        IKeyStateSource keys,
        IKeyResolver resolver,
        Func<DateTimeOffset>? clock = null,
        Func<ForegroundContext>? foreground = null)
    {
        _keyboard = keyboard ?? throw new ArgumentNullException(nameof(keyboard));
        _chroma = chroma ?? throw new ArgumentNullException(nameof(chroma));
        _keys = keys ?? throw new ArgumentNullException(nameof(keys));
        _clock = clock ?? (() => DateTimeOffset.UtcNow);
        _foreground = foreground;

        _composer = new FrameComposer(keyboard, resolver ?? throw new ArgumentNullException(nameof(resolver)));
        _effects = new EffectLayer(keyboard);
        _frame = _composer.CreateFrame();
        _session = new SessionManager(_settings.IdleTimeout, _clock);
    }

    /// <summary>Behaviour settings. Replacing them takes effect immediately.</summary>
    public EngineSettings Settings
    {
        get => _settings;
        set
        {
            ArgumentNullException.ThrowIfNull(value);

            var timeoutChanged = value.IdleTimeout != _settings.IdleTimeout;
            var effectChanged = value.Effect != _settings.Effect;
            _settings = value;

            if (effectChanged)
            {
                // Built here rather than per frame, and the layer clears both the effect being
                // put down and the one being taken up — a half-finished ripple must not run on
                // into whatever is chosen next.
                _effects.Effect = KeyEffects.Create(value.Effect, _keyboard);
                _activity.Clear();
            }

            if (timeoutChanged)
            {
                // The session manager takes its timeout at construction, so a change means a
                // new one. Carrying the state across keeps the lighting from flickering off.
                var previous = _session.State;
                _session = new SessionManager(value.IdleTimeout, _clock);

                if (previous == LightingState.Active)
                {
                    _session.NoteActivity();
                }
                else if (previous == LightingState.Paused)
                {
                    _session.Pause();
                }
            }

            _repaint = true;
        }
    }

    public LightingState State => _session.State;

    /// <summary>Raised for every composed frame, so a preview can mirror the hardware.</summary>
    public event Action<LedFrame>? FrameComposed;

    /// <summary>Raised on take-over and hand-back.</summary>
    public event Action<LightingState>? StateChanged;

    /// <summary>
    /// Raised when talking to Chroma fails, and again with <c>null</c> once it works. The engine
    /// keeps running and retrying either way, so the interface can say what is wrong and then take
    /// it back without either side tracking the other.
    /// </summary>
    /// <remarks>
    /// Both edges matter. A message that appears and never clears reads as broken long after the
    /// lighting has recovered, and a keyboard that goes dark with nothing said reads as this
    /// program having stopped for no reason.
    /// </remarks>
    public event Action<string?>? Fault;

    public void Pause() => _session.Pause();

    public void Resume() => _session.Resume();

    private bool _repaint;

    /// <summary>Runs until cancelled. Releases the lighting on the way out.</summary>
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        _session.StateChanged += OnSessionStateChanged;

        var connected = false;
        var faulted = false;
        var backoff = TimeSpan.FromSeconds(1);
        var lastSent = DateTimeOffset.MinValue;
        var takeoverAt = DateTimeOffset.MinValue;
        KeyboardState? lastState = null;
        ApplicationProfile? lastProfile = null;

        // See docs/en/architecture.md - three send rates, each for a different reason. Do not
        // collapse these into one; both simpler variants were tried and both are defective.
        var settleWindow = TimeSpan.FromSeconds(3);
        var settleInterval = TimeSpan.FromMilliseconds(120);
        var refreshInterval = TimeSpan.FromMilliseconds(750);

        // The fourth, and the only one that is not about the hand-over: while a keystroke effect
        // is moving, the picture changes although nothing about the keyboard state does. Thirty
        // a second reads as smooth and costs about two milliseconds each.
        var effectInterval = TimeSpan.FromMilliseconds(33);

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                if (_keys.AnyKeyDown())
                {
                    _session.NoteActivity();
                }

                if (_session.Advance() == LightingState.Active)
                {
                    try
                    {
                        if (!connected)
                        {
                            await _chroma.ConnectAsync(cancellationToken);
                            connected = true;
                            backoff = TimeSpan.FromSeconds(1);
                            takeoverAt = _clock();
                            lastSent = DateTimeOffset.MinValue;
                            lastState = null;
                        }

                        var now = _clock();
                        var current = _settings.OverrideState ?? _keys.Read();
                        var profile = SelectProfile();

                        // Only while an effect is chosen. This is the whole of the promise that
                        // the individual keys are otherwise never looked at.
                        // Only while an effect is chosen. This is the whole of the promise that
                        // the individual keys are otherwise never looked at.
                        //
                        // Every round, and not inside the sending below: what the effect has to
                        // say is what decides whether a frame is sent at all. Advanced only while
                        // sending, a keystroke waited for the next insurance frame — up to three
                        // quarters of a second — before the lighting answered it.
                        if (_effects.Effect is not null)
                        {
                            _activity.Observe(_keys.PressedKeys(), now);
                            _effects.Advance(_activity, now);
                        }

                        var changed = lastState != current || !ReferenceEquals(profile, lastProfile) || _repaint;

                        // A fourth rate, and the three that were there are untouched. An effect
                        // is a change nothing else reports: the keyboard state does not move
                        // while a fade runs, so without this the picture would jump from dark to
                        // lit in one step at the next insurance frame.
                        var interval = _effects.Animating
                            ? effectInterval
                            : now - takeoverAt < settleWindow ? settleInterval : refreshInterval;

                        if (changed || _effects.Animating || now - lastSent >= interval)
                        {
                            _composer.Compose(_frame, current, _settings.Scheme, _settings.Shortcuts, profile);
                            _effects.Paint(_frame, now);
                            lastProfile = profile;
                            await _chroma.SendAsync(_frame, cancellationToken);

                            lastState = current;
                            lastSent = _clock();
                            _repaint = false;

                            if (faulted)
                            {
                                faulted = false;
                                Fault?.Invoke(null);
                            }

                            FrameComposed?.Invoke(_frame);
                        }
                    }
                    catch (ChromaException ex)
                    {
                        faulted = true;
                        Fault?.Invoke($"{ex.Message} Retrying in {backoff.TotalSeconds:N0} s.");
                        connected = false;
                        await _chroma.DisconnectAsync(CancellationToken.None);
                        await Task.Delay(backoff, cancellationToken);
                        backoff = TimeSpan.FromSeconds(Math.Min(backoff.TotalSeconds * 2, 30));
                        continue;
                    }
                }
                else if (connected)
                {
                    await _chroma.DisconnectAsync(cancellationToken);
                    connected = false;
                    lastState = null;
                }

                await Task.Delay(16, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected on shutdown.
        }
        finally
        {
            _session.StateChanged -= OnSessionStateChanged;
            await _chroma.DisconnectAsync(CancellationToken.None);
        }
    }

    /// <summary>
    /// Composes a frame for a given state without sending it. The window uses this to show a
    /// layer while the lighting is idle.
    /// </summary>
    /// <param name="profile">
    /// Profile to compose with, or <c>null</c> to use whichever currently applies.
    /// </param>
    public LedFrame Preview(KeyboardState state, ApplicationProfile? profile = null)
    {
        var preview = _composer.CreateFrame();
        _composer.Compose(preview, state, _settings.Scheme, _settings.Shortcuts, profile ?? SelectProfile());

        return preview;
    }

    /// <summary>The application profile currently in effect, if any.</summary>
    public ApplicationProfile? ActiveProfile => SelectProfile();

    /// <summary>Raised when the applying profile changes, so the interface can say which.</summary>
    public event Action<ApplicationProfile?>? ProfileChanged;

    private ApplicationProfile? _announcedProfile;

    private ApplicationProfile? SelectProfile()
    {
        if (!_settings.UseApplicationProfiles || _foreground is null)
        {
            return null;
        }

        var selected = _settings.Profiles.Select(_foreground());

        if (!ReferenceEquals(selected, _announcedProfile))
        {
            _announcedProfile = selected;
            ProfileChanged?.Invoke(selected);
        }

        return selected;
    }

    /// <summary>The device this engine drives.</summary>
    public AttachedKeyboard Keyboard => _keyboard;

    private void OnSessionStateChanged(LightingState state) => StateChanged?.Invoke(state);
}
