import { readFile, readdir } from 'node:fs/promises';
import { join } from 'node:path';
import { PlaytestSession, type Artifact } from './session.js';
import { collectEvidence, storeArtifact, type EvidenceRequest } from './capture.js';
import { startRecording, type RecordingResult, type RecordingOptions } from './audio.js';
import { transcribe, type Transcript } from './transcription.js';
import { resolveEvidencePath, readBoundedFile } from './logs.js';
const uuid = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;
export interface CaptureRequest { version: 1; id: string; captureId: string; timestamp: string; kind: 'start' | 'stop' | 'snapshot'; evidence: EvidenceRequest; }
export function parseCaptureRequest(value: unknown, startedAt: number, now = Date.now()): CaptureRequest | undefined {
  if (!value || typeof value !== 'object') return undefined;
  const r = value as Record<string, unknown>;
  const time = typeof r.timestamp === 'string' ? Date.parse(r.timestamp) : NaN;
  if (r.version !== 1 || typeof r.id !== 'string' || !uuid.test(r.id) || typeof r.captureId !== 'string' || !uuid.test(r.captureId) || !Number.isFinite(time) || time < startedAt || now - time > 15000 || time - now > 2000 || !['start', 'stop', 'snapshot'].includes(String(r.kind))) return undefined;
  const evidence: EvidenceRequest = {};
  if (r.evidence !== undefined && r.evidence !== null) {
    if (!r.evidence || typeof r.evidence !== 'object') return undefined;
    for (const key of ['telemetry', 'screenshot', 'log'] as const) {
      const path = (r.evidence as Record<string, unknown>)[key];
      if (path !== undefined) { if (typeof path !== 'string' || path.length > 1024) return undefined; evidence[key] = path; }
    }
  }
  return { version: 1, id: r.id, captureId: r.captureId, timestamp: String(r.timestamp), kind: r.kind as CaptureRequest['kind'], evidence };
}
interface Dependencies { record: (options: RecordingOptions) => ReturnType<typeof startRecording>; transcribe: (audio: string) => Promise<Transcript>; }
interface Active { id: string; timestamp: string; output: string; recording: ReturnType<typeof startRecording>; evidence: ReturnType<typeof collectEvidence>; finalized?: Promise<void>; stopping?: Promise<void>; }
export class CaptureService {
  private active: Active | undefined;
  private failures: Error[] = [];
  private closedCaptures = new Set<string>();
  private pending = new Set<Promise<void>>();
  constructor(public readonly session: PlaytestSession, private gameRoot: string, private options: { device: string; ffmpeg: string; seconds: number }, private dependencies: Dependencies = { record: startRecording, transcribe }) {}
  get recording(): boolean { return !!this.active; }
  async start(id: string, request: EvidenceRequest): Promise<void> {
    if (!uuid.test(id)) throw new Error('Invalid capture ID');
    if (this.closedCaptures.has(id)) return; // A delayed start must not reopen a released capture.
    if (this.pending.size >= 8) throw new Error('Transcription backlog is full; wait for observations to finish');
    if (this.closedCaptures.size >= 5000) throw new Error('Capture limit reached; start a new session');
    if (this.active) { if (this.active.id === id) return; throw new Error('A capture is already active'); }
    const output = join(this.session.directory, 'audio', `${id}.wav`);
    const active: Active = { id, timestamp: new Date().toISOString(), output, recording: this.dependencies.record({ executable: this.options.ffmpeg, device: this.options.device, output, seconds: this.options.seconds }), evidence: collectEvidence(this.session, this.gameRoot, request) };
    this.active = active;
    void active.recording.completed.then(result => { void this.finish(active, result); });
  }
  async release(id?: string): Promise<void> {
    if (id) this.closedCaptures.add(id);
    const active = this.active;
    if (active && (!id || active.id === id)) {
      active.stopping ??= active.recording.stop().then(result => { void this.finish(active, result); });
      await active.stopping;
    }
  }
  async stop(id?: string): Promise<void> {
    await this.release(id);
    await Promise.allSettled([...this.pending]);
    if (this.failures.length) throw this.failures.shift();
  }
  private finish(active: Active, result: RecordingResult): Promise<void> {
    if (active.finalized) return active.finalized;
    this.closedCaptures.add(active.id);
    if (this.active === active) this.active = undefined;
    active.finalized = (async () => {
      const evidence = await active.evidence;
      let text = '[Audio capture failed]', audio: Artifact, transcript: Artifact;
      if (result.ok) {
        // The original WAV already resides in the session; hash it through a separate durable artifact.
        const bytes = await readFile(active.output);
        audio = await storeArtifact(this.session, `audio/${active.id}-recording.wav`, bytes);
        try {
          const value = await this.dependencies.transcribe(active.output);
          text = value.text || '[No speech recognized; original audio retained]';
          transcript = await storeArtifact(this.session, `audio/${active.id}-transcript.json`, JSON.stringify(value, null, 2));
        } catch (error) { text = '[Transcription failed; original audio retained]'; transcript = { status: 'error', error: String(error) }; }
      } else { audio = { status: 'error', error: result.error ?? 'Recording failed' }; transcript = { status: 'missing', error: 'No successful recording to transcribe' }; }
      await this.session.addObservation({ timestamp: active.timestamp, ...(evidence.aircraft ? { aircraft: evidence.aircraft } : {}), text, artifacts: { ...evidence.artifacts, audio, transcript } });
    })();
    this.pending.add(active.finalized);
    void active.finalized.then(() => { this.pending.delete(active.finalized!); }, error => {
      this.pending.delete(active.finalized!);
      this.failures.push(error instanceof Error ? error : new Error(String(error)));
    });
    return active.finalized;
  }
  async note(text: string, evidence: EvidenceRequest = {}): Promise<void> {
    const captured = await collectEvidence(this.session, this.gameRoot, evidence);
    await this.session.addObservation({ text, ...(captured.aircraft ? { aircraft: captured.aircraft } : {}), artifacts: captured.artifacts });
  }
}
export async function watchRequests(service: CaptureService, gameRoot: string, relativeDirectory: string, signal: AbortSignal, onError: (error: unknown) => void): Promise<void> {
  const startedAt = Date.now(), seen = new Set<string>(), requestIds = new Set<string>();
  let lastTimestamp = startedAt;
  while (!signal.aborted) {
    try {
      const directory = await resolveEvidencePath(gameRoot, relativeDirectory);
      const files = (await readdir(directory)).filter(name => name.endsWith('.json') && !seen.has(name)).sort().slice(0, 250);
      const requests: CaptureRequest[] = [];
      for (const name of files) {
        // A bad file cannot discard later release requests in the same batch.
        seen.add(name);
        try {
          const file = await resolveEvidencePath(gameRoot, join(relativeDirectory, name));
          const request = parseCaptureRequest(JSON.parse((await readBoundedFile(file, 16384)).toString('utf8')), startedAt);
          if (request && !requestIds.has(request.id)) { requestIds.add(request.id); requests.push(request); }
        } catch (error) { onError(error); }
      }
      requests.sort((a, b) => Date.parse(a.timestamp) - Date.parse(b.timestamp));
      for (const request of requests) {
        try {
          const timestamp = Date.parse(request.timestamp);
          // Late release still closes its capture; late starts cannot resurrect past gestures.
          if (timestamp < lastTimestamp && request.kind !== 'stop') { onError(new Error('Late capture request ignored')); continue; }
          lastTimestamp = Math.max(lastTimestamp, timestamp);
          if (request.kind === 'start') await service.start(request.captureId, request.evidence);
          else if (request.kind === 'stop') await service.release(request.captureId);
          else await service.note('Screenshot/telemetry snapshot', request.evidence);
        } catch (error) { onError(error); }
      }
      if (seen.size > 5000) throw new Error('Request limit reached; archive the request spool and start a new run');
    } catch (error) {
      if ((error as NodeJS.ErrnoException).code !== 'ENOENT') onError(error);
      if (seen.size > 5000) break;
    }
    await new Promise<void>(resolve => { const timer = setTimeout(done, 100); function done() { clearTimeout(timer); signal.removeEventListener('abort', done); resolve(); } signal.addEventListener('abort', done, { once: true }); });
  }
  await service.stop();
}
