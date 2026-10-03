param(
    [Parameter(Mandatory=$true)][string]$StageDirectory,
    [string]$GameDirectory = $env:NUCLEAR_OPTION_GAME_DIR
)
$ErrorActionPreference = 'Stop'
. "$PSScriptRoot/dev-common.ps1"
if (-not $GameDirectory) { throw 'Pass -GameDirectory or set NUCLEAR_OPTION_GAME_DIR.' }
Invoke-NovrDeploy $StageDirectory $GameDirectory | ConvertTo-Json -Depth 5
