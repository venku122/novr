$ErrorActionPreference = 'Stop'
$scriptPath = Join-Path $PSScriptRoot '../scripts/capture-head-dev.ps1'
$testRoot = Join-Path ([IO.Path]::GetTempPath()) ('novr-head-capture-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $testRoot | Out-Null
# Replace only the network boundary; no game tool is called.
function Invoke-RestMethod { param($Uri, $Method, $ContentType, $Body, $TimeoutSec) return [pscustomobject]@{ result = $script:fixture } }
try {
    $script:fixture = '{"cameraMode":"cockpit","seatReference":{"available":false},"gameCameraRoot":{"available":false}}'
    $menuOutput = Join-Path $testRoot 'menu'
    $refused = $false
    try { & $scriptPath -OutputDirectory $menuOutput -WaitForCockpitSeconds 1 -CaptureSeconds 1 } catch {
        if ($_.Exception.Message -notlike 'No cockpit evidence captured*') { throw }
        $refused = $true
    }
    $menuSession = Get-Content (Join-Path $menuOutput 'session.json') -Raw | ConvertFrom-Json
    if (-not $refused -or $menuSession.sampleCount -ne 0 -or (Test-Path (Join-Path $menuOutput 'head.jsonl'))) { throw 'Front-end default cockpit enum must not produce cockpit evidence.' }
    $script:fixture = '{"cameraMode":"cockpit","seatReference":{"available":true},"gameCameraRoot":{"available":true},"rawHeadPosition":{"x":0.1,"y":1.2,"z":0.3}}'
    $cockpitOutput = Join-Path $testRoot 'cockpit'
    & $scriptPath -OutputDirectory $cockpitOutput -WaitForCockpitSeconds 1 -CaptureSeconds 1
    $cockpitSession = Get-Content (Join-Path $cockpitOutput 'session.json') -Raw | ConvertFrom-Json
    if ($cockpitSession.sampleCount -lt 1 -or $cockpitSession.sampleCount -gt 3) { throw 'Cockpit capture must produce bounded samples.' }
    $first = Get-Content (Join-Path $cockpitOutput 'head.jsonl') | Select-Object -First 1 | ConvertFrom-Json
    if ($first.rawHeadPosition.y -ne 1.2) { throw 'Head capture lost nested numeric telemetry.' }
    Write-Output 'PASS: front-end default camera enum rejected, actual seat accepted, bounded nested head evidence preserved (mocked read-only bridge).'
} finally { Remove-Item -LiteralPath $testRoot -Recurse -Force }
