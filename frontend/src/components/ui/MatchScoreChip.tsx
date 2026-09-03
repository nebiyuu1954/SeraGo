/**
 * "72% match" pill for an AI match score (0-100).
 *
 * Scores come from the SeraGo-AI matching engine and are precomputed when a
 * job is published (For You feed) or when a talent applies (applications
 * page). A null score means the AI service hasn't scored the pair yet.
 */
export default function MatchScoreChip({
  score,
  matched,
  missing,
}: {
  score: number
  matched?: number
  missing?: number
}) {
  if (score == null) return null

  const tone =
    score >= 75
      ? 'bg-success/10 text-success'
      : score >= 50
        ? 'bg-amber-100 text-amber-800'
        : 'bg-surface-container-low text-on-surface-variant'

  const detail = [
    `Match score: ${score} out of 100`,
    matched != null && matched > 0 ? `${matched} matched` : '',
    missing != null && missing > 0 ? `${missing} missing` : '',
  ]
    .filter(Boolean)
    .join(' · ')

  return (
    <span
      title={detail}
      className={`inline-flex shrink-0 items-center gap-1 rounded-full px-2.5 py-0.5 font-label-sm text-label-sm font-semibold ${tone}`}
    >
      <span className="material-symbols-outlined text-sm">auto_awesome</span>
      {score}% match
    </span>
  )
}
