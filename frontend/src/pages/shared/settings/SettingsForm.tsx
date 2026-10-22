import { useCallback, useEffect, useState } from 'react'
import { useLocation, useNavigate } from 'react-router-dom'
import DashboardShell from '../../../components/dashboard/DashboardShell.tsx'
import UnsavedChangesDialog from '../../../components/ui/UnsavedChangesDialog.tsx'
import TalentOnboardingModal from '../../../components/dashboard/TalentOnboardingModal.tsx'
import { useRequireRole, useSettingsQuery, useUnsavedChanges } from '../../../hooks'
import type { RequiredRole } from '../../../hooks'
import type { UserSettings } from '../../../types'
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

type SettingsTab = 'all' | 'forYou' | 'notifications' | 'security' | 'ai'

export default function SettingsForm({ role }: SettingsFormProps) {
  const auth = useRequireRole(role)
  const navigate = useNavigate()
  const location = useLocation()
  const { settings, isLoading, saveSettings, refresh } = useSettingsQuery()
  const { showToast } = useToast()

  // Local draft — tracks edits before saving.
  const [draft, setDraft] = useState<UserSettings | null>(null)
  const [saving, setSaving] = useState(false)
  const [saveError, setSaveError] = useState<string | null>(null)
  const [sectorModalOpen, setSectorModalOpen] = useState(false)

  // Active pill tab navigation ('all' or specific category)
  const [activeTab, setActiveTab] = useState<SettingsTab>(() => {
    if (typeof window !== 'undefined') {
      const hash = window.location.hash.toLowerCase()
      const search = window.location.search.toLowerCase()
      if (hash.includes('telegram') || hash.includes('notifications') || search.includes('telegram')) {
        return 'notifications'
      }
      if (hash.includes('security')) return 'security'
      if (hash.includes('for-you') || hash.includes('foryou')) return 'forYou'
      if (hash.includes('ai')) return 'ai'
    }
    return 'all'
  })

  // Listen for openSectorModal trigger from resume cancellation flow
  useEffect(() => {
    const state = location.state as { openSectorModal?: boolean } | null
    if (state?.openSectorModal) {
      setSectorModalOpen(true)
      navigate(location.pathname, { replace: true, state: {} })
    }
  }, [location.state, location.pathname, navigate])

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
      showToast('Settings saved successfully.')
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
      <div className="w-full pb-16">
        {/* Header Block */}
        <div className="flex flex-col gap-4 pb-6 border-b border-surface-variant">
          <div>
            <h1 className="font-headline-lg text-headline-lg font-bold text-on-surface tracking-tight">
              {COPY[role].title}
            </h1>
            <p className="mt-1 font-body-md text-body-md text-on-surface-variant">
              {COPY[role].blurb}
            </p>
          </div>

          {/* Horizontal Pill Tab Bar */}
          <nav aria-label="Settings categories" className="flex items-center gap-2 overflow-x-auto pt-2 pb-1 no-scrollbar">
            <button
              type="button"
              onClick={() => setActiveTab('all')}
              className={`whitespace-nowrap px-5 py-2 rounded-full font-label-md text-label-md transition-all ${
                activeTab === 'all'
                  ? 'bg-primary text-on-primary shadow-sm font-semibold'
                  : 'bg-surface-container-lowest border border-outline-variant/60 text-on-surface-variant hover:text-on-surface hover:bg-surface-container-low'
              }`}
            >
              All Sections
            </button>

            {role === 'Talent' && (
              <button
                type="button"
                onClick={() => setActiveTab('forYou')}
                className={`whitespace-nowrap px-5 py-2 rounded-full font-label-md text-label-md transition-all ${
                  activeTab === 'forYou'
                    ? 'bg-primary text-on-primary shadow-sm font-semibold'
                    : 'bg-surface-container-lowest border border-outline-variant/60 text-on-surface-variant hover:text-on-surface hover:bg-surface-container-low'
                }`}
              >
                For You Feed
              </button>
            )}

            <button
              type="button"
              onClick={() => setActiveTab('notifications')}
              className={`whitespace-nowrap px-5 py-2 rounded-full font-label-md text-label-md transition-all ${
                activeTab === 'notifications'
                  ? 'bg-primary text-on-primary shadow-sm font-semibold'
                  : 'bg-surface-container-lowest border border-outline-variant/60 text-on-surface-variant hover:text-on-surface hover:bg-surface-container-low'
              }`}
            >
              Notifications
            </button>

            <button
              type="button"
              onClick={() => setActiveTab('security')}
              className={`whitespace-nowrap px-5 py-2 rounded-full font-label-md text-label-md transition-all ${
                activeTab === 'security'
                  ? 'bg-primary text-on-primary shadow-sm font-semibold'
                  : 'bg-surface-container-lowest border border-outline-variant/60 text-on-surface-variant hover:text-on-surface hover:bg-surface-container-low'
              }`}
            >
              Security
            </button>

            <button
              type="button"
              onClick={() => setActiveTab('ai')}
              className={`whitespace-nowrap px-5 py-2 rounded-full font-label-md text-label-md transition-all ${
                activeTab === 'ai'
                  ? 'bg-primary text-on-primary shadow-sm font-semibold'
                  : 'bg-surface-container-lowest border border-outline-variant/60 text-on-surface-variant hover:text-on-surface hover:bg-surface-container-low'
              }`}
            >
              AI Features
            </button>
          </nav>
        </div>

        {/* Main Settings Canvas - Unified divided container filling full width */}
        <div className="mt-8 w-full rounded-2xl border border-surface-variant bg-surface-container-lowest shadow-sm divide-y divide-surface-variant overflow-hidden">
          {role === 'Talent' && (activeTab === 'all' || activeTab === 'forYou') && (
            <ForYouSettings
              value={draft.forYou}
              onChange={(forYou) => updateDraft({ forYou })}
            />
          )}

          {(activeTab === 'all' || activeTab === 'notifications') && (
            <NotificationSettings
              role={role}
              value={draft.notifications}
              onChange={(notifications) => updateDraft({ notifications })}
              hasSectors={role === 'Talent' ? (draft.forYou?.sectorIds?.length ?? 0) > 0 : true}
            />
          )}

          {(activeTab === 'all' || activeTab === 'security') && (
            <SecuritySettings
              value={draft.security}
              onChange={(security) => updateDraft({ security })}
            />
          )}

          {(activeTab === 'all' || activeTab === 'ai') && (
            <AIFeaturesSettings
              role={role}
              value={draft.ai}
              onChange={(ai) => updateDraft({ ai })}
            />
          )}
        </div>

        {/* Floating Bottom Save Dock */}
        {isDirty && (
          <aside className="sticky bottom-6 z-30 mt-12 w-full rounded-2xl bg-surface-container-lowest/95 backdrop-blur-md shadow-xl border border-surface-variant p-4 flex flex-col sm:flex-row items-center justify-between gap-4">
            <div className="flex items-center gap-3">
              <span className="relative flex h-3 w-3">
                <span className="animate-ping absolute inline-flex h-full w-full rounded-full bg-accent opacity-75" />
                <span className="relative inline-flex rounded-full h-3 w-3 bg-accent" />
              </span>
              <div className="flex items-center gap-2">
                <span className="font-label-md text-label-md font-medium text-on-surface">Unsaved changes:</span>
                <span className="font-label-sm text-label-sm text-on-surface-variant">Settings have been modified</span>
              </div>
              {saveError && (
                <span className="ml-2 font-label-sm text-label-sm text-error">({saveError})</span>
              )}
            </div>

            <div className="flex items-center gap-3 w-full sm:w-auto justify-end">
              <button
                type="button"
                onClick={() => setDraft({ ...settings })}
                className="w-full sm:w-auto px-5 py-2.5 rounded-lg bg-surface-container-lowest text-on-surface font-label-md text-label-md shadow-sm hover:bg-surface-container-low transition-colors border border-outline-variant"
              >
                Discard
              </button>
              <button
                type="button"
                onClick={handleSave}
                disabled={saving}
                className="w-full sm:w-auto px-7 py-2.5 rounded-lg bg-accent text-on-accent font-label-md text-label-md font-medium shadow-md hover:opacity-90 active:scale-[0.98] transition-all flex items-center justify-center gap-2 disabled:pointer-events-none disabled:opacity-60"
              >
                {saving ? (
                  <>
                    <span className="h-4 w-4 animate-spin rounded-full border-2 border-on-accent/30 border-t-on-accent" />
                    <span>Saving…</span>
                  </>
                ) : (
                  <>
                    <span>Save settings</span>
                    <span className="material-symbols-outlined text-base">check</span>
                  </>
                )}
              </button>
            </div>
          </aside>
        )}
      </div>

      {/* Unsaved changes dialog */}
      <UnsavedChangesDialog
        open={dialogOpen}
        saving={dialogSaving}
        onSave={confirmSave}
        onDiscard={handleDiscard}
        onCancel={handleCancel}
      />

      {/* Manual sector choosing modal when redirected after resume parsing cancellation */}
      {role === 'Talent' && auth.status === 'authenticated' && (
        <TalentOnboardingModal
          open={sectorModalOpen}
          userId={auth.user.id}
          initialStep="sectors"
          onClose={() => setSectorModalOpen(false)}
          onSectorsSaved={async (savedSectorIds) => {
            setSectorModalOpen(false)
            await refresh()
            updateDraft({
              forYou: {
                ...(draft?.forYou ?? settings.forYou),
                sectorIds: savedSectorIds,
                onboardingCompleted: true,
              },
            })
            navigate('/dashboard/talent/jobs?view=forYou')
          }}
        />
      )}
    </DashboardShell>
  )
}
