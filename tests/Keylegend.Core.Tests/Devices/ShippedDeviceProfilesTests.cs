using Keylegend.Core.Devices;

namespace Keylegend.Core.Tests.Devices;

/// <summary>
/// Guards every profile under <c>devices/</c>. This is the check that catches a broken
/// contribution before it is merged, so it deliberately runs against the real files.
/// </summary>
public class ShippedDeviceProfilesTests
{
    public static TheoryData<string> ProfilePaths()
    {
        var data = new TheoryData<string>();
        foreach (var path in Directory.EnumerateFiles(DevicesDirectory, "device.json", SearchOption.AllDirectories))
        {
            data.Add(path);
        }

        return data;
    }

    [Fact]
    public void AtLeastOneDeviceProfileIsShipped()
    {
        var profiles = Directory.GetFiles(DevicesDirectory, "device.json", SearchOption.AllDirectories);

        Assert.NotEmpty(profiles);
    }

    [Theory]
    [MemberData(nameof(ProfilePaths))]
    public void ProfileIsValid(string path)
    {
        var profile = DeviceProfileLoader.Load(path);

        var problems = DeviceProfileValidator.Validate(profile);

        Assert.True(
            problems.Count == 0,
            $"{Path.GetFileName(Path.GetDirectoryName(path))}:{Environment.NewLine}  " +
            string.Join(Environment.NewLine + "  ", problems));
    }

    /// <summary>
    /// A picture is optional, but naming one that is not there is a mistake worth catching.
    /// </summary>
    [Theory]
    [MemberData(nameof(ProfilePaths))]
    public void NamedProfilePictureExists(string path)
    {
        var profile = DeviceProfileLoader.Load(path);
        if (string.IsNullOrWhiteSpace(profile.Image))
        {
            return;
        }

        var directory = Path.GetDirectoryName(path)!;

        Assert.True(
            File.Exists(Path.Combine(directory, profile.Image)),
            $"Profile references picture '{profile.Image}', which is missing from {directory}.");
    }

    private static string DevicesDirectory => TestPaths.DevicesDirectory;
}
