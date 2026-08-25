import type { JobResponse } from '../../types'
import {
  parseDescriptionSections,
  type DescriptionSection,
} from '../../lib/descriptionSections'

/**
 * Generic description renderer — used for SeraGo-posted jobs and unknown sources.
 * Parses the description text into sections and renders them with the standard
 * section heading + icon + content layout.
 */
export default function GenericDescription({ job }: { job: JobResponse }) {
  const sections = job.description
    ? parseDescriptionSections(job.description)
    : []

  if (sections.length === 0) {
    return (
      <div className="flex flex-col items-center justify-center rounded-xl border border-dashed border-outline-variant px-6 py-12 text-center">
        <span className="material-symbols-outlined text-4xl text-on-surface-variant">
          description
        </span>
        <p className="mt-3 font-body-md text-body-md text-on-surface-variant">
          No description available
        </p>
      </div>
    )
  }

  return (
    <div className="space-y-8">
      {sections.map((section, i) => (
        <DescriptionSection
          key={`${section.heading ?? 'intro'}-${i}`}
          section={section}
        />
      ))}
    </div>
  )
}

/** One parsed description section: icon + heading + content. */
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

/** Renders text with bullet lists, numbered lists, and paragraph breaks. */
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
