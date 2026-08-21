using Keylegend.Chroma;
using Keylegend.Core.Devices;
using Keylegend.Core.Input;
using Keylegend.Core.Lighting;
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
    private readonly DeviceProfile _profile;
    private readonly IChromaClient _chroma;
    private readonly IKeyStateSource _keys;
    private readonly FrameComposer _composer;
    private readonly Func<DateTimeOffset> _clock;
    private readonly LedFrame _frame;
    private readonly Func<ForegroundContext>? _foreground;

    private SessionManager _session;
    private EngineSettings _settings = new();

    /// <param name="foreground">
    /// Supplies which application is in front, for profile selection. Optional so the engine
    /// can be exercised without a desktop.
    /// </param>
    public LightingEngine(
        DeviceProfile profile,
        IChromaClient chroma,
        IKeyStateSource keys,
        IKeyResolver resolver,
        Func<DateTimeOffset>? clock = null,
        Func<ForegroundContext>? foreground = null)
    {
        _profile = profile ?? throw new ArgumentNullException(nameof(profile));
        _chroma = chroma ?? throw new ArgumentNullException(nameof(chroma));
        _keys = keys ?? throw new ArgumentNullException(nameof(keys));
        _clock = clock ?? (() => DateTimeOffset.UtcNow);
        _foreground = foreground;

        _composer = new FrameComposer(profile, resolver ?? throw new ArgumentNullException(nameof(resolver)));
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
            _settings = value;

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

    /// <summary>Reports a problem talking to Chroma. The engine keeps running regardless.</summary>
    public event Action<string>? Warning;

    public void Pause() => _session.Pause();

    public void Resume() => _session.Resume();

    private bool _repaint;

    /// <summary>Runs until cancelled. Releases the lighting on the way out.</summary>
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        _session.StateChanged += OnSessionStateChanged;

        var connected = false;
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

                        var current = _settings.OverrideState ?? _keys.Read();
                        var profile = SelectProfile();
                        var changed = lastState != current || !ReferenceEquals(profile, lastProfile) || _repaint;
                        var interval = _clock() - takeoverAt < settleWindow ? settleInterval : refreshInterval;

                        if (changed || _clock() - lastSent >= interval)
                        {
                            _composer.Compose(_frame, current, _settings.Scheme, _settings.Shortcuts, profile);
                            lastProfile = profile;
                            await _chroma.SendAsync(_frame, cancellationToken);

                            lastState = current;
                            lastSent = _clock();
                            _repaint = false;

                            FrameComposed?.Invoke(_frame);
                        }
                    }
                    catch (ChromaException ex)
                    {
                        Warning?.Invoke($"{ex.Message} Retrying in {backoff.TotalSeconds:N0} s.");
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
    public DeviceProfile Profile => _profile;

    private void OnSessionStateChanged(LightingState state) => StateChanged?.Invoke(state);
}
