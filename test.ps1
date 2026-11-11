param(
    [string]$Configuration = "Release",

    [ValidateSet("All", "Security", "Unit")]
    [string]$Category = "All"
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$sln = Join-Path $root "TrayMedia.sln"

$filter = $null
switch ($Category) {
    "Security" { $filter = "Category=Security" }
    "Unit" { $filter = "Category!=Security" }
}

Write-Host "Running tests ($Configuration, category=$Category)..."
if ($filter) {
    dotnet test $sln -c $Configuration --nologo --filter $filter
} else {
    dotnet test $sln -c $Configuration --nologo
}

if ($LASTEXITCODE -ne 0) {
    throw "Tests failed (exit $LASTEXITCODE)."
}

Write-Host "All tests passed."
