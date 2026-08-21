using Keylegend.Core.Devices;
using Keylegend.Core.Input;
using Keylegend.Windows;

namespace Keylegend.Host;

/// <summary>
/// Prints what every key resolves to under each modifier layer. This is how the character
/// resolution is checked without a keyboard attached to the eye — the AltGr column in
/// particular shows immediately whether the layout is being read correctly.
/// </summary>
internal static class LayoutDump
{
    public static void Run(DeviceProfile profile)
    {
        var resolver = new WindowsKeyResolver();
        resolver.RefreshLayout();

        // Optionally narrow the dump to a subset, e.g. --dump-layout Num
        var filter = Environment.GetCommandLineArgs()
            .SkipWhile(a => !a.Equals("--dump-layout", StringComparison.OrdinalIgnoreCase))
            .Skip(1)
            .FirstOrDefault(a => !a.StartsWith("--", StringComparison.Ordinal));

        Console.WriteLine(
            $"{"Key",-34} {"plain",-8} {"Shift",-8} {"AltGr",-8} {"category",-12} {"Num off",-8} category");
        Console.WriteLine(new string('-', 100));

        foreach (var key in profile.Keys.OrderBy(k => k.Y).ThenBy(k => k.X))
        {
            if (filter is not null && !key.Id.Contains(filter, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var plain = resolver.Resolve(key.Id, key.ScanCode, State());
            var shifted = resolver.Resolve(key.Id, key.ScanCode, State(ModifierKeys.LeftShift));
            var altGr = resolver.Resolve(key.Id, key.ScanCode, State(ModifierKeys.RightAlt));
            var numOff = resolver.Resolve(key.Id, key.ScanCode, State(numLock: false));

            // Report the category the composer would actually use, structural overrides
            // included - otherwise the dump would claim function keys are plain controls.
            var effective = KeyRoles.StructuralCategory(key.Id) ?? plain.Category;
            var effectiveNumOff = KeyRoles.StructuralCategory(key.Id) ?? numOff.Category;

            Console.WriteLine(
                $"{key.Id,-34} {Show(plain),-8} {Show(shifted),-8} {Show(altGr),-8} " +
                $"{effective,-12} {Show(numOff),-8} {effectiveNumOff}");
        }

        static KeyboardState State(ModifierKeys modifiers = ModifierKeys.None, bool numLock = true)
            => new(modifiers, new LockStates(NumLock: numLock, CapsLock: false, ScrollLock: false));

        static string Show(KeyMeaning meaning) => meaning.Character switch
        {
            null or "" => "—",
            var c when c.Length > 0 && char.IsControl(c[0]) => "(ctrl)",
            var c => c
        };
    }
}
