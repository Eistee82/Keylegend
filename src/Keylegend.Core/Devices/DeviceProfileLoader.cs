using System.Text.Json;
using System.Text.Json.Serialization;

namespace Keylegend.Core.Devices;

/// <summary>Reads device profiles from JSON.</summary>
public static class DeviceProfileLoader
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString
    };

    /// <summary>Parses a profile from JSON text.</summary>
    /// <exception cref="DeviceProfileException">The text is not a readable profile.</exception>
    public static DeviceProfile Parse(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);

        DeviceProfile? profile;
        try
        {
            profile = JsonSerializer.Deserialize<DeviceProfile>(json, Options);
        }
        catch (JsonException ex)
        {
            throw new DeviceProfileException($"Device profile is not valid JSON: {ex.Message}", ex);
        }

        return profile ?? throw new DeviceProfileException("Device profile is empty.");
    }

    /// <summary>Loads a profile from disk.</summary>
    /// <exception cref="DeviceProfileException">The file is missing or unreadable as a profile.</exception>
    public static DeviceProfile Load(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        if (!File.Exists(path))
        {
            throw new DeviceProfileException($"Device profile not found: {path}");
        }

        return Parse(File.ReadAllText(path));
    }
}

/// <summary>Raised when a device profile cannot be read or is not usable.</summary>
public sealed class DeviceProfileException : Exception
{
    public DeviceProfileException(string message) : base(message) { }
    public DeviceProfileException(string message, Exception inner) : base(message, inner) { }
}
