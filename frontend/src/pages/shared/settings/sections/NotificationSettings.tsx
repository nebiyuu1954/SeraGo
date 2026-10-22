import { useCallback, useState } from 'react'
import TelegramLinkCard from '../../../../components/settings/TelegramLinkCard.tsx'
import type {
  NotificationSettings as NotificationSettingsType,
  NotificationChannelToggles,
} from '../../../../types/settings.ts'
import type { RequiredRole } from '../../../../hooks'
import { cn } from '../../../../lib/cn.ts'

const toggleClass = (enabled: boolean) =>
  `relative inline-flex h-5 w-9 shrink-0 cursor-pointer rounded-full transition-colors ${enabled ? 'bg-primary' : 'bg-surface-variant'}`

interface NotificationSettingsProps {
  role: RequiredRole
  value: NotificationSettingsType
  onChange: (next: NotificationSettingsType) => void
  hasSectors?: boolean
}

// ── Pill button toggle with exact requested colors and disabled state ──
function ChannelToggle({
  label,
  enabled,
  disabled = false,
  title,
  onToggle,
}: {
  label: string
  enabled: boolean
  disabled?: boolean
  title?: string
  onToggle: () => void
}) {
  return (
    <button
      type="button"
      role="switch"
      disabled={disabled}
      title={title}
      aria-checked={enabled}
      aria-disabled={disabled}
      aria-label={`${label}: ${enabled ? 'On' : 'Off'}`}
      onClick={disabled ? undefined : onToggle}
      className={cn(
        'inline-flex items-center gap-1.5 px-4 py-2 rounded-full font-label-md text-label-md transition-all active:scale-[0.97]',
        disabled
          ? 'cursor-not-allowed opacity-50 bg-surface-container-low text-on-surface-variant/50 border border-transparent'
          : enabled
            ? 'bg-primary border border-primary text-white font-medium shadow-sm hover:opacity-95'
            : 'bg-surface-container-low border border-surface-variant/60 text-black font-medium hover:bg-surface-container',
      )}
    >
      <span
        className={cn(
          'material-symbols-outlined text-base inline-block transition-transform duration-500 ease-out',
          disabled ? 'text-on-surface-variant/50' : enabled ? 'text-white' : 'text-black',
          enabled ? 'rotate-[360deg]' : 'rotate-0',
        )}
      >
        add
      </span>
      <span className={disabled ? 'text-on-surface-variant/50' : enabled ? 'text-white' : 'text-black'}>{label}</span>
    </button>
  )
}

export default function NotificationSettings({
  role,
  value,
  onChange,
  hasSectors = true,
}: NotificationSettingsProps) {
  const isTalent = role === 'Talent'
  const isAdmin = role === 'Admin'

  const [isTelegramLinked, setIsTelegramLinked] = useState<boolean>(false)

  const handleTelegramStatusChange = useCallback(
    (linked: boolean) => {
      setIsTelegramLinked(linked)
      // If the user just linked their Telegram bot and has sectors, activate telegram job alerts by default
      if (linked && !value.jobAlerts.telegram && hasSectors) {
        onChange({
          ...value,
          jobAlerts: { ...value.jobAlerts, telegram: true },
        })
      }
    },
    [value, hasSectors, onChange],
  )

  const updateType = (
    key: keyof Pick<
      NotificationSettingsType,
      'jobAlerts' | 'applicationUpdates' | 'newApplications' | 'jobStatusChanges' | 'systemAlerts'
    >,
    channels: NotificationChannelToggles,
  ) => {
    onChange({ ...value, [key]: channels })
  }

  const updateEmailOnly = (
    patch: Partial<
      Pick<NotificationSettingsType, 'weeklyDigest' | 'weeklySummary' | 'marketing' | 'weeklyReport'>
    >,
  ) => onChange({ ...value, ...patch })

  const isJobAlertActive =
    hasSectors && (value.jobAlerts.inApp || (value.jobAlerts.telegram && isTelegramLinked))

  return (
    <div id="notifications" className="p-6 sm:p-8 lg:p-10 grid grid-cols-1 lg:grid-cols-12 gap-6 lg:gap-10">
      {/* Left Info Column */}
      <div className="lg:col-span-4 flex flex-col justify-start">
        <h2 className="font-headline-md text-headline-md font-semibold text-on-surface">
          Notifications
        </h2>
        <p className="font-body-md text-body-md text-on-surface-variant mt-2 leading-relaxed">
          Manage how and where you receive notifications for job alerts and platform updates.
        </p>
        <div className="mt-4 flex items-center gap-2">
          <span
            className={`inline-block w-2 h-2 rounded-full ${
              isJobAlertActive ? 'bg-emerald-500 animate-pulse' : 'bg-surface-variant'
            }`}
          />
          <span className="font-label-sm text-label-sm text-on-surface-variant">
            {isJobAlertActive ? 'Delivery channels active' : 'No active alert channels'}
          </span>
        </div>
      </div>

      {/* Right Controls Column */}
      <div className="lg:col-span-8 flex flex-col gap-6">
        {/* Telegram linking card */}
        <TelegramLinkCard onStatusChange={handleTelegramStatusChange} />

        {/* Talent notifications */}
        {isTalent && (
          <div className="pt-2">
            <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-2 mb-3">
              <div>
                <h3 className="font-label-md text-label-md font-semibold text-on-surface">Job Alerts</h3>
                <p className="font-body-md text-body-md text-on-surface-variant text-sm mt-0.5">
                  Get notified when new jobs match your chosen sectors and profile
                </p>
              </div>
              <span className="font-label-sm text-label-sm text-primary font-medium">
                {!hasSectors
                  ? 'Disabled (select at least 1 sector)'
                  : isJobAlertActive
                    ? 'Active'
                    : 'Turned off'}
              </span>
            </div>

            <div className="flex flex-wrap items-center gap-2.5 pt-1">
              {/* In-App Toggle (Email removed) */}
              <ChannelToggle
                label="In-App"
                enabled={value.jobAlerts.inApp}
                disabled={!hasSectors}
                title={!hasSectors ? 'Select at least 1 sector in For You feed to enable job alerts' : undefined}
                onToggle={() =>
                  updateType('jobAlerts', { ...value.jobAlerts, inApp: !value.jobAlerts.inApp })
                }
              />

              {/* Telegram Bot Toggle */}
              <ChannelToggle
                label="Telegram bot"
                enabled={value.jobAlerts.telegram && isTelegramLinked}
                disabled={!hasSectors || !isTelegramLinked}
                title={
                  !hasSectors
                    ? 'Select at least 1 sector in For You feed to enable job alerts'
                    : !isTelegramLinked
                      ? 'Connect Telegram bot above to enable Telegram notifications'
                      : undefined
                }
                onToggle={() =>
                  updateType('jobAlerts', { ...value.jobAlerts, telegram: !value.jobAlerts.telegram })
                }
              />
            </div>

            {/* Contextual notice */}
            {!hasSectors ? (
              <p className="mt-2.5 font-label-sm text-label-sm text-error/90 flex items-center gap-1.5">
                <span className="material-symbols-outlined text-base">info</span>
                <span>Select at least 1 sector in the For You Feed section above to enable job alerts.</span>
              </p>
            ) : !isTelegramLinked ? (
              <p className="mt-2.5 font-label-sm text-label-sm text-on-surface-variant/70 flex items-center gap-1.5">
                <span className="material-symbols-outlined text-base">info</span>
                <span>Connect your Telegram bot above to activate Telegram job alerts.</span>
              </p>
            ) : null}
          </div>
        )}

        {/* Admin notifications */}
        {isAdmin && (
          <>
            <div className="pt-2">
              <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-2 mb-3">
                <div>
                  <h3 className="font-label-md text-label-md font-semibold text-on-surface">System Alerts</h3>
                  <p className="font-body-md text-body-md text-on-surface-variant text-sm mt-0.5">
                    Critical system events, errors, and platform status changes
                  </p>
                </div>
              </div>

              <div className="flex flex-wrap items-center gap-2.5 pt-1">
                <ChannelToggle
                  label="In-App"
                  enabled={value.systemAlerts.inApp}
                  onToggle={() =>
                    updateType('systemAlerts', { ...value.systemAlerts, inApp: !value.systemAlerts.inApp })
                  }
                />
                <ChannelToggle
                  label="Telegram bot"
                  enabled={value.systemAlerts.telegram && isTelegramLinked}
                  disabled={!isTelegramLinked}
                  title={!isTelegramLinked ? 'Connect Telegram bot above to enable alerts' : undefined}
                  onToggle={() =>
                    updateType('systemAlerts', { ...value.systemAlerts, telegram: !value.systemAlerts.telegram })
                  }
                />
              </div>
            </div>

            <div className="pt-4 border-t border-surface-variant">
              <div className="flex items-center justify-between gap-4">
                <div className="min-w-0">
                  <p className="font-label-md text-label-md font-semibold text-on-surface">Weekly Report</p>
                  <p className="font-label-sm text-label-sm text-on-surface-variant/70">
                    Platform health, usage metrics, and analytics summary
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
                    className={`inline-block h-4 w-4 transform rounded-full bg-white shadow-sm transition-transform mt-0.5 ${
                      value.weeklyReport ? 'translate-x-4' : 'translate-x-0.5'
                    }`}
                  />
                </button>
              </div>
            </div>
          </>
        )}
      </div>
    </div>
  )
}
