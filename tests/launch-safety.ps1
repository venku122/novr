$ErrorActionPreference = 'Stop'
$fixture = Join-Path ([IO.Path]::GetTempPath()) ([Guid]::NewGuid().ToString())
$game = Join-Path $fixture 'steamapps/common/Nuclear Option'
New-Item -ItemType Directory -Force "$game/NuclearOption_Data/Managed" | Out-Null
Set-Content "$game/NuclearOption.exe" 'fixture'
Set-Content "$fixture/steamapps/appmanifest_2168680.acf" '"installdir" "Nuclear Option"'
$launchState = [pscustomobject]@{ requested = $false; polls = 0; verified = $false }
function Start-Process { param($FilePath) $launchState.requested = $true }
function Start-Sleep { param($Seconds) }
function Get-Process {
    param($Name, $ErrorAction)
    if (-not $launchState.requested) { return }
    $launchState.polls++
    if ($launchState.polls -eq 1) {
        $isLocked = $false
        try {
            $stream = [IO.File]::Open("$game/BepInEx/NOVR/dev-deployments/deployment.lock", [IO.FileMode]::Open, [IO.FileAccess]::ReadWrite, [IO.FileShare]::None)
            $stream.Dispose()
        } catch [IO.IOException] { $isLocked = $true }
        if (-not $isLocked) { throw 'Launch released deployment lock before process detection.' }
        $launchState.verified = $true
        return
    }
    if ($launchState.polls -le 3) { return [pscustomobject]@{ ProcessName = 'NuclearOption'; Id = 1 } }
}
try {
    & "$PSScriptRoot/../scripts/launch-dev.ps1" -GameDirectory $game
    if (-not $launchState.verified) { throw 'Did not test asynchronous launch interval.' }
    Write-Output 'PASS: deployment lock held through asynchronous Steam launch and process detection (mocked process boundary; no game launched)'
} finally {
    Remove-Item $fixture -Recurse -Force
}
