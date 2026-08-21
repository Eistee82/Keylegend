# Keylegend User Interface — Implementation Plan

**Goal:** A window showing the keyboard live, with colour editing, plus a tray icon so the program can run unattended.

**Architecture:** The lighting loop moves out of the console host into a reusable `Keylegend.Engine`, so the window and the console driver run the identical code. The preview is drawn from the device profile and filled by the same `FrameComposer` as the hardware — what the window shows is what lights up, by construction rather than by effort.

**Tech Stack:** WPF on .NET 10, `System.Windows.Forms.NotifyIcon` for the tray.

## Global Constraints

Everything from the lighting-engine plan still applies, in particular:

- **`Keylegend.Core` stays free of platform, network and UI dependencies.**
- **No global keyboard hooks.** State polling only.
- **No hardcoded layout tables.** Meanings come from `ToUnicodeEx`.
- **Device support stays data.** The window must not special-case any keyboard.
- Frames are sent at three rates (immediate on change, briskly after take-over, slow refresh otherwise) — see `docs/en/architecture.md`. Do not "simplify" this.
- Documentation is maintained in English and German; behaviour changes get a `CHANGELOG.md` entry.
- **Idle timeout defaults to 60 seconds.**

---

## File Structure

| File | Responsibility |
|---|---|
| `src/Keylegend.Engine/LightingEngine.cs` | The loop: activity → session → compose → send. Extracted from the host. |
| `src/Keylegend.Engine/EngineSettings.cs` | Idle timeout, brightness, colour scheme, active profile path |
| `src/Keylegend.App/MainWindow.xaml(.cs)` | Window hosting preview and editors |
| `src/Keylegend.App/Views/KeyboardPreview.cs` | Draws the device profile and fills it from a `LedFrame` |
| `src/Keylegend.App/Views/ModifierBar.xaml` | Clickable Shift / Ctrl / Alt / AltGr / Win / locks |
| `src/Keylegend.App/Views/ColourEditor.xaml` | Colour per category and per function group |
| `src/Keylegend.App/Views/SettingsPanel.xaml` | Idle timeout, brightness, autostart |
| `src/Keylegend.App/TrayIcon.cs` | Tray icon, menu, show/hide |
| `src/Keylegend.App/Configuration/ConfigStore.cs` | Load and save settings under `%APPDATA%` |
| `src/Keylegend.Host/Program.cs` | Reduced to a thin driver around the engine |

---

### Task 1: Extract the lighting engine

The loop currently lives inside the console host, where the window cannot reach it.

**Files:**
- Create: `src/Keylegend.Engine/Keylegend.Engine.csproj` (`net10.0-windows`, references Core, Windows, Chroma)
- Create: `src/Keylegend.Engine/LightingEngine.cs`, `EngineSettings.cs`
- Modify: `src/Keylegend.Host/Program.cs` to use it
- Test: `tests/Keylegend.Core.Tests/Engine/LightingEngineTests.cs`

**Interfaces:**
- Produces:
  - `sealed record EngineSettings { TimeSpan IdleTimeout = 60s; ColourScheme Scheme; ShortcutCatalogue Shortcuts; }`
  - `sealed class LightingEngine(DeviceProfile, IChromaClient, IKeyStateSource, IKeyResolver)` with
    `Task RunAsync(CancellationToken)`, `EngineSettings Settings { get; set; }`,
    `event Action<LedFrame>? FrameComposed`, `LightingState State { get; }`,
    `void Pause()`, `void Resume()`
  - `interface IKeyStateSource { KeyboardState Read(); bool AnyKeyDown(); }` — lets tests drive the engine without a keyboard

**Behaviour:** identical to the current host loop, including the three send rates and the retry-with-backoff on Chroma failure. `FrameComposed` fires on every composed frame so the preview can mirror it.

**Test cases:** engine takes over on activity and releases after the timeout (fake clock, fake key source, fake Chroma); `FrameComposed` fires with the same frame that was sent; a Chroma failure drops to idle and retries rather than throwing.

---

### Task 2: Keyboard preview

**Files:**
- Create: `src/Keylegend.App/Views/KeyboardPreview.cs`
- Test: manual — the preview must match the photograph in the device profile folder

**Interfaces:**
- Produces: `sealed class KeyboardPreview : FrameworkElement` with `DeviceProfile Profile { get; set; }`, `void Update(LedFrame frame)`, `event Action<KeyDefinition>? KeyClicked`

**Behaviour:**
- Keys are drawn as rounded rectangles from `x`/`y`/`width`/`height`, scaled to the control while preserving the canvas aspect ratio.
- Each key is filled with its colour from the frame; unlit keys get a dim outline so the layout stays readable.
- Keys **without** a matrix cell are drawn with a hatched outline — the DeathStalker's upper Enter half is exactly this case, and it must be visible rather than silently missing.
- Clicking a key raises `KeyClicked`, which the calibration view uses.

---

### Task 3: Modifier bar and live preview

**Files:**
- Create: `src/Keylegend.App/Views/ModifierBar.xaml(.cs)`
- Modify: `MainWindow`

**Behaviour:** Toggle buttons for Shift, Ctrl, left Alt, AltGr, Win and the three locks. When any is engaged, the preview shows that state instead of the live keyboard, so a layer can be inspected without holding keys down. A "follow keyboard" button returns to live mirroring.

This is the feature that makes colour editing practical: choosing the AltGr colour while physically holding AltGr is impossible.

---

### Task 4: Colour editor

**Files:**
- Create: `src/Keylegend.App/Views/ColourEditor.xaml(.cs)`

**Behaviour:** One swatch per `KeyCategory` and per `FunctionGroup`, plus the three lock pairs and a brightness slider. Editing updates `EngineSettings.Scheme` immediately, so both the preview and the hardware change as the user drags — the whole point of sharing the composer.

---

### Task 5: Settings and configuration storage

**Files:**
- Create: `src/Keylegend.App/Configuration/ConfigStore.cs`, `src/Keylegend.App/Views/SettingsPanel.xaml(.cs)`
- Test: `tests/Keylegend.Core.Tests/Configuration/ConfigStoreTests.cs`

**Behaviour:** JSON under `%APPDATA%\Keylegend\settings.json`. Idle timeout (default **60 s**, presented with the explanation that a take-over costs one to two seconds), brightness, autostart via the `Run` registry key, chosen device profile. Written atomically — write to a temporary file and move — so a crash mid-save cannot leave an unreadable configuration.

**Test cases:** round-trip of a full configuration; a missing file yields defaults; a corrupt file yields defaults plus a warning rather than a crash.

---

### Task 6: Tray icon

**Files:**
- Create: `src/Keylegend.App/TrayIcon.cs`
- Modify: `src/Keylegend.App/App.xaml.cs`

**Behaviour:** Icon in the notification area with menu entries Show, Pause/Resume, Quit. Closing the window hides it to the tray rather than exiting; Quit exits and **releases the Chroma session** so the vendor effect resumes. Requires `<UseWindowsForms>true</UseWindowsForms>`.

---

### Task 7: Calibration view

**Files:**
- Create: `src/Keylegend.App/Views/CalibrationView.xaml(.cs)`

**Behaviour:** The console calibration, but in the window:步 through keys, the preview highlights the expected key, buttons for "correct" and "wrong". Findings are written to the profile directly. Keys are labelled by the character they type, with the identifier secondary — the console version learned this the hard way.

---

## Self-review

**Spec coverage:** Section 7 of the design (preview, clickable modifiers, colour pickers, shortcut editor, profiles, settings, calibration, tray) is covered by tasks 2–7. Section 8's configuration storage is Task 5. The shortcut-set editor and application profiles are deliberately **not** in this plan — they are a third, separate step, because the window is useful without them and this plan is already large.

**Type consistency:** `LightingEngine`/`EngineSettings`/`IKeyStateSource` (1), `KeyboardPreview` (2), `ConfigStore` (5) — each defined once and referenced consistently.
