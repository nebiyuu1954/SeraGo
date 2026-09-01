import useSWR from 'swr'
import { useCallback, useMemo } from 'react'
import { fetchSettings, updateSettings } from '../api/settings.ts'
import { getStoredAuthTokens } from '../api/auth.ts'
import { DEFAULT_SETTINGS } from '../types/settings.ts'
import type { UserSettings, NotificationChannelToggles } from '../types/settings.ts'

/** Default channel toggles (all on except telegram). */
const ON: NotificationChannelToggles = { email: true, inApp: true, telegram: false }
const OFF: NotificationChannelToggles = { email: false, inApp: false, telegram: false }

/**
 * Migrate old settings format → new format.
 * Old: notifications.email = { jobAlerts: bool, ... } / notifications.inApp = { ... }
 * New: notifications.jobAlerts = { email, inApp, telegram }, ...
 */
function migrateNotifications(raw: any): UserSettings['notifications'] {
  const def = DEFAULT_SETTINGS.notifications

  // Already new format — if jobAlerts is an object with email/inApp/telegram
  if (raw?.jobAlerts && typeof raw.jobAlerts === 'object' && 'email' in raw.jobAlerts) {
    return { ...def, ...raw }
  }

  // Legacy format: raw.email and raw.inApp
  const emailSec = raw?.email ?? {}
  const inAppSec = raw?.inApp ?? {}

  const ch = (
    emailKey: string,
    inAppKey: string,
    defaultEnabled = true,
  ): NotificationChannelToggles => ({
    email: emailKey in emailSec ? !!emailSec[emailKey] : defaultEnabled,
    inApp: inAppKey in inAppSec ? !!inAppSec[inAppKey] : defaultEnabled,
    telegram: false,
  })

  return {
    jobAlerts: ch('jobAlerts', 'jobAlerts'),
    applicationUpdates: ch('applicationUpdates', 'applicationUpdates'),
    newApplications: ch('newApplications', 'newApplications'),
    jobStatusChanges: ch('jobStatusChanges', 'jobStatusChanges'),
    systemAlerts: ch('systemAlerts', 'systemAlerts'),
    weeklyDigest: emailSec.weeklyDigest ?? def.weeklyDigest,
    weeklySummary: emailSec.weeklySummary ?? def.weeklySummary,
    marketing: emailSec.marketing ?? def.marketing,
    weeklyReport: emailSec.weeklyReport ?? def.weeklyReport,
  }
}

/** Merge saved settings with defaults — missing keys get default values. */
function mergeWithDefaults(saved: string): UserSettings {
  try {
    const parsed = JSON.parse(saved) as any
    return {
      version: parsed.version ?? 1,
      account: { ...DEFAULT_SETTINGS.account, ...parsed.account },
      notifications: migrateNotifications(parsed.notifications),
      security: { ...DEFAULT_SETTINGS.security, ...parsed.security },
      ai: { ...DEFAULT_SETTINGS.ai, ...parsed.ai },
    }
  } catch {
    return { ...DEFAULT_SETTINGS }
  }
}

interface UseSettingsResult {
  settings: UserSettings
  version: number
  isLoading: boolean
  error: unknown
  /** Save the full settings blob. Updates the SWR cache on success. */
  saveSettings: (next: UserSettings) => Promise<void>
  /** Check if settings have unsaved changes. */
  isDirty: boolean
}

/**
 * SWR-based hook for the current user's settings.
 * Returns merged settings (saved + defaults), a save function, and dirty state.
 */
export function useSettingsQuery(): UseSettingsResult {
  const tokens = getStoredAuthTokens()

  const { data, error, isLoading, mutate } = useSWR(
    tokens ? 'settings' : null,
    () => fetchSettings(tokens!.accessToken),
  )

  const settings = useMemo(
    () => (data ? mergeWithDefaults(data.settings) : { ...DEFAULT_SETTINGS }),
    [data],
  )

  const version = data?.version ?? 1

  const saveSettings = useCallback(
    async (next: UserSettings) => {
      if (!tokens) throw new Error('Not signed in')
      const payload = JSON.stringify(next)
      const result = await updateSettings(tokens.accessToken, payload, version)
      // Update the SWR cache with the new data.
      await mutate(result, { revalidate: false })
    },
    [tokens, version, mutate],
  )

  // "Dirty" is always false when using this hook — the caller manages
  // its own dirty tracking via formik or local state. This is a
  // placeholder for future use.
  const isDirty = false

  return { settings, version, isLoading, error, saveSettings, isDirty }
}
