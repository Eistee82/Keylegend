using Keylegend.Chroma;
using Keylegend.Core.Devices;
using Keylegend.Core.Input;
using Keylegend.Core.Lighting;
using Keylegend.Core.Session;
using Keylegend.Engine;

namespace Keylegend.Core.Tests.Engine;

public class LightingEngineTests
{
    private sealed class FakeChroma : IChromaClient
    {
        public bool IsConnected { get; private set; }

        public int Connects { get; private set; }

        public int Disconnects { get; private set; }

        public int FramesSent { get; private set; }

        public Exception? FailNextSendWith { get; set; }

        public Task ConnectAsync(CancellationToken cancellationToken)
        {
            IsConnected = true;
            Connects++;
            return Task.CompletedTask;
        }

        public Task SendAsync(LedFrame frame, CancellationToken cancellationToken)
        {
            if (FailNextSendWith is { } failure)
            {
                FailNextSendWith = null;
                throw failure;
            }

            FramesSent++;
            return Task.CompletedTask;
        }

        public Task DisconnectAsync(CancellationToken cancellationToken)
        {
            if (IsConnected)
            {
                Disconnects++;
            }

            IsConnected = false;
            return Task.CompletedTask;
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    private sealed class FakeKeys : IKeyStateSource
    {
        public KeyboardState State { get; set; } = KeyboardState.Empty;

        public bool Down { get; set; }

        public KeyboardState Read() => State;

        public bool AnyKeyDown() => Down;
    }

    private sealed class FakeResolver : IKeyResolver
    {
        public KeyMeaning Resolve(string keyId, int? scanCode, KeyboardState state)
            => new("a", KeyCategory.Lowercase);
    }

    private static DeviceProfile Profile() => new(
        FormatVersion: 1, Name: "Test", Vendor: "Test", Model: "T1",
        PhysicalLayout: "ISO-DE", Image: "device.png",
        Canvas: new Canvas(500, 200), Matrix: new MatrixSize(6, 22), Verified: true,
        Keys: [new KeyDefinition("Keyboard_A", 0, 0, 19, 19, 3, 2)]);

    /// <summary>Runs the engine until <paramref name="check"/> holds, or fails after a timeout.</summary>
    private static async Task RunUntilAsync(
        LightingEngine engine,
        Func<bool> check,
        string what,
        Action? afterStart = null)
    {
        using var stopping = new CancellationTokenSource();
        var running = engine.RunAsync(stopping.Token);

        afterStart?.Invoke();

        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (!check() && DateTime.UtcNow < deadline)
        {
            await Task.Delay(10);
        }

        var satisfied = check();
        await stopping.CancelAsync();
        await running;

        Assert.True(satisfied, $"Timed out waiting for: {what}");
    }

    [Fact]
    public async Task TakesOverWhenAKeyIsPressed()
    {
        var chroma = new FakeChroma();
        var keys = new FakeKeys();
        var engine = new LightingEngine(Profile(), chroma, keys, new FakeResolver());

        await RunUntilAsync(engine, () => chroma.FramesSent > 0, "a frame to be sent",
            () => keys.Down = true);

        Assert.Equal(1, chroma.Connects);
    }

    [Fact]
    public async Task StaysIdleWhileNothingHappens()
    {
        var chroma = new FakeChroma();
        var engine = new LightingEngine(Profile(), chroma, new FakeKeys(), new FakeResolver());

        using var stopping = new CancellationTokenSource();
        var running = engine.RunAsync(stopping.Token);
        await Task.Delay(200);
        await stopping.CancelAsync();
        await running;

        Assert.Equal(0, chroma.Connects);
        Assert.Equal(LightingState.Idle, engine.State);
    }

    [Fact]
    public async Task HandsTheLightingBackAfterTheIdleTimeout()
    {
        var chroma = new FakeChroma();
        var keys = new FakeKeys();
        var engine = new LightingEngine(Profile(), chroma, keys, new FakeResolver())
        {
            Settings = new EngineSettings { IdleTimeout = TimeSpan.FromMilliseconds(150) }
        };

        await RunUntilAsync(
            engine,
            () => chroma.Disconnects > 0,
            "the lighting to be handed back",
            () =>
            {
                keys.Down = true;
                Task.Delay(80).Wait();
                keys.Down = false;      // stop typing; the timeout should now elapse
            });
    }

    [Fact]
    public async Task PublishesEveryComposedFrameSoAPreviewCanMirrorIt()
    {
        var chroma = new FakeChroma();
        var keys = new FakeKeys { Down = true };
        var engine = new LightingEngine(Profile(), chroma, keys, new FakeResolver());
        var published = 0;
        engine.FrameComposed += _ => published++;

        await RunUntilAsync(engine, () => published > 0, "a frame to be published");

        Assert.True(published > 0);
    }

    [Fact]
    public async Task SurvivesAChromaFailureAndRetries()
    {
        var chroma = new FakeChroma { FailNextSendWith = new ChromaException("service gone") };
        var keys = new FakeKeys { Down = true };
        var engine = new LightingEngine(Profile(), chroma, keys, new FakeResolver());
        var warnings = new List<string>();
        engine.Warning += warnings.Add;

        await RunUntilAsync(engine, () => warnings.Count > 0, "a warning to be reported");

        // The engine must not die on it.
        Assert.NotEmpty(warnings);
    }

    [Fact]
    public async Task PausingReleasesTheLighting()
    {
        var chroma = new FakeChroma();
        var keys = new FakeKeys { Down = true };
        var engine = new LightingEngine(Profile(), chroma, keys, new FakeResolver());

        await RunUntilAsync(
            engine,
            () => chroma.Disconnects > 0,
            "the lighting to be released on pause",
            () =>
            {
                Task.Delay(120).Wait();
                engine.Pause();
            });

        Assert.Equal(LightingState.Paused, engine.State);
    }

    [Fact]
    public void PreviewComposesWithoutSending()
    {
        var chroma = new FakeChroma();
        var engine = new LightingEngine(Profile(), chroma, new FakeKeys(), new FakeResolver());

        var frame = engine.Preview(KeyboardState.Empty);

        Assert.Equal(engine.Settings.Scheme.For(KeyCategory.Lowercase), frame[3, 2]);
        Assert.Equal(0, chroma.FramesSent);
    }

    [Fact]
    public void ChangingTheIdleTimeoutKeepsTheCurrentState()
    {
        var engine = new LightingEngine(Profile(), new FakeChroma(), new FakeKeys(), new FakeResolver());
        engine.Pause();

        engine.Settings = engine.Settings with { IdleTimeout = TimeSpan.FromSeconds(5) };

        // A settings change must not silently resume a paused engine.
        Assert.Equal(LightingState.Paused, engine.State);
    }
}
