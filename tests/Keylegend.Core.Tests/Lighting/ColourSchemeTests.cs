using Keylegend.Core.Input;
using Keylegend.Core.Lighting;
using Keylegend.Core.Shortcuts;

namespace Keylegend.Core.Tests.Lighting;

public class ColourSchemeTests
{
    [Fact]
    public void EveryVisibleCategoryRunsAtFullBrightness()
    {
        // Anything meant to be seen should drive at least one channel to 255. Dimming belongs
        // to the brightness setting, not to the palette, and a colour that quietly sits at
        // 80 % is easy to introduce and hard to spot.
        var dimOnPurpose = new[] { KeyCategory.Unassigned };

        foreach (var (category, colour) in ColourScheme.Default.Categories)
        {
            if (dimOnPurpose.Contains(category))
            {
                continue;
            }

            Assert.True(
                ColourScheme.RunsAtFullBrightness(colour),
                $"{category} is {colour}; no channel reaches 255.");
        }
    }

    [Fact]
    public void EveryFunctionGroupRunsAtFullBrightness()
    {
        foreach (var (group, colour) in ColourScheme.Default.Groups)
        {
            Assert.True(
                ColourScheme.RunsAtFullBrightness(colour),
                $"{group} is {colour}; no channel reaches 255.");
        }
    }

    [Fact]
    public void LockKeysAreBrightWhenOnAndDimWhenOff()
    {
        // Here the dimness carries the meaning, so it is deliberate rather than an oversight.
        var scheme = ColourScheme.Default;

        foreach (var pair in new[] { scheme.NumLock, scheme.CapsLock, scheme.ScrollLock })
        {
            Assert.True(ColourScheme.RunsAtFullBrightness(pair.On), $"{pair.On} should be full.");
            Assert.False(ColourScheme.RunsAtFullBrightness(pair.Off), $"{pair.Off} should be dim.");
        }
    }

    [Fact]
    public void UnassignedIsCompletelyOff()
        => Assert.Equal(RgbColor.Off, ColourScheme.Default.For(KeyCategory.Unassigned));

    [Fact]
    public void BrightnessStartsAtFull()
        => Assert.Equal(1.0, ColourScheme.Default.Brightness);

    [Fact]
    public void EveryCategoryAndGroupHasAColour()
    {
        // A missing entry would silently render as black, which looks like a broken key.
        foreach (var category in Enum.GetValues<KeyCategory>())
        {
            Assert.True(
                ColourScheme.Default.Categories.ContainsKey(category),
                $"No colour defined for {category}.");
        }

        foreach (var group in Enum.GetValues<FunctionGroup>())
        {
            Assert.True(
                ColourScheme.Default.Groups.ContainsKey(group),
                $"No colour defined for {group}.");
        }
    }

    [Fact]
    public void CategoriesAreFarEnoughApartToTellApart()
    {
        // Not merely different - visibly different. The first palette had digits, lowercase
        // and uppercase as three shades of blue about 85 apart, which read as one colour on
        // the keyboard. Being unable to distinguish them defeats the point of the program.
        var visible = ColourScheme.Default.Categories
            .Where(e => e.Key != KeyCategory.Unassigned)
            .ToArray();

        foreach (var a in visible)
        {
            foreach (var b in visible)
            {
                if (a.Key >= b.Key)
                {
                    continue;
                }

                var distance = ColourScheme.Distance(a.Value, b.Value);

                Assert.True(
                    distance >= ColourScheme.MinimumCategoryDistance,
                    $"{a.Key} {a.Value} and {b.Key} {b.Value} are only {distance:N0} apart; " +
                    $"at least {ColourScheme.MinimumCategoryDistance} is needed.");
            }
        }
    }

    [Fact]
    public void ShiftChangesTheHueRatherThanTheShade()
    {
        // Lowercase and uppercase are what a user switches between constantly, so they must be
        // the easiest pair of all to tell apart.
        var lower = ColourScheme.Default.For(KeyCategory.Lowercase);
        var upper = ColourScheme.Default.For(KeyCategory.Uppercase);

        Assert.True(
            ColourScheme.Distance(lower, upper) >= 250,
            $"lowercase {lower} and uppercase {upper} are too close.");
    }

    [Fact]
    public void FunctionGroupsAreFarEnoughApartToTellApart()
    {
        var groups = ColourScheme.Default.Groups.ToArray();

        // Every hue has to be a hue. One group may be white — Navigation is — and white is told
        // apart from any saturated colour; what cannot be told apart is white from nearly-white.
        var pale = groups
            .Where(g => ColourScheme.Saturation(g.Value) > 0.02
                     && ColourScheme.Saturation(g.Value) < ColourScheme.MinimumSaturation)
            .Select(g => $"{g.Key} {g.Value} at saturation {ColourScheme.Saturation(g.Value):N2}")
            .ToArray();

        Assert.True(
            pale.Length == 0,
            "Neither white nor a colour, so it reads as a tinted white on a lit keycap:"
            + Environment.NewLine + "  " + string.Join(Environment.NewLine + "  ", pale));

        foreach (var a in groups)
        {
            foreach (var b in groups)
            {
                if (a.Key >= b.Key)
                {
                    continue;
                }

                var white = ColourScheme.Saturation(a.Value) <= 0.02
                         || ColourScheme.Saturation(b.Value) <= 0.02;

                if (white)
                {
                    // A grey against a hue: the difference is the saturation, and the check above
                    // has already established that both are at one end of it.
                    continue;
                }

                var apart = ColourScheme.HueDistance(a.Value, b.Value);

                Assert.True(
                    apart >= ColourScheme.MinimumHueSeparation,
                    $"{a.Key} {a.Value} and {b.Key} {b.Value} are only {apart:N0} degrees apart.");
            }
        }
    }

    /// <summary>
    /// Why the group palette is checked by hue and saturation rather than by distance.
    /// </summary>
    /// <remarks>
    /// The pale blue that Tools used to be passed the distance check comfortably and was reported
    /// from the hardware as looking white. The orange that replaced it fails that same check
    /// against yellow and is easy to tell apart. So the measure was the thing that was wrong, and
    /// this records both halves of the evidence so the lesson is not undone by a tidy-up.
    /// </remarks>
    [Fact]
    public void DistanceAloneWouldHaveAcceptedThePaleBlueAndRejectedTheOrange()
    {
        var white = new RgbColor(255, 255, 255);
        var paleBlue = new RgbColor(128, 128, 255);
        var yellow = new RgbColor(255, 255, 0);
        var orange = new RgbColor(255, 140, 0);

        // What distance says: the unreadable pair is fine, the readable one is not.
        Assert.True(ColourScheme.Distance(paleBlue, white) >= ColourScheme.MinimumCategoryDistance);
        Assert.True(ColourScheme.Distance(orange, yellow) < ColourScheme.MinimumCategoryDistance);

        // What saturation and hue say, which is the other way round and matches the keyboard.
        Assert.True(ColourScheme.Saturation(paleBlue) < ColourScheme.MinimumSaturation);
        Assert.True(ColourScheme.Saturation(orange) >= ColourScheme.MinimumSaturation);
        Assert.True(ColourScheme.HueDistance(orange, yellow) >= ColourScheme.MinimumHueSeparation);
    }
}
