import { spawn } from 'node:child_process';
import { fileURLToPath } from 'node:url';
export interface Transcript { text: string; method: string; segments?: unknown[]; }
export async function transcribe(audioFile: string, powershell = 'powershell.exe'): Promise<Transcript> {
  const script = fileURLToPath(new URL('../../scripts/transcribe.ps1', import.meta.url));
  const child = spawn(powershell, ['-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', script, '-AudioFile', audioFile], { windowsHide: true, shell: false });
  return new Promise((resolve, reject) => {
    let stdout = '', stderr = '', settled = false;
    const timer = setTimeout(() => { child.kill(); finish(new Error('Local transcription timed out')); }, 45000);
    const finish = (error?: Error, value?: Transcript) => { if (settled) return; settled = true; clearTimeout(timer); if (error) reject(error); else resolve(value!); };
    child.stdout.on('data', (data: Buffer) => { stdout += data.toString(); if (stdout.length > 256 * 1024) { child.kill(); finish(new Error('Transcription output exceeded limit')); } });
    child.stderr.on('data', (data: Buffer) => { stderr = (stderr + data.toString()).slice(-8192); });
    child.on('error', error => finish(error));
    child.on('close', code => {
      if (code !== 0) { finish(new Error(`Transcription failed (${code}): ${stderr}`)); return; }
      try {
        const value = JSON.parse(stdout.trim()) as Transcript;
        if (typeof value.text !== 'string' || typeof value.method !== 'string') throw new Error('Invalid transcription response');
        finish(undefined, value);
      } catch (error) { finish(error instanceof Error ? error : new Error(String(error))); }
    });
  });
}
