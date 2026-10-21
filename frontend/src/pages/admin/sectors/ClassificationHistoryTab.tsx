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
  const [copiedJobId, setCopiedJobId] = useState<string | null>(null)

  // Inline assignment edits
  const [manualAssignments, setManualAssignments] = useState<Record<string, string>>({})
  const [savingJobId, setSavingJobId] = useState<string | null>(null)
  const [toastMessage, setToastMessage] = useState<string | null>(null)

  const handleCopyDebugInfo = (job: ClassificationHistoryItem) => {
    const rawTags = [
      ...(job.sourceSectors || []),
      ...(job.ethioCategories || []),
    ].filter(Boolean)

    const payload = {
      jobId: job.id,
      title: job.title,
      company: job.company || 'Company Undisclosed',
      location: job.location || 'N/A',
      sourceName: job.sourceName || 'Direct',
      rawScraperTags: rawTags.length > 0 ? rawTags : ['None / Untagged'],
      classifiedSector: job.sectorName || 'Uncategorized',
      classifiedSlug: job.sectorSlug || null,
      classificationMethod: job.classificationMethod,
      confidence: job.confidence != null ? `${Math.round(job.confidence * 100)}%` : 'N/A',
      reasoning: job.reasoning || job.classificationDetails,
      descriptionSnippet: job.descriptionPreview,
    }

    const textToCopy = `### Job Classification Debug & Prompt Refinement
- **Title:** ${job.title}
- **Company:** ${job.company || 'Company Undisclosed'}
- **Location:** ${job.location || 'N/A'}
- **Source:** ${job.sourceName || 'Direct'}
- **Scraper Raw Tag (Before):** ${rawTags.length > 0 ? rawTags.join(', ') : 'None / Untagged'}
- **Classified Sector (After):** ${job.sectorName || 'Uncategorized'}
- **Classification Method:** ${job.classificationMethod} (${job.classificationDetails})
- **Confidence:** ${payload.confidence}
- **LLM Reasoning:** ${job.reasoning || 'N/A'}

#### Description Preview:
${job.descriptionPreview}

#### Prompt Test Case (JSON):
\`\`\`json
${JSON.stringify(payload, null, 2)}
\`\`\`
`
    navigator.clipboard.writeText(textToCopy)
    setCopiedJobId(job.id)
    setToastMessage(`Copied details for "${job.title}" to clipboard!`)
    setTimeout(() => {
      setCopiedJobId((curr) => (curr === job.id ? null : curr))
    }, 2500)
  }

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
            <option value="gemini">Gemini Flash (Tier 1)</option>
            <option value="groq">Groq Cloud (Tier 2)</option>
            <option value="ollama">Local Ollama (Tier 3)</option>
            <option value="cosine">Cosine Similarity (Tier 4)</option>
            <option value="alias">Scraper Alias Rule</option>
            <option value="title">Title Keyword Rule</option>
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
            const rawTags = [
              ...(job.sourceSectors || []),
              ...(job.ethioCategories || []),
            ].filter(Boolean)

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
                {/* 1. Card Header: Title, Employer, Source & Top Action Buttons */}
                <div className="flex flex-col sm:flex-row sm:items-start justify-between gap-3">
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
                        <span className="font-semibold text-on-surface">
                          {job.company || 'Company Undisclosed'}
                        </span>
                        {job.location && ` · ${job.location}`}
                      </p>
                    </div>
                  </div>

                  {/* Top Action Buttons: Copy Debug Info + See in All Jobs */}
                  <div className="flex flex-wrap items-center gap-2 flex-shrink-0 self-start">
                    {/* Copy Debug & Classification Log Button */}
                    <button
                      type="button"
                      onClick={() => handleCopyDebugInfo(job)}
                      title="Copy job details, scraper tags, and classification log for prompt refinement"
                      className={cn(
                        'inline-flex items-center gap-1.5 rounded-lg border px-3 py-1.5 font-label-sm text-label-sm font-semibold transition-all shadow-sm',
                        copiedJobId === job.id
                          ? 'border-emerald-500 bg-emerald-50 text-emerald-800 ring-1 ring-emerald-500'
                          : 'border-surface-variant bg-surface-container-low text-on-surface hover:bg-surface-container-high',
                      )}
                    >
                      <span className="material-symbols-outlined text-sm">
                        {copiedJobId === job.id ? 'check' : 'content_copy'}
                      </span>
                      <span>{copiedJobId === job.id ? 'Copied Details!' : 'Copy Debug Info'}</span>
                    </button>

                    {/* See in All Jobs Button (Opens in new tab with search filled) */}
                    <a
                      href={`/dashboard/talent/jobs?view=all&search=${encodeURIComponent(job.title)}`}
                      target="_blank"
                      rel="noopener noreferrer"
                      title="Open in All Jobs tab with this job title in the search bar"
                      className="inline-flex items-center gap-1.5 rounded-lg border border-purple-200 bg-purple-50/70 px-3 py-1.5 font-label-sm text-label-sm font-semibold text-purple-900 hover:bg-purple-100 transition-colors shadow-sm"
                    >
                      <span>See in All Jobs</span>
                      <span className="material-symbols-outlined text-sm">open_in_new</span>
                    </a>
                  </div>
                </div>

                {/* 2. Description Preview */}
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

                {/* 3. Before (Scraper Tag) ➜ After (Classified Sector) Pipeline Box */}
                <div className="mt-3.5 rounded-xl border border-outline-variant/60 bg-surface-container-low/60 p-3.5">
                  <div className="grid grid-cols-1 md:grid-cols-2 gap-3 items-center">
                    {/* Before: Raw tag from the scraper */}
                    <div className="flex items-center gap-2 flex-wrap">
                      <span className="inline-flex items-center gap-1 font-label-xs text-xs font-bold uppercase tracking-wider text-on-surface-variant">
                        <span className="material-symbols-outlined text-sm text-amber-600">sell</span>
                        Before (Scraper Tag):
                      </span>
                      {rawTags.length > 0 ? (
                        rawTags.map((t) => (
                          <span
                            key={t}
                            className="rounded-md bg-amber-100/90 text-amber-900 border border-amber-300 px-2.5 py-0.5 font-mono text-xs font-semibold shadow-xs"
                          >
                            {t}
                          </span>
                        ))
                      ) : (
                        <span className="rounded-md bg-surface-container px-2 py-0.5 font-mono text-xs text-on-surface-variant italic">
                          Untagged by source
                        </span>
                      )}
                    </div>

                    {/* After: To which we classified it */}
                    <div className="flex items-center gap-2 flex-wrap md:justify-end">
                      <span className="material-symbols-outlined text-base text-purple-600 hidden md:inline">
                        arrow_forward
                      </span>
                      <span className="inline-flex items-center gap-1 font-label-xs text-xs font-bold uppercase tracking-wider text-on-surface-variant">
                        <span className="material-symbols-outlined text-sm text-purple-600">flag</span>
                        Classified To (After):
                      </span>
                      {job.sectorName ? (
                        <span className="rounded-md bg-purple-100 text-purple-900 border border-purple-300 px-2.5 py-0.5 font-label-sm text-xs font-bold shadow-xs">
                          {job.sectorName}
                        </span>
                      ) : (
                        <span className="rounded-md bg-rose-100 text-rose-900 border border-rose-300 px-2.5 py-0.5 font-label-sm text-xs font-bold shadow-xs">
                          Uncategorized
                        </span>
                      )}
                    </div>
                  </div>

                  {/* LLM Model & Confidence Metadata */}
                  <div className="mt-3 pt-2.5 border-t border-outline-variant/40 flex flex-wrap items-center justify-between gap-2 text-xs">
                    <div className="flex flex-wrap items-center gap-2">
                      <span className="font-semibold text-on-surface">Intelligence:</span>

                      {/* Model Badge */}
                      <span
                        className={cn(
                          'inline-flex items-center gap-1 rounded-full px-2.5 py-0.5 font-semibold text-xs border shadow-xs',
                          getMethodBadgeStyle(job.classificationMethod),
                        )}
                      >
                        <span className="material-symbols-outlined text-xs">
                          {getMethodIcon(job.classificationMethod)}
                        </span>
                        {formatMethodLabel(job.classificationMethod)}
                      </span>

                      {/* Confidence Pill */}
                      {job.confidence != null && (
                        <span
                          className={cn(
                            'inline-flex items-center gap-1 rounded-full px-2 py-0.5 text-xs font-bold border shadow-xs',
                            job.confidence >= 0.85
                              ? 'bg-emerald-100 text-emerald-900 border-emerald-300'
                              : job.confidence >= 0.70
                              ? 'bg-amber-100 text-amber-900 border-amber-300'
                              : 'bg-rose-100 text-rose-900 border-rose-300',
                          )}
                        >
                          <span>{Math.round(job.confidence * 100)}% Confidence</span>
                        </span>
                      )}
                    </div>

                    <span className="text-on-surface-variant font-medium">
                      {job.classificationDetails}
                    </span>
                  </div>

                  {/* AI Reasoning / Data explanation */}
                  {job.reasoning && (
                    <div className="mt-2.5 rounded-lg bg-surface-container/70 p-2 text-xs text-on-surface-variant font-body-sm flex items-start gap-1.5 border border-outline-variant/30">
                      <span className="material-symbols-outlined text-sm text-purple-600 flex-shrink-0 mt-0.5">
                        lightbulb
                      </span>
                      <p className="leading-snug">
                        <strong className="text-on-surface font-semibold">AI Reasoning: </strong>
                        {job.reasoning}
                      </p>
                    </div>
                  )}
                </div>

                {/* 4. Card Footer: Inline Sector Override & Save */}
                <div className="mt-3 flex flex-col sm:flex-row sm:items-center justify-between gap-3 pt-3 border-t border-surface-variant/60">
                  <span className="font-label-sm text-xs font-semibold text-on-surface-variant">
                    Manual Override:
                  </span>

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
        token={token}
        onAppliedCorrections={() => {
          loadHistory()
        }}
      />
    </div>
  )
}

function getMethodBadgeStyle(method: string): string {
  switch (method?.toLowerCase()) {
    case 'gemini':
      return 'bg-purple-100 text-purple-900 border-purple-300'
    case 'groq':
      return 'bg-sky-100 text-sky-900 border-sky-300'
    case 'ollama':
      return 'bg-indigo-100 text-indigo-900 border-indigo-300'
    case 'cosinesimilarity':
    case 'cosine':
      return 'bg-teal-100 text-teal-900 border-teal-300'
    case 'ai':
      return 'bg-purple-100 text-purple-900 border-purple-300'
    case 'aliasrule':
    case 'alias':
      return 'bg-blue-100 text-blue-900 border-blue-300'
    case 'titlekeyword':
    case 'title':
      return 'bg-amber-100 text-amber-900 border-amber-300'
    case 'adminmanual':
      return 'bg-emerald-100 text-emerald-900 border-emerald-300'
    default:
      return 'bg-rose-100 text-rose-900 border-rose-300'
  }
}

function getMethodIcon(method: string): string {
  switch (method?.toLowerCase()) {
    case 'gemini':
      return 'psychology'
    case 'groq':
      return 'bolt'
    case 'ollama':
      return 'terminal'
    case 'cosinesimilarity':
    case 'cosine':
      return 'query_stats'
    case 'ai':
      return 'psychology'
    case 'aliasrule':
    case 'alias':
      return 'link'
    case 'titlekeyword':
    case 'title':
      return 'match_case'
    case 'adminmanual':
      return 'person'
    default:
      return 'help_outline'
  }
}

function formatMethodLabel(method: string): string {
  switch (method?.toLowerCase()) {
    case 'gemini':
      return 'Google Gemini Flash (Tier 1)'
    case 'groq':
      return 'Groq Cloud LLM (Tier 2)'
    case 'ollama':
      return 'Local Ollama (Tier 3)'
    case 'cosinesimilarity':
    case 'cosine':
      return 'Cosine Similarity (Tier 4 Vector)'
    case 'ai':
      return 'Google Gemini Flash (Tier 1)'
    case 'aliasrule':
    case 'alias':
      return 'Scraper Alias Rule'
    case 'titlekeyword':
    case 'title':
      return 'Title Keyword Rule'
    case 'adminmanual':
      return 'Admin Manual Override'
    default:
      return 'Uncategorized'
  }
}
