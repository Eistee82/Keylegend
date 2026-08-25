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
        // with no window and no balloon in the way of whatever the user is doing.
        var minimised = Autostart.StartsMinimised(e.Args);

        // The lighting, not the window, is the program. Without this a start with no window
        // would count as "no windows left" and end the process before it ever lit anything.
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        DeviceProfile profile;
        try
        {
            // Ask the lighting service what is actually plugged in. It knows the model by name,
            // states the physical layout outright, and lists the keys the hardware really has —
            // all three of which the program used to infer. Where it answers, the profile is
            // built for that keyboard rather than chosen for a guessed one.
            profile = FromAttachedDevice()
                ?? throw new DeviceProfileException(Texts.Get("StartupNoKeyboard"));

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

        static DeviceProfile? FromAttachedDevice()
        {
            // Without the vendor's software there is nothing to read, and nothing to fall back
            // on either: the keyboard it describes and the drawing it keeps are where the profile
            // comes from. The caller says so and stops.
            var attached = SdkDeviceDescription.ReadAll().FirstOrDefault();

            if (attached is null || attached.Keys.Count == 0)
            {
                return null;
            }

            // The vendor's own drawing of the attached model. Everything a profile used to carry
            // is in it: the keys and their names, their real sizes, the casing, and the legends
            // printed on the caps in the right language. What it does not carry — which cell each
            // key lights — is a constant of the protocol.
            var drawing = SvgLayoutSource.Find(attached);

            if (drawing is not null
                && AttachedDeviceProfile.FromDrawing(attached, drawing) is { } drawn)
            {
                return drawn;
            }

            return null;
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
