param(
    [ValidateSet("portable", "runtime")]
    [string]$Mode = "portable",

    [switch]$SkipTests
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$project = Join-Path $root "TrayMedia.App\TrayMedia.csproj"
$out = Join-Path $root "dist"
$testScript = Join-Path $root "test.ps1"

if (-not $SkipTests) {
    & $testScript -Configuration Release
    if ($LASTEXITCODE -ne 0) {
        throw "Build aborted: tests failed."
    }
}

if (Test-Path $out) {
    Remove-Item -Recurse -Force $out
}

if ($Mode -eq "portable") {
    # Single-file self-contained: no .NET install needed (~73 MB).
    dotnet publish $project -c Release -r win-x64 --self-contained true `
        -p:PublishSingleFile=true `
        -p:EnableCompressionInSingleFile=true `
        -p:IncludeNativeLibrariesForSelfExtract=true `
        -o $out
} else {
    # Framework-dependent: needs .NET 7 Desktop Runtime (~20 MB).
    dotnet publish $project -c Release -r win-x64 --self-contained false `
        -p:PublishSingleFile=true `
        -p:EnableCompressionInSingleFile=false `
        -o $out
}

if ($LASTEXITCODE -ne 0) {
    throw "Publish failed (exit $LASTEXITCODE)."
}

$exe = Get-Item (Join-Path $out "TrayMedia.exe")
$version = "1.0.0-beta.1"
$labeled = Join-Path $out "TrayMedia-$version.exe"
Copy-Item $exe.FullName $labeled -Force

Write-Host ""
Write-Host "Built: $($exe.FullName)"
Write-Host "Also:  $labeled"
Write-Host ("Size:  {0:N1} MB" -f ($exe.Length / 1MB))
Write-Host "Mode:  $Mode"
