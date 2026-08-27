<#
.SYNOPSIS
  Fills the winget manifests in with a released version and its real hash.

.DESCRIPTION
  The manifests under packaging\winget describe Keylegend for the Windows Package Manager
  community repository. Two things in them change with every release — the version and the
  installer's SHA256 — and one of them cannot be known until the file is actually published:
  validation compares the hash against what the release serves, and rightly fails if they differ.

  So this reads the hash from the published download rather than from a local build. A local
  artefact and a published one should be identical, but "should be" is not a thing to submit a
  manifest on.

  What it does not do is open the pull request. That is deliberate: submitting to
  microsoft/winget-pkgs puts something in front of other people's moderators under your name, and
  it is worth looking at the files first. When you are ready:

      winget install wingetcreate
      wingetcreate submit --token <github-pat> packaging\winget

  Or fork winget-pkgs and copy the four files to
  manifests\e\Eistee82\Keylegend\<version>\.

.PARAMETER Version
  Version to write into the manifests, without a leading v.

.PARAMETER FromFile
  Take the hash from a local file instead of downloading. For a dry run before the release exists.

.EXAMPLE
  tools\winget-manifest.ps1 -Version 1.1.0
#>
param(
    [Parameter(Mandatory)] [string] $Version,
    [string] $FromFile
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$folder = Join-Path $root 'packaging\winget'
$installer = "Keylegend-$Version-setup.exe"
$url = "https://github.com/Eistee82/Keylegend/releases/download/v$Version/$installer"

if ($FromFile) {
    if (-not (Test-Path $FromFile)) { throw "No such file: $FromFile" }
    $hash = (Get-FileHash $FromFile -Algorithm SHA256).Hash.ToLower()
    Write-Warning "Hash taken from a local file. Re-run without -FromFile before submitting."
}
else {
    Write-Host "Downloading $url"
    $temp = Join-Path ([System.IO.Path]::GetTempPath()) $installer

    try {
        Invoke-WebRequest -Uri $url -OutFile $temp -UseBasicParsing
    }
    catch {
        throw "Could not download the release. Is v$Version published and not still a draft?`n$($_.Exception.Message)"
    }

    $hash = (Get-FileHash $temp -Algorithm SHA256).Hash.ToLower()
    Remove-Item $temp -Force
}

Write-Host "SHA256 $hash"

# The date belongs to the release, not to whenever this script happens to run.
$released = (Get-Date).ToString('yyyy-MM-dd')

foreach ($file in Get-ChildItem $folder -Filter *.yaml) {
    $text = Get-Content $file.FullName -Raw

    $text = $text -replace '(?m)^PackageVersion:\s*.+$', "PackageVersion: $Version"
    $text = $text -replace '(?m)^ReleaseDate:\s*.+$', "ReleaseDate: $released"
    $text = $text -replace '(?m)^(\s*)InstallerUrl:\s*.+$', "`$1InstallerUrl: $url"
    $text = $text -replace '(?m)^(\s*)InstallerSha256:\s*.+$', "`$1InstallerSha256: $hash"
    $text = $text -replace 'releases/tag/v[\d.]+', "releases/tag/v$Version"

    Set-Content $file.FullName $text -NoNewline -Encoding UTF8
    Write-Host "  $($file.Name)"
}

Write-Host "`nManifests updated for $Version." -ForegroundColor Green
Write-Host "Check them, then submit with: wingetcreate submit --token <pat> $folder"
