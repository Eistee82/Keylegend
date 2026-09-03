using Keylegend.Chroma;
using Keylegend.Core.Devices;

namespace Keylegend.Core.Tests.Chroma;

/// <summary>
/// The search that keeps looking for the attached keyboard until the vendor's software has it.
/// </summary>
/// <remarks>
/// <para>
/// Why it looks more than once. The lighting service writes its description of the attached
/// keyboard at logon and it is simply not there before then — on the machine this was written
/// on, that file was created ninety-five seconds after the system came up, and the autostart
/// entry for this program fired eight seconds later. Eight seconds is the whole margin, and it
/// is not a margin anyone owns: an update, a cold disk or one more program in the startup list
/// moves it either way.
/// </para>
/// <para>
/// A start that looks once and gives up loses that race whenever the vendor's software is a
/// little slower, and loses it in the worst way — at logon, with no window on screen to say so.
/// So the answer to "no keyboard yet" is to wait and look again, not to stop.
/// </para>
/// </remarks>
public class AttachedKeyboardSearchTests
{
    private static AttachedKeyboard Keyboard() => new(
        "Razer DeathStalker V2",
        "ISO-DE",
        new Canvas(100, 40),
        new MatrixSize(6, 22),
        [new KeyDefinition("Keyboard_A", 0, 0, 10, 10, Row: 3, Column: 2)]);

    /// <summary>Records what it was asked to wait for, and waits for none of it.</summary>
    private sealed class Clock
    {
        public List<TimeSpan> Waited { get; } = [];

        public Action? Before { get; set; }

        public Task Wait(TimeSpan period, CancellationToken cancellationToken)
        {
            Waited.Add(period);
            Before?.Invoke();

            return Task.CompletedTask;
        }
    }

    /// <summary>Hands out a prepared answer per look, and counts how often it was asked.</summary>
    private sealed class Looks(params AttachedKeyboardFind[] answers)
    {
        public int Count { get; private set; }

        public AttachedKeyboardFind Next()
        {
            var answer = answers[Math.Min(Count, answers.Length - 1)];
            Count++;

            return answer;
        }
    }

    [Fact]
    public async Task FindsTheKeyboardWithoutWaitingWhenItIsAlreadyThere()
    {
        var keyboard = Keyboard();
        var looks = new Looks(AttachedKeyboardFind.Found(keyboard));
        var clock = new Clock();
        var search = new AttachedKeyboardSearch(looks.Next, clock.Wait);

        var found = await search.WaitAsync(CancellationToken.None);

        Assert.Same(keyboard, found);
        Assert.Equal(1, looks.Count);
        Assert.Empty(clock.Waited);
    }

    /// <summary>The race itself: nothing there, then nothing there, then there.</summary>
    [Fact]
    public async Task KeepsLookingUntilTheKeyboardAppears()
    {
        var keyboard = Keyboard();
        var looks = new Looks(
            AttachedKeyboardFind.Absent(KeyboardAbsence.NoKeyboard),
            AttachedKeyboardFind.Absent(KeyboardAbsence.NoKeyboard),
            AttachedKeyboardFind.Found(keyboard));

        var search = new AttachedKeyboardSearch(looks.Next, new Clock().Wait);

        var found = await search.WaitAsync(CancellationToken.None);

        Assert.Same(keyboard, found);
        Assert.Equal(3, looks.Count);
    }

    /// <summary>
    /// Every fruitless look is announced, because that is what the window and the notification
    /// area say while the waiting lasts. The three reasons ask different things of the user and
    /// must not be flattened into one.
    /// </summary>
    [Fact]
    public async Task SaysWhyEachTimeItFindsNothing()
    {
        var looks = new Looks(
            AttachedKeyboardFind.Absent(KeyboardAbsence.NoKeyboard),
            AttachedKeyboardFind.Absent(KeyboardAbsence.NoDrawing),
            AttachedKeyboardFind.Found(Keyboard()));

        var search = new AttachedKeyboardSearch(looks.Next, new Clock().Wait);
        var said = new List<KeyboardAbsence>();
        search.Absent += find => said.Add(find.Absence);

        await search.WaitAsync(CancellationToken.None);

        Assert.Equal([KeyboardAbsence.NoKeyboard, KeyboardAbsence.NoDrawing], said);
    }

    /// <summary>
    /// A look that found no keyboard at all cost one directory listing, so it may be repeated
    /// briskly. A look that went on to search the vendor's cache for a drawing walked thousands
    /// of files, so it backs off — and the counting starts again when the reason changes, or
    /// the wait for a drawing would inherit a long delay from the minutes spent waiting for the
    /// device.
    /// </summary>
    [Fact]
    public async Task WaitsBrisklyForADeviceAndBacksOffWhileSearchingForItsDrawing()
    {
        var looks = new Looks(
            AttachedKeyboardFind.Absent(KeyboardAbsence.NoKeyboard),
            AttachedKeyboardFind.Absent(KeyboardAbsence.NoKeyboard),
            AttachedKeyboardFind.Absent(KeyboardAbsence.NoDrawing),
            AttachedKeyboardFind.Absent(KeyboardAbsence.NoDrawing),
            AttachedKeyboardFind.Absent(KeyboardAbsence.NoDrawing),
            AttachedKeyboardFind.Found(Keyboard()));

        var clock = new Clock();
        var search = new AttachedKeyboardSearch(looks.Next, clock.Wait);

        await search.WaitAsync(CancellationToken.None);

        Assert.Equal(
            [
                TimeSpan.FromSeconds(2),
                TimeSpan.FromSeconds(2),
                TimeSpan.FromSeconds(2),
                TimeSpan.FromSeconds(4),
                TimeSpan.FromSeconds(8)
            ],
            clock.Waited);
    }

    /// <summary>However long the vendor's software takes, the waiting never grows past this.</summary>
    [Fact]
    public void NeverWaitsLongerThanHalfAMinute()
    {
        Assert.Equal(TimeSpan.FromSeconds(30), AttachedKeyboardSearch.WaitAfter(KeyboardAbsence.NoDrawing, 12));
        Assert.Equal(TimeSpan.FromSeconds(2), AttachedKeyboardSearch.WaitAfter(KeyboardAbsence.NoKeyboard, 900));
    }

    /// <summary>Quitting while the program waits has to end the waiting, not outlive it.</summary>
    [Fact]
    public async Task StopsLookingWhenTheProgramIsShuttingDown()
    {
        using var stopping = new CancellationTokenSource();

        var looks = new Looks(AttachedKeyboardFind.Absent(KeyboardAbsence.NoKeyboard));
        var clock = new Clock { Before = stopping.Cancel };
        var search = new AttachedKeyboardSearch(looks.Next, clock.Wait);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => search.WaitAsync(stopping.Token));

        Assert.Equal(1, looks.Count);
    }

    // ------------------------------------------------------- what a real look reports

    private const string OneKeyKeyboard = """
        {
          "func": "AddDevice",
          "param": {
            "category": "keyboard",
            "productName": "Razer DeathStalker V2",
            "ledConfig": {
              "VID": 5426,
              "PID": 661,
              "Layout": 3,
              "MatrixMaxRow": 6,
              "MatrixMaxCol": 22,
              "LedInputMap": [
                { "InputData": [30, 0], "InputType": "kbd", "MatrixPos": [3, 2] }
              ]
            }
          }
        }
        """;

    private static string TemporaryDirectory()
    {
        var path = Path.Combine(
            Path.GetTempPath(), "keylegend-search-" + Guid.NewGuid().ToString("N")[..12]);

        Directory.CreateDirectory(path);

        return path;
    }

    /// <summary>
    /// The state Keylegend starts into at logon: the lighting service has written nothing yet.
    /// </summary>
    [Fact]
    public void ReportsNoKeyboardWhileTheServiceHasWrittenNothing()
    {
        var devices = TemporaryDirectory();
        var drawings = TemporaryDirectory();

        try
        {
            var find = AttachedKeyboardSearch.Look([devices], [drawings]);

            Assert.Null(find.Keyboard);
            Assert.Equal(KeyboardAbsence.NoKeyboard, find.Absence);
        }
        finally
        {
            Directory.Delete(devices, recursive: true);
            Directory.Delete(drawings, recursive: true);
        }
    }

    /// <summary>
    /// And the state just after: the device is announced, while the drawing its interface caches
    /// has not arrived. A different reason, and a different thing to tell the user.
    /// </summary>
    [Fact]
    public void ReportsNoDrawingOnceTheDeviceIsAnnouncedWithoutOne()
    {
        var devices = TemporaryDirectory();
        var drawings = TemporaryDirectory();

        try
        {
            File.WriteAllText(Path.Combine(devices, "device.json"), OneKeyKeyboard);

            var find = AttachedKeyboardSearch.Look([devices], [drawings]);

            Assert.Null(find.Keyboard);
            Assert.Equal(KeyboardAbsence.NoDrawing, find.Absence);
        }
        finally
        {
            Directory.Delete(devices, recursive: true);
            Directory.Delete(drawings, recursive: true);
        }
    }
}
