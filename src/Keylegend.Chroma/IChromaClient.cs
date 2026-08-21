using Keylegend.Core.Lighting;

namespace Keylegend.Chroma;

/// <summary>
/// Sends frames to the keyboard. Implementations own a session: while one is held, this
/// application controls the lighting and the vendor software does not.
/// </summary>
public interface IChromaClient : IAsyncDisposable
{
    /// <summary>Whether a session is currently held.</summary>
    bool IsConnected { get; }

    /// <summary>
    /// Acquires a session. Taking control from a running vendor effect costs roughly half a
    /// second; every frame afterwards is a couple of milliseconds.
    /// </summary>
    Task ConnectAsync(CancellationToken cancellationToken);

    /// <summary>Writes a frame to the keyboard.</summary>
    Task SendAsync(LedFrame frame, CancellationToken cancellationToken);

    /// <summary>
    /// Releases the session, which hands the lighting straight back to the vendor software.
    /// Safe to call when no session is held.
    /// </summary>
    Task DisconnectAsync(CancellationToken cancellationToken);
}

/// <summary>Raised when the Chroma service cannot be reached or refuses a request.</summary>
public sealed class ChromaException : Exception
{
    public ChromaException(string message) : base(message) { }

    public ChromaException(string message, Exception inner) : base(message, inner) { }
}
