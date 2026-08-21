using System.Text.Json;
using System.Text.Json.Serialization;

namespace Keylegend.Core.Configuration;

/// <summary>
/// Reads and writes the settings file.
/// </summary>
/// <remarks>
/// Two properties matter here. Saving is atomic — written to a temporary file and moved into
/// place — so losing power mid-save cannot leave an unreadable configuration. And loading never
/// throws: a damaged file yields defaults with an explanation, because refusing to start over a
/// bad colour value would be a poor trade.
/// </remarks>
public sealed class ConfigStore
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,

        // camelCase to match the shipped profiles under profiles/. The same types are written in
        // both places, and a user comparing their edited profile against the shipped one - or
        // lifting one into the other - should not meet two spellings of every field.
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,

        // Null means "not overridden" throughout the profile types, and writing it out turns a
        // short override into a wall of nulls that hides the one line that matters.
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly string _path;

    /// <param name="path">
    /// Settings file location. Defaults to <c>%APPDATA%\Keylegend\settings.json</c>.
    /// </param>
    public ConfigStore(string? path = null)
    {
        _path = path ?? DefaultPath();
    }

    public string Path => _path;

    public static string DefaultPath() => System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Keylegend",
        "settings.json");

    /// <summary>
    /// Loads the settings. Never throws: anything unreadable becomes defaults, with the reason
    /// reported so the interface can say what happened rather than silently discarding work.
    /// </summary>
    public (StoredSettings Settings, string? Problem) Load()
    {
        if (!File.Exists(_path))
        {
            return (new StoredSettings(), null);
        }

        try
        {
            var json = File.ReadAllText(_path);
            var settings = JsonSerializer.Deserialize<StoredSettings>(json, Options);

            if (settings is null)
            {
                return (new StoredSettings(), "The settings file was empty; defaults are in use.");
            }

            if (settings.FormatVersion > StoredSettings.SupportedFormatVersion)
            {
                return (new StoredSettings(),
                    $"The settings file was written by a newer version (format {settings.FormatVersion}); " +
                    "defaults are in use so it is not overwritten with less.");
            }

            return (settings, null);
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            return (new StoredSettings(), $"The settings file could not be read ({ex.Message}); defaults are in use.");
        }
    }

    /// <summary>Saves the settings atomically.</summary>
    /// <returns>An explanation if saving failed, otherwise <c>null</c>.</returns>
    public string? Save(StoredSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        try
        {
            var directory = System.IO.Path.GetDirectoryName(_path);

            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Write beside the target and move into place, so an interrupted save leaves the
            // previous file intact rather than a half-written one.
            var temporary = _path + ".tmp";
            File.WriteAllText(temporary, JsonSerializer.Serialize(settings, Options));
            File.Move(temporary, _path, overwrite: true);

            return null;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return $"Settings could not be saved: {ex.Message}";
        }
    }
}
