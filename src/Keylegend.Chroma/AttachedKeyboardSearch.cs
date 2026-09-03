using Keylegend.Core.Devices;

namespace Keylegend.Chroma;

/// <summary>Why the attached keyboard could not be described this time.</summary>
/// <remarks>
/// Four cases and not one message, because they ask different things of the user: wait, open the
/// vendor's interface once, report a changed file format, or report a bug here. One sentence
/// covering all four would send most of the people who read it the wrong way.
/// </remarks>
public enum KeyboardAbsence
{
    /// <summary>
    /// The lighting service names no keyboard. Either its software is not running yet — the
    /// ordinary case at logon — or none is plugged in.
    /// </summary>
    NoKeyboard,

    /// <summary>
    /// A keyboard is named, but the vendor's interface keeps no drawing of it. Its cache fills
    /// when that interface has shown the device once.
    /// </summary>
    NoDrawing,

    /// <summary>The drawing is there and could not be read, which means the format moved.</summary>
    DrawingUnreadable,

    /// <summary>A keyboard was assembled and does not hold together. <see cref="AttachedKeyboardFind.Detail"/> says how.</summary>
    Unusable
}

/// <summary>What one look for the attached keyboard turned up.</summary>
public readonly record struct AttachedKeyboardFind
{
    private AttachedKeyboardFind(AttachedKeyboard? keyboard, KeyboardAbsence absence, string? detail)
    {
        Keyboard = keyboard;
        Absence = absence;
        Detail = detail;
    }

    /// <summary>The keyboard, or <c>null</c> when this look found none.</summary>
    public AttachedKeyboard? Keyboard { get; }

    /// <summary>Why there is none. Meaningless when <see cref="Keyboard"/> is set.</summary>
    public KeyboardAbsence Absence { get; }

    /// <summary>What went wrong, where the reason alone does not say it.</summary>
    public string? Detail { get; }

    public static AttachedKeyboardFind Found(AttachedKeyboard keyboard)
        => new(keyboard ?? throw new ArgumentNullException(nameof(keyboard)), default, null);

    public static AttachedKeyboardFind Absent(KeyboardAbsence absence, string? detail = null)
        => new(null, absence, detail);
}

/// <summary>
/// Looks for the keyboard the vendor's software has, and keeps looking until it has one.
/// </summary>
/// <remarks>
/// <para>
/// The lighting service writes its description of the attached keyboard when its software comes
/// up at logon, and deletes it again when the device goes away — so before that moment the
/// description is not merely stale, it is absent. Measured on the machine this was written for:
/// the file appeared ninety-five seconds after the system started, and this program's own
/// autostart entry fired eight seconds later. Which of the two wins is decided by whatever else
/// the machine is doing that morning.
/// </para>
/// <para>
/// That is why the answer to "nothing there" is to wait and look again rather than to stop. The
/// lighting is the program, the keyboard is the only thing it cannot supply for itself, and a
/// user who has just switched the computer on has done nothing wrong.
/// </para>
/// </remarks>
public sealed class AttachedKeyboardSearch
{
    private readonly Func<AttachedKeyboardFind> _look;
    private readonly Func<TimeSpan, CancellationToken, Task> _wait;

    /// <param name="look">One look. Defaults to the real one, reading the vendor's files.</param>
    /// <param name="wait">
    /// How to let time pass between looks. Injected so that a test can run the waiting
    /// instantly, which is the only way to check the cadence at all.
    /// </param>
    public AttachedKeyboardSearch(
        Func<AttachedKeyboardFind>? look = null,
        Func<TimeSpan, CancellationToken, Task>? wait = null)
    {
        _look = look ?? (() => Look());
        _wait = wait ?? Task.Delay;
    }

    /// <summary>Raised for every look that found nothing, so the interface can say what is missing.</summary>
    public event Action<AttachedKeyboardFind>? Absent;

    /// <summary>
    /// Looks until there is a keyboard, and returns it. Ends only that way or by cancellation:
    /// there is no failure to report, because every reason it could report is one that may
    /// resolve itself a moment later.
    /// </summary>
    public async Task<AttachedKeyboard> WaitAsync(CancellationToken cancellationToken)
    {
        var reason = default(KeyboardAbsence);
        var running = 0;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var find = _look();

            if (find.Keyboard is { } keyboard)
            {
                return keyboard;
            }

            // Counted per reason. Waiting minutes for the device must not hand the search for
            // its drawing a delay it never earned.
            running = find.Absence == reason ? running + 1 : 1;
            reason = find.Absence;

            Absent?.Invoke(find);

            await _wait(WaitAfter(find.Absence, running), cancellationToken);
        }
    }

    /// <summary>
    /// How long to wait after a fruitless look, given what was missing and how many times in a
    /// row it has been missing.
    /// </summary>
    /// <remarks>
    /// A look that found no keyboard read one directory and stopped, so repeating it costs
    /// nothing and it stays brisk — this is the wait that runs while the vendor's software is
    /// still starting, and the keyboard should light as soon as it can. A look that went on to
    /// hunt for a drawing walked thousands of cache files, so it backs off; that case also
    /// usually needs the user to open the vendor's interface, which is not something that
    /// happens within seconds.
    /// </remarks>
    public static TimeSpan WaitAfter(KeyboardAbsence absence, int attempts)
    {
        if (absence == KeyboardAbsence.NoKeyboard)
        {
            return TimeSpan.FromSeconds(2);
        }

        var seconds = 2 * Math.Pow(2, Math.Max(attempts, 1) - 1);

        return TimeSpan.FromSeconds(Math.Min(seconds, 30));
    }

    /// <summary>
    /// One look at what the vendor's software has on disk.
    /// </summary>
    /// <param name="deviceDirectories">Where the lighting service writes; its own places by default.</param>
    /// <param name="drawingDirectories">Where the interface caches drawings; its own place by default.</param>
    public static AttachedKeyboardFind Look(
        IEnumerable<string>? deviceDirectories = null,
        IEnumerable<string>? drawingDirectories = null)
    {
        // What the lighting service says is plugged in. Absent when its software is not running,
        // or when no Razer keyboard is connected.
        var attached = SdkDeviceDescription.ReadAll(deviceDirectories).FirstOrDefault();

        if (attached is null || attached.Keys.Count == 0)
        {
            return AttachedKeyboardFind.Absent(KeyboardAbsence.NoKeyboard);
        }

        // The vendor's own drawing of the attached model. It carries the keys and their names,
        // their real sizes, the casing, and the legends printed on the caps in the right
        // language. What it does not carry — which cell each key lights — is a constant of the
        // protocol.
        //
        // Missing is its own case, and not the same as "no keyboard": the service is running and
        // has named the device. The drawing lives in the cache of the vendor's web interface,
        // which fills when that interface shows the device — so opening it once is the fix, and
        // saying "connect your keyboard" would be plainly wrong.
        var drawing = SvgLayoutSource.Find(attached, drawingDirectories);

        if (drawing is null)
        {
            return AttachedKeyboardFind.Absent(KeyboardAbsence.NoDrawing);
        }

        // Present but not understood is a third thing again, and nothing the user can act on:
        // the file is there and we cannot read it, which means the format moved under us.
        var keyboard = AttachedKeyboardBuilder.FromDrawing(attached, drawing);

        if (keyboard is null)
        {
            return AttachedKeyboardFind.Absent(KeyboardAbsence.DrawingUnreadable);
        }

        var problems = AttachedKeyboardValidator.Validate(keyboard);

        return problems.Count == 0
            ? AttachedKeyboardFind.Found(keyboard)
            : AttachedKeyboardFind.Absent(
                KeyboardAbsence.Unusable,
                "The attached keyboard could not be described:" + Environment.NewLine + "  " +
                string.Join(Environment.NewLine + "  ", problems));
    }
}
