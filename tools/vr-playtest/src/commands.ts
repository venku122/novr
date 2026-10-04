export type CommandKind = 'observe' | 'investigate' | 'implement' | 'deploy' | 'proposal';
export function parseCommand(text: string, source: 'typed' | 'transcript'): { kind: CommandKind; proposed?: string } {
  const normalized = text.trim().toLowerCase().replace(/[.!?]+$/, '').trim();
  const commands: Record<string, 'investigate' | 'implement' | 'deploy'> = {
    'investigate that': 'investigate', 'why is that happening': 'investigate',
    'implement that': 'implement', 'deploy that build': 'deploy',
  };
  const kind = commands[normalized];
  if (!kind) return { kind: 'observe' };
  // Speech is evidence, never authorization to launch an agent or mutate files.
  return source === 'transcript' ? { kind: 'proposal', proposed: kind } : { kind };
}
export function classifyObservation(text: string): string {
  if (/controller|trigger|thumbstick|button|pointer/i.test(text)) return 'controller';
  if (/canvas|\bui\b|menu|floating/i.test(text)) return 'ui';
  if (/head|cockpit|pilot|camera|seat|recenter|clipping/i.test(text)) return 'camera';
  if (/graphics|resolution|frame.?time|performance|blurry|render/i.test(text)) return 'rendering';
  return 'general';
}
