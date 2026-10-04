import { parseArgs, promisify } from 'node:util';
import { randomUUID } from 'node:crypto';
import { execFile } from 'node:child_process';
import { createInterface } from 'node:readline';
import { readFile } from 'node:fs/promises';
import { resolve } from 'node:path';
import { fileURLToPath } from 'node:url';
import { PlaytestSession } from './session.js';
import { CaptureService, watchRequests } from './service.js';
import { prepareCodexBundle } from './codex.js';
import { resolveEvidencePath } from './logs.js';
import { storeArtifact, type EvidenceRequest } from './capture.js';
const { values, positionals } = parseArgs({ allowPositionals: true, options: {
  game: { type: 'string' }, sessions: { type: 'string', default: 'playtest/sessions' }, device: { type: 'string' }, ffmpeg: { type: 'string', default: 'ffmpeg.exe' }, seconds: { type: 'string', default: '30' }, requests: { type: 'string' }, telemetry: { type: 'string' }, screenshot: { type: 'string' }, log: { type: 'string', default: 'BepInEx/LogOutput.log' }, text: { type: 'string' }, session: { type: 'string' }, intent: { type: 'string', default: 'investigate' }, repository: { type: 'string', default: '.' }, help: { type: 'boolean' },
} });
async function main(): Promise<void> {
  const command = positionals[0];
  if (values.help || !command) {
    console.log('NOVR external playtest helper (Node 22+)\nserve --game DIR --device NAME [--requests RELATIVE_DIR]\nnote --game DIR --text OBSERVATION [--telemetry RELATIVE_JSON --screenshot RELATIVE_PNG]\nrecord --game DIR --device NAME [--seconds 1..30]\nbundle --session DIR --intent investigate|implement --repository CHECKOUT\nNo microphone starts until start/record. Bundles prepare jobs only; no agent or deployment is launched.'); return;
  }
  if (command === 'bundle') {
    if (!values.session || !['investigate', 'implement'].includes(values.intent!)) throw new Error('bundle requires --session and investigate|implement intent');
    console.log(JSON.stringify(await prepareCodexBundle(values.session, values.intent as 'investigate' | 'implement', values.repository!), null, 2)); return;
  }
  if (!['serve', 'note', 'record'].includes(command)) throw new Error('Unknown command');
  const game = values.game ?? process.env.NUCLEAR_OPTION_GAME_DIR;
  if (!game) throw new Error('Provide --game or NUCLEAR_OPTION_GAME_DIR');
  const gameRoot = resolve(game), seconds = Number(values.seconds);
  if (!Number.isInteger(seconds) || seconds < 1 || seconds > 30) throw new Error('--seconds must be between 1 and 30');
  if (command !== 'note' && !values.device) throw new Error('Explicit --device microphone selection is required');
  const metadata: Record<string, unknown> = { novrCommit: process.env.NOVR_COMMIT ?? null, hostPlatform: process.platform, captureDevice: values.device ?? null, transcription: 'windows-system-speech-local', telemetryState: 'awaiting fresh snapshots' };
  if (process.platform === 'win32') {
    try {
      const environment = fileURLToPath(new URL('../../scripts/environment.ps1', import.meta.url));
      const { stdout } = await promisify(execFile)('powershell.exe', ['-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', environment, '-GameDirectory', gameRoot], { timeout: 15000, maxBuffer: 256 * 1024 });
      Object.assign(metadata, JSON.parse(stdout));
    } catch (error) { metadata.environmentError = String(error); }
  }
  try { metadata.deployedBuild = JSON.parse(await readFile(await resolveEvidencePath(gameRoot, 'BepInEx/NOVR/dev-deployments/current.json'), 'utf8')); } catch { metadata.deployedBuild = null; }
  const session = await PlaytestSession.create(resolve(values.sessions!), metadata);
  try { await storeArtifact(session, 'logs/novr-configuration.cfg', await readFile(await resolveEvidencePath(gameRoot, 'BepInEx/config/deltawing.novr.cfg'))); } catch (error) { await storeArtifact(session, 'logs/configuration-unavailable.txt', String(error)); }
  console.log(`SESSION ${session.directory}`);
  const service = new CaptureService(session, gameRoot, { device: values.device ?? '', ffmpeg: values.ffmpeg!, seconds });
  const evidence: EvidenceRequest = { log: values.log! };
  if (values.telemetry) evidence.telemetry = values.telemetry;
  if (values.screenshot) evidence.screenshot = values.screenshot;
  if (command === 'note') { if (!values.text) throw new Error('note requires --text'); await service.note(values.text, evidence); return; }
  const controller = new AbortController(); let activeId: string | undefined;
  const close = () => { controller.abort(); };
  process.once('SIGINT', close); process.once('SIGTERM', close);
  if (command === 'record') {
    activeId = randomUUID(); await service.start(activeId, evidence);
    console.log(`Recording for at most ${seconds}s. Ctrl+C releases capture.`);
    while (service.recording && !controller.signal.aborted) await new Promise(resolve => setTimeout(resolve, 100));
    await service.stop(activeId); return;
  }
  const watcher = values.requests ? watchRequests(service, gameRoot, values.requests, controller.signal, error => console.error(String(error))) : Promise.resolve();
  const input = createInterface({ input: process.stdin, output: process.stdout });
  controller.signal.addEventListener('abort', () => input.close(), { once: true });
  console.log('Ready; microphone OFF. Commands: start, stop, snapshot, note TEXT, quit. Requests accept capture only.');
  try {
    for await (const line of input) {
      const text = line.trim();
      try {
        if (text === 'start') { const id = randomUUID(); await service.start(id, evidence); activeId = id; console.log('Recording ON'); }
        else if (text === 'stop') { await service.stop(activeId); activeId = undefined; console.log('Recording OFF; observation saved'); }
        else if (text === 'snapshot') await service.note('Screenshot/telemetry snapshot', evidence);
        else if (text.startsWith('note ')) await service.note(text.slice(5), evidence);
        else if (text === 'quit') break;
        else if (text) console.log('Unknown command. Investigation/implementation/deployment require explicit separate commands.');
      } catch (error) { console.error(String(error)); }
    }
  } finally { close(); await service.stop(); await watcher; input.close(); }
}
main().catch(error => { console.error(String(error)); process.exitCode = 1; });
