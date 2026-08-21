<#
.SYNOPSIS
  Captures the Keylegend window to a PNG.

.DESCRIPTION
  For checking the interface against the product photograph, and for documentation images.

  Two details make this work where a naive screen capture does not:

  - SetProcessDPIAware. Without it, a non-DPI-aware caller receives virtualised window
    coordinates while PrintWindow renders in real pixels, so the capture comes out cropped at
    the right and bottom. That is easy to mistake for a layout bug in the application - it cost
    an embarrassing number of attempts once.

  - PrintWindow rather than CopyFromScreen, so the window is captured even when it is not in
    the foreground and nothing else can end up in the picture.

.PARAMETER Width
  Window width to set before capturing.

.PARAMETER Height
  Window height to set before capturing.

.PARAMETER Path
  Where to write the PNG.

.EXAMPLE
  tools\screenshot.ps1 -Width 1500 -Height 950 -Path docs\images\keyboard.png
#>
param(
    [int]    $Width  = 1500,
    [int]    $Height = 950,
    [string] $Path   = "keylegend.png"
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

Add-Type @"
using System;
using System.Runtime.InteropServices;
public class Capture {
  [DllImport("user32.dll")] public static extern bool SetProcessDPIAware();
  [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out Rect r);
  [DllImport("user32.dll")] public static extern bool MoveWindow(IntPtr h, int x, int y, int w, int ht, bool repaint);
  [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr h, int cmd);
  [DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr h, IntPtr hdc, uint flags);
  public struct Rect { public int Left, Top, Right, Bottom; }
}
"@

[void][Capture]::SetProcessDPIAware()

# A running Keylegend shows up as more than one process - the executable and the .NET host that
# launched it - and only one of them owns the window. Taking the first one hands PrintWindow a
# null handle, which surfaces much further down as "Parameter is not valid" from the Bitmap
# constructor, because GetWindowRect then reports a zero-sized rectangle.
$process = Get-Process Keylegend -ErrorAction SilentlyContinue |
    Where-Object { $_.MainWindowHandle -ne 0 } |
    Select-Object -First 1

if (-not $process) {
    if (Get-Process Keylegend -ErrorAction SilentlyContinue) {
        throw "Keylegend is running but has no window. It may be minimised to the notification area - open it from the tray icon first."
    }

    throw "Keylegend is not running. Start it first."
}

[void][Capture]::ShowWindow($process.MainWindowHandle, 9)      # restore if minimised
[void][Capture]::MoveWindow($process.MainWindowHandle, 40, 40, $Width, $Height, $true)
Start-Sleep -Milliseconds 1000

$rect = New-Object Capture+Rect
[void][Capture]::GetWindowRect($process.MainWindowHandle, [ref]$rect)

$w = $rect.Right - $rect.Left
$h = $rect.Bottom - $rect.Top

$bitmap = New-Object System.Drawing.Bitmap $w, $h
$graphics = [System.Drawing.Graphics]::FromImage($bitmap)
$hdc = $graphics.GetHdc()

# 2 = PW_RENDERFULLCONTENT, required for WPF windows.
[void][Capture]::PrintWindow($process.MainWindowHandle, $hdc, 2)

$graphics.ReleaseHdc($hdc)
$bitmap.Save($Path, [System.Drawing.Imaging.ImageFormat]::Png)
$graphics.Dispose()
$bitmap.Dispose()

Write-Host ("Wrote {0} ({1} x {2})" -f $Path, $w, $h)
