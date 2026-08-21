using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Keylegend.Core.Lighting;

namespace Keylegend.App.Views;

/// <summary>
/// One editable colour: a label, a swatch and its hex value.
/// </summary>
/// <remarks>
/// Built in code rather than as a XAML template because the colour list is generated from the
/// enums — adding a category should not require touching markup, so that the list cannot fall
/// out of step with what the program actually supports.
/// </remarks>
public sealed class ColourRow : Border
{
    private readonly Border _swatch;
    private readonly TextBlock _value;

    public ColourRow(string label, RgbColor colour, string? explanation = null)
    {
        Colour = colour;

        _swatch = new Border
        {
            Width = 34,
            Height = 24,
            CornerRadius = new CornerRadius(3),
            Background = Fill(colour),
            BorderBrush = new SolidColorBrush(Color.FromRgb(0x5A, 0x5A, 0x6C)),
            BorderThickness = new Thickness(1),
            Cursor = System.Windows.Input.Cursors.Hand,
            VerticalAlignment = VerticalAlignment.Center
        };

        _value = new TextBlock
        {
            Text = colour.ToString(),
            Foreground = new SolidColorBrush(Color.FromRgb(0x9A, 0x9A, 0xA8)),
            FontFamily = new FontFamily("Consolas"),
            FontSize = 12,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(10, 0, 0, 0),
            Width = 70
        };

        var name = new TextBlock
        {
            Text = label,
            Foreground = new SolidColorBrush(Color.FromRgb(0xE8, 0xE8, 0xEE)),
            FontFamily = new FontFamily("Segoe UI"),
            VerticalAlignment = VerticalAlignment.Center,
            Width = 170
        };

        var row = new StackPanel { Orientation = Orientation.Horizontal };
        row.Children.Add(name);
        row.Children.Add(_swatch);
        row.Children.Add(_value);

        if (!string.IsNullOrEmpty(explanation))
        {
            row.Children.Add(new TextBlock
            {
                Text = explanation,
                Foreground = new SolidColorBrush(Color.FromRgb(0x7A, 0x7A, 0x88)),
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 12,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(14, 0, 0, 0)
            });
        }

        Child = row;
        Padding = new Thickness(0, 4, 0, 4);
        Cursor = System.Windows.Input.Cursors.Hand;

        MouseLeftButtonUp += (_, _) => Picked?.Invoke(this);
    }

    /// <summary>The colour currently shown.</summary>
    public RgbColor Colour { get; private set; }

    /// <summary>Raised when the row is clicked, so the host can open a colour picker.</summary>
    public event Action<ColourRow>? Picked;

    public void Show(RgbColor colour)
    {
        Colour = colour;
        _swatch.Background = Fill(colour);
        _value.Text = colour.ToString();
    }

    private static SolidColorBrush Fill(RgbColor colour)
        => new(Color.FromRgb(colour.R, colour.G, colour.B));
}
