import { test } from 'node:test';
import assert from 'node:assert/strict';
import { mkdtemp, rm, readFile } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { PlaytestSession, atomicJson } from '../src/session.js';
import { CaptureService, parseCaptureRequest } from '../src/service.js';

test('requests reject stale/replayed authority, malformed ids and non-capture commands', () => {
  const now = Date.now();
  const request = { version: 1, id: '12345678-1234-1234-1234-123456789abc', captureId: 'abcdefab-1234-1234-1234-123456789abc', timestamp: new Date(now).toISOString(), kind: 'start' };
  assert.equal(parseCaptureRequest(request, now)?.kind, 'start');
  assert.equal(parseCaptureRequest({ ...request, timestamp: '2000-01-01' }, now), undefined);
  assert.equal(parseCaptureRequest({ ...request, kind: 'deploy' }, now), undefined);
  assert.equal(parseCaptureRequest({ ...request, id: '../escape' }, now), undefined);
});

test('idle service never records; capture failure still creates durable evidence and release only finalizes once', async () => {
  const root = await mkdtemp(join(tmpdir(), 'novr-service-'));
  try {
    const session = await PlaytestSession.create(join(root, 'sessions'), {});
    let started = 0, stopped = 0;
    let complete!: (result: { ok: boolean; error: string }) => void;
    const service = new CaptureService(session, root, { device: 'test', ffmpeg: 'unused', seconds: 30 }, {
      record: () => { started++; const completed = new Promise<{ ok: boolean; error: string }>(resolve => { complete = resolve; }); return { completed, stop: () => { stopped++; complete({ ok: false, error: 'microphone disconnected' }); return completed; } }; },
      transcribe: async () => { throw new Error('must not transcribe failed recording'); },
    });
    assert.equal(started, 0);
    await service.start('12345678-1234-1234-1234-123456789abc', {});
    await service.start('12345678-1234-1234-1234-123456789abc', {});
    assert.equal(started, 1);
    await Promise.all([service.stop('12345678-1234-1234-1234-123456789abc'), service.stop('12345678-1234-1234-1234-123456789abc')]);
    assert.equal(stopped, 1);
    const lines = (await readFile(join(session.directory, 'observations.jsonl'), 'utf8')).trim().split('\n');
    assert.equal(lines.length, 1);
    assert.equal(JSON.parse(lines[0]!).artifacts.audio.status, 'error');
    assert.equal(service.recording, false);
  } finally { await rm(root, { recursive: true, force: true }); }
});

test('malformed or rejected requests cannot discard release, and request IDs cannot replay through aliases', async () => {
  const { mkdir, writeFile } = await import('node:fs/promises');
  const { watchRequests } = await import('../src/service.js');
  const root = await mkdtemp(join(tmpdir(), 'novr-spool-'));
  try {
    await mkdir(join(root, 'requests'));
    const session = await PlaytestSession.create(join(root, 'sessions'), {});
    let started = 0, complete!: (value: { ok: boolean; error: string }) => void;
    const errors: unknown[] = [];
    const service = new CaptureService(session, root, { device: 'test', ffmpeg: 'unused', seconds: 30 }, {
      record: () => { started++; const completed = new Promise<{ ok: boolean; error: string }>(resolve => { complete = resolve; }); return { completed, stop: () => { complete({ ok: false, error: 'fixture' }); return completed; } }; },
      transcribe: async () => ({ text: 'unused', method: 'fixture' }),
    });
    const controller = new AbortController();
    const watcher = watchRequests(service, root, 'requests', controller.signal, error => errors.push(error));
    const base = { version: 1, captureId: 'abcdefab-1234-1234-1234-123456789abc', evidence: {} };
    const request = (id: string, kind: string, offset: number) => ({ ...base, id, kind, timestamp: new Date(Date.now() + 500 + offset).toISOString() });
    const start = request('12345678-1234-1234-1234-123456789abc', 'start', 0);
    await writeFile(join(root, 'requests/0.json'), 'malformed');
    await atomicJson(join(root, 'requests/1.json'), start);
    await atomicJson(join(root, 'requests/2.json'), { ...request('12345678-1234-1234-1234-123456789abd', 'start', 1), captureId: 'abcdefab-1234-1234-1234-123456789abd' });
    await atomicJson(join(root, 'requests/3.json'), request('12345678-1234-1234-1234-123456789abe', 'stop', 2));
    try {
      const deadline = Date.now() + 3000;
      while (((await readFile(join(session.directory, 'observations.jsonl'), 'utf8')).trim() === '' || service.recording) && Date.now() < deadline) await new Promise(resolve => setTimeout(resolve, 30));
      assert.equal(started, 1);
      assert.equal(service.recording, false);
      assert.ok(errors.length >= 2);
      await atomicJson(join(root, 'requests/alias.json'), start);
      await new Promise(resolve => setTimeout(resolve, 200));
      assert.equal(started, 1);
    } finally { controller.abort(); await watcher; }
  } finally { await rm(root, { recursive: true, force: true }); }
});

test('an automatic finalization failure is surfaced even after capture state has cleared', async () => {
  const root = await mkdtemp(join(tmpdir(), 'novr-finalize-'));
  try {
    const session = await PlaytestSession.create(join(root, 'sessions'), {});
    const service = new CaptureService(session, root, { device: 'fixture', ffmpeg: 'unused', seconds: 1 }, {
      record: () => ({ completed: Promise.resolve({ ok: true }), stop: async () => ({ ok: true }) }),
      transcribe: async () => ({ text: '', method: 'fixture' }),
    });
    await service.start('abcdefab-1234-1234-1234-123456789abc', {});
    await new Promise(resolve => setTimeout(resolve, 30));
    assert.equal(service.recording, false);
    await assert.rejects(service.stop(), /ENOENT/);
    // A stop arriving before its delayed start prevents later microphone activation.
    await service.stop('abcdefab-1234-1234-1234-123456789abd');
    await service.start('abcdefab-1234-1234-1234-123456789abd', {});
    assert.equal(service.recording, false);
  } finally { await rm(root, { recursive: true, force: true }); }
});

test('release accepts another gesture while transcription runs; shutdown drains both observations', async () => {
  const { writeFile } = await import('node:fs/promises');
  const root = await mkdtemp(join(tmpdir(), 'novr-background-'));
  try {
    const session = await PlaytestSession.create(join(root, 'sessions'), {});
    const transcriptResolvers: Array<(value: { text: string; method: string }) => void> = [];
    let recordings = 0;
    const service = new CaptureService(session, root, { device: 'fixture', ffmpeg: 'unused', seconds: 1 }, {
      record: options => {
        recordings++;
        let complete!: (value: { ok: boolean }) => void;
        const completed = new Promise<{ ok: boolean }>(resolve => { complete = resolve; });
        return { completed, stop: async () => { await writeFile(options.output, Buffer.alloc(100)); complete({ ok: true }); return completed; } };
      },
      transcribe: () => new Promise(resolve => { transcriptResolvers.push(resolve); }),
    });
    await service.start('abcdefab-1234-1234-1234-123456789abc', {});
    await service.release();
    assert.equal(service.recording, false);
    await service.start('abcdefab-1234-1234-1234-123456789abd', {});
    await service.release();
    assert.equal(recordings, 2);
    const deadline = Date.now() + 3000;
    while (transcriptResolvers.length < 2 && Date.now() < deadline) await new Promise(resolve => setTimeout(resolve, 10));
    assert.equal(transcriptResolvers.length, 2);
    for (const resolve of transcriptResolvers) resolve({ text: 'captured', method: 'fixture' });
    await service.stop();
    assert.equal((await readFile(join(session.directory, 'observations.jsonl'), 'utf8')).trim().split('\n').length, 2);
  } finally { await rm(root, { recursive: true, force: true }); }
});
