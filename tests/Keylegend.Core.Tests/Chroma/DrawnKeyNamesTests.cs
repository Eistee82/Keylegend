using Keylegend.Chroma;
using Keylegend.Core.Devices;

namespace Keylegend.Core.Tests.Chroma;

/// <summary>
/// Turning the names in the vendor's drawing into the ids the lighting protocol uses.
/// </summary>
/// <remarks>
/// This is where a keyboard silently loses keys. The drawing calls the key beside Enter
/// <c>Extra1</c> on an ISO board and <c>Backslash</c> on an ANSI one, and the same drawn name can
/// mean different ids depending on which keys the board has at all. A name that resolves to
/// nothing produces a key that is drawn but never lights, and nothing in the program complains.
/// </remarks>
public class DrawnKeyNamesTests
{
    private static IReadOnlySet<string> Available(params string[] ids)
        => new HashSet<string>(ids, StringComparer.Ordinal);

    /// <summary>Letters, digits and function keys are the name with the prefix put back on.</summary>
    [Theory]
    [InlineData("Q", "Keyboard_Q")]
    [InlineData("7", "Keyboard_7")]
    [InlineData("F11", "Keyboard_F11")]
    public void NeedsNoTableForTheKeysThatAreNamedAfterThemselves(string drawn, string expected)
        => Assert.Equal(expected, DrawnKeyNames.Resolve(drawn, Available(expected)));

    /// <summary>
    /// The names that differ, and the reason the table exists at all.
    /// </summary>
    [Theory]
    [InlineData("Esc", "Keyboard_Escape")]
    [InlineData("Tilde", "Keyboard_GraveAccentAndTilde")]
    [InlineData("Dash", "Keyboard_MinusAndUnderscore")]
    [InlineData("Function", "Keyboard_RightGui")]
    [InlineData("NumPad", "Keyboard_NumLock")]
    public void TranslatesTheNamesThatDoNotMatch(string drawn, string expected)
        => Assert.Equal(expected, DrawnKeyNames.Resolve(drawn, Available(expected)));

    /// <summary>
    /// The num pad's digits are <c>Keyboard_Num7</c>, not <c>Keyboard_NumPad7</c>. Getting this
    /// wrong leaves the whole pad drawn and dark, because every one of its ids resolves to nothing.
    /// </summary>
    [Fact]
    public void TheNumPadDigitsKeepTheProtocolsSpelling()
    {
        var available = Available("Keyboard_Num7");

        Assert.Equal("Keyboard_Num7", DrawnKeyNames.Resolve("NumPad7", available));
    }

    /// <summary>
    /// The decisive case: one drawn name, two possible ids, and which one is right depends on the
    /// board. An ISO keyboard has the extra key beside Enter; an ANSI one has the backslash.
    /// </summary>
    [Fact]
    public void PicksTheIdTheBoardActuallyHas()
    {
        Assert.Equal(
            "Keyboard_NonUsBackslash",
            DrawnKeyNames.Resolve("Backslash", Available("Keyboard_NonUsBackslash")));

        Assert.Equal(
            "Keyboard_Backslash",
            DrawnKeyNames.Resolve("Backslash", Available("Keyboard_Backslash")));
    }

    /// <summary>A name whose id the board does not have resolves to nothing, rather than to a
    /// key that cannot light.</summary>
    [Fact]
    public void ResolvesToNothingRatherThanToAKeyTheBoardLacks()
    {
        Assert.Null(DrawnKeyNames.Resolve("Extra1", Available("Keyboard_A")));
        Assert.Null(DrawnKeyNames.Resolve("Q", Available("Keyboard_A")));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void HasNothingToSayAboutAnUnnamedKey(string? drawn)
        => Assert.Null(DrawnKeyNames.Resolve(drawn, Available("Keyboard_A")));

    /// <summary>
    /// Every id the table can produce has to be one the lighting protocol knows — otherwise the
    /// table would name a key that cannot be addressed, and the mistake would only show on the
    /// one keyboard that has that key.
    /// </summary>
    [Fact]
    public void EveryIdItCanProduceIsAnIdTheProtocolKnows()
    {
        var known = StandardKeyMatrix.Ids.ToHashSet(StringComparer.Ordinal);
        var everything = new HashSet<string>(known, StringComparer.Ordinal);
        var unknown = new List<string>();

        // Resolve is asked with every id available, so it returns whatever the table prefers.
        foreach (var drawn in DrawnKeyNames.Translated)
        {
            if (DrawnKeyNames.Resolve(drawn, everything) is { } id && !known.Contains(id))
            {
                unknown.Add($"{drawn} -> {id}");
            }
        }

        Assert.Empty(unknown);
    }
}
