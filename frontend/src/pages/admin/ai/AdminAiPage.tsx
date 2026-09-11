import { useEffect, useState } from 'react'
import {
  fetchAiClassificationJobs,
  fetchAiClassificationStats,
  getApiErrorMessage,
  getStoredAuthTokens,
} from '../../../api'
import { useRequireRole } from '../../../hooks'
import type {
  AiClassificationDay,
  AiClassificationJobItem,
  AiClassificationJobsResponse,
  AiClassificationStatsResponse,
  AiClassificationWindowStats,
} from '../../../types'
import DashboardShell from '../../../components/dashboard/DashboardShell.tsx'
import { cn } from '../../../lib/cn.ts'

type AiTab = 'classification' | 'cover-letter'

const TABS: { id: AiTab; label: string; icon: string }[] = [
  { id: 'classification', label: 'AI Classification', icon: 'category' },
  { id: 'cover-letter', label: 'AI Cover Letter', icon: 'draft' },
]

const JOBS_PAGE_SIZE = 10

/** Fraction of the reasoning text shown inline in the table (the rest is behind the info button). */
const PREVIEW_RATIO = 0.2

/**
 * Admin AI page. Two horizontal views: AI Classification (live) and AI Cover
 * Letter (placeholder until that feature ships). The classification view shows
 * LLM usage — jobs processed and tokens sent/received per day, week, month and
 * all time — plus the per-job classify audit trail.
 */
export default function AdminAiPage() {
  const auth = useRequireRole('Admin')
  const [tab, setTab] = useState<AiTab>('classification')

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
        AI
      </h1>
      <p className="mt-1 font-body-md text-body-md text-on-surface-variant">
        LLM usage across SeraGo — every AI feature and what it costs in tokens.
      </p>

      {/* Horizontal view switcher */}
      <div
        role="tablist"
        aria-label="AI features"
        className="mt-6 flex flex-wrap items-center gap-1 rounded-xl border border-surface-variant bg-surface-container-low p-1"
      >
        {TABS.map((t) => {
          const active = tab === t.id
          return (
            <button
              key={t.id}
              type="button"
              role="tab"
              aria-selected={active}
              onClick={() => setTab(t.id)}
              className={cn(
                'flex items-center gap-2 rounded-lg px-4 py-2 font-label-md text-label-md transition-colors',
                active
                  ? 'bg-surface-container-lowest text-primary shadow-sm'
                  : 'text-on-surface-variant hover:text-on-surface',
              )}
            >
              <span className="material-symbols-outlined text-lg">{t.icon}</span>
              {t.label}
            </button>
          )
        })}
      </div>

      {tab === 'classification' ? <ClassificationTab /> : <CoverLetterTab />}
    </DashboardShell>
  )
}

// ------------------------------------------------------------ Classification

function ClassificationTab() {
  const [stats, setStats] = useState<AiClassificationStatsResponse | null>(null)
  const [jobs, setJobs] = useState<AiClassificationJobsResponse | null>(null)
  const [page, setPage] = useState(1)
  const [pageSize, setPageSize] = useState(JOBS_PAGE_SIZE)
  const [dailyPage, setDailyPage] = useState(1)
  const [dailyPageSize, setDailyPageSize] = useState(10)
  const [loading, setLoading] = useState(true)
  const [jobsLoading, setJobsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    const tokens = getStoredAuthTokens()
    if (!tokens) return
    let cancelled = false
    setLoading(true)
    fetchAiClassificationStats(tokens.accessToken, 30)
      .then((res) => {
        if (!cancelled) setStats(res)
      })
      .catch((err) => {
        if (!cancelled) setError(getApiErrorMessage(err))
      })
      .finally(() => {
        if (!cancelled) setLoading(false)
      })
    return () => {
      cancelled = true
    }
  }, [])

  useEffect(() => {
    const tokens = getStoredAuthTokens()
    if (!tokens) return
    let cancelled = false
    setJobsLoading(true)
    fetchAiClassificationJobs(tokens.accessToken, page, pageSize)
      .then((res) => {
        if (!cancelled) setJobs(res)
      })
      .catch((err) => {
        if (!cancelled) setError(getApiErrorMessage(err))
      })
      .finally(() => {
        if (!cancelled) setJobsLoading(false)
      })
    return () => {
      cancelled = true
    }
  }, [page, pageSize])

  if (loading) {
    return (
      <div className="mt-10 flex items-center justify-center">
        <span
          aria-hidden="true"
          className="h-8 w-8 animate-spin rounded-full border-2 border-primary/30 border-t-primary"
        />
      </div>
    )
  }

  const summary = stats?.summary
  const windows = stats?.windows
  const maxDailyTokens = Math.max(1, ...(stats?.daily.map((d) => d.totalTokens) ?? [1]))

  // Per-day rows are paginated client-side — the stats call already returns the
  // whole 30-day window in one payload.
  const dailyAll = stats?.daily ?? []
  const dailyTotalPages = Math.max(1, Math.ceil(dailyAll.length / dailyPageSize))
  const dailyCurrentPage = Math.min(dailyPage, dailyTotalPages)
  const dailyRows = dailyAll.slice(
    (dailyCurrentPage - 1) * dailyPageSize,
    dailyCurrentPage * dailyPageSize,
  )

  return (
    <>
      {error && (
        <div
          role="alert"
          className="mt-4 rounded-xl border border-error/30 bg-error-container px-4 py-3 font-label-md text-label-md text-on-error-container"
        >
          {error}
        </div>
      )}

      {stats?.message && (
        <div
          role="status"
          className="mt-4 rounded-xl border border-surface-variant bg-surface-container-low px-4 py-3 font-label-md text-label-md text-on-surface-variant"
        >
          {stats.message}
        </div>
      )}

      {/* ---- All-time totals ---- */}
      <section className="mt-8">
        <h2 className="font-label-md text-label-md font-semibold text-on-surface">All time</h2>
        <p className="mt-1 font-label-sm text-label-sm text-on-surface-variant">
          Everything the classifier has ever processed, since the first run.
        </p>
        <div className="mt-3 grid grid-cols-2 gap-3 sm:grid-cols-3 lg:grid-cols-5">
          <SummaryCard
            label="Jobs classified"
            value={summary?.totalJobsEver ?? 0}
            icon="task_alt"
            accent="text-primary"
          />
          <SummaryCard
            label="Categorised"
            value={summary?.totalSuccessfulEver ?? 0}
            icon="check_circle"
            accent="text-success"
          />
          <SummaryCard
            label="Uncategorised"
            value={summary?.totalUncategorizedEver ?? 0}
            icon="help"
            accent="text-error"
          />
          <SummaryCard
            label="Tokens sent"
            value={summary?.totalTokensSentEver ?? 0}
            icon="upload"
          />
          <SummaryCard
            label="Tokens received"
            value={summary?.totalTokensReceivedEver ?? 0}
            icon="download"
          />
          <SummaryCard
            label="Total tokens"
            value={summary?.totalTokensEver ?? 0}
            icon="data_usage"
            accent="text-primary"
          />
        </div>
      </section>

      {/* ---- Explained: today / this week / this month ---- */}
      <section className="mt-8">
        <h2 className="font-label-md text-label-md font-semibold text-on-surface">
          Tokens by period
        </h2>
        <p className="mt-1 font-label-sm text-label-sm text-on-surface-variant">
          Each window is UTC and starts at the boundary noted under it — so the numbers
          reset as the day, week and month roll over.
        </p>
        <div className="mt-3 grid gap-4 lg:grid-cols-3">
          <PeriodCard
            title="Today"
            caption="From 00:00 UTC today until now"
            stats={windows?.today}
          />
          <PeriodCard
            title="This week"
            caption="From Monday 00:00 UTC until now"
            stats={windows?.thisWeek}
          />
          <PeriodCard
            title="This month"
            caption="From the 1st of the month, 00:00 UTC, until now"
            stats={windows?.thisMonth}
          />
        </div>
      </section>

      {/* ---- Per-day token breakdown ---- */}
      <section className="mt-8">
        <h2 className="font-label-md text-label-md font-semibold text-on-surface">Per day</h2>
        <p className="mt-1 font-label-sm text-label-sm text-on-surface-variant">
          Daily token spend for the last 30 days. Only days with activity are listed.
        </p>
        {stats && stats.daily.length > 0 ? (
          <>
            <div className="mt-3 rounded-xl border border-surface-variant bg-surface-container-lowest shadow-sm">
              <table className="w-full table-fixed">
                <thead>
                  <tr className="border-b border-surface-variant bg-surface-container-low">
                    <Th align="left" width="w-[20%]">Day</Th>
                    <Th align="right" width="w-[9%]">Jobs</Th>
                    <Th align="right" width="w-[13%]">Categorised</Th>
                    <Th align="right" width="w-[13%]">Tokens sent</Th>
                    <Th align="right" width="w-[16%]">Tokens received</Th>
                    <Th align="right" width="w-[13%]">Total tokens</Th>
                    <Th align="left" width="w-[16%]">Load</Th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-surface-variant">
                  {dailyRows.map((day) => (
                    <DailyRow key={day.date} day={day} maxTokens={maxDailyTokens} />
                  ))}
                </tbody>
              </table>
            </div>

            <Pager
              page={dailyCurrentPage}
              pageSize={dailyPageSize}
              total={dailyAll.length}
              pageSizeOptions={[5, 10, 20, 30]}
              label={`${dailyAll.length.toLocaleString()} day${dailyAll.length === 1 ? '' : 's'} with activity`}
              onPage={setDailyPage}
              onPageSize={(size) => {
                setDailyPageSize(size)
                setDailyPage(1)
              }}
            />
          </>
        ) : (
          <EmptyCard message="No classify activity in the last 30 days." />
        )}
      </section>

      {/* ---- Per-job audit trail ---- */}
      <section className="mt-8">
        <h2 className="font-label-md text-label-md font-semibold text-on-surface">
          Classified jobs
        </h2>
        <p className="mt-1 font-label-sm text-label-sm text-on-surface-variant">
          One row per classify attempt — what sector the job had, what the model decided,
          and why. Hover the reasoning for a preview, or press the info button for the full text.
        </p>

        {jobsLoading && !jobs ? (
          <div className="mt-6 flex items-center justify-center">
            <span
              aria-hidden="true"
              className="h-6 w-6 animate-spin rounded-full border-2 border-primary/30 border-t-primary"
            />
          </div>
        ) : jobs && jobs.items.length > 0 ? (
          <>
            <div className="mt-3 rounded-xl border border-surface-variant bg-surface-container-lowest shadow-sm">
              <table className="w-full table-fixed">
                <thead>
                  <tr className="border-b border-surface-variant bg-surface-container-low">
                    <Th align="left" width="w-[22%]">Job</Th>
                    <Th align="left" width="w-[15%]">Sector before</Th>
                    <Th align="left" width="w-[17%]">Sector after</Th>
                    <Th align="right" width="w-[9%]">Conf.</Th>
                    <Th align="left" width="w-[15%]">Reasoning</Th>
                    <Th align="right" width="w-[10%]">Tokens</Th>
                    <Th align="left" width="w-[12%]">When</Th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-surface-variant">
                  {jobs.items.map((item) => (
                    <JobLogRow key={item.logId} item={item} />
                  ))}
                </tbody>
              </table>
            </div>

            <Pager
              page={jobs.page}
              pageSize={pageSize}
              total={jobs.total}
              busy={jobsLoading}
              label={`${jobs.total.toLocaleString()} classify attempt${jobs.total === 1 ? '' : 's'}`}
              onPage={setPage}
              onPageSize={(size) => {
                setPageSize(size)
                setPage(1)
              }}
            />
          </>
        ) : (
          <EmptyCard message={jobs?.message ?? 'No jobs have been classified yet.'} />
        )}
      </section>
    </>
  )
}

// ------------------------------------------------------------------ Cover letter

function CoverLetterTab() {
  return (
    <div className="mt-8 rounded-xl border border-surface-variant bg-surface-container-lowest px-6 py-10 text-center shadow-sm">
      <span className="material-symbols-outlined text-4xl text-on-surface-variant">draft</span>
      <h2 className="mt-3 font-title-lg text-title-lg font-semibold text-on-surface">
        AI Cover Letter
      </h2>
      <p className="mx-auto mt-2 max-w-md font-body-md text-body-md text-on-surface-variant">
        Not wired up yet. Once the cover-letter generator runs through the AI service,
        its token usage and generated letters will show up here, in the same shape as
        the classification tab.
      </p>
    </div>
  )
}

// ------------------------------------------------------------------ Pieces

function Th({
  children,
  align,
  width,
}: {
  children: React.ReactNode
  align: 'left' | 'right'
  /** Column width for `table-fixed` layouts — keeps the table inside its box. */
  width?: string
}) {
  return (
    <th
      className={cn(
        'px-4 py-3 font-label-sm text-label-sm font-semibold text-on-surface',
        align === 'right' ? 'text-right' : 'text-left',
        width,
      )}
    >
      {children}
    </th>
  )
}

function SummaryCard({
  label,
  value,
  icon,
  accent,
}: {
  label: string
  value: number
  icon: string
  accent?: string
}) {
  return (
    <div className="rounded-xl border border-surface-variant bg-surface-container-lowest px-4 py-3 shadow-sm">
      <div className="flex items-center gap-1.5 text-on-surface-variant">
        <span className="material-symbols-outlined text-base">{icon}</span>
        <p className="font-label-xs text-label-xs">{label}</p>
      </div>
      <p
        className={cn(
          'mt-1 font-headline-sm text-headline-sm font-bold',
          accent ?? 'text-on-surface',
        )}
      >
        {value.toLocaleString()}
      </p>
    </div>
  )
}

function PeriodCard({
  title,
  caption,
  stats,
}: {
  title: string
  caption: string
  stats?: AiClassificationWindowStats
}) {
  const s: AiClassificationWindowStats = stats ?? {
    jobsProcessed: 0,
    jobsSuccessful: 0,
    jobsUncategorized: 0,
    tokensSent: 0,
    tokensReceived: 0,
    totalTokens: 0,
    latencyMsAvg: 0,
  }

  return (
    <div className="rounded-xl border border-surface-variant bg-surface-container-lowest p-5 shadow-sm">
      <div className="flex items-baseline justify-between gap-2">
        <h3 className="font-title-md text-title-md font-semibold text-on-surface">{title}</h3>
        <span className="font-label-xs text-label-xs text-on-surface-variant">
          {s.jobsProcessed.toLocaleString()} job{s.jobsProcessed === 1 ? '' : 's'}
        </span>
      </div>
      <p className="mt-0.5 font-label-xs text-label-xs text-on-surface-variant">{caption}</p>

      <dl className="mt-4 space-y-2">
        <Row label="Tokens sent (prompt)" value={s.tokensSent} />
        <Row label="Tokens received (reply)" value={s.tokensReceived} />
        <Row label="Total tokens" value={s.totalTokens} strong />
        <div className="border-t border-surface-variant pt-2">
          <Row label="Categorised" value={s.jobsSuccessful} />
          <Row label="Uncategorised" value={s.jobsUncategorized} />
          <Row
            label="Avg response"
            value={s.latencyMsAvg}
            suffix=" ms"
            fractionDigits={0}
          />
        </div>
      </dl>
    </div>
  )
}

function Row({
  label,
  value,
  strong,
  suffix = '',
  fractionDigits = 0,
}: {
  label: string
  value: number
  strong?: boolean
  suffix?: string
  fractionDigits?: number
}) {
  return (
    <div className="flex items-center justify-between gap-3">
      <dt className="font-label-sm text-label-sm text-on-surface-variant">{label}</dt>
      <dd
        className={cn(
          'font-label-md text-label-md',
          strong ? 'font-bold text-primary' : 'font-semibold text-on-surface',
        )}
      >
        {value.toLocaleString(undefined, {
          minimumFractionDigits: fractionDigits,
          maximumFractionDigits: fractionDigits,
        })}
        {suffix}
      </dd>
    </div>
  )
}

function DailyRow({ day, maxTokens }: { day: AiClassificationDay; maxTokens: number }) {
  const pct = Math.max(2, Math.round((day.totalTokens / maxTokens) * 100))
  return (
    <tr className="transition-colors hover:bg-surface-container-low/50">
      <td className="truncate px-4 py-3 font-body-sm text-sm font-medium text-on-surface">
        {new Date(`${day.date}T00:00:00Z`).toLocaleDateString(undefined, {
          weekday: 'short',
          month: 'short',
          day: 'numeric',
          year: 'numeric',
        })}
      </td>
      <td className="px-4 py-3 text-right font-body-sm text-sm text-on-surface">
        {day.jobsProcessed.toLocaleString()}
      </td>
      <td className="px-4 py-3 text-right font-body-sm text-sm text-success">
        {day.jobsSuccessful.toLocaleString()}
      </td>
      <td className="px-4 py-3 text-right font-body-sm text-sm text-on-surface">
        {day.tokensSent.toLocaleString()}
      </td>
      <td className="px-4 py-3 text-right font-body-sm text-sm text-on-surface">
        {day.tokensReceived.toLocaleString()}
      </td>
      <td className="px-4 py-3 text-right font-body-sm text-sm font-semibold text-primary">
        {day.totalTokens.toLocaleString()}
      </td>
      <td className="px-4 py-3">
        <span
          aria-hidden="true"
          className="block h-2 rounded-full bg-primary/70"
          style={{ width: `${pct}%` }}
        />
      </td>
    </tr>
  )
}

function JobLogRow({ item }: { item: AiClassificationJobItem }) {
  const before = item.originalSectorName || item.originalSectorSlug
  const after = item.sectorName || item.sectorSlug

  return (
    <tr className="align-top transition-colors hover:bg-surface-container-low/50">
      <td className="px-4 py-3">
        <p className="truncate font-body-sm text-sm font-medium text-on-surface">
          {item.jobTitle ?? <span className="text-on-surface-variant italic">Untitled</span>}
        </p>
        <p className="truncate font-label-xs text-label-xs text-on-surface-variant">
          {item.jobCompany ? `${item.jobCompany} · ` : ''}
          <span className="font-mono">{item.jobId.slice(0, 8)}</span>
        </p>
      </td>
      <td className="px-4 py-3">
        {before ? (
          <span className="block max-w-full truncate rounded-full bg-surface-container-high px-2.5 py-0.5 font-label-xs text-label-xs text-on-surface-variant">
            {before}
          </span>
        ) : (
          <span className="font-label-xs text-label-xs text-on-surface-variant italic">
            none
          </span>
        )}
      </td>
      <td className="px-4 py-3">
        {item.categorized && after ? (
          <span className="flex max-w-full items-center gap-1 rounded-full bg-success-container px-2.5 py-0.5 font-label-xs text-label-xs text-on-success-container">
            <span className="material-symbols-outlined shrink-0 text-xs">arrow_forward</span>
            <span className="truncate">{after}</span>
          </span>
        ) : (
          <span className="block max-w-full truncate rounded-full bg-error-container px-2.5 py-0.5 font-label-xs text-label-xs text-on-error-container">
            Uncategorised
          </span>
        )}
      </td>
      <td className="px-4 py-3 text-right">
        <ConfidenceCell confidence={item.confidence} />
      </td>
      <td className="px-4 py-3">
        <ReasoningCell reasoning={item.reasoning} />
      </td>
      <td className="px-4 py-3 text-right">
        <p className="font-body-sm text-sm font-semibold text-primary">
          {item.totalTokens.toLocaleString()}
        </p>
        <p className="font-label-xs text-label-xs text-on-surface-variant">
          {item.tokensSent.toLocaleString()} / {item.tokensReceived.toLocaleString()}
        </p>
      </td>
      <td className="px-4 py-3">
        <p className="truncate font-label-sm text-label-sm text-on-surface">
          {formatDateTime(item.aiClassifiedAt)}
        </p>
        {item.latencyMs != null && (
          <p className="font-label-xs text-label-xs text-on-surface-variant">
            {item.latencyMs.toLocaleString()} ms
          </p>
        )}
      </td>
    </tr>
  )
}

function ConfidenceCell({ confidence }: { confidence: number | null }) {
  if (confidence == null) {
    return <span className="font-label-xs text-label-xs text-on-surface-variant">—</span>
  }
  const pct = Math.round(confidence * 100)
  return (
    <span
      className={cn(
        'inline-flex items-center rounded-full px-2.5 py-0.5 font-label-xs text-label-xs font-semibold',
        pct >= 80
          ? 'bg-success-container text-on-success-container'
          : pct >= 50
            ? 'bg-amber-100 text-amber-900'
            : 'bg-error-container text-on-error-container',
      )}
    >
      {pct}%
    </span>
  )
}

/**
 * Reasoning preview. Hovering the snippet shows a small "Reasoning" tooltip
 * with only the first few lines; the info button beside it opens a modal with
 * the full text.
 *
 * The tooltip is positioned from the element's bounding box and rendered
 * `fixed`, so the table's horizontal scroll container cannot clip it.
 */
function ReasoningCell({ reasoning }: { reasoning: string | null }) {
  const [tip, setTip] = useState<{ top: number; left: number } | null>(null)
  const [open, setOpen] = useState(false)
  const text = reasoning?.trim()

  if (!text) {
    return <span className="font-label-xs text-label-xs text-on-surface-variant">—</span>
  }

  // The cell shows only ~20% of the reasoning (never the whole thing) so a long
  // explanation can't widen the column and trigger a horizontal scrollbar. The
  // hover preview shows the first few lines, and the info button shows it all.
  const previewLength = Math.max(18, Math.round(text.length * PREVIEW_RATIO))
  const preview = text.length > previewLength ? `${text.slice(0, previewLength)}…` : text

  const showTip = (el: HTMLElement) => {
    const rect = el.getBoundingClientRect()
    const width = 256
    const left = Math.min(Math.max(8, rect.left), Math.max(8, window.innerWidth - width - 8))
    setTip({ top: rect.bottom + 6, left })
  }

  return (
    <div className="flex items-start gap-1.5">
      <p
        tabIndex={0}
        className="min-w-0 flex-1 cursor-help truncate font-body-sm text-sm text-on-surface-variant"
        onMouseEnter={(e) => showTip(e.currentTarget)}
        onMouseLeave={() => setTip(null)}
        onFocus={(e) => showTip(e.currentTarget)}
        onBlur={() => setTip(null)}
      >
        {preview}
      </p>

      <button
        type="button"
        onClick={() => setOpen(true)}
        aria-label="Show the full reasoning"
        title="Show full reasoning"
        className="shrink-0 text-on-surface-variant transition-colors hover:text-primary"
      >
        <span className="material-symbols-outlined text-base">info</span>
      </button>

      {tip && (
        <div
          role="tooltip"
          className="pointer-events-none fixed z-40 w-64 rounded-lg border border-surface-variant bg-surface-container-lowest p-2.5 shadow-lg"
          style={{ top: tip.top, left: tip.left }}
        >
          <p className="font-label-xs text-label-xs font-semibold text-on-surface">Reasoning</p>
          <p className="mt-0.5 line-clamp-3 font-body-sm text-sm text-on-surface-variant">
            {text}
          </p>
        </div>
      )}

      {open && <ReasoningModal reasoning={text} onClose={() => setOpen(false)} />}
    </div>
  )
}

/** Full-reasoning modal, opened from the info button. Esc or backdrop closes it. */
function ReasoningModal({ reasoning, onClose }: { reasoning: string; onClose: () => void }) {
  useEffect(() => {
    const onKey = (event: KeyboardEvent) => {
      if (event.key === 'Escape') onClose()
    }
    document.addEventListener('keydown', onKey)
    return () => document.removeEventListener('keydown', onKey)
  }, [onClose])

  return (
    <div
      role="dialog"
      aria-modal="true"
      aria-label="Model reasoning"
      className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 p-4"
      onClick={onClose}
    >
      <div
        className="w-full max-w-lg rounded-xl border border-surface-variant bg-surface-container-lowest p-5 shadow-xl"
        onClick={(event) => event.stopPropagation()}
      >
        <div className="flex items-start justify-between gap-3">
          <div className="flex items-center gap-2">
            <span className="material-symbols-outlined text-lg text-primary">psychology</span>
            <p className="font-label-md text-label-md font-semibold text-on-surface">
              Model reasoning
            </p>
          </div>
          <button
            type="button"
            onClick={onClose}
            aria-label="Close"
            className="text-on-surface-variant transition-colors hover:text-on-surface"
          >
            <span className="material-symbols-outlined text-lg">close</span>
          </button>
        </div>
        <p className="mt-3 max-h-72 overflow-y-auto whitespace-pre-wrap font-body-sm text-sm text-on-surface-variant">
          {reasoning}
        </p>
      </div>
    </div>
  )
}

/**
 * Shared table footer: a per-page size selector plus prev/next paging. Works
 * for both server-paged (classified jobs) and client-paged (per-day) tables.
 */
function Pager({
  page,
  pageSize,
  total,
  label,
  onPage,
  onPageSize,
  pageSizeOptions = [10, 25, 50],
  busy,
}: {
  page: number
  pageSize: number
  total: number
  label: string
  onPage: (page: number) => void
  onPageSize: (size: number) => void
  pageSizeOptions?: number[]
  busy?: boolean
}) {
  const totalPages = Math.max(1, Math.ceil(total / pageSize))
  const current = Math.min(page, totalPages)

  return (
    <div className="mt-3 flex flex-wrap items-center justify-between gap-3">
      <p className="font-label-sm text-label-sm text-on-surface-variant">{label}</p>

      <div className="flex flex-wrap items-center gap-3">
        <label className="flex items-center gap-1.5">
          <span className="font-label-sm text-label-sm text-on-surface-variant">Per page</span>
          <select
            value={pageSize}
            onChange={(event) => onPageSize(Number(event.target.value))}
            className="h-8 rounded-lg border border-outline-variant bg-surface-container-lowest px-2 font-label-sm text-label-sm text-on-surface focus:border-primary focus:outline-none"
          >
            {pageSizeOptions.map((size) => (
              <option key={size} value={size}>
                {size}
              </option>
            ))}
          </select>
        </label>

        <div className="flex items-center gap-1.5">
          <PagerButton
            disabled={current <= 1 || Boolean(busy)}
            onClick={() => onPage(current - 1)}
            icon="chevron_left"
            label="Prev"
          />
          <span className="min-w-[3.5rem] text-center font-label-sm text-label-sm text-on-surface-variant">
            {current} / {totalPages}
          </span>
          <PagerButton
            disabled={current >= totalPages || Boolean(busy)}
            onClick={() => onPage(current + 1)}
            icon="chevron_right"
            label="Next"
            iconFirst={false}
          />
        </div>
      </div>
    </div>
  )
}

function PagerButton({
  disabled,
  onClick,
  icon,
  label,
  iconFirst = true,
}: {
  disabled: boolean
  onClick: () => void
  icon: string
  label: string
  iconFirst?: boolean
}) {
  return (
    <button
      type="button"
      disabled={disabled}
      onClick={onClick}
      className="flex h-9 items-center justify-center gap-1 rounded-lg border border-outline-variant bg-surface-container-lowest px-3 font-label-md text-label-md text-on-surface transition-colors hover:bg-surface-container-low hover:text-primary disabled:pointer-events-none disabled:opacity-40"
    >
      {iconFirst && <span className="material-symbols-outlined text-lg">{icon}</span>}
      {label}
      {!iconFirst && <span className="material-symbols-outlined text-lg">{icon}</span>}
    </button>
  )
}

function EmptyCard({ message }: { message: string }) {
  return (
    <div className="mt-3 rounded-xl border border-surface-variant bg-surface-container-lowest px-5 py-4 shadow-sm">
      <p className="font-body-md text-body-md text-on-surface-variant">{message}</p>
    </div>
  )
}

function formatDateTime(value: string): string {
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return value
  return date.toLocaleString(undefined, {
    month: 'short',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  })
}
