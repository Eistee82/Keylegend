using System.Net.Http;
using System.Windows;
using Keylegend.App.Localisation;
using Keylegend.Chroma;
using Keylegend.Core.Configuration;
using Keylegend.Core.Devices;
using Keylegend.Core.Profiles;
using Keylegend.Engine;
using Keylegend.Windows;

namespace Keylegend.App;

public partial class App : Application
{
    private readonly CancellationTokenSource _stopping = new();

    private HttpClient? _http;
    private ChromaClient? _chroma;
    private LightingEngine? _engine;
    private Task? _running;
    private TrayIcon? _tray;
    private SingleInstance? _instance;

    private MainWindow? _window;
    private WaitingWindow? _waiting;

    /// <summary>Whether the waiting window has been put on screen once already.</summary>
    /// <remarks>
    /// Once, and never again by itself. A user who closed it has said what they think of it, and
    /// the search goes on for as long as the program runs — re-opening it on the next fruitless
    /// look would put a window back on screen every few seconds.
    /// </remarks>
    private bool _offeredWaiting;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Before anything else, because it must not touch the keyboard, the settings file or the
        // screen: --verify only reports whether this copy carries what it needs, and leaves.
        if (Verification.Requested(e.Args, out var reportPath))
        {
            Shutdown(Verification.Run(reportPath));
            return;
        }

        // Started by Windows at logon rather than by hand: come up in the notification area,
        // with no window in the way of whatever the user is doing.
        var minimised = Autostart.StartsMinimised(e.Args);

        // Only one copy may drive the keyboard. Two of them open two Chroma sessions for the
        // same device, the service gives it to one, and the other lights nothing while reporting
        // success — which looks like a program that has quietly stopped working.
        //
        // What to do about it depends on what is running. This very program from this very path
        // means somebody double-clicked the icon while it sat in the notification area: they want
        // the window, so it is asked for and this start bows out, leaving the lighting alone.
        // Anything else — an older version, or another folder — is superseded by this start.
        //
        // After --verify, which has to keep working while a copy runs: the release build calls it
        // against a packaged tree.
        try
        {
            _instance = SingleInstance.Claim("Keylegend");
        }
        catch (InvalidOperationException)
        {
            MessageBox.Show(
                Texts.Get("StartupAlreadyRunning"), "Keylegend",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            Shutdown(1);
            return;
        }

        if (!_instance.Owns)
        {
            // A logon start asks for nothing, so it says nothing: the copy already running is
            // doing the job, and a window appearing by itself is what --minimized exists to
            // prevent.
            if (!minimised)
            {
                _instance.AskRunningCopyToShow();
            }

            _instance.Dispose();
            _instance = null;
            Shutdown(0);
            return;
        }

        // The lighting, not the window, is the program. Without this a start with no window
        // would count as "no windows left" and end the process before it ever lit anything.
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        // Saved settings, or defaults where there are none. A damaged file costs the settings,
        // not the program: loading reports the problem instead of throwing.
        var store = new ConfigStore();
        var (stored, loadProblem) = store.Load();

        // The shipped profiles with the user's changes laid over them. Held here rather than
        // rebuilt from the engine's catalogue, because the catalogue is the flattened result and
        // no longer knows which parts the user overrode - which is what "reset" needs.
        var library = stored.ToProfileLibrary();

        // Unrecognised names fall back to following Windows rather than refusing to start, which
        // is what a hand-edited settings file most likely wants anyway.
        var language = Enum.TryParse<LanguageChoice>(stored.Language, out var choice)
            ? choice
            : LanguageChoice.Automatic;

        // Chosen here rather than in the main window, which is no longer the first thing to
        // speak: the notification area and the waiting window are on screen before it exists,
        // and would otherwise say their piece in whatever language Windows is set to.
        Texts.Instance.Use(language);

        // Before the keyboard is so much as looked for, and that is the point. The vendor's
        // software writes its description of the attached keyboard when it comes up at logon,
        // which may well be after this program has started; the icon has to stand there through
        // the whole wait, because an autostart that shows nothing looks like one that failed.
        _tray = new TrayIcon(Shutdown);

        _waiting = new WaitingWindow();
        _tray.Watch(_waiting);

        // Startup entries written before the switch existed still name the executable alone;
        // bring them up to date so the next logon is quiet as well. Done before the keyboard is
        // found, because it is true of this copy whether or not one ever turns up.
        Autostart.Refresh(Environment.ProcessPath);

        // The other side of the single-copy arrangement, both halves of it. A start of this same
        // program wants the window — whichever window this copy currently has; a different one
        // wants the keyboard, and gets it by this copy leaving through Shutdown, which hands the
        // Chroma session back so the keyboard returns to the vendor effect instead of freezing
        // on our last frame.
        _instance.WhenAskedToShow(() => Dispatcher.Invoke(ShowWindow), _stopping.Token);
        _instance.WhenAskedToQuit(() => Dispatcher.Invoke(() => Shutdown()), _stopping.Token);

        _ = StartWhenThereIsAKeyboard(store, stored, library, language, loadProblem, minimised);
    }

    /// <summary>
    /// Waits for the vendor's software to have a keyboard, then starts the lighting on it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This used to be one look, and a message box and an exit if it found nothing. That lost a
    /// race it could not win: the lighting service writes its description of the attached
    /// keyboard at logon, and on the machine this was measured on it did so ninety-five seconds
    /// after the system came up — eight seconds before this program's own autostart entry fired.
    /// Whichever of the two is slower that morning decided whether the program ran at all.
    /// </para>
    /// <para>
    /// So it waits instead. Nothing is opened to make the point: the user can see whether it
    /// works on the keyboard in front of them, and the notification area says the rest.
    /// </para>
    /// </remarks>
    private async Task StartWhenThereIsAKeyboard(
        ConfigStore store,
        StoredSettings stored,
        ProfileLibrary library,
        LanguageChoice language,
        string? loadProblem,
        bool minimised)
    {
        var search = new AttachedKeyboardSearch();
        search.Absent += find => Dispatcher.BeginInvoke(() => Waiting(find, minimised));

        AttachedKeyboard keyboard;

        try
        {
            // On a thread of its own: a look reads the lighting service's folder and, once that
            // names a device, walks the vendor's drawing cache — thousands of files, and slow on
            // a disk that has just been switched on. On the interface thread that alone would
            // hold the notification-area icon back for as long as it took.
            keyboard = await Task.Run(() => search.WaitAsync(_stopping.Token), _stopping.Token);
        }
        catch (OperationCanceledException)
        {
            // Quit while waiting. There is nothing to start and nothing to say.
            return;
        }

        // Quit in the moment between the keyboard turning up and this arriving back on the
        // interface thread. Building an engine and a window now would raise both after the
        // program had already let go of everything they need.
        if (_stopping.IsCancellationRequested)
        {
            return;
        }

        Begin(keyboard, store, stored, library, language, loadProblem, minimised);
    }

    /// <summary>Says what the latest look was missing, and offers the window that says it.</summary>
    private void Waiting(AttachedKeyboardFind find, bool minimised)
    {
        _waiting?.Report(find, DateTimeOffset.Now);

        if (minimised || _offeredWaiting || _waiting is null)
        {
            return;
        }

        // Only on a start that asked for a window. A logon start asked for none, and one
        // appearing over whatever the user is doing is exactly what --minimized exists to
        // prevent — the notification area carries this instead.
        _offeredWaiting = true;
        _waiting.Show();
    }

    /// <summary>Everything that needs a keyboard, once there is one.</summary>
    private void Begin(
        AttachedKeyboard keyboard,
        ConfigStore store,
        StoredSettings stored,
        ProfileLibrary library,
        LanguageChoice language,
        string? loadProblem,
        bool minimised)
    {
        var resolver = new WindowsKeyResolver();
        resolver.RefreshLayout();

        _http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
        _chroma = new ChromaClient(_http, new ChromaOptions());

        var foreground = new ForegroundWatcher();

        _engine = new LightingEngine(
            keyboard,
            _chroma,
            new WindowsKeyStateSource(keyboard),
            resolver,
            clock: null,
            foreground: () =>
            {
                var app = foreground.Read();

                return new ForegroundContext(app.ProcessName, app.WindowTitle, app.LooksLikeGame);
            });

        _engine.Settings = _engine.Settings with
        {
            IdleTimeout = stored.ToIdleTimeout(),
            Scheme = stored.ToColourScheme(),
            Profiles = library.Catalogue(),
            Shortcuts = stored.ToShortcutCatalogue(),
            UseApplicationProfiles = stored.UseApplicationProfiles,
            Effect = stored.ToKeyEffect()
        };

        // The engine runs for the whole life of the application; the window only observes it.
        // That way closing the window to the tray does not interrupt the lighting.
        _running = _engine.RunAsync(_stopping.Token);

        var window = new MainWindow(
            _engine, resolver, store, library, language,
            stored.ToIdlePeriod(), stored.HandBackWhenIdle, loadProblem);

        _window = window;
        _tray?.Watch(window);
        _tray?.Drive(_engine);

        // The real window takes the waiting one's place only where there was a place to take.
        // Let go of by the notification area first, above, so that this closes it rather than
        // hiding it.
        var wasWaitingOnScreen = _waiting?.IsVisible == true;

        _waiting?.Close();
        _waiting = null;

        // Somebody watching the waiting window gets the real one in its stead. Somebody who
        // closed that window meant it, and somebody who started at logon asked for no window at
        // all — both keep the notification area and a keyboard that simply starts working.
        if (!minimised && (wasWaitingOnScreen || !_offeredWaiting))
        {
            window.Show();
        }
    }

    /// <summary>Brings up whichever window this copy currently has.</summary>
    private void ShowWindow()
    {
        var window = (Window?)_window ?? _waiting;

        if (window is null)
        {
            return;
        }

        window.Show();
        window.WindowState = WindowState.Normal;
        window.Activate();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _stopping.Cancel();

        try
        {
            // Give the engine a moment to release the Chroma session, so the vendor effect
            // resumes rather than the keyboard freezing on our last frame.
            _running?.Wait(TimeSpan.FromSeconds(2));
        }
        catch (AggregateException)
        {
            // Cancellation surfaces here; nothing to do about it on the way out.
        }

        _tray?.Dispose();
        _chroma?.DisposeAsync().AsTask().Wait(TimeSpan.FromSeconds(2));
        _http?.Dispose();

        // Last, and deliberately so: releasing the claim is what lets a waiting copy start, and
        // it must not start while this one still holds the Chroma session.
        _instance?.Dispose();

        _stopping.Dispose();

        base.OnExit(e);
    }
}
