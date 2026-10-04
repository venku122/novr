param(
    [string]$OutputDirectory,
    [ValidateRange(1,65535)][int]$BridgePort = 3334,
    [ValidateRange(1,600)][int]$WaitForCockpitSeconds = 300,
    [ValidateRange(1,120)][int]$CaptureSeconds = 60,
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
Write-Output "Waiting for cockpit; read-only head evidence: $OutputDirectory"
try {
    while ([DateTime]::UtcNow -lt $deadline) {
        try {
            # The existing localhost bridge dispatches this read-only tool on Unity's main thread.
            $response = Invoke-RestMethod -Uri "http://localhost:$BridgePort/invoke" -Method Post -ContentType 'application/json' -Body $body -TimeoutSec 2
            $snapshot = $response.result | ConvertFrom-Json
            # CameraMode defaults to cockpit even in the front-end; require actual seat/root data.
            if ($snapshot.cameraMode -eq 'Cockpit' -and $snapshot.seatReference.available -and $snapshot.gameCameraRoot.available) {
                if (-not $captureDeadline) {
                    $captureDeadline = [DateTime]::UtcNow.AddSeconds($CaptureSeconds)
                    $deadline = $captureDeadline
                    Write-Output "Cockpit found; recording $CaptureSeconds seconds at bounded sampling interval."
                }
                Add-Content -LiteralPath (Join-Path $OutputDirectory 'head.jsonl') -Value ($snapshot | ConvertTo-Json -Depth 16 -Compress) -Encoding UTF8
                $count++
            }
        } catch { $lastError = $_.Exception.Message }
        Start-Sleep -Milliseconds $IntervalMilliseconds
    }
} finally {
    Write-NovrJson (Join-Path $OutputDirectory 'session.json') ([pscustomobject]@{
        schemaVersion=1; timestamp=[DateTime]::UtcNow.ToString('O'); kind='seated-head'; sampleCount=$count; bridgePort=$BridgePort;
        intervalMilliseconds=$IntervalMilliseconds; waitForCockpitSeconds=$WaitForCockpitSeconds; captureSeconds=$CaptureSeconds;
        lastError=$lastError; status=$(if ($count -gt 0) { 'captured' } else { 'no-cockpit-evidence' })
    })
}
if ($count -eq 0) { throw "No cockpit evidence captured. Last bridge error: $lastError" }
Write-Output "CAPTURED: $count read-only head snapshots in $OutputDirectory"
