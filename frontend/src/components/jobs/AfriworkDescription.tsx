import type { JobResponse } from '../../types'
import { parseSkills } from '../../lib/sourceCapabilities'
import {
  parseDescriptionSections,
  type DescriptionSection,
} from '../../lib/descriptionSections'

/**
 * Afriwork-specific description renderer.
 *
 * Afriwork provides rich HTML descriptions (stripped to structured plain text),
 * structured skill tags, sector tags, compensation data, and work mode.
 * This component renders the description sections plus the source-specific
 * extra data that other sources don't have.
 */
export default function AfriworkDescription({
  job,
}: {
  job: JobResponse
}) {
  const sections = job.description
    ? parseDescriptionSections(job.description)
    : []
  const skills = parseSkills(job.skills)
  const sectors = parseJsonArray(job.sourceSectors)
  const compensation = formatCompensation(
    job.compensationAmountCents,
    job.compensationType,
    job.compensationCurrency,
  )

  return (
    <div className="space-y-8">
      {/* Description sections */}
      {sections.map((section, i) => (
        <DescriptionSection
          key={`${section.heading ?? 'intro'}-${i}`}
          section={section}
        />
      ))}

      {/* Skills chips */}
      {skills.length > 0 && (
        <section>
          <h3 className="flex items-center gap-2 font-headline-md text-headline-md font-semibold text-on-surface">
            <span className="material-symbols-outlined text-xl text-primary">
              psychology
            </span>
            Skills
          </h3>
          <div className="mt-3 flex flex-wrap gap-2">
            {skills.map((skill) => (
              <span
                key={skill}
                className="inline-flex items-center rounded-full bg-primary-container/60 px-3 py-1 font-label-sm text-label-sm font-medium text-primary"
              >
                {skill}
              </span>
            ))}
          </div>
        </section>
      )}

      {/* Sectors */}
      {sectors.length > 0 && (
        <section>
          <h3 className="flex items-center gap-2 font-headline-md text-headline-md font-semibold text-on-surface">
            <span className="material-symbols-outlined text-xl text-primary">
              domain
            </span>
            Sectors
          </h3>
          <div className="mt-3 flex flex-wrap gap-2">
            {sectors.map((sector) => (
              <span
                key={sector}
                className="inline-flex items-center rounded-full bg-tertiary-container/60 px-3 py-1 font-label-sm text-label-sm font-medium text-on-tertiary-container"
              >
                {sector}
              </span>
            ))}
          </div>
        </section>
      )}

      {/* Compensation card */}
      {compensation && (
        <section>
          <h3 className="flex items-center gap-2 font-headline-md text-headline-md font-semibold text-on-surface">
            <span className="material-symbols-outlined text-xl text-primary">
              payments
            </span>
            Compensation
          </h3>
          <p className="mt-3 font-body-lg text-body-lg font-medium text-on-surface">
            {compensation}
          </p>
          {job.compensationType && (
            <p className="mt-1 font-body-md text-body-md text-on-surface-variant">
              {job.compensationType.charAt(0) +
                job.compensationType.slice(1).toLowerCase()}
            </p>
          )}
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

function parseJsonArray(json: string | null): string[] {
  if (!json) return []
  try {
    const parsed = JSON.parse(json)
    if (Array.isArray(parsed)) return parsed.filter((s) => typeof s === 'string')
  } catch {
    // not JSON
  }
  return []
}

function formatCompensation(
  cents: number | null,
  type: string | null,
  currency: string | null,
): string | null {
  if (!cents || !currency) return null
  const amount = cents / 100
  const freq = type
    ? type.charAt(0) + type.slice(1).toLowerCase()
    : ''
  return `${amount.toLocaleString()} ${currency}${freq ? ' ' + freq : ''}`
}
