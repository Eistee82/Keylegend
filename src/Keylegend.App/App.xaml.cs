using System.Net.Http;
using System.Windows;
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

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Started by Windows at logon rather than by hand: come up in the notification area,
        // with no window and no balloon in the way of whatever the user is doing.
        var minimised = Autostart.StartsMinimised(e.Args);

        // The lighting, not the window, is the program. Without this a start with no window
        // would count as "no windows left" and end the process before it ever lit anything.
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        DeviceProfile profile;
        try
        {
            // Ask Windows what is plugged in first. With one profile shipped this made no
            // difference; with thirty-two it is the difference between lighting the keyboard on
            // the desk and lighting whichever profile happened to sort first.
            var path = DeviceProfileLocator.FindDefault(
                    ConnectedKeyboards.Detect(),
                    ConnectedKeyboards.SuggestPhysicalLayout())
                ?? throw new DeviceProfileException(
                    "No device profile found. Expected a devices folder next to the application.");

            profile = DeviceProfileLoader.Load(path);

            var problems = DeviceProfileValidator.Validate(profile);
            if (problems.Count > 0)
            {
                throw new DeviceProfileException(
                    "The device profile has problems:" + Environment.NewLine + "  " +
                    string.Join(Environment.NewLine + "  ", problems));
            }
        }
        catch (DeviceProfileException ex)
        {
            MessageBox.Show(ex.Message, "Keylegend", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
            return;
        }

        var resolver = new WindowsKeyResolver();
        resolver.RefreshLayout();

        _http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
        _chroma = new ChromaClient(_http, new ChromaOptions());

        var foreground = new ForegroundWatcher();

        _engine = new LightingEngine(
            profile,
            _chroma,
            new WindowsKeyStateSource(profile),
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
        _stopping.Dispose();

        base.OnExit(e);
    }
}
