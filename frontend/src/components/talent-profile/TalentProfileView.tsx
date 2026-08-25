import { useNavigate } from 'react-router-dom'
import type { ProfileSnapshot } from '../../types/profileSnapshot.ts'
import ResumeLink from '../ui/ResumeLink.tsx'

// ────────────────────── helpers ──────────────────────

function parseEducationHistory(raw: string | undefined): EducationEntry[] {
  if (!raw || raw === '[]') return []
  try {
    return JSON.parse(raw) as EducationEntry[]
  } catch {
    return []
  }
}

function parsePreferredLocations(raw: string | undefined): string[] {
  if (!raw || raw === '[]') return []
  try {
    return JSON.parse(raw) as string[]
  } catch {
    return []
  }
}

interface EducationEntry {
  level?: string
  institution?: string
  degree?: string
  gpa?: string
  startYear?: string
  endYear?: string
}

const EDUCATION_LEVEL_LABELS: Record<string, string> = {
  HighSchool: 'High School',
  Bachelors: "Bachelor's Degree",
  Masters: "Master's Degree",
  PhD: 'PhD / Doctorate',
}

function formatDate(iso: string): string {
  return new Date(iso).toLocaleDateString('en-US', {
    month: 'short',
    day: 'numeric',
    year: 'numeric',
  })
}

function locationString(snap: ProfileSnapshot): string | null {
  const parts = [snap.city, snap.country].filter(Boolean)
  return parts.length > 0 ? parts.join(', ') : null
}

function initialsOf(name: string): string {
  return name
    .split(/\s+/)
    .filter(Boolean)
    .map((n) => n[0])
    .join('')
    .slice(0, 2)
    .toUpperCase()
}

// ────────────────────── props ──────────────────────

export interface TalentProfileViewProps {
  /** The parsed profile snapshot — drives all visible data. */
  snapshot: ProfileSnapshot
  /** Application-specific metadata shown in the header badge (recruiter view). */
  application?: {
    jobTitle: string
    appliedAt?: string
    statusUpdatedAt?: string | null
    coverLetter?: string | null
    jobCompany?: string
  } | null
  /** Header badge text — defaults to "Available for hire". */
  headerBadge?: string
  /** Back button label — defaults to "Back to profile". */
  backLabel?: string
  /** Back button link target. */
  backTo?: string
  /** Optional content to render in the top-right area (e.g. info banner). */
  topRight?: React.ReactNode
}

// ────────────────────── main component ──────────────────────

export default function TalentProfileView({
  snapshot: snap,
  application,
  headerBadge,
  backLabel = 'Back to profile',
  backTo,
  topRight,
}: TalentProfileViewProps) {
  const navigate = useNavigate()

  const fullName = [snap.firstName, snap.middleName, snap.lastName]
    .filter(Boolean)
    .join(' ')

  const location = locationString(snap)
  const education = parseEducationHistory(snap.educationHistory)
  const preferredLocations = parsePreferredLocations(snap.preferredLocations)

  return (
    <div className="w-full max-w-7xl mx-auto flex flex-col gap-8">
      {/* Top bar: back button (left) + optional top-right content */}
      <div className="flex items-center justify-between">
        <button
          type="button"
          onClick={() => backTo ? navigate(backTo) : navigate(-1)}
          className="inline-flex items-center gap-1.5 font-label-md text-label-md text-on-surface-variant transition-colors hover:text-primary"
        >
          <span className="material-symbols-outlined text-lg">arrow_back</span>
          {backLabel}
        </button>
        {topRight && <div>{topRight}</div>}
      </div>

      {/* ═══════════════════════════ Header Section ═══════════════════════════ */}
      <section className="bg-surface-container-lowest border border-surface-variant rounded-xl p-6 flex flex-col md:flex-row items-start md:items-center justify-between gap-6 relative overflow-hidden">
        <div className="flex items-center gap-6 z-10">
          {snap.avatarUrl ? (
            <img
              className="w-20 h-20 rounded-full object-cover border border-surface-variant"
              src={snap.avatarUrl}
              alt={fullName}
            />
          ) : (
            <span className="flex w-20 h-20 items-center justify-center rounded-full bg-primary-container/60 font-headline-lg text-headline-lg font-semibold text-primary">
              {initialsOf(fullName)}
            </span>
          )}
          <div>
            <h1 className="font-headline-lg text-headline-lg text-primary mb-1">
              {fullName}
            </h1>
            {snap.headline && (
              <p className="font-body-lg text-body-lg text-on-surface-variant mb-2">
                {snap.headline}
              </p>
            )}
            <div className="flex items-center gap-4">
              {location && (
                <span className="flex items-center gap-1 font-label-sm text-label-sm text-secondary">
                  <span className="material-symbols-outlined text-[16px]">
                    location_on
                  </span>
                  {location}
                </span>
              )}
              {application ? (
                <span className="inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full bg-[#E8F5E9] text-[#2E7D32] font-label-sm text-label-sm border border-[#C8E6C9]">
                  <span className="w-2 h-2 rounded-full bg-[#4CAF50]" />
                  Applied to {application.jobTitle}
                </span>
              ) : headerBadge ? (
                <span className="inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full bg-[#E8F5E9] text-[#2E7D32] font-label-sm text-label-sm border border-[#C8E6C9]">
                  <span className="w-2 h-2 rounded-full bg-[#4CAF50]" />
                  {headerBadge}
                </span>
              ) : null}
            </div>
          </div>
        </div>
        {/* Decorative background element */}
        <div className="absolute top-0 right-0 w-64 h-full bg-gradient-to-l from-surface-container-low to-transparent opacity-50 z-0 pointer-events-none" />
      </section>

      {/* ═══════════════════════════ Two Column Layout ═══════════════════════════ */}
      <div className="flex flex-col lg:flex-row gap-8">
        {/* ──────── Left Column: Main Details (70%) ──────── */}
        <div className="w-full lg:w-[70%] flex flex-col gap-8">
          {/* About Card */}
          {snap.headline && (
            <section className="bg-surface-container-lowest border border-surface-variant rounded-xl p-6">
              <h2 className="font-headline-md text-headline-md text-primary mb-6 flex items-center gap-2">
                <span className="material-symbols-outlined">person</span>
                About
              </h2>
              {snap.about && (
                <p className="font-body-md text-body-md text-on-surface-variant mb-6 leading-relaxed">
                  {snap.about}
                </p>
              )}
              <div className="flex flex-wrap gap-3">
                {snap.currentIndustry && (
                  <div className="flex items-center gap-2 bg-surface-container-low px-3 py-1.5 rounded-md border border-surface-variant">
                    <span className="material-symbols-outlined text-[16px] text-secondary">
                      domain
                    </span>
                    <span className="font-label-sm text-label-sm text-on-surface">
                      {snap.currentIndustry}
                    </span>
                  </div>
                )}
                {snap.currentProfession && (
                  <div className="flex items-center gap-2 bg-surface-container-low px-3 py-1.5 rounded-md border border-surface-variant">
                    <span className="material-symbols-outlined text-[16px] text-secondary">
                      code
                    </span>
                    <span className="font-label-sm text-label-sm text-on-surface">
                      {snap.currentProfession}
                    </span>
                  </div>
                )}
              </div>
            </section>
          )}

          {/* Experience Card */}
          {(snap.experienceLevel || snap.yearsOfExperience != null) && (
            <section className="bg-surface-container-lowest border border-surface-variant rounded-xl p-6">
              <div className="flex items-center justify-between mb-6">
                <h2 className="font-headline-md text-headline-md text-primary flex items-center gap-2">
                  <span className="material-symbols-outlined">work</span>
                  Experience
                </h2>
                <div className="flex items-center gap-3">
                  {snap.yearsOfExperience != null && (
                    <span className="font-label-md text-label-md text-on-surface-variant bg-surface-container-low px-2 py-1 rounded">
                      {snap.yearsOfExperience} year{snap.yearsOfExperience !== 1 ? 's' : ''}
                    </span>
                  )}
                  {snap.experienceLevel && (
                    <span className="font-label-md text-label-md text-primary bg-primary-container/10 px-2 py-1 rounded">
                      {snap.experienceLevel} Level
                    </span>
                  )}
                </div>
              </div>
              <p className="font-body-md text-body-md text-on-surface-variant">
                {snap.headline
                  ? `${snap.yearsOfExperience ?? ''} years of experience as ${snap.headline}.`
                  : `${snap.yearsOfExperience ?? ''} years of professional experience.`}
              </p>
            </section>
          )}

          {/* Education Card */}
          {education.length > 0 && (
            <section className="bg-surface-container-lowest border border-surface-variant rounded-xl p-6">
              <h2 className="font-headline-md text-headline-md text-primary mb-6 flex items-center gap-2">
                <span className="material-symbols-outlined">school</span>
                Education
              </h2>
              <div className="flex flex-col gap-6">
                {education.map((entry, idx) => (
                  <div key={idx}>
                    {idx > 0 && (
                      <div className="h-px w-full bg-surface-variant" />
                    )}
                    <div className={idx > 0 ? 'mt-6' : undefined}>
                      <h3 className="font-headline-md text-headline-md text-on-surface mb-1">
                        {entry.degree || EDUCATION_LEVEL_LABELS[entry.level ?? ''] || entry.level || 'Degree'}
                      </h3>
                      {entry.institution && (
                        <p className="font-body-md text-body-md text-primary mb-2">
                          {entry.institution}
                        </p>
                      )}
                      <div className="flex items-center gap-4 font-label-sm text-label-sm text-on-surface-variant">
                        {(entry.startYear || entry.endYear) && (
                          <span className="flex items-center gap-1">
                            <span className="material-symbols-outlined text-[14px]">
                              calendar_today
                            </span>
                            {entry.startYear}
                            {entry.endYear ? ` - ${entry.endYear}` : ' - Present'}
                          </span>
                        )}
                        {entry.gpa && (
                          <span className="flex items-center gap-1">
                            <span className="material-symbols-outlined text-[14px]">
                              grade
                            </span>
                            GPA: {entry.gpa}
                          </span>
                        )}
                      </div>
                    </div>
                  </div>
                ))}
              </div>
            </section>
          )}

          {/* Cover Letter (recruiter view only) */}
          {application?.coverLetter && (
            <section className="bg-surface-container-lowest border border-surface-variant rounded-xl p-6">
              <h2 className="font-headline-md text-headline-md text-primary mb-6 flex items-center gap-2">
                <span className="material-symbols-outlined">description</span>
                Cover Letter
              </h2>
              <p className="font-body-md text-body-md text-on-surface-variant whitespace-pre-line leading-relaxed">
                {application.coverLetter}
              </p>
            </section>
          )}
        </div>

        {/* ──────── Right Column: Sidebar Details (30%) ──────── */}
        <div className="w-full lg:w-[30%] flex flex-col gap-6">
          {/* Skills Card */}
          {snap.skills && snap.skills.length > 0 && (
            <section className="bg-surface-container-lowest border border-surface-variant rounded-xl p-6">
              <h3 className="font-headline-md text-[20px] leading-7 text-primary mb-4 flex items-center gap-2">
                <span className="material-symbols-outlined">psychology</span>
                Top Skills
              </h3>
              <div className="flex flex-wrap gap-2">
                {snap.skills.map((skill) => (
                  <span
                    key={skill}
                    className="px-4 py-1.5 rounded-full bg-primary-container/10 text-primary font-label-md text-label-md border border-primary-container/20"
                  >
                    {skill}
                  </span>
                ))}
              </div>
            </section>
          )}

          {/* Links & Socials Card */}
          {(snap.linkedinUrl || snap.githubUrl || snap.portfolioUrl || snap.resumeUrl) && (
            <section className="bg-surface-container-lowest border border-surface-variant rounded-xl p-6">
              <h3 className="font-headline-md text-[20px] leading-7 text-primary mb-4 flex items-center gap-2">
                <span className="material-symbols-outlined">link</span>
                Links &amp; Socials
              </h3>
              <div className="flex flex-col gap-4 mb-6">
                {snap.portfolioUrl && (
                  <a
                    href={snap.portfolioUrl}
                    target="_blank"
                    rel="noreferrer"
                    className="flex items-center gap-3 text-on-surface hover:text-primary transition-colors group"
                  >
                    <span className="material-symbols-outlined text-secondary group-hover:text-primary">
                      language
                    </span>
                    <span className="font-body-md text-body-md underline-offset-4 group-hover:underline">
                      Portfolio Website
                    </span>
                  </a>
                )}
                {snap.githubUrl && (
                  <a
                    href={snap.githubUrl}
                    target="_blank"
                    rel="noreferrer"
                    className="flex items-center gap-3 text-on-surface hover:text-primary transition-colors group"
                  >
                    <span className="material-symbols-outlined text-secondary group-hover:text-primary">
                      code_blocks
                    </span>
                    <span className="font-body-md text-body-md underline-offset-4 group-hover:underline">
                      GitHub Profile
                    </span>
                  </a>
                )}
                {snap.linkedinUrl && (
                  <a
                    href={snap.linkedinUrl}
                    target="_blank"
                    rel="noreferrer"
                    className="flex items-center gap-3 text-on-surface hover:text-primary transition-colors group"
                  >
                    <span className="material-symbols-outlined text-secondary group-hover:text-primary">
                      connect_without_contact
                    </span>
                    <span className="font-body-md text-body-md underline-offset-4 group-hover:underline">
                      LinkedIn
                    </span>
                  </a>
                )}
              </div>
              {snap.resumeUrl && (
                <ResumeLink
                  resumeUrl={snap.resumeUrl}
                  className="w-full flex items-center justify-center gap-2 px-4 py-2 border border-outline-variant rounded-lg bg-surface-container-lowest hover:bg-surface-container-low text-on-surface font-label-md text-label-md transition-colors"
                >
                  <span className="material-symbols-outlined">download</span>
                  Download CV
                </ResumeLink>
              )}
            </section>
          )}

          {/* Preferences Card — matches Stitch: Locations + Desired Roles */}
          {(preferredLocations.length > 0 || (snap.desiredRoles && snap.desiredRoles.length > 0)) && (
            <section className="bg-surface-container-lowest border border-surface-variant rounded-xl p-6">
              <h3 className="font-headline-md text-[20px] leading-7 text-primary mb-4 flex items-center gap-2">
                <span className="material-symbols-outlined">tune</span>
                Preferences
              </h3>
              {preferredLocations.length > 0 && (
                <div className="mb-5">
                  <h4 className="font-label-sm text-label-sm text-secondary uppercase tracking-wider mb-2">
                    Preferred Locations
                  </h4>
                  <div className="flex flex-wrap gap-2">
                    {preferredLocations.map((loc) => (
                      <span
                        key={loc}
                        className="px-3 py-1 bg-surface-container-low rounded border border-surface-variant font-body-md text-body-md text-on-surface-variant text-sm"
                      >
                        {loc}
                      </span>
                    ))}
                  </div>
                </div>
              )}
              {snap.desiredRoles && snap.desiredRoles.length > 0 && (
                <div>
                  <h4 className="font-label-sm text-label-sm text-secondary uppercase tracking-wider mb-2">
                    Desired Roles
                  </h4>
                  <div className="flex flex-wrap gap-2">
                    {snap.desiredRoles.map((role) => (
                      <span
                        key={role}
                        className="px-3 py-1 bg-surface-container-low rounded border border-surface-variant font-body-md text-body-md text-on-surface-variant text-sm"
                      >
                        {role}
                      </span>
                    ))}
                  </div>
                </div>
              )}
            </section>
          )}

          {/* Application Info (recruiter view only) */}
          {application && (
            <section className="bg-surface-container-lowest border border-surface-variant rounded-xl p-6">
              <h3 className="font-headline-md text-[20px] leading-7 text-primary mb-4 flex items-center gap-2">
                <span className="material-symbols-outlined">info</span>
                Application Info
              </h3>
              <div className="space-y-3">
                {application.appliedAt && (
                  <div>
                    <p className="font-label-sm text-label-sm text-secondary uppercase tracking-wider">
                      Applied
                    </p>
                    <p className="font-body-md text-body-md text-on-surface">
                      {formatDate(application.appliedAt)}
                    </p>
                  </div>
                )}
                <div>
                  <p className="font-label-sm text-label-sm text-secondary uppercase tracking-wider">
                    Job
                  </p>
                  <p className="font-body-md text-body-md text-on-surface">
                    {application.jobTitle}
                  </p>
                </div>
                {application.jobCompany && (
                  <div>
                    <p className="font-label-sm text-label-sm text-secondary uppercase tracking-wider">
                      Company
                    </p>
                    <p className="font-body-md text-body-md text-on-surface">
                      {application.jobCompany}
                    </p>
                  </div>
                )}
                {application.statusUpdatedAt && (
                  <div>
                    <p className="font-label-sm text-label-sm text-secondary uppercase tracking-wider">
                      Last Updated
                    </p>
                    <p className="font-body-md text-body-md text-on-surface">
                      {formatDate(application.statusUpdatedAt)}
                    </p>
                  </div>
                )}
              </div>
            </section>
          )}
        </div>
      </div>
    </div>
  )
}
