using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Keylegend.Core.Lighting;

namespace Keylegend.Chroma;

/// <summary>
/// Talks to the Chroma SDK over its local REST interface. No vendor libraries are needed,
/// which keeps this assembly dependency-free and the protocol visible.
/// </summary>
public sealed class ChromaClient : IChromaClient
{
    private readonly HttpClient _http;
    private readonly ChromaOptions _options;
    private readonly SemaphoreSlim _gate = new(1, 1);

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private string? _sessionUri;
    private Timer? _heartbeat;
    private bool _awaitingFirstFrame;

    public ChromaClient(HttpClient http, ChromaOptions options)
    {
        _http = http ?? throw new ArgumentNullException(nameof(http));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public bool IsConnected => _sessionUri is not null;

    public async Task ConnectAsync(CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (_sessionUri is not null)
            {
                return;
            }

            var request = new InitRequest(
                _options.ApplicationTitle,
                "Interactive keyboard lighting",
                new Author("Keylegend", "https://github.com/Eistee82/Keylegend"),
                ["keyboard"],
                "application");

            InitResponse? response;
            try
            {
                var message = await _http.PostAsJsonAsync(_options.BaseAddress, request, cancellationToken);
                var body = await ReadAcceptedBodyAsync(message, "Opening a Chroma session", cancellationToken);

                response = JsonSerializer.Deserialize<InitResponse>(body, JsonOptions);
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                throw new ChromaException(
                    "Could not reach the Chroma service. Is Razer Synapse running?", ex);
            }

            if (string.IsNullOrWhiteSpace(response?.Uri))
            {
                throw new ChromaException("The Chroma service did not return a session address.");
            }

            _sessionUri = response.Uri;
            _awaitingFirstFrame = true;
            _heartbeat = new Timer(
                _ => _ = SendHeartbeatAsync(),
                state: null,
                dueTime: _options.HeartbeatInterval,
                period: _options.HeartbeatInterval);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task SendAsync(LedFrame frame, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(frame);

        var session = _sessionUri
            ?? throw new ChromaException("Not connected; call ConnectAsync first.");

        var payload = new CustomEffect("CHROMA_CUSTOM", frame.ToBgrMatrix());

        try
        {
            var message = await _http.PutAsJsonAsync(
                $"{session}/keyboard", payload, cancellationToken);

            await ReadAcceptedBodyAsync(message, "Sending a frame", cancellationToken);

            // The first frame after a session is created is accepted with a success reply but
            // never actually shown: it only completes the hand-over from the vendor software,
            // which is why the lighting visibly freezes on the previous effect. Sending it a
            // second time is what makes it appear. Costs about two milliseconds, and only
            // once per take-over.
            if (_awaitingFirstFrame)
            {
                _awaitingFirstFrame = false;

                var repeat = await _http.PutAsJsonAsync(
                    $"{session}/keyboard", payload, cancellationToken);

                await ReadAcceptedBodyAsync(repeat, "Sending a frame", cancellationToken);
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            throw new ChromaException("Sending a frame to the keyboard failed.", ex);
        }
    }

    public async Task DisconnectAsync(CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (_sessionUri is not { } session)
            {
                return;
            }

            if (_heartbeat is not null)
            {
                await _heartbeat.DisposeAsync();
                _heartbeat = null;
            }

            _sessionUri = null;
            _awaitingFirstFrame = false;

            try
            {
                await _http.DeleteAsync(session, cancellationToken);
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                // The session is gone from our side either way, and it times out on the
                // service within seconds. Failing here would help nobody.
                _ = ex;
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        await DisconnectAsync(CancellationToken.None);
        _gate.Dispose();
    }

    private async Task SendHeartbeatAsync()
    {
        if (_sessionUri is not { } session)
        {
            return;
        }

        try
        {
            using var content = new StringContent(string.Empty);
            await _http.PutAsync($"{session}/heartbeat", content);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            // A missed heartbeat is not fatal; the next one may well succeed. If the session
            // has really gone, the next SendAsync surfaces it. Deliberately not thrown from
            // here: this runs on a timer thread, where an escaping exception takes the process
            // down with it.
            _ = ex;
        }
    }

    /// <summary>
    /// Reads a reply, fails if the service rejected the request, and hands back the body.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The Chroma service answers <em>everything</em> with HTTP 200, including requests it threw
    /// away. A frame with the wrong matrix size comes back as
    /// <c>{"error":"expecting a 2 dimensional array of 6 (rows) x 22 (columns) elements with
    /// integer values","result":87}</c> — with a 200 status. Checking the status code alone
    /// therefore reports success for frames the keyboard never showed, which is a silent
    /// failure: the lighting simply stops changing and nothing says why.
    /// </para>
    /// <para>
    /// So the body decides. <c>result</c> is zero on success, and where the service supplies an
    /// <c>error</c> in plain language it is worth more than anything this code could invent —
    /// it names the actual defect, down to the expected array dimensions.
    /// </para>
    /// </remarks>
    private static async Task<string> ReadAcceptedBodyAsync(
        HttpResponseMessage message, string what, CancellationToken cancellationToken)
    {
        var body = await message.Content.ReadAsStringAsync(cancellationToken);

        // A non-200 status still means failure; it is just not the only thing that does.
        if (!message.IsSuccessStatusCode)
        {
            throw new ChromaException(
                $"{what} failed: the Chroma service answered {(int)message.StatusCode}.");
        }

        ChromaReply? reply;
        try
        {
            reply = JsonSerializer.Deserialize<ChromaReply>(body, JsonOptions);
        }
        catch (JsonException)
        {
            // An unreadable body is not worth failing over on its own: the request may well
            // have been carried out, and the next one will surface a real problem.
            return body;
        }

        if (reply is null || reply.Result is null or 0)
        {
            return body;
        }

        throw new ChromaException($"{what} failed: {Describe(reply)}");
    }

    /// <summary>
    /// Turns a rejection into something worth reading. The service's own wording comes first
    /// where there is any; the numeric code is kept either way, because that is what somebody
    /// searching for the problem will have in front of them.
    /// </summary>
    private static string Describe(ChromaReply reply)
    {
        var code = reply.Result ?? 0;

        // Only the codes a user can act on are named. The rest fall through to whatever the
        // service said, which is usually more specific than a generic phrase would be.
        var meaning = code switch
        {
            4309 => "Chroma is switched off for this device in Razer Synapse",
            1152 => "another application is holding the Chroma session",
            1167 => "no Chroma device is connected",
            5 => "access was denied",
            50 => "the request is not supported",
            87 => "the request was malformed",
            _ => null
        };

        return (meaning, reply.Error) switch
        {
            (not null, not null) => $"{meaning} ({reply.Error}, result {code})",
            (not null, null) => $"{meaning} (result {code})",
            (null, not null) => $"{reply.Error} (result {code})",
            _ => $"the service returned result {code}"
        };
    }

    private sealed record Author(string Name, string Contact);

    private sealed record InitRequest(
        string Title,
        string Description,
        Author Author,
        [property: JsonPropertyName("device_supported")] string[] DeviceSupported,
        string Category);

    private sealed record InitResponse(
        [property: JsonPropertyName("sessionid")] int SessionId,
        [property: JsonPropertyName("uri")] string? Uri);

    private sealed record CustomEffect(string Effect, int[][] Param);

    /// <summary>
    /// The part every reply shares. <c>result</c> is absent from a successful session init,
    /// which returns the session details instead, so absence counts as success.
    /// </summary>
    private sealed record ChromaReply(
        [property: JsonPropertyName("result")] int? Result,
        [property: JsonPropertyName("error")] string? Error);
}
