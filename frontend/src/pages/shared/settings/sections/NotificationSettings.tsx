import AccordionSection from '../../../../components/ui/AccordionSection.tsx'
import type {
  NotificationSettings as NotificationSettingsType,
  NotificationChannelToggles,
} from '../../../../types/settings.ts'
import type { RequiredRole } from '../../../../hooks'

const toggleClass = (enabled: boolean) =>
  `relative inline-flex h-5 w-9 shrink-0 cursor-pointer rounded-full transition-colors ${enabled ? 'bg-primary' : 'bg-surface-variant'}`

interface NotificationSettingsProps {
  role: RequiredRole
  value: NotificationSettingsType
  onChange: (next: NotificationSettingsType) => void
}

// ── Small channel toggle pill ──
function ChannelToggle({
  label,
  enabled,
  onToggle,
}: {
  label: string
  enabled: boolean
  onToggle: () => void
}) {
  return (
    <button
      type="button"
      role="switch"
      aria-checked={enabled}
      aria-label={`${label}: ${enabled ? 'On' : 'Off'}`}
      onClick={onToggle}
      className={`inline-flex items-center gap-1.5 rounded-full border px-2.5 py-1 text-label-xs transition-colors ${
        enabled
          ? 'border-primary bg-primary/10 text-primary'
          : 'border-surface-variant bg-surface-variant/50 text-on-surface-variant/60'
      }`}
    >
      <span
        className={`inline-block h-3 w-3 rounded-full transition-colors ${
          enabled ? 'bg-primary' : 'bg-on-surface-variant/30'
        }`}
      />
      {label}
    </button>
  )
}

// ── Notification type row with 3 channel sub-toggles ──
function NotificationTypeRow({
  label,
  description,
  channels,
  onChange,
}: {
  label: string
  description?: string
  channels: NotificationChannelToggles
  onChange: (next: NotificationChannelToggles) => void
}) {
  return (
    <div className="py-3">
      <div className="flex items-center justify-between gap-4">
        <div className="min-w-0">
          <p className="font-label-md text-label-md text-on-surface">{label}</p>
          {description && (
            <p className="font-label-sm text-label-sm text-on-surface-variant/70">
              {description}
            </p>
          )}
        </div>
      </div>
      <div className="flex items-center gap-2 mt-2">
        <ChannelToggle
          label="Email"
          enabled={channels.email}
          onToggle={() => onChange({ ...channels, email: !channels.email })}
        />
        <ChannelToggle
          label="In-app"
          enabled={channels.inApp}
          onToggle={() => onChange({ ...channels, inApp: !channels.inApp })}
        />
        <ChannelToggle
          label="Telegram"
          enabled={channels.telegram}
          onToggle={() => onChange({ ...channels, telegram: !channels.telegram })}
        />
      </div>
    </div>
  )
}

function Divider() {
  return <div className="border-t border-surface-variant" />
}

export default function NotificationSettings({ role, value, onChange }: NotificationSettingsProps) {
  const isTalent = role === 'Talent'
  const isRecruiter = role === 'Recruiter'
  const isAdmin = role === 'Admin'

  const updateType = (
    key: keyof Pick<
      NotificationSettingsType,
      'jobAlerts' | 'applicationUpdates' | 'newApplications' | 'jobStatusChanges' | 'systemAlerts'
    >,
    channels: NotificationChannelToggles,
  ) => {
    onChange({ ...value, [key]: channels })
  }

  const updateEmailOnly = (patch: Partial<Pick<NotificationSettingsType, 'weeklyDigest' | 'weeklySummary' | 'marketing' | 'weeklyReport'>>) =>
    onChange({ ...value, ...patch })

  // Count: each notification type has 3 channels, email-only types count as 1
  const countChannels = (ch: NotificationChannelToggles) =>
    [ch.email, ch.inApp, ch.telegram].filter(Boolean).length

  let enabledCount = 0
  let totalToggles = 0
  if (isTalent) {
    enabledCount += countChannels(value.jobAlerts) + countChannels(value.applicationUpdates)
    totalToggles += 6
  }
  if (isRecruiter) {
    enabledCount += countChannels(value.newApplications) + countChannels(value.jobStatusChanges)
    totalToggles += 6
  }
  if (isAdmin) {
    enabledCount += countChannels(value.systemAlerts)
    totalToggles += 3
  }
  // Email-only extras count as 1 each
  if (isTalent) { enabledCount += value.weeklyDigest ? 1 : 0; totalToggles += 1 }
  if (isRecruiter) { enabledCount += value.weeklySummary ? 1 : 0; totalToggles += 1 }
  if (isAdmin) { enabledCount += value.weeklyReport ? 1 : 0; totalToggles += 1 }
  enabledCount += value.marketing ? 1 : 0
  totalToggles += 1

  return (
    <AccordionSection
      title="Notifications"
      description="Choose how you get notified for each event"
      icon="notifications"
      completion={{ filled: enabledCount, total: Math.max(totalToggles, 1) }}
    >
      {/* ── Talent notifications ── */}
      {isTalent && (
        <div className="md:col-span-2">
          <h3 className="font-label-md text-label-md font-semibold text-on-surface mb-1">
            Talent
          </h3>
          <p className="font-label-sm text-label-sm text-on-surface-variant/70 mb-2">
            Notifications for your job search and applications.
          </p>

          <NotificationTypeRow
            label="Job alerts"
            description="New jobs matching your profile and preferences"
            channels={value.jobAlerts}
            onChange={(ch) => updateType('jobAlerts', ch)}
          />
          <Divider />
          <NotificationTypeRow
            label="Application updates"
            description="When your application status changes"
            channels={value.applicationUpdates}
            onChange={(ch) => updateType('applicationUpdates', ch)}
          />
          <Divider />

          {/* Email-only extras */}
          <div className="py-3">
            <div className="flex items-center justify-between gap-4">
              <div className="min-w-0">
                <p className="font-label-md text-label-md text-on-surface">Weekly digest</p>
                <p className="font-label-sm text-label-sm text-on-surface-variant/70">
                  A summary of new jobs and activity each week
                </p>
              </div>
              <button
                type="button"
                role="switch"
                aria-checked={value.weeklyDigest}
                aria-label="Toggle weekly digest"
                onClick={() => updateEmailOnly({ weeklyDigest: !value.weeklyDigest })}
                className={toggleClass(value.weeklyDigest)}
              >
                <span
                  className={`inline-block h-4 w-4 transform rounded-full bg-white shadow-sm transition-transform mt-0.5 ${value.weeklyDigest ? 'translate-x-4' : 'translate-x-0.5'}`}
                />
              </button>
            </div>
          </div>
          <Divider />
          <div className="py-3">
            <div className="flex items-center justify-between gap-4">
              <div className="min-w-0">
                <p className="font-label-md text-label-md text-on-surface">Saved search alerts</p>
                <p className="font-label-sm text-label-sm text-on-surface-variant/70">
                  When saved searches have new matches
                </p>
              </div>
              <button
                type="button"
                role="switch"
                aria-checked={value.marketing}
                aria-label="Toggle saved search alerts"
                onClick={() => updateEmailOnly({ marketing: !value.marketing })}
                className={toggleClass(value.marketing)}
              >
                <span
                  className={`inline-block h-4 w-4 transform rounded-full bg-white shadow-sm transition-transform mt-0.5 ${value.marketing ? 'translate-x-4' : 'translate-x-0.5'}`}
                />
              </button>
            </div>
          </div>
        </div>
      )}

      {/* ── Recruiter notifications ── */}
      {isRecruiter && (
        <div className="md:col-span-2">
          <h3 className="font-label-md text-label-md font-semibold text-on-surface mb-1">
            Recruiter
          </h3>
          <p className="font-label-sm text-label-sm text-on-surface-variant/70 mb-2">
            Notifications for your job posts and applications.
          </p>

          <NotificationTypeRow
            label="New applications"
            description="When someone applies to your job posts"
            channels={value.newApplications}
            onChange={(ch) => updateType('newApplications', ch)}
          />
          <Divider />
          <NotificationTypeRow
            label="Job status changes"
            description="When your job post is approved, rejected, or expires"
            channels={value.jobStatusChanges}
            onChange={(ch) => updateType('jobStatusChanges', ch)}
          />
          <Divider />

          {/* Email-only extras */}
          <div className="py-3">
            <div className="flex items-center justify-between gap-4">
              <div className="min-w-0">
                <p className="font-label-md text-label-md text-on-surface">Weekly summary</p>
                <p className="font-label-sm text-label-sm text-on-surface-variant/70">
                  A summary of your posting activity each week
                </p>
              </div>
              <button
                type="button"
                role="switch"
                aria-checked={value.weeklySummary}
                aria-label="Toggle weekly summary"
                onClick={() => updateEmailOnly({ weeklySummary: !value.weeklySummary })}
                className={toggleClass(value.weeklySummary)}
              >
                <span
                  className={`inline-block h-4 w-4 transform rounded-full bg-white shadow-sm transition-transform mt-0.5 ${value.weeklySummary ? 'translate-x-4' : 'translate-x-0.5'}`}
                />
              </button>
            </div>
          </div>
          <Divider />
          <div className="py-3">
            <div className="flex items-center justify-between gap-4">
              <div className="min-w-0">
                <p className="font-label-md text-label-md text-on-surface">Marketing</p>
                <p className="font-label-sm text-label-sm text-on-surface-variant/70">
                  Product updates and tips
                </p>
              </div>
              <button
                type="button"
                role="switch"
                aria-checked={value.marketing}
                aria-label="Toggle marketing"
                onClick={() => updateEmailOnly({ marketing: !value.marketing })}
                className={toggleClass(value.marketing)}
              >
                <span
                  className={`inline-block h-4 w-4 transform rounded-full bg-white shadow-sm transition-transform mt-0.5 ${value.marketing ? 'translate-x-4' : 'translate-x-0.5'}`}
                />
              </button>
            </div>
          </div>
        </div>
      )}

      {/* ── Admin notifications ── */}
      {isAdmin && (
        <div className="md:col-span-2">
          <h3 className="font-label-md text-label-md font-semibold text-on-surface mb-1">
            Admin
          </h3>
          <p className="font-label-sm text-label-sm text-on-surface-variant/70 mb-2">
            System and platform notifications.
          </p>

          <NotificationTypeRow
            label="System alerts"
            description="Critical system events and errors"
            channels={value.systemAlerts}
            onChange={(ch) => updateType('systemAlerts', ch)}
          />
          <Divider />

          <div className="py-3">
            <div className="flex items-center justify-between gap-4">
              <div className="min-w-0">
                <p className="font-label-md text-label-md text-on-surface">Weekly report</p>
                <p className="font-label-sm text-label-sm text-on-surface-variant/70">
                  Platform health and usage summary
                </p>
              </div>
              <button
                type="button"
                role="switch"
                aria-checked={value.weeklyReport}
                aria-label="Toggle weekly report"
                onClick={() => updateEmailOnly({ weeklyReport: !value.weeklyReport })}
                className={toggleClass(value.weeklyReport)}
              >
                <span
                  className={`inline-block h-4 w-4 transform rounded-full bg-white shadow-sm transition-transform mt-0.5 ${value.weeklyReport ? 'translate-x-4' : 'translate-x-0.5'}`}
                />
              </button>
            </div>
          </div>
        </div>
      )}
    </AccordionSection>
  )
}
