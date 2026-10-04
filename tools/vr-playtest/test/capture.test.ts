import { test } from 'node:test';
import assert from 'node:assert/strict';
import { mkdtemp, writeFile, readFile, rm, mkdir, symlink } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { PlaytestSession } from '../src/session.js';
import { collectEvidence } from '../src/capture.js';
import { readLogTail, resolveEvidencePath } from '../src/logs.js';

test('bounded log tail preserves final lines without reading an entire large log', async () => {
  const root = await mkdtemp(join(tmpdir(), 'novr-tail-'));
  try {
    const path = join(root, 'log');
    await writeFile(path, 'old\n'.repeat(10000) + 'last one\nlast two\n');
    assert.equal(await readLogTail(path, 40, 2), 'last one\nlast two\n');
    await assert.rejects(resolveEvidencePath(root, '../secret'));
    await mkdir(join(root, 'linked-directory'));
    await symlink(join(root, 'linked-directory'), join(root, 'alias'), process.platform === 'win32' ? 'junction' : 'dir');
    await assert.rejects(resolveEvidencePath(root, 'alias'));
  } finally { await rm(root, { recursive: true, force: true }); }
});

test('captures fresh telemetry, on-demand screenshot and logs; reports stale data honestly', async () => {
  const root = await mkdtemp(join(tmpdir(), 'novr-evidence-'));
  try {
    const game = join(root, 'game'); await mkdir(game);
    await writeFile(join(game, 'telemetry.json'), JSON.stringify({ timestamp: new Date().toISOString(), aircraft: 'Revoker', runtime: 'SteamVR' }));
    await writeFile(join(game, 'LogOutput.log'), 'hello\nlast\n');
    await writeFile(join(game, 'screen.png'), Buffer.from('iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+aN1cAAAAASUVORK5CYII=', 'base64'));
    const session = await PlaytestSession.create(join(root, 'sessions'), {});
    const evidence = await collectEvidence(session, game, { telemetry: 'telemetry.json', screenshot: 'screen.png', log: 'LogOutput.log' });
    assert.equal(evidence.aircraft, 'Revoker');
    assert.equal(evidence.artifacts.telemetry?.status, 'present');
    assert.equal(evidence.artifacts.screenshot?.status, 'present');
    assert.equal(evidence.artifacts.logTail?.status, 'present');
    const log = evidence.artifacts.logTail;
    assert.equal(log?.status === 'present' && await readFile(join(session.directory, log.path), 'utf8'), 'hello\nlast\n');
    await writeFile(join(game, 'telemetry.json'), JSON.stringify({ timestamp: '2000-01-01T00:00:00Z', aircraft: 'old' }));
    const stale = await collectEvidence(session, game, { telemetry: 'telemetry.json', screenshot: 'missing.png' });
    assert.equal(stale.artifacts.telemetry?.status, 'missing');
    assert.equal(stale.aircraft, undefined);
    assert.equal(stale.artifacts.screenshot?.status, 'missing');
    const escape = await collectEvidence(session, game, { telemetry: '../secret' });
    assert.equal(escape.artifacts.telemetry?.status, 'error');
  } finally { await rm(root, { recursive: true, force: true }); }
});

test('an asynchronously written screenshot is not marked present until its PNG is complete', async () => {
  const root = await mkdtemp(join(tmpdir(), 'novr-png-'));
  try {
    const session = await PlaytestSession.create(join(root, 'sessions'), {});
    const bytes = Buffer.from('iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+aN1cAAAAASUVORK5CYII=', 'base64');
    await writeFile(join(root, 'partial.png'), bytes.subarray(0, 20));
    const pending = collectEvidence(session, root, { screenshot: 'partial.png' });
    await new Promise(resolve => setTimeout(resolve, 100));
    await writeFile(join(root, 'partial.png'), bytes);
    const artifact = (await pending).artifacts.screenshot;
    assert.equal(artifact?.status, 'present');
    if (artifact?.status === 'present') assert.deepEqual(await readFile(join(session.directory, artifact.path)), bytes);
  } finally { await rm(root, { recursive: true, force: true }); }
});
