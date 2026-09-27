import { useCallback, useEffect, useState } from 'react'
import DashboardShell from '../../../components/dashboard/DashboardShell.tsx'
import UnsavedChangesDialog from '../../../components/ui/UnsavedChangesDialog.tsx'
import { useRequireRole, useSettingsQuery, useUnsavedChanges } from '../../../hooks'
import type { RequiredRole } from '../../../hooks'
import type { UserSettings } from '../../../types'
import AccountSettings from './sections/AccountSettings.tsx'
import ForYouSettings from './sections/ForYouSettings.tsx'
import NotificationSettings from './sections/NotificationSettings.tsx'
import SecuritySettings from './sections/SecuritySettings.tsx'
import AIFeaturesSettings from './sections/AIFeaturesSettings.tsx'
import { useToast } from '../../../components/dashboard/Toast.tsx'

interface SettingsFormProps {
  role: RequiredRole
}

const COPY: Record<RequiredRole, { title: string; blurb: string }> = {
  Talent: {
    title: 'Settings',
    blurb: 'Manage your account preferences and notification settings.',
  },
  Admin: {
    title: 'Settings',
    blurb: 'Manage your account and platform preferences.',
  },
}

export default function SettingsForm({ role }: SettingsFormProps) {
  const auth = useRequireRole(role)
  const { settings, isLoading, saveSettings } = useSettingsQuery()
  const { showToast } = useToast()

  // Local draft — tracks edits before saving.
  const [draft, setDraft] = useState<UserSettings | null>(null)
  const [saving, setSaving] = useState(false)
  const [saveError, setSaveError] = useState<string | null>(null)

  // Initialize draft when settings load.
  useEffect(() => {
    if (!isLoading && draft === null) {
      setDraft({ ...settings })
    }
  }, [isLoading, settings, draft])



  const isDirty = draft !== null && JSON.stringify(draft) !== JSON.stringify(settings)

  const handleSave = useCallback(async () => {
    if (!draft) return
    setSaving(true)
    setSaveError(null)
    try {
      await saveSettings(draft)
      showToast('Settings saved.')
    } catch (err) {
      setSaveError(err instanceof Error ? err.message : 'Failed to save settings.')
    } finally {
      setSaving(false)
    }
  }, [draft, saveSettings, showToast])

  const { dialogOpen, saving: dialogSaving, handleSave: confirmSave, handleDiscard, handleCancel } =
    useUnsavedChanges({
      isDirty,
      onSave: handleSave,
    })

  const updateDraft = useCallback((patch: Partial<UserSettings>) => {
    setDraft((prev) => (prev ? { ...prev, ...patch } : prev))
  }, [])

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

  // Show loading until draft is initialized.
  if (draft === null) {
    return (
      <DashboardShell role={role} authUser={auth.user}>
        <div className="flex min-h-[30svh] items-center justify-center">
          <span
            aria-hidden="true"
            className="h-8 w-8 animate-spin rounded-full border-2 border-primary/30 border-t-primary"
          />
        </div>
      </DashboardShell>
    )
  }

  return (
    <DashboardShell role={role} authUser={auth.user}>
      {/* Header */}
      <div className="mb-6">
        <h1 className="font-headline-lg text-headline-lg font-bold tracking-tight text-primary">
          {COPY[role].title}
        </h1>
        <p className="mt-1 font-body-md text-body-md text-on-surface-variant">
          {COPY[role].blurb}
        </p>
      </div>

      {/* Settings sections */}
      <div className="space-y-4">
        <AccountSettings
          value={draft.account}
          onChange={(account) => updateDraft({ account })}
        />

        {role === 'Talent' && (
          <ForYouSettings
            value={draft.forYou}
            onChange={(forYou) => updateDraft({ forYou })}
          />
        )}

        <NotificationSettings
          role={role}
          value={draft.notifications}
          onChange={(notifications) => updateDraft({ notifications })}
        />

        <SecuritySettings
          value={draft.security}
          onChange={(security) => updateDraft({ security })}
        />

        <AIFeaturesSettings
          role={role}
          value={draft.ai}
          onChange={(ai) => updateDraft({ ai })}
        />
      </div>

      {/* Save bar — sticky at the bottom */}
      {isDirty && (
        <div className="sticky bottom-0 z-30 mt-6 flex items-center justify-between gap-4 rounded-xl border border-surface-variant bg-surface-container-lowest p-4 shadow-lg">
          <div className="flex items-center gap-2">
            {saveError && (
              <p className="font-label-sm text-label-sm text-error">{saveError}</p>
            )}
          </div>
          <div className="flex items-center gap-3">
            <button
              type="button"
              onClick={() => setDraft({ ...settings })}
              className="inline-flex items-center justify-center gap-2 rounded-[0.125rem] border border-outline-variant bg-surface-container-lowest px-4 py-2 font-label-md text-label-md font-medium tracking-[0.05em] text-on-surface transition-colors hover:bg-surface-container-low"
            >
              Discard
            </button>
            <button
              type="button"
              onClick={handleSave}
              disabled={saving}
              className="inline-flex items-center justify-center gap-2 rounded-[0.125rem] bg-accent px-6 py-2 font-label-md text-label-md font-medium tracking-[0.05em] text-on-accent transition-opacity hover:opacity-90 disabled:pointer-events-none disabled:opacity-60"
            >
              {saving ? (
                <>
                  <span className="h-4 w-4 animate-spin rounded-full border-2 border-on-accent/30 border-t-on-accent" />
                  Saving…
                </>
              ) : (
                'Save settings'
              )}
            </button>
          </div>
        </div>
      )}

      {/* Unsaved changes dialog */}
      <UnsavedChangesDialog
        open={dialogOpen}
        saving={dialogSaving}
        onSave={confirmSave}
        onDiscard={handleDiscard}
        onCancel={handleCancel}
      />
    </DashboardShell>
  )
}
