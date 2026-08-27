using Keylegend.Core.Input;
using System.Text.Json;

namespace Keylegend.Chroma;

/// <summary>
/// One key as the lighting service describes it: which scancode it sends and where the service
/// places it in the matrix.
/// </summary>
/// <param name="Scancode">Set 1 scancode, as the service reports it.</param>
/// <param name="Extended">
/// True for the keys that carry an <c>E0</c> prefix — the arrows, the navigation block, the
/// right-hand modifiers. Without it Insert and NumPad 0 are the same number.
/// </param>
/// <param name="Row">Row the service places the key in.</param>
/// <param name="Column">Column the service places the key in.</param>
public readonly record struct SdkKey(int Scancode, bool Extended, int Row, int Column);

/// <summary>
/// What the lighting service knows about the keyboard that is plugged in.
/// </summary>
/// <remarks>
/// <para>
/// The service writes one of these per attached device and deletes it again when the device
/// goes away. It is therefore a description of <em>this</em> keyboard, not a catalogue — which
/// is exactly what is wanted here, because the program only ever lights the keyboard on the
/// desk.
/// </para>
/// <para>
/// <see cref="Keys"/> is the honest part of the file: it says which keys the hardware actually
/// has, and that is layout-specific — a German board reports a key that a US one does not. The
/// row and column it carries are the service's own view and are <em>not</em> the coordinates a
/// custom frame is addressed by; measuring at the device shows the two disagree on roughly half
/// the keys. They are kept because they order the keys, not because they can be lit by.
/// </para>
/// </remarks>
public sealed record SdkDeviceDescription(
    string ProductName,
    int VendorId,
    int ProductId,
    int LayoutId,
    int MatrixRows,
    int MatrixColumns,
    IReadOnlyList<SdkKey> Keys,
    int SilentKeys = 0)
{
    /// <summary>Where the SDK keeps its device descriptions, 64-bit and 32-bit install alike.</summary>
    public static IEnumerable<string> DefaultDirectories()
    {
        foreach (var variable in new[] { "ProgramFiles(x86)", "ProgramFiles" })
        {
            var root = Environment.GetEnvironmentVariable(variable);

            if (!string.IsNullOrEmpty(root))
            {
                yield return Path.Combine(root, "Razer Chroma SDK", "Devices");
            }
        }
    }

    /// <summary>
    /// Reads every keyboard description found, newest first. Returns an empty list when the SDK
    /// is not installed or nothing is plugged in — which the caller has to treat as "no keyboard
    /// to light", because there is nothing shipped here to fall back on.
    /// </summary>
    public static IReadOnlyList<SdkDeviceDescription> ReadAll(IEnumerable<string>? directories = null)
    {
        var found = new List<(DateTime Written, SdkDeviceDescription Device)>();

        foreach (var directory in directories ?? DefaultDirectories())
        {
            if (!Directory.Exists(directory))
            {
                continue;
            }

            foreach (var file in Directory.EnumerateFiles(directory, "*.json"))
            {
                // A file that does not parse must not hide the ones that do: the service may be
                // writing this very moment, and the next file is probably fine.
                try
                {
                    var device = Read(File.ReadAllText(file));

                    if (device is not null)
                    {
                        found.Add((File.GetLastWriteTimeUtc(file), device));
                    }
                }
                catch (Exception exception) when (exception is IOException or JsonException or UnauthorizedAccessException)
                {
                }
            }
        }

        return found
            .OrderByDescending(f => f.Written)
            .Select(f => f.Device)
            .ToArray();
    }

    /// <summary>Parses one description. Returns <c>null</c> for anything that is not a keyboard.</summary>
    public static SdkDeviceDescription? Read(string json)
    {
        ArgumentNullException.ThrowIfNull(json);

        using var document = JsonDocument.Parse(json);

        if (!document.RootElement.TryGetProperty("param", out var param)
            || !param.TryGetProperty("ledConfig", out var config))
        {
            return null;
        }

        // Mice and headsets get the same treatment from the service and would otherwise arrive
        // here as keyboards with a one-row matrix.
        if (param.TryGetProperty("category", out var category)
            && !string.Equals(category.GetString(), "keyboard", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var keys = new List<SdkKey>();
        var silent = 0;

        if (config.TryGetProperty("LedInputMap", out var map) && map.ValueKind == JsonValueKind.Array)
        {
            foreach (var entry in map.EnumerateArray())
            {
                var key = ReadKey(entry);

                if (key is not null)
                {
                    keys.Add(key.Value);
                }
                else if (IsSilentKey(entry))
                {
                    silent++;
                }
            }
        }

        return new SdkDeviceDescription(
            ProductName: param.TryGetProperty("productName", out var name) ? name.GetString() ?? "" : "",
            VendorId: Number(config, "VID"),
            ProductId: Number(config, "PID"),
            LayoutId: Number(config, "Layout"),
            MatrixRows: Number(config, "MatrixMaxRow"),
            MatrixColumns: Number(config, "MatrixMaxCol"),
            Keys: keys,
            SilentKeys: silent);
    }

    /// <summary>
    /// Whether an entry describes a key that exists but sends nothing — fn and the media
    /// controls. They are lit like any other key, so a keyboard that has one really does have a
    /// key there; it just cannot be recognised by a scan code.
    /// </summary>
    private static bool IsSilentKey(JsonElement entry)
        => entry.TryGetProperty("InputType", out var type)
        && string.Equals(type.GetString(), "dkm", StringComparison.OrdinalIgnoreCase);

    private static SdkKey? ReadKey(JsonElement entry)
    {
        // "dkm" entries are the vendor's own keys — fn and the media controls. They send no
        // scancode, so nothing here can address them.
        if (!entry.TryGetProperty("InputType", out var type)
            || !string.Equals(type.GetString(), "kbd", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if (!entry.TryGetProperty("InputData", out var data)
            || data.ValueKind != JsonValueKind.Array
            || data.GetArrayLength() < 2)
        {
            return null;
        }

        if (!entry.TryGetProperty("MatrixPos", out var position)
            || position.ValueKind != JsonValueKind.Array
            || position.GetArrayLength() < 2)
        {
            return null;
        }

        var values = data.EnumerateArray().ToArray();
        var flag = values[1].GetInt32();
        var scancode = values[0].GetInt32();

        // 0 is a plain scan code and 2 carries the E0 prefix. 4 is the E1 sequence, which only
        // Pause uses: it arrives as 0x1D, the code for left control, and is only Pause because
        // of that prefix. It is reported under the code the scan code table lists it by, so that
        // the key can be recognised at all. 16 marks a vendor key — fn and the media controls —
        // which sends nothing and cannot be matched to a keyboard key.
        switch (flag)
        {
            case 0 or 2:
                break;

            case 4:
                scancode = ScanCodes.PauseSequence;
                break;

            default:
                return null;
        }

        var cell = position.EnumerateArray().ToArray();

        return new SdkKey(
            Scancode: scancode,
            Extended: flag == 2,
            Row: cell[0].GetInt32(),
            Column: cell[1].GetInt32());
    }

    private static int Number(JsonElement element, string name)
        => element.TryGetProperty(name, out var value) && value.TryGetInt32(out var number)
            ? number
            : 0;
}
