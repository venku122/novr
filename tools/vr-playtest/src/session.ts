import { randomUUID } from 'node:crypto';
import { mkdir, writeFile, rename, appendFile, readFile } from 'node:fs/promises';
import { join } from 'node:path';
import { classifyObservation, parseCommand } from './commands.js';
export type Artifact = { status: 'present'; path: string; sha256: string } | { status: 'missing' | 'error'; error: string };
export interface ObservationInput { timestamp?: string; aircraft?: string; text: string; artifacts: Record<string, Artifact>; }
export interface Observation extends ObservationInput { id: string; timestamp: string; category: string; speechIntent: ReturnType<typeof parseCommand>; }
export async function atomicJson(path: string, data: unknown): Promise<void> {
  const temporary = `${path}.${randomUUID()}.tmp`;
  await writeFile(temporary, JSON.stringify(data, null, 2) + '\n', { flag: 'wx' });
  await rename(temporary, path);
}
export class PlaytestSession {
  private queue: Promise<unknown> = Promise.resolve();
  private constructor(public readonly directory: string, private metadata: Record<string, unknown>) {}
  static async create(root: string, metadata: Record<string, unknown>): Promise<PlaytestSession> {
    const now = new Date().toISOString();
    const directory = join(root, `${now.replace(/[:.]/g, '-')}_steam-frame_${randomUUID().slice(0, 8)}`);
    await mkdir(directory, { recursive: true });
    for (const name of ['audio', 'logs', 'screenshots', 'telemetry', 'diffs']) await mkdir(join(directory, name));
    const session = new PlaytestSession(directory, { ...metadata, schemaVersion: 1, startedAt: now, observationCount: 0 });
    await writeFile(join(directory, 'observations.jsonl'), '', { flag: 'wx' });
    await atomicJson(join(directory, 'session.json'), session.metadata);
    return session;
  }
  recordRuntimeMetadata(snapshot: Record<string, unknown>): Promise<void> {
    const fields = ['timestamp', 'runtime', 'runtimeVersion', 'backend', 'gameVersion', 'gpu', 'leftProfile', 'rightProfile', 'renderScale', 'eyeWidth', 'eyeHeight', 'frameExtensionAvailable', 'frameExtensionEnabled'];
    const pending = this.queue.then(async () => {
      this.metadata.lastRuntimeSnapshot = Object.fromEntries(fields.filter(key => snapshot[key] !== undefined).map(key => [key, snapshot[key]]));
      await atomicJson(join(this.directory, 'session.json'), this.metadata);
    });
    this.queue = pending.catch(() => undefined);
    return pending;
  }
  addObservation(input: ObservationInput): Promise<Observation> {
    const pending = this.queue.then(async () => {
      const record: Observation = { ...input, id: randomUUID(), timestamp: input.timestamp ?? new Date().toISOString(), category: classifyObservation(input.text), speechIntent: parseCommand(input.text, 'transcript') };
      // Append is the durable source of truth. Count is recoverable if metadata replacement fails.
      await appendFile(join(this.directory, 'observations.jsonl'), JSON.stringify(record) + '\n');
      const contents = await readFile(join(this.directory, 'observations.jsonl'), 'utf8');
      this.metadata.observationCount = contents.trim().split('\n').filter(Boolean).length;
      await atomicJson(join(this.directory, 'session.json'), this.metadata);
      return record;
    });
    this.queue = pending.catch(() => undefined);
    return pending;
  }
}
