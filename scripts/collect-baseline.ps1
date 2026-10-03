param(
    [string]$GameDirectory = $env:NUCLEAR_OPTION_GAME_DIR,
    [string]$SessionDirectory
)
$ErrorActionPreference = 'Stop'
. "$PSScriptRoot/dev-common.ps1"
if (-not $GameDirectory) { throw 'Pass -GameDirectory or set NUCLEAR_OPTION_GAME_DIR.' }
Assert-NovrGameRoot $GameDirectory
if (-not $SessionDirectory) {
    $SessionDirectory = Join-Path $PSScriptRoot ('../playtest/sessions/' + [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssfffZ') + '-steam-frame-baseline')
}
if (Test-Path -LiteralPath $SessionDirectory) { throw 'SessionDirectory must be new; earlier evidence is preserved.' }
Assert-NovrSafePath $SessionDirectory
New-Item -ItemType Directory -Path $SessionDirectory | Out-Null
New-Item -ItemType Directory -Path (Join-Path $SessionDirectory 'logs') | Out-Null
$artifacts = @()
foreach ($relative in @('BepInEx/LogOutput.log', 'BepInEx/config/deltawing.novr.cfg',
    'BepInEx/plugins/NOVR/version.txt', 'BepInEx/NOVR/dev-deployments/current.json')) {
    $source = Join-Path $GameDirectory $relative
    if (Test-Path -LiteralPath $source -PathType Leaf) {
        $target = Join-Path $SessionDirectory ('logs/' + (Split-Path $source -Leaf))
        New-Item -ItemType Directory -Force (Split-Path $target -Parent) | Out-Null
        Copy-Item -LiteralPath $source -Destination $target
        $artifacts += [pscustomobject]@{ file = 'logs/' + (Split-Path $source -Leaf); sourceLastWriteAt = (Get-Item $source).LastWriteTimeUtc.ToString('O') }
    }
}
$rawRoot = Join-Path $GameDirectory 'BepInEx/NOVR/input-diagnostics'
$snapshots = @()
if (Test-Path $rawRoot) {
    $latest = Get-ChildItem -LiteralPath $rawRoot -Directory | Sort-Object Name -Descending | Select-Object -First 1
    if ($latest) {
        New-Item -ItemType Directory -Force (Join-Path $SessionDirectory 'telemetry/input') | Out-Null
        foreach ($file in Get-ChildItem -LiteralPath $latest.FullName -Filter '*.json' -File | Sort-Object Name) {
            Copy-Item -LiteralPath $file.FullName -Destination (Join-Path $SessionDirectory 'telemetry/input')
            $snapshots += $file
        }
    }
}
$lastSnapshot = if ($snapshots.Count -gt 0) { Get-Content -LiteralPath $snapshots[-1].FullName -Raw | ConvertFrom-Json } else { $null }
$xrRegistry = Get-ItemProperty 'HKLM:\SOFTWARE\Khronos\OpenXR\1' -ErrorAction SilentlyContinue
$steamRegistry = Get-ItemProperty 'HKCU:\Software\Valve\Steam' -ErrorAction SilentlyContinue
$steamPath = if ($steamRegistry) { $steamRegistry.SteamPath } else { $null }
$steamVrVersion = $null
if ($steamPath -and (Test-Path "$steamPath/logs/vrserver.txt")) {
    Get-Content "$steamPath/logs/vrserver.txt" -Tail 2000 | Set-Content (Join-Path $SessionDirectory 'logs/vrserver-tail.txt') -Encoding UTF8
    $steamVrVersion = Get-Content "$steamPath/logs/vrserver.txt" -TotalCount 15 | Select-String 'vrserver (\S+) startup' | ForEach-Object { $_.Matches[0].Groups[1].Value } | Select-Object -First 1
}
$metadata = [pscustomobject]@{
    schemaVersion = 1; capturedAt = [DateTime]::UtcNow.ToString('O'); kind = 'baseline-evidence-collection'
    note = 'Existing evidence collected on demand. Check snapshot timestamps; collection does not prove a new headset test occurred.'
    configuredOpenXrRuntimeManifest = if ($xrRegistry) { $xrRegistry.ActiveRuntime } else { $null }
    steamVrVersionFromLog = $steamVrVersion
    steamFrameOsVersion = 'unavailable'; microphoneDevice = 'not-yet-selected'
    snapshotCount = $snapshots.Count
    runtimeSnapshot = $lastSnapshot
    artifacts = $artifacts
}
Write-NovrJson (Join-Path $SessionDirectory 'session.json') $metadata
Write-Output "COLLECTED: $SessionDirectory ($($snapshots.Count) input snapshots)"
