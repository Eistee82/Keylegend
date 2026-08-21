using Keylegend.Chroma;
using Keylegend.Core.Devices;
using Keylegend.Core.Input;
using Keylegend.Core.Lighting;
using Keylegend.Core.Session;
using Keylegend.Core.Shortcuts;
using Keylegend.Host;
using Keylegend.Engine;
using Keylegend.Windows;

var profilePath = ArgumentValue("--profile")
    ?? DeviceProfileLocator.FindDefault(
        ConnectedKeyboards.Detect(),
        ConnectedKeyboards.SuggestPhysicalLayout())
    ?? Abort("No device profile found. Pass one with --profile <path to device.json>.");

DeviceProfile profile;
try
{
    profile = DeviceProfileLoader.Load(profilePath);
}
catch (DeviceProfileException ex)
{
    return Fail(ex.Message);
}

var problems = DeviceProfileValidator.Validate(profile);
if (problems.Count > 0)
{
    return Fail("The device profile has problems:" + Environment.NewLine + "  " +
                string.Join(Environment.NewLine + "  ", problems));
}

Console.WriteLine($"Keylegend — {profile.Name} ({profile.PhysicalLayout}), {profile.Keys.Count} keys");
if (ArgumentValue("--profile") is null)
{
    var attached = ConnectedKeyboards.Detect();
    Console.WriteLine(
        profile.Usb is not null && attached.Any(id => profile.Usb.Matches(id))
            ? $"Recognised by USB id {profile.Usb.VendorId}:{profile.Usb.ProductId}."
            : "Not recognised by USB id — chosen as the best available profile. " +
              "Pass --profile to override.");
}
if (!profile.Verified)
{
    Console.WriteLine("Note: this profile's LED mapping has not been confirmed on hardware yet.");
    Console.WriteLine("      Run with --calibrate to check it.");
}
Console.WriteLine();

using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
await using var chroma = new ChromaClient(http, new ChromaOptions());

using var stopping = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;      // release the lighting before leaving, rather than freezing a frame
    stopping.Cancel();
};

try
{
    if (HasFlag("--calibrate"))
    {
        await Calibration.RunAsync(profile, chroma, stopping.Token);
    }
    else if (HasFlag("--dump-layout"))
    {
        LayoutDump.Run(profile);
    }
    else if (HasFlag("--once"))
    {
        await ShowOnceAsync(profile, chroma, stopping.Token);
    }
    else if (HasFlag("--selftest"))
    {
        await SelfTest.RunAsync(profile, chroma, stopping.Token);
    }
    else if (HasFlag("--watch-foreground"))
    {
        await ForegroundProbe.RunAsync(stopping.Token);
    }
    else
    {
        await RunLightingAsync(profile, chroma, IdleTimeout(), stopping.Token);
    }
}
catch (OperationCanceledException)
{
    // Ctrl+C - the finally below still runs.
}
catch (ChromaException ex)
{
    return Fail(ex.Message);
}
finally
{
    Console.WriteLine();
    Console.WriteLine("Releasing the lighting…");
    await chroma.DisconnectAsync(CancellationToken.None);
}

return 0;

static async Task RunLightingAsync(
    DeviceProfile profile,
    IChromaClient chroma,
    TimeSpan idleTimeout,
    CancellationToken cancellationToken)
{
    // The loop itself lives in Keylegend.Engine so that this console driver and the window run
    // identical code. Everything here is presentation.
    var resolver = new WindowsKeyResolver();
    resolver.RefreshLayout();

    var foreground = new ForegroundWatcher();

    var engine = new LightingEngine(
        profile, chroma, new WindowsKeyStateSource(profile), resolver,
        clock: null,
        foreground: () =>
        {
            var app = foreground.Read();

            return new Keylegend.Core.Profiles.ForegroundContext(
                app.ProcessName, app.WindowTitle, app.LooksLikeGame);
        })
    {
        Settings = new EngineSettings { IdleTimeout = idleTimeout }
    };

    engine.ProfileChanged += p =>
        Console.WriteLine($"  [{DateTime.Now:HH:mm:ss}] profile: {p?.Name ?? "default"}");

    var verbose = Environment.GetCommandLineArgs()
        .Any(a => a.Equals("--verbose", StringComparison.OrdinalIgnoreCase));
    var frames = 0;

    engine.StateChanged += state =>
    {
        Console.WriteLine($"  [{DateTime.Now:HH:mm:ss.fff}] {state}");

        if (verbose && state == LightingState.Idle && frames > 0)
        {
            Console.WriteLine($"  released after {frames} frames");
            frames = 0;
        }
    };

    engine.FrameComposed += _ => frames++;
    engine.Warning += message => Console.WriteLine($"  {message}");

    Console.WriteLine($"Running. Idle timeout {idleTimeout.TotalSeconds:N0} s — press a key to take over.");
    Console.WriteLine("Press Ctrl+C to stop.");
    Console.WriteLine();

    await engine.RunAsync(cancellationToken);
}

/// <summary>
/// Paints the current state once and holds it, then releases. Useful for seeing the result
/// without having to keep typing, and for confirming that the lighting works at all.
/// </summary>
static async Task ShowOnceAsync(
    DeviceProfile profile,
    IChromaClient chroma,
    CancellationToken cancellationToken)
{
    var seconds = double.TryParse(ArgumentValue("--once"), out var given) && given > 0 ? given : 8.0;

    var resolver = new WindowsKeyResolver();
    resolver.RefreshLayout();

    var composer = new FrameComposer(profile, resolver);
    var frame = composer.CreateFrame();
    var state = new KeyboardStateReader().Read();

    composer.Compose(frame, state, ColourScheme.Default, DefaultShortcuts.Create());

    await chroma.ConnectAsync(cancellationToken);

    var lit = 0;
    for (var row = 0; row < frame.Rows; row++)
    {
        for (var column = 0; column < frame.Columns; column++)
        {
            if (frame[row, column] != RgbColor.Off)
            {
                lit++;
            }
        }
    }

    Console.WriteLine($"Painted {lit} keys. Num Lock {(state.Locks.NumLock ? "on" : "off")}, " +
                      $"Caps Lock {(state.Locks.CapsLock ? "on" : "off")}.");
    Console.WriteLine($"Holding for {seconds:N0} s, then handing back to Chroma Studio…");

    await SelfTest.HoldAsync(chroma, frame, TimeSpan.FromSeconds(seconds), cancellationToken);
}

static TimeSpan IdleTimeout()
{
    var value = ArgumentValue("--idle");

    // Sixty seconds by default. Taking the lighting back from the vendor effect costs one to
    // two seconds, so a short timeout makes that cost a frequent nuisance; an hour of typing
    // with normal pauses now stays inside a single session.
    return value is not null && double.TryParse(value, out var seconds) && seconds > 0
        ? TimeSpan.FromSeconds(seconds)
        : TimeSpan.FromSeconds(60);
}

static bool HasFlag(string name) =>
    Environment.GetCommandLineArgs().Any(a => string.Equals(a, name, StringComparison.OrdinalIgnoreCase));

static string? ArgumentValue(string name)
{
    var args = Environment.GetCommandLineArgs();
    var index = Array.FindIndex(args, a => string.Equals(a, name, StringComparison.OrdinalIgnoreCase));

    return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
}

/// <summary>Prints the reason and returns the process exit code.</summary>
static int Fail(string message)
{
    Console.Error.WriteLine(message);

    return 1;
}

/// <summary>Prints the reason and stops, for use where a value was required.</summary>
static string Abort(string message)
{
    Console.Error.WriteLine(message);
    Environment.Exit(1);

    return string.Empty;
}
