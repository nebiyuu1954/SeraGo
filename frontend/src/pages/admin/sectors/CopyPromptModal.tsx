import { useMemo, useState } from 'react'
import type { JobResponse, SectorDetailResponse } from '../../../types'
import { setJobSector, getApiErrorMessage } from '../../../api'
import {
  getSampleRolesForSector,
  SECTOR_DOMAIN_KEYWORDS,
} from '../../../lib/sectorMatching.ts'
import { cn } from '../../../lib/cn.ts'

interface CopyPromptModalProps {
  open: boolean
  onClose: () => void
  jobs: JobResponse[]
  sectors: SectorDetailResponse[]
  token: string | null
  onSuccess: (resolvedJobIds: string[]) => void
}

interface ParsedAssignment {
  jobId: string
  jobTitle: string
  company: string
  targetSectorId: string | null
  targetSectorName: string
  status: 'valid' | 'unknown_sector' | 'unknown_job'
}

/**
 * Builds the AI prompt containing system instructions, canonical sector ontology
 * with sample roles & keywords, and the uncategorized jobs.
 */
function buildSectorClassificationPrompt(
  jobs: JobResponse[],
  sectors: SectorDetailResponse[],
): string {
  // 1. Build canonical sector ontology
  const sectorListText = sectors
    .map((s, idx) => {
      const sampleRoles = getSampleRolesForSector(s.name)
      const keywords = (SECTOR_DOMAIN_KEYWORDS[s.name] || []).slice(0, 12)
      const rolesHint =
        sampleRoles.length > 0 ? `\n   Sample Roles: ${sampleRoles.join(', ')}` : ''
      const kwHint =
        keywords.length > 0 ? `\n   Domain Keywords: ${keywords.join(', ')}` : ''
      return `${idx + 1}. Sector: "${s.name}"\n   ID: "${s.id}"${rolesHint}${kwHint}`
    })
    .join('\n\n')

  // 2. Prepare jobs with cleaned, readable descriptions
  const jobsList = jobs.map((j) => {
    const cleanDesc = (j.description || '')
      .replace(/<[^>]*>/g, ' ')
      .replace(/\s+/g, ' ')
      .trim()
    const truncatedDesc =
      cleanDesc.length > 1200 ? `${cleanDesc.slice(0, 1200)}...` : cleanDesc

    return {
      jobId: j.id,
      title: j.title,
      company: j.company || 'Unknown',
      location: j.location || 'Ethiopia',
      description: truncatedDesc,
    }
  })

  return `You are an expert job-classification assistant for Ethiopian and African employment markets.
Your task is to classify each of the following ${jobs.length} uncategorized job postings into EXACTLY ONE canonical sector from the platform's official taxonomy below.

### OFFICIAL CANONICAL SECTORS
${sectorListText}

### CLASSIFICATION RULES:
1. For each job, evaluate its title, company, and description, and pick the SINGLE best canonical sector from the official list above.
2. You MUST use the exact "sectorId" and "sectorName" provided in the list above. Do NOT invent new sectors.
3. If a job is ambiguous or general, pick the closest matching sector (e.g. "Business & Administration" or "Skilled & General Labor").
4. Output MUST be ONLY a valid JSON array of objects. Do not write any explanations, markdown commentary, or introductory text outside the JSON array.

### REQUIRED JSON SCHEMA:
[
  {
    "jobId": "<uuid of the job>",
    "sectorId": "<uuid of the matching sector>",
    "sectorName": "<canonical sector name>"
  }
]

### JOBS TO CLASSIFY:
${JSON.stringify(jobsList, null, 2)}
`
}

export default function CopyPromptModal({
  open,
  onClose,
  jobs,
  sectors,
  token,
  onSuccess,
}: CopyPromptModalProps) {
  const [copied, setCopied] = useState(false)
  const [showPromptPreview, setShowPromptPreview] = useState(false)
  const [jsonInput, setJsonInput] = useState('')
  const [parseError, setParseError] = useState<string | null>(null)
  const [saving, setSaving] = useState(false)
  const [progress, setProgress] = useState<{ current: number; total: number } | null>(
    null,
  )
  const [saveError, setSaveError] = useState<string | null>(null)

  // Generate the formatted prompt
  const generatedPrompt = useMemo(() => {
    if (!open || jobs.length === 0 || sectors.length === 0) return ''
    return buildSectorClassificationPrompt(jobs, sectors)
  }, [open, jobs, sectors])

  // Copy prompt to clipboard
  const handleCopyPrompt = async () => {
    if (!generatedPrompt) return
    try {
      await navigator.clipboard.writeText(generatedPrompt)
      setCopied(true)
      setTimeout(() => setCopied(false), 2500)
    } catch {
      // Fallback
      const textArea = document.createElement('textarea')
      textArea.value = generatedPrompt
      document.body.appendChild(textArea)
      textArea.select()
      document.execCommand('copy')
      document.body.removeChild(textArea)
      setCopied(true)
      setTimeout(() => setCopied(false), 2500)
    }
  }

  // Parse and match pasted JSON
  const parsedAssignments = useMemo((): ParsedAssignment[] => {
    setParseError(null)
    if (!jsonInput.trim()) return []

    let raw = jsonInput.trim()

    // 1. Strip markdown code fence if present
    const codeBlockMatch = raw.match(/```(?:json)?\s*([\s\S]*?)\s*```/)
    if (codeBlockMatch) {
      raw = codeBlockMatch[1].trim()
    } else {
      // Try finding outer array brackets
      const start = raw.indexOf('[')
      const end = raw.lastIndexOf(']')
      if (start !== -1 && end !== -1 && end > start) {
        raw = raw.substring(start, end + 1)
      }
    }

    let parsedArray: any[] = []
    try {
      const parsedData = JSON.parse(raw)
      if (Array.isArray(parsedData)) {
        parsedArray = parsedData
      } else if (parsedData && typeof parsedData === 'object') {
        // If wrapped in an object like { jobs: [...] } or { results: [...] }
        const possibleArray = Object.values(parsedData).find((val) =>
          Array.isArray(val),
        )
        if (possibleArray && Array.isArray(possibleArray)) {
          parsedArray = possibleArray
        } else {
          setParseError(
            'Expected a JSON array of job classifications, but received an object.',
          )
          return []
        }
      }
    } catch (err: any) {
      setParseError(`Invalid JSON format: ${err.message}`)
      return []
    }

    const jobMap = new Map<string, JobResponse>()
    jobs.forEach((j) => jobMap.set(j.id.toLowerCase(), j))

    const sectorIdMap = new Map<string, SectorDetailResponse>()
    const sectorNameMap = new Map<string, SectorDetailResponse>()
    sectors.forEach((s) => {
      sectorIdMap.set(s.id.toLowerCase(), s)
      sectorNameMap.set(s.name.toLowerCase(), s)
      if (s.slug) sectorNameMap.set(s.slug.toLowerCase(), s)
    })

    const results: ParsedAssignment[] = []

    for (const item of parsedArray) {
      if (!item || typeof item !== 'object') continue

      const jobId = (item.jobId || item.id || '').toString().toLowerCase()
      const matchedJob = jobMap.get(jobId)

      if (!matchedJob) {
        results.push({
          jobId: item.jobId || item.id || 'unknown',
          jobTitle: item.title || 'Unrecognized Job ID',
          company: '',
          targetSectorId: null,
          targetSectorName: item.sectorName || 'N/A',
          status: 'unknown_job',
        })
        continue
      }

      // Resolve sector by ID first, then fallback to name or slug
      const inputSectorId = (item.sectorId || '').toString().toLowerCase()
      const inputSectorName = (item.sectorName || item.sector || '')
        .toString()
        .toLowerCase()

      let matchedSector = sectorIdMap.get(inputSectorId)
      if (!matchedSector && inputSectorName) {
        matchedSector = sectorNameMap.get(inputSectorName)
      }

      if (matchedSector) {
        results.push({
          jobId: matchedJob.id,
          jobTitle: matchedJob.title,
          company: matchedJob.company || '',
          targetSectorId: matchedSector.id,
          targetSectorName: matchedSector.name,
          status: 'valid',
        })
      } else {
        results.push({
          jobId: matchedJob.id,
          jobTitle: matchedJob.title,
          company: matchedJob.company || '',
          targetSectorId: null,
          targetSectorName: item.sectorName || 'Unrecognized Sector',
          status: 'unknown_sector',
        })
      }
    }

    return results
  }, [jsonInput, jobs, sectors])

  const validAssignments = useMemo(
    () => parsedAssignments.filter((a) => a.status === 'valid'),
    [parsedAssignments],
  )

  // Save the valid assignments to the API in concurrent batches
  const handleResolveAndSave = async () => {
    if (!token || validAssignments.length === 0 || saving) return

    setSaving(true)
    setSaveError(null)
    setProgress({ current: 0, total: validAssignments.length })

    const resolvedIds: string[] = []
    const BATCH_SIZE = 5

    try {
      for (let i = 0; i < validAssignments.length; i += BATCH_SIZE) {
        const batch = validAssignments.slice(i, i + BATCH_SIZE)

        await Promise.all(
          batch.map(async (item) => {
            if (item.targetSectorId) {
              await setJobSector(item.jobId, item.targetSectorId, token)
              resolvedIds.push(item.jobId)
            }
          }),
        )

        setProgress({
          current: Math.min(i + batch.length, validAssignments.length),
          total: validAssignments.length,
        })
      }

      onSuccess(resolvedIds)
      onClose()
      setJsonInput('')
    } catch (err) {
      setSaveError(getApiErrorMessage(err))
    } finally {
      setSaving(false)
      setProgress(null)
    }
  }

  if (!open) return null

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4">
      {/* Backdrop */}
      <div
        className="absolute inset-0 bg-inverse-surface/40 backdrop-blur-sm"
        onClick={() => !saving && onClose()}
      />

      {/* Dialog Container */}
      <div className="relative z-10 flex max-h-[92vh] w-full max-w-4xl flex-col rounded-2xl border border-surface-variant bg-surface-container-lowest shadow-2xl">
        {/* Header */}
        <div className="flex shrink-0 items-center justify-between border-b border-surface-variant px-6 py-4">
          <div>
            <div className="flex items-center gap-2">
              <span className="material-symbols-outlined text-xl text-primary">
                psychology
              </span>
              <h2 className="font-headline-md text-headline-md font-bold text-on-surface">
                AI Classification Prompt & Resolver
              </h2>
            </div>
            <p className="mt-0.5 font-label-sm text-label-sm text-on-surface-variant">
              Copy the prompt to classify {jobs.length} uncategorized jobs in any AI model, then paste the response to resolve them all at once.
            </p>
          </div>
          <button
            type="button"
            onClick={onClose}
            disabled={saving}
            aria-label="Close"
            className="rounded-lg p-1.5 text-on-surface-variant transition-colors hover:bg-surface-container-high hover:text-on-surface disabled:opacity-50"
          >
            <span className="material-symbols-outlined text-xl">close</span>
          </button>
        </div>

        {/* Scrollable Body */}
        <div className="flex-1 space-y-6 overflow-y-auto px-6 py-5">
          {/* Step 1: Copy Prompt Card */}
          <div className="rounded-xl border border-surface-variant bg-surface-container-low p-4">
            <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
              <div>
                <div className="flex items-center gap-2">
                  <span className="flex h-6 w-6 items-center justify-center rounded-full bg-primary font-label-sm text-label-sm font-bold text-on-primary">
                    1
                  </span>
                  <h3 className="font-label-md text-label-md font-semibold text-on-surface">
                    Copy Prompt for External AI
                  </h3>
                  <span className="rounded-full bg-surface-container-highest px-2.5 py-0.5 font-label-sm text-label-sm text-on-surface-variant">
                    {jobs.length} jobs · {sectors.length} sectors
                  </span>
                </div>
                <p className="mt-1 font-body-sm text-body-sm text-on-surface-variant">
                  Includes full canonical sectors, sample roles, domain keywords, and job descriptions.
                </p>
              </div>

              <div className="flex items-center gap-2">
                <button
                  type="button"
                  onClick={() => setShowPromptPreview((p) => !p)}
                  className="inline-flex items-center gap-1 rounded-lg border border-surface-variant bg-surface-container-lowest px-3 py-2 font-label-sm text-label-sm font-medium text-on-surface hover:bg-surface-container-high"
                >
                  <span className="material-symbols-outlined text-base">
                    {showPromptPreview ? 'visibility_off' : 'visibility'}
                  </span>
                  {showPromptPreview ? 'Hide preview' : 'View prompt'}
                </button>

                <button
                  type="button"
                  onClick={handleCopyPrompt}
                  className={cn(
                    'inline-flex items-center gap-1.5 rounded-lg px-4 py-2 font-label-md text-label-md font-medium transition-all shadow-sm',
                    copied
                      ? 'bg-emerald-600 text-white'
                      : 'bg-primary text-on-primary hover:opacity-90',
                  )}
                >
                  <span className="material-symbols-outlined text-lg">
                    {copied ? 'check' : 'content_copy'}
                  </span>
                  {copied ? 'Copied to clipboard!' : 'Copy prompt'}
                </button>
              </div>
            </div>

            {/* Prompt preview expansion */}
            {showPromptPreview && (
              <div className="mt-3">
                <div className="max-h-56 overflow-y-auto rounded-lg border border-surface-variant bg-surface-container-lowest p-3 font-mono text-xs text-on-surface-variant whitespace-pre-wrap select-all">
                  {generatedPrompt}
                </div>
              </div>
            )}
          </div>

          {/* Step 2: Paste Response & Resolve Card */}
          <div className="rounded-xl border border-surface-variant bg-surface-container-lowest p-4 shadow-sm">
            <div className="flex items-center gap-2 mb-2">
              <span className="flex h-6 w-6 items-center justify-center rounded-full bg-primary font-label-sm text-label-sm font-bold text-on-primary">
                2
              </span>
              <h3 className="font-label-md text-label-md font-semibold text-on-surface">
                Paste AI JSON Response
              </h3>
            </div>
            <p className="font-body-sm text-body-sm text-on-surface-variant mb-3">
              Paste the AI's JSON output below. Markdown code fences (<code className="rounded bg-surface-container-high px-1">```json</code>) are automatically parsed.
            </p>

            <textarea
              rows={6}
              value={jsonInput}
              onChange={(e) => setJsonInput(e.target.value)}
              placeholder={`Paste the AI's JSON response here...\n\nExample:\n[\n  {\n    "jobId": "${jobs[0]?.id || 'uuid'}",\n    "sectorId": "${sectors[0]?.id || 'uuid'}",\n    "sectorName": "${sectors[0]?.name || 'Technology & IT'}"\n  }\n]`}
              className="w-full rounded-xl border border-outline-variant bg-surface-container-lowest p-3 font-mono text-xs text-on-surface placeholder:text-on-surface-variant/50 focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary"
            />

            {/* Parsing error notification */}
            {parseError && (
              <div className="mt-2.5 flex items-center gap-2 rounded-lg border border-error/40 bg-error-container/50 px-3 py-2 font-label-sm text-label-sm text-on-error-container">
                <span className="material-symbols-outlined text-base text-error">
                  error
                </span>
                <span>{parseError}</span>
              </div>
            )}

            {/* Parse result statistics */}
            {jsonInput.trim() && !parseError && (
              <div className="mt-3 flex flex-wrap items-center justify-between gap-2 rounded-lg border border-surface-variant bg-surface-container-low px-3 py-2">
                <div className="flex items-center gap-3">
                  <span className="flex items-center gap-1 font-label-sm text-label-sm font-medium text-emerald-700">
                    <span className="material-symbols-outlined text-base text-emerald-600">
                      check_circle
                    </span>
                    {validAssignments.length} recognized & ready to save
                  </span>

                  {parsedAssignments.length - validAssignments.length > 0 && (
                    <span className="flex items-center gap-1 font-label-sm text-label-sm font-medium text-amber-700">
                      <span className="material-symbols-outlined text-base text-amber-600">
                        warning
                      </span>
                      {parsedAssignments.length - validAssignments.length} skipped / unrecognized
                    </span>
                  )}
                </div>

                <span className="font-label-sm text-label-sm text-on-surface-variant">
                  {jobs.length} jobs in queue
                </span>
              </div>
            )}

            {/* Preview of recognized assignments */}
            {validAssignments.length > 0 && (
              <div className="mt-3">
                <h4 className="font-label-sm text-label-sm font-semibold text-on-surface-variant mb-1.5">
                  Resolution Preview ({validAssignments.length} items):
                </h4>
                <div className="max-h-48 overflow-y-auto divide-y divide-surface-variant rounded-lg border border-surface-variant bg-surface-container-lowest px-3">
                  {validAssignments.map((a) => (
                    <div
                      key={a.jobId}
                      className="flex items-center justify-between gap-3 py-2 font-body-sm text-body-sm"
                    >
                      <div className="min-w-0 flex-1">
                        <p className="truncate font-medium text-on-surface">
                          {a.jobTitle}
                        </p>
                        {a.company && (
                          <p className="truncate text-xs text-on-surface-variant">
                            {a.company}
                          </p>
                        )}
                      </div>
                      <span className="shrink-0 rounded-full bg-primary/10 px-2.5 py-0.5 font-label-sm text-label-sm font-medium text-primary">
                        {a.targetSectorName}
                      </span>
                    </div>
                  ))}
                </div>
              </div>
            )}

            {/* Save error */}
            {saveError && (
              <div className="mt-3 flex items-center gap-2 rounded-lg border border-error/40 bg-error-container px-3 py-2 font-label-sm text-label-sm text-on-error-container">
                <span className="material-symbols-outlined text-base">error</span>
                <span>{saveError}</span>
              </div>
            )}

            {/* Progress indicator while saving */}
            {saving && progress && (
              <div className="mt-3 space-y-1.5">
                <div className="flex justify-between font-label-sm text-label-sm text-on-surface-variant">
                  <span>Saving sector assignments…</span>
                  <span>
                    {progress.current} / {progress.total}
                  </span>
                </div>
                <div className="h-2 w-full overflow-hidden rounded-full bg-surface-container-high">
                  <div
                    className="h-full bg-primary transition-all duration-200"
                    style={{
                      width: `${(progress.current / progress.total) * 100}%`,
                    }}
                  />
                </div>
              </div>
            )}
          </div>
        </div>

        {/* Footer */}
        <div className="flex shrink-0 items-center justify-end gap-3 border-t border-surface-variant px-6 py-4">
          <button
            type="button"
            onClick={onClose}
            disabled={saving}
            className="rounded-lg border border-surface-variant px-5 py-2.5 font-label-md text-label-md font-medium text-on-surface transition-colors hover:bg-surface-container-high disabled:opacity-50"
          >
            Cancel
          </button>

          <button
            type="button"
            onClick={handleResolveAndSave}
            disabled={saving || validAssignments.length === 0}
            className="inline-flex items-center gap-2 rounded-lg bg-primary px-6 py-2.5 font-label-md text-label-md font-medium text-on-primary shadow-sm transition-opacity hover:opacity-90 disabled:pointer-events-none disabled:opacity-50"
          >
            {saving ? (
              <>
                <span
                  aria-hidden="true"
                  className="h-4 w-4 animate-spin rounded-full border-2 border-on-primary/40 border-t-on-primary"
                />
                Saving…
              </>
            ) : (
              <>
                <span className="material-symbols-outlined text-lg">
                  task_alt
                </span>
                Save & Resolve ({validAssignments.length})
              </>
            )}
          </button>
        </div>
      </div>
    </div>
  )
}
