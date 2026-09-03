import AccordionSection from '../../../../components/ui/AccordionSection.tsx'
import { useSectorsQuery } from '../../../../hooks/query.ts'
import type { ForYouSettings as ForYouSettingsType } from '../../../../types/settings.ts'
import { cn } from '../../../../lib/cn.ts'

/** The feed is intentionally focused — a talent can follow at most 2 sectors. */
const MAX_SECTORS = 2

interface ForYouSettingsProps {
  value: ForYouSettingsType
  onChange: (next: ForYouSettingsType) => void
}

/**
 * "For you" job feed preferences (talent only).
 *
 * The chosen sectors power the For You tab on the jobs page AND the
 * new-job alerts the talent receives. Kept in settings (not the profile)
 * because they're a preference, not a fact about the talent.
 */
export default function ForYouSettings({ value, onChange }: ForYouSettingsProps) {
  const { sectors, isLoading } = useSectorsQuery(true)

  const toggleSector = (id: string) => {
    const selected = value.sectorIds
    if (selected.includes(id)) {
      onChange({ sectorIds: selected.filter((s) => s !== id) })
    } else if (selected.length < MAX_SECTORS) {
      onChange({ sectorIds: [...selected, id] })
    }
    // At the cap, clicking a new sector is a no-op — the hint below explains why.
  }

  return (
    <AccordionSection
      title="For You feed"
      description="Choose up to 2 sectors — your For You jobs and job alerts follow them."
      icon="auto_awesome"
      completion={{ filled: value.sectorIds.length, total: MAX_SECTORS }}
    >
      <div className="md:col-span-2">
        <div className="flex items-center justify-between gap-4">
          <div className="min-w-0">
            <p className="font-label-md text-label-md text-on-surface">Sectors to follow</p>
            <p className="font-label-sm text-label-sm text-on-surface-variant/70">
              Pick up to {MAX_SECTORS}. Your For You tab shows jobs in these sectors and
              you'll get alerts when matching jobs are posted. Leave empty to turn the
              feed off.
            </p>
          </div>
          <span className="shrink-0 font-label-md text-label-md text-on-surface-variant">
            {value.sectorIds.length}/{MAX_SECTORS}
          </span>
        </div>

        {isLoading ? (
          <div className="mt-3 h-10 animate-pulse rounded-lg bg-surface-container-low" />
        ) : sectors.length === 0 ? (
          <p className="mt-3 font-label-sm text-label-sm text-on-surface-variant">
            No sectors available yet.
          </p>
        ) : (
          <div className="mt-3 flex flex-wrap gap-2">
            {sectors.map((sector) => {
              const checked = value.sectorIds.includes(sector.id)
              const atCap = !checked && value.sectorIds.length >= MAX_SECTORS
              return (
                <button
                  key={sector.id}
                  type="button"
                  aria-pressed={checked}
                  aria-disabled={atCap}
                  disabled={atCap}
                  onClick={() => toggleSector(sector.id)}
                  className={cn(
                    'rounded-full px-3.5 py-1.5 font-label-sm text-label-sm font-medium transition-colors',
                    checked
                      ? 'bg-primary text-on-primary'
                      : atCap
                        ? 'cursor-not-allowed border border-surface-variant bg-surface-container-lowest text-on-surface-variant/40'
                        : 'border border-outline-variant bg-surface-container-lowest text-on-surface-variant hover:bg-surface-container-low',
                  )}
                >
                  {sector.name}
                </button>
              )
            })}
          </div>
        )}

        {value.sectorIds.length >= MAX_SECTORS && (
          <p className="mt-2 font-label-sm text-label-sm text-on-surface-variant/70">
            You've reached the {MAX_SECTORS}-sector limit — remove one to add another.
          </p>
        )}
      </div>
    </AccordionSection>
  )
}
