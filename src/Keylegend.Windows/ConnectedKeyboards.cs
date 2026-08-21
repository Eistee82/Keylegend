using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using Keylegend.Core.Devices;

namespace Keylegend.Windows;

/// <summary>
/// Asks Windows which keyboards are actually plugged in, so the right device profile can be
/// recognised instead of guessed.
/// </summary>
/// <remarks>
/// <para>
/// Raw Input rather than WMI: it is two P/Invokes against <c>user32</c>, answers in under a
/// millisecond, and needs no service running. <c>Get-PnpDevice</c> shows the same hardware and is
/// the friendlier way to look an id up by hand, but starting a WMI query at every launch to learn
/// four hex digits would be a poor trade.
/// </para>
/// <para>
/// Every device name comes back in the form <c>\\?\HID#VID_1532&amp;PID_0295&amp;MI_01#…</c>. The
/// vendor and product ids are the only part worth keeping.
/// </para>
/// </remarks>
public static class ConnectedKeyboards
{
    private const uint RidiDeviceName = 0x20000007;
    private const uint RimTypeKeyboard = 1;

    private static readonly Regex UsbIdPattern = new(
        @"VID_(?<vendor>[0-9A-Fa-f]{4}).*?PID_(?<product>[0-9A-Fa-f]{4})",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>
    /// Every keyboard Windows currently reports, without duplicates. A single keyboard usually
    /// appears several times — one entry per HID collection it exposes, for the keys, the media
    /// controls and whatever else it offers — and they all carry the same pair of ids.
    /// </summary>
    /// <remarks>
    /// Returns an empty list rather than throwing if Raw Input is unavailable. Not knowing which
    /// keyboard is attached is a reason to fall back to choosing a profile by other means, never
    /// a reason to refuse to start.
    /// </remarks>
    public static IReadOnlyList<UsbId> Detect()
    {
        try
        {
            return Enumerate().Distinct().ToArray();
        }
        catch (Exception exception) when (exception is DllNotFoundException or EntryPointNotFoundException)
        {
            return [];
        }
    }

    private static IEnumerable<UsbId> Enumerate()
    {
        var size = (uint)Marshal.SizeOf<RawInputDeviceList>();
        uint count = 0;

        if (NativeMethods.GetRawInputDeviceList(IntPtr.Zero, ref count, size) != 0 || count == 0)
        {
            yield break;
        }

        var buffer = Marshal.AllocHGlobal((int)(size * count));
        try
        {
            var written = NativeMethods.GetRawInputDeviceList(buffer, ref count, size);

            // The call returns unchecked (uint)-1 on failure, which is not a plausible count.
            if (written == uint.MaxValue)
            {
                yield break;
            }

            for (var i = 0; i < written; i++)
            {
                var device = Marshal.PtrToStructure<RawInputDeviceList>(buffer + (int)(i * size));

                if (device.Type != RimTypeKeyboard)
                {
                    continue;
                }

                var id = ReadUsbId(device.Device);
                if (id is not null)
                {
                    yield return id;
                }
            }
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    private static UsbId? ReadUsbId(IntPtr device)
    {
        uint length = 0;
        if (NativeMethods.GetRawInputDeviceInfoW(device, RidiDeviceName, IntPtr.Zero, ref length) != 0
            || length == 0)
        {
            return null;
        }

        var name = new StringBuilder((int)length + 1);
        if (NativeMethods.GetRawInputDeviceInfoW(device, RidiDeviceName, name, ref length) == uint.MaxValue)
        {
            return null;
        }

        var match = UsbIdPattern.Match(name.ToString());

        // PS/2 and virtual keyboards have no USB ids, and a laptop's built-in keyboard often
        // reports an ACPI name instead. Those simply cannot be recognised this way.
        return match.Success
            ? new UsbId(
                match.Groups["vendor"].Value.ToUpperInvariant(),
                match.Groups["product"].Value.ToUpperInvariant())
            : null;
    }

    /// <summary>
    /// The physical layout the active Windows keyboard layout suggests, as a profile would spell
    /// it — <c>ISO-DE</c>, <c>ANSI-US</c> and so on, or <c>null</c> where there is no good guess.
    /// </summary>
    /// <remarks>
    /// This is a hint and nothing more. The language someone types in and the shape of the board
    /// under their hands are different facts, and plenty of people pair a US keyboard with a
    /// German layout. It is used only to choose between profiles for the <em>same</em> model,
    /// where the alternative is picking one at random.
    /// </remarks>
    public static string? SuggestPhysicalLayout()
    {
        var culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;

        return culture switch
        {
            "de" => "ISO-DE",
            "fr" => "ISO-FR",
            "es" => "ISO-ES",
            "it" => "ISO-IT",
            "pt" => "ISO-PT",
            "pl" => "ISO-PL",
            "ru" => "ISO-RU",
            "uk" => "ISO-RU",       // Ukrainian boards follow the same physical shape
            "sv" or "fi" or "nb" or "no" or "da" => "ISO-NORDIC",
            "ja" => "JIS-JP",
            "en" => "ANSI-US",
            "nl" or "zh" => "ANSI-US",
            _ => null
        };
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RawInputDeviceList
    {
        public IntPtr Device;
        public uint Type;
    }
}
