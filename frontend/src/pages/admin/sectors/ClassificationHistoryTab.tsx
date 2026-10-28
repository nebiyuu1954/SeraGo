import { useCallback, useEffect, useState } from 'react'
import {
  fetchClassificationHistory,
  setJobSector,
  fetchProviderFallbacks,
  getApiErrorMessage,
} from '../../../api'
import type {
  ProviderFallbacksResponse,
} from '../../../api'
import type {
  ClassificationHistoryItem,
  ClassificationStats,
  SectorDetailResponse,
} from '../../../types'
import { cn } from '../../../lib/cn.ts'
import BatchCorrectionModal from './BatchCorrectionModal.tsx'

interface ClassificationHistoryTabProps {
  sectors: SectorDetailResponse[]
  token: string
}

function getTodayString(): string {
  const now = new Date()
  const year = now.getFullYear()
  const month = String(now.getMonth() + 1).padStart(2, '0')
  const day = String(now.getDate()).padStart(2, '0')
  return `${year}-${month}-${day}`
}

function generateDailyAuditPrompt(
  selectedDate: string,
  jobs: ClassificationHistoryItem[],
  sectors: SectorDetailResponse[],
): string {
  const canonicalSectors = sectors
    .filter((s) => s.isActive)
    .map((s) => `- ${s.name} (slug: "${s.slug}")`)
    .join('\n')

  const formattedJobs = jobs
    .map((job, idx) => {
      const rawTags = [
        ...(job.sourceSectors || []),
        ...(job.ethioCategories || []),
      ].filter(Boolean)

      const rawTagStr = rawTags.length > 0 ? rawTags.join(', ') : 'None / Untagged'
      const assigned = job.sectorName || 'Uncategorized'
      const method = formatMethodLabel(job.classificationMethod)
      const details = job.classificationDetails || ''
      const conf = job.confidence != null ? `${Math.round(job.confidence * 100)}%` : 'N/A'
      const snippet = (job.descriptionPreview || 'No description available')
        .replace(/\s+/g, ' ')
        .trim()
        .slice(0, 350)

      return `[Job #${idx + 1}]
- ID: ${job.id}
- Title: ${job.title}
- Company: ${job.company || 'Undisclosed'}
- Location: ${job.location || 'N/A'}
- Source: ${job.sourceName || 'Direct'}
- Before (Scraper Raw Tag): ${rawTagStr}
- After (Assigned Sector): ${assigned} (slug: "${job.sectorSlug || 'null'}")
- Classification Method: ${method} (${details})
- Confidence: ${conf}
- Reasoning: ${job.reasoning || 'N/A'}
- Description Snippet: ${snippet}...`
    })
    .join('\n\n---\n\n')

  return `You are an expert Chief Job Classification Auditor for SeraGo, Ethiopia's premier career marketplace.
Below is the batch of ${jobs.length} jobs classified on ${selectedDate}.

### CANONICAL SERAGO SECTORS:
${canonicalSectors}

### CRITICAL EVALUATION GUIDELINES:
1. Compare each job's Title, Company, Scraper Raw Tag (Before), and Description Snippet against the Assigned Sector (After).
2. Key classification disambiguation rules:
   - "SDR" / "Sales Development Representative" / "BPO cold calling" / "Telemarketer" / "Account Executive" -> MUST be "Sales & Marketing", NOT "Technology & IT" (even if the company is an IT company).
   - "Crane Rigger" / "Equipment Operator" / "Welder" / "Mason" / "Carpenter" / "General Labor" / "Auto Mechanic" / "Automotive Electrical Technician" / "EV Service Technician" -> MUST be "Skilled & General Labor" or "Engineering & Construction", NOT "Technology & IT".
   - "Real Estate Project Manager" / "Real Estate Development Manager" -> MUST be "Engineering & Construction", NOT "Sales & Marketing" (oversees physical site construction & contractor delivery, distinct from real estate sales agents).
   - "Accountant" / "Cashier" / "Auditor" / "Finance Officer" / Amharic ("አካውንታንት", "ሲኒየር አካውንታንት", "ሲኒዬር አካውንታንት", "የሂሳብ ባለሙያ", "ካሸር", "ገንዘብ ያዥ") -> MUST ALWAYS be "Accounting & Finance", even if employed by a school, training institute, factory, or hospital!
   - "Graphic Designer" / "UI/UX Designer" / "Motion Graphics" -> MUST be "Design & Creative".
   - "Video Editor" / "Social Media Manager" / "Content Writer" / "Journalist" -> MUST be "Media & Communication".
   - "Terms of Reference (TOR)" / Consultancy for developing accessible communication, multimedia, or Braille -> MUST be "Media & Communication", NOT "Procurement & Supply Chain".
   - "Civil Engineer" / "Site Supervisor" / "Draftsman" / "Structural Engineer" -> MUST be "Engineering & Construction".
   - "Tender Officer" / "Bidding Officer" for commercial goods, lab equipment, or trading -> MUST be "Procurement & Supply Chain", NOT "Engineering & Construction".
   - "Software Engineer" / "Backend Developer" / "Data Analyst" / "System Admin" / Telecom & Software Platforms ("OCS Billing Engineer", "Charging System", "RBT Operation", "VAS Specialist", "Core Network Product Engineer") -> MUST be "Technology & IT", NEVER "Engineering & Construction".
   - "Doctor" / "Nurse" / "Pharmacist" / "Health Officer" / "Medical Internist" -> MUST be "Healthcare".
   - "Lab Technician" in schools, academies, or educational institutions -> MUST be "Education & Training", NOT "Healthcare" (educational science lab, not clinical pathology).
   - "Safety Health Environment & Quality Manager (SHEQM)" in factories/manufacturing -> MUST be "Manufacturing & Production", NOT "Healthcare" (industrial QA & occupational safety).
   - "Green Development" in industrial parks/agro-industry -> MUST be "Agriculture & Natural Science", NOT "Engineering & Construction".
   - Bank Branch Managers / Credit Officers / Loan Officers / Relationship Manager-Credit -> MUST be "Banking & Insurance", NOT "Business & Administration" or "Accounting & Finance".
   - NGO Livelihoods / MEAL / Economic Strengthening -> MUST be "Social Science & Community", NOT "Accounting & Finance".
   - Retail & Commercial centers: "የሱቅ ሱፐርቫይዘር" (Shop/Retail Supervisor) -> MUST be "Sales & Marketing", NOT "Hospitality & Tourism".
   - Entertainment & Arcades: "የጌም ዞን" / Game Zone / Mall Entertainment Center -> MUST be "Hospitality & Tourism".
   - "የገበያ ጥናትና ልማት መምሪያ ሥራ አስኪያጅ" / Market Research & Business Development -> MUST be "Sales & Marketing", regardless of employer (church, school, or NGO).

### AUDIT TASK:
1. **Tier-by-Tier Error Count & Accuracy Breakdown:**
   - First, count how many jobs were wronged by each classification tier:
     * **LLM (Tier 1 OpenRouter Nemotron / Tier 2 Groq / Tier 3 Gemini)**
     * **Vector Semantic Fallback (Tier 5 Cosine Similarity)**
     * **Direct Tag Match (Scraper Direct Alias Rule)**
   - Explain what is not accurate for each tier, and calculate the accuracy rate per tier as well as the **overall batch accuracy percentage**.
2. **Itemized Audit of Misclassifications:**
   - For each misclassified or questionable job, specify:
     * Job # and Title
     * Current Sector
     * Recommended Correct Sector (from the canonical list)
     * Reason why it is misclassified and why the recommended sector is superior.
3. **Prompt & Code Engineering Recommendations:**
   - Detail the exact code and prompt changes needed in the backend (\`UnifiedJobClassificationService.cs\` system prompt and classification pipeline) so that today's wrong classifications can be permanently prevented for the overall system.
4. **1-Click Batch Fix JSON Payload:**
   - At the very end of your response, provide a clean JSON code block for SeraGo's 1-Click Batch Fix Tool containing ALL misclassified jobs formatted as:
\`\`\`json
[
  {
    "jobId": "<UUID>",
    "title": "<Job Title>",
    "company": "<Company Name>",
    "beforeSector": "<Current Sector>",
    "sectorSlug": "<canonical-slug>"
  }
]
\`\`\`
5. **Follow-Up Consultation:**
   - Always conclude your audit by proactively asking the user what follow-up prompt/rule refinements or code implementations they would like you to execute next.

### BATCH OF JOBS CLASSIFIED ON ${selectedDate} (${jobs.length} TOTAL):
${formattedJobs}
`
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
  const [pageSize, setPageSize] = useState<number>(20)
  const [totalPages, setTotalPages] = useState(1)
  const [totalCount, setTotalCount] = useState(0)
  const [search, setSearch] = useState('')
  const [debouncedSearch, setDebouncedSearch] = useState('')
  const [selectedSectorId, setSelectedSectorId] = useState<string>('')
  const [selectedMethod, setSelectedMethod] = useState<string>('')

  // Day filter & Prompt export
  const [selectedDate, setSelectedDate] = useState<string>(getTodayString())
  const [filterByDate, setFilterByDate] = useState<boolean>(false)
  const [isExportingPrompt, setIsExportingPrompt] = useState(false)
  const [dailyPromptCopied, setDailyPromptCopied] = useState(false)
  const [showBatchModal, setShowBatchModal] = useState(false)

  // Real-Time Provider Fallback Health & Transition Logs
  const [providerData, setProviderData] = useState<ProviderFallbacksResponse | null>(null)
  const [loadingProviders, setLoadingProviders] = useState(false)
  const [showFallbackEvents, setShowFallbackEvents] = useState(false)

  const loadProviderData = useCallback(async () => {
    if (!token) return
    setLoadingProviders(true)
    try {
      const data = await fetchProviderFallbacks(filterByDate ? selectedDate : undefined, token)
      setProviderData(data)
    } catch {
      // Non-blocking
    } finally {
      setLoadingProviders(false)
    }
  }, [filterByDate, selectedDate, token])

  useEffect(() => {
    loadProviderData()
  }, [loadProviderData])

  // Selection & UI state
  const [selectedJobIds, setSelectedJobIds] = useState<Set<string>>(new Set())
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
      classificationMethod: formatMethodLabel(job.classificationMethod),
      classificationDetails: job.classificationDetails,
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
- **Classification Method:** ${formatMethodLabel(job.classificationMethod)} (${job.classificationDetails})
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

  // Handle Gathering all jobs classified on selectedDate and copying AI prompt
  const handleCopyDailyPrompt = async () => {
    if (!selectedDate || isExportingPrompt) return
    setIsExportingPrompt(true)
    setError(null)
    try {
      // Fetch all jobs classified on that day (up to 500)
      const res = await fetchClassificationHistory(
        {
          date: selectedDate,
          pageSize: 500,
          page: 1,
        },
        token,
      )

      if (!res.items || res.items.length === 0) {
        setToastMessage(`No jobs found classified on ${selectedDate}. Try selecting another date.`)
        setTimeout(() => setToastMessage(null), 4000)
        return
      }

      const prompt = generateDailyAuditPrompt(selectedDate, res.items, sectors)
      await navigator.clipboard.writeText(prompt)

      setDailyPromptCopied(true)
      setToastMessage(
        `Copied prompt with ${res.items.length} jobs classified on ${selectedDate} to clipboard! Paste it into ChatGPT, Claude, or Gemini.`,
      )

      setTimeout(() => {
        setDailyPromptCopied(false)
      }, 3500)
      setTimeout(() => {
        setToastMessage(null)
      }, 6000)
    } catch (err) {
      setError(`Failed to gather jobs for ${selectedDate}: ${getApiErrorMessage(err)}`)
    } finally {
      setIsExportingPrompt(false)
    }
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
          pageSize,
          sectorId: selectedSectorId || undefined,
          method: selectedMethod || undefined,
          date: filterByDate ? selectedDate : undefined,
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
  }, [debouncedSearch, filterByDate, page, pageSize, selectedDate, selectedMethod, selectedSectorId, token])

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

  const startRecord = totalCount === 0 ? 0 : (page - 1) * pageSize + 1
  const endRecord = Math.min(page * pageSize, totalCount)

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

      {/* Real-Time AI Provider Health & Fallback Switch Tracker */}
      <div className="rounded-xl border border-outline-variant/60 bg-surface-container-lowest p-4 shadow-sm space-y-4">
        {/* Header bar */}
        <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3">
          <div className="flex items-center gap-2.5">
            <div className="flex h-9 w-9 items-center justify-center rounded-lg bg-purple-100 text-purple-700">
              <span className="material-symbols-outlined text-xl">hub</span>
            </div>
            <div>
              <div className="flex items-center gap-2">
                <h3 className="font-title-sm text-title-sm font-bold text-on-surface">
                  AI Provider Pipeline & Real-Time Transition Log
                </h3>
                <span className="inline-flex items-center gap-1 rounded-full bg-emerald-100 px-2 py-0.5 text-[11px] font-bold text-emerald-800">
                  <span className="h-1.5 w-1.5 rounded-full bg-emerald-600 animate-pulse" />
                  Live Sync
                </span>
              </div>
              <p className="font-body-sm text-body-sm text-on-surface-variant">
                Instant audit trail of model switches, rate-limits (HTTP 429), timeouts, and failovers
              </p>
            </div>
          </div>

          <div className="flex items-center gap-2 self-end sm:self-auto">
            <button
              type="button"
              onClick={() => setShowFallbackEvents(!showFallbackEvents)}
              className={cn(
                'inline-flex items-center gap-1.5 rounded-lg border px-3 py-1.5 font-label-sm text-label-sm font-semibold transition-colors shadow-xs',
                showFallbackEvents
                  ? 'border-purple-600 bg-purple-700 text-white'
                  : 'border-outline-variant bg-surface hover:bg-surface-container-low text-on-surface',
              )}
            >
              <span className="material-symbols-outlined text-base">
                {showFallbackEvents ? 'visibility_off' : 'history'}
              </span>
              <span>
                {showFallbackEvents ? 'Hide Log' : 'View Switch Log'}
                {providerData && providerData.eventsCount > 0 && (
                  <span
                    className={cn(
                      'ml-1.5 rounded-full px-1.5 py-0.2 text-[10px] font-bold',
                      showFallbackEvents
                        ? 'bg-purple-900 text-purple-100'
                        : 'bg-amber-100 text-amber-800',
                    )}
                  >
                    {providerData.eventsCount}
                  </span>
                )}
              </span>
            </button>

            <button
              type="button"
              onClick={loadProviderData}
              disabled={loadingProviders}
              title="Refresh provider status"
              className="inline-flex h-8 w-8 items-center justify-center rounded-lg border border-outline-variant bg-surface hover:bg-surface-container-low text-on-surface-variant hover:text-on-surface transition-colors disabled:opacity-50"
            >
              <span className={cn('material-symbols-outlined text-base', loadingProviders && 'animate-spin')}>
                refresh
              </span>
            </button>
          </div>
        </div>

        {/* 5-Tier Health Pills Grid */}
        <div className="grid grid-cols-2 md:grid-cols-5 gap-2.5">
          {providerData?.providers?.map((p) => {
            const isDegraded = p.status === 'degraded'
            const isWarning = p.status === 'warning'

            return (
              <div
                key={p.name}
                className={cn(
                  'rounded-lg border p-2.5 transition-all',
                  isDegraded
                    ? 'border-red-300 bg-red-50/60'
                    : isWarning
                    ? 'border-amber-300 bg-amber-50/60'
                    : 'border-outline-variant/50 bg-surface-container-low/60 hover:bg-surface-container-low',
                )}
              >
                <div className="flex items-center justify-between gap-1 mb-1">
                  <span className="text-[10px] font-bold uppercase tracking-wider text-on-surface-variant">
                    {p.tier}
                  </span>
                  <span
                    className={cn(
                      'inline-flex items-center gap-1 rounded-full px-1.5 py-0.2 text-[10px] font-semibold',
                      isDegraded
                        ? 'bg-red-100 text-red-800'
                        : isWarning
                        ? 'bg-amber-100 text-amber-800'
                        : 'bg-emerald-100 text-emerald-800',
                    )}
                  >
                    <span
                      className={cn(
                        'h-1.5 w-1.5 rounded-full',
                        isDegraded
                          ? 'bg-red-600'
                          : isWarning
                          ? 'bg-amber-600 animate-pulse'
                          : 'bg-emerald-600',
                      )}
                    />
                    {isDegraded ? 'Throttled' : isWarning ? 'Degraded' : 'Active'}
                  </span>
                </div>

                <p className="font-label-md text-label-md font-bold text-on-surface truncate" title={p.name}>
                  {p.name}
                </p>

                <div className="mt-1.5 flex items-center justify-between text-[11px] text-on-surface-variant">
                  <span>
                    Classified: <strong>{p.jobsClassifiedToday}</strong>
                  </span>
                  {p.failoversToday > 0 && (
                    <span className="text-amber-700 font-semibold" title={p.lastFailureReason || undefined}>
                      {p.failoversToday} switch{p.failoversToday > 1 ? 'es' : ''}
                    </span>
                  )}
                </div>
              </div>
            )
          })}
        </div>

        {/* Real-Time Fallback Events Table / History Card */}
        {showFallbackEvents && (
          <div className="rounded-lg border border-outline-variant/60 bg-surface-container-lowest overflow-hidden transition-all animate-fadeIn">
            <div className="border-b border-outline-variant/40 bg-surface-container-low/40 px-3 py-2 flex items-center justify-between">
              <span className="font-label-sm text-label-sm font-bold text-on-surface flex items-center gap-1.5">
                <span className="material-symbols-outlined text-amber-600 text-base">emergency_home</span>
                Recorded Provider Transitions & Failover Audit ({providerData?.events?.length || 0})
              </span>
              <span className="text-[11px] text-on-surface-variant">
                Target Date: {providerData?.date || selectedDate}
              </span>
            </div>

            {(!providerData?.events || providerData.events.length === 0) ? (
              <div className="p-6 text-center">
                <div className="inline-flex h-10 w-10 items-center justify-center rounded-full bg-emerald-100 text-emerald-700 mb-2">
                  <span className="material-symbols-outlined text-2xl">verified</span>
                </div>
                <p className="font-label-md text-label-md font-bold text-on-surface">
                  Zero Failovers Detected
                </p>
                <p className="font-body-sm text-body-sm text-on-surface-variant max-w-md mx-auto mt-0.5">
                  All job classifications on this day were handled directly by the primary model without falling back to secondary providers or vector similarity.
                </p>
              </div>
            ) : (
              <div className="overflow-x-auto max-h-72 overflow-y-auto">
                <table className="w-full text-left font-body-sm text-body-sm border-collapse">
                  <thead className="bg-surface-container-low text-[11px] font-bold uppercase tracking-wider text-on-surface-variant sticky top-0 z-10 border-b border-outline-variant/50">
                    <tr>
                      <th className="px-3 py-2">Time</th>
                      <th className="px-3 py-2">Attempted Tier</th>
                      <th className="px-3 py-2">Failure Reason & Error</th>
                      <th className="px-3 py-2">Switched To</th>
                      <th className="px-3 py-2">Jobs Affected</th>
                      <th className="px-3 py-2">Outcome</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-outline-variant/30 text-on-surface">
                    {providerData.events.map((evt) => (
                      <tr key={evt.id} className="hover:bg-surface-container-low/50 transition-colors">
                        <td className="px-3 py-2 font-mono text-[11px] whitespace-nowrap text-on-surface-variant">
                          {new Date(evt.occurredAt).toLocaleTimeString([], {
                            hour: '2-digit',
                            minute: '2-digit',
                            second: '2-digit',
                          })}
                        </td>
                        <td className="px-3 py-2 whitespace-nowrap">
                          <span className="inline-flex items-center gap-1 font-semibold text-red-700 bg-red-50 rounded px-1.5 py-0.5 text-xs">
                            <span className="material-symbols-outlined text-xs">cancel</span>
                            {evt.failedProvider}
                          </span>
                        </td>
                        <td className="px-3 py-2 max-w-xs">
                          <div className="flex flex-col">
                            <span className="font-semibold text-xs text-red-900">
                              {evt.errorCategory} {evt.httpStatusCode ? `(HTTP ${evt.httpStatusCode})` : ''}
                            </span>
                            <span
                              className="text-[11px] text-on-surface-variant truncate font-mono"
                              title={evt.errorMessage}
                            >
                              {evt.errorMessage}
                            </span>
                          </div>
                        </td>
                        <td className="px-3 py-2 whitespace-nowrap">
                          <span className="inline-flex items-center gap-1 font-semibold text-purple-800 bg-purple-50 rounded px-1.5 py-0.5 text-xs">
                            <span className="material-symbols-outlined text-xs">arrow_forward</span>
                            {evt.fallbackTier}
                          </span>
                        </td>
                        <td className="px-3 py-2 whitespace-nowrap">
                          <span className="font-semibold">{evt.jobCount} jobs</span>
                          {evt.jobTitlesSnippet && (
                            <span
                              className="block text-[10px] text-on-surface-variant max-w-[200px] truncate"
                              title={evt.jobTitlesSnippet}
                            >
                              {evt.jobTitlesSnippet}
                            </span>
                          )}
                        </td>
                        <td className="px-3 py-2 whitespace-nowrap">
                          {evt.wasRecovered ? (
                            <span className="inline-flex items-center gap-1 text-emerald-700 bg-emerald-50 rounded-full px-2 py-0.5 text-xs font-semibold">
                              <span className="material-symbols-outlined text-xs">check_circle</span>
                              Recovered
                            </span>
                          ) : (
                            <span className="inline-flex items-center gap-1 text-red-700 bg-red-50 rounded-full px-2 py-0.5 text-xs font-semibold">
                              <span className="material-symbols-outlined text-xs">warning</span>
                              Degraded
                            </span>
                          )}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </div>
        )}
      </div>

      {/* 2. Search & Filter Bar */}
      <div className="rounded-xl border border-surface-variant bg-surface-container-lowest p-4 shadow-sm space-y-3">
        <div className="flex flex-col md:flex-row items-stretch md:items-center gap-3">
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
            <option value="openrouter">NVIDIA Nemotron 3 Ultra (Tier 1 OpenRouter)</option>
            <option value="groq">Meta Llama 3.3 70B (Tier 2 Groq)</option>
            <option value="gemini">Google Gemini 2.0 Flash (Tier 3 Gemini)</option>
            <option value="ollama">Local Ollama (Tier 4 Ollama)</option>
            <option value="cosine">Cosine Similarity (Tier 5 Vector)</option>
            <option value="adminmanual">Admin Manual Override</option>
            <option value="alias">Scraper Direct Alias Rule</option>
            <option value="uncategorized">Uncategorized</option>
          </select>
        </div>
      </div>

      {/* 3. Daily Batch Export & Date Picker Toolbar (Replaces AI Judge Button) */}
      <div className="flex flex-col lg:flex-row items-stretch lg:items-center justify-between gap-4 rounded-xl border border-purple-200 bg-gradient-to-r from-purple-50/70 via-indigo-50/50 to-purple-50/70 p-4 shadow-sm">
        {/* Left: Day Selector & Filter */}
        <div className="flex flex-wrap items-center gap-3">
          <div className="flex items-center gap-2">
            <span className="material-symbols-outlined text-purple-700 text-xl">calendar_month</span>
            <label htmlFor="daily-date-picker" className="font-label-md text-label-md font-bold text-on-surface">
              Choose Day:
            </label>
            <input
              id="daily-date-picker"
              type="date"
              value={selectedDate}
              onChange={(e) => {
                setSelectedDate(e.target.value)
                setPage(1)
              }}
              className="rounded-lg border border-purple-300 bg-white px-3 py-1.5 font-label-md text-label-md font-semibold text-purple-950 shadow-xs focus:border-purple-600 focus:outline-none focus:ring-1 focus:ring-purple-600"
            />
          </div>

          {/* Quick jump to Today */}
          {selectedDate !== getTodayString() && (
            <button
              type="button"
              onClick={() => {
                setSelectedDate(getTodayString())
                setPage(1)
              }}
              className="inline-flex items-center gap-1 rounded-md bg-purple-100 hover:bg-purple-200 text-purple-800 px-2.5 py-1 text-xs font-semibold transition-colors"
            >
              <span className="material-symbols-outlined text-sm">today</span>
              <span>Set to Today</span>
            </button>
          )}

          {/* Toggle filter list by selected day */}
          <button
            type="button"
            onClick={() => {
              setFilterByDate(!filterByDate)
              setPage(1)
            }}
            className={cn(
              'inline-flex items-center gap-1.5 rounded-lg border px-3 py-1.5 font-label-sm text-label-sm font-semibold transition-colors shadow-xs',
              filterByDate
                ? 'border-purple-600 bg-purple-700 text-white'
                : 'border-purple-300 bg-white text-purple-900 hover:bg-purple-100/70',
            )}
          >
            <span className="material-symbols-outlined text-sm">
              {filterByDate ? 'check_box' : 'filter_alt'}
            </span>
            <span>{filterByDate ? `Filtered to ${selectedDate}` : 'Filter list by this day'}</span>
          </button>
        </div>

        {/* Right: Copy Daily Prompt Button */}
        <div className="flex items-center gap-2 flex-shrink-0">
          <button
            type="button"
            onClick={handleCopyDailyPrompt}
            disabled={isExportingPrompt || !selectedDate}
            title="Gather all jobs classified on this day and copy a comprehensive prompt to paste into ChatGPT, Claude, or Gemini"
            className={cn(
              'inline-flex items-center gap-2 rounded-xl px-5 py-2.5 font-label-md text-label-md font-bold text-white shadow-md transition-all disabled:opacity-50 disabled:pointer-events-none',
              dailyPromptCopied
                ? 'bg-emerald-600 hover:bg-emerald-700 ring-2 ring-emerald-400'
                : 'bg-gradient-to-r from-purple-700 via-indigo-700 to-purple-800 hover:from-purple-800 hover:to-indigo-800 shadow-purple-200',
            )}
          >
            <span className="material-symbols-outlined text-xl">
              {isExportingPrompt ? 'sync' : dailyPromptCopied ? 'check_circle' : 'content_copy'}
            </span>
            <span>
              {isExportingPrompt
                ? `Gathering jobs for ${selectedDate}…`
                : dailyPromptCopied
                ? `Copied Prompt to Clipboard!`
                : `Copy Prompt for ${selectedDate}`}
            </span>
          </button>

          {/* Quick Batch Corrections button */}
          <button
            type="button"
            onClick={() => setShowBatchModal(true)}
            className="inline-flex items-center gap-2 rounded-xl border border-purple-300 bg-white hover:bg-purple-100/80 text-purple-900 px-4 py-2.5 font-label-md text-label-md font-bold shadow-xs hover:shadow-sm transition-all"
            title="Paste JSON from Chief Auditor report to update misclassified sectors in one click"
          >
            <span className="material-symbols-outlined text-purple-700 text-xl">bolt</span>
            <span>Batch Fix</span>
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

      {/* Top Pagination Controls & Per-Page Selector */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3 px-1">
        <div className="flex items-center gap-2">
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
          {selectedJobIds.size > 0 && (
            <span className="text-xs font-bold text-purple-900 bg-purple-200 px-2.5 py-0.5 rounded-full">
              {selectedJobIds.size} selected
            </span>
          )}
        </div>

        <div className="flex items-center gap-4">
          <div className="flex items-center gap-2">
            <span className="font-label-sm text-xs font-semibold text-on-surface-variant">
              Per page:
            </span>
            <select
              value={pageSize}
              onChange={(e) => {
                setPageSize(Number(e.target.value))
                setPage(1)
              }}
              aria-label="Items per page"
              className="rounded-lg border border-outline-variant bg-surface-container-low px-2.5 py-1 text-xs font-semibold text-on-surface focus:border-primary focus:outline-none"
            >
              <option value={10}>10</option>
              <option value={20}>20</option>
              <option value={50}>50</option>
              <option value={100}>100</option>
            </select>
          </div>

          <span className="font-label-sm text-xs text-on-surface-variant font-medium">
            Showing {startRecord}–{endRecord} of {totalCount}
          </span>
        </div>
      </div>

      {/* 4. List of Job Classification Cards in 2-Column Grid */}
      {loading ? (
        <div className="flex items-center justify-center py-20">
          <div className="flex flex-col items-center gap-3">
            <span
              aria-hidden="true"
              className="h-9 w-9 animate-spin rounded-full border-3 border-purple-300 border-t-purple-700"
            />
            <span className="text-xs font-semibold text-on-surface-variant">
              Loading classification history…
            </span>
          </div>
        </div>
      ) : items.length === 0 ? (
        <div className="rounded-xl border border-surface-variant bg-surface-container-lowest p-12 text-center">
          <span className="material-symbols-outlined text-4xl text-on-surface-variant">folder_off</span>
          <p className="mt-2 font-headline-sm text-headline-sm font-semibold text-on-surface">
            No jobs found
          </p>
          <p className="mt-1 font-body-md text-body-md text-on-surface-variant">
            {filterByDate
              ? `No classifications found for ${selectedDate}. Try choosing a different date or disabling the day filter.`
              : 'Try adjusting your search query, sector filter, or method selection.'}
          </p>
        </div>
      ) : (
        <div className="grid grid-cols-1 lg:grid-cols-2 gap-4">
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
                  'flex flex-col justify-between rounded-xl border p-4.5 shadow-xs transition-all',
                  isSelected
                    ? 'border-purple-500 bg-purple-50/30 shadow-md ring-1 ring-purple-500'
                    : 'border-surface-variant bg-surface-container-lowest hover:border-outline-variant hover:shadow-sm',
                )}
              >
                <div>
                  {/* Top Bar: PROMINENT CLASSIFICATION METHOD BADGE & DATE */}
                  <div className="flex items-center justify-between gap-2 pb-2.5 mb-3 border-b border-surface-variant/70">
                    <div className="flex items-center gap-1.5 flex-wrap">
                      <span className="font-label-xs text-[11px] font-bold uppercase tracking-wider text-on-surface-variant">
                        Method:
                      </span>
                      <span
                        className={cn(
                          'inline-flex items-center gap-1 rounded-full px-2.5 py-0.5 font-bold text-xs border shadow-xs',
                          getMethodBadgeStyle(job.classificationMethod),
                        )}
                      >
                        <span className="material-symbols-outlined text-xs">
                          {getMethodIcon(job.classificationMethod)}
                        </span>
                        <span>{formatMethodLabel(job.classificationMethod)}</span>
                      </span>

                      {job.confidence != null && (
                        <span
                          className={cn(
                            'inline-flex items-center gap-0.5 rounded-full px-2 py-0.5 text-[11px] font-bold border shadow-xs',
                            job.confidence >= 0.85
                              ? 'bg-emerald-100 text-emerald-900 border-emerald-300'
                              : job.confidence >= 0.70
                              ? 'bg-amber-100 text-amber-900 border-amber-300'
                              : 'bg-rose-100 text-rose-900 border-rose-300',
                          )}
                        >
                          <span>{Math.round(job.confidence * 100)}% Conf.</span>
                        </span>
                      )}
                    </div>

                    {/* Classified / Updated Timestamp */}
                    <div className="flex items-center gap-1 text-[11px] text-on-surface-variant font-medium flex-shrink-0">
                      <span className="material-symbols-outlined text-xs">schedule</span>
                      <span>
                        {new Date(job.updatedAt || job.createdAt).toLocaleString(undefined, {
                          month: 'short',
                          day: 'numeric',
                          hour: '2-digit',
                          minute: '2-digit',
                        })}
                      </span>
                    </div>
                  </div>

                  {/* Header: Checkbox, Title, Employer, Source & Top Action Buttons */}
                  <div className="flex items-start justify-between gap-2">
                    <div className="flex items-start gap-2.5 min-w-0 flex-1">
                      <input
                        type="checkbox"
                        checked={isSelected}
                        onChange={() => handleToggleSelectItem(job.id)}
                        aria-label={`Select job ${job.title}`}
                        className="mt-1 h-4 w-4 rounded border-outline-variant text-purple-600 focus:ring-purple-500 flex-shrink-0"
                      />

                      <div className="min-w-0 flex-1">
                        <div className="flex flex-wrap items-center gap-1.5">
                          <h3 className="font-headline-sm text-base font-bold text-on-surface leading-tight">
                            {job.title}
                          </h3>

                          {job.sourceName && (
                            <span className="rounded-md bg-surface-container-high px-1.5 py-0.5 font-label-xs text-[11px] font-semibold text-on-surface-variant">
                              via {job.sourceName}
                            </span>
                          )}
                        </div>

                        <p className="font-label-md text-xs text-on-surface-variant mt-1">
                          <span className="font-semibold text-on-surface">
                            {job.company || 'Company Undisclosed'}
                          </span>
                          {job.location && ` · ${job.location}`}
                        </p>
                      </div>
                    </div>

                    {/* Top Action Buttons: Copy Debug Info + See in All Jobs */}
                    <div className="flex items-center gap-1.5 flex-shrink-0 self-start">
                      <button
                        type="button"
                        onClick={() => handleCopyDebugInfo(job)}
                        title="Copy job details, scraper tags, and classification log for prompt refinement"
                        className={cn(
                          'inline-flex items-center gap-1 rounded-lg border px-2.5 py-1 text-xs font-semibold transition-all shadow-xs',
                          copiedJobId === job.id
                            ? 'border-emerald-500 bg-emerald-50 text-emerald-800 ring-1 ring-emerald-500'
                            : 'border-surface-variant bg-surface-container-low text-on-surface hover:bg-surface-container-high',
                        )}
                      >
                        <span className="material-symbols-outlined text-sm">
                          {copiedJobId === job.id ? 'check' : 'content_copy'}
                        </span>
                        <span>{copiedJobId === job.id ? 'Copied' : 'Debug'}</span>
                      </button>

                      <a
                        href={`/dashboard/talent/jobs?view=all&search=${encodeURIComponent(job.title)}`}
                        target="_blank"
                        rel="noopener noreferrer"
                        title="Open in All Jobs tab with this job title prefilled in the search bar"
                        className="inline-flex items-center gap-1 rounded-lg border border-purple-200 bg-purple-50/70 px-2.5 py-1 text-xs font-semibold text-purple-900 hover:bg-purple-100 transition-colors shadow-xs"
                      >
                        <span>All Jobs</span>
                        <span className="material-symbols-outlined text-xs">open_in_new</span>
                      </a>
                    </div>
                  </div>

                  {/* Description Preview */}
                  {job.descriptionPreview && (
                    <div className="mt-2.5 text-xs text-on-surface/90 leading-relaxed">
                      <p className={cn(!isExpanded && 'line-clamp-2')}>
                        {job.descriptionPreview}
                      </p>
                      {job.descriptionPreview.length > 120 && (
                        <button
                          type="button"
                          onClick={() => toggleExpandDesc(job.id)}
                          className="mt-0.5 text-[11px] font-bold text-primary hover:underline inline-flex items-center gap-0.5"
                        >
                          <span>{isExpanded ? 'Show less' : 'Expand'}</span>
                          <span className="material-symbols-outlined text-xs">
                            {isExpanded ? 'expand_less' : 'expand_more'}
                          </span>
                        </button>
                      )}
                    </div>
                  )}

                  {/* Before (Scraper Tag) ➜ After (Classified Sector) Pipeline Box */}
                  <div className="mt-3 rounded-lg border border-outline-variant/60 bg-surface-container-low/70 p-3 text-xs">
                    <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-2">
                      {/* Before: Raw tag from the scraper */}
                      <div className="flex items-center gap-1.5 flex-wrap">
                        <span className="inline-flex items-center gap-1 font-label-xs text-[11px] font-bold uppercase tracking-wider text-on-surface-variant">
                          <span className="material-symbols-outlined text-xs text-amber-600">sell</span>
                          Before:
                        </span>
                        {rawTags.length > 0 ? (
                          rawTags.map((t) => (
                            <span
                              key={t}
                              className="rounded bg-amber-100/90 text-amber-900 border border-amber-300 px-2 py-0.5 font-mono text-[11px] font-semibold shadow-xs"
                            >
                              {t}
                            </span>
                          ))
                        ) : (
                          <span className="rounded bg-surface-container px-1.5 py-0.5 font-mono text-[11px] text-on-surface-variant italic">
                            Untagged
                          </span>
                        )}
                      </div>

                      {/* After: Classified To */}
                      <div className="flex items-center gap-1.5 flex-wrap sm:justify-end">
                        <span className="material-symbols-outlined text-sm text-purple-600 hidden sm:inline">
                          arrow_forward
                        </span>
                        <span className="inline-flex items-center gap-1 font-label-xs text-[11px] font-bold uppercase tracking-wider text-on-surface-variant">
                          <span className="material-symbols-outlined text-xs text-purple-600">flag</span>
                          After:
                        </span>
                        {job.sectorName ? (
                          <span className="rounded bg-purple-100 text-purple-900 border border-purple-300 px-2 py-0.5 text-[11px] font-bold shadow-xs">
                            {job.sectorName}
                          </span>
                        ) : (
                          <span className="rounded bg-rose-100 text-rose-900 border border-rose-300 px-2 py-0.5 text-[11px] font-bold shadow-xs">
                            Uncategorized
                          </span>
                        )}
                      </div>
                    </div>

                    {/* Classification details note */}
                    {job.classificationDetails && (
                      <div className="mt-2 pt-2 border-t border-outline-variant/30 flex items-center gap-1 text-[11px] text-on-surface-variant">
                        <span className="font-semibold text-on-surface">Details:</span>
                        <span>{job.classificationDetails}</span>
                      </div>
                    )}

                    {/* AI Reasoning / Data explanation */}
                    {job.reasoning && (
                      <div className="mt-1.5 rounded bg-surface-container/70 p-2 text-[11px] text-on-surface-variant flex items-start gap-1.5 border border-outline-variant/30">
                        <span className="material-symbols-outlined text-xs text-purple-600 flex-shrink-0 mt-0.5">
                          lightbulb
                        </span>
                        <p className="leading-snug">
                          <strong className="text-on-surface font-semibold">Reasoning: </strong>
                          {job.reasoning}
                        </p>
                      </div>
                    )}
                  </div>
                </div>

                {/* Card Footer: Manual Sector Override */}
                <div className="mt-3 flex items-center justify-between gap-2 pt-2.5 border-t border-surface-variant/60">
                  <span className="font-label-sm text-[11px] font-semibold text-on-surface-variant">
                    Manual Override:
                  </span>

                  <div className="flex items-center gap-1.5">
                    <select
                      value={currentAssignment}
                      onChange={(e) =>
                        setManualAssignments((prev) => ({
                          ...prev,
                          [job.id]: e.target.value,
                        }))
                      }
                      aria-label={`Assign sector to ${job.title}`}
                      className="rounded-lg border border-outline-variant bg-surface-container-low px-2 py-1 text-xs font-semibold text-on-surface focus:border-primary focus:outline-none"
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
                          className="rounded-lg bg-primary px-2.5 py-1 text-xs font-semibold text-on-primary hover:opacity-90 disabled:opacity-50"
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

      {/* 5. Bottom Pagination Controls */}
      {totalPages > 1 && (
        <div className="flex flex-col sm:flex-row items-center justify-between gap-3 pt-4 border-t border-surface-variant">
          <p className="font-label-sm text-label-sm text-on-surface-variant">
            Page {page} of {totalPages} ({totalCount} total jobs)
          </p>
          <div className="flex items-center gap-1.5">
            <button
              type="button"
              disabled={page <= 1 || loading}
              onClick={() => setPage(1)}
              className="rounded-lg border border-surface-variant px-2.5 py-1.5 font-label-sm text-label-sm font-medium text-on-surface hover:bg-surface-container-high disabled:opacity-40"
              title="First Page"
            >
              First
            </button>
            <button
              type="button"
              disabled={page <= 1 || loading}
              onClick={() => setPage((p) => Math.max(1, p - 1))}
              className="rounded-lg border border-surface-variant px-3 py-1.5 font-label-sm text-label-sm font-medium text-on-surface hover:bg-surface-container-high disabled:opacity-40"
            >
              Previous
            </button>

            <span className="px-2 font-label-sm text-label-sm font-bold text-primary">
              {page} / {totalPages}
            </span>

            <button
              type="button"
              disabled={page >= totalPages || loading}
              onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
              className="rounded-lg border border-surface-variant px-3 py-1.5 font-label-sm text-label-sm font-medium text-on-surface hover:bg-surface-container-high disabled:opacity-40"
            >
              Next
            </button>
            <button
              type="button"
              disabled={page >= totalPages || loading}
              onClick={() => setPage(totalPages)}
              className="rounded-lg border border-surface-variant px-2.5 py-1.5 font-label-sm text-label-sm font-medium text-on-surface hover:bg-surface-container-high disabled:opacity-40"
              title="Last Page"
            >
              Last
            </button>
          </div>
        </div>
      )}

      {/* 5. Batch Classification Correction Modal */}
      <BatchCorrectionModal
        open={showBatchModal}
        onClose={() => setShowBatchModal(false)}
        sectors={sectors}
        historyJobs={items}
        token={token}
        onSuccess={(count) => {
          setToastMessage(`Successfully updated ${count} jobs to their canonical sectors!`)
          loadHistory()
        }}
      />
    </div>
  )
}

function getMethodBadgeStyle(method: string): string {
  const m = method?.toLowerCase() || ''
  if (m.includes('openrouter') || m.includes('nemotron')) {
    return 'bg-emerald-100 text-emerald-950 border-emerald-300 font-bold'
  }
  if (m.includes('groq') || m.includes('llama')) {
    return 'bg-sky-100 text-sky-950 border-sky-300 font-bold'
  }
  if (m.includes('gemini')) {
    return 'bg-purple-100 text-purple-950 border-purple-300 font-bold'
  }
  if (m.includes('ollama')) {
    return 'bg-indigo-100 text-indigo-950 border-indigo-300 font-bold'
  }
  if (m.includes('cosine')) {
    return 'bg-teal-100 text-teal-950 border-teal-300 font-bold'
  }
  if (m.includes('admin') || m.includes('manual')) {
    return 'bg-amber-100 text-amber-950 border-amber-300 font-bold'
  }
  if (m.includes('alias')) {
    return 'bg-blue-100 text-blue-950 border-blue-300 font-semibold'
  }
  return 'bg-surface-variant text-on-surface-variant border-outline-variant'
}

function getMethodIcon(method: string): string {
  const m = method?.toLowerCase() || ''
  if (m.includes('openrouter') || m.includes('nemotron')) return 'smart_toy'
  if (m.includes('groq') || m.includes('llama')) return 'bolt'
  if (m.includes('gemini')) return 'auto_awesome'
  if (m.includes('ollama')) return 'terminal'
  if (m.includes('cosine')) return 'query_stats'
  if (m.includes('admin') || m.includes('manual')) return 'person_edit'
  if (m.includes('alias')) return 'link'
  return 'help_outline'
}

function formatMethodLabel(method: string): string {
  const m = method?.toLowerCase() || ''
  if (m.includes('openrouter') || m.includes('nemotron')) {
    return 'NVIDIA Nemotron 3 Ultra (Tier 1 OpenRouter)'
  }
  if (m.includes('8b')) {
    return 'Meta Llama 3.1 8B Instant (Tier 2-Fast Groq Cloud)'
  }
  if (m.includes('groq') || m.includes('llama')) {
    return 'Meta Llama 3.3 70B (Tier 2 Groq Cloud)'
  }
  if (m.includes('gemini')) {
    return 'Google Gemini 2.0 Flash (Tier 3 Gemini)'
  }
  if (m.includes('ollama')) {
    return 'Local Ollama (Tier 4 Ollama)'
  }
  if (m.includes('cosine')) {
    return 'Cosine Similarity (Tier 5 Vector Fallback)'
  }
  if (m.includes('admin') || m.includes('manual')) {
    return 'Admin Manual Override'
  }
  if (m.includes('alias')) {
    return 'Scraper Direct Alias Rule'
  }
  return method || 'Uncategorized'
}
