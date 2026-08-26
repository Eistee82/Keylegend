using Keylegend.Windows;

namespace Keylegend.Core.Tests.Windows;

/// <summary>
/// The arbitration between two copies of the program starting at once.
/// </summary>
/// <remarks>
/// <para>
/// Every test uses a name of its own, so that a run of this file cannot collide with itself, with
/// a second run alongside it, or with a copy of Keylegend the developer happens to have open.
/// </para>
/// <para>
/// Each simulated copy gets a thread of its own, and that is not decoration. The claim is a
/// Windows mutex, and a mutex belongs to the <em>thread</em> that took it: asking for it twice
/// from one thread succeeds both times, because the second ask is the owner asking. Two copies of
/// a program are two processes and therefore two threads, so a test that claims twice from the
/// same thread would be testing a situation that cannot happen and would see it succeed.
/// </para>
/// </remarks>
public class SingleInstanceTests
{
    private static string AName() => $"KeylegendTest.{Guid.NewGuid():N}";

    /// <summary>
    /// Stands in for one running copy: it claims on a thread of its own, holds the claim until
    /// told to let go, and lets go on that same thread — as a process would.
    /// </summary>
    private sealed class Copy : IDisposable
    {
        private readonly Thread _thread;
        private readonly ManualResetEventSlim _claimed = new(false);
        private readonly ManualResetEventSlim _letGo = new(false);

        public SingleInstance? Instance { get; private set; }
        public Exception? Failure { get; private set; }

        private Copy(string name, TimeSpan? patience, string? identity, Action<SingleInstance>? onClaimed)
        {
            _thread = new Thread(() =>
            {
                try
                {
                    Instance = SingleInstance.Claim(name, patience, identity);
                    onClaimed?.Invoke(Instance);
                }
                catch (Exception ex)
                {
                    Failure = ex;
                }
                finally
                {
                    _claimed.Set();
                }

                _letGo.Wait();
                Instance?.Dispose();
            })
            { IsBackground = true };

            _thread.Start();
        }

        /// <summary>Starts a copy and waits until it has finished trying to claim.</summary>
        public static Copy Started(
            string name, TimeSpan? patience = null, string? identity = null,
            Action<SingleInstance>? onClaimed = null)
        {
            var copy = new Copy(name, patience, identity, onClaimed);

            Assert.True(copy._claimed.Wait(TimeSpan.FromSeconds(10)),
                "The copy never finished claiming.");

            return copy;
        }

        /// <summary>Lets the copy release its claim, the way quitting would.</summary>
        public void LetGo()
        {
            _letGo.Set();
            _thread.Join(TimeSpan.FromSeconds(10));
        }

        public void Dispose()
        {
            LetGo();
            _claimed.Dispose();
            _letGo.Dispose();
        }
    }

    [Fact]
    public void TheFirstCopyGetsTheClaimAndReplacesNobody()
    {
        using var first = Copy.Started(AName());

        Assert.Null(first.Failure);
        Assert.NotNull(first.Instance);
        Assert.True(first.Instance.Owns);
        Assert.False(first.Instance.ReplacedAnother);
        Assert.False(first.Instance.HadToForce);
    }

    [Fact]
    public void ACopyThatHasGoneLeavesTheClaimFree()
    {
        var name = AName();

        Copy.Started(name).LetGo();

        using var second = Copy.Started(name);

        Assert.Null(second.Failure);

        // Nothing was running by the time this started, so there was nothing to replace.
        Assert.False(second.Instance!.ReplacedAnother);
    }

    /// <summary>
    /// The whole point: a second start asks the first to go, and the first hears it.
    /// </summary>
    [Fact]
    public void AStartAsksTheRunningCopyToMakeWay()
    {
        var name = AName();
        using var asked = new ManualResetEventSlim(false);
        using var stopping = new CancellationTokenSource();

        using var running = Copy.Started(
            name, identity: "older",
            onClaimed: claim => claim.WhenAskedToQuit(asked.Set, stopping.Token));

        // A different program: this one wants the keyboard, not the window. It asks but never
        // gets the claim, because the running copy is told to hold on to it. Asking is what this
        // test is about; the hand-over is the next one.
        using var newer = Copy.Started(name, TimeSpan.FromMilliseconds(200), identity: "newer");

        Assert.IsType<InvalidOperationException>(newer.Failure);
        Assert.True(asked.Wait(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken),
            "The running copy was never asked to quit.");
    }

    /// <summary>
    /// And when it does make way, the newer copy takes over and says that it replaced one.
    /// </summary>
    [Fact]
    public void TheNewerCopyTakesOverWhenTheRunningOneLeaves()
    {
        var name = AName();
        using var stopping = new CancellationTokenSource();

        Copy? running = null;

        // Quitting on being asked is what the application does, and it releases the claim on the
        // thread that took it — which is the only thread allowed to.
        running = Copy.Started(
            name, identity: "older",
            onClaimed: claim => claim.WhenAskedToQuit(() => running!.LetGo(), stopping.Token));

        using var newer = Copy.Started(name, TimeSpan.FromSeconds(10), identity: "newer");

        Assert.Null(newer.Failure);
        Assert.True(newer.Instance!.ReplacedAnother);
        Assert.False(newer.Instance.HadToForce, "It should not have come to killing anything.");
    }

    [Fact]
    public void ACopyThatIsClosingIsNotAskedToQuit()
    {
        var name = AName();
        using var asked = new ManualResetEventSlim(false);
        using var stopping = new CancellationTokenSource();

        using var running = Copy.Started(
            name, identity: "older",
            onClaimed: claim => claim.WhenAskedToQuit(asked.Set, stopping.Token));

        stopping.Cancel();

        Assert.False(asked.Wait(TimeSpan.FromMilliseconds(300), TestContext.Current.CancellationToken),
            "Cancelling the token must end the wait, not trigger the quit.");
    }

    /// <summary>
    /// The same program from the same place: the window is what the user wants, not a restart.
    /// Nothing is replaced, nothing is killed, and the running copy keeps the keyboard.
    /// </summary>
    [Fact]
    public void AnIdenticalCopyIsLeftAloneAndAskedForItsWindow()
    {
        var name = AName();
        using var shown = new ManualResetEventSlim(false);
        using var stopping = new CancellationTokenSource();

        using var running = Copy.Started(
            name, identity: "same",
            onClaimed: claim => claim.WhenAskedToShow(shown.Set, stopping.Token));

        using var second = Copy.Started(name, identity: "same");

        Assert.Null(second.Failure);
        Assert.False(second.Instance!.Owns, "It must stand down for a copy of itself.");
        Assert.False(second.Instance.ReplacedAnother);
        Assert.False(second.Instance.HadToForce);

        // The running copy still holds the claim, and is the one that shows a window.
        Assert.True(running.Instance!.Owns);

        second.Instance.AskRunningCopyToShow();

        Assert.True(shown.Wait(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken),
            "The running copy was never asked to show its window.");
    }

    /// <summary>
    /// The same executable at a different version is not the same program: an update replaces
    /// what it supersedes rather than handing it the window.
    /// </summary>
    [Fact]
    public void ADifferentVersionIsReplacedRatherThanAskedForItsWindow()
    {
        var name = AName();
        using var asked = new ManualResetEventSlim(false);
        using var stopping = new CancellationTokenSource();

        Copy? running = null;

        running = Copy.Started(
            name, identity: "1.0.0",
            onClaimed: claim => claim.WhenAskedToQuit(() => running!.LetGo(), stopping.Token));

        using var newer = Copy.Started(name, TimeSpan.FromSeconds(10), identity: "1.1.0");

        Assert.Null(newer.Failure);
        Assert.True(newer.Instance!.Owns);
        Assert.True(newer.Instance.ReplacedAnother);
    }

    /// <summary>
    /// A copy that has quit leaves nothing behind that would make the next start think an
    /// identical one is still there.
    /// </summary>
    [Fact]
    public void AfterAnIdenticalCopyQuitsTheNextStartOwnsTheKeyboard()
    {
        var name = AName();

        Copy.Started(name, identity: "same").LetGo();

        using var next = Copy.Started(name, identity: "same");

        Assert.Null(next.Failure);
        Assert.True(next.Instance!.Owns, "Nothing was running, so this one owns the keyboard.");
        Assert.False(next.Instance.ReplacedAnother);
    }

    /// <summary>
    /// The identity has to separate two paths and two versions, and be usable as part of a
    /// kernel object name — which a path with its backslashes is not.
    /// </summary>
    [Fact]
    public void TheFingerprintSeparatesPathsAndVersions()
    {
        var installed = SingleInstance.Fingerprint(@"c:\program files\keylegend\keylegend.exe|1.1.0");
        var built = SingleInstance.Fingerprint(@"d:\work\keylegend\keylegend.exe|1.1.0");
        var older = SingleInstance.Fingerprint(@"c:\program files\keylegend\keylegend.exe|1.0.0");

        Assert.NotEqual(installed, built);
        Assert.NotEqual(installed, older);

        // Same input, same answer: two copies of one program have to agree on it.
        Assert.Equal(installed,
            SingleInstance.Fingerprint(@"c:\program files\keylegend\keylegend.exe|1.1.0"));

        Assert.DoesNotContain(@"\", installed);
        Assert.NotEmpty(installed);
    }

    /// <summary>
    /// The process filter, which is the dangerous part. Razer's own SDK runs a process named
    /// after every application registered with it, so on this machine there can be a second
    /// <c>Keylegend.exe</c> that belongs to Razer. Matching by name alone would end it on every
    /// start and cut the very session the program is about to open.
    /// </summary>
    [Fact]
    public void OnlyProcessesFromTheSameFileCount()
    {
        var ours = Environment.ProcessPath;

        Assert.NotNull(ours);

        var found = SingleInstance.Own(ours, Environment.ProcessId);

        try
        {
            // This test process runs that executable and must never be in the list.
            Assert.DoesNotContain(Environment.ProcessId, found.Select(p => p.Id));

            // Anything that is in it has to be the very same file, not merely the same name.
            foreach (var process in found)
            {
                Assert.Equal(ours, process.MainModule?.FileName, ignoreCase: true);
            }
        }
        finally
        {
            foreach (var process in found)
            {
                process.Dispose();
            }
        }
    }

    [Fact]
    public void AnExecutableNobodyIsRunningMatchesNothing()
    {
        var absent = Path.Combine(Path.GetTempPath(), $"keylegend-absent-{Guid.NewGuid():N}.exe");

        Assert.Empty(SingleInstance.Own(absent, Environment.ProcessId));
    }

    [Fact]
    public void AClaimNeedsAName()
    {
        Assert.Throws<ArgumentException>(() => SingleInstance.Claim("  "));
        Assert.Throws<ArgumentNullException>(() => SingleInstance.Claim(null!));
    }
}
