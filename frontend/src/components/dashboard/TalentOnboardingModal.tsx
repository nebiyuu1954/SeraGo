import { useEffect, useRef, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import {
  getApiErrorMessage,
  getStoredAuthTokens,
  parseResume,
  patchSettingsCategory,
  uploadFile,
} from '../../api'
import { useSectorsQuery } from '../../hooks/query.ts'
import { useToast } from './Toast.tsx'
import type { SectorResponse } from '../../types'
import { cn } from '../../lib/cn.ts'
import {
  getSampleRolesForSector,
  MAX_FOR_YOU_SECTORS,
} from '../../lib/sectorMatching.ts'

interface TalentOnboardingModalProps {
  open: boolean
  userId: string
  onClose: () => void
  onSectorsSaved: (sectorIds: string[]) => void
  initialStep?: OnboardingStep
}

type OnboardingStep = 'cv' | 'sectors'

export default function TalentOnboardingModal({
  open,
  userId,
  onClose,
  onSectorsSaved,
  initialStep = 'cv',
}: TalentOnboardingModalProps) {
  const navigate = useNavigate()
  const { showToast } = useToast()
  const { sectors, isLoading: sectorsLoading } = useSectorsQuery(open)

  const [step, setStep] = useState<OnboardingStep>(initialStep)
  const [isDragging, setIsDragging] = useState(false)
  const [uploading, setUploading] = useState(false)
  const [parsingPhase, setParsingPhase] = useState<'uploading' | 'analyzing'>('uploading')
  const [uploadError, setUploadError] = useState<string | null>(null)

  useEffect(() => {
    if (open) {
      setStep(initialStep)
    }
  }, [open, initialStep])

  // Sector selection state (Step 2)
  const [selectedSectorIds, setSelectedSectorIds] = useState<string[]>([])
  const [previewSectorId, setPreviewSectorId] = useState<string | null>(null)
  const [savingSectors, setSavingSectors] = useState(false)

  const fileInputRef = useRef<HTMLInputElement>(null)

  // Dismiss helper
  const handleDismiss = () => {
    if (uploading) return
    localStorage.setItem(`serago_onboarding_dismissed_${userId}`, 'true')
    onClose()
  }

  // Auto-select preview sector when sectors load or first selection happens
  useEffect(() => {
    if (!previewSectorId && sectors.length > 0) {
      setPreviewSectorId(sectors[0].id)
    }
  }, [sectors, previewSectorId])

  if (!open) return null

  // CV Upload Handler
  const processResumeFile = async (file: File) => {
    if (file.type !== 'application/pdf') {
      setUploadError('Please select a PDF document.')
      return
    }
    if (file.size > 10 * 1024 * 1024) {
      setUploadError('File size must be under 10MB.')
      return
    }

    const tokens = getStoredAuthTokens()
    if (!tokens) {
      setUploadError('You are not signed in.')
      return
    }

    setUploadError(null)
    setUploading(true)
    setParsingPhase('uploading')

    try {
      // 1. Upload file to S3 / storage
      const resumeKey = await uploadFile(file, 'resume')

      // 2. Extract sections via AI
      setParsingPhase('analyzing')
      const result = await parseResume(tokens.accessToken, resumeKey)

      // Close modal and navigate directly to profile page with state
      onClose()
      navigate('/dashboard/talent/profile', {
        state: {
          fromOnboarding: true,
          autoParsedResume: {
            resumeUrl: resumeKey,
            parsedProfile: result.profile ?? {
              headline: null,
              about: null,
              skills: [],
              experience: [],
              education: [],
              currentProfession: null,
              currentIndustry: null,
              experienceLevel: null,
              yearsOfExperience: null,
            },
            fieldsFound: result.fieldsFound ?? 0,
          },
        },
      })
    } catch (err) {
      setUploadError(getApiErrorMessage(err))
      setUploading(false)
    }
  }

  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0]
    if (file) {
      processResumeFile(file)
    }
  }

  const handleDrop = (e: React.DragEvent) => {
    e.preventDefault()
    setIsDragging(false)
    const file = e.dataTransfer.files?.[0]
    if (file) {
      processResumeFile(file)
    }
  }

  const toggleSector = (sector: SectorResponse) => {
    setPreviewSectorId(sector.id)
    setSelectedSectorIds((prev) => {
      if (prev.includes(sector.id)) {
        return prev.filter((id) => id !== sector.id)
      }
      if (prev.length < MAX_FOR_YOU_SECTORS) {
        return [...prev, sector.id]
      }
      return prev
    })
  }

  const handleSaveSectors = async () => {
    if (selectedSectorIds.length === 0) return
    const tokens = getStoredAuthTokens()
    if (!tokens) return

    setSavingSectors(true)
    try {
      await patchSettingsCategory(
        tokens.accessToken,
        'forYou',
        JSON.stringify({
          sectorIds: selectedSectorIds,
          onboardingCompleted: true,
        }),
      )
      localStorage.setItem(`serago_onboarding_dismissed_${userId}`, 'true')

      const sectorNames = sectors
        .filter((s) => selectedSectorIds.includes(s.id))
        .map((s) => s.name)
        .join(', ')

      showToast(
        `For You feed ready! Showing jobs in ${sectorNames || 'selected sectors'}.`,
        'success',
      )

      onSectorsSaved(selectedSectorIds)
      onClose()
    } catch (err) {
      showToast(getApiErrorMessage(err), 'error')
    } finally {
      setSavingSectors(false)
    }
  }

  const currentPreviewSector = sectors.find((s) => s.id === previewSectorId)
  const currentPreviewSectorRoles = getSampleRolesForSector(currentPreviewSector?.name)

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4">
      {/* Backdrop */}
      <div
        className="absolute inset-0 bg-inverse-surface/50 backdrop-blur-sm transition-opacity"
        onClick={handleDismiss}
      />

      {/* Modal Dialog */}
      <div className="relative z-10 w-full max-w-2xl max-h-[92vh] flex flex-col rounded-2xl border border-surface-variant bg-surface-container-lowest shadow-2xl overflow-hidden animate-in fade-in zoom-in-95 duration-200">
        {/* Top Header */}
        <div className="flex items-center justify-between border-b border-surface-variant px-6 py-4.5 bg-surface-container-lowest">
          <div className="flex items-center gap-2.5">
            {step === 'sectors' && initialStep !== 'sectors' && (
              <button
                type="button"
                onClick={() => setStep('cv')}
                aria-label="Back to CV upload"
                className="mr-1 rounded-lg p-1.5 text-on-surface-variant hover:bg-surface-container-low transition-colors"
              >
                <span className="material-symbols-outlined text-lg">arrow_back</span>
              </button>
            )}
            <div className="flex h-9 w-9 items-center justify-center rounded-xl bg-primary/10 text-primary">
              <span className="material-symbols-outlined text-[20px]">
                {step === 'cv' ? 'auto_awesome' : 'work'}
              </span>
            </div>
            <div>
              <h2 className="font-headline-md text-headline-md font-bold text-on-surface">
                {step === 'cv'
                  ? 'Welcome to SeraGo!'
                  : 'What kind of jobs do you want to see?'}
              </h2>
              <p className="font-body-sm text-body-sm text-on-surface-variant">
                {step === 'cv'
                  ? 'Let’s get your profile ready and personalize your opportunities.'
                  : 'Choose up to 2 sectors to shape your personalized For You feed.'}
              </p>
            </div>
          </div>
          <button
            type="button"
            onClick={handleDismiss}
            disabled={uploading}
            aria-label="Close modal"
            className="rounded-lg p-1.5 text-on-surface-variant hover:bg-surface-container-low transition-colors disabled:opacity-40 disabled:pointer-events-none"
          >
            <span className="material-symbols-outlined text-xl">close</span>
          </button>
        </div>

        {/* Modal Body */}
        <div className="flex-1 overflow-y-auto p-6 space-y-6">
          {step === 'cv' ? (
            /* ==================== STEP 1: CV / RESUME UPLOAD ==================== */
            <div className="space-y-6">
              {uploadError && (
                <div
                  role="alert"
                  className="flex items-center gap-2 rounded-xl border border-error/30 bg-error-container/40 p-3.5 font-label-md text-label-md text-on-error-container"
                >
                  <span className="material-symbols-outlined text-lg text-error">error</span>
                  <span>{uploadError}</span>
                </div>
              )}

              {uploading ? (
                /* Uploading & Parsing Loading State */
                <div className="flex flex-col items-center justify-center rounded-2xl border border-primary/20 bg-primary/5 py-12 px-6 text-center">
                  <div className="relative mb-5 flex h-16 w-16 items-center justify-center rounded-full bg-primary/15 text-primary">
                    <span className="material-symbols-outlined text-3xl animate-spin">
                      progress_activity
                    </span>
                    <span className="absolute -top-1 -right-1 flex h-5 w-5 items-center justify-center rounded-full bg-primary text-on-primary text-xs">
                      <span className="material-symbols-outlined text-[14px]">auto_awesome</span>
                    </span>
                  </div>
                  <h3 className="font-headline-md text-headline-md font-semibold text-on-surface">
                    {parsingPhase === 'uploading'
                      ? 'Uploading your resume…'
                      : 'AI is analyzing your experience…'}
                  </h3>
                  <p className="mt-2 max-w-md font-body-sm text-body-sm text-on-surface-variant">
                    {parsingPhase === 'uploading'
                      ? 'Securely transferring your document.'
                      : 'Extracting your skills, past roles, education, and finding your best career sectors.'}
                  </p>
                  <div className="mt-5 flex items-center gap-2 rounded-full bg-surface-container-lowest px-4 py-1.5 border border-surface-variant shadow-sm text-xs font-medium text-primary">
                    <span className="h-2 w-2 rounded-full bg-primary animate-pulse" />
                    Directing to profile review in a moment…
                  </div>
                </div>
              ) : (
                /* Drag & Drop Upload Zone */
                <>
                  <input
                    ref={fileInputRef}
                    type="file"
                    accept="application/pdf"
                    onChange={handleFileChange}
                    className="hidden"
                  />
                  <div
                    onDragOver={(e) => {
                      e.preventDefault()
                      setIsDragging(true)
                    }}
                    onDragLeave={() => setIsDragging(false)}
                    onDrop={handleDrop}
                    onClick={() => fileInputRef.current?.click()}
                    className={cn(
                      'group relative flex cursor-pointer flex-col items-center justify-center rounded-2xl border-2 border-dashed p-8 text-center transition-all',
                      isDragging
                        ? 'border-primary bg-primary/10 shadow-md'
                        : 'border-outline-variant hover:border-primary/60 hover:bg-surface-container-low/60 bg-surface-container-lowest',
                    )}
                  >
                    <div className="flex h-14 w-14 items-center justify-center rounded-2xl bg-primary/10 text-primary transition-transform group-hover:scale-105">
                      <span className="material-symbols-outlined text-3xl">upload_file</span>
                    </div>
                    <h3 className="mt-4 font-label-lg text-label-lg font-bold text-on-surface">
                      Drop your CV or Resume here
                    </h3>
                    <p className="mt-1 font-body-sm text-body-sm text-on-surface-variant">
                      or <span className="font-semibold text-primary underline">browse your computer</span>
                    </p>
                    <div className="mt-3 flex items-center gap-2 text-[12px] text-on-surface-variant/80">
                      <span className="material-symbols-outlined text-[15px]">picture_as_pdf</span>
                      <span>PDF format up to 10MB</span>
                    </div>
                  </div>

                  {/* Value Highlights */}
                  <div className="grid grid-cols-1 sm:grid-cols-3 gap-3">
                    <div className="flex items-start gap-2.5 rounded-xl border border-surface-variant bg-surface-container-low/40 p-3">
                      <span className="material-symbols-outlined text-primary text-lg mt-0.5">
                        bolt
                      </span>
                      <div>
                        <h4 className="font-label-md text-label-md font-semibold text-on-surface">
                          Instant Profile
                        </h4>
                        <p className="font-body-xs text-[11px] text-on-surface-variant">
                          Pre-fills your skills, roles & education automatically.
                        </p>
                      </div>
                    </div>
                    <div className="flex items-start gap-2.5 rounded-xl border border-surface-variant bg-surface-container-low/40 p-3">
                      <span className="material-symbols-outlined text-primary text-lg mt-0.5">
                        auto_awesome
                      </span>
                      <div>
                        <h4 className="font-label-md text-label-md font-semibold text-on-surface">
                          Auto-Matching
                        </h4>
                        <p className="font-body-xs text-[11px] text-on-surface-variant">
                          Detects your top industry sectors for the feed.
                        </p>
                      </div>
                    </div>
                    <div className="flex items-start gap-2.5 rounded-xl border border-surface-variant bg-surface-container-low/40 p-3">
                      <span className="material-symbols-outlined text-primary text-lg mt-0.5">
                        edit_note
                      </span>
                      <div>
                        <h4 className="font-label-md text-label-md font-semibold text-on-surface">
                          You Stay in Control
                        </h4>
                        <p className="font-body-xs text-[11px] text-on-surface-variant">
                          Review, tweak, and approve everything before saving.
                        </p>
                      </div>
                    </div>
                  </div>
                </>
              )}
            </div>
          ) : (
            /* ==================== STEP 2: SECTORS SELECTION WITH LIVE PREVIEW ==================== */
            <div className="space-y-6">
              {/* Sector selector pills */}
              <div>
                <div className="flex items-center justify-between mb-3">
                  <label className="font-label-md text-label-md font-semibold text-on-surface">
                    Select your sectors
                  </label>
                  <span className="font-label-sm text-label-sm font-semibold text-primary">
                    {selectedSectorIds.length}/{MAX_FOR_YOU_SECTORS} selected
                  </span>
                </div>

                {sectorsLoading ? (
                  <div className="flex flex-wrap gap-2">
                    {[1, 2, 3, 4, 5, 6].map((i) => (
                      <div
                        key={i}
                        className="h-9 w-32 animate-pulse rounded-lg bg-surface-container-low"
                      />
                    ))}
                  </div>
                ) : (
                  <div className="flex flex-wrap gap-2 max-h-48 overflow-y-auto p-1 border border-surface-variant/50 rounded-xl bg-surface-container-low/20">
                    {sectors.map((s) => {
                      const isSelected = selectedSectorIds.includes(s.id)
                      const isPreviewing = previewSectorId === s.id
                      return (
                        <button
                          key={s.id}
                          type="button"
                          onClick={() => toggleSector(s)}
                          className={cn(
                            'group flex items-center gap-1.5 rounded-lg px-3 py-2 text-label-sm font-medium transition-all text-left',
                            isSelected
                              ? 'bg-primary text-on-primary shadow-sm ring-2 ring-primary/40'
                              : isPreviewing
                                ? 'bg-surface-container-high border border-primary/50 text-primary'
                                : 'bg-surface-container-lowest border border-outline-variant text-on-surface hover:border-primary/50 hover:bg-surface-container-low',
                          )}
                        >
                          <span
                            className={cn(
                              'material-symbols-outlined text-[16px]',
                              isSelected ? 'text-on-primary' : 'text-primary/70',
                            )}
                          >
                            {isSelected ? 'check_circle' : 'work_outline'}
                          </span>
                          <span>{s.name}</span>
                        </button>
                      )
                    })}
                  </div>
                )}
              </div>

              {/* Typical Roles / Job Titles Container */}
              <div className="rounded-xl border border-surface-variant bg-surface-container-lowest p-4 shadow-sm">
                <div className="flex items-center justify-between mb-3 border-b border-surface-variant pb-2.5">
                  <div className="flex items-center gap-2">
                    <span className="material-symbols-outlined text-primary text-lg">work</span>
                    <span className="font-label-md text-label-md font-semibold text-on-surface">
                      Typical roles in {currentPreviewSector?.name ?? 'Selected Sector'}
                    </span>
                  </div>
                  <span className="text-[12px] font-medium text-on-surface-variant">
                    Included career paths
                  </span>
                </div>

                <div className="flex flex-wrap gap-2 py-1">
                  {currentPreviewSectorRoles.map((role) => (
                    <span
                      key={role}
                      className="inline-flex items-center gap-1.5 rounded-lg border border-primary/20 bg-surface-container-low px-3 py-1.5 font-label-sm text-label-sm font-medium text-on-surface shadow-xs transition-colors hover:border-primary/40 hover:bg-primary/5"
                    >
                      <span className="material-symbols-outlined text-[14px] text-primary">trending_up</span>
                      <span>{role}</span>
                    </span>
                  ))}
                </div>
              </div>
            </div>
          )}
        </div>

        {/* Modal Footer Controls */}
        <div className="flex flex-col-reverse sm:flex-row items-center justify-between gap-3 border-t border-surface-variant px-6 py-4 bg-surface-container-lowest">
          {step === 'cv' ? (
            <>
              <button
                type="button"
                onClick={handleDismiss}
                disabled={uploading}
                className="text-label-sm font-medium text-on-surface-variant hover:text-on-surface transition-colors disabled:opacity-40 disabled:pointer-events-none"
              >
                Skip for now
              </button>

              <button
                type="button"
                onClick={() => setStep('sectors')}
                disabled={uploading}
                className="inline-flex items-center gap-2 rounded-xl border border-primary/40 bg-primary/5 px-4.5 py-2.5 font-label-md text-label-md font-semibold text-primary hover:bg-primary/10 transition-colors disabled:opacity-40 disabled:pointer-events-none"
              >
                <span>I don't have a CV right now</span>
                <span className="material-symbols-outlined text-lg">arrow_forward</span>
              </button>
            </>
          ) : (
            <>
              {initialStep !== 'sectors' ? (
                <button
                  type="button"
                  onClick={() => setStep('cv')}
                  disabled={savingSectors}
                  className="inline-flex items-center gap-1.5 rounded-lg border border-outline-variant px-4 py-2 font-label-md text-label-md text-on-surface hover:bg-surface-container-low transition-colors"
                >
                  <span className="material-symbols-outlined text-lg">arrow_back</span>
                  <span>Back</span>
                </button>
              ) : (
                <div />
              )}

              <div className="flex items-center gap-2">
                <button
                  type="button"
                  onClick={handleDismiss}
                  className="px-3 py-2 text-label-sm font-medium text-on-surface-variant hover:text-on-surface transition-colors"
                >
                  Skip
                </button>
                <button
                  type="button"
                  onClick={handleSaveSectors}
                  disabled={selectedSectorIds.length === 0 || savingSectors}
                  className="inline-flex items-center gap-2 rounded-xl bg-primary px-5 py-2.5 font-label-md text-label-md font-semibold text-on-primary hover:opacity-90 transition-opacity disabled:pointer-events-none disabled:opacity-50 shadow-sm"
                >
                  {savingSectors ? (
                    <>
                      <span className="h-4 w-4 animate-spin rounded-full border-2 border-on-primary/30 border-t-on-primary" />
                      <span>Saving feed…</span>
                    </>
                  ) : (
                    <>
                      <span className="material-symbols-outlined text-lg">check</span>
                      <span>Save & View My Feed</span>
                    </>
                  )}
                </button>
              </div>
            </>
          )}
        </div>
      </div>
    </div>
  )
}
