using Keylegend.Core.Input;

namespace Keylegend.Core.Tests.Input;

public class KeyboardStateTests
{
    private static KeyboardState With(ModifierKeys modifiers)
        => new(modifiers, new LockStates(false, false, false));

    [Fact]
    public void ShiftIsEitherSide()
    {
        Assert.True(With(ModifierKeys.LeftShift).Shift);
        Assert.True(With(ModifierKeys.RightShift).Shift);
        Assert.False(KeyboardState.Empty.Shift);
    }

    [Fact]
    public void RightAltMeansAltGr()
    {
        Assert.True(With(ModifierKeys.RightAlt).AltGr);
        Assert.False(With(ModifierKeys.LeftAlt).AltGr);
    }

    [Fact]
    public void AltGrTakesPrecedenceOverCtrlAndAlt()
    {
        // Windows reports AltGr as Ctrl + right Alt. Without this rule the Ctrl shortcut
        // layer would appear whenever the user pressed AltGr.
        var altGr = With(ModifierKeys.RightAlt | ModifierKeys.LeftCtrl);

        Assert.True(altGr.AltGr);
        Assert.False(altGr.Ctrl);
        Assert.False(altGr.Alt);
    }

    [Fact]
    public void CtrlPlusLeftAltIsNotAltGr()
    {
        // The user really pressed both, so the Ctrl+Alt shortcut set applies.
        var combination = With(ModifierKeys.LeftCtrl | ModifierKeys.LeftAlt);

        Assert.False(combination.AltGr);
        Assert.True(combination.Ctrl);
        Assert.True(combination.Alt);
    }

    [Fact]
    public void ShiftAloneDoesNotFilter()
    {
        // Shift changes which character a key produces; it must not blank the keyboard.
        Assert.False(With(ModifierKeys.LeftShift).HasFilteringModifier);
    }

    [Theory]
    [InlineData(ModifierKeys.RightAlt)]
    [InlineData(ModifierKeys.LeftCtrl)]
    [InlineData(ModifierKeys.LeftAlt)]
    [InlineData(ModifierKeys.LeftWin)]
    public void FilteringModifiersFilter(ModifierKeys modifier)
        => Assert.True(With(modifier).HasFilteringModifier);

    [Fact]
    public void NumpadTypesDigitsWithNumLockOnAndNoShift()
    {
        var state = new KeyboardState(ModifierKeys.None, new LockStates(NumLock: true, false, false));

        Assert.True(state.NumpadDigitsActive);
    }

    [Fact]
    public void ShiftSuspendsNumLock()
    {
        // Holding Shift with Num Lock on turns the pad back into navigation keys.
        var state = new KeyboardState(ModifierKeys.LeftShift, new LockStates(NumLock: true, false, false));

        Assert.False(state.NumpadDigitsActive);
    }

    [Fact]
    public void ShiftDoesNotFlipTheNumpadTheOtherWay()
    {
        // With Num Lock off, Shift must leave the pad as navigation - that is what makes
        // Shift plus the pad's arrows select text instead of typing numbers.
        var state = new KeyboardState(ModifierKeys.LeftShift, new LockStates(NumLock: false, false, false));

        Assert.False(state.NumpadDigitsActive);
    }

    [Fact]
    public void NumpadStaysNavigationWithNumLockOff()
    {
        var state = new KeyboardState(ModifierKeys.None, new LockStates(NumLock: false, false, false));

        Assert.False(state.NumpadDigitsActive);
    }

    [Fact]
    public void LocksAreNotModifiers()
    {
        var state = new KeyboardState(ModifierKeys.None, new LockStates(true, true, true));

        Assert.False(state.HasFilteringModifier);
        Assert.True(state.Locks.CapsLock);
    }
}
