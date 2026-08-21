using Keylegend.Core.Devices;

namespace Keylegend.Core.Tests.Devices;

/// <summary>
/// Guards the choice of default profile against the regression that prompted it: with one
/// profile shipped, "the first file found" was the right one; with thirty-two it was a 60 %
/// layout, which left two thirds of a full-size keyboard dark.
/// </summary>
public class DeviceProfileLocatorTests : IDisposable
{
    private readonly string _root = Path.Combine(
        Path.GetTempPath(), "keylegend-locator-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }

        GC.SuppressFinalize(this);
    }

    [Fact]
    public void RecognisedKeyboardWinsOverEverythingElse()
    {
        // Deliberately stacked against the right answer: the rival sorts first, is verified,
        // and is larger. Only the USB id says otherwise, and that has to be enough.
        Write("aaa-rival", keys: 105, verified: true);
        Write("zzz-attached", keys: 61, verified: false, usb: ("1532", "0295"));

        var chosen = Choose(attached: [new UsbId("1532", "0295")]);

        Assert.Equal("zzz-attached", chosen);
    }

    [Fact]
    public void PreferredLayoutSeparatesVariantsOfTheSameModel()
    {
        Write("model-ansi", keys: 104, layout: "ANSI-US", usb: ("1532", "0295"));
        Write("model-iso", keys: 105, layout: "ISO-DE", usb: ("1532", "0295"));

        var chosen = Choose([new UsbId("1532", "0295")], preferredLayout: "ISO-DE");

        Assert.Equal("model-iso", chosen);
    }

    /// <summary>
    /// Found by running the packaging on a machine with no Razer keyboard: the choice landed on
    /// a named model, and the window then announced a "Razer BlackWidow V4" to somebody who does
    /// not own one. A generic profile claims only what is actually known.
    /// </summary>
    [Fact]
    public void GenericProfileWinsOverANamedModelWhenNothingIsRecognised()
    {
        Write("aaa-vendor-model", keys: 109, verified: true, vendor: "Razer");
        Write("zzz-generic", keys: 104, verified: false, vendor: "Generic");

        Assert.Equal("zzz-generic", Choose(attached: []));
    }

    /// <summary>
    /// But recognition still beats it: a model identified by its USB ids is not a guess.
    /// </summary>
    [Fact]
    public void RecognisedModelStillBeatsAGenericProfile()
    {
        Write("aaa-generic", keys: 104, vendor: "Generic");
        Write("zzz-recognised", keys: 61, vendor: "Razer", usb: ("1532", "0295"));

        Assert.Equal("zzz-recognised", Choose([new UsbId("1532", "0295")]));
    }

    /// <summary>
    /// And the physical shape outranks it: drawing the wrong keyboard is worse than naming one
    /// vaguely.
    /// </summary>
    [Fact]
    public void LayoutStillBeatsAGenericProfile()
    {
        Write("aaa-generic-wrong-layout", keys: 104, layout: "ANSI-US", vendor: "Generic");
        Write("zzz-model-right-layout", keys: 105, layout: "ISO-DE", vendor: "Razer");

        Assert.Equal("zzz-model-right-layout", Choose(attached: [], preferredLayout: "ISO-DE"));
    }

    [Fact]
    public void VerifiedWinsWhenNothingIsRecognised()
    {
        Write("aaa-generated", keys: 105, verified: false, vendor: "Generic");
        Write("zzz-verified", keys: 61, verified: true, vendor: "Generic");

        Assert.Equal("zzz-verified", Choose(attached: []));
    }

    /// <summary>
    /// Guessing too large is the cheaper mistake: a full-size profile on a smaller board draws
    /// keys that are not there, while a compact profile leaves real keys dark.
    /// </summary>
    [Fact]
    public void LargerProfileWinsAmongEqualCandidates()
    {
        Write("aaa-sixty", keys: 61);
        Write("zzz-fullsize", keys: 105);

        Assert.Equal("zzz-fullsize", Choose(attached: []));
    }

    [Fact]
    public void IgnoresAUsbIdThatMatchesNothingAttached()
    {
        Write("aaa-other-vendor", keys: 61, usb: ("046D", "C52B"));
        Write("zzz-verified", keys: 61, verified: true);

        Assert.Equal("zzz-verified", Choose([new UsbId("1532", "0295")]));
    }

    /// <summary>
    /// A profile that will not parse must not take the application down with it — the next one
    /// may be perfectly good.
    /// </summary>
    [Fact]
    public void SkipsAnUnreadableProfile()
    {
        Directory.CreateDirectory(Path.Combine(_root, "devices", "aaa-broken"));
        File.WriteAllText(Path.Combine(_root, "devices", "aaa-broken", "device.json"), "{ not json");
        Write("zzz-good", keys: 105);

        Assert.Equal("zzz-good", Choose(attached: []));
    }

    [Fact]
    public void ReturnsNullWhereThereAreNoProfiles()
    {
        Directory.CreateDirectory(Path.Combine(_root, "devices"));

        Assert.Null(Choose(attached: []));
    }

    [Theory]
    [InlineData("1532", "0295", true)]
    [InlineData("1532", "295", true)]      // leading zeroes are not significant
    [InlineData("1532", "0296", false)]
    [InlineData("046D", "0295", false)]
    public void UsbIdsCompareOnValueNotSpelling(string vendor, string product, bool expected)
    {
        var attached = new UsbId("1532", "0295");

        Assert.Equal(expected, new UsbId(vendor, product).Matches(attached));
    }

    [Fact]
    public void UsbIdsCompareCaseInsensitively()
    {
        Assert.True(new UsbId("046d", "c52b").Matches(new UsbId("046D", "C52B")));
    }

    // ------------------------------------------------------------------------------------

    /// <summary>
    /// Runs the ranking over the profiles written into the temporary folder. The locator finds
    /// its own directory by walking up from the binaries, so the folder is passed explicitly
    /// here by asking it to rank exactly what this test wrote.
    /// </summary>
    private string? Choose(IReadOnlyList<UsbId> attached, string? preferredLayout = null)
    {
        var paths = Directory.Exists(Path.Combine(_root, "devices"))
            ? Directory
                .EnumerateFiles(Path.Combine(_root, "devices"), "device.json", SearchOption.AllDirectories)
                .OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
                .ToArray()
            : [];

        var chosen = DeviceProfileLocator.ChooseFrom(paths, attached, preferredLayout);

        return chosen is null ? null : Path.GetFileName(Path.GetDirectoryName(chosen));
    }

    private void Write(
        string folder,
        int keys,
        bool verified = false,
        string layout = "ISO-DE",
        (string Vendor, string Product)? usb = null,
        string vendor = "Test")
    {
        var directory = Path.Combine(_root, "devices", folder);
        Directory.CreateDirectory(directory);

        var entries = string.Join(",", Enumerable.Range(0, keys).Select(i =>
            $$"""{"id":"Keyboard_K{{i}}","x":{{i * 20}},"y":0,"width":19,"height":19,"row":0,"column":0}"""));

        // "usb": null is a valid profile, so the field can always be written and no conditional
        // line assembly is needed.
        var usbJson = usb is null
            ? "null"
            : $$"""{ "vendorId": "{{usb.Value.Vendor}}", "productId": "{{usb.Value.Product}}" }""";

        File.WriteAllText(
            Path.Combine(directory, "device.json"),
            $$"""
            {
              "formatVersion": 1,
              "name": "{{folder}}",
              "vendor": "{{vendor}}",
              "model": "T1",
              "physicalLayout": "{{layout}}",
              "usb": {{usbJson}},
              "canvas": { "width": 100000, "height": 200 },
              "matrix": { "rows": 6, "columns": 22 },
              "verified": {{(verified ? "true" : "false")}},
              "keys": [{{entries}}]
            }
            """);
    }
}
