import { useCallback, useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import {
  fetchAdminStatsOverview,
  fetchAdminUsers,
  getApiErrorMessage,
  getStoredAuthTokens,
  updateUserRole,
  updateUserStatus,
} from '../../../api'
import { useRequireRole } from '../../../hooks'
import type { AdminStatsOverviewResponse, AdminUserResponse } from '../../../types'
import DashboardShell from '../../../components/dashboard/DashboardShell.tsx'
import { cn } from '../../../lib/cn.ts'

const ROLE_OPTIONS = ['Talent', 'Admin']
const ROLE_COLORS: Record<string, string> = {
  Talent: 'bg-blue-100 text-blue-800',
  Admin: 'bg-purple-100 text-purple-800',
}

export default function UsersPage() {
  const auth = useRequireRole('Admin')
  const navigate = useNavigate()
  const [users, setUsers] = useState<AdminUserResponse[]>([])
  const [stats, setStats] = useState<AdminStatsOverviewResponse['users'] | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [page, setPage] = useState(1)
  const [totalPages, setTotalPages] = useState(0)
  const [hasNext, setHasNext] = useState(false)
  const [search, setSearch] = useState('')
  const [roleFilter, setRoleFilter] = useState('')
  const [activeFilter, setActiveFilter] = useState<boolean | undefined>(undefined)
  const [pageSize] = useState(20)

  const load = useCallback(async () => {
    const tokens = getStoredAuthTokens()
    if (!tokens) return
    setLoading(true)
    setError(null)
    try {
      const [usersData, statsData] = await Promise.allSettled([
        fetchAdminUsers({ q: search || undefined, role: roleFilter || undefined, isActive: activeFilter, page, pageSize }, tokens.accessToken),
        fetchAdminStatsOverview(tokens.accessToken),
      ])
      if (usersData.status === 'fulfilled') {
        setUsers(usersData.value.items)
        setTotalPages(usersData.value.totalPages)
        setHasNext(usersData.value.hasNextPage)
      }
      if (statsData.status === 'fulfilled') {
        setStats(statsData.value.users)
      }
    } catch (err) {
      setError(getApiErrorMessage(err))
    } finally {
      setLoading(false)
    }
  }, [search, roleFilter, activeFilter, page, pageSize])

  useEffect(() => { load() }, [load])

  const handleRoleChange = async (user: AdminUserResponse, newRole: string) => {
    const tokens = getStoredAuthTokens()
    if (!tokens) return
    try {
      await updateUserRole(user.id, newRole, tokens.accessToken)
      setUsers((prev) => prev.map((u) => u.id === user.id ? { ...u, userType: newRole } : u))
    } catch (err) {
      setError(getApiErrorMessage(err))
    }
  }

  const handleToggleActive = async (user: AdminUserResponse) => {
    const tokens = getStoredAuthTokens()
    if (!tokens) return
    try {
      const updated = await updateUserStatus(user.id, !user.isActive, tokens.accessToken)
      setUsers((prev) => prev.map((u) => u.id === user.id ? updated : u))
    } catch (err) {
      setError(getApiErrorMessage(err))
    }
  }

  if (auth.status !== 'authenticated') {
    return (
      <div className="flex min-h-[50svh] items-center justify-center">
        <span aria-hidden="true" className="h-8 w-8 animate-spin rounded-full border-2 border-primary/30 border-t-primary" />
      </div>
    )
  }

  return (
    <DashboardShell role="Admin" authUser={auth.user}>
      <h1 className="font-headline-lg text-headline-lg font-bold tracking-tight text-primary">Users</h1>
      <p className="mt-1 font-body-md text-body-md text-on-surface-variant">
        Manage all registered users.
      </p>

      {/*
        Stat cards — three rows of three (on sm+):
          row 1  who is active right now / this week / this month
          row 2  cumulative totals (all users, and the two real roles)
          row 3  new sign-ups in the same periods
        Admin is deliberately omitted — there is only ever one admin account.
      */}
      {stats && (
        <div className="mt-5 grid grid-cols-2 gap-3 sm:grid-cols-3">
          {/* Row 1 — activity */}
          <StatCard icon="play_arrow" label="Active today" value={stats.activeToday} accent="text-primary" />
          <StatCard icon="date_range" label="Active this week" value={stats.activeThisWeek} accent="text-accent" />
          <StatCard icon="calendar_month" label="Active this month" value={stats.activeThisMonth} accent="text-accent" />

          {/* Row 2 — totals */}
          <StatCard icon="group" label="Total users" value={stats.total} accent="text-on-surface" />
          <StatCard icon="person" label="Total talent" value={stats.byRole.talent} accent="text-blue-600" />
          <StatCard icon="admin_panel_settings" label="Total admin" value={stats.byRole.admin} accent="text-purple-600" />

          {/* Row 3 — new sign-ups */}
          <StatCard icon="today" label="New today" value={stats.newToday} accent="text-accent" />
          <StatCard icon="date_range" label="New this week" value={stats.newThisWeek} accent="text-primary" />
          <StatCard icon="calendar_month" label="New this month" value={stats.newThisMonth} accent="text-accent" />
        </div>
      )}

      {/* Filters */}
      <div className="mt-5 flex flex-col gap-3 rounded-xl border border-surface-variant bg-surface-container-lowest p-4 shadow-sm sm:flex-row sm:items-center">
        <div className="relative flex-1">
          <span className="material-symbols-outlined absolute left-3 top-1/2 -translate-y-1/2 text-lg text-on-surface-variant">search</span>
          <input
            type="search"
            value={search}
            onChange={(e) => { setSearch(e.target.value); setPage(1) }}
            placeholder="Search by name or email..."
            className="w-full rounded-lg border border-outline-variant bg-surface-container-lowest py-2 pl-10 pr-4 font-body-md text-body-md text-on-surface placeholder:text-on-surface-variant/60 focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary"
          />
        </div>
        <select
          value={roleFilter}
          onChange={(e) => { setRoleFilter(e.target.value); setPage(1) }}
          className="rounded-lg border border-outline-variant bg-surface-container-lowest px-3 py-2 font-label-md text-label-md text-on-surface focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary"
        >
          <option value="">All roles</option>
          {ROLE_OPTIONS.map((r) => <option key={r} value={r}>{r}</option>)}
        </select>
        <select
          value={activeFilter === undefined ? '' : String(activeFilter)}
          onChange={(e) => { setActiveFilter(e.target.value === '' ? undefined : e.target.value === 'true'); setPage(1) }}
          className="rounded-lg border border-outline-variant bg-surface-container-lowest px-3 py-2 font-label-md text-label-md text-on-surface focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary"
        >
          <option value="">All statuses</option>
          <option value="true">Active</option>
          <option value="false">Inactive</option>
        </select>
      </div>

      {error && (
        <div role="alert" className="mt-4 rounded-xl border border-error/30 bg-error-container px-4 py-3 font-label-md text-label-md text-on-error-container">
          {error}
        </div>
      )}

      {/* User table */}
      {loading ? (
        <div className="mt-10 flex items-center justify-center">
          <span aria-hidden="true" className="h-8 w-8 animate-spin rounded-full border-2 border-primary/30 border-t-primary" />
        </div>
      ) : users.length === 0 ? (
        <div className="mt-10 text-center font-body-md text-body-md text-on-surface-variant">No users found.</div>
      ) : (
        <div className="mt-5 overflow-hidden rounded-xl border border-surface-variant bg-surface-container-lowest shadow-sm">
          <table className="w-full">
            <thead>
              <tr className="border-b border-surface-variant bg-surface-container-low">
                <th className="px-5 py-3 text-left font-label-md text-label-md font-semibold text-on-surface">User</th>
                <th className="px-5 py-3 text-left font-label-md text-label-md font-semibold text-on-surface">Role</th>
                <th className="px-5 py-3 text-left font-label-md text-label-md font-semibold text-on-surface">Status</th>
                <th className="px-5 py-3 text-left font-label-md text-label-md font-semibold text-on-surface">Joined</th>
                <th className="px-5 py-3 text-right font-label-md text-label-md font-semibold text-on-surface">Actions</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-surface-variant">
              {users.map((user) => (
                <tr key={user.id} className="cursor-pointer transition-colors hover:bg-surface-container-low/50" onClick={() => navigate(`/dashboard/admin/users/${user.id}`)}>
                  <td className="px-5 py-4">
                    <div className="flex items-center gap-3">
                      {user.avatarUrl ? (
                        <img src={user.avatarUrl} alt="" className="h-9 w-9 rounded-full object-cover" />
                      ) : (
                        <div className="flex h-9 w-9 items-center justify-center rounded-full bg-primary-container/40 font-label-md text-label-md font-medium text-primary">
                          {user.firstName[0]}{user.lastName[0]}
                        </div>
                      )}
                      <div className="min-w-0">
                        <p className="truncate font-body-md text-body-md font-medium text-on-surface">
                          {user.firstName} {user.middleName ? `${user.middleName} ` : ''}{user.lastName}
                        </p>
                        <p className="truncate font-label-sm text-label-sm text-on-surface-variant">{user.email}</p>
                      </div>
                    </div>
                  </td>
                  <td className="px-5 py-4">
                    <select
                      value={user.userType}
                      onChange={(e) => { e.stopPropagation(); handleRoleChange(user, e.target.value) }}
                      className={cn(
                        'rounded-full px-3 py-1 font-label-sm text-label-sm font-medium border-0 focus:ring-1 focus:ring-primary',
                        ROLE_COLORS[user.userType] ?? 'bg-gray-100 text-gray-800',
                      )}
                    >
                      {ROLE_OPTIONS.map((r) => <option key={r} value={r}>{r}</option>)}
                    </select>
                  </td>
                  <td className="px-5 py-4">
                    <span className={cn(
                      'inline-flex items-center rounded-full px-2.5 py-0.5 font-label-sm text-label-sm font-medium',
                      user.isActive ? 'bg-success-container text-on-success-container' : 'bg-error-container text-on-error-container',
                    )}>
                      {user.isActive ? 'Active' : 'Inactive'}
                    </span>
                  </td>
                  <td className="px-5 py-4 font-body-sm text-sm text-on-surface-variant">
                    {new Date(user.createdAt).toLocaleDateString()}
                  </td>
                  <td className="px-5 py-4 text-right">
                    <button
                      type="button"
                      onClick={(e) => { e.stopPropagation(); handleToggleActive(user) }}
                      className={cn(
                        'rounded-lg px-3 py-1.5 font-label-sm text-label-sm font-medium transition-colors',
                        user.isActive
                          ? 'text-error hover:bg-error-container/30'
                          : 'text-success hover:bg-success-container/30',
                      )}
                    >
                      {user.isActive ? 'Deactivate' : 'Activate'}
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {/* Pagination */}
      {totalPages > 1 && (
        <div className="mt-6 flex items-center justify-center gap-2">
          <button type="button" disabled={page <= 1} onClick={() => setPage((p) => Math.max(1, p - 1))}
            className="flex h-9 w-9 items-center justify-center rounded-lg border border-outline-variant text-on-surface-variant transition-colors hover:bg-surface-container-low disabled:pointer-events-none disabled:opacity-40">
            <span className="material-symbols-outlined text-lg">chevron_left</span>
          </button>
          <span className="font-label-md text-label-md text-on-surface-variant">
            Page {page} of {totalPages}
          </span>
          <button type="button" disabled={!hasNext} onClick={() => setPage((p) => p + 1)}
            className="flex h-9 w-9 items-center justify-center rounded-lg border border-outline-variant text-on-surface-variant transition-colors hover:bg-surface-container-low disabled:pointer-events-none disabled:opacity-40">
            <span className="material-symbols-outlined text-lg">chevron_right</span>
          </button>
        </div>
      )}
    </DashboardShell>
  )
}

function StatCard({ icon, label, value, accent }: {
  icon: string; label: string; value: number; accent: string
}) {
  return (
    <div className="flex items-center gap-3 rounded-xl border border-surface-variant bg-surface-container-lowest p-4 shadow-sm">
      <div className="flex h-10 w-10 items-center justify-center rounded-lg bg-primary-container/30">
        <span className="material-symbols-outlined text-xl text-primary">{icon}</span>
      </div>
      <div>
        <p className={cn('font-headline-md text-headline-md font-bold', accent)}>{value.toLocaleString()}</p>
        <p className="font-label-xs text-label-xs text-on-surface-variant">{label}</p>
      </div>
    </div>
  )
}
