import { cn } from '../../lib/cn.ts'

/**
 * The save/bookmark toggle shown on job cards and the job detail page.
 * Filled + "Saved" when the job is in the user's saved list.
 */
export default function SaveJobButton({
  saved,
  onToggle,
  className,
}: {
  saved: boolean
  onToggle: () => void
  className?: string
}) {
  return (
    <button
      type="button"
      onClick={onToggle}
      aria-pressed={saved}
      title={saved ? 'Remove from saved jobs' : 'Save this job for later'}
      className={cn(
        'inline-flex items-center justify-center gap-1.5 rounded px-4 py-2.5 font-label-md text-label-md transition-colors',
        saved
          ? 'border border-primary/50 bg-primary-container/50 text-primary hover:bg-primary-container/70'
          : 'border border-outline-variant bg-surface-container-lowest text-on-surface-variant hover:bg-surface-container-low hover:text-on-surface',
        className,
      )}
    >
      <span className="material-symbols-outlined text-lg">
        {saved ? 'bookmark' : 'bookmark_add'}
      </span>
      {saved ? 'Saved' : 'Save'}
    </button>
  )
}
