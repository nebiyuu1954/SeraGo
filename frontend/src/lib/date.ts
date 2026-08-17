/** "2026-09-15T14:00:00Z" → "Sep 15, 2026" — or '' when null/unparseable. */
export function formatDate(iso: string | null): string {
  if (!iso) return ''
  const d = new Date(iso)
  if (Number.isNaN(d.getTime())) return ''
  return d.toLocaleDateString(undefined, {
    month: 'short',
    day: 'numeric',
    year: 'numeric',
  })
}

/** "2026-09-15T14:00:00Z" → "Today" / "2d ago" / "3w ago" / "4mo ago". */
export function timeAgo(iso: string | null): string {
  if (!iso) return ''
  const then = new Date(iso).getTime()
  if (Number.isNaN(then)) return ''
  const days = Math.max(0, Math.floor((Date.now() - then) / 86_400_000))
  if (days < 1) return 'Today'
  if (days < 7) return `${days}d ago`
  if (days < 30) return `${Math.floor(days / 7)}w ago`
  if (days < 365) return `${Math.floor(days / 30)}mo ago`
  return `${Math.floor(days / 365)}y ago`
}

/**
 * Card date label: "Refreshed 2d ago" when the listing was refreshed after
 * publishing (the employer renewed it — the original publish date would
 * mislead), else "Posted 4w ago".
 */
export function postedLabel(
  publishedAt: string | null,
  refreshedAt: string | null,
): string {
  const published = publishedAt ? new Date(publishedAt).getTime() : NaN
  const refreshed = refreshedAt ? new Date(refreshedAt).getTime() : NaN
  if (!Number.isNaN(refreshed) && (Number.isNaN(published) || refreshed > published)) {
    return `Refreshed ${timeAgo(refreshedAt) || 'recently'}`
  }
  return publishedAt ? `Posted ${timeAgo(publishedAt) || 'recently'}` : ''
}
