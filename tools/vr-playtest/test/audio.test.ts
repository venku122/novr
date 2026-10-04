import { test } from 'node:test';
import assert from 'node:assert/strict';
import { mkdtemp, rm, writeFile } from 'node:fs/promises';
import { join } from 'node:path';
import { tmpdir } from 'node:os';
import { spawn } from 'node:child_process';
import { buildRecordingArgs, startRecording } from '../src/audio.js';

test('microphone arguments are structured and duration is always bounded', () => {
  const args = buildRecordingArgs('Mic with spaces & characters', 'out file.wav', 30);
  assert.equal(args[args.indexOf('-i') + 1], 'audio=Mic with spaces & characters');
  assert.equal(args[args.indexOf('-t') + 1], '30');
  assert.throws(() => buildRecordingArgs('Mic', 'out.wav', 31));
  assert.throws(() => buildRecordingArgs('', 'out.wav', 10));
});

test('release gracefully stops the recorder; spawn failure is reported rather than losing the note', async () => {
  const root = await mkdtemp(join(tmpdir(), 'novr-audio-'));
  try {
    const output = join(root, 'voice.wav'), fixture = join(root, 'recorder.cjs');
    await writeFile(fixture, `const fs=require('fs');process.stdin.on('data',()=>{fs.writeFileSync(process.argv[2],Buffer.alloc(100));process.exit(0)});setInterval(()=>{},100);`);
    const recording = startRecording({ executable: 'unused', device: 'Mic', output, seconds: 1 }, () => spawn(process.execPath, [fixture, output], { stdio: ['pipe', 'ignore', 'pipe'] }));
    const result = await recording.stop();
    assert.equal(result.ok, true);
    const failed = startRecording({ executable: join(root, 'does-not-exist'), device: 'Mic', output: join(root, 'absent.wav'), seconds: 1 });
    assert.equal((await failed.completed).ok, false);
  } finally { await rm(root, { recursive: true, force: true }); }
});
