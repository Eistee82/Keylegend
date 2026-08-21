using System.Net;
using System.Text;
using System.Text.Json;
using Keylegend.Chroma;
using Keylegend.Core.Lighting;

namespace Keylegend.Core.Tests.Chroma;

public class ChromaClientTests
{
    /// <summary>Records requests and replies with canned responses - no service needed.</summary>
    private sealed class StubHandler : HttpMessageHandler
    {
        public List<(HttpMethod Method, string Url, string Body)> Requests { get; } = [];

        public Func<HttpRequestMessage, HttpResponseMessage>? Responder { get; set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var body = request.Content is null
                ? string.Empty
                : await request.Content.ReadAsStringAsync(cancellationToken);

            Requests.Add((request.Method, request.RequestUri!.ToString(), body));

            return Responder?.Invoke(request)
                ?? Json("""{"sessionid":1,"uri":"http://localhost:1/chromasdk"}""");
        }

        public static HttpResponseMessage Json(string content) => new(HttpStatusCode.OK)
        {
            Content = new StringContent(content, Encoding.UTF8, "application/json")
        };
    }

    private static (ChromaClient Client, StubHandler Handler) CreateClient()
    {
        var handler = new StubHandler();
        var http = new HttpClient(handler);
        var options = new ChromaOptions { ApplicationTitle = "Test" };

        return (new ChromaClient(http, options), handler);
    }

    [Fact]
    public async Task ConnectCreatesASession()
    {
        var (client, handler) = CreateClient();

        await client.ConnectAsync(CancellationToken.None);

        Assert.True(client.IsConnected);
        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Contains("chromasdk", request.Url);
        Assert.Contains("\"keyboard\"", request.Body);
    }

    [Fact]
    public async Task SendWritesTheFrameAsABgrMatrix()
    {
        var (client, handler) = CreateClient();
        await client.ConnectAsync(CancellationToken.None);
        handler.Requests.Clear();

        var frame = new LedFrame(6, 22);
        frame.Set(0, 0, new RgbColor(255, 0, 0));
        await client.SendAsync(frame, CancellationToken.None);

        // The first frame of a session is deliberately sent twice; the payload is what
        // matters here, so inspect the first of them.
        var request = handler.Requests.First(r => r.Method == HttpMethod.Put);
        Assert.EndsWith("/keyboard", request.Url);

        using var document = JsonDocument.Parse(request.Body);
        Assert.Equal("CHROMA_CUSTOM", document.RootElement.GetProperty("effect").GetString());

        var matrix = document.RootElement.GetProperty("param");
        Assert.Equal(6, matrix.GetArrayLength());
        Assert.Equal(22, matrix[0].GetArrayLength());
        Assert.Equal(0x0000FF, matrix[0][0].GetInt32());   // red, BGR-packed
    }

    [Fact]
    public async Task TheFirstFrameAfterConnectingIsSentTwice()
    {
        // Chroma accepts the first frame of a session but never displays it - it only
        // completes the hand-over. Without the repeat the user sees the previous effect
        // freeze and nothing else until the next frame happens to arrive.
        var (client, handler) = CreateClient();
        await client.ConnectAsync(CancellationToken.None);
        handler.Requests.Clear();

        await client.SendAsync(new LedFrame(6, 22), CancellationToken.None);

        Assert.Equal(2, handler.Requests.Count(r => r.Method == HttpMethod.Put));
    }

    [Fact]
    public async Task LaterFramesAreSentOnce()
    {
        var (client, handler) = CreateClient();
        await client.ConnectAsync(CancellationToken.None);
        await client.SendAsync(new LedFrame(6, 22), CancellationToken.None);
        handler.Requests.Clear();

        await client.SendAsync(new LedFrame(6, 22), CancellationToken.None);

        Assert.Single(handler.Requests, r => r.Method == HttpMethod.Put);
    }

    [Fact]
    public async Task ReconnectingArmsTheRepeatAgain()
    {
        // Every take-over needs the repeat, not just the first one in the process's life.
        var (client, handler) = CreateClient();
        await client.ConnectAsync(CancellationToken.None);
        await client.SendAsync(new LedFrame(6, 22), CancellationToken.None);
        await client.DisconnectAsync(CancellationToken.None);

        await client.ConnectAsync(CancellationToken.None);
        handler.Requests.Clear();
        await client.SendAsync(new LedFrame(6, 22), CancellationToken.None);

        Assert.Equal(2, handler.Requests.Count(r => r.Method == HttpMethod.Put));
    }

    [Fact]
    public async Task DisconnectReleasesTheSessionSoChromaStudioResumes()
    {
        var (client, handler) = CreateClient();
        await client.ConnectAsync(CancellationToken.None);
        handler.Requests.Clear();

        await client.DisconnectAsync(CancellationToken.None);

        Assert.False(client.IsConnected);
        Assert.Contains(handler.Requests, r => r.Method == HttpMethod.Delete);
    }

    [Fact]
    public async Task SendingWithoutAConnectionIsARefusal()
    {
        var (client, _) = CreateClient();

        await Assert.ThrowsAsync<ChromaException>(
            () => client.SendAsync(new LedFrame(6, 22), CancellationToken.None));
    }

    [Fact]
    public async Task ConnectFailureSurfacesAsChromaException()
    {
        var (client, handler) = CreateClient();
        handler.Responder = _ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);

        await Assert.ThrowsAsync<ChromaException>(() => client.ConnectAsync(CancellationToken.None));
        Assert.False(client.IsConnected);
    }

    [Fact]
    public async Task ConnectingTwiceDoesNotCreateASecondSession()
    {
        var (client, handler) = CreateClient();

        await client.ConnectAsync(CancellationToken.None);
        await client.ConnectAsync(CancellationToken.None);

        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task DisconnectingWhenIdleIsHarmless()
    {
        var (client, handler) = CreateClient();

        await client.DisconnectAsync(CancellationToken.None);

        Assert.Empty(handler.Requests);
    }

    // ----------------------------------------------------------------------------------------
    // Rejections. The Chroma service answers everything with HTTP 200 and puts the outcome in
    // the body, so a refused request looks exactly like an accepted one to the status code.
    // The replies below were captured from the real service, not invented.

    /// <summary>
    /// The failure that prompted these tests: a frame the keyboard never showed, reported as a
    /// success. Silent, and indistinguishable from the lighting simply not changing.
    /// </summary>
    [Fact]
    public async Task ARejectedFrameIsNotMistakenForSuccess()
    {
        var (client, handler) = CreateClient();
        await client.ConnectAsync(CancellationToken.None);

        handler.Responder = request => request.Method == HttpMethod.Put
            ? StubHandler.Json(
                """
                {"error":"expecting a 2 dimensional array of 6 (rows) x 22 (columns) elements with integer values","result":87}
                """)
            : StubHandler.Json("""{"sessionid":1,"uri":"http://localhost:1/chromasdk"}""");

        var thrown = await Assert.ThrowsAsync<ChromaException>(
            () => client.SendAsync(new LedFrame(6, 22), CancellationToken.None));

        // The service's own wording is more use than anything we could write ourselves.
        Assert.Contains("6 (rows) x 22 (columns)", thrown.Message);
        Assert.Contains("87", thrown.Message);
    }

    [Fact]
    public async Task ASuccessfulFrameReplyIsAccepted()
    {
        var (client, handler) = CreateClient();
        await client.ConnectAsync(CancellationToken.None);

        handler.Responder = _ => StubHandler.Json("""{"result":0}""");

        await client.SendAsync(new LedFrame(6, 22), CancellationToken.None);
    }

    /// <summary>
    /// The codes a user can actually do something about are translated; the raw number stays,
    /// because that is what somebody searching for the problem will have.
    /// </summary>
    [Theory]
    [InlineData(4309, "switched off")]
    [InlineData(1152, "another application")]
    [InlineData(1167, "no Chroma device")]
    public async Task ActionableResultCodesAreExplained(int code, string expected)
    {
        var (client, handler) = CreateClient();
        handler.Responder = _ => StubHandler.Json($$"""{"result":{{code}}}""");

        var thrown = await Assert.ThrowsAsync<ChromaException>(
            () => client.ConnectAsync(CancellationToken.None));

        Assert.Contains(expected, thrown.Message);
        Assert.Contains(code.ToString(), thrown.Message);
        Assert.False(client.IsConnected);
    }

    /// <summary>
    /// A successful session init carries no result field at all — it returns the session
    /// details instead. Absence must therefore count as success, not as a missing zero.
    /// </summary>
    [Fact]
    public async Task ASessionReplyWithoutAResultFieldIsSuccess()
    {
        var (client, _) = CreateClient();

        await client.ConnectAsync(CancellationToken.None);

        Assert.True(client.IsConnected);
    }

    /// <summary>
    /// A body that cannot be read is not itself a reason to fail: the request may well have
    /// been carried out, and a real problem surfaces on the next one.
    /// </summary>
    [Fact]
    public async Task AnUnreadableReplyDoesNotFailTheFrame()
    {
        var (client, handler) = CreateClient();
        await client.ConnectAsync(CancellationToken.None);

        handler.Responder = _ => StubHandler.Json("not json at all");

        await client.SendAsync(new LedFrame(6, 22), CancellationToken.None);
    }
}
