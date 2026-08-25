import type { JobResponse } from '../../types'
import {
  parseDescriptionSections,
  type DescriptionSection,
} from '../../lib/descriptionSections'

/**
 * Ethiopian Reporter Jobs-specific description renderer.
 *
 * ReporterJobs only exposes listing cards (no detail page), so the scraper
 * synthesizes a structured description from card-level fields. This component
 * renders that synthesized description plus the source-specific chips
 * (job type badge, posted text).
 */
export default function ReporterJobsDescription({
  job,
}: {
  job: JobResponse
}) {
  const sections = job.description
    ? parseDescriptionSections(job.description)
    : []

  return (
    <div className="space-y-8">
      {/* Description sections (synthesized from card data) */}
      {sections.map((section, i) => (
        <DescriptionSection
          key={`${section.heading ?? 'intro'}-${i}`}
          section={section}
        />
      ))}

      {/* Job type badge */}
      {job.jobTypeText && (
        <section>
          <h3 className="flex items-center gap-2 font-headline-md text-headline-md font-semibold text-on-surface">
            <span className="material-symbols-outlined text-xl text-primary">
              schedule
            </span>
            Employment type
          </h3>
          <div className="mt-3">
            <span className="inline-flex items-center rounded-full bg-primary-container/60 px-4 py-1.5 font-label-md text-label-md font-medium text-primary">
              {job.jobTypeText}
            </span>
          </div>
        </section>
      )}

      {/* Posted text */}
      {job.postedText && (
        <section>
          <p className="font-body-md text-body-md text-on-surface-variant">
            <span className="material-symbols-outlined mr-1 text-base">
              schedule
            </span>
            {job.postedText}
          </p>
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
