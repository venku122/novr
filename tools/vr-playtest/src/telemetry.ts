export function parseFreshTelemetry(text: string, now = Date.now(), maxAgeMs = 15000): Record<string, unknown> | undefined {
  const value: unknown = JSON.parse(text);
  if (!value || typeof value !== 'object' || Array.isArray(value)) throw new Error('Telemetry must be an object');
  const record = value as Record<string, unknown>;
  const time = typeof record.timestamp === 'string' ? Date.parse(record.timestamp) : NaN;
  if (!Number.isFinite(time) || now - time > maxAgeMs || time - now > 2000) return undefined;
  return record;
}
