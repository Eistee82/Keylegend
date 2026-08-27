using Keylegend.App;

namespace Keylegend.Core.Tests.App;

/// <summary>
/// The self-check a published copy runs on itself, reached through <c>--verify</c>.
/// </summary>
/// <remarks>
/// <para>
/// This is what <c>tools/build-release.ps1</c> asks the packaged tree before it wraps it up, and
/// it is the last thing standing between a packaging mistake and a release. Two such mistakes
/// have happened here, and neither showed up in a build or in a test of the build output: one
/// executable overwriting another, and data the project files did not carry into the package.
/// </para>
/// <para>
/// So the switch that catches them is worth checking itself. What cannot be checked from here is
/// the case it exists for — a copy that is missing something — because this test runs against the
/// build output, which by definition has everything. What is checked is that it reads its
/// arguments correctly, writes what it found, and answers through its exit code.
/// </para>
/// </remarks>
public class VerificationTests
{
    private static string ATempPath()
        => Path.Combine(Path.GetTempPath(), $"keylegend-verify-{Guid.NewGuid():N}.txt");

    [Fact]
    public void TheSwitchIsRecognised()
    {
        Assert.True(Verification.Requested(["--verify"], out _));
        Assert.True(Verification.Requested(["--VERIFY"], out _));
        Assert.True(Verification.Requested(["--minimized", "--verify"], out _));
    }

    [Fact]
    public void WithoutTheSwitchNothingIsRequested()
    {
        Assert.False(Verification.Requested([], out var path));
        Assert.Null(path);

        Assert.False(Verification.Requested(["--minimized"], out _));

        // Near misses must not count: this decides whether the program opens a window at all.
        Assert.False(Verification.Requested(["verify"], out _));
        Assert.False(Verification.Requested(["--verifying"], out _));
    }

    [Fact]
    public void TheWordAfterTheSwitchIsWhereTheReportGoes()
    {
        Assert.True(Verification.Requested(["--verify", @"c:\tmp\report.txt"], out var path));
        Assert.Equal(@"c:\tmp\report.txt", path);
    }

    /// <summary>
    /// A switch is not a path. Reading one as the other would have the check write its findings
    /// to a file called <c>--minimized</c> and say nothing about it.
    /// </summary>
    [Fact]
    public void AnotherSwitchIsNotMistakenForAPath()
    {
        Assert.True(Verification.Requested(["--verify", "--minimized"], out var path));
        Assert.Null(path);

        Assert.True(Verification.Requested(["--verify"], out var none));
        Assert.Null(none);
    }

    /// <summary>
    /// The build output carries everything, so the check has to pass on it. If this ever fails,
    /// the packaging is wrong or the check is.
    /// </summary>
    [Fact]
    public void ThisCopyPassesItsOwnCheck()
    {
        var report = ATempPath();

        try
        {
            Assert.Equal(0, Verification.Run(report));

            var written = File.ReadAllText(report);

            Assert.Contains("sound", written, StringComparison.OrdinalIgnoreCase);

            // The two counts are the point of the report: they are what a packaging mistake
            // changes, and what a human reads to see that it did not.
            Assert.Contains("shipped profiles", written, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("languages", written, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            File.Delete(report);
        }
    }

    /// <summary>
    /// The exit code is the answer; the file is only there to say what was wrong. So a path that
    /// cannot be written must not turn a sound copy into a failed one.
    /// </summary>
    [Fact]
    public void AReportThatCannotBeWrittenDoesNotChangeTheVerdict()
    {
        var impossible = Path.Combine(
            Path.GetTempPath(), $"keylegend-absent-{Guid.NewGuid():N}", "nested", "report.txt");

        Assert.Equal(0, Verification.Run(impossible));
        Assert.False(File.Exists(impossible));
    }

    [Fact]
    public void WithNoPathItStillAnswers()
    {
        Assert.Equal(0, Verification.Run(null));
    }
}
