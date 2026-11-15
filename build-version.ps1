# Single source of truth: <Version> in TrayMedia.App\TrayMedia.csproj
function Get-TrayMediaVersion {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ProjectPath
    )

    $raw = Get-Content -LiteralPath $ProjectPath -Raw
    if ($raw -notmatch '<Version>\s*([^<]+?)\s*</Version>') {
        throw "Version not found in $ProjectPath"
    }

    return $Matches[1].Trim()
}

function Get-TrayMediaVersionNumeric {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Version
    )

    $core = ($Version -split '-', 2)[0]
    $parts = @($core.Split('.') | Where-Object { $_ -ne '' })
    while ($parts.Count -lt 4) {
        $parts += '0'
    }

    return ($parts[0..3] -join '.')
}
