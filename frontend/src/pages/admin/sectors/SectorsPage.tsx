import { useCallback, useEffect, useState } from 'react'
import {
  addSectorAliases,
  createSector,
  deleteSector,
  fetchAdminSectors,
  fetchJobs,
  getApiErrorMessage,
  getStoredAuthTokens,
  removeSectorAlias,
  setJobSector,
  classifyJobs,
  syncScrapedJobs,
  updateSector,
} from '../../../api'
import { useRequireRole } from '../../../hooks'
import type { JobResponse, SectorDetailResponse, SyncScrapedJobsResult } from '../../../types'
import DashboardShell from '../../../components/dashboard/DashboardShell.tsx'
import { cn } from '../../../lib/cn.ts'

type Tab = 'sectors' | 'aliases' | 'review'

/**
 * Admin console for the sector vocabulary: manage canonical sectors and the
 * alias map that standardizes scraped names, run the scraped-job sync, and
 * review/retag jobs the normalizer couldn't categorize.
 */
export default function SectorsPage() {
  const auth = useRequireRole('Admin')
  const [tab, setTab] = useState<Tab>('sectors')

  if (auth.status !== 'authenticated') {
    return (
      <div className="flex min-h-[50svh] items-center justify-center">
        <span
          aria-hidden="true"
          className="h-8 w-8 animate-spin rounded-full border-2 border-primary/30 border-t-primary"
        />
      </div>
    )
  }

  return (
    <DashboardShell role="Admin" authUser={auth.user}>
      <h1 className="font-headline-lg text-headline-lg font-bold tracking-tight text-primary">
        Sectors
      </h1>
      <p className="mt-1 font-body-md text-body-md text-on-surface-variant">
        The canonical job-sector vocabulary. Every scraped site's sector names
        are standardized against this list.
      </p>

      <div className="mt-6 flex gap-1 rounded-lg border border-surface-variant bg-surface-container-lowest p-1 shadow-sm sm:w-fit">
        {(
          [
            { id: 'sectors', label: 'Sectors' },
            { id: 'aliases', label: 'Aliases' },
            { id: 'review', label: 'Review & sync' },
          ] as { id: Tab; label: string }[]
        ).map((t) => (
          <button
            key={t.id}
            type="button"
            onClick={() => setTab(t.id)}
            aria-pressed={tab === t.id}
            className={cn(
              'rounded-md px-4 py-2 font-label-md text-label-md transition-colors',
              tab === t.id
                ? 'bg-primary font-medium text-on-primary'
                : 'text-on-surface-variant hover:text-on-surface',
            )}
          >
            {t.label}
          </button>
        ))}
      </div>

      <div className="mt-6">
        {tab === 'sectors' && <SectorsTab />}
        {tab === 'aliases' && <AliasesTab />}
        {tab === 'review' && <ReviewTab />}
      </div>
    </DashboardShell>
  )
}

/* ------------------------------------------------------------------ Hooks */

function useAdminToken() {
  const tokens = getStoredAuthTokens()
  return tokens?.accessToken ?? null
}

function useSectors() {
  const token = useAdminToken()
  const [sectors, setSectors] = useState<SectorDetailResponse[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  const reload = useCallback(async () => {
    if (!token) return
    setLoading(true)
    setError(null)
    try {
      setSectors(await fetchAdminSectors(token))
    } catch (err) {
      setError(getApiErrorMessage(err))
    } finally {
      setLoading(false)
    }
  }, [token])

  useEffect(() => {
    reload()
  }, [reload])

  return { sectors, setSectors, loading, error, reload }
}

/* -------------------------------------------------------------- Sectors */

function SectorsTab() {
  const token = useAdminToken()
  const { sectors, setSectors, loading, error, reload } = useSectors()
  const [name, setName] = useState('')
  const [busy, setBusy] = useState(false)
  const [actionError, setActionError] = useState<string | null>(null)
  // Which row is being renamed + its draft value.
  const [editingId, setEditingId] = useState<string | null>(null)
  const [editName, setEditName] = useState('')

  const add = async () => {
    if (!token || !name.trim()) return
    setBusy(true)
    setActionError(null)
    try {
      await createSector({ name: name.trim() }, token)
      setName('')
      await reload()
    } catch (err) {
      setActionError(getApiErrorMessage(err))
    } finally {
      setBusy(false)
    }
  }

  const toggleActive = async (sector: SectorDetailResponse) => {
    if (!token) return
    try {
      await updateSector(sector.id, { isActive: !sector.isActive }, token)
      setSectors((prev) =>
        prev.map((s) =>
          s.id === sector.id ? { ...s, isActive: !sector.isActive } : s,
        ),
      )
    } catch (err) {
      setActionError(getApiErrorMessage(err))
    }
  }

  const startEdit = (sector: SectorDetailResponse) => {
    setEditingId(sector.id)
    setEditName(sector.name)
  }

  const saveEdit = async (sector: SectorDetailResponse) => {
    if (!token || !editName.trim()) return
    try {
      await updateSector(sector.id, { name: editName.trim() }, token)
      setSectors((prev) =>
        prev.map((s) => (s.id === sector.id ? { ...s, name: editName.trim() } : s)),
      )
      setEditingId(null)
    } catch (err) {
      setActionError(getApiErrorMessage(err))
    }
  }

  const remove = async (sector: SectorDetailResponse) => {
    if (!token) return
    setActionError(null)
    try {
      await deleteSector(sector.id, token)
      setSectors((prev) => prev.filter((s) => s.id !== sector.id))
    } catch (err) {
      setActionError(getApiErrorMessage(err))
    }
  }

  return (
    <div className="space-y-6">
      {actionError && (
        <div
          role="alert"
          className="rounded-xl border border-error/30 bg-error-container px-4 py-3 font-label-md text-label-md text-on-error-container"
        >
          {actionError}
        </div>
      )}

      {/* Add sector */}
      <div className="flex flex-col gap-3 rounded-xl border border-surface-variant bg-surface-container-lowest p-4 shadow-sm sm:flex-row">
        <input
          type="text"
          value={name}
          onChange={(e) => {
            if (e.target.value.length <= 120) setName(e.target.value)
          }}
          onKeyDown={(e) => e.key === 'Enter' && add()}
          placeholder="New sector name, e.g. Government & Public Service"
          maxLength={120}
          className="w-full flex-1 rounded-lg border border-outline-variant bg-surface-container-lowest px-3.5 py-2.5 font-body-md text-body-md text-on-surface transition-colors placeholder:text-on-surface-variant/60 focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary"
        />
        <button
          type="button"
          onClick={add}
          disabled={busy || !name.trim()}
          className="rounded-lg bg-primary px-6 py-2.5 font-label-md text-label-md font-medium text-on-primary transition-opacity hover:opacity-90 disabled:pointer-events-none disabled:opacity-60"
        >
          Add sector
        </button>
      </div>

      {/* Sector list */}
      {loading ? (
        <div className="flex items-center justify-center py-16">
          <span
            aria-hidden="true"
            className="h-8 w-8 animate-spin rounded-full border-2 border-primary/30 border-t-primary"
          />
        </div>
      ) : error ? (
        <p className="font-body-md text-body-md text-error">{error}</p>
      ) : sectors.length === 0 ? (
        <p className="font-body-md text-body-md text-on-surface-variant">
          No sectors yet — add the first one above.
        </p>
      ) : (
        <ul className="divide-y divide-surface-variant overflow-hidden rounded-xl border border-surface-variant bg-surface-container-lowest shadow-sm">
          {sectors.map((sector) => (
            <li
              key={sector.id}
              className="flex flex-wrap items-center gap-3 px-5 py-4"
            >
              {editingId === sector.id ? (
                <input
                  type="text"
                  value={editName}
                  onChange={(e) => {
                    if (e.target.value.length <= 120) setEditName(e.target.value)
                  }}
                  onKeyDown={(e) => e.key === 'Enter' && saveEdit(sector)}
                  maxLength={120}
                  autoFocus
                  className="w-56 rounded-lg border border-outline-variant bg-surface-container-lowest px-3 py-1.5 font-body-md text-body-md text-on-surface focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary"
                />
              ) : (
                <span
                  className={cn(
                    'min-w-0 flex-1 font-body-md text-body-md font-medium',
                    !sector.isActive && 'text-on-surface-variant line-through',
                  )}
                >
                  {sector.name}
                </span>
              )}

              <span className="rounded-full bg-surface-container-low px-2.5 py-0.5 font-label-sm text-label-sm text-on-surface-variant">
                {sector.jobCount} job{sector.jobCount === 1 ? '' : 's'}
              </span>

              <div className="ml-auto flex items-center gap-2">
                {editingId === sector.id ? (
                  <>
                    <button
                      type="button"
                      onClick={() => saveEdit(sector)}
                      className="rounded-lg bg-primary px-3 py-1.5 font-label-md text-label-md font-medium text-on-primary transition-opacity hover:opacity-90"
                    >
                      Save
                    </button>
                    <button
                      type="button"
                      onClick={() => setEditingId(null)}
                      className="rounded-lg border border-outline-variant px-3 py-1.5 font-label-md text-label-md text-on-surface-variant transition-colors hover:bg-surface-container-low"
                    >
                      Cancel
                    </button>
                  </>
                ) : (
                  <button
                    type="button"
                    onClick={() => startEdit(sector)}
                    title="Rename"
                    aria-label={`Rename ${sector.name}`}
                    className="flex h-9 w-9 items-center justify-center rounded-lg text-on-surface-variant transition-colors hover:bg-surface-container-low hover:text-primary"
                  >
                    <span className="material-symbols-outlined text-lg">edit</span>
                  </button>
                )}

                {/* Active toggle */}
                <button
                  type="button"
                  role="switch"
                  aria-checked={sector.isActive}
                  aria-label={sector.isActive ? 'Deactivate' : 'Activate'}
                  onClick={() => toggleActive(sector)}
                  className={cn(
                    'relative h-6 w-11 rounded-full transition-colors',
                    sector.isActive ? 'bg-primary' : 'bg-outline-variant',
                  )}
                >
                  <span
                    className={cn(
                      'absolute top-0.5 h-5 w-5 rounded-full bg-on-primary shadow transition-all',
                      sector.isActive ? 'left-[22px]' : 'left-0.5',
                    )}
                  />
                </button>

                <button
                  type="button"
                  onClick={() => remove(sector)}
                  disabled={sector.jobCount > 0}
                  title={
                    sector.jobCount > 0
                      ? 'Deactivate instead — this sector has jobs'
                      : 'Delete'
                  }
                  aria-label={`Delete ${sector.name}`}
                  className="flex h-9 w-9 items-center justify-center rounded-lg text-on-surface-variant transition-colors hover:bg-error-container/50 hover:text-error disabled:pointer-events-none disabled:opacity-30"
                >
                  <span className="material-symbols-outlined text-lg">delete</span>
                </button>
              </div>
            </li>
          ))}
        </ul>
      )}
    </div>
  )
}

/* -------------------------------------------------------------- Aliases */

function AliasesTab() {
  const token = useAdminToken()
  const { sectors, setSectors, loading, error } = useSectors()
  const [selectedId, setSelectedId] = useState<string>('')
  const [input, setInput] = useState('')
  const [busy, setBusy] = useState(false)
  const [actionError, setActionError] = useState<string | null>(null)

  // Default the selection to the first sector once loaded.
  useEffect(() => {
    if (!selectedId && sectors.length > 0) setSelectedId(sectors[0].id)
  }, [sectors, selectedId])

  const selected = sectors.find((s) => s.id === selectedId) ?? null

  const addAliases = async () => {
    if (!token || !selected || !input.trim()) return
    setBusy(true)
    setActionError(null)
    try {
      const added = await addSectorAliases(
        selected.id,
        input.split(',').map((a) => a.trim()).filter(Boolean),
        token,
      )
      setSectors((prev) =>
        prev.map((s) =>
          s.id === selected.id
            ? {
                ...s,
                aliases: [
                  ...s.aliases.filter(
                    (a) => !added.some((n) => n.alias === a.alias),
                  ),
                  ...added,
                ],
              }
            : s,
        ),
      )
      setInput('')
    } catch (err) {
      setActionError(getApiErrorMessage(err))
    } finally {
      setBusy(false)
    }
  }

  const removeAlias = async (aliasId: string) => {
    if (!token || !selected) return
    setActionError(null)
    try {
      await removeSectorAlias(selected.id, aliasId, token)
      setSectors((prev) =>
        prev.map((s) =>
          s.id === selected.id
            ? { ...s, aliases: s.aliases.filter((a) => a.id !== aliasId) }
            : s,
        ),
      )
    } catch (err) {
      setActionError(getApiErrorMessage(err))
    }
  }

  return (
    <div className="space-y-6">
      <p className="font-body-md text-body-md text-on-surface-variant">
        Every raw name a source uses for a sector maps here. When the sync
        imports a job it looks up its sector name in this table — add aliases as
        new sites appear, no code changes needed.
      </p>

      {actionError && (
        <div
          role="alert"
          className="rounded-xl border border-error/30 bg-error-container px-4 py-3 font-label-md text-label-md text-on-error-container"
        >
          {actionError}
        </div>
      )}

      {loading ? (
        <div className="flex items-center justify-center py-16">
          <span
            aria-hidden="true"
            className="h-8 w-8 animate-spin rounded-full border-2 border-primary/30 border-t-primary"
          />
        </div>
      ) : error ? (
        <p className="font-body-md text-body-md text-error">{error}</p>
      ) : (
        <>
          <select
            value={selectedId}
            onChange={(e) => setSelectedId(e.target.value)}
            aria-label="Sector"
            className="w-full max-w-sm rounded-lg border border-outline-variant bg-surface-container-lowest px-3 py-2.5 font-body-md text-body-md text-on-surface focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary"
          >
            {sectors.map((s) => (
              <option key={s.id} value={s.id}>
                {s.name}
              </option>
            ))}
          </select>

          {selected && (
            <div className="rounded-xl border border-surface-variant bg-surface-container-lowest p-5 shadow-sm">
              <h3 className="font-label-md text-label-md font-semibold text-on-surface">
                {selected.name} — {selected.aliases.length} alias
                {selected.aliases.length === 1 ? '' : 'es'}
              </h3>
              {selected.aliases.length > 0 && (
                <div className="mt-4 flex flex-wrap gap-2">
                  {selected.aliases.map((alias) => (
                    <span
                      key={alias.id}
                      className="inline-flex items-center gap-1.5 rounded-md bg-surface-container-low px-2.5 py-1 font-label-sm text-label-sm text-on-surface"
                    >
                      {alias.alias}
                      <button
                        type="button"
                        onClick={() => removeAlias(alias.id)}
                        aria-label={`Remove alias ${alias.alias}`}
                        className="text-on-surface-variant transition-colors hover:text-error"
                      >
                        <span className="material-symbols-outlined text-sm">
                          close
                        </span>
                      </button>
                    </span>
                  ))}
                </div>
              )}
              <div className="mt-4 flex flex-col gap-3 sm:flex-row">
                <input
                  type="text"
                  value={input}
                  onChange={(e) => {
                    if (e.target.value.length <= 500) setInput(e.target.value)
                  }}
                  onKeyDown={(e) => e.key === 'Enter' && addAliases()}
                  placeholder="Add aliases, comma-separated — e.g. ICT, Information Technology"
                  maxLength={500}
                  className="w-full flex-1 rounded-lg border border-outline-variant bg-surface-container-lowest px-3.5 py-2 font-body-md text-body-md text-on-surface transition-colors placeholder:text-on-surface-variant/60 focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary"
                />
                <button
                  type="button"
                  onClick={addAliases}
                  disabled={busy || !input.trim()}
                  className="rounded-lg bg-primary px-6 py-2 font-label-md text-label-md font-medium text-on-primary transition-opacity hover:opacity-90 disabled:pointer-events-none disabled:opacity-60"
                >
                  Add
                </button>
              </div>
            </div>
          )}
        </>
      )}
    </div>
  )
}

/* ------------------------------------------------------- Review & sync */

function ReviewTab() {
  const token = useAdminToken()
  const { sectors } = useSectors()
  const [syncResult, setSyncResult] = useState<SyncScrapedJobsResult | null>(null)
  const [syncing, setSyncing] = useState(false)
  const [uncategorized, setUncategorized] = useState<JobResponse[]>([])
  const [reviewLoading, setReviewLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [assignments, setAssignments] = useState<Record<string, string>>({})
  const [refreshKey, setRefreshKey] = useState(0)
  
  const [classifying, setClassifying] = useState(false)
  const [aiSuggestions, setAiSuggestions] = useState<Record<string, any[]>>({})

  const runAiClassification = async () => {
    if (!token || classifying || uncategorized.length === 0) return
    setClassifying(true)
    setError(null)
    try {
      const jobIds = uncategorized.map((j) => j.id)
      const res = await classifyJobs(jobIds, token)
      
      // The API returns a 'results' array
      if (res && res.results) {
        const newSuggestions: Record<string, any[]> = { ...aiSuggestions }
        
        res.results.forEach((r: any) => {
          if (r.uncategorized && r.suggestedSectors) {
            newSuggestions[r.jobId] = r.suggestedSectors
          }
        })
        
        setAiSuggestions(newSuggestions)
        // Refresh the list to remove jobs that were auto-assigned!
        setRefreshKey((k) => k + 1)
      }
    } catch (err) {
      setError(getApiErrorMessage(err))
    } finally {
      setClassifying(false)
    }
  }

  const loadUncategorized = useCallback(async () => {
    if (!token) return
    setReviewLoading(true)
    setError(null)
    try {
      const data = await fetchJobs({ uncategorized: true, pageSize: 50 }, token)
      setUncategorized(data.items)
    } catch (err) {
      setError(getApiErrorMessage(err))
    } finally {
      setReviewLoading(false)
    }
  }, [token])

  useEffect(() => {
    loadUncategorized()
  }, [loadUncategorized, refreshKey])

  const runSync = async () => {
    if (!token || syncing) return
    setSyncing(true)
    setError(null)
    try {
      setSyncResult(await syncScrapedJobs(token))
      // Re-list the review queue after importing.
      setRefreshKey((k) => k + 1)
    } catch (err) {
      setError(getApiErrorMessage(err))
    } finally {
      setSyncing(false)
    }
  }

  const assign = async (job: JobResponse) => {
    if (!token) return
    const sectorId = assignments[job.id]
    setError(null)
    try {
      await setJobSector(job.id, sectorId || null, token)
      setUncategorized((prev) => prev.filter((j) => j.id !== job.id))
      setAssignments((prev) => {
        const next = { ...prev }
        delete next[job.id]
        return next
      })
    } catch (err) {
      setError(getApiErrorMessage(err))
    }
  }

  return (
    <div className="space-y-6">
      {/* Sync */}
      <div className="flex flex-col gap-4 rounded-xl border border-surface-variant bg-surface-container-lowest p-5 shadow-sm sm:flex-row sm:items-center sm:justify-between">
        <div>
          <h3 className="font-label-md text-label-md font-semibold text-on-surface">
            Import scraped jobs
          </h3>
          <p className="mt-0.5 font-label-sm text-label-sm text-on-surface-variant">
            Pulls published jobs from the scraper database, standardizes their
            sectors (alias map, then title classification), and upserts them.
            Safe to run repeatedly.
          </p>
        </div>
        <div className="flex items-center gap-3">
          <button
            type="button"
            onClick={runAiClassification}
            disabled={classifying || uncategorized.length === 0}
            className="inline-flex items-center justify-center gap-2 rounded-lg bg-surface-container-high px-6 py-3 font-label-md text-label-md font-medium text-on-surface transition-colors hover:bg-surface-container-highest disabled:pointer-events-none disabled:opacity-50"
          >
            {classifying ? (
              <>
                <span
                  aria-hidden="true"
                  className="h-4 w-4 animate-spin rounded-full border-2 border-on-surface/40 border-t-on-surface"
                />
                Classifying…
              </>
            ) : (
              'Classify with AI'
            )}
          </button>
          <button
            type="button"
            onClick={runSync}
            disabled={syncing}
            className="inline-flex items-center justify-center gap-2 rounded-lg bg-accent px-6 py-3 font-label-md text-label-md font-medium text-on-accent transition-opacity hover:opacity-90 disabled:pointer-events-none disabled:opacity-60"
          >
            {syncing ? (
              <>
                <span
                  aria-hidden="true"
                  className="h-4 w-4 animate-spin rounded-full border-2 border-on-accent/40 border-t-on-accent"
                />
                Syncing…
              </>
            ) : (
              'Sync scraped jobs'
            )}
          </button>
        </div>
      </div>

      {syncResult && (
        <div className="grid grid-cols-2 gap-3 sm:grid-cols-5">
          {[
            { label: 'Inserted', value: syncResult.inserted },
            { label: 'Updated', value: syncResult.updated },
            { label: 'Unchanged', value: syncResult.unchanged },
            { label: 'Uncategorized', value: syncResult.uncategorized },
            { label: 'Deactivated', value: syncResult.deactivated },
          ].map((stat) => (
            <div
              key={stat.label}
              className="rounded-xl border border-surface-variant bg-surface-container-lowest px-4 py-3 shadow-sm"
            >
              <p className="font-headline-md text-headline-md font-bold text-primary">
                {stat.value}
              </p>
              <p className="font-label-sm text-label-sm text-on-surface-variant">
                {stat.label}
              </p>
            </div>
          ))}
          {syncResult.unknownSectors.length > 0 && (
            <div className="col-span-2 rounded-xl border border-amber-200 bg-amber-50 px-4 py-3 sm:col-span-5">
              <p className="font-label-md text-label-md font-medium text-amber-900">
                Unknown sector names seen ({syncResult.unknownSectors.length}) —
                map them as aliases:
              </p>
              <p className="mt-1 flex flex-wrap gap-1.5">
                {syncResult.unknownSectors.map((name) => (
                  <span
                    key={name}
                    className="rounded-full bg-amber-100 px-2.5 py-0.5 font-label-sm text-label-sm text-amber-800"
                  >
                    {name}
                  </span>
                ))}
              </p>
            </div>
          )}
        </div>
      )}

      {error && (
        <div
          role="alert"
          className="rounded-xl border border-error/30 bg-error-container px-4 py-3 font-label-md text-label-md text-on-error-container"
        >
          {error}
        </div>
      )}

      {/* Uncategorized review queue */}
      <div className="rounded-xl border border-surface-variant bg-surface-container-lowest p-5 shadow-sm">
        <h3 className="font-label-md text-label-md font-semibold text-on-surface">
          Uncategorized jobs
        </h3>
        <p className="mt-0.5 font-label-sm text-label-sm text-on-surface-variant">
          Jobs the normalizer couldn't place — assign a sector or leave them
          for a future alias.
        </p>

        {reviewLoading ? (
          <div className="flex items-center justify-center py-10">
            <span
              aria-hidden="true"
              className="h-7 w-7 animate-spin rounded-full border-2 border-primary/30 border-t-primary"
            />
          </div>
        ) : uncategorized.length === 0 ? (
          <p className="mt-4 font-body-md text-body-md text-on-surface-variant">
            Nothing waiting — every job has a sector.
          </p>
        ) : (
          <ul className="mt-4 divide-y divide-surface-variant">
            {uncategorized.map((job) => (
              <li
                key={job.id}
                className="flex flex-wrap items-center gap-3 py-3"
              >
                <div className="min-w-0 flex-1">
                  <p className="truncate font-body-md text-body-md font-medium text-on-surface">
                    {job.title}
                  </p>
                  <p className="truncate font-label-sm text-label-sm text-on-surface-variant">
                    {job.company || 'Company undisclosed'}
                    {job.sourceName ? ` · via ${job.sourceName}` : ''}
                  </p>
                </div>
                <select
                  value={assignments[job.id] ?? ''}
                  onChange={(e) =>
                    setAssignments((prev) => ({
                      ...prev,
                      [job.id]: e.target.value,
                    }))
                  }
                  aria-label={`Assign sector to ${job.title}`}
                  className="max-w-[200px] truncate rounded-lg border border-outline-variant bg-surface-container-lowest px-3 py-1.5 font-body-md text-body-md text-on-surface focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary sm:max-w-none"
                >
                  <option value="">Uncategorized</option>
                  {sectors.map((s) => {
                    const isSuggested = aiSuggestions[job.id]?.some(
                      (ai) => ai.sectorId === s.id || ai.sectorSlug === s.slug
                    )
                    return (
                      <option key={s.id} value={s.id}>
                        {isSuggested ? `✨ ${s.name}` : s.name}
                      </option>
                    )
                  })}
                </select>
                <button
                  type="button"
                  onClick={() => assign(job)}
                  disabled={!assignments[job.id]}
                  className="rounded-lg bg-primary px-4 py-1.5 font-label-md text-label-md font-medium text-on-primary transition-opacity hover:opacity-90 disabled:pointer-events-none disabled:opacity-50"
                >
                  Assign
                </button>
              </li>
            ))}
          </ul>
        )}
      </div>
    </div>
  )
}
