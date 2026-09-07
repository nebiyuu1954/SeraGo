import { useEffect, useState } from 'react'
import { Link, useLocation } from 'react-router-dom'
import {
  fetchJobViewsStats,
  getApiErrorMessage,
  getStoredAuthTokens,
} from '../../../api'
import { useRequireRole } from '../../../hooks'
import type { JobViewsStatsResponse } from '../../../types'
import DashboardShell from '../../../components/dashboard/DashboardShell.tsx'
import PieChart from '../../../components/ui/PieChart.tsx'
import BarChart from '../../../components/ui/BarChart.tsx'

const PIE_COLORS = [
  '#6750A4', '#7D5260', '#00696E', '#B3261E', '#4A628A',
  '#7D9B76', '#C48F2C', '#5B5B5B', '#9A4A8F', '#2C6491',
]

function toSlices(items: { name: string; value: number }[]) {
  // Group everything past the top 9 slices into "Other" so the pie stays readable.
  if (items.length <= 10) {
    return items.map((it, i) => ({ label: it.name, value: it.value, color: PIE_COLORS[i % PIE_COLORS.length] }))
  }
  const top = items.slice(0, 9).map((it, i) => ({ label: it.name, value: it.value, color: PIE_COLORS[i] }))
  const restValue = items.slice(9).reduce((s, it) => s + it.value, 0)
  return [...top, { label: 'Other', value: restValue, color: PIE_COLORS[9] }]
}

export default function AdminJobsViewsPage() {
  const auth = useRequireRole('Admin')
  const location = useLocation() as { state?: { from?: string } }
  const backTo = location.state?.from ?? '/dashboard/admin'

  const [stats, setStats] = useState<JobViewsStatsResponse | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    const tokens = getStoredAuthTokens()
    if (!tokens) return
    setLoading(true)
    setError(null)
    fetchJobViewsStats(tokens.accessToken)
      .then(setStats)
      .catch((err) => setError(getApiErrorMessage(err)))
      .finally(() => setLoading(false))
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
      <div className="flex items-center gap-3">
        <Link
          to={backTo}
          className="inline-flex items-center gap-1.5 font-label-md text-label-md text-on-surface-variant transition-colors hover:text-primary"
        >
          <span className="material-symbols-outlined text-lg">arrow_back</span>
          {backTo === '/dashboard/admin/jobs' ? 'Back to jobs' : 'Back to dashboard'}
        </Link>
      </div>

      <h1 className="mt-4 font-headline-lg text-headline-lg font-bold tracking-tight text-primary">
        View analytics
      </h1>
      <p className="mt-1 font-body-md text-body-md text-on-surface-variant">
        How talents discover and view jobs across SeraGo and scraped sources.
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
      ) : stats ? (
        <>
          {/* ──── Headline stats ──── */}
          <div className="mt-5 grid grid-cols-2 gap-3 sm:grid-cols-3 lg:grid-cols-5">
            <HeadlineStat icon="visibility" label="Total views" value={stats.totalViews} accent="text-primary" />
            <HeadlineStat icon="today" label="Today" value={stats.viewsToday} />
            <HeadlineStat icon="date_range" label="Last 7 days" value={stats.viewsThisWeek} />
            <HeadlineStat icon="calendar_month" label="Last 30 days" value={stats.viewsThisMonth} />
            <HeadlineStat icon="event" label="Last 12 months" value={stats.viewsThisYear} />
          </div>

          {/* ──── Pie charts: by source + by sector ──── */}
          <div className="mt-6 grid gap-4 lg:grid-cols-2">
            <ChartCard title="Views by source" subtitle="Which platform the viewed jobs came from">
              <PieChart slices={toSlices(stats.bySource)} size={190} />
            </ChartCard>
            <ChartCard title="Views by sector" subtitle="Which sectors attract the most views">
              <PieChart slices={toSlices(stats.bySector)} size={190} />
            </ChartCard>
          </div>

          {/* ──── Bar charts: per day + per month ──── */}
          <div className="mt-4 grid gap-4 lg:grid-cols-2">
            <ChartCard title="Views per day" subtitle="Last 30 days">
              <BarChart
                data={stats.perDay.map((d) => ({ label: d.date.slice(5), value: d.views }))}
                height={180}
              />
            </ChartCard>
            <ChartCard title="Views per month" subtitle="Last 12 months">
              <BarChart
                data={stats.perMonth.map((m) => ({ label: m.name, value: m.value }))}
                height={180}
                color="#00696E"
              />
            </ChartCard>
          </div>

          {/* ──── Top jobs by views ──── */}
          <ChartCard
            title="Top 10 jobs by views"
            subtitle="All-time view leaders"
            className="mt-4"
          >
            {stats.topJobs.length === 0 ? (
              <p className="py-6 text-center font-body-md text-body-md text-on-surface-variant">
                No views recorded yet.
              </p>
            ) : (
              <div className="overflow-hidden rounded-lg border border-surface-variant">
                <table className="w-full">
                  <thead>
                    <tr className="border-b border-surface-variant bg-surface-container-low">
                      <th className="px-4 py-2.5 text-left font-label-sm text-label-sm font-semibold text-on-surface">#</th>
                      <th className="px-4 py-2.5 text-left font-label-sm text-label-sm font-semibold text-on-surface">Job</th>
                      <th className="px-4 py-2.5 text-left font-label-sm text-label-sm font-semibold text-on-surface">Source</th>
                      <th className="px-4 py-2.5 text-right font-label-sm text-label-sm font-semibold text-on-surface">Views</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-surface-variant">
                    {stats.topJobs.map((job, idx) => (
                      <tr key={job.id} className="transition-colors hover:bg-surface-container-low/50">
                        <td className="px-4 py-2.5 font-label-sm text-label-sm text-on-surface-variant">{idx + 1}</td>
                        <td className="px-4 py-2.5">
                          <a
                            href={`/dashboard/admin/jobs/${job.id}`}
                            className="block max-w-sm truncate font-body-sm text-sm font-medium text-primary hover:underline"
                          >
                            {job.title}
                          </a>
                          <span className="block max-w-sm truncate font-label-xs text-[11px] text-on-surface-variant">
                            {job.company || 'No company'}
                            {job.sectorName ? ` · ${job.sectorName}` : ''}
                          </span>
                        </td>
                        <td className="px-4 py-2.5 font-label-sm text-label-sm text-on-surface-variant">
                          {job.sourceName || 'SeraGo'}
                        </td>
                        <td className="px-4 py-2.5 text-right">
                          <span className="inline-flex items-center gap-1 font-headline-sm font-bold text-on-surface">
                            <span className="material-symbols-outlined text-base">visibility</span>
                            {job.viewCount.toLocaleString()}
                          </span>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </ChartCard>
        </>
      ) : null}
    </DashboardShell>
  )
}

function HeadlineStat({ icon, label, value, accent }: {
  icon: string; label: string; value: number; accent?: string
}) {
  return (
    <div className="flex items-center gap-3 rounded-xl border border-surface-variant bg-surface-container-lowest p-4 shadow-sm">
      <div className="flex h-10 w-10 items-center justify-center rounded-lg bg-primary-container/30">
        <span className="material-symbols-outlined text-xl text-primary">{icon}</span>
      </div>
      <div>
        <p className={`font-headline-md text-headline-md font-bold ${accent ?? 'text-on-surface'}`}>{value.toLocaleString()}</p>
        <p className="font-label-xs text-label-xs text-on-surface-variant">{label}</p>
      </div>
    </div>
  )
}

function ChartCard({ title, subtitle, children, className }: {
  title: string; subtitle?: string; children: React.ReactNode; className?: string
}) {
  return (
    <div className={`rounded-xl border border-surface-variant bg-surface-container-lowest p-5 shadow-sm ${className ?? ''}`}>
      <h2 className="font-headline-sm text-headline-sm font-bold text-on-surface">{title}</h2>
      {subtitle && <p className="mt-0.5 font-label-sm text-label-sm text-on-surface-variant">{subtitle}</p>}
      <div className="mt-4">{children}</div>
    </div>
  )
}
