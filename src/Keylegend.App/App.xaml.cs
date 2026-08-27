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

        AttachedKeyboard keyboard;
        try
        {
            // Ask the lighting service what is actually plugged in. It knows the model by name,
            // states the physical layout outright, and lists the keys the hardware really has.
            // So the keyboard this program lights is described from the hardware, never guessed.
            keyboard = FromAttachedDevice();

            var problems = AttachedKeyboardValidator.Validate(keyboard);
            if (problems.Count > 0)
            {
                throw new AttachedKeyboardException(
                    "The attached keyboard could not be described:" + Environment.NewLine + "  " +
                    string.Join(Environment.NewLine + "  ", problems));
            }
        }
        catch (AttachedKeyboardException ex)
        {
            MessageBox.Show(ex.Message, "Keylegend", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
            return;
        }

        // Three ways this can fail and there is nothing shipped here to fall back on, so each
        // says which one it was. They call for different things of the user, and one message
        // covering all three would send two thirds of the people who see it the wrong way.
        static AttachedKeyboard FromAttachedDevice()
        {
            // What the lighting service says is plugged in. Absent when Synapse is not installed
            // or not running, or when no Razer keyboard is connected.
            var attached = SdkDeviceDescription.ReadAll().FirstOrDefault();

            if (attached is null || attached.Keys.Count == 0)
            {
                throw new AttachedKeyboardException(Texts.Get("StartupNoKeyboard"));
            }

            // The vendor's own drawing of the attached model. It carries the keys and their
            // names, their real sizes, the casing, and the legends printed on the caps in the
            // right language. What it does not carry — which cell each key lights — is a constant
            // of the protocol.
            //
            // Missing is its own case, and not the same as "no keyboard": Synapse is running and
            // has named the device. The drawing lives in the cache of its web interface, which
            // fills when that interface shows the device — so opening Synapse once is the fix,
            // and saying "connect your keyboard" would be plainly wrong.
            var drawing = SvgLayoutSource.Find(attached)
                ?? throw new AttachedKeyboardException(Texts.Get("StartupNoDrawing"));

            // Present but not understood is a third thing again, and nothing the user can act on:
            // the file is there and we cannot read it, which means the format moved under us.
            return AttachedKeyboardBuilder.FromDrawing(attached, drawing)
                ?? throw new AttachedKeyboardException(Texts.Get("StartupDrawingUnreadable"));
        }

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

                return new Core.Profiles.ForegroundContext(app.ProcessName, app.WindowTitle, app.LooksLikeGame);
            });

        // Saved settings, or defaults where there are none. A damaged file costs the settings,
        // not the program: loading reports the problem instead of throwing.
        var store = new ConfigStore();
        var (stored, loadProblem) = store.Load();

        // The shipped profiles with the user's changes laid over them. Held here rather than
        // rebuilt from the engine's catalogue, because the catalogue is the flattened result and
        // no longer knows which parts the user overrode - which is what "reset" needs.
        var library = stored.ToProfileLibrary();

        _engine.Settings = _engine.Settings with
        {
            IdleTimeout = stored.ToIdleTimeout(),
            Scheme = stored.ToColourScheme(),
            Profiles = library.Catalogue(),
            Shortcuts = stored.ToShortcutCatalogue(),
            UseApplicationProfiles = stored.UseApplicationProfiles
        };

        // The engine runs for the whole life of the application; the window only observes it.
        // That way closing the window to the tray does not interrupt the lighting.
        _running = _engine.RunAsync(_stopping.Token);

        // Unrecognised names fall back to following Windows rather than refusing to start, which
        // is what a hand-edited settings file most likely wants anyway.
        var language = Enum.TryParse<Localisation.LanguageChoice>(stored.Language, out var choice)
            ? choice
            : Localisation.LanguageChoice.Automatic;

        var window = new MainWindow(
            _engine, resolver, store, library, language,
            stored.ToIdlePeriod(), stored.HandBackWhenIdle, loadProblem);
        _tray = new TrayIcon(_engine, window, Shutdown);

        // Startup entries written before the switch existed still name the executable alone;
        // bring them up to date so the next logon is quiet as well.
        Autostart.Refresh(Environment.ProcessPath);

        if (!minimised)
        {
            window.Show();
        }

        // The other side of the same arrangement, both halves of it. A start of this same
        // program wants the window; a different one wants the keyboard, and gets it by this copy
        // leaving through Shutdown — which hands the Chroma session back, so the keyboard returns
        // to the vendor effect instead of freezing on our last frame.
        _instance?.WhenAskedToShow(
            () => Dispatcher.Invoke(() =>
            {
                window.Show();
                window.WindowState = WindowState.Normal;
                window.Activate();
            }),
            _stopping.Token);

        _instance?.WhenAskedToQuit(
            () => Dispatcher.Invoke(() => Shutdown()), _stopping.Token);
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
