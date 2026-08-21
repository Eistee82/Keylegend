using Microsoft.Win32;

namespace Keylegend.Windows;

/// <summary>
/// Registers the application to start with Windows.
/// </summary>
/// <remarks>
/// Uses the per-user <c>Run</c> key rather than a scheduled task or a service: it needs no
/// elevation, is visible to the user in Task Manager's startup list, and can be removed there
/// without hunting for it. A lighting utility has no business installing anything harder to
/// find than that.
/// </remarks>
public static class Autostart
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "Keylegend";

    /// <summary>
    /// The switch the startup entry carries, telling the application to come up in the
    /// notification area instead of opening its window.
    /// </summary>
    /// <remarks>
    /// A window in the face at every logon is the fastest way to have a background utility
    /// switched off again. The state is carried in the command line rather than in the settings
    /// file, so that a manual start always shows the window — that is what the user asked for by
    /// double-clicking — while the logon start never does.
    /// </remarks>
    public const string MinimisedSwitch = "--minimized";

    // Written in one spelling, accepted in both: the code around it is in British English, and
    // someone adding the switch to a shortcut by hand will type whichever they are used to.
    private static readonly string[] MinimisedSwitches = [MinimisedSwitch, "--minimised"];

    /// <summary>Whether the application is registered to start with Windows.</summary>
    public static bool IsEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKey);

            return key?.GetValue(ValueName) is string value && !string.IsNullOrWhiteSpace(value);
        }
        catch (Exception ex) when (ex is System.Security.SecurityException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    /// <summary>
    /// Whether these command line arguments ask for a start without a window.
    /// </summary>
    public static bool StartsMinimised(IEnumerable<string>? arguments)
        => arguments is not null
           && arguments.Any(argument => MinimisedSwitches.Contains(
               argument.Trim(), StringComparer.OrdinalIgnoreCase));

    /// <summary>
    /// Turns autostart on or off.
    /// </summary>
    /// <param name="executablePath">Full path to the executable to register.</param>
    /// <returns>An explanation if it failed, otherwise <c>null</c>.</returns>
    public static string? Set(bool enabled, string executablePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(executablePath);

        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(RunKey, writable: true);

            if (key is null)
            {
                return "The Windows startup list could not be opened.";
            }

            if (enabled)
            {
                key.SetValue(ValueName, CommandLineFor(executablePath));
            }
            else
            {
                key.DeleteValue(ValueName, throwOnMissingValue: false);
            }

            return null;
        }
        catch (Exception ex) when (ex is System.Security.SecurityException
                                      or UnauthorizedAccessException
                                      or IOException)
        {
            return $"Autostart could not be changed: {ex.Message}";
        }
    }

    /// <summary>
    /// Brings an existing startup entry up to date with what <see cref="Set"/> writes today.
    /// </summary>
    /// <remarks>
    /// Entries written by earlier versions name the executable and nothing else, and would keep
    /// opening the window at every logon until autostart was switched off and on again. Only an
    /// entry pointing at this very executable is touched: one pointing somewhere else was either
    /// left behind by another copy or edited on purpose, and neither is ours to overwrite.
    /// </remarks>
    public static void Refresh(string? executablePath)
    {
        if (string.IsNullOrWhiteSpace(executablePath))
        {
            return;
        }

        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: true);

            if (key?.GetValue(ValueName) is not string value || string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            var wanted = CommandLineFor(executablePath);

            if (value.Equals(wanted, StringComparison.OrdinalIgnoreCase)
                || !ExecutableIn(value).Equals(executablePath, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            key.SetValue(ValueName, wanted);
        }
        catch (Exception ex) when (ex is System.Security.SecurityException
                                      or UnauthorizedAccessException
                                      or IOException)
        {
            // An unwritable startup list costs the minimised start, not the program.
        }
    }

    // Quoted: the path routinely contains spaces, and an unquoted value would be parsed as a
    // command with arguments.
    private static string CommandLineFor(string executablePath)
        => $"\"{executablePath}\" {MinimisedSwitch}";

    /// <summary>The program named by a <c>Run</c> value, with quotes and arguments stripped.</summary>
    private static string ExecutableIn(string value)
    {
        var trimmed = value.Trim();

        if (trimmed.StartsWith('"'))
        {
            var closing = trimmed.IndexOf('"', 1);

            return closing > 0 ? trimmed[1..closing] : trimmed.Trim('"');
        }

        var space = trimmed.IndexOf(' ');

        return space > 0 ? trimmed[..space] : trimmed;
    }
}
