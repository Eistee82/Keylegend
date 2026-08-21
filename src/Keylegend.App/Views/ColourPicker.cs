using Keylegend.Core.Lighting;
using Forms = System.Windows.Forms;

namespace Keylegend.App.Views;

/// <summary>
/// Asks the user for a colour.
/// </summary>
/// <remarks>
/// Uses the standard Windows colour dialog rather than a hand-built picker. It already offers a
/// palette, a spectrum, custom colours and keyboard operation, and people know it — writing a
/// worse one to avoid a WinForms reference would be a poor trade, and WinForms is already
/// referenced for the tray icon.
/// </remarks>
internal static class ColourPicker
{
    private static readonly int[] Custom = BuildCustomPalette();

    /// <summary>Returns the chosen colour, or <c>null</c> if the user cancelled.</summary>
    public static RgbColor? Choose(RgbColor current)
    {
        using var dialog = new Forms.ColorDialog
        {
            Color = System.Drawing.Color.FromArgb(current.R, current.G, current.B),
            FullOpen = true,
            AllowFullOpen = true,
            SolidColorOnly = true,
            CustomColors = Custom
        };

        if (dialog.ShowDialog() != Forms.DialogResult.OK)
        {
            return null;
        }

        return new RgbColor(dialog.Color.R, dialog.Color.G, dialog.Color.B);
    }

    /// <summary>
    /// Pre-loads the dialog's custom slots with fully saturated colours. Keyboard LEDs look
    /// best driven at full channel, and the dialog's own palette is full of muted shades that
    /// are a poor starting point here.
    /// </summary>
    private static int[] BuildCustomPalette()
    {
        var colours = new (byte R, byte G, byte B)[]
        {
            (255, 0, 0), (255, 128, 0), (255, 255, 0), (0, 255, 0),
            (0, 255, 128), (0, 255, 255), (0, 128, 255), (0, 0, 255),
            (128, 0, 255), (255, 0, 255), (255, 0, 128), (255, 255, 255),
            (128, 128, 255), (255, 128, 128), (128, 255, 128), (30, 30, 30)
        };

        // The dialog wants BGR-packed integers - the same order Chroma uses, coincidentally.
        return [.. colours.Select(c => (c.B << 16) | (c.G << 8) | c.R)];
    }
}
