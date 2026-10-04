param([Parameter(Mandatory=$true)][string]$GameDirectory)
$ErrorActionPreference = 'Stop'
$runtime = $null
try { $runtime = (Get-ItemProperty -LiteralPath 'HKLM:\SOFTWARE\Khronos\OpenXR\1' -Name ActiveRuntime -ErrorAction Stop).ActiveRuntime } catch {}
$steamDirectory = $null
try { $steamDirectory = (Get-ItemProperty -LiteralPath 'HKCU:\Software\Valve\Steam' -Name SteamPath -ErrorAction Stop).SteamPath } catch {}
$steamVrVersion = $null
if ($steamDirectory) {
    $server = Join-Path $steamDirectory 'steamapps\common\SteamVR\bin\win64\vrserver.exe'
    if (Test-Path -LiteralPath $server) { $steamVrVersion = (Get-Item -LiteralPath $server).VersionInfo.FileVersion }
}
$gameVersion = $null
$game = Join-Path $GameDirectory 'NuclearOption.exe'
if (Test-Path -LiteralPath $game) { $gameVersion = (Get-Item -LiteralPath $game).VersionInfo.ProductVersion }
$gpu = @(Get-CimInstance Win32_VideoController | ForEach-Object { $_.Name })
@{ detectedAt = [DateTime]::UtcNow.ToString('o'); configuredOpenXrRuntime = $runtime; steamVrBinaryVersion = $steamVrVersion; gameExecutableVersion = $gameVersion; gpu = $gpu; frameOsVersion = $null; xrBackend = $null; interactionProfiles = $null; note = 'Configured runtime and installed binary versions are not proof of the active headset session. Fresh telemetry supplies actual backend/profile state.' } | ConvertTo-Json -Depth 4 -Compress
