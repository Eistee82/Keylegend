using System.Diagnostics;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace Keylegend.Windows;

/// <summary>
/// Arranges that one copy of the program drives the keyboard, and decides what a second start
/// does about it.
/// </summary>
/// <remarks>
/// <para>
/// Two copies at once is not a cosmetic problem. Both open a Chroma session for the same
/// keyboard, and the service hands the keyboard to one of them; the other keeps sending frames
/// that go nowhere and keeps reporting success. What the user sees is lighting that answers
/// intermittently, or not at all, with no window saying why.
/// </para>
/// <para>
/// What a second start does depends on <em>what</em> is already running, because the two cases
/// want opposite things:
/// </para>
/// <list type="bullet">
/// <item><description>
/// <b>The very same program, from the same place.</b> Somebody double-clicked the icon while it
/// was sitting in the notification area — they want the window, not a restart. So the running
/// copy is asked to show itself and this start bows out. Nothing is killed, no session changes
/// hands, and the lighting does not blink.
/// </description></item>
/// <item><description>
/// <b>Anything else</b> — an older version, or the same version from another folder, such as an
/// installed copy while a build runs from a working directory. Then the newest start is the one
/// the user means, and it takes over.
/// </description></item>
/// </list>
/// <para>
/// Taking over is done gently first. The running copy is <em>asked</em> to leave before it is
/// made to: it holds the Chroma session, and handing that back is what lets the vendor effect
/// resume instead of the keyboard freezing on the last frame it was sent. It may also be part-way
/// through writing the settings file. So the order is: ask, wait, and only then insist.
/// </para>
/// <para>
/// Named kernel objects do the arbitration rather than a lock file, because they cannot be left
/// behind: Windows releases them when the process ends, however it ends. A copy that crashed
/// therefore blocks nothing. They are per logon session (<c>Local\</c>), so two users switched
/// between each get their own copy — which is what they should get, since each has their own
/// settings.
/// </para>
/// <para>
/// <b>Claim and dispose on the same thread.</b> The claim is a mutex, and a mutex belongs to the
/// thread that took it: only that thread can release it. In this application both happen on the
/// UI thread, in <c>OnStartup</c> and <c>OnExit</c>. Releasing from elsewhere does not corrupt
/// anything — the handle closes and Windows frees the mutex when the process ends — but the claim
/// then stays held for the rest of the run, and a newer copy would have to wait out its patience
/// and force the issue for no reason.
/// </para>
/// </remarks>
public sealed class SingleInstance : IDisposable
{
    private readonly Mutex? _held;
    private readonly EventWaitHandle? _quit;
    private readonly EventWaitHandle _show;
    private bool _released;

    private SingleInstance(
        Mutex? held, EventWaitHandle? quit, EventWaitHandle show,
        bool owns, bool replaced, bool forced)
    {
        _held = held;
        _quit = quit;
        _show = show;
        Owns = owns;
        ReplacedAnother = replaced;
        HadToForce = forced;
    }

    /// <summary>
    /// Whether this copy may run. When <c>false</c>, an identical copy is already running and has
    /// been left in place — this one should quit without touching the keyboard.
    /// </summary>
    public bool Owns { get; }

    /// <summary>Whether a copy that was running gave way to this one.</summary>
    public bool ReplacedAnother { get; }

    /// <summary>
    /// Whether the copy that gave way had to be ended outright because it did not answer.
    /// </summary>
    /// <remarks>
    /// Worth reporting rather than hiding: it means a Chroma session was not handed back, so the
    /// keyboard may sit on a stale frame until this copy takes over.
    /// </remarks>
    public bool HadToForce { get; }

    /// <summary>
    /// Settles whether this copy runs, replacing an unlike one or standing down for an
    /// identical one.
    /// </summary>
    /// <param name="name">
    /// Identifies the program to the operating system. Shared by every copy whatever its version
    /// or location, because copies that cannot see each other cannot arbitrate at all.
    /// </param>
    /// <param name="patience">
    /// How long a copy being replaced is given to leave of its own accord. It has a Chroma session
    /// to hand back, which takes a moment; two seconds is comfortably more than that and short
    /// enough not to look like a program that failed to start.
    /// </param>
    /// <param name="identity">
    /// What counts as "the same program". Defaults to this executable's path and version, which
    /// is what makes an installed copy and a copy built in a working directory two different
    /// programs — and an update at the same path different from the version it replaced.
    /// </param>
    public static SingleInstance Claim(
        string name, TimeSpan? patience = null, string? identity = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var wait = patience ?? TimeSpan.FromSeconds(2);
        var who = identity ?? IdentityOfThisCopy();

        // Two objects, and the difference between them is the whole design. The mutex is shared
        // by every copy, whatever its version or path, so that unlike ones can see each other at
        // all. The "show" event carries the identity in its name, so whether it had to be created
        // answers the question the mutex cannot: is what is running the same program as me? A
        // copy keeps its own open for as long as it runs, so finding one already there means one
        // is running. Surer than reading the process list, and it needs no permission to look
        // inside somebody else's process.
        var show = new EventWaitHandle(
            false, EventResetMode.AutoReset, $@"Local\{name}.show.{who}", out var noneLikeUs);

        var quit = new EventWaitHandle(false, EventResetMode.AutoReset, $@"Local\{name}.quit");
        var held = new Mutex(false, $@"Local\{name}");

        if (Take(held, TimeSpan.Zero))
        {
            return new SingleInstance(held, quit, show, owns: true, replaced: false, forced: false);
        }

        // Something else holds the keyboard, and it is this same program: the window is what this
        // start is really asking for, so nothing is replaced and nothing blinks.
        if (!noneLikeUs)
        {
            held.Dispose();
            quit.Dispose();

            return new SingleInstance(
                null, null, show, owns: false, replaced: false, forced: false);
        }

        // Not us: an older version, or the same one from somewhere else. Ask it to go.
        quit.Set();

        if (Take(held, wait))
        {
            return new SingleInstance(held, quit, show, owns: true, replaced: true, forced: false);
        }

        // It did not answer — hung, or a version that does not listen. Now insist.
        var ended = EndOthers();

        // Even a killed owner releases the mutex, so this succeeds; it is reported abandoned,
        // which is exactly what happened and not an error here.
        if (!Take(held, TimeSpan.FromSeconds(5)))
        {
            show.Dispose();
            quit.Dispose();
            held.Dispose();

            throw new InvalidOperationException(
                "Another copy of the program is running and could not be replaced.");
        }

        return new SingleInstance(held, quit, show, owns: true, replaced: true, forced: ended > 0);
    }

    /// <summary>
    /// Asks the identical copy that is already running to bring its window up. Only meaningful
    /// when <see cref="Owns"/> is <c>false</c>.
    /// </summary>
    /// <remarks>
    /// Not called when this start was a logon start: nobody asked for a window then, and one
    /// appearing over whatever they are doing is exactly what the autostart switch avoids.
    /// </remarks>
    public void AskRunningCopyToShow() => _show.Set();

    /// <summary>
    /// Calls <paramref name="show"/> when another start of this same program asks for the window.
    /// Returns at once; the waiting happens on a background thread.
    /// </summary>
    public void WhenAskedToShow(Action show, CancellationToken stopping)
        => Await(_show, show, stopping);

    /// <summary>
    /// Calls <paramref name="quit"/> when a different program asks this copy to make way.
    /// Returns at once; the waiting happens on a background thread.
    /// </summary>
    public void WhenAskedToQuit(Action quit, CancellationToken stopping)
    {
        if (_quit is not null)
        {
            Await(_quit, quit, stopping);
        }
    }

    private static void Await(EventWaitHandle handle, Action then, CancellationToken stopping)
    {
        ArgumentNullException.ThrowIfNull(then);

        var thread = new Thread(() =>
        {
            try
            {
                while (true)
                {
                    // Either handle ending the wait is a reason to stop waiting: the first is the
                    // request, the second means this copy is closing anyway. The token's handle
                    // belongs to its source and is deliberately not disposed here.
                    if (WaitHandle.WaitAny([handle, stopping.WaitHandle]) != 0
                        || stopping.IsCancellationRequested)
                    {
                        return;
                    }

                    then();

                    // Round again: a window can be asked for more than once in a run.
                }
            }
            catch (ObjectDisposedException)
            {
                // Shutdown got there first. Nothing left to answer for.
            }
        })
        {
            IsBackground = true,
            Name = "Keylegend single instance",
        };

        thread.Start();
    }

    /// <summary>
    /// What counts as "the same program": this executable, at this path, at this version.
    /// </summary>
    /// <remarks>
    /// The version comes from the assembly that is loaded rather than from the file on disk,
    /// because those are not the same thing during an update: the file can already be the new
    /// version while the old one is still running out of memory. Asking the running image is what
    /// makes an update at the same path replace its predecessor instead of joining it.
    /// </remarks>
    private static string IdentityOfThisCopy()
    {
        var path = Environment.ProcessPath ?? Assembly.GetEntryAssembly()?.Location ?? "?";
        var version = Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "?";

        return Fingerprint($"{path.ToLowerInvariant()}|{version}");
    }

    /// <summary>
    /// A short, stable, filename-safe stand-in for a longer string. Kernel object names have a
    /// length limit and may not contain a backslash, which a path does.
    /// </summary>
    public static string Fingerprint(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        var digest = SHA256.HashData(Encoding.UTF8.GetBytes(text));

        return Convert.ToHexString(digest, 0, 8);
    }

    /// <summary>
    /// Ends the other copies of this same executable. Returns how many were ended.
    /// </summary>
    /// <remarks>
    /// The path is what identifies them, and checking it is not optional. The Chroma SDK runs a
    /// process of its own for every application registered with it, named after that application
    /// — so on a machine with Keylegend registered there is a second <c>Keylegend.exe</c> that
    /// belongs to Razer, and ending it cuts the very session this copy is about to open. Matching
    /// by name alone would do that on every single start.
    /// </remarks>
    private static int EndOthers()
    {
        var ours = Environment.ProcessPath;

        if (string.IsNullOrEmpty(ours))
        {
            return 0;
        }

        var ended = 0;

        foreach (var other in Own(ours, Environment.ProcessId))
        {
            try
            {
                other.Kill();
                other.WaitForExit(TimeSpan.FromSeconds(3));
                ended++;
            }
            catch (Exception)
            {
                // Gone already, or not ours to end. Either way the wait below decides.
            }
            finally
            {
                other.Dispose();
            }
        }

        return ended;
    }

    /// <summary>
    /// The running processes that are this same executable, excluding the caller.
    /// </summary>
    /// <param name="executable">Full path of the executable to match.</param>
    /// <param name="self">Process id to leave out — the caller's own.</param>
    public static IReadOnlyList<Process> Own(string executable, int self)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(executable);

        var name = Path.GetFileNameWithoutExtension(executable);
        var mine = new List<Process>();

        foreach (var candidate in Process.GetProcessesByName(name))
        {
            var keep = false;

            try
            {
                keep = candidate.Id != self
                    && string.Equals(candidate.MainModule?.FileName, executable,
                        StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception)
            {
                // A process owned by somebody else refuses to say where it came from. That
                // answers the question: not ours.
            }

            if (keep)
            {
                mine.Add(candidate);
            }
            else
            {
                candidate.Dispose();
            }
        }

        return mine;
    }

    private static bool Take(Mutex mutex, TimeSpan wait)
    {
        try
        {
            return mutex.WaitOne(wait);
        }
        catch (AbandonedMutexException)
        {
            // The holder went away without releasing it — killed, or crashed. The claim is ours
            // regardless; that is what "abandoned" means.
            return true;
        }
    }

    public void Dispose()
    {
        if (_held is not null && !_released)
        {
            try
            {
                _held.ReleaseMutex();
            }
            catch (ApplicationException)
            {
                // Not held by this thread, which happens when the claim came from an abandoned
                // mutex. Disposing below releases the handle either way.
            }

            _released = true;
        }

        _held?.Dispose();
        _quit?.Dispose();
        _show.Dispose();
    }
}
