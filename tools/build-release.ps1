<#
.SYNOPSIS
  Publishes the application, assembles a staging directory, and packages the release.

.DESCRIPTION
  The same script the release workflow runs, so that what CI produces can be reproduced and
  debugged on a desk rather than only in a log.

  Framework-dependent on purpose: self-contained measures 167 MB against 2 MB, for a utility
  whose users mostly have the runtime already and whose installer offers it to those who do not.

  The smoke test in the middle is not ceremony. It asks the published copy rather than the build
  output, because that is where a release breaks while the build and the tests stay green: data the
  project files do not carry into the package. The copy answers for itself through --verify, which
  opens no window and needs neither Razer Synapse nor a keyboard — a build machine has neither.

.PARAMETER Version
  Version to stamp into the assemblies and the file names. Defaults to the one in
  Directory.Build.props, so a local build cannot quietly produce artefacts named after the
  previous release. The release workflow passes the git tag instead.

.PARAMETER Output
  Directory for the staging tree and the finished artefacts.

.PARAMETER SkipInstaller
  Assemble and zip, but do not call Inno Setup. For machines without it.

.EXAMPLE
  tools\build-release.ps1

.EXAMPLE
  tools\build-release.ps1 -Version 1.2.0-rc1
#>
param(
    [string] $Version,
    [string] $Output = "out",
    [switch] $SkipInstaller
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
Push-Location $root

# The version lives in one place. Reading it from there means a local build after a version bump
# cannot quietly produce artefacts named after the previous release.
if (-not $Version) {
    $props = Join-Path $root 'Directory.Build.props'
    $Version = ([xml](Get-Content $props)).Project.PropertyGroup.Version

    if (-not $Version) { throw "No <Version> in $props, and none was given." }

    Write-Host "Version $Version, from Directory.Build.props" -ForegroundColor DarkGray
}

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
    # No --nologo here. Under the Microsoft Testing Platform runner, "dotnet test" hands that
    # option to the test executable, which does not know it: no tests run, and it exits 5.
    dotnet test -v minimal
    if ($LASTEXITCODE -ne 0) { throw "Tests failed - not packaging this." }

    # --------------------------------------------------------------------------------------
    Write-Host "`n== Publish ==" -ForegroundColor Cyan
    Write-Host "  src\Keylegend.App"
    dotnet publish 'src\Keylegend.App' -c Release -o $staging --nologo -v quiet `
        -p:Version=$Version -p:DebugType=none
    if ($LASTEXITCODE -ne 0) { throw "Publishing the application failed." }

    # Symbols are for debugging a build, not for shipping one.
    Get-ChildItem $staging -Filter *.pdb -Recurse | Remove-Item -Force

    foreach ($file in 'LICENSE', 'NOTICE.md', 'README.md', 'CHANGELOG.md') {
        Copy-Item (Join-Path $root $file) $staging -Force
    }

    # --------------------------------------------------------------------------------------
    Write-Host "`n== Smoke test ==" -ForegroundColor Cyan

    $exe = Join-Path $staging 'Keylegend.exe'
    if (-not (Test-Path $exe)) {
        throw "Keylegend.exe is missing from the staging directory."
    }

    # The satellite assemblies, one directory per language. dotnet publish carries them, but a
    # packaging step that walks the tree by hand can drop them, and the interface then shows
    # English to everybody with nothing anywhere saying why.
    $languages = @('de', 'es', 'fr', 'it', 'nl', 'pl', 'pt', 'ru', 'uk', 'zh-Hans')
    $absent = $languages | Where-Object { -not (Test-Path (Join-Path $staging $_)) }
    if ($absent) {
        throw "The staging directory has no texts for: $($absent -join ', ')"
    }
    Write-Host "  $($languages.Count + 1) languages present"

    # Asks the published copy itself. --verify opens no window and touches no keyboard, which
    # is what makes it usable here: a build machine has neither Synapse nor hardware. It answers
    # through its exit code and writes what it found next to the artefacts.
    $report = Join-Path $Output 'verify.txt'
    $verify = Start-Process $exe -ArgumentList '--verify', $report -PassThru -Wait
    if ($verify.ExitCode -ne 0) {
        $found = if (Test-Path $report) { Get-Content $report -Raw } else { '(no report written)' }
        throw "The published copy failed its own check:`n$found"
    }
    Write-Host "  $(((Get-Content $report) | ForEach-Object { $_.Trim() } | Where-Object { $_ }) -join '; ')"

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
