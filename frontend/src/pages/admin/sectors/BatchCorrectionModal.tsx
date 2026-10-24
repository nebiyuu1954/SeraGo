import { useMemo, useState } from 'react'
import type { SectorDetailResponse } from '../../../types'
import { bulkSetJobSectors, getApiErrorMessage } from '../../../api'
import { cn } from '../../../lib/cn.ts'

interface BatchCorrectionModalProps {
  open: boolean
  onClose: () => void
  sectors: SectorDetailResponse[]
  token: string
  onSuccess: (count: number) => void
}

interface ParsedCorrection {
  jobId: string
  sectorId: string | null
  sectorSlug: string | null
  sectorName: string
  status: 'valid' | 'invalid_job_id' | 'invalid_sector'
  error?: string
}

export default function BatchCorrectionModal({
  open,
  onClose,
  sectors,
  token,
  onSuccess,
}: BatchCorrectionModalProps) {
  const [rawInput, setRawInput] = useState('')
  const [submitting, setSubmitting] = useState(false)
  const [serverError, setServerError] = useState<string | null>(null)
  const [successCount, setSuccessCount] = useState<number | null>(null)

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

      if (!jobId || !uuidRegex.test(jobId)) {
        resolved.push({
          jobId: jobId || 'missing-id',
          sectorId: null,
          sectorSlug: null,
          sectorName: 'Invalid UUID',
          status: 'invalid_job_id',
          error: 'Job ID must be a valid 36-character UUID',
        })
        continue
      }

      // Try resolving sector by ID first
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
          sectorId: matchedSector.id,
          sectorSlug: matchedSector.slug,
          sectorName: matchedSector.name,
          status: 'valid',
        })
      } else {
        resolved.push({
          jobId,
          sectorId: null,
          sectorSlug: sectorSlugInput || null,
          sectorName: sectorSlugInput || 'Unknown Sector',
          status: 'invalid_sector',
          error: `Sector "${sectorSlugInput || sectorIdInput}" not found in canonical list`,
        })
      }
    }

    return { items: resolved, parseError: null }
  }, [rawInput, sectorById, sectorBySlug])

  const validItems = useMemo(() => items.filter((i) => i.status === 'valid'), [items])

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

      const res = await bulkSetJobSectors(payload, token)
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
      <div className="relative w-full max-w-3xl max-h-[90vh] flex flex-col rounded-2xl border border-surface-variant bg-surface-container-lowest shadow-2xl overflow-hidden">
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
                Paste the correction JSON from your AI audit report to update misclassified jobs in one click.
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
              rows={6}
              value={rawInput}
              onChange={(e) => setRawInput(e.target.value)}
              placeholder={`[\n  {\n    "jobId": "c18ba00c-b400-4ffd-9dbb-fcd16f35c261",\n    "sectorSlug": "sales-marketing"\n  },\n  {\n    "jobId": "d60b2e91-9123-4754-a7ed-6f68a5a12113",\n    "sectorSlug": "procurement-supply-chain"\n  }\n]`}
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

              <div className="max-h-56 overflow-y-auto rounded-xl border border-surface-variant divide-y divide-surface-variant bg-surface-container-lowest">
                {items.map((item, idx) => (
                  <div
                    key={`${item.jobId}-${idx}`}
                    className="flex items-center justify-between px-3 py-2 text-xs"
                  >
                    <div className="flex items-center gap-2 font-mono text-on-surface">
                      <span className="text-on-surface-variant font-sans font-semibold">#{idx + 1}</span>
                      <span className="truncate max-w-[200px] sm:max-w-[280px]" title={item.jobId}>
                        {item.jobId}
                      </span>
                    </div>

                    <div className="flex items-center gap-2">
                      <span className="material-symbols-outlined text-sm text-on-surface-variant">
                        arrow_forward
                      </span>
                      {item.status === 'valid' ? (
                        <span className="inline-flex items-center gap-1 font-semibold text-purple-900 bg-purple-100 px-2 py-0.5 rounded-md">
                          <span className="material-symbols-outlined text-xs text-purple-700">check</span>
                          {item.sectorName}
                        </span>
                      ) : (
                        <span
                          className="inline-flex items-center gap-1 font-semibold text-red-900 bg-red-100 px-2 py-0.5 rounded-md"
                          title={item.error}
                        >
                          <span className="material-symbols-outlined text-xs text-red-700">error</span>
                          {item.error || 'Invalid'}
                        </span>
                      )}
                    </div>
                  </div>
                ))}
              </div>
            </div>
          )}
        </div>

        {/* Footer */}
        <div className="flex items-center justify-between border-t border-surface-variant px-6 py-4 bg-surface-container-low">
          <div className="text-xs text-on-surface-variant">
            {validItems.length > 0 ? (
              <span>Ready to update <b>{validItems.length}</b> jobs instantly.</span>
            ) : (
              <span>Paste JSON above to preview changes.</span>
            )}
          </div>

          <div className="flex items-center gap-2">
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
