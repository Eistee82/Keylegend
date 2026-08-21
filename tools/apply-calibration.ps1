<#
.SYNOPSIS
  Applies calibration findings to a device profile.

.DESCRIPTION
  Calibration produces two kinds of finding, and this script handles both:

  - A key that lit up nothing at all. The vendor's matrix can address the cell, but this
    particular model has no LED there. Such keys keep their geometry - the key exists, after
    all, and the on-screen preview should draw it - but lose their row/column, so nothing is
    sent into the void.

  - A key mapped to the wrong cell. Give the corrected row and column.

  The profile is rewritten in place; a .bak copy is kept.

.PARAMETER ProfilePath
  Path to the device.json to edit.

.PARAMETER Unlit
  Key ids that lit up nothing. Their row/column are cleared.

.PARAMETER Remap
  Corrections as "KeyId=row,column", e.g. "Keyboard_Enter=3,14".

.PARAMETER MarkVerified
  Set "verified": true. Only pass this once every key has been confirmed on hardware.

.EXAMPLE
  .\apply-calibration.ps1 -ProfilePath ..\devices\razer-deathstalker-v2-de\device.json `
      -Unlit Keyboard_Backslash,Keyboard_PauseBreak -MarkVerified
#>
param(
    [Parameter(Mandatory)] [string]   $ProfilePath,
    [string[]] $Unlit = @(),
    [string[]] $Remap = @(),
    [switch]   $MarkVerified
)

$ErrorActionPreference = 'Stop'

if (-not (Test-Path -LiteralPath $ProfilePath)) {
    throw "Device profile not found: $ProfilePath"
}

Copy-Item -LiteralPath $ProfilePath -Destination "$ProfilePath.bak" -Force
$profileData = Get-Content -LiteralPath $ProfilePath -Raw | ConvertFrom-Json

$known = $profileData.keys.id
$changed = 0

foreach ($id in $Unlit) {
    $key = $profileData.keys | Where-Object { $_.id -eq $id }

    if (-not $key) {
        Write-Warning "Unknown key id '$id' - skipped. Known ids include: $($known[0..4] -join ', ') ..."
        continue
    }

    $key.row = $null
    $key.column = $null
    $changed++
    Write-Host "  cleared mapping: $id" -ForegroundColor Yellow
}

foreach ($entry in $Remap) {
    if ($entry -notmatch '^(?<id>[^=]+)=(?<row>\d+),(?<col>\d+)$') {
        throw "Cannot read '$entry'. Expected the form KeyId=row,column."
    }

    $id = $Matches['id']
    $key = $profileData.keys | Where-Object { $_.id -eq $id }

    if (-not $key) {
        Write-Warning "Unknown key id '$id' - skipped."
        continue
    }

    $key.row = [int]$Matches['row']
    $key.column = [int]$Matches['col']
    $changed++
    Write-Host "  remapped: $id -> row $($key.row), column $($key.column)" -ForegroundColor Cyan
}

if ($MarkVerified) {
    $profileData.verified = $true
    Write-Host "  marked as verified" -ForegroundColor Green
}

$profileData | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $ProfilePath -Encoding UTF8

$mapped = ($profileData.keys | Where-Object { $null -ne $_.row }).Count
Write-Host ""
Write-Host ("{0} change(s). {1} of {2} keys now have an LED mapping." -f `
    $changed, $mapped, $profileData.keys.Count)
Write-Host "Run 'dotnet test' to confirm the profile is still valid."
