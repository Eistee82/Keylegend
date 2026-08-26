using Keylegend.Core.Devices;
using Keylegend.Core.Input;

namespace Keylegend.Windows;

/// <summary>
/// The real keyboard, read by polling. Bundles the state reader and the activity tracker so
/// callers depend on one thing rather than two.
/// </summary>
public sealed class WindowsKeyStateSource : IKeyStateSource
{
    private readonly KeyboardStateReader _reader = new();
    private readonly ActivityTracker _activity;

    public WindowsKeyStateSource(AttachedKeyboard keyboard)
    {
        _activity = new ActivityTracker(keyboard);
    }

    public KeyboardState Read() => _reader.Read();

    public bool AnyKeyDown() => _activity.AnyKeyDown();
}
