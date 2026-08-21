using Keylegend.Chroma;
using Keylegend.Core.Devices;
using Keylegend.Core.Input;
using Keylegend.Core.Lighting;
using Keylegend.Windows;

namespace Keylegend.Host;

/// <summary>
/// Lights one key at a time and names it, so a device profile's LED mapping can be confirmed
/// against real hardware. This is the only way to be certain a mapping is right — everything
/// else is inference from a vendor table.
/// </summary>
internal static class Calibration
{
    public static async Task RunAsync(
        DeviceProfile profile,
        IChromaClient chroma,
        CancellationToken cancellationToken)
    {
        var mapped = profile.Keys
            .Where(k => k.Row.HasValue && k.Column.HasValue)
            .OrderBy(k => k.Y)
            .ThenBy(k => k.X)
            .ToArray();

        var wrong = new List<(KeyDefinition Key, string Note)>();

        // Written as we go, not at the end. Calibration is patient work, and losing it
        // because a window was closed would be inexcusable - as it already proved to be once.
        var logPath = Path.Combine(
            Path.GetDirectoryName(Environment.ProcessPath) ?? ".",
            "calibration-findings.txt");
        await File.WriteAllTextAsync(
            logPath,
            $"Calibration of {profile.Name} ({profile.PhysicalLayout}){Environment.NewLine}" +
            $"Findings are appended live; this file survives closing the window.{Environment.NewLine}{Environment.NewLine}",
            cancellationToken);

        // Key identifiers follow the US layout by convention, so on a German keyboard
        // "Keyboard_MinusAndUnderscore" is the eszett key. Showing what the key actually types
        // on this machine is the difference between a usable calibration and one where the
        // user has to translate every name in their head.
        var resolver = new WindowsKeyResolver();
        resolver.RefreshLayout();
        var typing = new KeyboardState(ModifierKeys.None, new LockStates(true, false, false));

        Console.WriteLine($"Calibration — {mapped.Length} mapped keys, in reading order.");
        Console.WriteLine();
        Console.WriteLine("  Enter / →   this key is correct, go to the next");
        Console.WriteLine("  F           wrong key lit up — record it");
        Console.WriteLine("  ←           back one key");
        Console.WriteLine("  A           light every mapped key at once");
        Console.WriteLine("  S           skip to the summary");
        Console.WriteLine("  Q / Esc     stop");
        Console.WriteLine();
        Console.WriteLine("The key named below should be the one lighting up white.");
        Console.WriteLine();

        await chroma.ConnectAsync(cancellationToken);

        var frame = new LedFrame(profile.Matrix.Rows, profile.Matrix.Columns);
        var index = 0;
        var confirmed = 0;

        while (index < mapped.Length && !cancellationToken.IsCancellationRequested)
        {
            var key = mapped[index];

            frame.Clear();
            frame.Set(key.Row!.Value, key.Column!.Value, new RgbColor(255, 255, 255));

            Console.Write(
                $"\r[{index + 1,3}/{mapped.Length}] {Describe(key, resolver, typing),-32} " +
                $"({key.Id,-34} row {key.Row}, col {key.Column})    ");

            var pressed = await HoldAsync(chroma, frame, cancellationToken);

            switch (pressed.Key)
            {
                case ConsoleKey.Q or ConsoleKey.Escape or ConsoleKey.S:
                    Summarise(wrong, confirmed, mapped.Length, stopped: true, logPath);
                    return;

                case ConsoleKey.F:
                    Console.WriteLine();
                    Console.Write("   Which key actually lit up? (name it, or press Enter for 'none') ");
                    var note = Console.ReadLine() ?? string.Empty;
                    var finding = string.IsNullOrWhiteSpace(note) ? "nothing lit up" : note.Trim();
                    wrong.Add((key, finding));

                    await File.AppendAllTextAsync(
                        logPath,
                        $"{key.Id}\trow {key.Row}, col {key.Column}\t{finding}{Environment.NewLine}",
                        cancellationToken);

                    index++;
                    break;

                case ConsoleKey.LeftArrow or ConsoleKey.Backspace:
                    index = Math.Max(0, index - 1);
                    confirmed = Math.Max(0, confirmed - 1);
                    break;

                case ConsoleKey.A:
                    await LightEverythingAsync(profile, chroma, frame, cancellationToken);
                    break;

                default:
                    confirmed++;
                    index++;
                    break;
            }
        }

        Summarise(wrong, confirmed, mapped.Length, stopped: false, logPath);
    }

    /// <summary>
    /// Describes a key the way the person in front of the keyboard sees it: by the character
    /// it types on this machine where there is one, and by a readable name otherwise.
    /// </summary>
    private static string Describe(KeyDefinition key, IKeyResolver resolver, KeyboardState state)
    {
        var meaning = resolver.Resolve(key.Id, key.ScanCode, state);

        if (!string.IsNullOrEmpty(meaning.Character) && !char.IsControl(meaning.Character[0]))
        {
            return $"the  {meaning.Character}  key";
        }

        // No printable character - fall back to a tidied-up identifier.
        var name = key.Id.StartsWith("Keyboard_", StringComparison.Ordinal)
            ? key.Id["Keyboard_".Length..]
            : key.Id;

        return name switch
        {
            "LeftGui" => "left Windows",
            "RightGui" => "right Windows / fn",
            "Application" => "context menu",
            "GraveAccentAndTilde" => "the  ^  key",
            "NonUsBackslash" => "the  <  key",
            "NonUsTilde" => "the  #  key",
            _ => name
        };
    }

    /// <summary>
    /// Shows a frame and keeps showing it until a key is pressed.
    /// </summary>
    /// <remarks>
    /// Sending once and then waiting does not work: Chroma discards frames while it is taking
    /// control from the vendor software, and reports success for them, so the very first frame
    /// of a session reliably disappears. During calibration that is worse than elsewhere,
    /// because a key that silently failed to light would be recorded as a wrong mapping.
    /// </remarks>
    private static async Task<ConsoleKeyInfo> HoldAsync(
        IChromaClient chroma,
        LedFrame frame,
        CancellationToken cancellationToken)
    {
        while (!Console.KeyAvailable)
        {
            await chroma.SendAsync(frame, cancellationToken);
            await Task.Delay(150, cancellationToken);
        }

        return Console.ReadKey(intercept: true);
    }

    private static void Summarise(
        List<(KeyDefinition Key, string Note)> wrong,
        int confirmed,
        int total,
        bool stopped,
        string logPath)
    {
        Console.WriteLine();
        Console.WriteLine();
        Console.WriteLine("=== Summary ===");
        Console.WriteLine($"  checked and correct : {confirmed} of {total}");
        Console.WriteLine($"  reported wrong      : {wrong.Count}");

        if (wrong.Count > 0)
        {
            Console.WriteLine();
            Console.WriteLine("These need their row/column corrected in device.json:");

            foreach (var (key, note) in wrong)
            {
                Console.WriteLine($"  {key.Id,-38} row {key.Row}, col {key.Column}   → {note}");
            }
        }
        else if (!stopped)
        {
            Console.WriteLine();
            Console.WriteLine("Every key matched. Set \"verified\": true in the device profile.");
        }

        if (stopped)
        {
            Console.WriteLine();
            Console.WriteLine("Stopped early — the remaining keys were not checked.");
        }

        Console.WriteLine();
        Console.WriteLine($"Findings were written to:{Environment.NewLine}  {logPath}");
        Console.WriteLine("They are safe there even if this window is closed.");
        Console.WriteLine();
        Console.WriteLine("Press any key to close…");
        Console.ReadKey(intercept: true);
    }

    /// <summary>
    /// Lights every mapped cell at once. A dark key here means the profile is missing it —
    /// often the fastest way to spot an incomplete mapping.
    /// </summary>
    private static async Task LightEverythingAsync(
        DeviceProfile profile,
        IChromaClient chroma,
        LedFrame frame,
        CancellationToken cancellationToken)
    {
        frame.Clear();

        foreach (var key in profile.Keys)
        {
            if (key.Row is { } row && key.Column is { } column)
            {
                frame.Set(row, column, new RgbColor(0, 120, 255));
            }
        }

        Console.Write("\r   All mapped keys lit — any dark key is missing from the profile. Press a key…    ");
        await HoldAsync(chroma, frame, cancellationToken);
    }
}
