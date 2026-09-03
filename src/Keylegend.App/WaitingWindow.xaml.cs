using System.ComponentModel;
using System.Windows;
using Keylegend.App.Localisation;
using Keylegend.Chroma;

namespace Keylegend.App;

/// <summary>
/// What the program shows while the vendor's software has no keyboard for it yet.
/// </summary>
/// <remarks>
/// <para>
/// A window rather than a message box, because a message box is modal and would hold the start
/// still: at logon that meant a process with no notification-area icon, waiting behind whatever
/// else was on screen for an answer nobody could see it asking for.
/// </para>
/// <para>
/// It says what is missing, that the program keeps looking, and when it last looked — the last
/// of those because a window that says "waiting" and never changes is indistinguishable from one
/// that has hung.
/// </para>
/// </remarks>
public partial class WaitingWindow : Window
{
    private KeyboardAbsence _absence = KeyboardAbsence.NoKeyboard;
    private string? _detail;
    private DateTimeOffset? _looked;

    public WaitingWindow()
    {
        InitializeComponent();

        // The texts are written in code rather than bound, so a language change has to write
        // them again.
        Texts.Instance.PropertyChanged += OnTextsChanged;

        Write();
    }

    /// <summary>Says what the latest look was missing, and when it happened.</summary>
    public void Report(AttachedKeyboardFind find, DateTimeOffset looked)
    {
        _absence = find.Absence;
        _detail = find.Detail;
        _looked = looked;

        Write();
    }

    private void OnTextsChanged(object? sender, PropertyChangedEventArgs e) => Write();

    private void Write()
    {
        // A detail is only carried where the reason alone cannot say it — a keyboard that was
        // assembled and does not hold together, which names the parts that disagree.
        Reason.Text = _detail ?? Texts.Get(TextFor(_absence));

        LastAttempt.Text = _looked is { } at
            ? Texts.Get("WaitingLastAttempt", at.LocalDateTime.ToString("T", Texts.Instance.Culture))
            : string.Empty;
    }

    private static string TextFor(KeyboardAbsence absence) => absence switch
    {
        KeyboardAbsence.NoDrawing => "StartupNoDrawing",
        KeyboardAbsence.DrawingUnreadable => "StartupDrawingUnreadable",
        _ => "StartupNoKeyboard"
    };

    protected override void OnClosed(EventArgs e)
    {
        Texts.Instance.PropertyChanged -= OnTextsChanged;

        base.OnClosed(e);
    }
}
