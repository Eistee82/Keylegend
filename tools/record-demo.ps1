<#
.SYNOPSIS
  Records the frames for the README demonstration by driving the running application.

.DESCRIPTION
  Captures what Keylegend actually does, rather than an illustration of it. The whole claim of
  the design is that the on-screen preview and the hardware are filled by the same code; a
  picture drawn by any other means would quietly stop being true the first time the colouring
  changed.

  Two things make this possible without a screen recorder:

  - The layer buttons on the keyboard tab are ordinary WPF toggles, so UI Automation can press
    them. That shows the modifier layers without holding keys down, which is exactly what those
    buttons exist for.

  - tools\screenshot.ps1 captures through PrintWindow, so Keylegend is photographed correctly
    while a *different* application holds the foreground. That is what makes the second half —
    a profile per application — recordable at all.

  The frames land in the output directory, numbered in order. tools\build-demo-gif.py assembles
  them.

  Run it on a machine with the hardware if you can: the header then reads "verified on hardware"
  rather than naming a generic profile.

.PARAMETER Output
  Directory to write the numbered PNG frames into.

.PARAMETER ExePath
  Keylegend.exe to drive. Defaults to the Release build, falling back to Debug.

.PARAMETER Settle
  Seconds to wait after each state change before capturing. Raise it on a slow machine: the
  lighting engine repaints within a frame or two, but the window needs to have caught up.

.EXAMPLE
  tools\record-demo.ps1 -Output out\frames
#>
param(
    [string] $Output = "out\frames",
    [string] $ExePath,
    [double] $Settle = 1.2
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes

$root = Split-Path -Parent $PSScriptRoot

if (-not $ExePath) {
    $candidates = @(
        "$root\src\Keylegend.App\bin\Release\net10.0-windows\Keylegend.exe",
        "$root\src\Keylegend.App\bin\Debug\net10.0-windows\Keylegend.exe"
    )
    $ExePath = $candidates | Where-Object { Test-Path $_ } | Select-Object -First 1
}

if (-not $ExePath -or -not (Test-Path $ExePath)) {
    throw "Keylegend.exe not found. Build it first, or pass -ExePath."
}

New-Item -ItemType Directory -Path $Output -Force | Out-Null
Get-ChildItem -Path $Output -Filter "frame-*.png" -ErrorAction SilentlyContinue | Remove-Item -Force

Add-Type @"
using System;
using System.Runtime.InteropServices;
public class Demo {
  [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
  [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr h, int cmd);
}
"@

# ------------------------------------------------------------------------------------------
# The application under test
# ------------------------------------------------------------------------------------------

Get-Process Keylegend -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Milliseconds 800

Write-Host "Starting $ExePath"
Start-Process $ExePath | Out-Null

$app = $null
foreach ($attempt in 1..30) {
    Start-Sleep -Milliseconds 500
    $app = Get-Process Keylegend -ErrorAction SilentlyContinue |
        Where-Object MainWindowHandle -ne 0 | Select-Object -First 1
    if ($app) { break }
}

if (-not $app) { throw "Keylegend started but never showed a window." }

# A fixed size keeps every frame identical in shape, which the GIF needs.
$window = [System.Windows.Automation.AutomationElement]::FromHandle($app.MainWindowHandle)

# ------------------------------------------------------------------------------------------
# Interface language
# ------------------------------------------------------------------------------------------
# The window follows the Windows display language, so the recording would come out in whatever
# the recorder happens to run. One picture is shared by all eleven READMEs, so it is put into
# English — the language every translation falls back to — and the original setting is restored
# afterwards, because this is somebody's actual installation.

# Tabs are picked by position, not by name: their headers are translated, and the language is
# exactly what is about to be changed. Order is fixed in the markup — keyboard, colours,
# profiles, shortcuts, settings.
$KeyboardTab = 0
$SettingsTab = 4

function SelectTab([int] $index) {
    $condition = New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
        [System.Windows.Automation.ControlType]::TabItem)
    $tabs = $window.FindAll([System.Windows.Automation.TreeScope]::Descendants, $condition)

    if ($index -ge $tabs.Count) { return $false }

    $tabs[$index].GetCurrentPattern(
        [System.Windows.Automation.SelectionItemPattern]::Pattern).Select()
    Start-Sleep -Milliseconds 700

    return $true
}

# Entries are addressed by position for the same reason the tabs are: their names are in
# whichever language is currently set, which is precisely what this changes. Restoring by the
# name read beforehand would look for "Windows folgen" in an interface that has meanwhile
# started saying "Follow Windows".
$EnglishEntry = 2      # Follow Windows, Deutsch, English, Español, ...

function SetLanguageByIndex([int] $index) {
    $condition = New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::AutomationIdProperty, 'LanguageBox')
    $box = $window.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $condition)

    if (-not $box) { return -1 }

    # WPF builds the entries only once the list is open, so searching a collapsed box finds
    # nothing at all.
    $expand = $box.GetCurrentPattern(
        [System.Windows.Automation.ExpandCollapsePattern]::Pattern)
    $expand.Expand()
    Start-Sleep -Milliseconds 500

    $itemCondition = New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
        [System.Windows.Automation.ControlType]::ListItem)
    $items = $box.FindAll([System.Windows.Automation.TreeScope]::Descendants, $itemCondition)

    $previous = -1
    for ($i = 0; $i -lt $items.Count; $i++) {
        $pattern = $items[$i].GetCurrentPattern(
            [System.Windows.Automation.SelectionItemPattern]::Pattern)
        if ($pattern.Current.IsSelected) { $previous = $i }
    }

    if ($index -ge 0 -and $index -lt $items.Count) {
        $items[$index].GetCurrentPattern(
            [System.Windows.Automation.SelectionItemPattern]::Pattern).Select()
        Start-Sleep -Milliseconds 900
    }
    else {
        Write-Warning "  language entry $index not offered - keeping the current one"
        $expand.Collapse()
    }

    Start-Sleep -Milliseconds 400

    return $previous
}

$script:frame = 0
function Capture([string] $label) {
    $script:frame++
    $path = Join-Path $Output ("frame-{0:d2}-{1}.png" -f $script:frame, $label)
    & "$PSScriptRoot\screenshot.ps1" -Width 1400 -Height 900 -Path $path | Out-Null
    Write-Host ("  {0,-22} {1}" -f $label, (Split-Path -Leaf $path))
}

function Toggle([string] $name, [bool] $on) {
    $condition = New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::AutomationIdProperty, $name)
    $element = $window.FindFirst(
        [System.Windows.Automation.TreeScope]::Descendants, $condition)

    if (-not $element) { throw "Could not find the '$name' button in the window." }

    $toggle = $element.GetCurrentPattern(
        [System.Windows.Automation.TogglePattern]::Pattern)
    $state = $toggle.Current.ToggleState

    $wanted = if ($on) { [System.Windows.Automation.ToggleState]::On }
              else     { [System.Windows.Automation.ToggleState]::Off }

    if ($state -ne $wanted) { $toggle.Toggle() }
    Start-Sleep -Seconds $Settle
}

function ClearLayers() {
    foreach ($name in 'ShiftToggle','AltGrToggle','CtrlToggle','AltToggle','WinToggle') {
        Toggle $name $false
    }
}

# ------------------------------------------------------------------------------------------
# Part one: what a key means
# ------------------------------------------------------------------------------------------

[void][Demo]::SetForegroundWindow($app.MainWindowHandle)
Start-Sleep -Seconds $Settle

$previousLanguage = $null
if (SelectTab $SettingsTab) {
    $previousLanguage = SetLanguageByIndex $EnglishEntry
    Write-Host "Interface set to English (previous entry: $previousLanguage)"
}
else {
    Write-Warning "Settings tab not found - recording in the current interface language"
}

[void](SelectTab $KeyboardTab)
Start-Sleep -Seconds $Settle

Write-Host "`nPart 1 - key meaning and modifier layers"

ClearLayers
Capture "base"

Toggle 'ShiftToggle' $true;  Capture "shift"
Toggle 'ShiftToggle' $false
Toggle 'AltGrToggle' $true;  Capture "altgr"
Toggle 'AltGrToggle' $false
Toggle 'WinToggle'   $true;  Capture "win"
Toggle 'WinToggle'   $false
Toggle 'CtrlToggle'  $true;  Capture "ctrl"
Toggle 'CtrlToggle'  $false

# Caps Lock on, and — because NumToggle stays off while another lock is engaged — Num Lock off
# with it. One frame, two behaviours: the letters take the uppercase colour, and the number pad
# recolours to navigation. Both are real, and both are captioned as such.
Toggle 'CapsToggle' $true
Capture "locks"
Toggle 'CapsToggle' $false

ClearLayers

# ------------------------------------------------------------------------------------------
# Part two: a profile per application
# ------------------------------------------------------------------------------------------
# Only programs that are on every Windows installation and have a shipped profile, so that this
# reproduces on somebody else's machine.
#
# The Ctrl layer is engaged for this part, and that is not decoration. These three profiles
# define shortcut layers and no highlights, so with no modifier held they are identical to the
# defaults by design — a profile that has no opinion about the base layer does not invent one.
# Ctrl is where they actually differ, and showing them without it would record three identical
# pictures and call them a demonstration.

Write-Host "`nPart 2 - a profile per application"

Toggle 'CtrlToggle' $true

$demoApps = @(
    @{ Name = 'notepad';  Start = { Start-Process notepad -PassThru } },
    @{ Name = 'terminal'; Start = { Start-Process wt -PassThru } },
    @{ Name = 'explorer'; Start = { Start-Process explorer.exe -ArgumentList $env:USERPROFILE -PassThru } }
)

$started = @()

foreach ($demo in $demoApps) {
    Write-Host "  bringing $($demo.Name) to the front"

    try {
        $process = & $demo.Start
    }
    catch {
        Write-Warning "  could not start $($demo.Name): $($_.Exception.Message) - skipping"
        continue
    }

    # Explorer hands off to the running instance and exits, so the handle has to be looked up.
    Start-Sleep -Seconds 2
    if (-not $process -or $process.HasExited -or $process.MainWindowHandle -eq 0) {
        $process = Get-Process -ErrorAction SilentlyContinue |
            Where-Object { $_.MainWindowHandle -ne 0 -and $_.ProcessName -match $demo.Name } |
            Select-Object -First 1
    }

    if (-not $process) {
        Write-Warning "  no window for $($demo.Name) - skipping"
        continue
    }

    $started += $process
    [void][Demo]::SetForegroundWindow($process.MainWindowHandle)
    Start-Sleep -Seconds ($Settle * 2)

    # Keylegend is behind another window here. PrintWindow captures it anyway.
    Capture "app-$($demo.Name)"
}

foreach ($process in $started) {
    if (-not $process.HasExited -and $process.ProcessName -ne 'explorer') {
        $process.CloseMainWindow() | Out-Null
    }
}

[void][Demo]::SetForegroundWindow($app.MainWindowHandle)
Start-Sleep -Seconds $Settle
Toggle 'CtrlToggle' $false
Capture "back"

if ($previousLanguage -ge 0) {
    [void](SelectTab $SettingsTab)
    [void](SetLanguageByIndex $previousLanguage)
    [void](SelectTab $KeyboardTab)
    Write-Host "Interface language restored (entry $previousLanguage)"
}

Write-Host ("`n{0} frames written to {1}" -f $script:frame, (Resolve-Path $Output))
Write-Host "Assemble them with: python tools\build-demo-gif.py --frames $Output"
