param(
    [string]$GameDirectory = $env:NUCLEAR_OPTION_GAME_DIR,
    [string]$StageDirectory,
    [string]$DotNet = 'dotnet',
    [string]$NodeExecutable = $env:NOVR_NODE_EXECUTABLE
)
$ErrorActionPreference = 'Stop'
. "$PSScriptRoot/dev-common.ps1"
$repo = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
function Invoke-SourceGit([string[]]$Arguments) {
    if (Get-Command git -ErrorAction SilentlyContinue) {
        & git -c "safe.directory=$repo" @Arguments
    } elseif ($repo -match '^\\\\wsl(?:\.localhost|\$)\\([^\\]+)\\(.+)$') {
        $distribution = $Matches[1]
        $linuxRepo = '/' + $Matches[2].Replace('\', '/')
        & wsl.exe -d $distribution --cd $linuxRepo -- git @Arguments
    } else { throw 'Git is required for build provenance. Install Git or use a WSL source checkout.' }
    if ($LASTEXITCODE -ne 0) { throw 'Git provenance command failed.' }
}
if (-not $GameDirectory) { throw 'Pass -GameDirectory or set NUCLEAR_OPTION_GAME_DIR (read-only references).' }
Assert-NovrGameRoot $GameDirectory
if (-not $StageDirectory) {
    $StageDirectory = Join-Path $repo ('staging/' + [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssfffZ') + '-' + [Guid]::NewGuid().ToString('N').Substring(0, 8))
}
$StageDirectory = [IO.Path]::GetFullPath($StageDirectory)
Assert-NovrSafePath $StageDirectory
if (Test-Path -LiteralPath $StageDirectory) { throw 'StageDirectory must be new; existing builds are preserved.' }
# A stage must never overlap the live installation, including source/target ancestors.
$gamePath = [IO.Path]::GetFullPath($GameDirectory).TrimEnd('\', '/') + [IO.Path]::DirectorySeparatorChar
$stagePath = $StageDirectory.TrimEnd('\', '/') + [IO.Path]::DirectorySeparatorChar
if ($stagePath.StartsWith($gamePath, [StringComparison]::OrdinalIgnoreCase) -or
    $gamePath.StartsWith($stagePath, [StringComparison]::OrdinalIgnoreCase)) { throw 'Staging and game paths must not overlap.' }
New-Item -ItemType Directory -Path $StageDirectory | Out-Null
Push-Location $repo
try {
    $commit = Invoke-SourceGit @('rev-parse', 'HEAD')
    Invoke-SourceGit @('diff', 'HEAD', '--binary') | Set-Content (Join-Path $StageDirectory 'source.diff') -Encoding UTF8
    $source = @()
    $sourceNames = Invoke-SourceGit @('ls-files', '--cached', '--others', '--exclude-standard')
    foreach ($name in $sourceNames) {
        $path = Join-Path $repo $name
        if (Test-Path -LiteralPath $path -PathType Leaf) {
            $source += [pscustomobject]@{ path = $name; sha256 = (Get-FileHash -LiteralPath $path).Hash.ToLowerInvariant() }
        }
    }
    Write-NovrJson (Join-Path $StageDirectory 'source-files.json') $source
    $sourceHash = (Get-FileHash (Join-Path $StageDirectory 'source-files.json')).Hash.ToLowerInvariant()
    $common = @('-c', 'Release', '-p:NovrAutoDeploy=false', "-p:NuclearOptionGameDir=$GameDirectory",
        "-p:BuildOutputDir=$StageDirectory/build-output", "-p:GameLayoutOutputDir=$StageDirectory/game",
        "-p:NovrIntermediateRoot=$StageDirectory/obj", '--nologo')
    # Build only runtime projects. Installer packaging and obsolete native XInput are not needed for iteration.
    foreach ($project in @('NOVR/NOVR.csproj', 'NOVR.Patcher/NOVR.Patcher.csproj', 'NOVR.McpBridge/NOVR.McpBridge.csproj', 'NOVR.Gamepad/NOVR.Gamepad.csproj')) {
        & $DotNet build $project @common 2>&1 | Tee-Object -FilePath (Join-Path $StageDirectory ((Split-Path $project -Leaf) + '.build.txt'))
        if ($LASTEXITCODE -ne 0) { throw "Build failed: $project. Incomplete stage is not deployable." }
    }
    & $DotNet run --project tests/Diagnostics/Diagnostics.csproj -c Release '-p:NovrAutoDeploy=false' "-p:NovrIntermediateRoot=$StageDirectory/test-obj"
    if ($LASTEXITCODE -ne 0) { throw 'Diagnostic policy tests failed.' }
    & $DotNet run --project tests/Gamepad/Gamepad.csproj -c Release '-p:NovrAutoDeploy=false' "-p:NovrIntermediateRoot=$StageDirectory/gamepad-test-obj"
    if ($LASTEXITCODE -ne 0) { throw 'Gamepad routing/lifecycle tests failed.' }
    & "$repo/tests/deployment-safety.ps1"
    & "$repo/tests/launch-safety.ps1"
    & "$repo/tests/head-capture-safety.ps1"
    $playtestTestStatus = 'skipped: optional Windows Node not available'
    if (-not $NodeExecutable) {
        $nodeCommand = Get-Command node.exe -ErrorAction SilentlyContinue
        if ($nodeCommand) { $NodeExecutable = $nodeCommand.Source }
    }
    if ($NodeExecutable) {
        $compiler = Join-Path $repo 'tools/vr-playtest/node_modules/typescript/bin/tsc'
        if (-not (Test-Path -LiteralPath $compiler)) { throw 'Run npm ci --ignore-scripts in tools/vr-playtest before building its optional tests.' }
        & $NodeExecutable $compiler -p (Join-Path $repo 'tools/vr-playtest/tsconfig.json')
        if ($LASTEXITCODE -ne 0) { throw 'Playtest TypeScript compilation failed.' }
        & $NodeExecutable --test 'tools/vr-playtest/dist/test/*.test.js' 2>&1 | Tee-Object -FilePath (Join-Path $StageDirectory 'playtest-tests.txt')
        if ($LASTEXITCODE -ne 0) { throw 'Playtest tests failed; stage is not deployable.' }
        $playtestTestStatus = 'passed; Windows synthetic audio fixture runs only when NOVR_TEST_WINDOWS_AUDIO=1'
    } else { Write-Warning $playtestTestStatus }
    Write-NovrJson (Join-Path $StageDirectory 'test-summary.json') ([pscustomobject]@{ runtimePolicyAndGesture = 'passed'; gamepadPolicyAndLifecycle = 'passed'; deploymentSafety = 'passed'; launchSafety = 'passed (mocked)'; headCaptureSafety = 'passed (mocked read-only bridge)'; playtest = $playtestTestStatus; hardware = 'not-tested' })
    $pluginSource = Get-Content (Join-Path $repo 'NOVR/NOVRPlugin.cs') -Raw
    if ($pluginSource -notmatch '"NOVR",\s*"([^"]+)"') { throw 'Cannot read NOVR plugin version.' }
    Set-Content (Join-Path $StageDirectory 'game/BepInEx/plugins/NOVR/version.txt') ($Matches[1] + '-dev+' + $commit.Trim().Substring(0, 12) + '-diagnostics') -Encoding ASCII
    $manifest = New-NovrManifest $StageDirectory $commit.Trim() $sourceHash
    Write-Output "READY: $StageDirectory (build $($manifest.buildId), commit $($manifest.commit))"
} finally { Pop-Location }
