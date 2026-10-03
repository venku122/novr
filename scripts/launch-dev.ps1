param(
    [string]$GameDirectory = $env:NUCLEAR_OPTION_GAME_DIR,
    [switch]$Attach
)
$ErrorActionPreference = 'Stop'
. "$PSScriptRoot/dev-common.ps1"
if (-not $GameDirectory) { throw 'Pass -GameDirectory or set NUCLEAR_OPTION_GAME_DIR.' }
Assert-NovrGameRoot $GameDirectory
$deadline = [DateTime]::UtcNow.AddSeconds(90)
if (Test-Path (Join-Path $GameDirectory 'BepInEx/NOVR/dev-deployments/pending.json')) {
    throw 'Recover the interrupted deployment with rollback-dev before launching.'
}
if (-not $Attach) {
    Assert-NovrGameStopped
    $lock = Open-NovrDeploymentLock $GameDirectory
    try {
        if (Test-Path (Join-Path $GameDirectory 'BepInEx/NOVR/dev-deployments/pending.json')) {
            throw 'Recover the interrupted deployment with rollback-dev before launching.'
        }
        # Steam URI targets the installed app. Refuse to launch a different library/Puppet tree.
        $manifestPath = Join-Path $GameDirectory '../../appmanifest_2168680.acf'
        if (-not (Test-Path $manifestPath)) { throw 'Steam app manifest not found beside this game library.' }
        $manifest = Get-Content $manifestPath -Raw
        if ($manifest -notmatch '"installdir"\s+"([^"]+)"' -or
            (Split-Path ([IO.Path]::GetFullPath($GameDirectory).TrimEnd('\', '/')) -Leaf) -ne $Matches[1]) {
            throw 'Steam app manifest does not identify this game folder.'
        }
        # NOVR initializes OpenXR in-process; no forced OpenVR command-line switch.
        Start-Process 'steam://rungameid/2168680'
        while (-not (Get-Process NuclearOption -ErrorAction SilentlyContinue)) {
            if ([DateTime]::UtcNow -gt $deadline) { throw 'Nuclear Option did not start within 90 seconds.' }
            Start-Sleep -Seconds 1
        }
    } finally { $lock.Dispose() }
}
while (-not (Get-Process NuclearOption -ErrorAction SilentlyContinue)) {
    if ([DateTime]::UtcNow -gt $deadline) { throw 'Nuclear Option did not start within 90 seconds.' }
    Start-Sleep -Seconds 1
}
Write-Output 'Attached. NOVR raw evidence is captured in-process when enabled. Watching BepInEx log; collecting evidence after game exits.'
$logPath = Join-Path $GameDirectory 'BepInEx/LogOutput.log'
$position = 0L
while (Get-Process NuclearOption -ErrorAction SilentlyContinue) {
    if (Test-Path $logPath) {
        $stream = [IO.File]::Open($logPath, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::ReadWrite)
        try {
            if ($position -gt $stream.Length) { $position = 0 }
            $null = $stream.Seek($position, [IO.SeekOrigin]::Begin)
            $reader = New-Object IO.StreamReader($stream)
            $text = $reader.ReadToEnd()
            $position = $stream.Position
            if ($text) { Write-Host $text -NoNewline }
        } finally { $stream.Dispose() }
    }
    Start-Sleep -Seconds 1
}
& "$PSScriptRoot/collect-baseline.ps1" -GameDirectory $GameDirectory
