param([Parameter(Mandatory=$true)][string]$AudioFile, [string]$Culture = 'en-US')
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Speech
$recognizerInfo = [System.Speech.Recognition.SpeechRecognitionEngine]::InstalledRecognizers() | Where-Object { $_.Culture.Name -eq $Culture } | Select-Object -First 1
if (-not $recognizerInfo) { throw "No local speech recognizer installed for $Culture" }
$engine = New-Object System.Speech.Recognition.SpeechRecognitionEngine($recognizerInfo)
try {
    $engine.LoadGrammar((New-Object System.Speech.Recognition.DictationGrammar))
    $engine.SetInputToWaveFile((Resolve-Path -LiteralPath $AudioFile).Path)
    $results = @()
    while ($results.Count -lt 100) {
        try { $result = $engine.Recognize([TimeSpan]::FromSeconds(35)) }
        catch {
            # File input is detached at EOF by some desktop recognizer versions.
            if ($results.Count -gt 0 -and $_.Exception.InnerException -is [System.InvalidOperationException] -and $engine.AudioState -eq [System.Speech.Recognition.AudioState]::Stopped) { break }
            throw
        }
        if ($null -eq $result) { break }
        $results += @{ text = $result.Text; confidence = $result.Confidence }
    }
    $data = @{ text = (($results | ForEach-Object { $_.text }) -join ' '); segments = $results; method = 'windows-system-speech'; culture = $Culture }
    [Console]::OutputEncoding = New-Object System.Text.UTF8Encoding($false)
    $data | ConvertTo-Json -Depth 4 -Compress
} finally { $engine.Dispose() }
