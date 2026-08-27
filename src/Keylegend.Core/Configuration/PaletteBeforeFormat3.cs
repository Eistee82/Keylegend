using Keylegend.Core.Input;
using Keylegend.Core.Lighting;
using Keylegend.Core.Shortcuts;

namespace Keylegend.Core.Configuration;

/// <summary>
/// The shipped palette as it stood while settings files were written in format 2.
/// </summary>
/// <remarks>
/// <para>
/// Format 2 wrote every colour to the file, the untouched ones included, so such a file cannot say
/// which of its entries were decisions and which were defaults echoed back. This is what makes the
/// difference recoverable: an entry equal to the palette of the time was a default, and honouring
/// it would pin that palette on everyone who has ever run the program — the improvement would
/// reach nobody. An entry that differs was somebody's choice and is kept.
/// </para>
/// <para>
/// It is a frozen copy on purpose. Reading <see cref="ColourScheme.Default"/> here would defeat the
/// whole comparison the moment the palette changes again, which is exactly when it is needed. So
/// this table does not follow the palette; it records where the palette was.
/// </para>
/// </remarks>
internal static class PaletteBeforeFormat3
{
    private static readonly Dictionary<KeyCategory, RgbColor> Categories = new()
    {
        [KeyCategory.Unassigned] = new(0, 0, 0),
        [KeyCategory.Digit] = new(0, 255, 255),
        [KeyCategory.Lowercase] = new(0, 0, 255),
        [KeyCategory.Uppercase] = new(0, 255, 0),
        [KeyCategory.Symbol] = new(255, 255, 0),
        [KeyCategory.Control] = new(200, 0, 255),
        [KeyCategory.DeadKey] = new(255, 80, 0),
        [KeyCategory.FunctionKey] = new(255, 255, 255),
    };

    private static readonly Dictionary<FunctionGroup, RgbColor> Groups = new()
    {
        [FunctionGroup.Edit] = new(0, 255, 0),
        [FunctionGroup.File] = new(0, 0, 255),
        [FunctionGroup.Search] = new(255, 255, 0),
        [FunctionGroup.View] = new(255, 0, 255),
        [FunctionGroup.Window] = new(0, 255, 255),
        [FunctionGroup.System] = new(255, 0, 0),

        // The one that made this file necessary: a pale blue, reported from the hardware as
        // looking white beside the function row and beside Navigation, which are both white.
        [FunctionGroup.Tools] = new(128, 128, 255),

        [FunctionGroup.Navigation] = new(255, 255, 255),
    };

    /// <summary>Whether this was the shipped colour for that category in format 2.</summary>
    public static bool WasCategoryDefault(KeyCategory category, RgbColor colour)
        => Categories.TryGetValue(category, out var original) && original == colour;

    /// <summary>Whether this was the shipped colour for that group in format 2.</summary>
    public static bool WasGroupDefault(FunctionGroup group, RgbColor colour)
        => Groups.TryGetValue(group, out var original) && original == colour;
}
