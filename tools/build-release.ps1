<#
.SYNOPSIS
  Publishes both executables, assembles a staging directory, and packages the release.

.DESCRIPTION
  The same script the release workflow runs, so that what CI produces can be reproduced and
  debugged on a desk rather than only in a log.

  Framework-dependent on purpose: self-contained measures 167 MB against 2 MB, for a utility
  whose users mostly have the runtime already and whose installer offers it to those who do not.

  The smoke test in the middle is not ceremony. dotnet publish does not carry devices\ unless
  the project files ask it to, and without those profiles every installed copy starts and
  immediately gives up with "No device profile found". That went unnoticed through development,
  where the repository's own folder always happens to be a few levels up. It cannot go unnoticed
  again.

.PARAMETER Version
  Version to stamp into the assemblies and the file names.

.PARAMETER Output
  Directory for the staging tree and the finished artefacts.

.PARAMETER SkipInstaller
  Assemble and zip, but do not call Inno Setup. For machines without it.

.EXAMPLE
  tools\build-release.ps1 -Version 1.0.0
#>
param(
    [string] $Version = "1.0.0",
    [string] $Output = "out",
    [switch] $SkipInstaller
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
Push-Location $root

try {
    $staging = Join-Path $Output "staging"

    if (Test-Path $staging) { Remove-Item $staging -Recurse -Force }

    # Artefacts from an earlier run go too. Otherwise the checksum file lists files this build
    # did not produce — and the release workflow uploads whatever happens to be lying about,
    # which on a machine that has built more than one version is the wrong ones.
    Get-ChildItem $Output -File -ErrorAction SilentlyContinue |
        Where-Object { $_.Extension -in '.zip', '.exe' -or $_.Name -eq 'SHA256SUMS.txt' } |
        Remove-Item -Force

    New-Item -ItemType Directory -Path $staging -Force | Out-Null

    # --------------------------------------------------------------------------------------
    Write-Host "`n== Tests ==" -ForegroundColor Cyan
    dotnet test --nologo -v minimal
    if ($LASTEXITCODE -ne 0) { throw "Tests failed - not packaging this." }

    # --------------------------------------------------------------------------------------
    Write-Host "`n== Publish ==" -ForegroundColor Cyan
    foreach ($project in 'src\Keylegend.App', 'src\Keylegend.Host') {
        Write-Host "  $project"
        dotnet publish $project -c Release -o $staging --nologo -v quiet `
            -p:Version=$Version -p:DebugType=none
        if ($LASTEXITCODE -ne 0) { throw "Publishing $project failed." }
    }

    # Symbols are for debugging a build, not for shipping one.
    Get-ChildItem $staging -Filter *.pdb -Recurse | Remove-Item -Force

    foreach ($file in 'LICENSE', 'NOTICE.md', 'README.md', 'CHANGELOG.md') {
        Copy-Item (Join-Path $root $file) $staging -Force
    }

    # --------------------------------------------------------------------------------------
    Write-Host "`n== Smoke test ==" -ForegroundColor Cyan

    # Both programs, by name. Windows file names are case-insensitive, so the console tool was
    # once called keylegend.exe and quietly overwrote the application's Keylegend.exe when the
    # two were published into one directory. What shipped was a release with no interface, and
    # nothing in the build said so. Counting the files is what catches that.
    foreach ($expected in 'Keylegend.exe', 'keylegend-cli.exe') {
        $path = Join-Path $staging $expected
        if (-not (Test-Path $path)) {
            throw "$expected is missing from the staging directory."
        }
    }

    # Same name, different subsystem: the application is a GUI binary, the tool a console one.
    # If one had overwritten the other they would be byte-identical.
    $app = Get-FileHash (Join-Path $staging 'Keylegend.exe') -Algorithm SHA256
    $cli = Get-FileHash (Join-Path $staging 'keylegend-cli.exe') -Algorithm SHA256
    if ($app.Hash -eq $cli.Hash) {
        throw "Keylegend.exe and keylegend-cli.exe are the same file. One has overwritten the other."
    }
    Write-Host "  both executables present and distinct"

    $profiles = Get-ChildItem (Join-Path $staging "devices") -Filter device.json -Recurse -ErrorAction SilentlyContinue
    if (-not $profiles) {
        throw "The staging directory has no device profiles. An installed copy would refuse to start."
    }
    Write-Host "  $($profiles.Count) device profiles present"

    # Runs the real program against the real layout, from the directory a user would have.
    $dump = & (Join-Path $staging "keylegend-cli.exe") --dump-layout 2>&1 | Out-String
    if ($LASTEXITCODE -ne 0 -or $dump -match 'No device profile found') {
        throw "The published copy could not find a device profile:`n$dump"
    }
    Write-Host "  $(($dump -split "`n")[0].Trim())"

    # --------------------------------------------------------------------------------------
    Write-Host "`n== Package ==" -ForegroundColor Cyan

    $zip = Join-Path $Output "Keylegend-$Version-portable.zip"
    if (Test-Path $zip) { Remove-Item $zip -Force }
    Compress-Archive -Path (Join-Path $staging '*') -DestinationPath $zip
    Write-Host "  $(Split-Path -Leaf $zip)  ($([math]::Round((Get-Item $zip).Length / 1MB, 1)) MB)"

    if (-not $SkipInstaller) {
        # Chocolatey and the .exe installer land in Program Files; winget installs per user.
        # Both are ordinary installations of the same tool, so both are worth looking in.
        $iscc = @(
            "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
            "$env:ProgramFiles\Inno Setup 6\ISCC.exe",
            "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe",
            (Get-Command ISCC.exe -ErrorAction SilentlyContinue).Source
        ) | Where-Object { $_ -and (Test-Path $_) } | Select-Object -First 1

        if (-not $iscc) {
            Write-Warning "Inno Setup not found - skipping the installer. Install it, or pass -SkipInstaller."
        }
        else {
            & $iscc /Q "/DVersion=$Version" "/DSource=$((Resolve-Path $staging).Path)" `
                (Join-Path $root "installer\keylegend.iss")
            if ($LASTEXITCODE -ne 0) { throw "Inno Setup failed." }

            $setup = Join-Path $Output "Keylegend-$Version-setup.exe"
            Write-Host "  $(Split-Path -Leaf $setup)  ($([math]::Round((Get-Item $setup).Length / 1MB, 1)) MB)"
        }
    }

    # --------------------------------------------------------------------------------------
    Write-Host "`n== Checksums ==" -ForegroundColor Cyan

    $sums = Join-Path $Output "SHA256SUMS.txt"
    Get-ChildItem $Output -File |
        Where-Object { $_.Extension -in '.zip', '.exe' } |
        ForEach-Object { "{0}  {1}" -f (Get-FileHash $_.FullName -Algorithm SHA256).Hash.ToLower(), $_.Name } |
        Tee-Object -FilePath $sums |
        ForEach-Object { Write-Host "  $_" }

    Write-Host "`nArtefacts in $((Resolve-Path $Output).Path)" -ForegroundColor Green
}
finally {
    Pop-Location
}
