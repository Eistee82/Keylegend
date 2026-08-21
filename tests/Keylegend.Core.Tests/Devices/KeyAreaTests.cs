using Keylegend.Core.Devices;

namespace Keylegend.Core.Tests.Devices;

public class KeyAreaTests
{
    private static KeyDefinition Key(IReadOnlyList<KeyArea>? parts = null)
        => new("Keyboard_Enter", 10, 20, 30, 19, 3, 14, null, parts);

    [Fact]
    public void AnOrdinaryKeyHasOneArea()
    {
        var areas = Key().Areas().ToArray();

        Assert.Single(areas);
        Assert.Equal(new KeyArea(10, 20, 30, 19), areas[0]);
    }

    [Fact]
    public void AComposedKeyReportsEveryArea()
    {
        var key = Key([new KeyArea(5, 1, 35, 19)]);

        var areas = key.Areas().ToArray();

        Assert.Equal(2, areas.Length);
        Assert.Equal(new KeyArea(10, 20, 30, 19), areas[0]);   // main area first
        Assert.Equal(new KeyArea(5, 1, 35, 19), areas[1]);
    }

    [Fact]
    public void APartOutsideTheCanvasIsRejected()
    {
        // Parts are validated like the main area, or a typo would draw off-screen unnoticed.
        var profile = ProfileWith(Key([new KeyArea(400, 20, 200, 19)]));

        Assert.Contains(DeviceProfileValidator.Validate(profile), p => p.Contains("outside the canvas"));
    }

    [Fact]
    public void APartWithNoSizeIsRejected()
    {
        var profile = ProfileWith(Key([new KeyArea(10, 1, 0, 19)]));

        Assert.Contains(DeviceProfileValidator.Validate(profile), p => p.Contains("non-positive size"));
    }

    [Fact]
    public void AValidComposedKeyPasses()
    {
        var profile = ProfileWith(Key([new KeyArea(5, 1, 35, 19)]));

        Assert.Empty(DeviceProfileValidator.Validate(profile));
    }

    [Fact]
    public void TheShippedProfileDrawsEnterAsOneKey()
    {
        // The upper half of the ISO Enter drives no LED on this model, so it is part of the
        // Enter key rather than a key of its own - otherwise the interface shows a key that
        // does not exist.
        var profile = DeviceProfileLoader.Load(TestPaths.ShippedProfile("razer-deathstalker-v2-de"));

        Assert.DoesNotContain(profile.Keys, k => k.Id == "Keyboard_Backslash");

        var enter = profile.Keys.Single(k => k.Id == "Keyboard_Enter");
        Assert.Equal(2, enter.Areas().Count());
    }

    private static DeviceProfile ProfileWith(params KeyDefinition[] keys)
        => new(1, "Test", "Test", "T1", "ISO-DE", "device.png",
            new Canvas(500, 200), new MatrixSize(6, 22), true, keys);
}
