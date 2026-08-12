/**
 * Derives avatar initials from a display name:
 * "Jane Recruiter" → "JR", "Jane" → "JA", empty → "?".
 */
export function initialsOf(name: string): string {
  const parts = name.trim().split(/\s+/).filter(Boolean)
  if (parts.length === 0) return '?'
  if (parts.length === 1) return parts[0].slice(0, 2).toUpperCase()
  const first = parts[0] ?? ''
  const last = parts[parts.length - 1] ?? ''
  return (first[0] ?? '') + (last[0] ?? '').toUpperCase()
}
