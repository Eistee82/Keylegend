using Keylegend.Core.Configuration;
using Keylegend.Core.Input;
using Keylegend.Core.Lighting;
using Keylegend.Core.Profiles;
using Keylegend.Core.Shortcuts;

namespace Keylegend.Core.Tests.Configuration;

/// <summary>
/// How colours are stored, and why only the changed ones may be.
/// </summary>
/// <remarks>
/// A settings file that states every colour pins every colour, including the ones nobody chose.
/// Improving the shipped palette would then reach nobody who has ever run the program. It is the
/// same fault the shortcut sets can have, and it is not theoretical: it surfaced when the Tools
/// colour was changed because the previous one looked white on the hardware, and the new colour
/// did not appear.
/// </remarks>
public class StoredColourTests
{
    private static StoredSettings Save(ColourScheme scheme)
        => StoredSettings.From(
            idleTimeout: TimeSpan.FromSeconds(60),
            handBackWhenIdle: true,
            scheme: scheme,
            startWithWindows: false,
            useApplicationProfiles: true,
            language: "Automatic",
            profiles: new ProfileLibrary(shipped: [], userProfiles: [], overrides: [], hidden: []),
            shortcuts: null);

    [Fact]
    public void AnUntouchedPaletteIsNotWrittenAtAll()
    {
        var stored = Save(ColourScheme.Default);

        Assert.Empty(stored.CategoryColours);
        Assert.Empty(stored.GroupColours);
    }

    [Fact]
    public void AChangedColourIsWrittenAndOnlyThatOne()
    {
        var groups = new Dictionary<FunctionGroup, RgbColor>(ColourScheme.Default.Groups)
        {
            [FunctionGroup.Edit] = new(10, 20, 30)
        };

        var stored = Save(ColourScheme.Default with { Groups = groups });

        Assert.Equal(["Edit"], stored.GroupColours.Keys);
        Assert.Empty(stored.CategoryColours);
    }

    [Fact]
    public void AChangedColourComesBack()
    {
        var chosen = new RgbColor(10, 20, 30);
        var groups = new Dictionary<FunctionGroup, RgbColor>(ColourScheme.Default.Groups)
        {
            [FunctionGroup.Edit] = chosen
        };

        var stored = Save(ColourScheme.Default with { Groups = groups });

        Assert.Equal(chosen, stored.ToColourScheme().Groups[FunctionGroup.Edit]);
    }

    /// <summary>
    /// The migration. A format 2 file listed everything, so an entry equal to the palette of the
    /// time has to be read as a default rather than as a decision — otherwise it pins the old
    /// palette forever.
    /// </summary>
    [Fact]
    public void AFormat2FileDoesNotPinTheOldPalette()
    {
        var stored = new StoredSettings
        {
            FormatVersion = 2,
            GroupColours = new Dictionary<string, string>
            {
                // Exactly what the palette was then: this must not survive.
                ["Tools"] = "#8080FF",

                // Not the palette, so somebody set it: this must survive.
                ["Edit"] = "#123456"
            }
        };

        var scheme = stored.ToColourScheme();

        Assert.Equal(ColourScheme.Default.Groups[FunctionGroup.Tools], scheme.Groups[FunctionGroup.Tools]);
        Assert.Equal(new RgbColor(0x12, 0x34, 0x56), scheme.Groups[FunctionGroup.Edit]);
    }

    /// <summary>
    /// A current file means what it says: every entry in it is a decision, including one that
    /// happens to equal an old default.
    /// </summary>
    [Fact]
    public void ACurrentFileIsTakenLiterally()
    {
        var stored = new StoredSettings
        {
            FormatVersion = StoredSettings.SupportedFormatVersion,
            GroupColours = new Dictionary<string, string> { ["Tools"] = "#8080FF" }
        };

        Assert.Equal(
            new RgbColor(0x80, 0x80, 0xFF),
            stored.ToColourScheme().Groups[FunctionGroup.Tools]);
    }

    /// <summary>Categories migrate the same way.</summary>
    [Fact]
    public void CategoriesMigrateTheSameWay()
    {
        var stored = new StoredSettings
        {
            FormatVersion = 2,
            CategoryColours = new Dictionary<string, string>
            {
                ["Symbol"] = "#FFFF00",     // the palette of the time
                ["Digit"] = "#ABCDEF"       // a decision
            }
        };

        var scheme = stored.ToColourScheme();

        Assert.Equal(ColourScheme.Default.Categories[KeyCategory.Symbol], scheme.Categories[KeyCategory.Symbol]);
        Assert.Equal(new RgbColor(0xAB, 0xCD, 0xEF), scheme.Categories[KeyCategory.Digit]);
    }
}
