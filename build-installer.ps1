param(
    [ValidateSet("portable", "runtime")]
    [string]$Mode = "portable",

    [switch]$SkipTests
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$project = Join-Path $root "TrayMedia.App\TrayMedia.csproj"
$dist = Join-Path $root "dist"
$payload = Join-Path $dist "installer-payload"
$iss = Join-Path $root "installer\TrayMedia.iss"
$testScript = Join-Path $root "test.ps1"
$version = "1.0.0-beta.1"

$isccCandidates = @(
    "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
    "${env:ProgramFiles}\Inno Setup 6\ISCC.exe",
    "${env:LocalAppData}\Programs\Inno Setup 6\ISCC.exe"
)
$iscc = $isccCandidates | Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $iscc) {
    throw "Inno Setup 6 (ISCC.exe) not found. Install JRSoftware.InnoSetup via winget."
}

if (-not $SkipTests) {
    & $testScript -Configuration Release
    if ($LASTEXITCODE -ne 0) {
        throw "Build aborted: tests failed."
    }
}

if (Test-Path $payload) {
    Remove-Item -Recurse -Force $payload
}
New-Item -ItemType Directory -Path $payload | Out-Null

Write-Host "Publishing payload ($Mode)..."
if ($Mode -eq "portable") {
    dotnet publish $project -c Release -r win-x64 --self-contained true `
        -p:PublishSingleFile=true `
        -p:EnableCompressionInSingleFile=true `
        -p:IncludeNativeLibrariesForSelfExtract=true `
        -o $payload
} else {
    dotnet publish $project -c Release -r win-x64 --self-contained false `
        -p:PublishSingleFile=true `
        -p:EnableCompressionInSingleFile=false `
        -o $payload
}

if ($LASTEXITCODE -ne 0) {
    throw "Publish failed (exit $LASTEXITCODE)."
}

Write-Host "Compiling installer with Inno Setup..."
& $iscc $iss
if ($LASTEXITCODE -ne 0) {
    throw "ISCC failed (exit $LASTEXITCODE)."
}

$setup = Get-Item (Join-Path $dist "TrayMedia-Setup-$version.exe")
Write-Host ""
Write-Host "Installer: $($setup.FullName)"
Write-Host ("Size:      {0:N1} MB" -f ($setup.Length / 1MB))
Write-Host "Mode:      $Mode"
Write-Host "Wizard:    License agreement required; Autostart task preselected."
