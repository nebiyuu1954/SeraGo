import { useCallback, useEffect, useState } from 'react'
import {
  fetchClassificationHistory,
  setJobSector,
  getApiErrorMessage,
} from '../../../api'
import type {
  ClassificationHistoryItem,
  ClassificationStats,
  SectorDetailResponse,
} from '../../../types'
import { cn } from '../../../lib/cn.ts'
import AiAuditModal from './AiAuditModal.tsx'

interface ClassificationHistoryTabProps {
  sectors: SectorDetailResponse[]
  token: string
}

export default function ClassificationHistoryTab({
  sectors,
  token,
}: ClassificationHistoryTabProps) {
  const [items, setItems] = useState<ClassificationHistoryItem[]>([])
  const [stats, setStats] = useState<ClassificationStats | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  // Filters & Pagination
  const [page, setPage] = useState(1)
  const [totalPages, setTotalPages] = useState(1)
  const [totalCount, setTotalCount] = useState(0)
  const [search, setSearch] = useState('')
  const [debouncedSearch, setDebouncedSearch] = useState('')
  const [selectedSectorId, setSelectedSectorId] = useState<string>('')
  const [selectedMethod, setSelectedMethod] = useState<string>('')
  const [todayOnly, setTodayOnly] = useState(false)

  // Selection
  const [selectedJobIds, setSelectedJobIds] = useState<Set<string>>(new Set())
  const [auditModalOpen, setAuditModalOpen] = useState(false)
  const [expandedDescIds, setExpandedDescIds] = useState<Set<string>>(new Set())

  // Inline assignment edits
  const [manualAssignments, setManualAssignments] = useState<Record<string, string>>({})
  const [savingJobId, setSavingJobId] = useState<string | null>(null)
  const [toastMessage, setToastMessage] = useState<string | null>(null)

  // Debounce search input
  useEffect(() => {
    const handler = setTimeout(() => {
      setDebouncedSearch(search)
      setPage(1)
    }, 350)
    return () => clearTimeout(handler)
  }, [search])

  const loadHistory = useCallback(async () => {
    if (!token) return
    setLoading(true)
    setError(null)
    try {
      const res = await fetchClassificationHistory(
        {
          page,
          pageSize: 20,
          sectorId: selectedSectorId || undefined,
          method: selectedMethod || undefined,
          todayOnly,
          search: debouncedSearch || undefined,
        },
        token,
      )
      setItems(res.items)
      setStats(res.stats)
      setTotalPages(res.totalPages)
      setTotalCount(res.totalCount)
    } catch (err) {
      setError(getApiErrorMessage(err))
    } finally {
      setLoading(false)
    }
  }, [debouncedSearch, page, selectedMethod, selectedSectorId, todayOnly, token])

  useEffect(() => {
    loadHistory()
  }, [loadHistory])

  // Select all visible toggle
  const allVisibleSelected =
    items.length > 0 && items.every((i) => selectedJobIds.has(i.id))

  const handleToggleSelectAllVisible = () => {
    if (allVisibleSelected) {
      const next = new Set(selectedJobIds)
      items.forEach((i) => next.delete(i.id))
      setSelectedJobIds(next)
    } else {
      const next = new Set(selectedJobIds)
      items.forEach((i) => next.add(i.id))
      setSelectedJobIds(next)
    }
  }

  // Toggle single item
  const handleToggleSelectItem = (id: string) => {
    const next = new Set(selectedJobIds)
    if (next.has(id)) {
      next.delete(id)
    } else {
      next.add(id)
    }
    setSelectedJobIds(next)
  }

  // Select Today's jobs
  const handleSelectTodayJobs = () => {
    const today = new Date().toISOString().slice(0, 10)
    const todayItems = items.filter((i) => i.createdAt.slice(0, 10) === today || i.updatedAt.slice(0, 10) === today)
    const next = new Set(selectedJobIds)
    todayItems.forEach((i) => next.add(i.id))
    setSelectedJobIds(next)
    setToastMessage(`Selected ${todayItems.length} jobs from today.`)
    setTimeout(() => setToastMessage(null), 3000)
  }

  // Description expand toggle
  const toggleExpandDesc = (id: string) => {
    setExpandedDescIds((prev) => {
      const next = new Set(prev)
      if (next.has(id)) next.delete(id)
      else next.add(id)
      return next
    })
  }

  // Inline Sector change
  const handleSaveSector = async (job: ClassificationHistoryItem) => {
    const newSectorId = manualAssignments[job.id]
    if (newSectorId === undefined || savingJobId) return
    setSavingJobId(job.id)
    try {
      await setJobSector(job.id, newSectorId || null, token)
      setItems((prev) =>
        prev.map((i) => {
          if (i.id === job.id) {
            const foundSec = sectors.find((s) => s.id === newSectorId)
            return {
              ...i,
              sectorId: newSectorId || null,
              sectorName: foundSec?.name ?? null,
              sectorSlug: foundSec?.slug ?? null,
              classificationMethod: 'AdminManual',
              classificationDetails: 'Manually assigned by admin',
            }
          }
          return i
        }),
      )
      setToastMessage(`Assigned sector for "${job.title}"`)
      setTimeout(() => setToastMessage(null), 4000)
    } catch (err) {
      setError(getApiErrorMessage(err))
    } finally {
      setSavingJobId(null)
    }
  }

  const selectedJobs = items.filter((i) => selectedJobIds.has(i.id))

  return (
    <div className="space-y-6">
      {/* 1. Header Metrics Stats */}
      {stats && (
        <div className="grid grid-cols-2 gap-3 sm:grid-cols-4">
          <div className="rounded-xl border border-surface-variant bg-surface-container-lowest px-4 py-3 shadow-sm">
            <p className="font-headline-md text-headline-md font-bold text-primary">
              {stats.totalJobs}
            </p>
            <p className="font-label-sm text-label-sm text-on-surface-variant">
              Total platform jobs
            </p>
          </div>

          <div className="rounded-xl border border-surface-variant bg-surface-container-lowest px-4 py-3 shadow-sm">
            <div className="flex items-center gap-1.5">
              <span className="material-symbols-outlined text-purple-600 text-lg">today</span>
              <p className="font-headline-md text-headline-md font-bold text-purple-700">
                {stats.todayJobsCount}
              </p>
            </div>
            <p className="font-label-sm text-label-sm text-on-surface-variant">
              Synced / added today
            </p>
          </div>

          <div className="rounded-xl border border-surface-variant bg-surface-container-lowest px-4 py-3 shadow-sm">
            <div className="flex items-center gap-1.5">
              <span className="material-symbols-outlined text-indigo-600 text-lg">psychology</span>
              <p className="font-headline-md text-headline-md font-bold text-indigo-700">
                {stats.aiClassifiedCount}
              </p>
            </div>
            <p className="font-label-sm text-label-sm text-on-surface-variant">
              AI classified (LLM)
            </p>
          </div>

          <div className="rounded-xl border border-surface-variant bg-surface-container-lowest px-4 py-3 shadow-sm">
            <div className="flex items-center gap-1.5">
              <span className="material-symbols-outlined text-amber-600 text-lg">pending_actions</span>
              <p className="font-headline-md text-headline-md font-bold text-amber-700">
                {stats.uncategorizedCount}
              </p>
            </div>
            <p className="font-label-sm text-label-sm text-on-surface-variant">
              Uncategorized
            </p>
          </div>
        </div>
      )}

      {/* 2. Search & Filter Bar */}
      <div className="rounded-xl border border-surface-variant bg-surface-container-lowest p-4 shadow-sm space-y-3">
        <div className="flex flex-col sm:flex-row items-stretch sm:items-center gap-3">
          {/* Search */}
          <div className="relative flex-1">
            <span className="material-symbols-outlined absolute left-3 top-1/2 -translate-y-1/2 text-on-surface-variant text-lg">
              search
            </span>
            <input
              type="text"
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              placeholder="Search by job title or company..."
              className="w-full rounded-lg border border-outline-variant bg-surface-container-low pl-9 pr-3 py-2 font-body-md text-body-md text-on-surface placeholder:text-on-surface-variant focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary"
            />
            {search && (
              <button
                type="button"
                onClick={() => setSearch('')}
                className="absolute right-3 top-1/2 -translate-y-1/2 text-on-surface-variant hover:text-on-surface"
              >
                <span className="material-symbols-outlined text-base">close</span>
              </button>
            )}
          </div>

          {/* Sector filter */}
          <select
            value={selectedSectorId}
            onChange={(e) => {
              setSelectedSectorId(e.target.value)
              setPage(1)
            }}
            aria-label="Filter by Sector"
            className="rounded-lg border border-outline-variant bg-surface-container-low px-3 py-2 font-body-md text-body-md text-on-surface focus:border-primary focus:outline-none"
          >
            <option value="">All Sectors</option>
            {sectors.map((s) => (
              <option key={s.id} value={s.id}>
                {s.name}
              </option>
            ))}
          </select>

          {/* Classification Method filter */}
          <select
            value={selectedMethod}
            onChange={(e) => {
              setSelectedMethod(e.target.value)
              setPage(1)
            }}
            aria-label="Filter by Method"
            className="rounded-lg border border-outline-variant bg-surface-container-low px-3 py-2 font-body-md text-body-md text-on-surface focus:border-primary focus:outline-none"
          >
            <option value="">All Methods</option>
            <option value="ai">AI Classified (LLM)</option>
            <option value="uncategorized">Uncategorized</option>
          </select>

          {/* Today Only Toggle */}
          <button
            type="button"
            onClick={() => {
              setTodayOnly(!todayOnly)
              setPage(1)
            }}
            className={cn(
              'inline-flex items-center gap-1.5 rounded-lg border px-3.5 py-2 font-label-md text-label-md font-medium transition-colors',
              todayOnly
                ? 'border-purple-600 bg-purple-600 text-white'
                : 'border-surface-variant bg-surface-container-low text-on-surface-variant hover:bg-surface-container-high',
            )}
          >
            <span className="material-symbols-outlined text-base">today</span>
            <span>Today Only</span>
          </button>
        </div>
      </div>

      {/* 3. Batch Action Toolbar */}
      <div className="flex flex-wrap items-center justify-between gap-3 rounded-xl border border-purple-200 bg-purple-50/40 p-4 shadow-sm">
        <div className="flex flex-wrap items-center gap-3">
          <label className="flex items-center gap-2 cursor-pointer select-none">
            <input
              type="checkbox"
              checked={allVisibleSelected}
              onChange={handleToggleSelectAllVisible}
              className="h-4 w-4 rounded border-outline-variant text-purple-600 focus:ring-purple-500"
            />
            <span className="font-label-md text-label-md font-semibold text-on-surface">
              Select all visible ({items.length})
            </span>
          </label>

          <button
            type="button"
            onClick={handleSelectTodayJobs}
            className="inline-flex items-center gap-1 text-xs font-semibold text-purple-700 bg-purple-100 hover:bg-purple-200 px-2.5 py-1 rounded-md transition-colors"
          >
            <span className="material-symbols-outlined text-sm">schedule</span>
            Select Today's Jobs
          </button>

          {selectedJobIds.size > 0 && (
            <span className="text-xs font-bold text-purple-900 bg-purple-200/80 px-2.5 py-1 rounded-full">
              {selectedJobIds.size} selected
            </span>
          )}
        </div>

        {/* AI Judge Audit Trigger */}
        <div className="flex items-center gap-2">
          <button
            type="button"
            onClick={() => setAuditModalOpen(true)}
            disabled={selectedJobIds.size === 0}
            className="inline-flex items-center gap-2 rounded-lg bg-gradient-to-r from-purple-700 to-indigo-700 px-5 py-2.5 font-label-md text-label-md font-semibold text-white shadow-md hover:from-purple-800 hover:to-indigo-800 transition-all disabled:opacity-50 disabled:pointer-events-none"
          >
            <span className="material-symbols-outlined text-lg">gavel</span>
            {selectedJobIds.size > 0
              ? `Audit with AI Judge (${selectedJobIds.size})`
              : 'Audit with AI Judge'}
          </button>
        </div>
      </div>

      {error && (
        <div
          role="alert"
          className="rounded-xl border border-error/30 bg-error-container p-4 font-label-md text-label-md text-on-error-container"
        >
          {error}
        </div>
      )}

      {toastMessage && (
        <div
          role="status"
          className="flex items-center gap-2 rounded-xl border border-emerald-300 bg-emerald-50 px-4 py-3 font-label-md text-label-md text-emerald-900 shadow-sm"
        >
          <span className="material-symbols-outlined text-emerald-700">check_circle</span>
          <span>{toastMessage}</span>
        </div>
      )}

      {/* 4. List of Job Classification Cards */}
      {loading ? (
        <div className="flex items-center justify-center py-16">
          <span
            aria-hidden="true"
            className="h-8 w-8 animate-spin rounded-full border-3 border-primary/30 border-t-primary"
          />
        </div>
      ) : items.length === 0 ? (
        <div className="rounded-xl border border-surface-variant bg-surface-container-lowest p-12 text-center">
          <span className="material-symbols-outlined text-4xl text-on-surface-variant">folder_off</span>
          <p className="mt-2 font-headline-sm text-headline-sm font-semibold text-on-surface">
            No jobs found
          </p>
          <p className="mt-1 font-body-md text-body-md text-on-surface-variant">
            Try adjusting your search query, sector filter, or method selection.
          </p>
        </div>
      ) : (
        <div className="space-y-4">
          {items.map((job) => {
            const isSelected = selectedJobIds.has(job.id)
            const isExpanded = expandedDescIds.has(job.id)
            const isSaving = savingJobId === job.id
            const currentAssignment = manualAssignments[job.id] ?? job.sectorId ?? ''

            return (
              <div
                key={job.id}
                className={cn(
                  'rounded-xl border p-5 shadow-sm transition-all',
                  isSelected
                    ? 'border-purple-500 bg-purple-50/20 shadow-md ring-1 ring-purple-500'
                    : 'border-surface-variant bg-surface-container-lowest hover:border-outline-variant',
                )}
              >
                {/* Card Header */}
                <div className="flex items-start justify-between gap-3">
                  <div className="flex items-start gap-3 min-w-0 flex-1">
                    <input
                      type="checkbox"
                      checked={isSelected}
                      onChange={() => handleToggleSelectItem(job.id)}
                      aria-label={`Select job ${job.title}`}
                      className="mt-1 h-4 w-4 rounded border-outline-variant text-purple-600 focus:ring-purple-500"
                    />

                    <div className="min-w-0 flex-1">
                      <div className="flex flex-wrap items-center gap-2">
                        <h3 className="font-headline-sm text-lg font-bold text-on-surface">
                          {job.title}
                        </h3>

                        {job.sourceName && (
                          <span className="rounded-md bg-surface-container-high px-2 py-0.5 font-label-xs text-xs font-semibold text-on-surface-variant">
                            via {job.sourceName}
                          </span>
                        )}

                        <span className="font-label-xs text-xs text-on-surface-variant">
                          {new Date(job.createdAt).toLocaleDateString()}
                        </span>
                      </div>

                      <p className="font-label-md text-label-md text-on-surface-variant mt-0.5">
                        <span className="font-medium text-on-surface">{job.company || 'Company Undisclosed'}</span>
                        {job.location && ` · ${job.location}`}
                      </p>
                    </div>
                  </div>

                  {/* "See more" button -> open job detail in new tab */}
                  <div className="flex items-center gap-2 flex-shrink-0">
                    <a
                      href={`/dashboard/talent/jobs/${job.id}`}
                      target="_blank"
                      rel="noopener noreferrer"
                      className="inline-flex items-center gap-1 rounded-lg border border-surface-variant bg-surface-container-low px-3 py-1.5 font-label-sm text-label-sm font-semibold text-primary hover:bg-surface-container-high transition-colors"
                    >
                      <span>See more</span>
                      <span className="material-symbols-outlined text-sm">open_in_new</span>
                    </a>
                  </div>
                </div>

                {/* Description Preview */}
                {job.descriptionPreview && (
                  <div className="mt-3 text-body-md text-on-surface/90 leading-relaxed">
                    <p className={cn(!isExpanded && 'line-clamp-2')}>
                      {job.descriptionPreview}
                    </p>
                    {job.descriptionPreview.length > 150 && (
                      <button
                        type="button"
                        onClick={() => toggleExpandDesc(job.id)}
                        className="mt-1 text-xs font-bold text-primary hover:underline inline-flex items-center gap-0.5"
                      >
                        <span>{isExpanded ? 'Show less' : 'Expand description'}</span>
                        <span className="material-symbols-outlined text-xs">
                          {isExpanded ? 'expand_less' : 'expand_more'}
                        </span>
                      </button>
                    )}
                  </div>
                )}

                {/* Raw Scraper Tags if present */}
                {(job.sourceSectors.length > 0 || job.ethioCategories.length > 0) && (
                  <div className="mt-3 flex flex-wrap items-center gap-1.5 text-xs">
                    <span className="font-semibold text-on-surface-variant">Raw source tags:</span>
                    {job.sourceSectors.map((t) => (
                      <span
                        key={t}
                        className="rounded bg-surface-container px-2 py-0.5 font-mono text-on-surface-variant"
                      >
                        {t}
                      </span>
                    ))}
                    {job.ethioCategories.map((c) => (
                      <span
                        key={c}
                        className="rounded bg-surface-container px-2 py-0.5 font-mono text-on-surface-variant"
                      >
                        {c}
                      </span>
                    ))}
                  </div>
                )}

                {/* Card Footer: Classification Method & Details */}
                <div className="mt-4 flex flex-col sm:flex-row sm:items-center justify-between gap-3 pt-3 border-t border-surface-variant/60">
                  <div className="flex flex-wrap items-center gap-2">
                    <span className="font-label-sm text-label-sm font-semibold text-on-surface">
                      Classification:
                    </span>

                    {/* Method Badge */}
                    {job.classificationMethod === 'AI' ? (
                      <span className="inline-flex items-center gap-1 rounded-full bg-purple-100 px-2.5 py-0.5 font-label-sm text-label-sm font-semibold text-purple-900 border border-purple-200">
                        <span className="material-symbols-outlined text-xs">psychology</span>
                        AI Classified (LLM)
                      </span>
                    ) : job.classificationMethod === 'AliasRule' ? (
                      <span className="inline-flex items-center gap-1 rounded-full bg-blue-100 px-2.5 py-0.5 font-label-sm text-label-sm font-semibold text-blue-900 border border-blue-200">
                        <span className="material-symbols-outlined text-xs">link</span>
                        Static Alias Rule
                      </span>
                    ) : job.classificationMethod === 'TitleKeyword' ? (
                      <span className="inline-flex items-center gap-1 rounded-full bg-amber-100 px-2.5 py-0.5 font-label-sm text-label-sm font-semibold text-amber-900 border border-amber-200">
                        <span className="material-symbols-outlined text-xs">match_case</span>
                        Title Keyword Rule
                      </span>
                    ) : job.classificationMethod === 'AdminManual' ? (
                      <span className="inline-flex items-center gap-1 rounded-full bg-slate-100 px-2.5 py-0.5 font-label-sm text-label-sm font-semibold text-slate-800 border border-slate-200">
                        <span className="material-symbols-outlined text-xs">person</span>
                        Admin Manual
                      </span>
                    ) : (
                      <span className="inline-flex items-center gap-1 rounded-full bg-red-100 px-2.5 py-0.5 font-label-sm text-label-sm font-semibold text-red-800 border border-red-200">
                        <span className="material-symbols-outlined text-xs">help_outline</span>
                        Uncategorized
                      </span>
                    )}

                    {/* Detailed deduction info */}
                    <span className="font-label-sm text-label-sm text-on-surface-variant font-medium">
                      ({job.classificationDetails})
                    </span>
                  </div>

                  {/* Inline Sector Selector & Save */}
                  <div className="flex items-center gap-2">
                    <select
                      value={currentAssignment}
                      onChange={(e) =>
                        setManualAssignments((prev) => ({
                          ...prev,
                          [job.id]: e.target.value,
                        }))
                      }
                      aria-label={`Assign sector to ${job.title}`}
                      className="rounded-lg border border-outline-variant bg-surface-container-low px-2.5 py-1 text-xs font-semibold text-on-surface focus:border-primary focus:outline-none"
                    >
                      <option value="">Uncategorized</option>
                      {sectors.map((s) => (
                        <option key={s.id} value={s.id}>
                          {s.name}
                        </option>
                      ))}
                    </select>

                    {manualAssignments[job.id] !== undefined &&
                      manualAssignments[job.id] !== (job.sectorId ?? '') && (
                        <button
                          type="button"
                          onClick={() => handleSaveSector(job)}
                          disabled={isSaving}
                          className="rounded-lg bg-primary px-3 py-1 font-label-xs text-xs font-semibold text-on-primary hover:opacity-90 disabled:opacity-50"
                        >
                          {isSaving ? 'Saving…' : 'Save'}
                        </button>
                      )}
                  </div>
                </div>
              </div>
            )
          })}
        </div>
      )}

      {/* 5. Pagination */}
      {totalPages > 1 && (
        <div className="flex items-center justify-between pt-4 border-t border-surface-variant">
          <p className="font-label-sm text-label-sm text-on-surface-variant">
            Page {page} of {totalPages} ({totalCount} total jobs)
          </p>
          <div className="flex items-center gap-2">
            <button
              type="button"
              disabled={page <= 1 || loading}
              onClick={() => setPage((p) => Math.max(1, p - 1))}
              className="rounded-lg border border-surface-variant px-3 py-1.5 font-label-sm text-label-sm font-medium text-on-surface hover:bg-surface-container-high disabled:opacity-50"
            >
              Previous
            </button>
            <button
              type="button"
              disabled={page >= totalPages || loading}
              onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
              className="rounded-lg border border-surface-variant px-3 py-1.5 font-label-sm text-label-sm font-medium text-on-surface hover:bg-surface-container-high disabled:opacity-50"
            >
              Next
            </button>
          </div>
        </div>
      )}

      {/* 6. AI Audit & Judge Modal */}
      <AiAuditModal
        open={auditModalOpen}
        onClose={() => setAuditModalOpen(false)}
        selectedJobs={selectedJobs}
        allSectors={sectors}
        token={token}
        onAppliedCorrections={() => {
          loadHistory()
        }}
      />
    </div>
  )
}
