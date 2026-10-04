Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Assert-NovrGameStopped {
    if (Get-Process -Name NuclearOption -ErrorAction SilentlyContinue) {
        throw 'NuclearOption.exe is running. Close the game; staging is preserved.'
    }
}

function Assert-NovrGameRoot([string]$GameDirectory) {
    Assert-NovrSafePath $GameDirectory
    if (-not (Test-Path "$GameDirectory/NuclearOption.exe" -PathType Leaf) -or
        -not (Test-Path "$GameDirectory/NuclearOption_Data/Managed" -PathType Container)) {
        throw 'GameDirectory must contain NuclearOption.exe and NuclearOption_Data/Managed.'
    }
}

function Assert-NovrSafePath([string]$Path) {
    $cursor = [IO.Path]::GetFullPath($Path)
    while ($cursor) {
        if (Test-Path -LiteralPath $cursor) {
            if ((Get-Item -LiteralPath $cursor -Force).Attributes -band [IO.FileAttributes]::ReparsePoint) {
                throw "Reparse point is not supported: $cursor"
            }
        }
        $parent = Split-Path $cursor -Parent
        if ($parent -eq $cursor) { break }
        $cursor = $parent
    }
}

function Resolve-NovrPayloadPath([string]$Root, [string]$RelativePath) {
    if ($RelativePath -notmatch '^(?:BepInEx/(?:(?:plugins|patchers)/NOVR|plugins/NOVR\.Gamepad)/[^:]+|NuclearOption_Data/Managed/Unity\.XR\.(?:OpenXR|Management)\.dll)$' -or
        $RelativePath -match '\\|(^|/)\.\.?(/|$)' -or [IO.Path]::IsPathRooted($RelativePath)) {
        throw "Unsafe NOVR payload path: $RelativePath"
    }
    $rootPath = [IO.Path]::GetFullPath($Root).TrimEnd('\', '/')
    $path = [IO.Path]::GetFullPath((Join-Path $rootPath $RelativePath))
    if (-not $path.StartsWith($rootPath + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Path escapes root: $RelativePath"
    }
    Assert-NovrSafePath $path
    return $path
}

function Write-NovrJson([string]$Path, $Value) {
    Assert-NovrSafePath $Path
    New-Item -ItemType Directory -Force (Split-Path $Path -Parent) | Out-Null
    $temporary = $Path + '.' + [Guid]::NewGuid().ToString('N') + '.tmp'
    $Value | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath $temporary -Encoding UTF8
    Move-Item -LiteralPath $temporary -Destination $Path -Force
}

function Assert-NovrManagedXrConsistency($Files) {
    foreach ($entry in $Files) {
        if ($entry.path -match '^NuclearOption_Data/Managed/(Unity\.XR\.(?:OpenXR|Management)\.dll)$') {
            $assembly = $Matches[1]
            $copyPath = "BepInEx/patchers/NOVR/CopyToGame/Data/Managed/$assembly"
            $copy = @($Files | Where-Object path -eq $copyPath)
            if ($copy.Count -ne 1 -or $copy[0].sha256 -ne $entry.sha256) {
                throw "Managed XR must match its staged CopyToGame payload: $assembly"
            }
        }
    }
}

function New-NovrManifest([string]$StageDirectory, [string]$Commit, [string]$DiffHash) {
    $root = [IO.Path]::GetFullPath((Join-Path $StageDirectory 'game')).TrimEnd('\', '/')
    $files = @()
    foreach ($file in Get-ChildItem -LiteralPath $root -File -Recurse) {
        $relative = $file.FullName.Substring($root.Length + 1).Replace('\', '/')
        $null = Resolve-NovrPayloadPath $root $relative
        $files += [pscustomobject]@{ path = $relative; sha256 = (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash.ToLowerInvariant() }
    }
    if (-not ($files | Where-Object path -eq 'BepInEx/plugins/NOVR/NOVR.dll') -or
        -not ($files | Where-Object path -eq 'BepInEx/patchers/NOVR/NOVR.Patcher.dll')) {
        throw 'Stage is missing NOVR.dll or NOVR.Patcher.dll.'
    }
    Assert-NovrManagedXrConsistency $files
    $manifest = [pscustomobject]@{
        schemaVersion = 1; status = 'succeeded'; buildId = [Guid]::NewGuid().ToString('N')
        commit = $Commit; sourceSnapshotSha256 = $DiffHash; createdAt = [DateTime]::UtcNow.ToString('O'); files = $files
    }
    Write-NovrJson (Join-Path $StageDirectory 'build.json') $manifest
    return $manifest
}

function Read-NovrManifest([string]$StageDirectory) {
    $manifest = Get-Content -LiteralPath (Join-Path $StageDirectory 'build.json') -Raw | ConvertFrom-Json
    if ($manifest.schemaVersion -ne 1 -or $manifest.status -ne 'succeeded' -or
        $manifest.buildId -notmatch '^[a-f0-9]{32}$' -or @($manifest.files).Count -eq 0) { throw 'Invalid or unsuccessful staged build.' }
    $seen = @{}
    foreach ($entry in $manifest.files) {
        if ($seen.ContainsKey($entry.path)) { throw 'Duplicate staged path.' }
        $seen[$entry.path] = $true
        $path = Resolve-NovrPayloadPath (Join-Path $StageDirectory 'game') $entry.path
        if ($entry.sha256 -notmatch '^[a-fA-F0-9]{64}$' -or
            (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash -ne $entry.sha256) { throw "Stage integrity check failed: $($entry.path)" }
    }
    if (-not $seen.ContainsKey('BepInEx/plugins/NOVR/NOVR.dll') -or
        -not $seen.ContainsKey('BepInEx/patchers/NOVR/NOVR.Patcher.dll')) { throw 'Stage missing required runtime files.' }
    Assert-NovrManagedXrConsistency $manifest.files
    return $manifest
}

function Restore-NovrBackup([string]$GameDirectory, [string]$BackupDirectory, $Backup) {
    # Validate the entire backup before the first live write.
    foreach ($entry in $Backup.files) {
        $null = Resolve-NovrPayloadPath $GameDirectory $entry.path
        if ($entry.existed) {
            $source = Resolve-NovrPayloadPath (Join-Path $BackupDirectory 'files') $entry.path
            if ((Get-FileHash -LiteralPath $source).Hash -ne $entry.sha256) { throw "Rollback integrity check failed: $($entry.path)" }
        }
    }
    foreach ($entry in $Backup.files) {
        Assert-NovrGameStopped
        $target = Resolve-NovrPayloadPath $GameDirectory $entry.path
        if ($entry.existed) {
            New-Item -ItemType Directory -Force (Split-Path $target -Parent) | Out-Null
            Copy-Item -LiteralPath (Resolve-NovrPayloadPath (Join-Path $BackupDirectory 'files') $entry.path) -Destination $target -Force
        } elseif (Test-Path -LiteralPath $target) {
            Remove-Item -LiteralPath $target -Force
        }
    }
}

function Open-NovrDeploymentLock([string]$GameDirectory) {
    $directory = Join-Path $GameDirectory 'BepInEx/NOVR/dev-deployments'
    Assert-NovrSafePath $directory
    New-Item -ItemType Directory -Force $directory | Out-Null
    return [IO.File]::Open((Join-Path $directory 'deployment.lock'), [IO.FileMode]::OpenOrCreate, [IO.FileAccess]::ReadWrite, [IO.FileShare]::None)
}

function Invoke-NovrDeploy([string]$StageDirectory, [string]$GameDirectory) {
    Assert-NovrGameStopped
    Assert-NovrGameRoot $GameDirectory
    $manifest = Read-NovrManifest $StageDirectory
    # Validate all target chains before creating any backup/installation files.
    foreach ($entry in $manifest.files) { $null = Resolve-NovrPayloadPath $GameDirectory $entry.path }
    $lock = Open-NovrDeploymentLock $GameDirectory
    try {
        Assert-NovrGameStopped
        $pendingPath = Join-Path $GameDirectory 'BepInEx/NOVR/dev-deployments/pending.json'
        if (Test-Path $pendingPath) { throw 'An interrupted deployment needs rollback first.' }
        $backupDirectory = Join-Path $GameDirectory ('BepInEx/NOVR/dev-deployments/' + [Guid]::NewGuid().ToString('N'))
        $receiptPath = Join-Path $GameDirectory 'BepInEx/NOVR/dev-deployments/current.json'
        $previousReceipt = if (Test-Path $receiptPath) { Get-Content $receiptPath -Raw | ConvertFrom-Json } else { $null }
        $backupFiles = @()
        foreach ($entry in $manifest.files) {
            $target = Resolve-NovrPayloadPath $GameDirectory $entry.path
            $existed = Test-Path -LiteralPath $target -PathType Leaf
            $hash = $null
            if ($existed) {
                $hash = (Get-FileHash -LiteralPath $target).Hash
                $saved = Resolve-NovrPayloadPath (Join-Path $backupDirectory 'files') $entry.path
                New-Item -ItemType Directory -Force (Split-Path $saved -Parent) | Out-Null
                Copy-Item -LiteralPath $target -Destination $saved
            }
            $backupFiles += [pscustomobject]@{ path = $entry.path; existed = $existed; sha256 = $hash }
        }
        $backup = [pscustomobject]@{ schemaVersion = 1; previousReceipt = $previousReceipt; files = $backupFiles }
        Write-NovrJson (Join-Path $backupDirectory 'backup.json') $backup
        Write-NovrJson $pendingPath ([pscustomobject]@{ backupDirectory = $backupDirectory; buildId = $manifest.buildId })
        try {
            foreach ($entry in $manifest.files) {
                Assert-NovrGameStopped
                $target = Resolve-NovrPayloadPath $GameDirectory $entry.path
                New-Item -ItemType Directory -Force (Split-Path $target -Parent) | Out-Null
                Copy-Item -LiteralPath (Resolve-NovrPayloadPath (Join-Path $StageDirectory 'game') $entry.path) -Destination $target -Force
                if ((Get-FileHash -LiteralPath $target).Hash -ne $entry.sha256) { throw 'Deployed hash mismatch.' }
            }
            $receipt = [pscustomobject]@{
                schemaVersion = 1; status = 'deployed'; buildId = $manifest.buildId; commit = $manifest.commit
                deployedAt = [DateTime]::UtcNow.ToString('O'); backupDirectory = $backupDirectory; files = $manifest.files
            }
            Write-NovrJson $receiptPath $receipt
            Remove-Item -LiteralPath $pendingPath
            return $receipt
        } catch {
            $failure = $_
            # Recovery is also forbidden while the game runs. Preserve backup location for later recovery.
            Write-Warning "Deployment failed. Rollback backup: $backupDirectory"
            Assert-NovrGameStopped
            Restore-NovrBackup $GameDirectory $backupDirectory $backup
            if ($null -ne $previousReceipt) { Write-NovrJson $receiptPath $previousReceipt }
            elseif (Test-Path $receiptPath) { Remove-Item -LiteralPath $receiptPath }
            Remove-Item -LiteralPath $pendingPath
            throw $failure
        }
    } finally { $lock.Dispose() }
}

function Invoke-NovrRollback([string]$GameDirectory) {
    Assert-NovrGameStopped
    Assert-NovrGameRoot $GameDirectory
    $lock = Open-NovrDeploymentLock $GameDirectory
    try {
        $receiptPath = Join-Path $GameDirectory 'BepInEx/NOVR/dev-deployments/current.json'
        $pendingPath = Join-Path $GameDirectory 'BepInEx/NOVR/dev-deployments/pending.json'
        $recoverPending = Test-Path -LiteralPath $pendingPath
        $readPath = if ($recoverPending) { $pendingPath } else { $receiptPath }
        $receipt = Get-Content -LiteralPath $readPath -Raw | ConvertFrom-Json
        $expectedParent = [IO.Path]::GetFullPath((Join-Path $GameDirectory 'BepInEx/NOVR/dev-deployments')).TrimEnd('\', '/')
        $backupPath = [IO.Path]::GetFullPath($receipt.backupDirectory)
        Assert-NovrSafePath $backupPath
        if ((Split-Path $backupPath -Parent) -ne $expectedParent -or (Split-Path $backupPath -Leaf) -notmatch '^[a-f0-9]{32}$') {
            throw 'Unsafe rollback backup location.'
        }
        $backup = Get-Content -LiteralPath (Join-Path $receipt.backupDirectory 'backup.json') -Raw | ConvertFrom-Json
        if ($backup.schemaVersion -ne 1 -or @($backup.files).Count -eq 0) { throw 'Invalid rollback backup.' }
        Restore-NovrBackup $GameDirectory $receipt.backupDirectory $backup
        if ($null -ne $backup.previousReceipt) { Write-NovrJson $receiptPath $backup.previousReceipt }
        elseif (Test-Path $receiptPath) { Remove-Item -LiteralPath $receiptPath }
        if ($recoverPending) { Remove-Item -LiteralPath $pendingPath }
        return [pscustomobject]@{ status = 'rolled-back'; replacedBuildId = $receipt.buildId; previousDeployment = $backup.previousReceipt }
    } finally { $lock.Dispose() }
}
