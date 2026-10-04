import { spawn, type ChildProcessByStdio } from 'node:child_process';
import type { Writable, Readable } from 'node:stream';
import { stat } from 'node:fs/promises';
export interface RecordingOptions { executable: string; device: string; output: string; seconds: number; }
export interface RecordingResult { ok: boolean; error?: string; }
type RecorderProcess = ChildProcessByStdio<Writable, null, Readable>;
export function buildRecordingArgs(device: string, output: string, seconds: number): string[] {
  if (!device.trim() || /[\r\n\0]/.test(device)) throw new Error('A microphone device name is required');
  if (!Number.isInteger(seconds) || seconds < 1 || seconds > 30) throw new Error('Capture duration must be between 1 and 30 seconds');
  return ['-hide_banner', '-loglevel', 'warning', '-n', '-f', 'dshow', '-i', `audio=${device}`, '-t', String(seconds), '-ac', '1', '-ar', '16000', '-c:a', 'pcm_s16le', output];
}
export function startRecording(options: RecordingOptions, launch?: (args: string[]) => RecorderProcess): { completed: Promise<RecordingResult>; stop: () => Promise<RecordingResult> } {
  const args = buildRecordingArgs(options.device, options.output, options.seconds);
  const child = launch ? launch(args) : spawn(options.executable, args, { windowsHide: true, shell: false, stdio: ['pipe', 'ignore', 'pipe'] });
  let timer: NodeJS.Timeout, stopTimer: NodeJS.Timeout | undefined, stopped = false;
  let stderr = '';
  child.stderr.on('data', (data: Buffer) => { stderr = (stderr + data.toString()).slice(-8192); });
  child.stdin.on('error', () => { /* A closing recorder may already have closed its input. */ });
  const completed = new Promise<RecordingResult>(resolve => {
    let settled = false;
    const finish = (result: RecordingResult) => { if (settled) return; settled = true; clearTimeout(timer); clearTimeout(stopTimer); resolve(result); };
    child.once('error', error => finish({ ok: false, error: error.message }));
    child.once('close', async code => {
      if (code !== 0) { finish({ ok: false, error: `Recorder exited ${code}: ${stderr}` }); return; }
      try {
        if ((await stat(options.output)).size <= 44) throw new Error('No audio samples were captured');
        finish({ ok: true });
      } catch (error) { finish({ ok: false, error: String(error) }); }
    });
    timer = setTimeout(() => { child.kill(); }, (options.seconds + 3) * 1000);
  });
  return { completed, stop: () => {
    if (!stopped) {
      stopped = true;
      if (child.exitCode === null && !child.killed) {
        child.stdin.write('q\n');
        stopTimer = setTimeout(() => child.kill(), 5000);
      }
    }
    return completed;
  } };
}
