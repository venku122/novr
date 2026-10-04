$ErrorActionPreference = 'Stop'
$scriptPath = Join-Path $PSScriptRoot '../scripts/capture-head-dev.ps1'
$testRoot = Join-Path ([IO.Path]::GetTempPath()) ('novr-head-capture-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $testRoot | Out-Null
# Replace only the network boundary; no game tool is called.
function Invoke-RestMethod {
    param($Uri, $Method, $ContentType, $Body, $TimeoutSec)
    $tool = ($Body | ConvertFrom-Json).tool
    if ($tool -eq 'get_gaze_hud_state') { return [pscustomobject]@{ result = '{"aimSource":"Eye","aimValid":true,"uiDirection":{"x":0.1,"y":0,"z":0.99}}' } }
    if ($tool -ne 'get_vr_head_state') { throw 'Capture must invoke read-only tools only.' }
    return [pscustomobject]@{ result = $global:NovrHeadCaptureTestFixture }
}
try {
    $global:NovrHeadCaptureTestFixture = '{"cameraMode":"cockpit","seatReference":{"available":false},"gameCameraRoot":{"available":false}}'
    $menuOutput = Join-Path $testRoot 'menu'
    $refused = $false
    try { & $scriptPath -OutputDirectory $menuOutput -WaitForCockpitSeconds 1 -CaptureSeconds 1 } catch {
        if ($_.Exception.Message -notlike 'No cockpit evidence captured*') { throw }
        $refused = $true
    }
    $menuSession = Get-Content (Join-Path $menuOutput 'session.json') -Raw | ConvertFrom-Json
    if (-not $refused -or $menuSession.sampleCount -ne 0 -or (Test-Path (Join-Path $menuOutput 'head.jsonl'))) { throw 'Front-end default cockpit enum must not produce cockpit evidence.' }
    $startupOutput = Join-Path $testRoot 'startup-gaze'
    try { & $scriptPath -OutputDirectory $startupOutput -WaitForCockpitSeconds 1 -CaptureSeconds 1 -IncludeGazeHud } catch {
        if ($_.Exception.Message -notlike 'No cockpit evidence captured*') { throw }
    }
    $startup = Get-Content (Join-Path $startupOutput 'session.json') -Raw | ConvertFrom-Json
    if ($startup.startupGazeSamples -lt 1 -or $startup.gazeHudSamples -ne 0 -or $startup.sampleCount -ne 0 -or
        -not (Test-Path (Join-Path $startupOutput 'startup-gaze-hud.jsonl')) -or (Test-Path (Join-Path $startupOutput 'head.jsonl'))) {
        throw 'Startup gaze must be captured separately and never counted as cockpit evidence.'
    }
    $global:NovrHeadCaptureTestFixture = '{"cameraMode":"cockpit","seatReference":{"available":true},"gameCameraRoot":{"available":true},"rawHeadPosition":{"x":0.1,"y":1.2,"z":0.3}}'
    $cockpitOutput = Join-Path $testRoot 'cockpit'
    & $scriptPath -OutputDirectory $cockpitOutput -WaitForCockpitSeconds 1 -CaptureSeconds 1
    $cockpitSession = Get-Content (Join-Path $cockpitOutput 'session.json') -Raw | ConvertFrom-Json
    if ($cockpitSession.sampleCount -lt 1 -or $cockpitSession.sampleCount -gt 3) { throw 'Cockpit capture must produce bounded samples.' }
    $first = Get-Content (Join-Path $cockpitOutput 'head.jsonl') | Select-Object -First 1 | ConvertFrom-Json
    if ($first.rawHeadPosition.y -ne 1.2) { throw 'Head capture lost nested numeric telemetry.' }
    $combinedOutput = Join-Path $testRoot 'head-and-gaze'
    & $scriptPath -OutputDirectory $combinedOutput -WaitForCockpitSeconds 1 -CaptureSeconds 1 -IncludeGazeHud
    $combined = Get-Content (Join-Path $combinedOutput 'session.json') -Raw | ConvertFrom-Json
    $gaze = Get-Content (Join-Path $combinedOutput 'gaze-hud.jsonl') | Select-Object -First 1 | ConvertFrom-Json
    if (-not $combined.gazeHudRequested -or $combined.gazeHudSamples -ne $combined.sampleCount -or $gaze.aimSource -ne 'Eye' -or $gaze.uiDirection.z -ne .99) { throw 'Head/gaze capture association or nested values lost.' }
    Write-Output 'PASS: front-end default camera enum rejected, actual seat accepted, bounded nested head evidence preserved (mocked read-only bridge).'
} finally {
    Remove-Variable NovrHeadCaptureTestFixture -Scope Global -ErrorAction SilentlyContinue
    Remove-Item -LiteralPath $testRoot -Recurse -Force
}
