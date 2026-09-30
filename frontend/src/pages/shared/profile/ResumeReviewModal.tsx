import type { ParsedResumeProfile } from '../../../types'
import RichTextDisplay from '../../../components/ui/RichTextDisplay.tsx'

interface ResumeReviewModalProps {
  open: boolean
  profile: ParsedResumeProfile | null
  onApply: () => void
  onCancel: () => void
}

export default function ResumeReviewModal({
  open,
  profile,
  onApply,
  onCancel,
}: ResumeReviewModalProps) {
  if (!open || !profile) return null

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4">
      {/* Backdrop */}
      <div
        className="absolute inset-0 bg-inverse-surface/40 backdrop-blur-sm"
        onClick={onCancel}
      />

      {/* Dialog */}
      <div className="relative z-10 w-full max-w-2xl max-h-[90vh] flex flex-col rounded-xl border border-surface-variant bg-surface-container-lowest shadow-xl">
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
            {/* Headline & Profession */}
            {(profile.headline || profile.currentProfession) && (
              <div>
                <h3 className="font-label-md text-label-md font-semibold text-on-surface mb-2">
                  Headline & Profession
                </h3>
                <div className="rounded-lg border border-outline-variant bg-surface-container-low p-3 space-y-1">
                  {profile.headline && (
                    <p className="font-body-sm text-body-sm text-on-surface">
                      <span className="font-medium text-on-surface-variant">Headline: </span>
                      {profile.headline}
                    </p>
                  )}
                  {profile.currentProfession && (
                    <p className="font-body-sm text-body-sm text-on-surface">
                      <span className="font-medium text-on-surface-variant">Profession: </span>
                      {profile.currentProfession}
                    </p>
                  )}
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
            onClick={onApply}
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
