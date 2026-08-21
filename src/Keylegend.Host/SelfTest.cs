using Keylegend.Chroma;
using Keylegend.Core.Devices;
using Keylegend.Core.Lighting;

namespace Keylegend.Host;

/// <summary>
/// Proves — or disproves — that this program can drive the keyboard at all, by painting
/// unmistakable full-keyboard colours for long enough to be seen, and reporting exactly what
/// the service replied at every step.
/// </summary>
internal static class SelfTest
{
    public static async Task RunAsync(
        DeviceProfile profile,
        IChromaClient chroma,
        CancellationToken cancellationToken)
    {
        var seconds = double.TryParse(HoldSeconds(), out var given) && given > 0 ? given : 5.0;

        static string? HoldSeconds()
        {
            var args = Environment.GetCommandLineArgs();
            var index = Array.FindIndex(args, a => a.Equals("--selftest", StringComparison.OrdinalIgnoreCase));

            return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
        }

        Console.WriteLine("=== Self test ===");
        Console.WriteLine("Watch the keyboard. Each colour is held for " +
                          $"{seconds:N0} seconds across every mapped key.");
        Console.WriteLine();

        Console.Write("Connecting… ");
        await chroma.ConnectAsync(cancellationToken);
        Console.WriteLine("session acquired.");
        Console.WriteLine();

        var steps = new (string Name, RgbColor Colour)[]
        {
            ("WHITE — every key at full brightness", new RgbColor(255, 255, 255)),
            ("RED", new RgbColor(255, 0, 0)),
            ("GREEN", new RgbColor(0, 255, 0)),
            ("BLUE", new RgbColor(0, 0, 255))
        };

        var frame = new LedFrame(profile.Matrix.Rows, profile.Matrix.Columns);

        foreach (var (name, colour) in steps)
        {
            frame.Clear();
            var painted = 0;

            foreach (var key in profile.Keys)
            {
                if (key.Row is { } row && key.Column is { } column)
                {
                    frame.Set(row, column, colour);
                    painted++;
                }
            }

            Console.WriteLine($"  {name}  ({painted} keys, BGR 0x{colour.ToBgr():X6})");
            await HoldAsync(chroma, frame, TimeSpan.FromSeconds(seconds), cancellationToken);
        }

        // Every cell of the matrix, including those no key claims. If the coloured steps above
        // showed nothing but this one does, the matrix mapping is wrong rather than the transport.
        Console.WriteLine("  YELLOW — every matrix cell, mapped or not");
        frame.Clear();
        for (var row = 0; row < frame.Rows; row++)
        {
            for (var column = 0; column < frame.Columns; column++)
            {
                frame.Set(row, column, new RgbColor(255, 220, 0));
            }
        }

        await HoldAsync(chroma, frame, TimeSpan.FromSeconds(seconds), cancellationToken);

        Console.WriteLine();
        Console.WriteLine("Self test finished. Releasing the lighting.");
    }

    /// <summary>
    /// Shows a frame for a while, resending it as it goes. Sending once is not enough:
    /// Chroma discards frames while taking control from the vendor software and still reports
    /// success, so the first frame of a session disappears without a trace.
    /// </summary>
    internal static async Task HoldAsync(
        IChromaClient chroma,
        LedFrame frame,
        TimeSpan duration,
        CancellationToken cancellationToken)
    {
        var until = DateTimeOffset.UtcNow + duration;

        while (DateTimeOffset.UtcNow < until)
        {
            await chroma.SendAsync(frame, cancellationToken);
            await Task.Delay(150, cancellationToken);
        }
    }
}
