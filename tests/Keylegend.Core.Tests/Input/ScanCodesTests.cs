using Keylegend.Core.Devices;
using Keylegend.Core.Input;

namespace Keylegend.Core.Tests.Input;

public class ScanCodesTests
{
    [Theory]
    [InlineData("Keyboard_Escape", 0x01)]
    [InlineData("Keyboard_A", 0x1E)]
    [InlineData("Keyboard_1", 0x02)]
    [InlineData("Keyboard_F1", 0x3B)]
    [InlineData("Keyboard_Num7", 0x47)]
    [InlineData("Keyboard_Space", 0x39)]
    public void ResolvesStandardKeys(string keyId, int expected)
    {
        Assert.True(ScanCodes.TryGet(keyId, out var code));
        Assert.Equal(expected, code);
    }

    [Theory]
    [InlineData("Keyboard_RightCtrl", 0xE01D)]
    [InlineData("Keyboard_ArrowUp", 0xE048)]
    [InlineData("Keyboard_Delete", 0xE053)]
    [InlineData("Keyboard_RightAlt", 0xE038)]
    public void ExtendedKeysCarryThePrefix(string keyId, int expected)
    {
        Assert.True(ScanCodes.TryGet(keyId, out var code));
        Assert.Equal(expected, code);
    }

    [Fact]
    public void ResolvesTheIsoOnlyKey()
    {
        // The extra key left of Y/Z exists only on ISO keyboards.
        Assert.True(ScanCodes.TryGet("Keyboard_NonUsBackslash", out var code));
        Assert.Equal(0x56, code);
    }

    /// <summary>
    /// Pause is the one key that arrives as an <c>E1</c> sequence. Its code must carry that
    /// prefix, or it collapses onto Num Lock, whose plain <c>0x45</c> it otherwise shares.
    /// </summary>
    [Fact]
    public void PauseDoesNotShareNumLocksCode()
    {
        Assert.True(ScanCodes.TryGet("Keyboard_PauseBreak", out var pause));
        Assert.True(ScanCodes.TryGet("Keyboard_NumLock", out var numLock));

        Assert.Equal(ScanCodes.PauseSequence, pause);
        Assert.Equal(0x45, numLock);
        Assert.NotEqual(pause, numLock);
    }

    [Fact]
    public void UnknownKeysReturnFalseRatherThanThrowing()
    {
        // A keyboard may legitimately have macro or media keys nothing can be typed with.
        Assert.False(ScanCodes.TryGet("Keyboard_Macro1", out _));
    }

    [Fact]
    public void EveryTypingKeyOnTheMeasuredKeyboardResolves()
    {
        // Catches the table and the measured keyboard drifting apart.
        var measured = MeasuredKeys.Load();

        var unresolved = measured
            .Where(k => k.ScanCode is null && !ScanCodes.TryGet(k.Id, out _))
            .Select(k => k.Id)
            .ToArray();

        Assert.True(
            unresolved.Length == 0,
            "Keys without a scan code: " + string.Join(", ", unresolved));
    }
}
