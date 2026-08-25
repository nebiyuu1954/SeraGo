import type { JobResponse } from '../../types'
import {
  parseDescriptionSections,
  type DescriptionSection,
} from '../../lib/descriptionSections'

/**
 * HaHuJobs-specific description renderer.
 *
 * HaHuJobs is an aggregator — it provides a short summary paragraph, entity
 * logo, sector/sub-sector, salary, experience years, application method,
 * and the upstream source name (e.g. "hahujobs_telegram").
 */
export default function HaHuJobsDescription({
  job,
}: {
  job: JobResponse
}) {
  const sections = job.description
    ? parseDescriptionSections(job.description)
    : []

  return (
    <div className="space-y-8">
      {/* Description sections */}
      {sections.map((section, i) => (
        <DescriptionSection
          key={`${section.heading ?? 'intro'}-${i}`}
          section={section}
        />
      ))}

      {/* Sector + Sub-sector */}
      {(job.sectorName || job.subSectorName) && (
        <section>
          <h3 className="flex items-center gap-2 font-headline-md text-headline-md font-semibold text-on-surface">
            <span className="material-symbols-outlined text-xl text-primary">
              domain
            </span>
            Sector
          </h3>
          <div className="mt-3">
            {job.sectorName && (
              <p className="font-body-md text-body-md font-medium text-on-surface">
                {job.sectorName}
              </p>
            )}
            {job.subSectorName && (
              <p className="mt-1 font-body-md text-body-md text-on-surface-variant">
                {job.subSectorName}
              </p>
            )}
          </div>
        </section>
      )}

      {/* Area */}
      {job.areaName && (
        <section>
          <h3 className="flex items-center gap-2 font-headline-md text-headline-md font-semibold text-on-surface">
            <span className="material-symbols-outlined text-xl text-primary">
              location_on
            </span>
            Area
          </h3>
          <p className="mt-3 font-body-md text-body-md text-on-surface">
            {job.areaName}
          </p>
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

      {/* Applicants count */}
      {job.numberOfApplicants != null && job.numberOfApplicants > 0 && (
        <section>
          <p className="font-body-md text-body-md text-on-surface-variant">
            <span className="material-symbols-outlined mr-1 text-base">
              group
            </span>
            {job.numberOfApplicants} applicant
            {job.numberOfApplicants !== 1 ? 's' : ''}
          </p>
        </section>
      )}

      {/* Upstream source attribution */}
      {job.upstreamSource && (
        <section className="rounded-xl bg-surface-container-low p-4">
          <p className="font-body-sm text-body-sm text-on-surface-variant">
            <span className="material-symbols-outlined mr-1 text-base">
              info
            </span>
            Originally posted via{' '}
            <span className="font-medium text-on-surface">
              {formatUpstreamSource(job.upstreamSource)}
            </span>
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

function formatApplicationMethod(method: string): string {
  const map: Record<string, string> = {
    link: 'Apply online via the link below',
    email: 'Send your application by email',
    in_person: 'Apply in person',
    hahujobs_primary: 'Apply through HaHuJobs',
  }
  return map[method] || method
}

function formatUpstreamSource(source: string): string {
  return source
    .replace(/_/g, ' ')
    .replace(/\b\w/g, (c) => c.toUpperCase())
}
