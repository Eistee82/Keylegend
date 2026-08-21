using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Keylegend.Windows;

/// <summary>What is currently in front of the user.</summary>
/// <param name="ProcessName">Executable name without extension, lower case, or empty if unknown.</param>
/// <param name="WindowTitle">Title of the foreground window, or empty.</param>
/// <param name="LooksLikeGame">Whether this appears to be a game or other full-screen application.</param>
public readonly record struct ForegroundApp(string ProcessName, string WindowTitle, bool LooksLikeGame)
{
    public static ForegroundApp None { get; } = new(string.Empty, string.Empty, false);
}

/// <summary>
/// Reports which application is in front, and whether it looks like a game.
/// </summary>
/// <remarks>
/// Two signals are combined, because neither alone is sufficient.
/// <para>
/// Windows itself tracks whether a Direct3D application has the screen, in order to suppress
/// notifications during games — <c>SHQueryUserNotificationState</c> exposes that, and it is the
/// closest thing to an official answer.
/// </para>
/// <para>
/// It misses borderless-windowed games, which is how most modern titles run, so a window
/// filling an entire monitor counts as well. Deliberately not attempted: a list of known game
/// executables, which is out of date the moment it is written.
/// </para>
/// </remarks>
public sealed class ForegroundWatcher
{
    /// <summary>A Direct3D application owns the screen — Windows' own "a game is running".</summary>
    private const int QUNS_RUNNING_D3D_FULL_SCREEN = 3;

    /// <summary>Presentation mode; treated the same, since the user is equally busy.</summary>
    private const int QUNS_PRESENTATION_MODE = 4;

    private IntPtr _lastWindow;
    private ForegroundApp _last = ForegroundApp.None;

    /// <summary>
    /// Reads the current foreground application. Cheap enough to call in the polling loop:
    /// the expensive process lookup only happens when the foreground window changes.
    /// </summary>
    public ForegroundApp Read()
    {
        var window = NativeMethods.GetForegroundWindow();

        if (window == IntPtr.Zero)
        {
            return ForegroundApp.None;
        }

        // The fullscreen check is cheap and its answer can change without the window changing,
        // so it is evaluated every time; the process name is cached per window.
        var fullScreen = IsFullScreen(window) || IsDirect3DFullScreen();

        if (window == _lastWindow)
        {
            return _last with { LooksLikeGame = fullScreen };
        }

        _lastWindow = window;
        _last = new ForegroundApp(ProcessNameOf(window), TitleOf(window), fullScreen);

        return _last;
    }

    private static bool IsDirect3DFullScreen()
    {
        return NativeMethods.SHQueryUserNotificationState(out var state) == 0
            && state is QUNS_RUNNING_D3D_FULL_SCREEN or QUNS_PRESENTATION_MODE;
    }

    /// <summary>
    /// Whether the window covers a whole monitor. Catches borderless-windowed games, which the
    /// Direct3D check does not see.
    /// </summary>
    private static bool IsFullScreen(IntPtr window)
    {
        if (window == NativeMethods.GetDesktopWindow() || window == NativeMethods.GetShellWindow())
        {
            return false;
        }

        if (!NativeMethods.GetWindowRect(window, out var bounds))
        {
            return false;
        }

        var monitor = NativeMethods.MonitorFromWindow(window, NativeMethods.MONITOR_DEFAULTTONEAREST);
        var info = new NativeMethods.MonitorInfo { Size = Marshal.SizeOf<NativeMethods.MonitorInfo>() };

        if (!NativeMethods.GetMonitorInfo(monitor, ref info))
        {
            return false;
        }

        // Exact equality would miss windows that overshoot the screen edge by a pixel, which
        // some games do.
        const int slack = 2;

        return bounds.Left <= info.Monitor.Left + slack
            && bounds.Top <= info.Monitor.Top + slack
            && bounds.Right >= info.Monitor.Right - slack
            && bounds.Bottom >= info.Monitor.Bottom - slack;
    }

    private static string ProcessNameOf(IntPtr window)
    {
        try
        {
            NativeMethods.GetWindowThreadProcessId(window, out var processId);

            if (processId == 0)
            {
                return string.Empty;
            }

            using var process = Process.GetProcessById((int)processId);

            return process.ProcessName.ToLowerInvariant();
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            // The process ended between the two calls, or is not ours to inspect.
            return string.Empty;
        }
    }

    private static string TitleOf(IntPtr window)
    {
        var length = NativeMethods.GetWindowTextLength(window);

        if (length <= 0)
        {
            return string.Empty;
        }

        var buffer = new System.Text.StringBuilder(length + 1);
        NativeMethods.GetWindowText(window, buffer, buffer.Capacity);

        return buffer.ToString();
    }
}
