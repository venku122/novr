$ErrorActionPreference = 'Stop'
. "$PSScriptRoot/../scripts/dev-common.ps1"
function Assert($value, $message) { if (-not $value) { throw $message } }
function MustFail([scriptblock]$action, [string]$message) {
    $failed = $false
    try { & $action } catch { $failed = $true }
    Assert $failed $message
}
$fixture = Join-Path ([IO.Path]::GetTempPath()) ([Guid]::NewGuid().ToString())
try {
    $game = Join-Path $fixture 'game'
    $stage = Join-Path $fixture 'stage'
    New-Item -ItemType Directory -Force "$game/NuclearOption_Data/Managed", "$stage/game/BepInEx/plugins/NOVR", "$stage/game/BepInEx/patchers/NOVR", "$game/BepInEx/plugins/NOVR" | Out-Null
    Set-Content "$game/NuclearOption.exe" 'fixture'
    Set-Content "$stage/game/BepInEx/plugins/NOVR/NOVR.dll" 'new-plugin'
    Set-Content "$stage/game/BepInEx/patchers/NOVR/NOVR.Patcher.dll" 'new-patcher'
    Set-Content "$game/BepInEx/plugins/NOVR/NOVR.dll" 'old-plugin'
    Set-Content "$game/BepInEx/plugins/NOVR/unrelated.dll" 'preserve'
    New-Item -ItemType Directory -Force "$stage/game/BepInEx/plugins/NOVR.Gamepad" | Out-Null
    Set-Content "$stage/game/BepInEx/plugins/NOVR.Gamepad/NOVR.Gamepad.dll" 'optional-gamepad'
    MustFail { Resolve-NovrPayloadPath $game 'BepInEx/plugins/Other/Other.dll' } 'Unrelated plugin path refused'
    MustFail { Resolve-NovrPayloadPath $game 'BepInEx/patchers/NOVR.Gamepad/Other.dll' } 'Gamepad patcher path refused'
    New-NovrManifest $stage 'test-sha' 'test-diff' | Out-Null
    $original = (Get-FileHash "$game/BepInEx/plugins/NOVR/NOVR.dll").Hash

    # Only this test's command lookup is replaced, never production script arguments.
    function Get-Process { param($Name, $ErrorAction) [pscustomobject]@{ ProcessName = 'NuclearOption'; Id = 1 } }
    MustFail { Invoke-NovrDeploy $stage $game } 'Running game must refuse deployment'
    Assert ((Get-FileHash "$game/BepInEx/plugins/NOVR/NOVR.dll").Hash -eq $original) 'Running-game refusal must not write plugin'
    function Get-Process { param($Name, $ErrorAction) }

    New-Item -ItemType Junction -Path "$fixture/stage-link" -Target $game | Out-Null
    MustFail { & "$PSScriptRoot/../scripts/build-dev.ps1" -GameDirectory $game -StageDirectory "$fixture/stage-link/unsafe-build" } 'Staging junction must refuse build'
    Assert (-not (Test-Path "$game/unsafe-build")) 'Build cannot write through a staging junction into live game'
    # Avoid recursive deletion following the test junction during fixture cleanup.
    [IO.Directory]::Delete("$fixture/stage-link")
    New-Item -ItemType Junction -Path "$fixture/game-link" -Target $game | Out-Null
    Set-Content "$fixture/fail-dotnet.cmd" '@exit /b 1'
    MustFail { & "$PSScriptRoot/../scripts/build-dev.ps1" -GameDirectory "$fixture/game-link" -StageDirectory "$game/unsafe-inverse" -DotNet "$fixture/fail-dotnet.cmd" } 'Game alias must refuse staging within actual game'
    Assert (-not (Test-Path "$game/unsafe-inverse")) 'Game alias cannot bypass live overlap check'
    [IO.Directory]::Delete("$fixture/game-link")
    New-Item -ItemType Directory -Force "$fixture/outside" | Out-Null
    New-Item -ItemType Junction -Path "$game/BepInEx/NOVR" -Target "$fixture/outside" | Out-Null
    MustFail { Invoke-NovrDeploy $stage $game } 'Deployment metadata junction must refuse'
    Assert (@(Get-ChildItem "$fixture/outside" -Force).Count -eq 0) 'Refusal cannot write lock or metadata through junction'
    [IO.Directory]::Delete("$game/BepInEx/NOVR")

    Set-Content "$stage/game/BepInEx/plugins/NOVR/NOVR.dll" 'tampered'
    MustFail { Invoke-NovrDeploy $stage $game } 'Tampered build must refuse deployment'
    Assert ((Get-FileHash "$game/BepInEx/plugins/NOVR/NOVR.dll").Hash -eq $original) 'Tamper refusal must not write plugin'
    New-NovrManifest $stage 'test-sha' 'test-diff' | Out-Null
    $manifest = Get-Content "$stage/build.json" -Raw | ConvertFrom-Json
    $manifest.files[0].path = '../../outside.dll'
    $manifest | ConvertTo-Json -Depth 8 | Set-Content "$stage/build.json"
    MustFail { Invoke-NovrDeploy $stage $game } 'Path traversal must refuse deployment'
    New-NovrManifest $stage 'test-sha' 'test-diff' | Out-Null
    $receipt = Invoke-NovrDeploy $stage $game
    Assert ($receipt.status -eq 'deployed') 'Deployment receipt'
    Assert (Test-Path "$game/BepInEx/plugins/NOVR.Gamepad/NOVR.Gamepad.dll") 'Optional gamepad deployed'
    Assert ((Get-Content "$game/BepInEx/plugins/NOVR/NOVR.dll" -Raw).Trim() -eq 'tampered') 'Exact staged payload installed'
    Assert ((Get-Content "$game/BepInEx/plugins/NOVR/unrelated.dll" -Raw).Trim() -eq 'preserve') 'Extra plugin preserved'
    $firstDeployedHash = (Get-FileHash "$game/BepInEx/plugins/NOVR/NOVR.dll").Hash
    Set-Content "$stage/game/BepInEx/plugins/NOVR/NOVR.dll" 'next-plugin'
    New-NovrManifest $stage 'next-sha' 'next-diff' | Out-Null
    $script:failPendingDelete = $true
    function Remove-Item {
        param([string]$LiteralPath, [switch]$Force)
        if ($LiteralPath.EndsWith('pending.json') -and $script:failPendingDelete) {
            $script:failPendingDelete = $false
            throw 'Injected failure after receipt publication'
        }
        Microsoft.PowerShell.Management\Remove-Item -LiteralPath $LiteralPath -Force:$Force
    }
    MustFail { Invoke-NovrDeploy $stage $game } 'Late deployment failure must propagate'
    Microsoft.PowerShell.Management\Remove-Item -LiteralPath Function:\Remove-Item
    $recovered = Get-Content "$game/BepInEx/NOVR/dev-deployments/current.json" -Raw | ConvertFrom-Json
    Assert ($recovered.buildId -eq $receipt.buildId) 'Recovery restores prior deployment receipt'
    Assert ((Get-FileHash "$game/BepInEx/plugins/NOVR/NOVR.dll").Hash -eq $firstDeployedHash) 'Recovery restores prior binaries'
    function Get-Process { param($Name, $ErrorAction) [pscustomobject]@{ ProcessName = 'NuclearOption'; Id = 1 } }
    MustFail { Invoke-NovrRollback $game } 'Running game must refuse rollback'
    function Get-Process { param($Name, $ErrorAction) }
    Invoke-NovrRollback $game | Out-Null
    Assert ((Get-FileHash "$game/BepInEx/plugins/NOVR/NOVR.dll").Hash -eq $original) 'Rollback restores exact old plugin'
    Assert (-not (Test-Path "$game/BepInEx/patchers/NOVR/NOVR.Patcher.dll")) 'Rollback removes newly introduced files'
    Assert (-not (Test-Path "$game/BepInEx/plugins/NOVR.Gamepad/NOVR.Gamepad.dll")) 'Rollback removes new optional gamepad'
    Assert (Test-Path "$game/BepInEx/plugins/NOVR/unrelated.dll") 'Rollback preserves unrelated files'
    Write-Output 'PASS: stopped-game gates, payload integrity, path traversal, staging/metadata junction refusal, deployment, rollback and unrelated-file preservation'
} finally {
    if (Test-Path $fixture) { Remove-Item $fixture -Recurse -Force }
}
