using Keylegend.Core.Input;

namespace Keylegend.Core.Tests.Input;

public class NumpadKeysTests
{
    [Theory]
    [InlineData("Keyboard_Num0", 0x60)]
    [InlineData("Keyboard_Num5", 0x65)]
    [InlineData("Keyboard_Num9", 0x69)]
    [InlineData("Keyboard_NumPeriodAndDelete", 0x6E)]
    public void WithNumLockTheKeysAreDigits(string keyId, int expected)
    {
        Assert.True(NumpadKeys.TryGetVirtualKey(keyId, numLock: true, out var virtualKey));
        Assert.Equal(expected, virtualKey);
    }

    [Theory]
    [InlineData("Keyboard_Num0", 0x2D)]   // Insert
    [InlineData("Keyboard_Num1", 0x23)]   // End
    [InlineData("Keyboard_Num8", 0x26)]   // Up
    [InlineData("Keyboard_NumPeriodAndDelete", 0x2E)]  // Delete
    public void WithoutNumLockTheKeysNavigate(string keyId, int expected)
    {
        Assert.True(NumpadKeys.TryGetVirtualKey(keyId, numLock: false, out var virtualKey));
        Assert.Equal(expected, virtualKey);
    }

    [Fact]
    public void TheCentreKeyHasNoFunctionWithoutNumLock()
    {
        // With Num Lock off the "5" reports "clear", which does nothing - so it must not light up.
        Assert.True(NumpadKeys.TryGetVirtualKey("Keyboard_Num5", numLock: false, out var virtualKey));
        Assert.Null(virtualKey);
    }

    [Theory]
    [InlineData("Keyboard_NumSlash", 0x6F)]
    [InlineData("Keyboard_NumAsterisk", 0x6A)]
    [InlineData("Keyboard_NumMinus", 0x6D)]
    [InlineData("Keyboard_NumPlus", 0x6B)]
    public void OperatorsAreUnaffectedByNumLock(string keyId, int expected)
    {
        Assert.True(NumpadKeys.TryGetVirtualKey(keyId, numLock: true, out var on));
        Assert.True(NumpadKeys.TryGetVirtualKey(keyId, numLock: false, out var off));

        Assert.Equal(expected, on);
        Assert.Equal(expected, off);
    }

    [Fact]
    public void NonNumpadKeysAreNotClaimed()
    {
        Assert.False(NumpadKeys.TryGetVirtualKey("Keyboard_A", numLock: true, out _));
        Assert.False(NumpadKeys.IsNumpadKey("Keyboard_A"));
    }

    [Fact]
    public void EveryNumpadKeyInTheShippedProfileIsCovered()
    {
        // Catches a profile listing a number pad key the table does not know about.
        var measured = MeasuredKeys.Load();

        var missing = measured
            .Where(k => k.Id.StartsWith("Keyboard_Num", StringComparison.Ordinal))
            .Where(k => k.Id != "Keyboard_NumLock")     // the toggle itself is not a pad key
            .Where(k => !NumpadKeys.IsNumpadKey(k.Id))
            .Select(k => k.Id)
            .ToArray();

        Assert.True(missing.Length == 0, "Number pad keys missing from the table: " + string.Join(", ", missing));
    }
}
