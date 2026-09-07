import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import {
  fetchScraperWeekStats,
  fetchAdminStatsOverview,
  getApiErrorMessage,
  getStoredAuthTokens,
} from '../../../api'
import { useRequireRole } from '../../../hooks'
import type { ScraperWeekStatsList, AdminStatsOverviewResponse } from '../../../types'
import DashboardShell from '../../../components/dashboard/DashboardShell.tsx'
import { cn } from '../../../lib/cn.ts'

const STATUS_COLORS: Record<string, string> = {
  success: 'bg-success-container text-on-success-container',
  partial: 'bg-amber-100 text-amber-900',
  failed: 'bg-error-container text-on-error-container',
  running: 'bg-blue-100 text-blue-800',
  unconfigured: 'bg-surface-container-low text-on-surface-variant',
  unreachable: 'bg-error-container text-on-error-container',
  no_data: 'bg-surface-container-low text-on-surface-variant',
  week_stats: 'bg-blue-100 text-blue-800',
}

export default function AdminScraperPage() {
  const auth = useRequireRole('Admin')
  const navigate = useNavigate()

  const [weekStats, setWeekStats] = useState<ScraperWeekStatsList | null>(null)
  const [overview, setOverview] = useState<AdminStatsOverviewResponse | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    const tokens = getStoredAuthTokens()
    if (!tokens) return
    setLoading(true)
    setError(null)
    Promise.allSettled([
      fetchScraperWeekStats(tokens.accessToken),
      fetchAdminStatsOverview(tokens.accessToken),
    ]).then(([weeksRes, statsRes]) => {
      if (weeksRes.status === 'fulfilled') setWeekStats(weeksRes.value)
      if (statsRes.status === 'fulfilled') setOverview(statsRes.value)
    }).catch((err) => setError(getApiErrorMessage(err))).finally(() => setLoading(false))
  }, [])

  if (auth.status !== 'authenticated') {
    return (
      <div className="flex min-h-[50svh] items-center justify-center">
        <span aria-hidden="true" className="h-8 w-8 animate-spin rounded-full border-2 border-primary/30 border-t-primary" />
      </div>
    )
  }

  return (
    <DashboardShell role="Admin" authUser={auth.user}>
      <h1 className="font-headline-lg text-headline-lg font-bold tracking-tight text-primary">Scraper</h1>
      <p className="mt-1 font-body-md text-body-md text-on-surface-variant">
        Scrape activity across all job sources, by day and by week.
      </p>

      {error && (
        <div role="alert" className="mt-4 rounded-xl border border-error/30 bg-error-container px-4 py-3 font-label-md text-label-md text-on-error-container">
          {error}
        </div>
      )}

      {loading ? (
        <div className="mt-10 flex items-center justify-center">
          <span aria-hidden="true" className="h-8 w-8 animate-spin rounded-full border-2 border-primary/30 border-t-primary" />
        </div>
      ) : (
        <>
          {/* Today's scraper activity */}
          <div className="mt-6">
            <h2 className="font-label-md text-label-md font-semibold text-on-surface">Today</h2>
            <p className="mt-1 font-label-sm text-label-sm text-on-surface-variant">
              Today's scrape results across all sources.
            </p>
            {overview?.scraper.day ? (
              <TodayScraperRow scraper={overview.scraper} />
            ) : (
              <div className="mt-3 rounded-xl border border-surface-variant bg-surface-container-lowest px-5 py-4 shadow-sm">
                <p className="font-body-md text-body-md text-on-surface-variant">
                  No scrape data available for today yet.
                </p>
              </div>
            )}
          </div>

          {/* Per-site breakdown table */}
          {overview?.scraper.sites.length && (
            <div className="mt-6">
              <h2 className="font-label-md text-label-md font-semibold text-on-surface">Per-site breakdown</h2>
              <p className="mt-1 font-label-sm text-label-sm text-on-surface-variant">
                Today's scrape results per source.
              </p>
              <div className="mt-3 overflow-x-auto rounded-xl border border-surface-variant bg-surface-container-lowest shadow-sm">
                <table className="w-full">
                  <thead>
                    <tr className="border-b border-surface-variant bg-surface-container-low">
                      <th className="px-4 py-3 text-left font-label-sm text-label-sm font-semibold text-on-surface">Site</th>
                      <th className="px-4 py-3 text-left font-label-sm text-label-sm font-semibold text-on-surface">Status</th>
                      <th className="px-4 py-3 text-right font-label-sm text-label-sm font-semibold text-on-surface">Runs</th>
                      <th className="px-4 py-3 text-right font-label-sm text-label-sm font-semibold text-on-surface">API hits</th>
                      <th className="px-4 py-3 text-right font-label-sm text-label-sm font-semibold text-on-surface">Found</th>
                      <th className="px-4 py-3 text-right font-label-sm text-label-sm font-semibold text-on-surface">Inserted</th>
                      <th className="px-4 py-3 text-right font-label-sm text-label-sm font-semibold text-on-surface">Updated</th>
                      <th className="px-4 py-3 text-right font-label-sm text-label-sm font-semibold text-on-surface">Skipped</th>                  </tr>
                </thead>
                <tbody className="divide-y divide-surface-variant">
                  {overview.scraper.sites.map((site) => (
                      <tr key={site.source} className="transition-colors hover:bg-surface-container-low/50">
                        <td className="px-4 py-3 font-body-sm text-sm font-medium text-on-surface">{site.name}</td>
                        <td className="px-4 py-3">
                          <span className={cn(
                            'inline-flex items-center rounded-full px-2 py-0.5 font-label-xs text-[11px] font-medium',
                            STATUS_COLORS[site.status] ?? 'bg-surface-container-low text-on-surface-variant',
                          )}>
                            {site.status}
                          </span>
                        </td>
                        <td className="px-4 py-3 text-right font-body-sm text-sm text-on-surface">{site.runCount}</td>
                        <td className="px-4 py-3 text-right font-body-sm text-sm text-on-surface">{site.apiHits}</td>
                        <td className="px-4 py-3 text-right font-body-sm text-sm text-on-surface">{site.itemsFound}</td>
                        <td className="px-4 py-3 text-right font-body-sm text-sm text-success">{site.itemsInserted}</td>
                        <td className="px-4 py-3 text-right font-body-sm text-sm text-on-surface-variant">{site.itemsUpdated}</td>
                        <td className="px-4 py-3 text-right font-body-sm text-sm text-on-surface-variant">{site.itemsSkipped}</td>
                      </tr>
                    ))}
                    <tr className="divide-y divide-surface-variant font-body-sm text-sm text-on-surface">
                      <td className="px-4 py-3 font-medium">Total</td>
                      <td className="px-4 py-3"></td>
                      <td className="px-4 py-3 text-right">{overview.scraper.sites.reduce((s, site) => s + site.runCount, 0)}</td>
                      <td className="px-4 py-3 text-right">{overview.scraper.sites.reduce((s, site) => s + site.apiHits, 0)}</td>
                      <td className="px-4 py-3 text-right">{overview.scraper.sites.reduce((s, site) => s + site.itemsFound, 0)}</td>
                      <td className="px-4 py-3 text-right text-success">{overview.scraper.sites.reduce((s, site) => s + site.itemsInserted, 0)}</td>
                      <td className="px-4 py-3 text-right text-on-surface-variant">{overview.scraper.sites.reduce((s, site) => s + site.itemsUpdated, 0)}</td>
                      <td className="px-4 py-3 text-right text-on-surface-variant">{overview.scraper.sites.reduce((s, site) => s + site.itemsSkipped, 0)}</td>
                    </tr>
                  </tbody>
                </table>
              </div>
            </div>
          )}

          {/* This week section */}
          <div className="mt-8">
            <h2 className="font-label-md text-label-md font-semibold text-on-surface">This week</h2>
            <p className="mt-1 font-label-sm text-label-sm text-on-surface-variant">
              Pick a week to see its full scraper statistics.
            </p>

            {weekStats && weekStats.weeks.length > 0 ? (
              <div className="mt-3 space-y-2">
                {weekStats.weeks.slice(0, 12).map((week) => (
                  <WeekRow
                    key={week.periodStart}
                    week={week}
                    onShowStat={() => navigate(`/dashboard/admin/scraper/weeks/${week.periodStart}`)}
                  />
                ))}
              </div>
            ) : (
              <div className="mt-3 rounded-xl border border-surface-variant bg-surface-container-lowest px-5 py-4 shadow-sm">
                <p className="font-body-md text-body-md text-on-surface-variant">
                  No scraper week data available yet.
                </p>
              </div>
            )}
          </div>
        </>
      )}
    </DashboardShell>
  )
}

function TodayScraperRow({ scraper }: { scraper: AdminStatsOverviewResponse['scraper'] }) {
  return (
    <div className="mt-3 rounded-xl border border-surface-variant bg-surface-container-lowest px-5 py-4 shadow-sm">
      <div className="flex items-center gap-2 mb-3">
        <span className={cn(
          'inline-flex items-center rounded-full px-2.5 py-0.5 font-label-sm text-label-sm font-medium',
          STATUS_COLORS[scraper.status] ?? 'bg-surface-container-low text-on-surface-variant',
        )}>
          {scraper.status}
        </span>
        <span className="font-label-sm text-label-sm text-on-surface-variant">
          {scraper.day ? `Data for ${new Date(scraper.day).toLocaleDateString()}` : 'No scrape data available'}
        </span>
      </div>
      <div className="grid grid-cols-2 gap-3 sm:grid-cols-5">
        <StatPill label="Sites scraped" value={scraper.sitesScraped} />
        <StatPill label="Total runs" value={scraper.totalRunCount} />
        <StatPill label="API hits" value={scraper.totalApiHits} />
        <StatPill label="Items found" value={scraper.totalItemsFound} accent="text-success" />
        <StatPill label="Items inserted" value={scraper.totalItemsInserted} accent="text-primary" />
      </div>
    </div>
  )
}

function WeekRow({ week, onShowStat }: { week: ScraperWeekStatsList['weeks'][0]; onShowStat: () => void }) {
  return (
    <div className="flex items-center justify-between gap-3 rounded-xl border border-surface-variant bg-surface-container-lowest px-4 py-3.5 shadow-sm transition-colors hover:border-outline-variant hover:shadow-md">
      <div className="min-w-0 flex-1">
        <div className="flex items-center gap-2">
          <span className="font-body-md text-body-md font-medium text-on-surface">
            {new Date(week.periodStart + 'T00:00:00Z').toLocaleDateString(undefined, { month: 'long', day: 'numeric', year: 'numeric' })}
          </span>
          {week.worstDay ? (
            <span className="inline-flex items-center gap-1 rounded-full bg-error/10 px-2 py-0.5 font-label-xs text-error">
              <span className="material-symbols-outlined text-xs">warning</span>
              {week.worstDay}
            </span>
          ) : null}
        </div>
        <div className="mt-1 flex flex-wrap gap-2">
          <span className="inline-flex items-center gap-1 rounded-full bg-surface-container-high px-2.5 py-0.5 font-label-xs text-label-xs text-on-surface-variant">
            <span className="material-symbols-outlined text-xs">schedule</span>
            {week.daysWithRuns} day(s)
          </span>
          <span className="inline-flex items-center gap-1 rounded-full bg-surface-container-high px-2.5 py-0.5 font-label-xs text-label-xs text-on-surface-variant">
            <span className="material-symbols-outlined text-xs">play_arrow</span>
            {week.runCount} runs
          </span>
          <span className="inline-flex items-center gap-1 rounded-full bg-surface-container-high px-2.5 py-0.5 font-label-xs text-label-xs text-on-surface-variant">
            <span className="material-symbols-outlined text-xs">dns</span>
            {week.apiHits} API hits
          </span>
          <span className="inline-flex items-center gap-1 rounded-full bg-surface-container-high px-2.5 py-0.5 font-label-xs text-label-xs text-on-surface-variant">
            <span className="material-symbols-outlined text-xs">library_add</span>
            {week.itemsInserted} inserted
          </span>
        </div>
        <p className="mt-1 font-label-xs text-label-xs text-on-surface-variant">
          {week.statusSummary}
        </p>
      </div>
      <button
        type="button"
        onClick={onShowStat}
        className="flex h-9 min-w-0 shrink-0 items-center justify-center rounded-lg border border-outline-variant bg-surface-container-lowest px-4 font-label-md text-label-md text-on-surface transition-colors hover:bg-surface-container-low hover:text-primary disabled:pointer-events-none disabled:opacity-40"
      >
        <span className="material-symbols-outlined text-lg mr-1">bar_chart</span>
        Show stat
      </button>
    </div>
  )
}

function StatPill({ label, value, accent }: { label: string; value: number; accent?: string }) {
  return (
    <div className="rounded-lg bg-surface-container-high px-3 py-2 text-center">
      <p className={cn('font-headline-sm text-headline-sm font-bold text-on-surface', accent ?? 'text-on-surface')}>
        {value.toLocaleString()}
      </p>
      <p className="font-label-xs text-label-xs text-on-surface-variant">{label}</p>
    </div>
  )
}
