import { useNavigate } from 'react-router-dom'
import RichTextDisplay from '../ui/RichTextDisplay'

// ────────────────────── helpers ──────────────────────

export interface RecruiterProfileData {
  firstName?: string
  lastName?: string
  avatarUrl?: string
  city?: string
  country?: string
  companyName?: string
  industry?: string
  companySize?: string
  websiteUrl?: string
  about?: string
  // New attributes
  foundedYear?: number | null
  headquarters?: string
  phoneNumber?: string
  email?: string
  companyType?: string | null
  linkedInUrl?: string
  twitterUrl?: string
  // Privacy
  companyVisibility?: string  // JSON object
  isCompanyPrivate?: boolean
}

/** Parse the companyVisibility JSON, defaulting every key to true. */
function parseCompanyVisibility(raw?: string): Record<string, boolean> {
  const defaults: Record<string, boolean> = {
    avatar: true, companyName: true, industry: true, companySize: true,
    websiteUrl: true, about: true, foundedYear: true,
    headquarters: true, phoneNumber: true, email: true,
    companyType: true, linkedInUrl: true, twitterUrl: true,
  }
  if (!raw || raw === '{}') return defaults
  try {
    return { ...defaults, ...JSON.parse(raw) }
  } catch {
    return defaults
  }
}

function locationString(data: RecruiterProfileData): string | null {
  const parts = [data.city, data.country].filter(Boolean)
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

export interface RecruiterProfileViewProps {
  /** The recruiter's own profile data — drives all visible data. */
  data: RecruiterProfileData
  /** Header badge text — defaults to "Actively hiring". */
  headerBadge?: string
  /** Back button label — defaults to "Back to profile". */
  backLabel?: string
  /** Back button link target. */
  backTo?: string
  /** Optional content to render in the top-right area (e.g. info banner). */
  topRight?: React.ReactNode
}

// ────────────────────── main component ──────────────────────

export default function RecruiterProfileView({
  data,
  headerBadge = 'Actively hiring',
  backLabel = 'Back to profile',
  backTo,
  topRight,
}: RecruiterProfileViewProps) {
  const navigate = useNavigate()

  const isPrivate = data.isCompanyPrivate ?? false
  const vis = parseCompanyVisibility(data.companyVisibility)

  // When private, mask everything
  const companyName = isPrivate ? 'Confidential Company' : (data.companyName ?? '')
  const showIndustry = !isPrivate && vis.industry && data.industry
  const showSize = !isPrivate && vis.companySize && data.companySize
  const showWebsite = !isPrivate && vis.websiteUrl && data.websiteUrl
  const showAbout = !isPrivate && vis.about && data.about
  const showFoundedYear = !isPrivate && vis.foundedYear && data.foundedYear
  const showHeadquarters = !isPrivate && vis.headquarters && data.headquarters
  const showPhone = !isPrivate && vis.phoneNumber && data.phoneNumber
  const showEmail = !isPrivate && vis.email && data.email
  const showCompanyType = !isPrivate && vis.companyType && data.companyType
  const showLinkedIn = !isPrivate && vis.linkedInUrl && data.linkedInUrl
  const showTwitter = !isPrivate && vis.twitterUrl && data.twitterUrl

  const fullName = [data.firstName, data.lastName].filter(Boolean).join(' ')
  const location = locationString(data)

  const COMPANY_TYPE_LABELS: Record<string, string> = {
    Public: 'Public',
    Private: 'Private',
    NonProfit: 'Non-profit / NGO',
    Government: 'Government',
    Startup: 'Startup',
    SoleProprietorship: 'Sole Proprietorship',
    Partnership: 'Partnership',
  }

  return (
    <div className="w-full max-w-7xl mx-auto flex flex-col gap-8">
      {/* Top bar: back button (left) + optional top-right content */}
      <div className="flex items-center justify-between">
        <button
          type="button"
          onClick={() => (backTo ? navigate(backTo) : navigate(-1))}
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
          {!isPrivate && vis.avatar && data.avatarUrl ? (
            <img
              className="w-20 h-20 rounded-full object-cover border border-surface-variant"
              src={data.avatarUrl}
              alt={fullName || 'Recruiter'}
            />
          ) : (
            <span className="flex w-20 h-20 items-center justify-center rounded-full bg-primary-container/60 font-headline-lg text-headline-lg font-semibold text-primary">
              {isPrivate ? '🔒' : (initialsOf(fullName) || '?')}
            </span>
          )}
          <div>
            <h1 className="font-headline-lg text-headline-lg text-primary mb-1">
              {isPrivate ? 'Confidential Recruiter' : (fullName || 'Recruiter')}
            </h1>
            {vis.companyName && (
              <p className="font-body-lg text-body-lg text-on-surface-variant mb-2">
                {isPrivate ? 'Hiring for a private company' : `Hiring for ${companyName}`}
              </p>
            )}
            <div className="flex items-center gap-4">
              {!isPrivate && location && (
                <span className="flex items-center gap-1 font-label-sm text-label-sm text-secondary">
                  <span className="material-symbols-outlined text-[16px]">
                    location_on
                  </span>
                  {location}
                </span>
              )}
              <span className="inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full bg-[#E8F5E9] text-[#2E7D32] font-label-sm text-label-sm border border-[#C8E6C9]">
                <span className="w-2 h-2 rounded-full bg-[#4CAF50]" />
                {headerBadge}
              </span>
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
          {(showAbout || (vis.companyName && companyName)) && (
            <section className="bg-surface-container-lowest border border-surface-variant rounded-xl p-6">
              <h2 className="font-headline-md text-headline-md text-primary mb-6 flex items-center gap-2">
                <span className="material-symbols-outlined">business</span>
                About{vis.companyName && companyName ? ` ${companyName}` : ''}
              </h2>
              {showAbout ? (
                <RichTextDisplay html={data.about!} className="text-on-surface-variant" />
              ) : (
                <p className="font-body-md text-body-md text-on-surface-variant/60 italic">
                  No description added yet.
                </p>
              )}
              {(showIndustry || showCompanyType) && (
                <div className="mt-6 flex flex-wrap gap-3">
                  {showIndustry && (
                    <div className="flex items-center gap-2 bg-surface-container-low px-3 py-1.5 rounded-md border border-surface-variant">
                      <span className="material-symbols-outlined text-[16px] text-secondary">
                        domain
                      </span>
                      <span className="font-label-sm text-label-sm text-on-surface">
                        {data.industry}
                      </span>
                    </div>
                  )}
                  {showCompanyType && (
                    <div className="flex items-center gap-2 bg-surface-container-low px-3 py-1.5 rounded-md border border-surface-variant">
                      <span className="material-symbols-outlined text-[16px] text-secondary">
                        category
                      </span>
                      <span className="font-label-sm text-label-sm text-on-surface">
                        {COMPANY_TYPE_LABELS[data.companyType!] ?? data.companyType}
                      </span>
                    </div>
                  )}
                </div>
              )}
            </section>
          )}
        </div>

        {/* ──────── Right Column: Sidebar Details (30%) ──────── */}
        <div className="w-full lg:w-[30%] flex flex-col gap-6">
          {/* Company Card */}
          {(vis.companyName && companyName) && (
            <section className="bg-surface-container-lowest border border-surface-variant rounded-xl p-6">
              <h3 className="font-headline-md text-[20px] leading-7 text-primary mb-4 flex items-center gap-2">
                <span className="material-symbols-outlined">apartment</span>
                Company
              </h3>
              <div className="flex items-center gap-4">
                <div className="flex items-center justify-center h-12 w-12 rounded-lg bg-primary-container/20 text-primary shrink-0">
                <span className="material-symbols-outlined">apartment</span>
              </div>
                <div className="min-w-0">
                  <p className="font-label-md text-label-md font-medium text-on-surface truncate">
                    {companyName || 'Company not set'}
                  </p>
                  {showSize && (
                    <p className="font-label-sm text-label-sm text-on-surface-variant">
                      {data.companySize}
                    </p>
                  )}
                  {showFoundedYear && (
                    <p className="font-label-sm text-label-sm text-on-surface-variant">
                      Founded {data.foundedYear}
                    </p>
                  )}
                  {showHeadquarters && (
                    <p className="font-label-sm text-label-sm text-on-surface-variant">
                      {data.headquarters}
                    </p>
                  )}
                </div>
              </div>
            </section>
          )}

          {/* Contact Card */}
          {(showPhone || showEmail) && (
            <section className="bg-surface-container-lowest border border-surface-variant rounded-xl p-6">
              <h3 className="font-headline-md text-[20px] leading-7 text-primary mb-4 flex items-center gap-2">
                <span className="material-symbols-outlined">contact_mail</span>
                Contact
              </h3>
              <div className="flex flex-col gap-3">
                {showPhone && (
                  <div className="flex items-center gap-3">
                    <span className="material-symbols-outlined text-[18px] text-secondary">phone</span>
                    <span className="font-body-md text-body-md text-on-surface">{data.phoneNumber}</span>
                  </div>
                )}
                {showEmail && (
                  <div className="flex items-center gap-3">
                    <span className="material-symbols-outlined text-[18px] text-secondary">email</span>
                    <a href={`mailto:${data.email}`} className="font-body-md text-body-md text-on-surface hover:text-primary transition-colors">
                      {data.email}
                    </a>
                  </div>
                )}
              </div>
            </section>
          )}

          {/* Links Card */}
          {(showWebsite || showLinkedIn || showTwitter) && (
            <section className="bg-surface-container-lowest border border-surface-variant rounded-xl p-6">
              <h3 className="font-headline-md text-[20px] leading-7 text-primary mb-4 flex items-center gap-2">
                <span className="material-symbols-outlined">link</span>
                Links &amp; Socials
              </h3>
              <div className="flex flex-col gap-4">
                {showWebsite && (
                  <a
                    href={data.websiteUrl}
                    target="_blank"
                    rel="noreferrer"
                    className="flex items-center gap-3 text-on-surface hover:text-primary transition-colors group"
                  >
                    <span className="material-symbols-outlined text-secondary group-hover:text-primary">
                      language
                    </span>
                    <span className="font-body-md text-body-md underline-offset-4 group-hover:underline break-all">
                      Company Website
                    </span>
                  </a>
                )}
                {showLinkedIn && (
                  <a
                    href={data.linkedInUrl}
                    target="_blank"
                    rel="noreferrer"
                    className="flex items-center gap-3 text-on-surface hover:text-primary transition-colors group"
                  >
                    <span className="material-symbols-outlined text-secondary group-hover:text-primary">
                      work
                    </span>
                    <span className="font-body-md text-body-md underline-offset-4 group-hover:underline break-all">
                      LinkedIn
                    </span>
                  </a>
                )}
                {showTwitter && (
                  <a
                    href={data.twitterUrl}
                    target="_blank"
                    rel="noreferrer"
                    className="flex items-center gap-3 text-on-surface hover:text-primary transition-colors group"
                  >
                    <span className="material-symbols-outlined text-secondary group-hover:text-primary">
                      tag
                    </span>
                    <span className="font-body-md text-body-md underline-offset-4 group-hover:underline break-all">
                      Twitter / X
                    </span>
                  </a>
                )}
              </div>
            </section>
          )}

          {/* Location Card */}
          {location && (
            <section className="bg-surface-container-lowest border border-surface-variant rounded-xl p-6">
              <h3 className="font-headline-md text-[20px] leading-7 text-primary mb-4 flex items-center gap-2">
                <span className="material-symbols-outlined">location_on</span>
                Location
              </h3>
              <p className="font-body-md text-body-md text-on-surface">{location}</p>
            </section>
          )}
        </div>
      </div>
    </div>
  )
}
