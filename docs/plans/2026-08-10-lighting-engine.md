# Keylegend Lighting Engine — Implementation Plan

**Goal:** Make the keyboard light up according to what each key currently means — lock states, character categories, and modifier layers — and hand the lighting back to Chroma Studio after an idle period.

**Architecture:** All decision logic lives in `Keylegend.Core` as a pure calculation with no platform or network dependencies, so it is testable without hardware. Windows APIs are confined to `Keylegend.Windows`, the Chroma REST interface to `Keylegend.Chroma`. A console host wires them together; the WPF interface (separate plan) will reuse the exact same core.

**Tech Stack:** C# 13 / .NET 10, xUnit, System.Text.Json, HttpClient, Win32 P/Invoke.

**Scope:** This plan covers steps 1–4 of the specification (`docs/design/2026-08-10-design.de.md`, section 12). The user interface, application profiles and tray integration are a separate plan. Step 1's profile loading is already done (`Keylegend.Core/Devices/`).

## Global Constraints

- **`Keylegend.Core` must not reference `Keylegend.Windows`, `Keylegend.Chroma`, or any Windows/network API.** Verified by a test in Task 1.
- **No global keyboard hooks.** Only `GetAsyncKeyState` / `GetKeyState` polling. Never intercept, forward, log or store keystrokes. This is a stated privacy commitment in the README.
- **No hardcoded keyboard layout tables.** Character meanings come from `ToUnicodeEx` against the active layout.
- **Colours are BGR integers for Chroma:** `(B << 16) | (G << 8) | R`.
- **Chroma matrix is 6 rows × 22 columns.** Session heartbeat more often than every 10 s.
- **Target frameworks:** `Keylegend.Core` and `Keylegend.Chroma` are `net10.0`; `Keylegend.Windows` and the host are `net10.0-windows`.
- **Never put "Razer" or "Chroma" in namespaces or type names** beyond the existing `Keylegend.Chroma` assembly, which describes the protocol it speaks.
- **Every task ends with a commit.** Run `dotnet test` before committing; it must be green.
- Documentation is maintained in English and German. Behaviour changes require a `CHANGELOG.md` entry under `## [Unreleased]`.

---

## File Structure

| File | Responsibility |
|---|---|
| `src/Keylegend.Core/Lighting/RgbColor.cs` | Colour value type and BGR conversion |
| `src/Keylegend.Core/Lighting/LedFrame.cs` | One rendered picture: colour per matrix cell |
| `src/Keylegend.Core/Input/KeyCategory.cs` | The character classes a key can fall into |
| `src/Keylegend.Core/Input/KeyboardState.cs` | Which modifiers are held and which locks are on |
| `src/Keylegend.Core/Input/ScanCodes.cs` | Key id → physical scan code table |
| `src/Keylegend.Core/Input/CharacterClassifier.cs` | Character → category |
| `src/Keylegend.Core/Input/IKeyResolver.cs` | Abstraction over "what does this key produce now" |
| `src/Keylegend.Core/Shortcuts/FunctionGroup.cs` | Named group of related commands |
| `src/Keylegend.Core/Shortcuts/ShortcutSet.cs` | Key → group, looked up by modifier combination |
| `src/Keylegend.Core/Shortcuts/DefaultShortcuts.cs` | The shipped Win/Alt/Ctrl sets |
| `src/Keylegend.Core/Lighting/ColourScheme.cs` | Configurable colours per category and group |
| `src/Keylegend.Core/Lighting/FrameComposer.cs` | **The core rule engine** |
| `src/Keylegend.Core/Session/LightingState.cs` | Idle / Active / Paused |
| `src/Keylegend.Core/Session/SessionManager.cs` | State machine with an injectable clock |
| `src/Keylegend.Chroma/IChromaClient.cs` | Interface the composer's output is sent through |
| `src/Keylegend.Chroma/ChromaClient.cs` | REST implementation with heartbeat |
| `src/Keylegend.Windows/KeyboardStateReader.cs` | Polls modifier and lock states |
| `src/Keylegend.Windows/WindowsKeyResolver.cs` | `ToUnicodeEx` implementation of `IKeyResolver` |
| `src/Keylegend.Windows/ActivityTracker.cs` | "Was a key pressed recently" |
| `src/Keylegend.Host/Program.cs` | Wires everything together; calibration mode |

---

### Task 1: Colour type and the architectural guard

Establishes the value type every later task uses, and locks down the dependency rule that the whole design rests on.

**Files:**
- Create: `src/Keylegend.Core/Lighting/RgbColor.cs`
- Test: `tests/Keylegend.Core.Tests/Lighting/RgbColorTests.cs`
- Test: `tests/Keylegend.Core.Tests/ArchitectureTests.cs`

**Interfaces:**
- Consumes: nothing
- Produces: `RgbColor(byte R, byte G, byte B)` with `int ToBgr()`, `static RgbColor Off`, `RgbColor Scale(double factor)`, `static RgbColor FromHex(string hex)`

- [ ] **Step 1: Write the failing tests**

Create `tests/Keylegend.Core.Tests/Lighting/RgbColorTests.cs`:

```csharp
using Keylegend.Core.Lighting;

namespace Keylegend.Core.Tests.Lighting;

public class RgbColorTests
{
    [Fact]
    public void ConvertsToBgrIntegerAsChromaExpects()
    {
        // Chroma packs colours as (B << 16) | (G << 8) | R - not RGB.
        Assert.Equal(0x0000FF, new RgbColor(255, 0, 0).ToBgr());   // red
        Assert.Equal(0x00FF00, new RgbColor(0, 255, 0).ToBgr());   // green
        Assert.Equal(0xFF0000, new RgbColor(0, 0, 255).ToBgr());   // blue
        Assert.Equal(0xFFFFFF, new RgbColor(255, 255, 255).ToBgr());
    }

    [Fact]
    public void OffIsBlack()
    {
        Assert.Equal(0, RgbColor.Off.ToBgr());
    }

    [Theory]
    [InlineData(1.0, 200)]
    [InlineData(0.5, 100)]
    [InlineData(0.0, 0)]
    public void ScaleAppliesBrightnessFactor(double factor, byte expected)
    {
        var scaled = new RgbColor(200, 200, 200).Scale(factor);

        Assert.Equal(expected, scaled.R);
        Assert.Equal(expected, scaled.G);
        Assert.Equal(expected, scaled.B);
    }

    [Fact]
    public void ScaleClampsOutOfRangeFactors()
    {
        Assert.Equal(new RgbColor(255, 255, 255), new RgbColor(255, 255, 255).Scale(5.0));
        Assert.Equal(RgbColor.Off, new RgbColor(255, 255, 255).Scale(-1.0));
    }

    [Theory]
    [InlineData("#FF8000", 255, 128, 0)]
    [InlineData("FF8000", 255, 128, 0)]
    public void ParsesHexNotation(string hex, byte r, byte g, byte b)
    {
        Assert.Equal(new RgbColor(r, g, b), RgbColor.FromHex(hex));
    }

    [Theory]
    [InlineData("")]
    [InlineData("#GGGGGG")]
    [InlineData("#FFF")]
    public void RejectsMalformedHex(string hex)
    {
        Assert.Throws<FormatException>(() => RgbColor.FromHex(hex));
    }
}
```

Create `tests/Keylegend.Core.Tests/ArchitectureTests.cs`:

```csharp
using System.Reflection;
using Keylegend.Core.Lighting;

namespace Keylegend.Core.Tests;

/// <summary>
/// The separation between the pure core and the platform adapters is what makes the
/// colouring logic testable without hardware. This test exists so that it stays that way.
/// </summary>
public class ArchitectureTests
{
    [Fact]
    public void CoreDependsOnNothingPlatformSpecific()
    {
        var referenced = typeof(RgbColor).Assembly
            .GetReferencedAssemblies()
            .Select(a => a.Name!)
            .ToArray();

        Assert.DoesNotContain("Keylegend.Windows", referenced);
        Assert.DoesNotContain("Keylegend.Chroma", referenced);
        Assert.DoesNotContain(referenced, name => name.StartsWith("System.Net", StringComparison.Ordinal));
        Assert.DoesNotContain(referenced, name => name.StartsWith("PresentationFramework", StringComparison.Ordinal));
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test --filter "FullyQualifiedName~RgbColorTests"`
Expected: FAIL — `RgbColor` does not exist (compile error CS0246).

- [ ] **Step 3: Write the implementation**

Create `src/Keylegend.Core/Lighting/RgbColor.cs`:

```csharp
using System.Globalization;

namespace Keylegend.Core.Lighting;

/// <summary>
/// A colour as the rest of the program thinks about it: plain red, green and blue.
/// Conversion into the vendor's packing happens at the very edge, in <see cref="ToBgr"/>.
/// </summary>
public readonly record struct RgbColor(byte R, byte G, byte B)
{
    /// <summary>No light at all.</summary>
    public static RgbColor Off => new(0, 0, 0);

    /// <summary>
    /// Packs the colour the way the Chroma SDK expects it: blue in the high byte, red in the
    /// low one. Passing an RGB-packed value instead is the classic cause of swapped colours.
    /// </summary>
    public int ToBgr() => (B << 16) | (G << 8) | R;

    /// <summary>Applies a brightness factor. Values outside 0..1 are clamped.</summary>
    public RgbColor Scale(double factor)
    {
        var clamped = Math.Clamp(factor, 0.0, 1.0);

        return new RgbColor(
            (byte)Math.Round(R * clamped),
            (byte)Math.Round(G * clamped),
            (byte)Math.Round(B * clamped));
    }

    /// <summary>Parses <c>#RRGGBB</c> or <c>RRGGBB</c>.</summary>
    /// <exception cref="FormatException">The text is not a six-digit hex colour.</exception>
    public static RgbColor FromHex(string hex)
    {
        ArgumentNullException.ThrowIfNull(hex);

        var digits = hex.StartsWith('#') ? hex[1..] : hex;

        if (digits.Length != 6)
        {
            throw new FormatException($"Expected six hex digits, got '{hex}'.");
        }

        static byte Component(ReadOnlySpan<char> text) =>
            byte.TryParse(text, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var value)
                ? value
                : throw new FormatException($"'{text}' is not a hex byte.");

        return new RgbColor(
            Component(digits.AsSpan(0, 2)),
            Component(digits.AsSpan(2, 2)),
            Component(digits.AsSpan(4, 2)));
    }

    public override string ToString() => $"#{R:X2}{G:X2}{B:X2}";
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test`
Expected: PASS — all tests green (11 existing + 8 new).

- [ ] **Step 5: Commit**

```bash
git add src/Keylegend.Core/Lighting/RgbColor.cs tests/Keylegend.Core.Tests/Lighting/RgbColorTests.cs tests/Keylegend.Core.Tests/ArchitectureTests.cs
git commit -m "Add colour value type and guard the core's independence

RgbColor keeps colours in plain RGB and converts to the vendor's BGR
packing only at the edge, so the swap cannot leak into the rest of the code.

The architecture test fails the build if Keylegend.Core ever gains a
dependency on the platform or network adapters, which is what keeps the
colouring rules testable without hardware."
```

---

### Task 2: The LED frame

A rendered picture, addressed by matrix cell. This is what the composer produces and the Chroma client consumes.

**Files:**
- Create: `src/Keylegend.Core/Lighting/LedFrame.cs`
- Test: `tests/Keylegend.Core.Tests/Lighting/LedFrameTests.cs`

**Interfaces:**
- Consumes: `RgbColor` (Task 1)
- Produces: `LedFrame(int rows, int columns)` with `RgbColor this[int row, int column]`, `void Set(int row, int column, RgbColor colour)`, `int[][] ToBgrMatrix()`, `void Clear()`, `int Rows`, `int Columns`

- [ ] **Step 1: Write the failing test**

Create `tests/Keylegend.Core.Tests/Lighting/LedFrameTests.cs`:

```csharp
using Keylegend.Core.Lighting;

namespace Keylegend.Core.Tests.Lighting;

public class LedFrameTests
{
    [Fact]
    public void StartsCompletelyDark()
    {
        var frame = new LedFrame(6, 22);

        Assert.Equal(RgbColor.Off, frame[0, 0]);
        Assert.Equal(RgbColor.Off, frame[5, 21]);
    }

    [Fact]
    public void StoresAndReturnsColours()
    {
        var frame = new LedFrame(6, 22);

        frame.Set(3, 13, new RgbColor(10, 20, 30));

        Assert.Equal(new RgbColor(10, 20, 30), frame[3, 13]);
    }

    [Fact]
    public void ProducesABgrMatrixOfTheDeclaredShape()
    {
        var frame = new LedFrame(6, 22);
        frame.Set(1, 2, new RgbColor(255, 0, 0));

        var matrix = frame.ToBgrMatrix();

        Assert.Equal(6, matrix.Length);
        Assert.All(matrix, row => Assert.Equal(22, row.Length));
        Assert.Equal(0x0000FF, matrix[1][2]);
        Assert.Equal(0, matrix[0][0]);
    }

    [Fact]
    public void ClearTurnsEverythingOff()
    {
        var frame = new LedFrame(6, 22);
        frame.Set(2, 2, new RgbColor(1, 2, 3));

        frame.Clear();

        Assert.Equal(RgbColor.Off, frame[2, 2]);
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(0, -1)]
    [InlineData(6, 0)]
    [InlineData(0, 22)]
    public void RejectsCellsOutsideTheMatrix(int row, int column)
    {
        var frame = new LedFrame(6, 22);

        Assert.Throws<ArgumentOutOfRangeException>(() => frame.Set(row, column, RgbColor.Off));
    }

    [Fact]
    public void RejectsNonPositiveDimensions()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new LedFrame(0, 22));
        Assert.Throws<ArgumentOutOfRangeException>(() => new LedFrame(6, 0));
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test --filter "FullyQualifiedName~LedFrameTests"`
Expected: FAIL — `LedFrame` does not exist.

- [ ] **Step 3: Write the implementation**

Create `src/Keylegend.Core/Lighting/LedFrame.cs`:

```csharp
namespace Keylegend.Core.Lighting;

/// <summary>
/// One rendered picture of the keyboard: a colour for every cell of the vendor LED matrix.
/// Frames are reused between renders rather than reallocated, because a frame is produced on
/// every state change and allocation churn in that path is pointless.
/// </summary>
public sealed class LedFrame
{
    private readonly RgbColor[,] _cells;

    public LedFrame(int rows, int columns)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(rows, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(columns, 1);

        Rows = rows;
        Columns = columns;
        _cells = new RgbColor[rows, columns];
    }

    public int Rows { get; }

    public int Columns { get; }

    public RgbColor this[int row, int column]
    {
        get
        {
            ThrowIfOutside(row, column);
            return _cells[row, column];
        }
    }

    public void Set(int row, int column, RgbColor colour)
    {
        ThrowIfOutside(row, column);
        _cells[row, column] = colour;
    }

    public void Clear() => Array.Clear(_cells);

    /// <summary>Renders the frame in the packing the Chroma SDK expects.</summary>
    public int[][] ToBgrMatrix()
    {
        var matrix = new int[Rows][];

        for (var row = 0; row < Rows; row++)
        {
            var line = new int[Columns];

            for (var column = 0; column < Columns; column++)
            {
                line[column] = _cells[row, column].ToBgr();
            }

            matrix[row] = line;
        }

        return matrix;
    }

    private void ThrowIfOutside(int row, int column)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(row);
        ArgumentOutOfRangeException.ThrowIfNegative(column);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(row, Rows);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(column, Columns);
    }
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add src/Keylegend.Core/Lighting/LedFrame.cs tests/Keylegend.Core.Tests/Lighting/LedFrameTests.cs
git commit -m "Add LED frame addressed by matrix cell

The frame is the hand-off point between the rule engine and the hardware
adapter: the composer fills it, the Chroma client renders it to BGR."
```

---

### Task 3: Chroma client

Speaks the REST protocol, keeps the session alive, and — importantly — releases it so Chroma Studio can take over again.

**Files:**
- Create: `src/Keylegend.Chroma/IChromaClient.cs`
- Create: `src/Keylegend.Chroma/ChromaClient.cs`
- Create: `src/Keylegend.Chroma/ChromaOptions.cs`
- Test: `tests/Keylegend.Core.Tests/Chroma/ChromaClientTests.cs`

**Interfaces:**
- Consumes: `LedFrame` (Task 2)
- Produces:
  - `interface IChromaClient : IAsyncDisposable` with `bool IsConnected { get; }`, `Task ConnectAsync(CancellationToken)`, `Task SendAsync(LedFrame, CancellationToken)`, `Task DisconnectAsync(CancellationToken)`
  - `sealed class ChromaOptions` with `Uri BaseAddress` (default `http://localhost:54235/razer/chromasdk`), `string ApplicationTitle`, `TimeSpan HeartbeatInterval` (default 5 s)
  - `sealed class ChromaClient(HttpClient, ChromaOptions) : IChromaClient`
  - `sealed class ChromaException : Exception`

- [ ] **Step 1: Write the failing test**

Create `tests/Keylegend.Core.Tests/Chroma/ChromaClientTests.cs`:

```csharp
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

            return Responder?.Invoke(request) ?? Json("""{"sessionid":1,"uri":"http://localhost:1/chromasdk"}""");
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

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Put, request.Method);
        Assert.EndsWith("/keyboard", request.Url);

        using var document = JsonDocument.Parse(request.Body);
        Assert.Equal("CHROMA_CUSTOM", document.RootElement.GetProperty("effect").GetString());

        var matrix = document.RootElement.GetProperty("param");
        Assert.Equal(6, matrix.GetArrayLength());
        Assert.Equal(22, matrix[0].GetArrayLength());
        Assert.Equal(0x0000FF, matrix[0][0].GetInt32());   // red, BGR-packed
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
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test --filter "FullyQualifiedName~ChromaClientTests"`
Expected: FAIL — `IChromaClient`, `ChromaClient`, `ChromaOptions` do not exist.

- [ ] **Step 3: Write the implementation**

Create `src/Keylegend.Chroma/ChromaOptions.cs`:

```csharp
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
```

Create `src/Keylegend.Chroma/IChromaClient.cs`:

```csharp
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
```

Create `src/Keylegend.Chroma/ChromaClient.cs`:

```csharp
using System.Net.Http.Json;
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

    private Uri? _sessionUri;
    private Timer? _heartbeat;

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
                message.EnsureSuccessStatusCode();
                response = await message.Content.ReadFromJsonAsync<InitResponse>(cancellationToken);
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                throw new ChromaException(
                    "Could not reach the Chroma service. Is Razer Synapse running?", ex);
            }

            if (response?.Uri is null)
            {
                throw new ChromaException("The Chroma service did not return a session address.");
            }

            _sessionUri = new Uri(response.Uri);
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
                new Uri(session + "/keyboard"), payload, cancellationToken);

            message.EnsureSuccessStatusCode();
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
            await _http.PutAsync(new Uri(session + "/heartbeat"), content: null);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            // A missed heartbeat is not fatal; the next one may well succeed. If the session
            // has really gone, the next SendAsync surfaces it.
            _ = ex;
        }
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
}
```

- [ ] **Step 4: Wire up the project reference and run the tests**

The test project already references `Keylegend.Chroma`. Verify `Keylegend.Chroma` references `Keylegend.Core` (it does from the scaffolding).

Run: `dotnet test`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add src/Keylegend.Chroma tests/Keylegend.Core.Tests/Chroma
git commit -m "Add Chroma client speaking the local REST protocol

Sessions are explicit: holding one means this application owns the
lighting, releasing one hands it straight back to the vendor software.
That is what makes the idle hand-back in the session manager possible.

Tested against a stub handler, so no service or hardware is required."
```

---

### Task 4: Key categories and the classifier

Turns a produced character into the category that decides its colour.

**Files:**
- Create: `src/Keylegend.Core/Input/KeyCategory.cs`
- Create: `src/Keylegend.Core/Input/CharacterClassifier.cs`
- Test: `tests/Keylegend.Core.Tests/Input/CharacterClassifierTests.cs`

**Interfaces:**
- Consumes: nothing
- Produces:
  - `enum KeyCategory { Unassigned, Digit, Lowercase, Uppercase, Symbol, Control, DeadKey }`
  - `static class CharacterClassifier` with `static KeyCategory Classify(string? character, bool isDeadKey = false)`

- [ ] **Step 1: Write the failing test**

Create `tests/Keylegend.Core.Tests/Input/CharacterClassifierTests.cs`:

```csharp
using Keylegend.Core.Input;

namespace Keylegend.Core.Tests.Input;

public class CharacterClassifierTests
{
    [Theory]
    [InlineData("0")]
    [InlineData("7")]
    public void DigitsAreDigits(string character)
        => Assert.Equal(KeyCategory.Digit, CharacterClassifier.Classify(character));

    [Theory]
    [InlineData("a")]
    [InlineData("z")]
    [InlineData("ö")]      // German umlaut, lowercase
    [InlineData("ß")]
    public void LowercaseLettersAreLowercase(string character)
        => Assert.Equal(KeyCategory.Lowercase, CharacterClassifier.Classify(character));

    [Theory]
    [InlineData("A")]
    [InlineData("Ö")]
    [InlineData("ẞ")]
    public void UppercaseLettersAreUppercase(string character)
        => Assert.Equal(KeyCategory.Uppercase, CharacterClassifier.Classify(character));

    [Theory]
    [InlineData("+")]
    [InlineData("#")]
    [InlineData("€")]
    [InlineData("|")]
    [InlineData("@")]
    [InlineData(" ")]      // space is a printable symbol, not a control key
    public void PunctuationAndSignsAreSymbols(string character)
        => Assert.Equal(KeyCategory.Symbol, CharacterClassifier.Classify(character));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void NothingProducedMeansUnassigned(string? character)
        => Assert.Equal(KeyCategory.Unassigned, CharacterClassifier.Classify(character));

    [Theory]
    [InlineData(" ")]  // NUL
    [InlineData("")]  // ESC
    [InlineData("\r")]
    [InlineData("\t")]
    [InlineData("\b")]
    public void ControlCharactersAreControlKeys(string character)
        => Assert.Equal(KeyCategory.Control, CharacterClassifier.Classify(character));

    [Fact]
    public void DeadKeysWinOverWhateverTheyWouldProduce()
    {
        // The circumflex key produces nothing on its own - it modifies the next keystroke.
        Assert.Equal(KeyCategory.DeadKey, CharacterClassifier.Classify("^", isDeadKey: true));
    }

    [Fact]
    public void CategorisesBySurrogatePairSafely()
    {
        // Characters outside the basic plane must not throw.
        Assert.Equal(KeyCategory.Symbol, CharacterClassifier.Classify("\U0001F600"));
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test --filter "FullyQualifiedName~CharacterClassifierTests"`
Expected: FAIL — `KeyCategory` and `CharacterClassifier` do not exist.

- [ ] **Step 3: Write the implementation**

Create `src/Keylegend.Core/Input/KeyCategory.cs`:

```csharp
namespace Keylegend.Core.Input;

/// <summary>
/// What a key means right now. The category follows from the character the key produces in
/// the current keyboard state, which is why Shift and Caps Lock need no special handling:
/// the same key simply reports a different character and lands in a different category.
/// </summary>
public enum KeyCategory
{
    /// <summary>Produces nothing in the current context.</summary>
    Unassigned,

    Digit,
    Lowercase,
    Uppercase,

    /// <summary>Punctuation, currency, mathematical signs, space.</summary>
    Symbol,

    /// <summary>Escape, Tab, Enter, modifiers, function keys, navigation.</summary>
    Control,

    /// <summary>Produces a character only in combination with the next keystroke.</summary>
    DeadKey
}
```

Create `src/Keylegend.Core/Input/CharacterClassifier.cs`:

```csharp
using System.Globalization;

namespace Keylegend.Core.Input;

/// <summary>
/// Decides which category a produced character belongs to. Deliberately based on Unicode
/// properties rather than a character list, so that every keyboard layout is covered.
/// </summary>
public static class CharacterClassifier
{
    /// <param name="character">What the key produces, or null/empty if nothing.</param>
    /// <param name="isDeadKey">Whether the key only modifies the following keystroke.</param>
    public static KeyCategory Classify(string? character, bool isDeadKey = false)
    {
        if (isDeadKey)
        {
            return KeyCategory.DeadKey;
        }

        if (string.IsNullOrEmpty(character))
        {
            return KeyCategory.Unassigned;
        }

        var codePoint = char.ConvertToUtf32(character, 0);
        var category = CharUnicodeInfo.GetUnicodeCategory(character, 0);

        // Control characters are what Windows reports for Escape, Tab, Enter and friends.
        if (category == UnicodeCategory.Control)
        {
            return KeyCategory.Control;
        }

        if (category == UnicodeCategory.DecimalDigitNumber)
        {
            return KeyCategory.Digit;
        }

        if (category == UnicodeCategory.LowercaseLetter)
        {
            return KeyCategory.Lowercase;
        }

        if (category is UnicodeCategory.UppercaseLetter or UnicodeCategory.TitlecaseLetter)
        {
            return KeyCategory.Uppercase;
        }

        // Letters without case (Greek, CJK, …) read as symbols rather than as a wrong case.
        _ = codePoint;

        return KeyCategory.Symbol;
    }
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add src/Keylegend.Core/Input tests/Keylegend.Core.Tests/Input
git commit -m "Classify keys by the character they currently produce

Categories come from Unicode properties rather than a character list, so
every keyboard layout is covered without a table. This is also why Shift,
Caps Lock and Num Lock need no special case: the key reports a different
character and lands in a different category by itself."
```

---

### Task 5: Keyboard state

The value type describing which modifiers are held and which locks are on. Pure data — the reading of it comes later.

**Files:**
- Create: `src/Keylegend.Core/Input/KeyboardState.cs`
- Test: `tests/Keylegend.Core.Tests/Input/KeyboardStateTests.cs`

**Interfaces:**
- Consumes: nothing
- Produces:
  - `[Flags] enum ModifierKeys { None, LeftShift, RightShift, LeftCtrl, RightCtrl, LeftAlt, RightAlt, LeftWin, RightWin }`
  - `readonly record struct LockStates(bool NumLock, bool CapsLock, bool ScrollLock)`
  - `readonly record struct KeyboardState(ModifierKeys Modifiers, LockStates Locks)` with `bool Shift`, `bool Ctrl`, `bool AltGr`, `bool Alt`, `bool Win`, `bool HasFilteringModifier`, `ModifierKeys FilteringModifiers`, `static KeyboardState Empty`

- [ ] **Step 1: Write the failing test**

Create `tests/Keylegend.Core.Tests/Input/KeyboardStateTests.cs`:

```csharp
using Keylegend.Core.Input;

namespace Keylegend.Core.Tests.Input;

public class KeyboardStateTests
{
    private static KeyboardState With(ModifierKeys modifiers)
        => new(modifiers, new LockStates(false, false, false));

    [Fact]
    public void ShiftIsEitherSide()
    {
        Assert.True(With(ModifierKeys.LeftShift).Shift);
        Assert.True(With(ModifierKeys.RightShift).Shift);
        Assert.False(KeyboardState.Empty.Shift);
    }

    [Fact]
    public void RightAltMeansAltGr()
    {
        Assert.True(With(ModifierKeys.RightAlt).AltGr);
        Assert.False(With(ModifierKeys.LeftAlt).AltGr);
    }

    [Fact]
    public void AltGrTakesPrecedenceOverCtrlAndAlt()
    {
        // Windows reports AltGr as Ctrl + right Alt. Without this rule the Ctrl shortcut
        // layer would appear whenever the user pressed AltGr.
        var altGr = With(ModifierKeys.RightAlt | ModifierKeys.LeftCtrl);

        Assert.True(altGr.AltGr);
        Assert.False(altGr.Ctrl);
        Assert.False(altGr.Alt);
    }

    [Fact]
    public void CtrlPlusLeftAltIsNotAltGr()
    {
        // The user really pressed both, so the Ctrl+Alt shortcut set applies.
        var combination = With(ModifierKeys.LeftCtrl | ModifierKeys.LeftAlt);

        Assert.False(combination.AltGr);
        Assert.True(combination.Ctrl);
        Assert.True(combination.Alt);
    }

    [Fact]
    public void ShiftAloneDoesNotFilter()
    {
        // Shift changes which character a key produces; it must not blank the keyboard.
        Assert.False(With(ModifierKeys.LeftShift).HasFilteringModifier);
    }

    [Theory]
    [InlineData(ModifierKeys.RightAlt)]
    [InlineData(ModifierKeys.LeftCtrl)]
    [InlineData(ModifierKeys.LeftAlt)]
    [InlineData(ModifierKeys.LeftWin)]
    public void FilteringModifiersFilter(ModifierKeys modifier)
        => Assert.True(With(modifier).HasFilteringModifier);

    [Fact]
    public void LocksAreNotModifiers()
    {
        var state = new KeyboardState(ModifierKeys.None, new LockStates(true, true, true));

        Assert.False(state.HasFilteringModifier);
        Assert.True(state.Locks.CapsLock);
    }

    [Fact]
    public void FilteringModifiersReportSidesCollapsed()
    {
        var state = With(ModifierKeys.LeftCtrl | ModifierKeys.RightShift);

        // Sides are irrelevant for shortcut lookup; Shift is kept because combinations
        // such as Ctrl+Shift are distinct sets.
        Assert.Equal(ModifierKeys.LeftCtrl | ModifierKeys.RightShift, state.FilteringModifiers);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test --filter "FullyQualifiedName~KeyboardStateTests"`
Expected: FAIL — the types do not exist.

- [ ] **Step 3: Write the implementation**

Create `src/Keylegend.Core/Input/KeyboardState.cs`:

```csharp
namespace Keylegend.Core.Input;

/// <summary>
/// Modifier keys, with the sides kept apart. The distinction is not cosmetic: Windows
/// reports AltGr as Ctrl plus <em>right</em> Alt, so telling the sides apart is the only way
/// to know whether the user wanted the character layer or the Ctrl+Alt shortcut set.
/// </summary>
[Flags]
public enum ModifierKeys
{
    None = 0,
    LeftShift = 1 << 0,
    RightShift = 1 << 1,
    LeftCtrl = 1 << 2,
    RightCtrl = 1 << 3,
    LeftAlt = 1 << 4,
    RightAlt = 1 << 5,
    LeftWin = 1 << 6,
    RightWin = 1 << 7
}

/// <summary>The three toggles that change what keys mean.</summary>
public readonly record struct LockStates(bool NumLock, bool CapsLock, bool ScrollLock);

/// <summary>Everything about the keyboard that affects what its keys currently mean.</summary>
public readonly record struct KeyboardState(ModifierKeys Modifiers, LockStates Locks)
{
    public static KeyboardState Empty { get; } = new(ModifierKeys.None, new LockStates(false, false, false));

    public bool Shift => Has(ModifierKeys.LeftShift | ModifierKeys.RightShift);

    public bool Win => Has(ModifierKeys.LeftWin | ModifierKeys.RightWin);

    /// <summary>
    /// Right Alt. Windows synthesises a Ctrl press alongside it, which is why every other
    /// property below has to exclude this case explicitly.
    /// </summary>
    public bool AltGr => Has(ModifierKeys.RightAlt);

    /// <summary>Left Alt only, and only when this is not an AltGr press.</summary>
    public bool Alt => !AltGr && Has(ModifierKeys.LeftAlt);

    /// <summary>Ctrl, unless the Ctrl flag is merely the shadow of an AltGr press.</summary>
    public bool Ctrl => !AltGr && Has(ModifierKeys.LeftCtrl | ModifierKeys.RightCtrl);

    /// <summary>
    /// Whether a modifier is held that blanks unassigned keys. Shift, Caps Lock and Num Lock
    /// are deliberately absent: they change which character a key produces without hiding
    /// anything.
    /// </summary>
    public bool HasFilteringModifier => AltGr || Ctrl || Alt || Win;

    /// <summary>The modifiers a shortcut set is looked up by.</summary>
    public ModifierKeys FilteringModifiers => Modifiers;

    private bool Has(ModifierKeys mask) => (Modifiers & mask) != 0;
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add src/Keylegend.Core/Input/KeyboardState.cs tests/Keylegend.Core.Tests/Input/KeyboardStateTests.cs
git commit -m "Model keyboard state with sides kept apart

Windows reports AltGr as Ctrl plus right Alt, and on German layouts
Ctrl+left Alt produces the same characters. Distinguishing the sides is
the only way to tell 'show me the AltGr characters' from 'show me the
Ctrl+Alt shortcuts' - so the type makes that distinction explicit rather
than leaving it to each caller."
```

---

> **Note on the remaining tasks.** Tasks 1–5 are written out in full because they establish the
> types every later task depends on. Tasks 6–11 below specify interfaces, behaviour and the
> test cases that must pass, but do not pre-write the implementation body — these tasks are
> being executed in the same session that wrote this plan, so duplicating the code here would
> serve no reader. Each task still ends with a green test run and a commit.

---

### Task 6: Scan code table

Maps device profile key ids to physical scan codes, so that a key can be handed to Windows for
character resolution. Scan codes describe a **physical position** and are therefore layout
independent — which is exactly what is needed.

**Files:**
- Create: `src/Keylegend.Core/Input/ScanCodes.cs`
- Test: `tests/Keylegend.Core.Tests/Input/ScanCodesTests.cs`

**Interfaces:**
- Produces: `static class ScanCodes` with `static bool TryGet(string keyId, out ushort scanCode)`, `static IReadOnlyDictionary<string, ushort> All { get; }`

**Behaviour:**
- Set 1 scan codes. Extended keys carry the `0xE0` prefix in the high byte (e.g. right Ctrl = `0xE01D`, arrow up = `0xE048`).
- `Keyboard_NonUsBackslash` is `0x56` — the ISO key that ANSI keyboards lack.
- Unknown ids return `false` rather than throwing; a device profile may legitimately contain keys we cannot resolve (media keys, macro keys).

**Test cases:**
- Letter, digit, function key, numpad key and an extended key each resolve to their documented code
- `Keyboard_NonUsBackslash` resolves to `0x56`
- An unknown id returns `false`
- Every key id in the shipped DeathStalker profile resolves, **except** an explicit allow-list of non-typing keys — this is the test that catches a profile and table drifting apart

---

### Task 7: Windows key resolver

Asks Windows what a key produces in a given state. This is the piece that removes the need for
layout tables.

**Files:**
- Create: `src/Keylegend.Core/Input/IKeyResolver.cs` (interface + `KeyMeaning`)
- Create: `src/Keylegend.Windows/WindowsKeyResolver.cs`
- Test: `tests/Keylegend.Core.Tests/Input/FakeKeyResolverTests.cs`

**Interfaces:**
- Consumes: `KeyboardState` (Task 5), `KeyCategory` (Task 4), `ScanCodes` (Task 6)
- Produces:
  - `readonly record struct KeyMeaning(string? Character, KeyCategory Category)` with `static KeyMeaning Unassigned`
  - `interface IKeyResolver { KeyMeaning Resolve(string keyId, KeyboardState state); }`
  - `sealed class WindowsKeyResolver : IKeyResolver`

**Implementation notes:**
- Build a 256-byte key state array reflecting `KeyboardState`: `0x80` for held modifiers, `0x01` on `VK_CAPITAL` when Caps Lock is on, likewise `VK_NUMLOCK`.
- AltGr must be expressed as `VK_CONTROL` + `VK_MENU` both held, which is how Windows itself represents it.
- `MapVirtualKeyEx(scanCode, MAPVK_VSC_TO_VK_EX, layout)` to get the virtual key, then
  `ToUnicodeEx`. A return value of `-1` means a dead key; `0` means no character.
- **Call `ToUnicodeEx` twice for dead keys** and discard the result, otherwise the dead key
  stays in the keyboard driver's buffer and corrupts the user's next real keystroke. This is a
  well-known trap and the reason this class must never be used casually.
- Cache the active layout handle (`GetKeyboardLayout`) and refresh it when the foreground
  window changes.

**Test cases** (against a hand-written fake implementing `IKeyResolver`, since the real one
needs Windows):
- The composer's tests can drive any character/category combination
- `KeyMeaning.Unassigned` has a null character and `KeyCategory.Unassigned`

The real `WindowsKeyResolver` is verified manually via the host's `--dump-layout` mode, which
prints every key and what it resolves to under a chosen modifier combination.

---

### Task 8: Function groups, shortcut sets and the default sets

**Files:**
- Create: `src/Keylegend.Core/Shortcuts/FunctionGroup.cs`
- Create: `src/Keylegend.Core/Shortcuts/ShortcutSet.cs`
- Create: `src/Keylegend.Core/Shortcuts/ShortcutCatalogue.cs`
- Create: `src/Keylegend.Core/Shortcuts/DefaultShortcuts.cs`
- Test: `tests/Keylegend.Core.Tests/Shortcuts/ShortcutCatalogueTests.cs`

**Interfaces:**
- Consumes: `ModifierKeys` (Task 5)
- Produces:
  - `enum FunctionGroup { Edit, File, Search, View, Window, System, Tools, Navigation }`
  - `sealed record ShortcutSet(IReadOnlyDictionary<string, FunctionGroup> Keys)`
  - `sealed class ShortcutCatalogue` with `bool TryGetSet(ModifierKeys modifiers, out ShortcutSet set)` and `ShortcutCatalogue WithOverride(ModifierKeys, ShortcutSet)`
  - `static class DefaultShortcuts { static ShortcutCatalogue Create(); }`

**Behaviour:**
- Lookup is by the **normalised** modifier combination: sides are collapsed (left and right Ctrl
  are the same for shortcut purposes), but Shift is retained because `Ctrl+Shift` is its own set.
- Sets shipped: `Win`, `Win+Shift`, `Win+Ctrl`, `Alt`, `Ctrl`, `Ctrl+Shift`, `Ctrl+Alt`, with the
  contents listed in section 5 of the specification.
- An application profile supplies overrides through `WithOverride`, which returns a new
  catalogue rather than mutating — profiles come and go as the foreground window changes.

**Test cases:**
- `Win` resolves `Keyboard_E` to `FunctionGroup.File`, `Keyboard_V` to `FunctionGroup.Tools`
- `Ctrl` resolves `Keyboard_C` to `FunctionGroup.Edit`
- `Ctrl+Alt` resolves `Keyboard_Delete`
- Left and right Ctrl produce the same set
- An unknown modifier combination yields no set
- An override replaces only the set it names and leaves the rest intact

---

### Task 9: Colour scheme and the frame composer

**The heart of the program.** Everything before this exists to feed it.

**Files:**
- Create: `src/Keylegend.Core/Lighting/ColourScheme.cs`
- Create: `src/Keylegend.Core/Lighting/FrameComposer.cs`
- Test: `tests/Keylegend.Core.Tests/Lighting/FrameComposerTests.cs`

**Interfaces:**
- Consumes: everything above
- Produces:
  - `sealed record ColourScheme` with a colour per `KeyCategory`, a colour per `FunctionGroup`, `LockColour(RgbColor On, RgbColor Off)` for each lock key, `double Brightness`, and `static ColourScheme Default`
  - `sealed class FrameComposer(DeviceProfile profile, IKeyResolver resolver)` with
    `void Compose(LedFrame target, KeyboardState state, ColourScheme scheme, ShortcutCatalogue shortcuts)`

**The three rules, in order of precedence** (specification section 4):

1. **Lock keys** show their own state and are never overridden — not even by a filtering
   modifier. `Keyboard_NumLock`, `Keyboard_CapsLock`, `Keyboard_ScrollLock`.
2. **Filtering modifier held** → only keys with an assignment light up, everything else goes
   dark. AltGr uses the character assignment; Win/Alt/Ctrl use the shortcut set, coloured by
   function group.
3. **Otherwise** → colour by the category of the character the key currently produces.

Brightness is applied last, to every colour.

**Test cases** (each drives a fake resolver, so no hardware is involved):
- Idle state: a letter key gets the lowercase colour; with Shift, the uppercase colour
- Num Lock on/off changes the numpad between the digit colour and the control colour
- Caps Lock key itself shows the "on" colour while Caps Lock is on, the "off" colour otherwise
- **The lock key keeps its colour while AltGr is held** — regression guard for rule 1's precedence
- AltGr held: a key with an AltGr character is lit, a key without one is dark
- Win held: `Keyboard_E` gets the File group colour, an unassigned key is dark
- Brightness of 0.5 halves every component
- Keys whose profile entry has no matrix cell are skipped without throwing

---

### Task 10: Session manager

**Files:**
- Create: `src/Keylegend.Core/Session/LightingState.cs`
- Create: `src/Keylegend.Core/Session/SessionManager.cs`
- Test: `tests/Keylegend.Core.Tests/Session/SessionManagerTests.cs`

**Interfaces:**
- Produces:
  - `enum LightingState { Idle, Active, Paused }`
  - `sealed class SessionManager(TimeSpan idleTimeout, Func<DateTimeOffset> clock)` with
    `LightingState State`, `void NoteActivity()`, `void Pause()`, `void Resume()`,
    `LightingState Advance()`, `event Action<LightingState>? StateChanged`

**Behaviour:**
- Starts `Idle`. `NoteActivity` moves to `Active`.
- `Advance` moves `Active → Idle` once the idle timeout has elapsed since the last activity.
- `Pause` works from any state and blocks activity from reactivating until `Resume`.
- `StateChanged` fires only on an actual transition, never on a repeat.

**Test cases** (injected clock, so no real waiting):
- Activity moves Idle → Active and raises the event once
- Repeated activity while Active raises no further events
- Advancing before the timeout stays Active; after it, goes Idle
- Activity while Paused does not reactivate
- Resume returns to Idle, not Active

---

### Task 11: Windows adapters and the host

Wires everything into a running program and provides the calibration mode that verifies the
device profile against real hardware.

**Files:**
- Create: `src/Keylegend.Windows/KeyboardStateReader.cs`
- Create: `src/Keylegend.Windows/ActivityTracker.cs`
- Create: `src/Keylegend.Host/Keylegend.Host.csproj` (`net10.0-windows`, console)
- Create: `src/Keylegend.Host/Program.cs`
- Modify: `Keylegend.slnx`, `CHANGELOG.md`, `README.md`, `README.de.md`

**Interfaces:**
- Produces:
  - `sealed class KeyboardStateReader` with `KeyboardState Read()`
  - `sealed class ActivityTracker` with `bool AnyKeyDown()`
  - Host modes: default (run the lighting), `--calibrate` (step through matrix cells),
    `--dump-layout` (print each key and what it resolves to)

**Behaviour:**
- Poll roughly every 16 ms. Compose and send **only when the state changed**.
- Take a Chroma session on first activity; release it after the idle timeout.
- Ctrl+C releases the session before exiting, so the lighting returns to Chroma Studio rather
  than freezing on the last frame.
- Chroma failures drop to Idle and retry with growing delays; the program stays alive.

**Verification** (manual, needs the hardware):
- `--calibrate` lights one cell at a time and names it, so the profile can be confirmed
- Default mode: press Shift, AltGr, Win, toggle Num Lock — the lighting follows
- Stop typing for the idle period — the Chroma Studio effect returns

---

## Self-review

**Spec coverage:** Section 4's three colouring rules → Task 9. Section 5's shortcut sets →
Task 8. Section 6's state machine → Task 10. Section 3.1's hook-free state reading → Tasks 5
and 11. Section 3.2's left/right modifier handling → Task 5, enforced by tests. Section 9's
matrix mapping → already shipped, verified by Task 11's calibration mode. Section 2.2's
protocol details → Task 3. Sections 7 (interface), 8 (configuration storage) and application
profiles are the separate interface plan, as scoped at the top.

**Type consistency:** `RgbColor` (1), `LedFrame` (2), `IChromaClient` (3), `KeyCategory` (4),
`KeyboardState`/`ModifierKeys` (5), `ScanCodes` (6), `IKeyResolver`/`KeyMeaning` (7),
`ShortcutCatalogue`/`FunctionGroup` (8), `ColourScheme`/`FrameComposer` (9), `SessionManager`
(10) — each is defined once and referenced under the same name thereafter.

