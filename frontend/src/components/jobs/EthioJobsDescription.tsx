import type { JobResponse } from '../../types'
import {
  parseDescriptionSections,
  type DescriptionSection,
} from '../../lib/descriptionSections'

/**
 * EthioJobs-specific description renderer.
 *
 * EthioJobs provides rich HTML descriptions, structured category/catalog data,
 * company logos, and application method details (ATS / Email / Career Page).
 */
export default function EthioJobsDescription({
  job,
}: {
  job: JobResponse
}) {
  const sections = job.description
    ? parseDescriptionSections(job.description)
    : []
  const categories = parseCategories(job.sourceCategories)

  return (
    <div className="space-y-8">
      {/* Description sections */}
      {sections.map((section, i) => (
        <DescriptionSection
          key={`${section.heading ?? 'intro'}-${i}`}
          section={section}
        />
      ))}

      {/* Categories / Catalogs */}
      {categories.length > 0 && (
        <section>
          <h3 className="flex items-center gap-2 font-headline-md text-headline-md font-semibold text-on-surface">
            <span className="material-symbols-outlined text-xl text-primary">
              category
            </span>
            Categories
          </h3>
          <div className="mt-3 flex flex-wrap gap-2">
            {categories.map((cat) => (
              <span
                key={cat}
                className="inline-flex items-center rounded-full bg-tertiary-container/60 px-3 py-1 font-label-sm text-label-sm font-medium text-on-tertiary-container"
              >
                {cat}
              </span>
            ))}
          </div>
        </section>
      )}

      {/* Application method */}
      {job.applicationMethod && (
        <section>
          <h3 className="flex items-center gap-2 font-headline-md text-headline-md font-semibold text-on-surface">
            <span className="material-symbols-outlined text-xl text-primary">
              how_to_reg
            </span>
            How to apply
          </h3>
          <div className="mt-3 space-y-2">
            <p className="font-body-md text-body-md text-on-surface">
              {formatApplicationMethod(job.applicationMethod)}
            </p>
            {job.applicationEmail && (
              <p className="font-body-md text-body-md text-on-surface">
                Email:{' '}
                <a
                  href={`mailto:${job.applicationEmail}`}
                  className="text-primary underline"
                >
                  {job.applicationEmail}
                </a>
              </p>
            )}
            {job.applicationUrl && (
              <p className="font-body-md text-body-md text-on-surface">
                Apply online:{' '}
                <a
                  href={job.applicationUrl}
                  target="_blank"
                  rel="noreferrer"
                  className="text-primary underline"
                >
                  {job.applicationUrl}
                </a>
              </p>
            )}
          </div>
        </section>
      )}
    </div>
  )
}

function DescriptionSection({
  section,
}: {
  section: DescriptionSection
}) {
  return (
    <section>
      {section.heading && (
        <h3 className="flex items-center gap-2 font-headline-md text-headline-md font-semibold text-on-surface">
          <span className="material-symbols-outlined text-xl text-primary">
            {section.icon}
          </span>
          {section.heading}
        </h3>
      )}
      <div className={section.heading ? 'mt-3' : undefined}>
        <SectionContent text={section.content} />
      </div>
    </section>
  )
}

function SectionContent({ text }: { text: string }) {
  const paragraphs = text
    .split(/\n{2,}/)
    .map((p) => p.trim())
    .filter(Boolean)
  if (paragraphs.length === 0) return null
  return (
    <div className="space-y-4">
      {paragraphs.map((paragraph, i) => {
        const lines = paragraph
          .split('\n')
          .map((l) => l.trim())
          .filter(Boolean)
        const isBulletList =
          lines.length > 1 && lines.every((l) => /^[-•*·]\s+/.test(l))
        if (isBulletList) {
          return (
            <ul key={i} className="space-y-2">
              {lines.map((line, j) => (
                <li key={j} className="flex gap-2.5">
                  <span
                    aria-hidden="true"
                    className="mt-[0.55em] h-1.5 w-1.5 shrink-0 rounded-full bg-accent"
                  />
                  <span className="font-body-md text-body-md leading-relaxed text-on-surface">
                    {line.replace(/^[-•*·]\s+/, '')}
                  </span>
                </li>
              ))}
            </ul>
          )
        }
        return (
          <p
            key={i}
            className="whitespace-pre-line font-body-md text-body-md leading-relaxed text-on-surface"
          >
            {lines.join('\n')}
          </p>
        )
      })}
    </div>
  )
}

function parseCategories(json: string | null): string[] {
  if (!json) return []
  try {
    const parsed = JSON.parse(json)
    if (Array.isArray(parsed)) {
      return parsed
        .map((c) => (typeof c === 'object' && c?.name ? c.name : null))
        .filter(Boolean)
    }
  } catch {
    // not JSON
  }
  return []
}

function formatApplicationMethod(method: string): string {
  const map: Record<string, string> = {
    ATS: 'Apply through the company\'s applicant tracking system',
    EMAIL: 'Send your application by email',
    CAREER_PAGE_LINK: 'Apply on the company\'s career page',
    IN_PERSON: 'Apply in person',
  }
  return map[method] || method
}
