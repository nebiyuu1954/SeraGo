import { useEffect, useMemo, useState } from 'react'
import type { ClassificationHistoryItem, SectorDetailResponse } from '../../../types'
import { bulkSetJobSectors, fetchJobsMetadataBulk, getApiErrorMessage } from '../../../api'

interface BatchCorrectionModalProps {
  open: boolean
  onClose: () => void
  sectors: SectorDetailResponse[]
  historyJobs?: ClassificationHistoryItem[]
  token: string
  onSuccess: (count: number) => void
}

interface ParsedCorrection {
  jobId: string
  jobTitle: string
  company: string
  currentSector: string
  sectorId: string | null
  sectorSlug: string | null
  targetSectorName: string
  status: 'valid' | 'invalid_job_id' | 'invalid_sector'
  error?: string
}

export default function BatchCorrectionModal({
  open,
  onClose,
  sectors,
  historyJobs = [],
  token,
  onSuccess,
}: BatchCorrectionModalProps) {
  const [rawInput, setRawInput] = useState('')
  const [submitting, setSubmitting] = useState(false)
  const [serverError, setServerError] = useState<string | null>(null)
  const [successCount, setSuccessCount] = useState<number | null>(null)
  const [syncToTelegram, setSyncToTelegram] = useState(true)
  const [fetchedMetadata, setFetchedMetadata] = useState<
    Record<string, { title: string; company: string; currentSector: string }>
  >({})

  // Fast sector lookup maps
  const { sectorById, sectorBySlug } = useMemo(() => {
    const byId = new Map<string, SectorDetailResponse>()
    const bySlug = new Map<string, SectorDetailResponse>()

    for (const s of sectors) {
      byId.set(s.id.toLowerCase(), s)
      if (s.slug) bySlug.set(s.slug.toLowerCase(), s)
      if (s.name) bySlug.set(s.name.toLowerCase(), s)
    }

    return { sectorById: byId, sectorBySlug: bySlug }
  }, [sectors])

  // Fast map of existing history jobs
  const historyJobsMap = useMemo(() => {
    const map = new Map<string, ClassificationHistoryItem>()
    for (const j of historyJobs) {
      map.set(j.id.toLowerCase(), j)
    }
    return map
  }, [historyJobs])

  // Parse raw pasted input (Array, Object with results, or Key-Value map)
  const { items, parseError } = useMemo(() => {
    if (!rawInput.trim()) {
      return { items: [] as ParsedCorrection[], parseError: null }
    }

    let cleaned = rawInput.trim()

    // 1. Strip markdown code fence if present
    const fenceMatch = cleaned.match(/```(?:json)?\s*([\s\S]*?)\s*```/)
    if (fenceMatch) {
      cleaned = fenceMatch[1].trim()
    }

    let parsed: any
    try {
      parsed = JSON.parse(cleaned)
    } catch (e: any) {
      return {
        items: [] as ParsedCorrection[],
        parseError: `Invalid JSON syntax: ${e.message}`,
      }
    }

    let listToProcess: any[] = []

    if (Array.isArray(parsed)) {
      listToProcess = parsed
    } else if (parsed && typeof parsed === 'object') {
      // Check if wrapped under keys like "corrections", "assignments", "results", "jobs"
      const arrayProperty = Object.values(parsed).find((val) => Array.isArray(val))
      if (arrayProperty && Array.isArray(arrayProperty)) {
        listToProcess = arrayProperty
      } else {
        // Assume key-value map: { "jobId": "sectorSlugOrId" }
        listToProcess = Object.entries(parsed).map(([jobId, sectorVal]) => {
          if (typeof sectorVal === 'string') {
            return { jobId, sectorSlug: sectorVal }
          }
          return { jobId, ...(typeof sectorVal === 'object' ? sectorVal : {}) }
        })
      }
    }

    const uuidRegex = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i

    const resolved: ParsedCorrection[] = []

    for (const rawItem of listToProcess) {
      if (!rawItem || typeof rawItem !== 'object') continue

      const jobId = (rawItem.jobId || rawItem.id || '').toString().trim()
      const sectorIdInput = (rawItem.sectorId || '').toString().trim()
      const sectorSlugInput = (
        rawItem.sectorSlug ||
        rawItem.slug ||
        rawItem.sector ||
        rawItem.sectorName ||
        ''
      )
        .toString()
        .trim()

      const historyMatch = historyJobsMap.get(jobId.toLowerCase())
      const fetchedMatch = fetchedMetadata[jobId.toLowerCase()]

      const jobTitle =
        rawItem.title ||
        rawItem.jobTitle ||
        historyMatch?.title ||
        fetchedMatch?.title ||
        'Loading title…'

      const company =
        rawItem.company ||
        historyMatch?.company ||
        fetchedMatch?.company ||
        'Company Undisclosed'

      const currentSector =
        rawItem.beforeSector ||
        rawItem.currentSector ||
        rawItem.oldSector ||
        historyMatch?.sectorName ||
        fetchedMatch?.currentSector ||
        'Current Sector'

      if (!jobId || !uuidRegex.test(jobId)) {
        resolved.push({
          jobId: jobId || 'missing-id',
          jobTitle: jobTitle !== 'Loading title…' ? jobTitle : 'Invalid Job ID',
          company,
          currentSector,
          sectorId: null,
          sectorSlug: null,
          targetSectorName: 'Invalid UUID',
          status: 'invalid_job_id',
          error: 'Job ID must be a valid 36-character UUID',
        })
        continue
      }

      // Try resolving target sector by ID first
      let matchedSector: SectorDetailResponse | undefined
      if (sectorIdInput) {
        matchedSector = sectorById.get(sectorIdInput.toLowerCase())
      }

      // If not resolved by ID, try resolving by slug or name
      if (!matchedSector && sectorSlugInput) {
        matchedSector = sectorBySlug.get(sectorSlugInput.toLowerCase())
      }

      if (matchedSector) {
        resolved.push({
          jobId,
          jobTitle,
          company,
          currentSector,
          sectorId: matchedSector.id,
          sectorSlug: matchedSector.slug,
          targetSectorName: matchedSector.name,
          status: 'valid',
        })
      } else {
        resolved.push({
          jobId,
          jobTitle,
          company,
          currentSector,
          sectorId: null,
          sectorSlug: sectorSlugInput || null,
          targetSectorName: sectorSlugInput || 'Unknown Sector',
          status: 'invalid_sector',
          error: `Sector "${sectorSlugInput || sectorIdInput}" not found in canonical list`,
        })
      }
    }

    return { items: resolved, parseError: null }
  }, [rawInput, sectorById, sectorBySlug, historyJobsMap, fetchedMetadata])

  const validItems = useMemo(() => items.filter((i) => i.status === 'valid'), [items])

  // Asynchronously fetch job metadata in one batch request for jobs not found in historyJobs or raw JSON
  useEffect(() => {
    if (!open || !token) return

    const missingIds = items
      .filter(
        (i) =>
          i.status === 'valid' &&
          (i.jobTitle === 'Loading title…' || i.currentSector === 'Current Sector') &&
          !fetchedMetadata[i.jobId.toLowerCase()],
      )
      .map((i) => i.jobId)

    if (missingIds.length === 0) return

    const batchToFetch = Array.from(new Set(missingIds)).slice(0, 100)

    let isSubscribed = true
    fetchJobsMetadataBulk(batchToFetch, token)
      .then((records) => {
        if (!isSubscribed) return
        const updates: Record<string, { title: string; company: string; currentSector: string }> = {}
        for (const r of records) {
          updates[r.id.toLowerCase()] = {
            title: r.title || 'Untitled Job',
            company: r.company || 'Company Undisclosed',
            currentSector: r.sectorName || 'Uncategorized',
          }
        }
        setFetchedMetadata((prev) => ({ ...prev, ...updates }))
      })
      .catch((err) => {
        console.error('Failed to bulk fetch job metadata', err)
      })

    return () => {
      isSubscribed = false
    }
  }, [items, fetchedMetadata, open, token])

  const handleApply = async () => {
    if (validItems.length === 0 || submitting) return
    setSubmitting(true)
    setServerError(null)

    try {
      const payload = validItems.map((i) => ({
        jobId: i.jobId,
        sectorId: i.sectorId,
        sectorSlug: i.sectorSlug,
      }))

      const res = await bulkSetJobSectors(payload, token, syncToTelegram)
      setSuccessCount(res.updatedCount)

      setTimeout(() => {
        onSuccess(res.updatedCount)
        onClose()
        setRawInput('')
        setSuccessCount(null)
      }, 1400)
    } catch (err: any) {
      setServerError(getApiErrorMessage(err))
    } finally {
      setSubmitting(false)
    }
  }

  if (!open) return null

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/60 backdrop-blur-xs animate-in fade-in duration-200">
      <div className="relative w-full max-w-4xl max-h-[92vh] flex flex-col rounded-2xl border border-surface-variant bg-surface-container-lowest shadow-2xl overflow-hidden">
        {/* Header */}
        <div className="flex items-center justify-between border-b border-surface-variant px-6 py-4 bg-gradient-to-r from-purple-50/80 via-indigo-50/50 to-surface-container-lowest">
          <div className="flex items-center gap-2.5">
            <span className="flex h-9 w-9 items-center justify-center rounded-xl bg-purple-600 text-white shadow-sm">
              <span className="material-symbols-outlined text-xl">bolt</span>
            </span>
            <div>
              <h2 className="font-headline-sm text-headline-sm font-bold text-on-surface">
                Batch Sector Corrections
              </h2>
              <p className="font-body-sm text-body-sm text-on-surface-variant">
                Review job titles, companies, before/after sectors, and apply updates in one click.
              </p>
            </div>
          </div>
          <button
            type="button"
            onClick={onClose}
            className="rounded-lg p-1.5 text-on-surface-variant hover:bg-surface-container-high hover:text-on-surface transition-colors"
          >
            <span className="material-symbols-outlined text-xl">close</span>
          </button>
        </div>

        {/* Body */}
        <div className="flex-1 overflow-y-auto p-6 space-y-4">
          {serverError && (
            <div className="rounded-xl border border-error/30 bg-error-container p-3 font-label-md text-label-md text-on-error-container">
              {serverError}
            </div>
          )}

          {successCount !== null && (
            <div className="rounded-xl border border-emerald-300 bg-emerald-50 p-4 font-label-md text-label-md text-emerald-900 flex items-center gap-2">
              <span className="material-symbols-outlined text-emerald-600">check_circle</span>
              <span className="font-semibold">
                Success! Successfully updated {successCount} jobs to their canonical sectors.
              </span>
            </div>
          )}

          {/* JSON Textarea */}
          <div>
            <div className="flex items-center justify-between mb-1.5">
              <label htmlFor="batch-json-input" className="font-label-md text-label-md font-bold text-on-surface">
                Paste Correction JSON:
              </label>
              {rawInput && (
                <button
                  type="button"
                  onClick={() => setRawInput('')}
                  className="font-label-sm text-label-sm text-purple-700 hover:text-purple-900 font-semibold"
                >
                  Clear input
                </button>
              )}
            </div>
            <textarea
              id="batch-json-input"
              rows={4}
              value={rawInput}
              onChange={(e) => setRawInput(e.target.value)}
              placeholder={`[\n  {\n    "jobId": "be1fbb3d-2096-4177-a0f6-1a10d86481f9",\n    "sectorSlug": "design-creative"\n  }\n]`}
              className="w-full font-mono text-xs rounded-xl border border-outline-variant bg-surface-container-low p-3 text-on-surface placeholder:text-on-surface-variant focus:border-purple-600 focus:outline-none focus:ring-1 focus:ring-purple-600"
            />
          </div>

          {parseError && (
            <div className="rounded-xl border border-amber-300 bg-amber-50 p-3 font-label-sm text-label-sm text-amber-900 flex items-center gap-2">
              <span className="material-symbols-outlined text-amber-700">warning</span>
              <span>{parseError}</span>
            </div>
          )}

          {/* Live Parsing Preview */}
          {items.length > 0 && (
            <div className="space-y-2">
              <div className="flex items-center justify-between">
                <span className="font-label-md text-label-md font-bold text-on-surface">
                  Preview: {validItems.length} valid of {items.length} parsed item(s)
                </span>
                {validItems.length < items.length && (
                  <span className="text-xs font-semibold text-amber-700 bg-amber-100 px-2 py-0.5 rounded-full">
                    {items.length - validItems.length} issue(s) detected
                  </span>
                )}
              </div>

              {/* Rich Preview List */}
              <div className="max-h-72 overflow-y-auto rounded-xl border border-surface-variant divide-y divide-surface-variant bg-surface-container-lowest shadow-inner">
                {items.map((item, idx) => (
                  <div
                    key={`${item.jobId}-${idx}`}
                    className="flex flex-col sm:flex-row sm:items-center justify-between gap-3 p-3.5 hover:bg-surface-container-high/40 transition-colors"
                  >
                    {/* Left: Job Title, Company, UUID & Clickable Link */}
                    <div className="flex-1 min-w-0 pr-2">
                      <div className="flex items-center gap-2">
                        <span className="font-label-sm text-xs font-bold text-on-surface-variant/70 min-w-[22px]">
                          #{idx + 1}
                        </span>
                        <a
                          href={`/dashboard/talent/jobs/${item.jobId}`}
                          target="_blank"
                          rel="noopener noreferrer"
                          className="font-headline-sm text-sm font-bold text-purple-900 hover:text-purple-700 hover:underline inline-flex items-center gap-1 truncate"
                          title="Open public job post in a new tab"
                        >
                          <span className="truncate">{item.jobTitle}</span>
                          <span className="material-symbols-outlined text-xs flex-shrink-0 text-purple-600">
                            open_in_new
                          </span>
                        </a>
                      </div>

                      <div className="flex flex-wrap items-center gap-2 text-xs text-on-surface-variant mt-1 pl-[30px]">
                        <span className="font-semibold text-on-surface">{item.company}</span>
                        <span>•</span>
                        <span className="font-mono text-[11px] text-on-surface-variant/70">
                          {item.jobId}
                        </span>
                      </div>
                    </div>

                    {/* Right: Before -> After Transition */}
                    <div className="flex items-center gap-2 flex-shrink-0 pl-[30px] sm:pl-0">
                      {/* Before Sector */}
                      <div className="flex flex-col items-end">
                        <span className="text-[10px] uppercase font-bold text-on-surface-variant/70 tracking-wider">
                          Before
                        </span>
                        <span className="inline-flex items-center gap-1 rounded-md bg-rose-50 border border-rose-200 px-2 py-0.5 text-xs font-semibold text-rose-800 line-through">
                          {item.currentSector}
                        </span>
                      </div>

                      <span className="material-symbols-outlined text-base text-on-surface-variant mt-3">
                        arrow_forward
                      </span>

                      {/* After / Target Sector */}
                      <div className="flex flex-col items-start">
                        <span className="text-[10px] uppercase font-bold text-purple-700 tracking-wider">
                          New Target
                        </span>
                        {item.status === 'valid' ? (
                          <span className="inline-flex items-center gap-1 rounded-md bg-purple-100 border border-purple-300 px-2.5 py-0.5 text-xs font-bold text-purple-950 shadow-xs">
                            <span className="material-symbols-outlined text-xs text-purple-700">check_circle</span>
                            <span>{item.targetSectorName}</span>
                          </span>
                        ) : (
                          <span
                            className="inline-flex items-center gap-1 rounded-md bg-red-100 border border-red-300 px-2.5 py-0.5 text-xs font-bold text-red-950"
                            title={item.error}
                          >
                            <span className="material-symbols-outlined text-xs text-red-700">error</span>
                            <span>{item.error || 'Invalid Sector'}</span>
                          </span>
                        )}
                      </div>
                    </div>
                  </div>
                ))}
              </div>
            </div>
          )}
        </div>

        {/* Footer */}
        <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3 border-t border-surface-variant px-6 py-4 bg-surface-container-low">
          <div className="flex flex-col sm:flex-row sm:items-center gap-3">
            <label className="inline-flex items-center gap-2 cursor-pointer select-none text-xs text-on-surface">
              <input
                type="checkbox"
                checked={syncToTelegram}
                onChange={(e) => setSyncToTelegram(e.target.checked)}
                className="h-4 w-4 rounded border-outline-variant text-purple-600 focus:ring-purple-500 cursor-pointer"
              />
              <span className="inline-flex items-center gap-1.5 font-medium text-on-surface">
                <span className="material-symbols-outlined text-sm text-sky-600">send</span>
                <span>Sync to Telegram (Bot DMs & Channel Edit)</span>
              </span>
            </label>

            {validItems.length > 0 && (
              <span className="text-xs text-on-surface-variant hidden sm:inline">•</span>
            )}

            <div className="text-xs text-on-surface-variant">
              {validItems.length > 0 ? (
                <span>Ready to update <b>{validItems.length}</b> jobs instantly.</span>
              ) : (
                <span>Paste JSON above to preview changes.</span>
              )}
            </div>
          </div>

          <div className="flex items-center gap-2 justify-end">
            <button
              type="button"
              onClick={onClose}
              disabled={submitting}
              className="rounded-xl border border-outline-variant bg-white px-4 py-2 font-label-md text-label-md font-semibold text-on-surface hover:bg-surface-container-high transition-colors disabled:opacity-50"
            >
              Cancel
            </button>
            <button
              type="button"
              onClick={handleApply}
              disabled={validItems.length === 0 || submitting}
              className="inline-flex items-center gap-2 rounded-xl bg-purple-700 hover:bg-purple-800 px-5 py-2 font-label-md text-label-md font-bold text-white shadow-md shadow-purple-200 transition-all disabled:opacity-50 disabled:pointer-events-none"
            >
              <span className="material-symbols-outlined text-lg">
                {submitting ? 'sync' : 'bolt'}
              </span>
              <span>
                {submitting
                  ? 'Applying Updates…'
                  : `Apply ${validItems.length} Correction${validItems.length === 1 ? '' : 's'}`}
              </span>
            </button>
          </div>
        </div>
      </div>
    </div>
  )
}
