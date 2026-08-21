namespace Keylegend.Chroma;

/// <summary>Settings for talking to the local Chroma service.</summary>
public sealed class ChromaOptions
{
    /// <summary>Where the SDK listens. The port is fixed by the vendor.</summary>
    public Uri BaseAddress { get; init; } = new("http://localhost:54235/razer/chromasdk");

    /// <summary>Name shown to the SDK for this application.</summary>
    public string ApplicationTitle { get; init; } = "Keylegend";

    /// <summary>
    /// How often to keep the session alive. The service drops sessions that go quiet for
    /// more than ten seconds, so this must stay comfortably below that.
    /// </summary>
    public TimeSpan HeartbeatInterval { get; init; } = TimeSpan.FromSeconds(5);
}
