using Keylegend.Core.Input;

namespace Keylegend.Core.Tests.Input;

public class CharacterClassifierTests
{
    [Theory]
    [InlineData("0")]
    [InlineData("7")]
    public void DigitsAreDigits(string character)
        => Assert.Equal(KeyCategory.Digit, CharacterClassifier.Classify(character));

    [Theory]
    [InlineData("a")]
    [InlineData("z")]
    [InlineData("ö")]
    [InlineData("ß")]
    public void LowercaseLettersAreLowercase(string character)
        => Assert.Equal(KeyCategory.Lowercase, CharacterClassifier.Classify(character));

    [Theory]
    [InlineData("A")]
    [InlineData("Ö")]
    public void UppercaseLettersAreUppercase(string character)
        => Assert.Equal(KeyCategory.Uppercase, CharacterClassifier.Classify(character));

    [Theory]
    [InlineData("+")]
    [InlineData("#")]
    [InlineData("€")]
    [InlineData("|")]
    [InlineData("@")]
    [InlineData(" ")]
    public void PunctuationAndSignsAreSymbols(string character)
        => Assert.Equal(KeyCategory.Symbol, CharacterClassifier.Classify(character));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void NothingProducedMeansUnassigned(string? character)
        => Assert.Equal(KeyCategory.Unassigned, CharacterClassifier.Classify(character));

    [Theory]
    [InlineData("")]  // Escape
    [InlineData("\r")]
    [InlineData("\t")]
    [InlineData("\b")]
    public void ControlCharactersAreControlKeys(string character)
        => Assert.Equal(KeyCategory.Control, CharacterClassifier.Classify(character));

    [Fact]
    public void DeadKeysWinOverWhateverTheyWouldProduce()
    {
        // The circumflex key produces nothing on its own - it modifies the next keystroke.
        Assert.Equal(KeyCategory.DeadKey, CharacterClassifier.Classify("^", isDeadKey: true));
    }

    [Fact]
    public void HandlesCharactersOutsideTheBasicPlane()
        => Assert.Equal(KeyCategory.Symbol, CharacterClassifier.Classify("\U0001F600"));
}
