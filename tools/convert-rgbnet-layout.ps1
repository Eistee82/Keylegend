<#
.SYNOPSIS
  Converts an RGB.NET / Artemis device layout (XML) into a Keylegend device profile (JSON).

.DESCRIPTION
  Reads the geometry from an RGB.NET layout file and combines it with the Razer Chroma
  key matrix (row/column) to produce a device.json. The matrix table below follows
  Razer's own RZKEY definitions from RzChromaSDKTypes.h, where each key is encoded as
  0xRRCC (RR = row, CC = column).

  Key names in Razer's table follow the US layout. On a German keyboard the physical
  Z sits where Razer calls Y and vice versa - this only affects naming, not position.

.PARAMETER LayoutPath
  Path to the RGB.NET layout XML.

.PARAMETER ImageName
  File name of the keyboard picture that ships next to the profile.

.PARAMETER OutputPath
  Path of the device.json to write.

.EXAMPLE
  .\convert-rgbnet-layout.ps1 -LayoutPath "layout.xml" -ImageName "device.png" -OutputPath "device.json"
#>
param(
    [Parameter(Mandatory)] [string] $LayoutPath,
    [Parameter(Mandatory)] [string] $ImageName,
    [Parameter(Mandatory)] [string] $OutputPath,
    [string] $DeviceName = "Unknown keyboard",
    [string] $Vendor     = "Unknown",
    [string] $Layout     = "ISO-DE"
)

$ErrorActionPreference = 'Stop'
$ci = [System.Globalization.CultureInfo]::InvariantCulture

# RGB.NET LedId -> Razer matrix cell (row, column).
# Source: RZKEY_* constants in Razer's RzChromaSDKTypes.h (0xRRCC).
$MATRIX = @{
    'Keyboard_Escape'=@(0,1)
    'Keyboard_F1'=@(0,3);  'Keyboard_F2'=@(0,4);  'Keyboard_F3'=@(0,5);  'Keyboard_F4'=@(0,6)
    'Keyboard_F5'=@(0,7);  'Keyboard_F6'=@(0,8);  'Keyboard_F7'=@(0,9);  'Keyboard_F8'=@(0,10)
    'Keyboard_F9'=@(0,11); 'Keyboard_F10'=@(0,12);'Keyboard_F11'=@(0,13);'Keyboard_F12'=@(0,14)
    'Keyboard_PrintScreen'=@(0,15); 'Keyboard_ScrollLock'=@(0,16); 'Keyboard_PauseBreak'=@(0,17)

    'Keyboard_GraveAccentAndTilde'=@(1,1)     # OEM_1  - circumflex on DE
    'Keyboard_1'=@(1,2); 'Keyboard_2'=@(1,3); 'Keyboard_3'=@(1,4); 'Keyboard_4'=@(1,5)
    'Keyboard_5'=@(1,6); 'Keyboard_6'=@(1,7); 'Keyboard_7'=@(1,8); 'Keyboard_8'=@(1,9)
    'Keyboard_9'=@(1,10);'Keyboard_0'=@(1,11)
    'Keyboard_MinusAndUnderscore'=@(1,12)     # OEM_2  - eszett on DE
    'Keyboard_EqualsAndPlus'=@(1,13)          # OEM_3  - acute on DE
    'Keyboard_Backspace'=@(1,14)
    'Keyboard_Insert'=@(1,15); 'Keyboard_Home'=@(1,16); 'Keyboard_PageUp'=@(1,17)
    'Keyboard_NumLock'=@(1,18); 'Keyboard_NumSlash'=@(1,19)
    'Keyboard_NumAsterisk'=@(1,20); 'Keyboard_NumMinus'=@(1,21)

    'Keyboard_Tab'=@(2,1)
    'Keyboard_Q'=@(2,2); 'Keyboard_W'=@(2,3); 'Keyboard_E'=@(2,4); 'Keyboard_R'=@(2,5)
    'Keyboard_T'=@(2,6); 'Keyboard_Y'=@(2,7); 'Keyboard_U'=@(2,8); 'Keyboard_I'=@(2,9)
    'Keyboard_O'=@(2,10);'Keyboard_P'=@(2,11)
    'Keyboard_BracketLeft'=@(2,12)            # OEM_4  - U-umlaut on DE
    'Keyboard_BracketRight'=@(2,13)           # OEM_5  - plus on DE
    'Keyboard_Backslash'=@(2,14)              # OEM_6  - upper half of the ISO enter
    'Keyboard_Delete'=@(2,15); 'Keyboard_End'=@(2,16); 'Keyboard_PageDown'=@(2,17)
    'Keyboard_Num7'=@(2,18); 'Keyboard_Num8'=@(2,19); 'Keyboard_Num9'=@(2,20)
    'Keyboard_NumPlus'=@(2,21)

    'Keyboard_CapsLock'=@(3,1)
    'Keyboard_A'=@(3,2); 'Keyboard_S'=@(3,3); 'Keyboard_D'=@(3,4); 'Keyboard_F'=@(3,5)
    'Keyboard_G'=@(3,6); 'Keyboard_H'=@(3,7); 'Keyboard_J'=@(3,8); 'Keyboard_K'=@(3,9)
    'Keyboard_L'=@(3,10)
    'Keyboard_SemicolonAndColon'=@(3,11)      # OEM_7  - O-umlaut on DE
    'Keyboard_ApostropheAndDoubleQuote'=@(3,12) # OEM_8 - A-umlaut on DE
    'Keyboard_NonUsTilde'=@(3,13)             # EUR_1  - hash key, ISO only
    'Keyboard_Enter'=@(3,14)
    'Keyboard_Num4'=@(3,18); 'Keyboard_Num5'=@(3,19); 'Keyboard_Num6'=@(3,20)

    'Keyboard_LeftShift'=@(4,1)
    'Keyboard_NonUsBackslash'=@(4,2)          # EUR_2  - the extra ISO key left of Y/Z
    'Keyboard_Z'=@(4,3); 'Keyboard_X'=@(4,4); 'Keyboard_C'=@(4,5); 'Keyboard_V'=@(4,6)
    'Keyboard_B'=@(4,7); 'Keyboard_N'=@(4,8); 'Keyboard_M'=@(4,9)
    'Keyboard_CommaAndLessThan'=@(4,10)       # OEM_9
    'Keyboard_PeriodAndBiggerThan'=@(4,11)    # OEM_10
    'Keyboard_SlashAndQuestionMark'=@(4,12)   # OEM_11 - hyphen on DE
    'Keyboard_RightShift'=@(4,14)
    'Keyboard_ArrowUp'=@(4,16)
    'Keyboard_Num1'=@(4,18); 'Keyboard_Num2'=@(4,19); 'Keyboard_Num3'=@(4,20)
    'Keyboard_NumEnter'=@(4,21)

    'Keyboard_LeftCtrl'=@(5,1); 'Keyboard_LeftGui'=@(5,2); 'Keyboard_LeftAlt'=@(5,3)
    'Keyboard_Space'=@(5,7); 'Keyboard_RightAlt'=@(5,11)
    'Keyboard_Function'=@(5,12)               # RZKEY_FN
    'Keyboard_RightGui'=@(5,12)               # some layouts put fn here
    'Keyboard_Application'=@(5,13)            # RZKEY_RMENU - context menu key
    'Keyboard_RightCtrl'=@(5,14)
    'Keyboard_ArrowLeft'=@(5,15); 'Keyboard_ArrowDown'=@(5,16); 'Keyboard_ArrowRight'=@(5,17)
    'Keyboard_Num0'=@(5,19); 'Keyboard_NumPeriodAndDelete'=@(5,20)
}

[xml]$doc = Get-Content -LiteralPath $LayoutPath -Raw
$unit = 19.0
$px = 0.0; $py = 0.0; $pw = 0.0; $ph = 0.0
$keys = @()
$unmapped = @()

function Get-Size([string]$v) {
    if ([string]::IsNullOrWhiteSpace($v)) { return $unit }
    return [double]::Parse($v, $ci) * $unit
}

foreach ($led in $doc.Device.Leds.Led) {
    $w = Get-Size $led.Width
    $h = Get-Size $led.Height

    # X: empty means "directly after the previous key"; "+n" adds a gap of n
    $vx = $led.X
    if ([string]::IsNullOrWhiteSpace($vx))          { $x = $px + $pw }
    elseif ($vx.Trim() -eq '+')                     { $x = $px + $pw }
    elseif ($vx.Trim().StartsWith('+'))             { $x = $px + $pw + [double]::Parse($vx.Trim().Substring(1), $ci) }
    else                                            { $x = [double]::Parse($vx.Trim(), $ci) }

    # Y: empty means "same row"; "+" drops by the previous height, "~" by its own
    $vy = $led.Y
    if ([string]::IsNullOrWhiteSpace($vy))          { $y = $py }
    elseif ($vy.Trim() -eq '+')                     { $y = $py + $ph }
    elseif ($vy.Trim() -eq '~')                     { $y = $py + $h }
    else                                            { $y = [double]::Parse($vy.Trim(), $ci) }

    $id = $led.Id
    $cell = $MATRIX[$id]
    if ($null -eq $cell) { $unmapped += $id }

    $keys += [ordered]@{
        id     = $id
        x      = [math]::Round($x, 2)
        y      = [math]::Round($y, 2)
        width  = [math]::Round($w, 2)
        height = [math]::Round($h, 2)
        row    = if ($cell) { $cell[0] } else { $null }
        column = if ($cell) { $cell[1] } else { $null }
    }

    $px = $x; $py = $y; $pw = $w; $ph = $h
}

$profile = [ordered]@{
    '$schema'    = "../device-profile.schema.json"
    formatVersion = 1
    name         = $DeviceName
    vendor       = $Vendor
    model        = $doc.Device.Model
    physicalLayout = $Layout
    image        = $ImageName
    canvas       = [ordered]@{
        width  = [double]$doc.Device.Width
        height = [double]$doc.Device.Height
    }
    matrix       = [ordered]@{ rows = 6; columns = 22 }
    verified     = $false
    keys         = $keys
}

$json = $profile | ConvertTo-Json -Depth 6
Set-Content -LiteralPath $OutputPath -Value $json -Encoding UTF8

Write-Host ("Wrote {0} keys to {1}" -f $keys.Count, $OutputPath)
if ($unmapped.Count -gt 0) {
    Write-Warning ("{0} key(s) without a matrix cell: {1}" -f $unmapped.Count, ($unmapped -join ', '))
    Write-Warning "Fill these in manually or use the calibration mode."
}
