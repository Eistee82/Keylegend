using Keylegend.Core.Profiles;
using Keylegend.Windows;

namespace Keylegend.Host;

/// <summary>
/// Reports what the foreground watcher sees, so the game detection can be checked against real
/// applications rather than assumed to work.
/// </summary>
internal static class ForegroundProbe
{
    public static async Task RunAsync(CancellationToken cancellationToken)
    {
        var watcher = new ForegroundWatcher();
        var profiles = ShippedProfiles.Create();

        Console.WriteLine("Watching the foreground application. Switch windows, start a game,");
        Console.WriteLine("alt-tab out of it — every change is reported below.");
        Console.WriteLine("Press Ctrl+C to stop.");
        Console.WriteLine();
        Console.WriteLine($"{"process",-24} {"game?",-7} {"profile",-12} title");
        Console.WriteLine(new string('-', 92));

        var last = (Process: string.Empty, Game: false);

        while (!cancellationToken.IsCancellationRequested)
        {
            var app = watcher.Read();

            if ((app.ProcessName, app.LooksLikeGame) != last)
            {
                last = (app.ProcessName, app.LooksLikeGame);

                var context = new ForegroundContext(app.ProcessName, app.WindowTitle, app.LooksLikeGame);
                var profile = profiles.Select(context);

                Console.WriteLine(
                    $"{Trim(app.ProcessName, 24),-24} " +
                    $"{(app.LooksLikeGame ? "YES" : "no"),-7} " +
                    $"{Trim(profile?.Name ?? "—", 12),-12} " +
                    $"{Trim(app.WindowTitle, 40)}");
            }

            await Task.Delay(250, cancellationToken);
        }
    }

    private static string Trim(string value, int length)
        => value.Length <= length ? value : value[..(length - 1)] + "…";
}
