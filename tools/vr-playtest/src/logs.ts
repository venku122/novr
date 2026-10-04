import { open, lstat } from 'node:fs/promises';
import { resolve, relative, isAbsolute, sep, parse } from 'node:path';
export async function resolveEvidencePath(root: string, path: string): Promise<string> {
  if (isAbsolute(path) || path.includes(':') || path.split(/[\\/]/).includes('..')) throw new Error('Evidence path must remain inside the configured game directory');
  const base = resolve(root), target = resolve(base, path), rel = relative(base, target);
  if (rel.startsWith('..') || isAbsolute(rel)) throw new Error('Evidence path escapes game directory');
  // Reject symlink/junction ancestors, including the configured root.
  const pathRoot = parse(target).root;
  let current = pathRoot;
  if ((await lstat(current)).isSymbolicLink()) throw new Error('Linked evidence paths are not allowed');
  const parts = target.slice(pathRoot.length).split(sep).filter(Boolean);
  for (const part of parts) {
    current = resolve(current, part);
    if ((await lstat(current)).isSymbolicLink()) throw new Error('Linked evidence paths are not allowed');
  }
  return target;
}
export async function readLogTail(path: string, maxBytes = 256 * 1024, maxLines = 500): Promise<string> {
  const file = await open(path, 'r');
  try {
    const size = (await file.stat()).size, start = Math.max(0, size - maxBytes);
    const buffer = Buffer.alloc(Math.min(maxBytes, size));
    const { bytesRead } = await file.read(buffer, 0, buffer.length, start);
    let text = buffer.subarray(0, bytesRead).toString('utf8');
    if (start > 0) text = text.slice(text.indexOf('\n') + 1);
    const trailing = text.endsWith('\n');
    return text.trimEnd().split('\n').slice(-maxLines).join('\n') + (trailing ? '\n' : '');
  } finally { await file.close(); }
}

export async function readBoundedFile(path: string, maxBytes: number): Promise<Buffer> {
  const file = await open(path, 'r');
  try {
    const bytes = Buffer.alloc(maxBytes + 1);
    let total = 0;
    while (total < bytes.length) {
      const result = await file.read(bytes, total, bytes.length - total, total);
      if (!result.bytesRead) break;
      total += result.bytesRead;
    }
    if (total > maxBytes) throw new Error(`File exceeds ${maxBytes} byte limit`);
    return bytes.subarray(0, total);
  } finally { await file.close(); }
}
