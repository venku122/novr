import { randomUUID, createHash } from 'node:crypto';
import { writeFile } from 'node:fs/promises';
import { join } from 'node:path';
import { PlaytestSession, type Artifact } from './session.js';
import { readLogTail, resolveEvidencePath, readBoundedFile } from './logs.js';
import { parseFreshTelemetry } from './telemetry.js';
export function isCompletePng(bytes: Buffer): boolean {
  if (bytes.length < 8 || !bytes.subarray(0, 8).equals(Buffer.from([137, 80, 78, 71, 13, 10, 26, 10]))) return false;
  let offset = 8, chunks = 0, hasData = false;
  while (offset + 12 <= bytes.length) {
    const length = bytes.readUInt32BE(offset), type = bytes.toString('ascii', offset + 4, offset + 8);
    if (length > bytes.length - offset - 12) return false;
    if (chunks++ === 0 && (type !== 'IHDR' || length !== 13)) return false;
    if (type === 'IDAT') hasData = true;
    offset += length + 12;
    if (type === 'IEND') return length === 0 && hasData && offset === bytes.length;
  }
  return false;
}
export interface EvidenceRequest { telemetry?: string; screenshot?: string; log?: string; }
export async function storeArtifact(session: PlaytestSession, relative: string, bytes: Buffer | string): Promise<Artifact> {
  const data = Buffer.isBuffer(bytes) ? bytes : Buffer.from(bytes);
  await writeFile(join(session.directory, relative), data, { flag: 'wx' });
  return { status: 'present', path: relative.replace(/\\/g, '/'), sha256: createHash('sha256').update(data).digest('hex') };
}
function failure(error: unknown): Artifact {
  return { status: (error as NodeJS.ErrnoException).code === 'ENOENT' ? 'missing' : 'error', error: error instanceof Error ? error.message : String(error) };
}
export async function collectEvidence(session: PlaytestSession, gameRoot: string, request: EvidenceRequest): Promise<{ aircraft?: string; artifacts: Record<string, Artifact> }> {
  const id = randomUUID(), artifacts: Record<string, Artifact> = {}; let aircraft: string | undefined;
  for (const [kind, path] of Object.entries(request)) {
    if (!path) continue;
    try {
      // Unity screenshot writing is asynchronous; wait briefly outside the game thread.
      let source: string;
      const deadline = Date.now() + (kind === 'screenshot' ? 1500 : 0);
      while (true) {
        try { source = await resolveEvidencePath(gameRoot, path); break; }
        catch (error) {
          if ((error as NodeJS.ErrnoException).code !== 'ENOENT' || Date.now() >= deadline) throw error;
          await new Promise(resolve => setTimeout(resolve, 50));
        }
      }
      if (kind === 'log') {
        artifacts.logTail = await storeArtifact(session, `logs/${id}.log`, await readLogTail(source)); continue;
      }
      let bytes = await readBoundedFile(source, 16 * 1024 * 1024);
      while (kind === 'screenshot' && !isCompletePng(bytes)) {
        if (Date.now() >= deadline) throw new Error('Screenshot was not a complete PNG within the capture deadline');
        await new Promise(resolve => setTimeout(resolve, 50));
        bytes = await readBoundedFile(source, 16 * 1024 * 1024);
      }
      if (kind === 'telemetry') {
        const data = parseFreshTelemetry(bytes.toString('utf8'));
        if (!data) { artifacts.telemetry = { status: 'missing', error: 'Telemetry is stale or lacks a capture timestamp' }; continue; }
        if (typeof data.aircraft === 'string') aircraft = data.aircraft;
        await session.recordRuntimeMetadata(data);
      }
      artifacts[kind] = await storeArtifact(session, `${kind === 'screenshot' ? 'screenshots' : 'telemetry'}/${id}.${kind === 'screenshot' ? 'png' : 'json'}`, bytes);
    } catch (error) { artifacts[kind === 'log' ? 'logTail' : kind] = failure(error); }
  }
  for (const kind of ['telemetry', 'screenshot', 'logTail']) artifacts[kind] ??= { status: 'missing', error: 'No evidence source configured' };
  return aircraft ? { aircraft, artifacts } : { artifacts };
}
