// Not among the implicit usings of a WPF project, and LoadIcon needs IOException.
using System.IO;
using System.Windows;
using Keylegend.App.Localisation;
using Keylegend.Core.Session;
using Keylegend.Engine;

// Aliased rather than imported: these namespaces share type names with WPF (Brush, Size,
// Application), and mixing them makes every such name ambiguous across the project.
using ComponentModel = System.ComponentModel;
using Drawing = System.Drawing;
using Forms = System.Windows.Forms;
using Threading = System.Windows.Threading;

namespace Keylegend.App;

/// <summary>
/// Notification-area icon, so the program can run unattended without a window in the way.
/// </summary>
/// <remarks>
/// <para>
/// Closing the window hides it here rather than exiting, because the lighting is the point of
/// the program and it should keep working. Quitting goes through the application's shutdown,
/// which releases the Chroma session — otherwise the keyboard would be left frozen on the last
/// frame until the session timed out.
/// </para>
/// <para>
/// It is built before there is anything to drive, and told about the engine and the window
/// afterwards. That order is the point: at logon the vendor's software may not have named the
/// keyboard yet, and the icon has to be there through the whole wait — an autostart that shows
/// nothing in the notification area is indistinguishable from one that failed.
/// </para>
/// </remarks>
public sealed class TrayIcon : IDisposable
{
    private readonly Forms.NotifyIcon _icon;
    private readonly Drawing.Icon _image;
    private readonly Threading.Dispatcher _dispatcher;
    private readonly Forms.ToolStripMenuItem _showItem;
    private readonly Forms.ToolStripMenuItem _pauseItem;
    private readonly Forms.ToolStripMenuItem _quitItem;

    private LightingEngine? _engine;
    private Window? _window;
    private ComponentModel.CancelEventHandler? _hideInsteadOfClosing;

    private bool _quitting;
    private string? _fault;

    public TrayIcon(Action quit)
    {
        ArgumentNullException.ThrowIfNull(quit);

        // Built on the interface thread, and everything it touches has to happen there. Held
        // rather than taken from a window, because for a while there is no window to take it from.
        _dispatcher = Threading.Dispatcher.CurrentDispatcher;

        _showItem = new Forms.ToolStripMenuItem(
            Texts.Get("TrayShow"), image: null, (_, _) => ShowWindow());

        _pauseItem = new Forms.ToolStripMenuItem(
            Texts.Get("TrayPause"), image: null, (_, _) => TogglePause());

        _quitItem = new Forms.ToolStripMenuItem(Texts.Get("TrayQuit"), image: null, (_, _) =>
        {
            _quitting = true;
            quit();
        });

        var menu = new Forms.ContextMenuStrip();
        menu.Items.Add(_showItem);
        menu.Items.Add(_pauseItem);
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add(_quitItem);

        // Held on to: NotifyIcon does not own the icon it is given, so nobody else frees it.
        _image = LoadIcon();

        _icon = new Forms.NotifyIcon
        {
            Icon = _image,
            Visible = true,
            ContextMenuStrip = menu
        };

        _icon.DoubleClick += (_, _) => ShowWindow();

        // The menu is built once and never rebound, so it has to be written again when the
        // language changes - otherwise it would keep the wording it was created with.
        Texts.Instance.PropertyChanged += OnTextsChanged;

        Refresh();
    }

    /// <summary>
    /// The window the icon shows, and which is hidden here rather than closed.
    /// </summary>
    /// <remarks>
    /// Given again when the wait for the keyboard ends and the real window takes the waiting
    /// one's place. The one it leaves behind is let go of first, so that it can then be closed
    /// for real instead of merely hiding itself.
    /// </remarks>
    public void Watch(Window window)
    {
        ArgumentNullException.ThrowIfNull(window);

        if (_window is not null && _hideInsteadOfClosing is not null)
        {
            _window.Closing -= _hideInsteadOfClosing;
        }

        _window = window;

        // Closing the window keeps the lighting running; only Quit really exits. Silently: the
        // icon in the notification area says the program is still there, and a balloon on every
        // close says the same thing again to somebody who already knows.
        _hideInsteadOfClosing = (_, e) =>
        {
            if (_quitting)
            {
                return;
            }

            e.Cancel = true;
            window.Hide();
        };

        window.Closing += _hideInsteadOfClosing;
    }

    /// <summary>
    /// The engine, once there is a keyboard to drive. Until then the icon has a state of its
    /// own: waiting, with nothing to pause.
    /// </summary>
    public void Drive(LightingEngine engine)
    {
        ArgumentNullException.ThrowIfNull(engine);

        _engine = engine;
        engine.StateChanged += OnStateChanged;
        engine.Fault += OnFault;

        Refresh();
    }

    private void ShowWindow()
    {
        if (_window is not { } window)
        {
            return;
        }

        window.Show();
        window.WindowState = WindowState.Normal;
        window.Activate();
    }

    private void TogglePause()
    {
        if (_engine is not { } engine)
        {
            return;
        }

        if (engine.State == LightingState.Paused)
        {
            engine.Resume();
        }
        else
        {
            engine.Pause();
        }

        Refresh();
    }

    private void OnStateChanged(LightingState state) => Refresh();

    private void Refresh()
    {
        // Raised from the engine's loop, so hop to the UI thread before touching the icon.
        if (!_dispatcher.CheckAccess())
        {
            _dispatcher.BeginInvoke(Refresh);
            return;
        }

        var state = _engine?.State;

        _showItem.Text = Texts.Get("TrayShow");
        _quitItem.Text = Texts.Get("TrayQuit");
        _pauseItem.Text = Texts.Get(state == LightingState.Paused ? "TrayResume" : "TrayPause");

        // Nothing to pause while there is no keyboard: the entry stays visible so the menu does
        // not change shape underneath the user, and says by being grey that it is not yet its turn.
        _pauseItem.Enabled = _engine is not null;

        _icon.Text = state switch
        {
            null => Texts.Get("TrayTooltipWaiting"),
            _ when _fault is not null => Texts.Get("TrayTooltipTrouble"),
            LightingState.Active => Texts.Get("TrayTooltipActive"),
            LightingState.Paused => Texts.Get("TrayTooltipPaused"),
            _ => Texts.Get("TrayTooltipIdle")
        };
    }

    /// <summary>
    /// What the notification area says while the lighting is not working, and the one balloon
    /// that says it out loud.
    /// </summary>
    /// <remarks>
    /// This is the case the window cannot cover: it is usually closed, so a keyboard that stops
    /// lighting looks like the program having quietly given up. The tooltip carries the reason for
    /// as long as it lasts; the balloon fires once per fault rather than on every retry, because
    /// the engine backs off and retrying forever must not mean notifying forever.
    /// </remarks>
    private void OnFault(string? message)
    {
        if (!_dispatcher.CheckAccess())
        {
            _dispatcher.BeginInvoke(() => OnFault(message));
            return;
        }

        var announced = _fault is not null;
        _fault = message;

        Refresh();

        if (message is not null && !announced)
        {
            _icon.ShowBalloonTip(5000, "Keylegend", message, Forms.ToolTipIcon.Warning);
        }
    }

    /// <summary>Writes the menu again after a language change.</summary>
    private void OnTextsChanged(object? sender, ComponentModel.PropertyChangedEventArgs e)
        => Refresh();

    /// <summary>
    /// Takes the application icon from the resources, at the size the notification area is
    /// asking for.
    /// </summary>
    /// <remarks>
    /// The file carries a frame per size from 16 px upwards, each drawn for that size; picking
    /// the matching one keeps the icon sharp instead of leaving a large drawing to be squeezed
    /// down to a smudge. The size asked for follows the display's scaling, which is why the
    /// file also has the 20, 24 and 40 px frames that high-DPI screens want.
    /// </remarks>
    private static Drawing.Icon LoadIcon()
    {
        try
        {
            var resource = Application.GetResourceStream(new Uri("keylegend.ico", UriKind.Relative));
            if (resource is not null)
            {
                using var stream = resource.Stream;
                return new Drawing.Icon(stream, Forms.SystemInformation.SmallIconSize);
            }
        }
        catch (Exception e) when (e is IOException or ArgumentException)
        {
            // Falls through to the drawn icon below - a missing icon is no reason to start
            // without one.
        }

        return BuildIcon();
    }

    /// <summary>
    /// The fallback for a build whose icon resource is missing: three coloured key caps,
    /// echoing what the program does.
    /// </summary>
    private static Drawing.Icon BuildIcon()
    {
        using var bitmap = new Drawing.Bitmap(32, 32, Drawing.Imaging.PixelFormat.Format32bppArgb);

        using (var graphics = Drawing.Graphics.FromImage(bitmap))
        {
            graphics.SmoothingMode = Drawing.Drawing2D.SmoothingMode.AntiAlias;
            graphics.Clear(Drawing.Color.Transparent);

            var caps = new (Drawing.Rectangle Area, Drawing.Color Fill)[]
            {
                (new Drawing.Rectangle(3, 6, 11, 9), Drawing.Color.FromArgb(60, 140, 255)),
                (new Drawing.Rectangle(17, 6, 11, 9), Drawing.Color.FromArgb(255, 150, 0)),
                (new Drawing.Rectangle(3, 18, 25, 9), Drawing.Color.FromArgb(0, 220, 140))
            };

            foreach (var (area, fill) in caps)
            {
                using var brush = new Drawing.SolidBrush(fill);
                graphics.FillRectangle(brush, area);
            }
        }

        return Drawing.Icon.FromHandle(bitmap.GetHicon());
    }

    public void Dispose()
    {
        if (_engine is { } engine)
        {
            engine.StateChanged -= OnStateChanged;
            engine.Fault -= OnFault;
        }

        Texts.Instance.PropertyChanged -= OnTextsChanged;
        _icon.Visible = false;
        _icon.Dispose();
        _image.Dispose();
    }
}
