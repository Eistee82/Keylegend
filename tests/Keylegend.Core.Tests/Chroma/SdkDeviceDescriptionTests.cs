using Keylegend.Chroma;
using Keylegend.Core.Input;

namespace Keylegend.Core.Tests.Chroma;

/// <summary>
/// Guards the reader for the description the lighting service writes for the attached keyboard.
/// The samples below are trimmed copies of a real file, keeping the shapes that matter.
/// </summary>
public class SdkDeviceDescriptionTests
{
    private const string DeathStalker = """
        {
          "func": "AddDevice",
          "param": {
            "category": "keyboard",
            "productName": "Razer DeathStalker V2",
            "pid": 661,
            "ledConfig": {
              "VID": 5426,
              "PID": 661,
              "Layout": 3,
              "MatrixMaxRow": 6,
              "MatrixMaxCol": 22,
              "LedInputMap": [
                { "InputData": [1, 0],  "InputType": "kbd", "MatrixPos": [0, 0] },
                { "InputData": [30, 0], "InputType": "kbd", "MatrixPos": [3, 2] },
                { "InputData": [82, 2], "InputType": "kbd", "MatrixPos": [1, 15] },
                { "InputData": [1, 16], "InputType": "dkm", "MatrixPos": [5, 12] },
                { "InputData": [29, 4], "InputType": "kbd", "MatrixPos": [0, 17] }
              ]
            }
          }
        }
        """;

    [Fact]
    public void ReadsNameAndIdentity()
    {
        var device = SdkDeviceDescription.Read(DeathStalker);

        Assert.NotNull(device);
        Assert.Equal("Razer DeathStalker V2", device.ProductName);
        Assert.Equal(5426, device.VendorId);
        Assert.Equal(661, device.ProductId);
        Assert.Equal(6, device.MatrixRows);
        Assert.Equal(22, device.MatrixColumns);
    }

    /// <summary>
    /// The layout id is what separates the German board from the US one. Verified against the
    /// attached hardware: a German DeathStalker reports 3.
    /// </summary>
    [Fact]
    public void ReadsTheLayoutId()
    {
        var device = SdkDeviceDescription.Read(DeathStalker);

        Assert.Equal(3, device!.LayoutId);
    }

    [Fact]
    public void ReadsPlainAndExtendedKeys()
    {
        var device = SdkDeviceDescription.Read(DeathStalker);

        Assert.Contains(new SdkKey(1, false, 0, 0), device!.Keys);
        Assert.Contains(new SdkKey(30, false, 3, 2), device.Keys);
        Assert.Contains(new SdkKey(82, true, 1, 15), device.Keys);
    }

    /// <summary>
    /// fn reaches the file as a vendor key with the same scancode as Escape. Reading it as one
    /// would put Escape in the bottom row.
    /// </summary>
    [Fact]
    public void SkipsVendorKeys()
    {
        var device = SdkDeviceDescription.Read(DeathStalker);

        Assert.DoesNotContain(device!.Keys, k => k.Row == 5 && k.Column == 12);
        Assert.DoesNotContain(device.Keys, k => k is { Scancode: 1, Row: 5 });
    }

    /// <summary>Pause arrives with the E1 flag and is neither plain nor E0-extended.</summary>
    [Fact]
    public void SkipsTheE1Sequence()
    {
        var device = SdkDeviceDescription.Read(DeathStalker);

        Assert.DoesNotContain(device!.Keys, k => k is { Scancode: 29, Row: 0, Column: 17 });
        Assert.Contains(
            device.Keys,
            k => k is { Extended: false, Row: 0, Column: 17 } && k.Scancode == ScanCodes.PauseSequence);
    }

    [Fact]
    public void IgnoresDevicesThatAreNotKeyboards()
    {
        const string mouse = """
            { "param": { "category": "mouse", "productName": "Razer Viper",
              "ledConfig": { "VID": 5426, "PID": 100 } } }
            """;

        Assert.Null(SdkDeviceDescription.Read(mouse));
    }

    [Fact]
    public void IgnoresAnythingWithoutALedConfig()
    {
        Assert.Null(SdkDeviceDescription.Read("""{ "param": { "category": "keyboard" } }"""));
        Assert.Null(SdkDeviceDescription.Read("{}"));
    }

    /// <summary>A missing folder is the normal case without the vendor software installed.</summary>
    [Fact]
    public void ReadAllSurvivesAMissingFolder()
    {
        var devices = SdkDeviceDescription.ReadAll([Path.Combine(Path.GetTempPath(), "keylegend-does-not-exist")]);

        Assert.Empty(devices);
    }

    [Fact]
    public void ReadAllSkipsBrokenFilesAndKeepsTheRest()
    {
        var directory = Path.Combine(Path.GetTempPath(), "keylegend-sdk-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);

        try
        {
            File.WriteAllText(Path.Combine(directory, "broken.json"), "{ not json");
            File.WriteAllText(Path.Combine(directory, "good.json"), DeathStalker);

            var devices = SdkDeviceDescription.ReadAll([directory]);

            Assert.Single(devices);
            Assert.Equal("Razer DeathStalker V2", devices[0].ProductName);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
