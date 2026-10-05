import { useEffect, useState } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import {
  fetchScraperWeekDetail,
  getApiErrorMessage,
  getStoredAuthTokens,
} from '../../../api'
import { useRequireRole } from '../../../hooks'
import type { ScraperWeekDetail } from '../../../types'
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

export default function AdminScraperWeekDetailPage() {
  const { periodStart } = useParams<{ periodStart: string }>()
  const auth = useRequireRole('Admin')
  const navigate = useNavigate()

  const [detail, setDetail] = useState<ScraperWeekDetail | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    const tokens = getStoredAuthTokens()
    if (!tokens || !periodStart) return
    setLoading(true)
    setError(null)
    fetchScraperWeekDetail(periodStart, tokens.accessToken)
      .then(setDetail)
      .catch((err) => setError(getApiErrorMessage(err)))
      .finally(() => setLoading(false))
  }, [periodStart])

  if (auth.status !== 'authenticated') {
    return (
      <div className="flex min-h-[50svh] items-center justify-center">
        <span aria-hidden="true" className="h-8 w-8 animate-spin rounded-full border-2 border-primary/30 border-t-primary" />
      </div>
    )
  }

  return (
    <DashboardShell role="Admin" authUser={auth.user}>
      <div className="flex items-center gap-3">
        <button
          type="button"
          onClick={() => navigate('/dashboard/admin/scraper')}
          className="inline-flex items-center gap-1.5 font-label-md text-label-md text-on-surface-variant transition-colors hover:text-primary"
        >
          <span className="material-symbols-outlined text-lg">arrow_back</span>
          Back to scraper
        </button>
      </div>

      {error && (
        <div role="alert" className="mt-4 rounded-xl border border-error/30 bg-error-container px-4 py-3 font-label-md text-label-md text-on-error-container">
          {error}
        </div>
      )}

      {loading ? (
        <div className="mt-10 flex items-center justify-center">
          <span aria-hidden="true" className="h-8 w-8 animate-spin rounded-full border-2 border-primary/30 border-t-primary" />
        </div>
      ) : detail ? (
        <>
          {/* Header */}
          <div className="mt-6 flex items-center justify-between gap-4">
            <div>
              <h1 className="font-headline-lg text-headline-lg font-bold tracking-tight text-primary">
                {detail.periodLabel}
              </h1>
              <p className="mt-1 font-body-md text-body-md text-on-surface-variant">
                Scraper activity for this week.
              </p>
            </div>
            {detail.archiveNote && (
              <div className="rounded-xl border border-surface-variant bg-surface-container-lowest px-4 py-3 shadow-sm">
                <p className="font-label-sm text-label-sm text-on-surface-variant">Archive</p>
                <p className="mt-0.5 font-body-sm text-sm text-on-surface">{detail.archiveNote}</p>
              </div>
            )}
          </div>

          {/* Week totals */}
          <div className="mt-4 grid grid-cols-2 gap-3 sm:grid-cols-5">
            <StatPill label="Days with runs" value={detail.daysWithRuns} />
            <StatPill label="Total runs" value={detail.totalRunCount} />
            <StatPill label="API hits" value={detail.totalApiHits} />
            <StatPill label="Items found" value={detail.totalItemsFound} accent="text-success" />
            <StatPill label="Items inserted" value={detail.totalItemsInserted} accent="text-primary" />
          </div>

          {/* Status summary */}
          <div className="mt-4 flex items-center gap-3 rounded-xl border border-surface-variant bg-surface-container-lowest px-5 py-4 shadow-sm">
            <span className="font-label-sm text-label-sm text-on-surface-variant">Status summary:</span>
            <span className="font-label-sm text-label-sm font-medium text-on-surface">{detail.statusSummary}</span>
          </div>

          {/* Per-day table */}
          <div className="mt-6">
            <h2 className="font-label-md text-label-md font-semibold text-on-surface">Daily activity</h2>
            <p className="mt-1 font-label-sm text-label-sm text-on-surface-variant">
              One row per day with the master log totals and per-site breakdown.
            </p>
            <div className="mt-3 overflow-x-auto rounded-xl border border-surface-variant bg-surface-container-lowest shadow-sm">
              <table className="w-full">
                <thead>
                  <tr className="border-b border-surface-variant bg-surface-container-low">
                    <th className="px-4 py-3 text-left font-label-sm text-label-sm font-semibold text-on-surface">Day</th>
                    <th className="px-4 py-3 text-left font-label-sm text-label-sm font-semibold text-on-surface">Status</th>
                    <th className="px-4 py-3 text-right font-label-sm text-label-sm font-semibold text-on-surface">Runs</th>
                    <th className="px-4 py-3 text-right font-label-sm text-label-sm font-semibold text-on-surface">API hits</th>
                    <th className="px-4 py-3 text-right font-label-sm text-label-sm font-semibold text-on-surface">Found</th>
                    <th className="px-4 py-3 text-right font-label-sm text-label-sm font-semibold text-on-surface">Inserted</th>
                    <th className="px-4 py-3 text-right font-label-sm text-label-sm font-semibold text-on-surface">Updated</th>
                    <th className="px-4 py-3 text-right font-label-sm text-label-sm font-semibold text-on-surface">Skipped</th>
                    <th className="px-4 py-3 text-left font-label-sm text-label-sm font-semibold text-on-surface">Sites</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-surface-variant">
                  {detail.days.map((day) => (
                    <tr key={day.day} className="transition-colors hover:bg-surface-container-low/50">
                      <td className="px-4 py-3 font-body-sm text-sm font-medium text-on-surface">
                        {new Date(day.day + 'T00:00:00Z').toLocaleDateString(undefined, { weekday: 'short', month: 'short', day: 'numeric' })}
                      </td>
                      <td className="px-4 py-3">
                        <span className={cn('inline-flex items-center rounded-full px-2 py-0.5 font-label-xs text-[11px] font-medium', STATUS_COLORS[day.status] ?? 'bg-surface-container-low text-on-surface-variant')}>
                          {day.status}
                        </span>
                      </td>
                      <td className="px-4 py-3 text-right font-body-sm text-sm text-on-surface">{day.runCount}</td>
                      <td className="px-4 py-3 text-right font-body-sm text-sm text-on-surface">{day.apiHits}</td>
                      <td className="px-4 py-3 text-right font-body-sm text-sm text-on-surface">{day.itemsFound}</td>
                      <td className="px-4 py-3 text-right font-body-sm text-sm text-success">{day.itemsInserted}</td>
                      <td className="px-4 py-3 text-right font-body-sm text-sm text-on-surface-variant">{day.itemsUpdated}</td>
                      <td className="px-4 py-3 text-right font-body-sm text-sm text-on-surface-variant">{day.itemsSkipped}</td>
                      <td className="px-4 py-3 font-label-xs text-label-xs text-on-surface-variant">{day.websitesCount} site(s)</td>
                    </tr>
                  ))}
                  <tr className="divide-y divide-surface-variant font-body-sm text-sm text-on-surface">
                    <td className="px-4 py-3 font-medium">Total</td>
                    <td className="px-4 py-3"></td>
                    <td className="px-4 py-3 text-right">{detail.days.reduce((s, d) => s + d.runCount, 0)}</td>
                    <td className="px-4 py-3 text-right">{detail.days.reduce((s, d) => s + d.apiHits, 0)}</td>
                    <td className="px-4 py-3 text-right">{detail.days.reduce((s, d) => s + d.itemsFound, 0)}</td>
                    <td className="px-4 py-3 text-right text-success">{detail.days.reduce((s, d) => s + d.itemsInserted, 0)}</td>
                    <td className="px-4 py-3 text-right text-on-surface-variant">{detail.days.reduce((s, d) => s + d.itemsUpdated, 0)}</td>
                    <td className="px-4 py-3 text-right text-on-surface-variant">{detail.days.reduce((s, d) => s + d.itemsSkipped, 0)}</td>
                    <td className="px-4 py-3 text-left text-on-surface-variant"></td>
                  </tr>
                </tbody>
              </table>
            </div>
          </div>

          {/* Per-site breakdown table */}
          {detail.sources.length > 0 && (
            <div className="mt-6">
              <h2 className="font-label-md text-label-md font-semibold text-on-surface">Per-site breakdown</h2>
              <p className="mt-1 font-label-sm text-label-sm text-on-surface-variant">
                Aggregated totals per source for this week.
              </p>
              <div className="mt-3 overflow-x-auto rounded-xl border border-surface-variant bg-surface-container-lowest shadow-sm">
                <table className="w-full">
                  <thead>
                    <tr className="border-b border-surface-variant bg-surface-container-low">
                      <th className="px-4 py-3 text-left font-label-sm text-label-sm font-semibold text-on-surface">Source</th>
                      <th className="px-4 py-3 text-right font-label-sm text-label-sm font-semibold text-on-surface">Days active</th>
                      <th className="px-4 py-3 text-right font-label-sm text-label-sm font-semibold text-on-surface">Runs</th>
                      <th className="px-4 py-3 text-right font-label-sm text-label-sm font-semibold text-on-surface">API hits</th>
                      <th className="px-4 py-3 text-right font-label-sm text-label-sm font-semibold text-on-surface">Found</th>
                      <th className="px-4 py-3 text-right font-label-sm text-label-sm font-semibold text-on-surface">Inserted</th>
                      <th className="px-4 py-3 text-right font-label-sm text-label-sm font-semibold text-on-surface">Updated</th>
                      <th className="px-4 py-3 text-right font-label-sm text-label-sm font-semibold text-on-surface">Skipped</th>
                      <th className="px-4 py-3 text-left font-label-sm text-label-sm font-semibold text-on-surface">Status</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-surface-variant">
                    {detail.sources.map((src) => (
                      <tr key={src.source} className="transition-colors hover:bg-surface-container-low/50">
                        <td className="px-4 py-3 font-body-sm text-sm font-medium text-on-surface">{src.name}</td>
                        <td className="px-4 py-3 text-right font-body-sm text-sm text-on-surface">{src.daysActive}</td>
                        <td className="px-4 py-3 text-right font-body-sm text-sm text-on-surface">{src.runCount}</td>
                        <td className="px-4 py-3 text-right font-body-sm text-sm text-on-surface">{src.apiHits}</td>
                        <td className="px-4 py-3 text-right font-body-sm text-sm text-success">{src.itemsFound}</td>
                        <td className="px-4 py-3 text-right font-body-sm text-sm text-primary">{src.itemsInserted}</td>
                        <td className="px-4 py-3 text-right font-body-sm text-sm text-on-surface-variant">{src.itemsUpdated}</td>
                        <td className="px-4 py-3 text-right font-body-sm text-sm text-on-surface-variant">{src.itemsSkipped}</td>
                        <td className="px-4 py-3">
                          <span className={cn('inline-flex items-center rounded-full px-2 py-0.5 font-label-xs text-[11px] font-medium', (STATUS_COLORS[src.statusSummary.includes('failed') ? 'failed' : src.statusSummary.includes('partial') ? 'partial' : 'success'] ?? 'bg-surface-container-low text-on-surface-variant'))}>
                            {src.statusSummary}
                          </span>
                        </td>
                      </tr>
                    ))}
                    <tr className="divide-y divide-surface-variant font-body-sm text-sm text-on-surface">
                      <td className="px-4 py-3 font-medium">Total</td>
                      <td className="px-4 py-3 text-right">{detail.sources.reduce((s, src) => s + src.daysActive, 0)}</td>
                      <td className="px-4 py-3 text-right">{detail.sources.reduce((s, src) => s + src.runCount, 0)}</td>
                      <td className="px-4 py-3 text-right">{detail.sources.reduce((s, src) => s + src.apiHits, 0)}</td>
                      <td className="px-4 py-3 text-right text-success">{detail.sources.reduce((s, src) => s + src.itemsFound, 0)}</td>
                      <td className="px-4 py-3 text-right text-primary">{detail.sources.reduce((s, src) => s + src.itemsInserted, 0)}</td>
                      <td className="px-4 py-3 text-right text-on-surface-variant">{detail.sources.reduce((s, src) => s + src.itemsUpdated, 0)}</td>
                      <td className="px-4 py-3 text-right text-on-surface-variant">{detail.sources.reduce((s, src) => s + src.itemsSkipped, 0)}</td>
                      <td className="px-4 py-3"></td>
                    </tr>
                  </tbody>
                </table>
              </div>
            </div>
          )}

          {/* Runs detail table (master runs) */}
          {detail.days.some(d => d.runs.length > 0) && (
            <div className="mt-6">
              <h2 className="font-label-md text-label-md font-semibold text-on-surface">Run sweeps</h2>
              <p className="mt-1 font-label-sm text-label-sm text-on-surface-variant">
                Each overall scrape sweep (the daily scrape_all runs) with its totals.
              </p>
              <div className="mt-3 overflow-x-auto rounded-xl border border-surface-variant bg-surface-container-lowest shadow-sm">
                <table className="w-full">
                  <thead>
                    <tr className="border-b border-surface-variant bg-surface-container-low">
                      <th className="px-4 py-3 text-left font-label-sm text-label-sm font-semibold text-on-surface">Day</th>
                      <th className="px-4 py-3 text-left font-label-sm text-label-sm font-semibold text-on-surface">Run</th>
                      <th className="px-4 py-3 text-left font-label-sm text-label-sm font-semibold text-on-surface">Time</th>
                      <th className="px-4 py-3 text-right font-label-sm text-label-sm font-semibold text-on-surface">Hits</th>
                      <th className="px-4 py-3 text-right font-label-sm text-label-sm font-semibold text-on-surface">Found</th>
                      <th className="px-4 py-3 text-right font-label-sm text-label-sm font-semibold text-on-surface">Inserted</th>
                      <th className="px-4 py-3 text-right font-label-sm text-label-sm font-semibold text-on-surface">Updated</th>
                      <th className="px-4 py-3 text-right font-label-sm text-label-sm font-semibold text-on-surface">Skipped</th>
                      <th className="px-4 py-3 text-left font-label-sm text-label-sm font-semibold text-on-surface">Status</th>
                      <th className="px-4 py-3 text-left font-label-sm text-label-sm font-semibold text-on-surface">Message</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-surface-variant">
                    {[...detail.days].reverse().map((day) =>
                      [...day.runs].reverse().map((run) => (
                        <tr key={`${day.day}-run-${run.run}`} className="transition-colors hover:bg-surface-container-low/50">
                          <td className="px-4 py-3 font-body-sm text-sm text-on-surface">
                            {new Date(day.day + 'T00:00:00Z').toLocaleDateString(undefined, { weekday: 'short', month: 'short', day: 'numeric' })}
                          </td>
                          <td className="px-4 py-3 text-right font-body-sm text-sm text-on-surface-variant">#{run.run}</td>
                          <td className="px-4 py-3 font-body-sm text-sm text-on-surface-variant">{run.time || '—'}</td>
                          <td className="px-4 py-3 text-right font-body-sm text-sm text-on-surface">{run.hits}</td>
                          <td className="px-4 py-3 text-right font-body-sm text-sm text-success">{run.found}</td>
                          <td className="px-4 py-3 text-right font-body-sm text-sm text-primary">{run.inserted}</td>
                          <td className="px-4 py-3 text-right font-body-sm text-sm text-on-surface-variant">{run.updated}</td>
                          <td className="px-4 py-3 text-right font-body-sm text-sm text-on-surface-variant">{run.skipped}</td>
                          <td className="px-4 py-3">
                            <span className={cn('inline-flex items-center rounded-full px-2 py-0.5 font-label-xs text-[11px] font-medium', STATUS_COLORS[run.status] ?? 'bg-surface-container-low text-on-surface-variant')}>
                              {run.status}
                            </span>
                          </td>
                          <td className="px-4 py-3 font-body-sm text-sm text-on-surface-variant max-w-xs truncate">{run.message || '—'}</td>
                        </tr>
                      ))
                    )}
                  </tbody>
                </table>
              </div>
            </div>
          )}

          {/* Top errors table */}
          {detail.topErrors.length > 0 && (
            <div className="mt-6">
              <h2 className="font-label-md text-label-md font-semibold text-on-surface">Top errors</h2>
              <p className="mt-1 font-label-sm text-label-sm text-on-surface-variant">
                Most common error messages across all per-site runs this week.
              </p>
              <div className="mt-3 overflow-x-auto rounded-xl border border-surface-variant bg-surface-container-lowest shadow-sm">
                <table className="w-full">
                  <thead>
                    <tr className="border-b border-surface-variant bg-surface-container-low">
                      <th className="px-4 py-3 text-left font-label-sm text-label-sm font-semibold text-on-surface">Error message</th>
                      <th className="px-4 py-3 text-right font-label-sm text-label-sm font-semibold text-on-surface">Count</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-surface-variant">
                    {detail.topErrors.map((err, idx) => (
                      <tr key={idx} className="transition-colors hover:bg-surface-container-low/50">
                        <td className="px-4 py-3 font-body-sm text-sm text-on-surface max-w-xs truncate">{err.message}</td>
                        <td className="px-4 py-3 text-right font-body-sm text-sm text-error font-medium">{err.count}</td>
                      </tr>
                    ))}
                    <tr className="divide-y divide-surface-variant font-body-sm text-sm text-on-surface">
                      <td className="px-4 py-3 font-medium">Total</td>
                      <td className="px-4 py-3 text-right text-error">{detail.topErrors.reduce((s, e) => s + e.count, 0)}</td>
                    </tr>
                  </tbody>
                </table>
              </div>
            </div>
          )}

          {/* Clear data display: JSON view */}
          <div className="mt-6">
            <h2 className="font-label-md text-label-md font-semibold text-on-surface">Raw data</h2>
            <p className="mt-1 font-label-sm text-label-sm text-on-surface-variant">
              The full week detail in JSON format — useful for debugging and export.
            </p>
            <pre className="mt-3 max-h-96 overflow-auto rounded-xl border border-surface-variant bg-inverse-surface p-4 font-body-sm text-sm text-on-surface">
              {JSON.stringify(detail, null, 2)}
            </pre>
          </div>
        </>
      ) : null}
    </DashboardShell>
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
