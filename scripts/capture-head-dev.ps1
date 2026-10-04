param(
    [string]$OutputDirectory,
    [ValidateRange(1,65535)][int]$BridgePort = 3334,
    [ValidateRange(1,3600)][int]$WaitForCockpitSeconds = 300,
    [ValidateRange(1,120)][int]$CaptureSeconds = 60,
    [switch]$IncludeGazeHud,
    [ValidateRange(250,5000)][int]$IntervalMilliseconds = 500
)
$ErrorActionPreference = 'Stop'
. "$PSScriptRoot/dev-common.ps1"
if (-not $OutputDirectory) {
    $OutputDirectory = Join-Path $PSScriptRoot ('../playtest/sessions/' + [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssfffZ') + '-seated-head')
}
Assert-NovrSafePath $OutputDirectory
if (Test-Path -LiteralPath $OutputDirectory) { throw 'OutputDirectory must be new; existing evidence is preserved.' }
New-Item -ItemType Directory -Path $OutputDirectory | Out-Null
$deadline = [DateTime]::UtcNow.AddSeconds($WaitForCockpitSeconds)
$captureDeadline = $null
$count = 0
$lastError = ''
$body = @{ tool='get_vr_head_state'; args=@{} } | ConvertTo-Json -Compress
$gazeBody = @{ tool='get_gaze_hud_state'; args=@{} } | ConvertTo-Json -Compress
$gazeCount = 0
$startupGazeCount = 0
$nextStartupGaze = [DateTime]::MinValue
Write-Output "Waiting for cockpit; read-only head evidence: $OutputDirectory"
try {
    while ([DateTime]::UtcNow -lt $deadline) {
        try {
            # The existing localhost bridge dispatches this read-only tool on Unity's main thread.
            $response = Invoke-RestMethod -Uri "http://localhost:$BridgePort/invoke" -Method Post -ContentType 'application/json' -Body $body -TimeoutSec 2
            $snapshot = $response.result | ConvertFrom-Json
            # Startup probes are bounded and kept separate from cockpit acceptance evidence.
            if ($IncludeGazeHud -and -not $captureDeadline -and [DateTime]::UtcNow -ge $nextStartupGaze) {
                $nextStartupGaze = [DateTime]::UtcNow.AddSeconds(5)
                try {
                    $startupResponse = Invoke-RestMethod -Uri "http://localhost:$BridgePort/invoke" -Method Post -ContentType 'application/json' -Body $gazeBody -TimeoutSec 2
                    $startupGaze = $startupResponse.result | ConvertFrom-Json
                    Add-Content -LiteralPath (Join-Path $OutputDirectory 'startup-gaze-hud.jsonl') -Value ($startupGaze | ConvertTo-Json -Depth 16 -Compress) -Encoding UTF8
                    $startupGazeCount++
                } catch { $lastError = $_.Exception.Message }
            }
            # CameraMode defaults to cockpit even in the front-end; require actual seat/root data.
            if ($snapshot.cameraMode -eq 'Cockpit' -and $snapshot.seatReference.available -and $snapshot.gameCameraRoot.available) {
                if (-not $captureDeadline) {
                    $captureDeadline = [DateTime]::UtcNow.AddSeconds($CaptureSeconds)
                    $deadline = $captureDeadline
                    Write-Output "Cockpit found; recording $CaptureSeconds seconds at bounded sampling interval."
                }
                Add-Content -LiteralPath (Join-Path $OutputDirectory 'head.jsonl') -Value ($snapshot | ConvertTo-Json -Depth 16 -Compress) -Encoding UTF8
                $count++
                if ($IncludeGazeHud) {
                    $gazeResponse = Invoke-RestMethod -Uri "http://localhost:$BridgePort/invoke" -Method Post -ContentType 'application/json' -Body $gazeBody -TimeoutSec 2
                    $gaze = $gazeResponse.result | ConvertFrom-Json
                    Add-Content -LiteralPath (Join-Path $OutputDirectory 'gaze-hud.jsonl') -Value ($gaze | ConvertTo-Json -Depth 16 -Compress) -Encoding UTF8
                    $gazeCount++
                }
            }
        } catch { $lastError = $_.Exception.Message }
        Start-Sleep -Milliseconds $IntervalMilliseconds
    }
} finally {
    Write-NovrJson (Join-Path $OutputDirectory 'session.json') ([pscustomobject]@{
        schemaVersion=1; startupGazeSamples=$startupGazeCount; gazeHudRequested=[bool]$IncludeGazeHud; gazeHudSamples=$gazeCount; timestamp=[DateTime]::UtcNow.ToString('O'); kind='seated-head'; sampleCount=$count; bridgePort=$BridgePort;
        intervalMilliseconds=$IntervalMilliseconds; waitForCockpitSeconds=$WaitForCockpitSeconds; captureSeconds=$CaptureSeconds;
        lastError=$lastError; status=$(if ($count -eq 0) { 'no-cockpit-evidence' } elseif ($IncludeGazeHud -and $gazeCount -eq 0) { 'head-only:gaze-hud-unavailable' } elseif ($IncludeGazeHud -and $gazeCount -lt $count) { 'partial-gaze-hud' } else { 'captured' })
    })
}
if ($count -eq 0) { throw "No cockpit evidence captured. Last bridge error: $lastError" }
if ($IncludeGazeHud -and $gazeCount -eq 0) { throw "Head evidence captured, but gaze/HUD evidence unavailable. Last bridge error: $lastError" }
Write-Output "CAPTURED: $count read-only head snapshots in $OutputDirectory"
