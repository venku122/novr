param(
    [Parameter(Mandatory=$true)][string]$StageDirectory,
    [Parameter(Mandatory=$true)][string]$OutputZip
)
$ErrorActionPreference = 'Stop'
. "$PSScriptRoot/dev-common.ps1"
$manifest = Read-NovrManifest $StageDirectory
Assert-NovrSafePath $OutputZip
if (Test-Path -LiteralPath $OutputZip) { throw 'OutputZip must be new; existing packages are preserved.' }
$temporary = Join-Path ([IO.Path]::GetTempPath()) ('novr-package-' + [Guid]::NewGuid().ToString('N'))
try {
    New-Item -ItemType Directory $temporary | Out-Null
    foreach ($entry in $manifest.files) {
        $source = Resolve-NovrPayloadPath (Join-Path $StageDirectory 'game') $entry.path
        $target = Join-Path $temporary ('game/' + $entry.path)
        New-Item -ItemType Directory -Force (Split-Path $target -Parent) | Out-Null
        Copy-Item -LiteralPath $source -Destination $target
    }
    Copy-Item -LiteralPath (Join-Path $StageDirectory 'build.json') -Destination $temporary
    Copy-Item -LiteralPath (Join-Path $StageDirectory 'test-summary.json') -Destination $temporary
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot '../README.STEAM-FRAME.md') -Destination $temporary
    foreach ($notice in Get-ChildItem (Join-Path $PSScriptRoot '..') -File | Where-Object Name -Match '^(LICENSE|COPYING|NOTICE)') {
        Copy-Item -LiteralPath $notice.FullName -Destination $temporary
    }
    New-Item -ItemType Directory (Join-Path $temporary 'scripts') | Out-Null
    foreach ($script in @('dev-common.ps1','deploy-dev.ps1','rollback-dev.ps1','capture-head-dev.ps1')) {
        Copy-Item -LiteralPath (Join-Path $PSScriptRoot $script) -Destination (Join-Path $temporary 'scripts')
    }
    New-Item -ItemType Directory -Force (Split-Path ([IO.Path]::GetFullPath($OutputZip)) -Parent) | Out-Null
    Compress-Archive -Path (Join-Path $temporary '*') -DestinationPath $OutputZip -CompressionLevel Optimal
    Write-Output "PACKAGED: $OutputZip (build $($manifest.buildId), commit $($manifest.commit))"
} finally { Remove-Item -LiteralPath $temporary -Recurse -Force -ErrorAction SilentlyContinue }
