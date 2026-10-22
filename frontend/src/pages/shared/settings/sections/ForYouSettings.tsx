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
 * 2-column split layout matching the modern settings aesthetic.
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
  }

  return (
    <div id="for-you" className="p-6 sm:p-8 lg:p-10 grid grid-cols-1 lg:grid-cols-12 gap-6 lg:gap-10">
      {/* Left Info Column */}
      <div className="lg:col-span-4 flex flex-col justify-start">
        <h2 className="font-headline-md text-headline-md font-semibold text-on-surface">
          For You Feed
        </h2>
        <p className="font-body-md text-body-md text-on-surface-variant mt-2 leading-relaxed">
          Choose up to 2 sectors to customize your job feed. Your alerts and recommended roles follow these preferences.
        </p>
        <a
          href="/dashboard/talent/jobs?view=forYou"
          className="inline-flex items-center gap-1.5 font-label-md text-label-md text-primary hover:underline mt-4 font-medium"
        >
          <span>Browse For You jobs</span>
          <span className="material-symbols-outlined text-sm leading-none">north_east</span>
        </a>
      </div>

      {/* Right Controls Column */}
      <div className="lg:col-span-8 flex flex-col gap-6">
        <div>
          <div className="flex items-center justify-between pb-3">
            <span className="font-label-md text-label-md text-on-surface font-medium">
              Selected Sectors
            </span>
            <span className="inline-flex items-center px-2.5 py-1 rounded-full bg-surface-container text-on-surface-variant font-label-sm text-label-sm font-medium">
              {value.sectorIds.length}/{MAX_SECTORS} selected {value.sectorIds.length >= MAX_SECTORS ? '(limit reached)' : ''}
            </span>
          </div>

          {/* Active Tags with Remove button */}
          <div className="flex flex-wrap items-center gap-2.5 pt-1">
            {value.sectorIds.map((id) => {
              const matched = sectors.find((s) => s.id === id)
              const name = matched ? matched.name : id
              return (
                <button
                  key={id}
                  type="button"
                  onClick={() => toggleSector(id)}
                  aria-label={`Remove ${name}`}
                  className="inline-flex items-center gap-1.5 px-4 py-2 rounded-full bg-primary border border-primary text-black font-label-md text-label-md font-medium shadow-sm transition-all hover:opacity-95 active:scale-[0.97] group"
                >
                  <span className="material-symbols-outlined text-base text-black inline-block transition-transform duration-500 ease-out rotate-[360deg] group-hover:rotate-0">
                    add
                  </span>
                  <span className="text-black">{name}</span>
                </button>
              )
            })}

            {value.sectorIds.length === 0 && (
              <p className="font-label-sm text-label-sm text-on-surface-variant/70 italic py-1">
                No sectors selected yet. Choose from the list below to activate your feed.
              </p>
            )}
          </div>
        </div>

        {/* Sector selection pool */}
        <div className="pt-4 border-t border-surface-variant">
          <p className="font-label-sm text-label-sm text-on-surface-variant font-medium mb-3">
            {value.sectorIds.length >= MAX_SECTORS
              ? 'Remove a selected sector above to choose another:'
              : 'Available sectors (click to add):'}
          </p>

          {isLoading ? (
            <div className="h-10 animate-pulse rounded-lg bg-surface-container-low" />
          ) : sectors.length === 0 ? (
            <p className="font-label-sm text-label-sm text-on-surface-variant">
              No sectors available yet.
            </p>
          ) : (
            <div className="flex flex-wrap gap-2">
              {sectors.map((sector) => {
                const checked = value.sectorIds.includes(sector.id)
                const atCap = !checked && value.sectorIds.length >= MAX_SECTORS
                if (checked) return null
                return (
                  <button
                    key={sector.id}
                    type="button"
                    disabled={atCap}
                    onClick={() => toggleSector(sector.id)}
                    className={cn(
                      'inline-flex items-center gap-1.5 px-4 py-2 rounded-full font-label-md text-label-md font-medium transition-colors active:scale-[0.97] group',
                      atCap
                        ? 'cursor-not-allowed opacity-50 bg-surface-container-low text-on-surface-variant/50 border border-transparent'
                        : 'bg-surface-container-low border border-surface-variant/60 text-black hover:bg-surface-container',
                    )}
                  >
                    <span
                      className={cn(
                        'material-symbols-outlined text-base inline-block transition-transform duration-500 ease-out',
                        atCap ? 'text-on-surface-variant/50' : 'text-black group-hover:rotate-[360deg]',
                      )}
                    >
                      add
                    </span>
                    <span className={atCap ? 'text-on-surface-variant/50' : 'text-black'}>{sector.name}</span>
                  </button>
                )
              })}
            </div>
          )}
        </div>

        {/* Contextual Tip */}
        <div className="flex items-start gap-3 p-3.5 rounded-lg bg-surface-container-low text-on-surface-variant border border-surface-variant/50">
          <span className="material-symbols-outlined text-primary text-xl mt-0.5 shrink-0">
            verified_user
          </span>
          <p className="font-label-sm text-label-sm text-on-surface-variant leading-snug">
            Your chosen sectors prioritize matching jobs in your feed and trigger instant alerts when new opportunities match.
          </p>
        </div>
      </div>
    </div>
  )
}
