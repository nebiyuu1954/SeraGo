import { useCallback, useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import {
  fetchAdminStats,
  fetchAdminUsers,
  fetchJobs,
  getApiErrorMessage,
  getStoredAuthTokens,
} from '../../../api'
import { useRequireRole } from '../../../hooks'
import type { AdminStatsTopResponse } from '../../../types'
import DashboardShell from '../../../components/dashboard/DashboardShell.tsx'


export default function AdminDashboardPage() {
  const auth = useRequireRole('Admin')
  const navigate = useNavigate()

  // Counts
  const [userCount, setUserCount] = useState<number | null>(null)
  const [jobCount, setJobCount] = useState<number | null>(null)
  const [stats, setStats] = useState<AdminStatsTopResponse | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  const load = useCallback(async () => {
    const tokens = getStoredAuthTokens()
    if (!tokens) return
    setLoading(true)
    setError(null)
    try {
      const [usersData, jobsData, statsData] = await Promise.allSettled([
        fetchAdminUsers({ pageSize: 1 }, tokens.accessToken),
        fetchJobs({ pageSize: 1 }, tokens.accessToken),
        fetchAdminStats('week', tokens.accessToken),
      ])
      if (usersData.status === 'fulfilled') setUserCount(usersData.value.totalCount)
      if (jobsData.status === 'fulfilled') setJobCount(jobsData.value.pagination.totalCount)
      if (statsData.status === 'fulfilled') setStats(statsData.value)
    } catch (err) {
      setError(getApiErrorMessage(err))
    } finally {
      setLoading(false)
    }
  }, [])

  useEffect(() => { load() }, [load])

  if (auth.status !== 'authenticated') {
    return (
      <div className="flex min-h-[50svh] items-center justify-center">
        <span aria-hidden="true" className="h-8 w-8 animate-spin rounded-full border-2 border-primary/30 border-t-primary" />
      </div>
    )
  }

  return (
    <DashboardShell role="Admin" authUser={auth.user}>
      <h1 className="font-headline-lg text-headline-lg font-bold tracking-tight text-primary">Admin Dashboard</h1>
      <p className="mt-1 font-body-md text-body-md text-on-surface-variant">
        Platform overview and scraper health.
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
          {/* Stat cards */}
          <div className="mt-6 grid grid-cols-1 gap-4 sm:grid-cols-3">
            <StatCard
              icon="group"
              label="Total users"
              value={userCount ?? '—'}
              onClick={() => navigate('/dashboard/admin/users')}
            />
            <StatCard
              icon="work"
              label="Total jobs"
              value={jobCount ?? '—'}
              onClick={() => navigate('/dashboard/admin/jobs')}
            />
            <StatCard
              icon="category"
              label="Sectors"
              value={stats?.topSectors.length ?? '—'}
              onClick={() => navigate('/dashboard/admin/sectors')}
            />
          </div>

          {/* Scraper health */}
          {stats && (
            <div className="mt-6 grid grid-cols-1 gap-6 lg:grid-cols-2">
              {/* Top websites */}
              <div className="rounded-xl border border-surface-variant bg-surface-container-lowest p-5 shadow-sm">
                <h2 className="font-label-md text-label-md font-semibold text-on-surface">
                  Top websites this week
                </h2>
                <p className="mt-0.5 font-label-sm text-label-sm text-on-surface-variant">
                  {stats.start} — {stats.end}
                </p>
                {stats.topWebsites.length === 0 ? (
                  <p className="mt-4 font-body-md text-body-md text-on-surface-variant">No data yet.</p>
                ) : (
                  <ul className="mt-4 space-y-3">
                    {stats.topWebsites.map((w: { slug: string; name: string; itemsFound: number; runCount: number }) => (
                      <li key={w.slug} className="flex items-center gap-3">
                        <span className="min-w-0 flex-1 truncate font-body-md text-body-md font-medium text-on-surface">{w.name}</span>
                        <span className="rounded-full bg-primary-container/40 px-2.5 py-0.5 font-label-sm text-label-sm font-medium text-primary">
                          {w.itemsFound.toLocaleString()} items
                        </span>
                        <span className="font-label-sm text-label-sm text-on-surface-variant">
                          {w.runCount} runs
                        </span>
                      </li>
                    ))}
                  </ul>
                )}
              </div>

              {/* Top sectors */}
              <div className="rounded-xl border border-surface-variant bg-surface-container-lowest p-5 shadow-sm">
                <h2 className="font-label-md text-label-md font-semibold text-on-surface">
                  Top sectors this week
                </h2>
                <p className="mt-0.5 font-label-sm text-label-sm text-on-surface-variant">
                  Jobs scraped per sector
                </p>
                {stats.topSectors.length === 0 ? (
                  <p className="mt-4 font-body-md text-body-md text-on-surface-variant">No data yet.</p>
                ) : (
                  <ul className="mt-4 space-y-3">
                    {stats.topSectors.map((s: { name: string; count: number }) => (
                      <li key={s.name} className="flex items-center gap-3">
                        <span className="min-w-0 flex-1 truncate font-body-md text-body-md font-medium text-on-surface">{s.name}</span>
                        <span className="rounded-full bg-accent-container/40 px-2.5 py-0.5 font-label-sm text-label-sm font-medium text-accent">
                          {s.count.toLocaleString()}
                        </span>
                      </li>
                    ))}
                  </ul>
                )}
              </div>
            </div>
          )}
        </>
      )}
    </DashboardShell>
  )
}

function StatCard({ icon, label, value, onClick }: { icon: string; label: string; value: number | string; onClick: () => void }) {
  return (
    <button
      type="button"
      onClick={onClick}
      className="flex items-center gap-4 rounded-xl border border-surface-variant bg-surface-container-lowest p-5 shadow-sm text-left transition-all hover:border-outline-variant hover:shadow-md"
    >
      <div className="flex h-12 w-12 items-center justify-center rounded-lg bg-primary-container/30">
        <span className="material-symbols-outlined text-2xl text-primary">{icon}</span>
      </div>
      <div>
        <p className="font-headline-lg text-headline-lg font-bold text-on-surface">{value}</p>
        <p className="font-label-sm text-label-sm text-on-surface-variant">{label}</p>
      </div>
    </button>
  )
}
