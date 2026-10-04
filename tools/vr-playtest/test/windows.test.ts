import { test } from 'node:test';
import assert from 'node:assert/strict';
import { spawn, execFile } from 'node:child_process';
import { promisify } from 'node:util';
import { mkdtemp, rm, readFile } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { fileURLToPath } from 'node:url';
import { startRecording } from '../src/audio.js';
import { transcribe } from '../src/transcription.js';
import { PlaytestSession } from '../src/session.js';
import { CaptureService } from '../src/service.js';
const enabled = process.platform === 'win32' && process.env.NOVR_TEST_WINDOWS_AUDIO === '1';
test('real Windows FFmpeg writes bounded WAV and local speech transcribes synthetic speech', { skip: !enabled }, async () => {
  const root = await mkdtemp(join(tmpdir(), 'novr-windows-audio-'));
  try {
    const output = join(root, 'tone.wav');
    const recorder = startRecording({ executable: 'ffmpeg.exe', device: 'synthetic only', output, seconds: 1 }, () => spawn('ffmpeg.exe', ['-hide_banner', '-loglevel', 'error', '-f', 'lavfi', '-i', 'sine=frequency=440:duration=1', '-ac', '1', '-ar', '16000', '-c:a', 'pcm_s16le', output], { windowsHide: true, stdio: ['pipe', 'ignore', 'pipe'] }));
    assert.equal((await recorder.completed).ok, true);
    assert.equal((await readFile(output)).subarray(0, 4).toString(), 'RIFF');
    const speech = join(root, 'synthetic-speech.wav');
    await promisify(execFile)('powershell.exe', ['-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', fileURLToPath(new URL('../../test/synthesize.ps1', import.meta.url)), '-OutputFile', speech]);
    const result = await transcribe(speech);
    assert.match(result.text, /head position/i);
    console.log(`Synthetic Windows recognition: ${result.text}`);
    const session = await PlaytestSession.create(join(root, 'sessions'), { test: 'synthetic, no microphone' });
    const service = new CaptureService(session, root, { device: 'synthetic only', ffmpeg: 'ffmpeg.exe', seconds: 10 }, {
      record: options => startRecording(options, () => spawn('ffmpeg.exe', ['-hide_banner', '-loglevel', 'error', '-i', speech, '-t', '10', '-ac', '1', '-ar', '16000', '-c:a', 'pcm_s16le', options.output], { windowsHide: true, stdio: ['pipe', 'ignore', 'pipe'] })),
      transcribe,
    });
    await service.start('abcdefab-1234-1234-1234-123456789abc', {});
    const deadline = Date.now() + 15000;
    while (service.recording && Date.now() < deadline) await new Promise(resolve => setTimeout(resolve, 30));
    await service.stop();
    const observation = JSON.parse((await readFile(join(session.directory, 'observations.jsonl'), 'utf8')).trim());
    assert.match(observation.text, /head position/i);
    assert.equal(observation.category, 'camera');
    assert.equal(observation.artifacts.audio.status, 'present');
    assert.equal(observation.artifacts.transcript.status, 'present');
    assert.equal(observation.artifacts.telemetry.status, 'missing');
  } finally { await rm(root, { recursive: true, force: true }); }
});
