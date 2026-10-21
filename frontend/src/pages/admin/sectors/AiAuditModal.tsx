import { useCallback, useEffect, useState } from 'react'
import { applyAuditCorrections, auditJobClassifications, getApiErrorMessage } from '../../../api'
import type {
  AuditReportResponse,
  ClassificationHistoryItem,
  CorrectionRequestItem,
} from '../../../types'
import { cn } from '../../../lib/cn.ts'

interface AiAuditModalProps {
  open: boolean
  onClose: () => void
  selectedJobs: ClassificationHistoryItem[]
  token: string
  onAppliedCorrections: (count: number) => void
}

export default function AiAuditModal({
  open,
  onClose,
  selectedJobs,
  token,
  onAppliedCorrections,
}: AiAuditModalProps) {
  const [loading, setLoading] = useState(false)
  const [report, setReport] = useState<AuditReportResponse | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [applying, setApplying] = useState(false)
  const [appliedJobIds, setAppliedJobIds] = useState<Set<string>>(new Set())
  const [toastMessage, setToastMessage] = useState<string | null>(null)

  const runAudit = useCallback(async () => {
    if (selectedJobs.length === 0 || !token) return
    setLoading(true)
    setError(null)
    setReport(null)
    setAppliedJobIds(new Set())
    try {
      const res = await auditJobClassifications(
        { jobIds: selectedJobs.map((j) => j.id) },
        token,
      )
      setReport(res)
    } catch (err) {
      setError(getApiErrorMessage(err))
    } finally {
      setLoading(false)
    }
  }, [selectedJobs, token])

  useEffect(() => {
    if (open && selectedJobs.length > 0) {
      runAudit()
    } else if (!open) {
      setReport(null)
      setError(null)
      setAppliedJobIds(new Set())
    }
  }, [open, runAudit, selectedJobs.length])

  if (!open) return null

  // Single job correction
  const handleApplySingle = async (jobId: string, suggestedSectorId: string | null) => {
    if (!suggestedSectorId || applying) return
    setApplying(true)
    try {
      const payload: CorrectionRequestItem[] = [{ jobId, newSectorId: suggestedSectorId }]
      const res = await applyAuditCorrections(payload, token)
      setAppliedJobIds((prev) => new Set(prev).add(jobId))
      setToastMessage(res.message || 'Sector updated successfully.')
      onAppliedCorrections(1)
    } catch (err) {
      setError(getApiErrorMessage(err))
    } finally {
      setApplying(false)
    }
  }

  // Batch apply all mismatches
  const handleApplyAll = async () => {
    if (!report || applying) return
    const unappliedMismatches = report.evaluations.filter(
      (e) =>
        e.verdict === 'MISMATCH' &&
        e.suggestedSectorId &&
        !appliedJobIds.has(e.jobId),
    )
    if (unappliedMismatches.length === 0) return

    setApplying(true)
    try {
      const payload: CorrectionRequestItem[] = unappliedMismatches.map((e) => ({
        jobId: e.jobId,
        newSectorId: e.suggestedSectorId!,
      }))
      const res = await applyAuditCorrections(payload, token)
      const newApplied = new Set(appliedJobIds)
      unappliedMismatches.forEach((e) => newApplied.add(e.jobId))
      setAppliedJobIds(newApplied)
      setToastMessage(res.message || `Successfully corrected ${res.updated} jobs.`)
      onAppliedCorrections(res.updated)
    } catch (err) {
      setError(getApiErrorMessage(err))
    } finally {
      setApplying(false)
    }
  }

  const unappliedCount =
    report?.evaluations.filter(
      (e) =>
        e.verdict === 'MISMATCH' &&
        e.suggestedSectorId &&
        !appliedJobIds.has(e.jobId),
    ).length ?? 0

  return (
    <div
      role="dialog"
      aria-modal="true"
      aria-labelledby="audit-modal-title"
      className="fixed inset-0 z-50 flex items-center justify-center p-3 sm:p-6"
    >
      {/* Backdrop */}
      <div
        className="fixed inset-0 bg-slate-900/60 backdrop-blur-sm transition-opacity"
        onClick={onClose}
        aria-hidden="true"
      />

      {/* Modal Dialog Card */}
      <div className="relative flex max-h-[92vh] w-full max-w-4xl flex-col rounded-2xl border border-surface-variant bg-surface-container-lowest shadow-2xl overflow-hidden">
        {/* Header */}
        <div className="flex items-center justify-between border-b border-surface-variant px-6 py-4">
          <div className="flex items-center gap-3">
            <div className="flex h-10 w-10 items-center justify-center rounded-xl bg-purple-500/10 text-purple-600">
              <span className="material-symbols-outlined text-2xl">gavel</span>
            </div>
            <div>
              <h2 id="audit-modal-title" className="font-headline-sm text-headline-sm font-bold text-on-surface">
                AI Quality Auditor & Judge
              </h2>
              <p className="font-label-sm text-label-sm text-on-surface-variant">
                Independent AI evaluation of {selectedJobs.length} job{selectedJobs.length === 1 ? '' : 's'} against SeraGo's 23 official sectors
              </p>
            </div>
          </div>
          <button
            type="button"
            onClick={onClose}
            className="rounded-lg p-2 text-on-surface-variant hover:bg-surface-container-high transition-colors"
            aria-label="Close modal"
          >
            <span className="material-symbols-outlined text-xl">close</span>
          </button>
        </div>

        {/* Body content */}
        <div className="flex-1 overflow-y-auto px-6 py-5 space-y-6">
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

          {loading ? (
            <div className="flex flex-col items-center justify-center py-16 space-y-4">
              <span
                aria-hidden="true"
                className="h-10 w-10 animate-spin rounded-full border-3 border-purple-500/30 border-t-purple-600"
              />
              <div className="text-center">
                <p className="font-label-lg text-label-lg font-semibold text-on-surface">
                  Auditing job classifications…
                </p>
                <p className="font-label-sm text-label-sm text-on-surface-variant mt-1">
                  Comparing job titles, descriptions, and companies with canonical industry sectors
                </p>
              </div>
            </div>
          ) : report ? (
            <>
              {/* Executive Summary Stats */}
              <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
                <div className="rounded-xl border border-surface-variant bg-surface-container-low p-4 text-center sm:text-left">
                  <p className="font-label-sm text-label-sm text-on-surface-variant">
                    Accuracy Rating
                  </p>
                  <div className="mt-1 flex items-baseline justify-center sm:justify-start gap-2">
                    <span
                      className={cn(
                        'font-headline-lg text-headline-lg font-extrabold',
                        report.accuracyRate >= 80 ? 'text-emerald-600' : 'text-amber-600',
                      )}
                    >
                      {report.accuracyRate}%
                    </span>
                    <span className="font-label-sm text-label-sm text-on-surface-variant">
                      ({report.agreedCount}/{report.totalAudited} correct)
                    </span>
                  </div>
                </div>

                <div className="rounded-xl border border-surface-variant bg-surface-container-low p-4 text-center sm:text-left">
                  <p className="font-label-sm text-label-sm text-on-surface-variant">
                    Agreed / Correct
                  </p>
                  <div className="mt-1 flex items-baseline justify-center sm:justify-start gap-2">
                    <span className="font-headline-lg text-headline-lg font-extrabold text-emerald-600">
                      {report.agreedCount}
                    </span>
                    <span className="font-label-sm text-label-sm text-emerald-700 font-medium">
                      Accurate
                    </span>
                  </div>
                </div>

                <div className="rounded-xl border border-surface-variant bg-surface-container-low p-4 text-center sm:text-left">
                  <p className="font-label-sm text-label-sm text-on-surface-variant">
                    Mismatches Flagged
                  </p>
                  <div className="mt-1 flex items-baseline justify-center sm:justify-start gap-2">
                    <span
                      className={cn(
                        'font-headline-lg text-headline-lg font-extrabold',
                        report.mismatchedCount > 0 ? 'text-amber-600' : 'text-on-surface',
                      )}
                    >
                      {report.mismatchedCount}
                    </span>
                    <span className="font-label-sm text-label-sm text-amber-700 font-medium">
                      Need correction
                    </span>
                  </div>
                </div>
              </div>

              {/* Summary & Recommendations */}
              <div className="rounded-xl border border-purple-200 bg-purple-50/50 p-4 space-y-2">
                <div className="flex items-center gap-2 text-purple-900 font-semibold font-label-md text-label-md">
                  <span className="material-symbols-outlined text-lg text-purple-700">insights</span>
                  <span>Auditor Insights & Optimization Tips</span>
                </div>
                {report.overallSummary && (
                  <p className="font-body-md text-body-md text-purple-950">
                    {report.overallSummary}
                  </p>
                )}
                {report.optimizationTips && report.optimizationTips.length > 0 && (
                  <ul className="list-disc list-inside space-y-1 font-label-sm text-label-sm text-purple-900 pt-1">
                    {report.optimizationTips.map((tip, idx) => (
                      <li key={idx}>{tip}</li>
                    ))}
                  </ul>
                )}
              </div>

              {/* Evaluations List */}
              <div className="space-y-3">
                <div className="flex items-center justify-between">
                  <h3 className="font-label-md text-label-md font-semibold text-on-surface">
                    Audit Breakdown ({report.evaluations.length} items)
                  </h3>
                  {unappliedCount > 0 && (
                    <span className="font-label-sm text-label-sm text-amber-700 font-medium bg-amber-100 px-2.5 py-0.5 rounded-full">
                      {unappliedCount} correction{unappliedCount === 1 ? '' : 's'} available
                    </span>
                  )}
                </div>

                <div className="space-y-3">
                  {report.evaluations.map((evalItem) => {
                    const isMismatch = evalItem.verdict === 'MISMATCH'
                    const isApplied = appliedJobIds.has(evalItem.jobId)

                    return (
                      <div
                        key={evalItem.jobId}
                        className={cn(
                          'rounded-xl border p-4 transition-all',
                          isMismatch
                            ? isApplied
                              ? 'border-emerald-200 bg-emerald-50/30'
                              : 'border-amber-300 bg-amber-50/40'
                            : 'border-surface-variant bg-surface-container-lowest',
                        )}
                      >
                        <div className="flex flex-col sm:flex-row sm:items-start justify-between gap-3">
                          <div className="space-y-1 min-w-0 flex-1">
                            <div className="flex flex-wrap items-center gap-2">
                              <h4 className="font-body-md text-body-md font-bold text-on-surface">
                                {evalItem.title}
                              </h4>
                              {evalItem.company && (
                                <span className="font-label-sm text-label-sm text-on-surface-variant">
                                  · {evalItem.company}
                                </span>
                              )}
                            </div>

                            {/* Verdict Comparison */}
                            <div className="flex flex-wrap items-center gap-2 pt-1 font-label-sm text-label-sm">
                              <span className="text-on-surface-variant font-medium">Assigned:</span>
                              <span className="rounded-md bg-surface-container-high px-2 py-0.5 text-on-surface font-semibold">
                                {evalItem.currentSectorName}
                              </span>

                              {isMismatch && (
                                <>
                                  <span className="material-symbols-outlined text-sm text-amber-700">arrow_forward</span>
                                  <span className="text-amber-900 font-medium">AI Recommended:</span>
                                  <span className="rounded-md bg-amber-200 px-2 py-0.5 font-bold text-amber-950">
                                    {evalItem.suggestedSectorName}
                                  </span>
                                </>
                              )}
                            </div>

                            {/* Critique / Reasoning */}
                            <p className="font-label-sm text-label-sm text-on-surface-variant pt-1 leading-relaxed">
                              {evalItem.reason}
                            </p>

                            {/* Root Cause pill */}
                            {evalItem.rootCause && (
                              <div className="pt-1">
                                <span className="inline-flex items-center gap-1 font-label-xs text-xs text-on-surface-variant/80 bg-surface-container px-2 py-0.5 rounded">
                                  <span>Root cause:</span>
                                  <span className="font-medium text-on-surface">{evalItem.rootCause}</span>
                                </span>
                              </div>
                            )}
                          </div>

                          {/* Verdict Badge & Actions */}
                          <div className="flex sm:flex-col items-center sm:items-end justify-between sm:justify-start gap-2 flex-shrink-0">
                            {isMismatch ? (
                              <span className="inline-flex items-center gap-1 rounded-full bg-amber-100 px-3 py-1 font-label-sm text-label-sm font-semibold text-amber-900 border border-amber-300">
                                <span className="material-symbols-outlined text-sm">warning</span>
                                MISMATCH
                              </span>
                            ) : (
                              <span className="inline-flex items-center gap-1 rounded-full bg-emerald-100 px-3 py-1 font-label-sm text-label-sm font-semibold text-emerald-900 border border-emerald-300">
                                <span className="material-symbols-outlined text-sm">check_circle</span>
                                AGREE
                              </span>
                            )}

                            {isMismatch && (
                              <>
                                {isApplied ? (
                                  <span className="inline-flex items-center gap-1 font-label-sm text-label-sm font-semibold text-emerald-700 mt-1">
                                    <span className="material-symbols-outlined text-sm">done</span>
                                    Corrected
                                  </span>
                                ) : (
                                  <button
                                    type="button"
                                    onClick={() => handleApplySingle(evalItem.jobId, evalItem.suggestedSectorId ?? null)}
                                    disabled={applying || !evalItem.suggestedSectorId}
                                    className="rounded-lg bg-primary px-3 py-1.5 font-label-sm text-label-sm font-medium text-on-primary transition-opacity hover:opacity-90 disabled:opacity-50"
                                  >
                                    Apply Correction
                                  </button>
                                )}
                              </>
                            )}
                          </div>
                        </div>
                      </div>
                    )
                  })}
                </div>
              </div>
            </>
          ) : (
            <p className="font-body-md text-body-md text-on-surface-variant text-center py-10">
              No audit report available. Click re-run audit.
            </p>
          )}
        </div>

        {/* Footer */}
        <div className="flex flex-wrap items-center justify-between gap-3 border-t border-surface-variant bg-surface-container-low px-6 py-4">
          <button
            type="button"
            onClick={runAudit}
            disabled={loading || applying}
            className="inline-flex items-center gap-1.5 rounded-lg border border-surface-variant bg-surface-container-lowest px-4 py-2 font-label-md text-label-md font-medium text-on-surface transition-colors hover:bg-surface-container-high disabled:opacity-50"
          >
            <span className="material-symbols-outlined text-lg">refresh</span>
            Re-run Audit
          </button>

          <div className="flex items-center gap-3">
            {unappliedCount > 0 && (
              <button
                type="button"
                onClick={handleApplyAll}
                disabled={applying || loading}
                className="inline-flex items-center gap-1.5 rounded-lg bg-emerald-700 px-5 py-2 font-label-md text-label-md font-semibold text-white shadow-sm hover:bg-emerald-800 transition-colors disabled:opacity-50"
              >
                {applying ? (
                  <>
                    <span className="h-4 w-4 animate-spin rounded-full border-2 border-white/40 border-t-white" />
                    Applying…
                  </>
                ) : (
                  <>
                    <span className="material-symbols-outlined text-lg">done_all</span>
                    Apply All Corrections ({unappliedCount})
                  </>
                )}
              </button>
            )}

            <button
              type="button"
              onClick={onClose}
              className="rounded-lg bg-surface-container-high px-5 py-2 font-label-md text-label-md font-medium text-on-surface hover:bg-surface-container-highest transition-colors"
            >
              Close
            </button>
          </div>
        </div>
      </div>
    </div>
  )
}
