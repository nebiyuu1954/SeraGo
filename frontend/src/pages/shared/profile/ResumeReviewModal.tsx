import { useEffect, useState } from 'react'
import type { ParsedResumeProfile, JobResponse } from '../../../types'
import RichTextDisplay from '../../../components/ui/RichTextDisplay.tsx'
import { useSectorsQuery } from '../../../hooks/query.ts'
import { matchSuggestedSectorIds, MAX_FOR_YOU_SECTORS } from '../../../lib/sectorMatching.ts'
import { fetchJobs, getStoredAuthTokens } from '../../../api'
import { cn } from '../../../lib/cn.ts'

interface ResumeReviewModalProps {
  open: boolean
  profile: ParsedResumeProfile | null
  onApply: (selectedSectorIds: string[]) => void
  onCancel: () => void
}

export default function ResumeReviewModal({
  open,
  profile,
  onApply,
  onCancel,
}: ResumeReviewModalProps) {
  const { sectors, isLoading: sectorsLoading } = useSectorsQuery(open)
  const [selectedSectorIds, setSelectedSectorIds] = useState<string[]>([])
  const [isPickerExpanded, setIsPickerExpanded] = useState<boolean>(false)
  const [previewJobs, setPreviewJobs] = useState<JobResponse[]>([])
  const [previewJobsLoading, setPreviewJobsLoading] = useState<boolean>(false)

  useEffect(() => {
    if (profile && sectors.length > 0) {
      const initial = matchSuggestedSectorIds(profile, sectors)
      setSelectedSectorIds(initial)
    }
  }, [profile, sectors])

  const primarySectorId = selectedSectorIds[0] || (sectors[0]?.id ?? '')
  const primarySector = sectors.find((s) => s.id === primarySectorId)

  useEffect(() => {
    if (!open || !primarySectorId) return
    const tokens = getStoredAuthTokens()
    if (!tokens?.accessToken) return

    let cancelled = false
    setPreviewJobsLoading(true)

    fetchJobs({ sectorId: primarySectorId, pageSize: 4 }, tokens.accessToken)
      .then((data) => {
        if (!cancelled) {
          setPreviewJobs(data.items ?? [])
        }
      })
      .catch(() => {
        if (!cancelled) setPreviewJobs([])
      })
      .finally(() => {
        if (!cancelled) setPreviewJobsLoading(false)
      })

    return () => {
      cancelled = true
    }
  }, [open, primarySectorId])

  if (!open || !profile) return null

  const toggleSector = (id: string) => {
    setSelectedSectorIds((prev) => {
      if (prev.includes(id)) {
        return prev.filter((s) => s !== id)
      }
      if (prev.length < MAX_FOR_YOU_SECTORS) {
        return [...prev, id]
      }
      return prev
    })
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4">
      {/* Backdrop */}
      <div
        className="absolute inset-0 bg-inverse-surface/40 backdrop-blur-sm"
        onClick={onCancel}
      />

      {/* Dialog */}
      <div className="relative z-10 w-full max-w-5xl max-h-[90vh] flex flex-col rounded-2xl border border-surface-variant bg-surface-container-lowest shadow-2xl">
        <div className="p-6 border-b border-surface-variant flex shrink-0 items-center justify-between">
          <div>
            <h2 className="font-headline-md text-headline-md text-on-surface mb-1">
              Resume parsed successfully
            </h2>
            <p className="font-body-sm text-body-sm text-on-surface-variant">
              Here's what we found. You can review and apply these fields to your profile.
            </p>
          </div>
          <button
            type="button"
            onClick={onCancel}
            className="rounded-lg p-2 text-on-surface-variant hover:bg-surface-container-low transition-colors"
          >
            <span className="material-symbols-outlined">close</span>
          </button>
        </div>

        <div className="p-6 overflow-y-auto flex-1 bg-surface-container-lowest">
          <div className="grid gap-6">
            {/* Highest Matching Sector & Jobs Preview */}
            <div className="rounded-xl border border-primary/25 bg-primary/5 p-4.5 transition-all">
              <div className="flex items-start justify-between gap-4">
                <div className="flex items-start gap-3">
                  <div className="mt-0.5 flex h-9 w-9 shrink-0 items-center justify-center rounded-lg bg-primary/15 text-primary">
                    <span className="material-symbols-outlined text-[20px]">auto_awesome</span>
                  </div>
                  <div>
                    <p className="font-body-md text-body-md text-on-surface">
                      Based on the resume you provided, the highest matching sector is{' '}
                      <strong className="font-bold text-primary">
                        {primarySector?.name ?? 'Loading...'}
                      </strong>
                      .{' '}
                      <button
                        type="button"
                        onClick={() => setIsPickerExpanded((prev) => !prev)}
                        className="font-semibold text-primary underline underline-offset-2 hover:text-primary/80 transition-colors cursor-pointer inline-flex items-center gap-0.5"
                      >
                        To change, click here
                        <span className="material-symbols-outlined text-[16px]">
                          {isPickerExpanded ? 'expand_less' : 'expand_more'}
                        </span>
                      </button>
                    </p>
                    {selectedSectorIds.length > 1 && (
                      <p className="font-body-xs text-[12px] text-on-surface-variant mt-1">
                        Also following:{' '}
                        {selectedSectorIds
                          .filter((id) => id !== primarySectorId)
                          .map((id) => sectors.find((s) => s.id === id)?.name)
                          .filter(Boolean)
                          .join(', ')}
                      </p>
                    )}
                  </div>
                </div>
                <span className="shrink-0 font-label-sm text-label-sm font-semibold text-primary rounded-full bg-primary/10 px-2.5 py-1">
                  {selectedSectorIds.length}/{MAX_FOR_YOU_SECTORS} selected
                </span>
              </div>

              {/* Collapsible Sector Dropdown/Picker */}
              {isPickerExpanded && (
                <div className="mt-4 border-t border-primary/15 pt-3.5">
                  <div className="flex items-center justify-between mb-2">
                    <span className="font-label-sm text-label-sm font-semibold text-on-surface">
                      Pick up to 2 sectors to follow:
                    </span>
                    <button
                      type="button"
                      onClick={() => setIsPickerExpanded(false)}
                      className="text-[12px] font-medium text-primary hover:underline"
                    >
                      Done
                    </button>
                  </div>
                  {sectorsLoading ? (
                    <div className="h-10 animate-pulse rounded-lg bg-surface-container-low" />
                  ) : (
                    <div className="flex flex-wrap gap-2 max-h-48 overflow-y-auto pr-1">
                      {sectors.map((sector) => {
                        const checked = selectedSectorIds.includes(sector.id)
                        const atCap = !checked && selectedSectorIds.length >= MAX_FOR_YOU_SECTORS
                        return (
                          <button
                            key={sector.id}
                            type="button"
                            aria-pressed={checked}
                            disabled={atCap}
                            onClick={() => toggleSector(sector.id)}
                            className={cn(
                              'inline-flex items-center gap-1.5 rounded-full px-3 py-1 font-label-sm text-label-sm font-medium transition-all',
                              checked
                                ? 'bg-primary text-on-primary shadow-sm ring-1 ring-primary'
                                : atCap
                                  ? 'cursor-not-allowed border border-surface-variant bg-surface-container-lowest text-on-surface-variant/40 opacity-50'
                                  : 'border border-outline-variant bg-surface-container-lowest text-on-surface-variant hover:border-primary/40 hover:bg-surface-container-low',
                            )}
                          >
                            {checked && (
                              <span className="material-symbols-outlined text-[14px]">check</span>
                            )}
                            <span>{sector.name}</span>
                          </button>
                        )
                      })}
                    </div>
                  )}
                </div>
              )}

              {/* 3 or 4 Live Jobs in this Sector */}
              <div className="mt-4 border-t border-primary/15 pt-3.5">
                <div className="flex items-center justify-between mb-2.5">
                  <span className="font-label-sm text-label-sm font-semibold text-on-surface flex items-center gap-1.5">
                    <span className="material-symbols-outlined text-[16px] text-primary">work</span>
                    Featured jobs in {primarySector?.name ?? 'this sector'}
                  </span>
                  <span className="text-[11px] text-on-surface-variant">Live opportunities</span>
                </div>

                {previewJobsLoading ? (
                  <div className="grid gap-2 sm:grid-cols-2">
                    {[1, 2, 3, 4].map((i) => (
                      <div key={i} className="h-14 animate-pulse rounded-lg bg-surface-container-low" />
                    ))}
                  </div>
                ) : previewJobs.length > 0 ? (
                  <div className="grid gap-2 sm:grid-cols-2">
                    {previewJobs.slice(0, 4).map((job) => (
                      <div
                        key={job.id}
                        className="flex flex-col justify-between rounded-lg border border-surface-variant/70 bg-surface-container-lowest p-2.5 shadow-xs"
                      >
                        <div className="min-w-0">
                          <h4 className="font-label-sm text-label-sm font-semibold text-on-surface truncate">
                            {job.title}
                          </h4>
                          <div className="flex items-center gap-1.5 mt-0.5 text-[11px] text-on-surface-variant truncate">
                            <span className="truncate">{job.company}</span>
                            <span>•</span>
                            <span className="truncate">{job.location || 'Ethiopia'}</span>
                          </div>
                        </div>
                        <div className="flex items-center gap-1.5 mt-2 shrink-0">
                          {job.workMode && (
                            <span className="rounded bg-secondary-container/50 px-1.5 py-0.5 text-[10px] font-medium text-on-secondary-container">
                              {job.workMode}
                            </span>
                          )}
                          <span className="rounded bg-primary/10 px-1.5 py-0.5 text-[10px] font-medium text-primary">
                            {job.jobType || 'Full-time'}
                          </span>
                        </div>
                      </div>
                    ))}
                  </div>
                ) : (
                  <p className="text-[12px] text-on-surface-variant/70 italic py-1">
                    No active job postings in this sector at the moment.
                  </p>
                )}
              </div>
            </div>

            {/* Profession */}
            {profile.currentProfession && (
              <div>
                <h3 className="font-label-md text-label-md font-semibold text-on-surface mb-2">
                  Profession
                </h3>
                <div className="rounded-lg border border-outline-variant bg-surface-container-low p-3">
                  <p className="font-body-sm text-body-sm text-on-surface">
                    <span className="font-medium text-on-surface-variant">Profession: </span>
                    {profile.currentProfession}
                  </p>
                </div>
              </div>
            )}

            {/* About */}
            {profile.about && (
              <div>
                <h3 className="font-label-md text-label-md font-semibold text-on-surface mb-2">
                  About Summary
                </h3>
                <div className="rounded-lg border border-outline-variant bg-surface-container-low p-3">
                  <p className="font-body-sm text-body-sm text-on-surface line-clamp-3">
                    {profile.about}
                  </p>
                </div>
              </div>
            )}

            {/* Experience & Industry */}
            {(profile.experienceLevel || profile.yearsOfExperience !== null || profile.currentIndustry) && (
              <div>
                <h3 className="font-label-md text-label-md font-semibold text-on-surface mb-2">
                  Experience Level
                </h3>
                <div className="rounded-lg border border-outline-variant bg-surface-container-low p-3 flex flex-wrap gap-4">
                  {profile.experienceLevel && (
                    <div>
                      <span className="block font-label-sm text-label-sm text-on-surface-variant mb-0.5">Level</span>
                      <span className="font-body-sm text-body-sm text-on-surface font-medium">{profile.experienceLevel}</span>
                    </div>
                  )}
                  {profile.yearsOfExperience !== null && (
                    <div>
                      <span className="block font-label-sm text-label-sm text-on-surface-variant mb-0.5">Years</span>
                      <span className="font-body-sm text-body-sm text-on-surface font-medium">{profile.yearsOfExperience}</span>
                    </div>
                  )}
                  {profile.currentIndustry && (
                    <div>
                      <span className="block font-label-sm text-label-sm text-on-surface-variant mb-0.5">Industry</span>
                      <span className="font-body-sm text-body-sm text-on-surface font-medium">{profile.currentIndustry}</span>
                    </div>
                  )}
                </div>
              </div>
            )}

            {/* Skills */}
            {profile.skills.length > 0 && (
              <div>
                <h3 className="font-label-md text-label-md font-semibold text-on-surface mb-2">
                  Skills ({profile.skills.length})
                </h3>
                <div className="flex flex-wrap gap-2">
                  {profile.skills.map((skill, i) => (
                    <span
                      key={i}
                      className="inline-flex items-center rounded-full border border-primary/20 bg-primary-container/20 px-3 py-1 font-label-sm text-label-sm text-on-primary-container"
                    >
                      {skill}
                    </span>
                  ))}
                </div>
              </div>
            )}

            {/* Work Experience */}
            {profile.experience.length > 0 && (
              <div>
                <h3 className="font-label-md text-label-md font-semibold text-on-surface mb-2">
                  Work Experience ({profile.experience.length})
                </h3>
                <div className="space-y-3">
                  {profile.experience.map((exp, i) => (
                    <div key={i} className="rounded-lg border border-outline-variant bg-surface-container-low p-4">
                      <div className="flex justify-between items-start gap-4 mb-1.5">
                        <div>
                          <p className="font-label-md text-label-md text-on-surface font-semibold">{exp.title}</p>
                          <p className="font-body-sm text-body-sm text-on-surface-variant font-medium">{exp.company}</p>
                        </div>
                        {(exp.startDate || exp.endDate) && (
                          <span className="shrink-0 font-label-sm text-label-sm text-on-surface-variant/80 mt-0.5">
                            {exp.startDate || '?'} – {exp.endDate || 'Present'}
                          </span>
                        )}
                      </div>
                      {exp.description && (
                        <div className="mt-2 overflow-hidden max-h-32 relative">
                          <RichTextDisplay html={exp.description} />
                          <div className="absolute bottom-0 left-0 right-0 h-8 bg-gradient-to-t from-surface-container-low to-transparent" />
                        </div>
                      )}
                    </div>
                  ))}
                </div>
              </div>
            )}

            {/* Education */}
            {profile.education.length > 0 && (
              <div>
                <h3 className="font-label-md text-label-md font-semibold text-on-surface mb-2">
                  Education ({profile.education.length})
                </h3>
                <div className="space-y-3">
                  {profile.education.map((edu, i) => (
                    <div key={i} className="rounded-lg border border-outline-variant bg-surface-container-low p-4">
                      <div className="flex justify-between items-start gap-4 mb-1.5">
                        <div>
                          <p className="font-label-md text-label-md text-on-surface font-semibold">
                            {edu.degree ? `${edu.degree} (${edu.level})` : edu.level}
                          </p>
                          <p className="font-body-sm text-body-sm text-on-surface-variant font-medium">{edu.institution}</p>
                        </div>
                        {(edu.startYear || edu.endYear) && (
                          <span className="shrink-0 font-label-sm text-label-sm text-on-surface-variant/80 mt-0.5">
                            {edu.startYear || '?'} – {edu.endYear || 'Present'}
                          </span>
                        )}
                      </div>
                      {edu.gpa && (
                        <p className="font-label-sm text-label-sm text-on-surface mt-2">
                          <span className="font-medium text-on-surface-variant">GPA:</span> {edu.gpa}
                        </p>
                      )}
                    </div>
                  ))}
                </div>
              </div>
            )}
          </div>
        </div>

        <div className="p-4 border-t border-surface-variant flex shrink-0 items-center justify-end gap-3 bg-surface-container-lowest rounded-b-xl">
          <button
            type="button"
            onClick={onCancel}
            className="px-4 py-2 rounded-lg font-label-md text-label-md text-on-surface-variant border border-outline-variant hover:bg-surface-container-low transition-colors"
          >
            Cancel
          </button>
          <button
            type="button"
            onClick={() => onApply(selectedSectorIds)}
            className="inline-flex items-center gap-2 px-5 py-2 rounded-lg font-label-md text-label-md font-medium text-on-primary bg-primary hover:opacity-90 transition-opacity"
          >
            <span className="material-symbols-outlined text-[18px]">check</span>
            Apply to Profile
          </button>
        </div>
      </div>
    </div>
  )
}
