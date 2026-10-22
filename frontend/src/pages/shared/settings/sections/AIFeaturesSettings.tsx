import type { AISettings as AISettingsType } from '../../../../types/settings.ts'
import type { RequiredRole } from '../../../../hooks'

const toggleClass = (enabled: boolean) =>
  `relative inline-flex h-6 w-11 shrink-0 cursor-pointer rounded-full transition-colors ${enabled ? 'bg-primary' : 'bg-surface-variant'}`

interface AIFeaturesSettingsProps {
  role: RequiredRole
  value: AISettingsType
  onChange: (next: AISettingsType) => void
}

function AIToggleRow({
  icon,
  label,
  description,
  enabled,
  onToggle,
}: {
  icon: string
  label: string
  description: string
  enabled: boolean
  onToggle: () => void
}) {
  return (
    <div className="flex items-center justify-between py-3.5 first:pt-0 last:pb-0 gap-4">
      <div className="flex items-center gap-3.5 min-w-0">
        <div className="w-9 h-9 rounded-lg bg-primary/10 flex items-center justify-center text-primary shrink-0">
          <span className="material-symbols-outlined text-xl">{icon}</span>
        </div>
        <div className="min-w-0">
          <div className="flex items-center gap-2">
            <h3 className="font-label-md text-label-md font-semibold text-on-surface">{label}</h3>
            <span className="inline-flex items-center rounded-full bg-surface-container-low px-2 py-0.5 text-label-xs font-medium text-on-surface-variant">
              Beta
            </span>
          </div>
          <p className="font-label-sm text-label-sm text-on-surface-variant/70 mt-0.5">
            {description}
          </p>
        </div>
      </div>

      <button
        type="button"
        role="switch"
        aria-checked={enabled}
        aria-label={`Toggle ${label}`}
        onClick={onToggle}
        className={toggleClass(enabled)}
      >
        <span
          className={`inline-block h-5 w-5 transform rounded-full bg-white shadow-sm transition-transform mt-0.5 ${
            enabled ? 'translate-x-5' : 'translate-x-0.5'
          }`}
        />
      </button>
    </div>
  )
}

export default function AIFeaturesSettings({ role, value, onChange }: AIFeaturesSettingsProps) {
  const isTalent = role === 'Talent'

  return (
    <div id="ai-features" className="p-6 sm:p-8 lg:p-10 grid grid-cols-1 lg:grid-cols-12 gap-6 lg:gap-10">
      {/* Left Info Column */}
      <div className="lg:col-span-4 flex flex-col justify-start">
        <h2 className="font-headline-md text-headline-md font-semibold text-on-surface">
          AI Features
        </h2>
        <p className="font-body-md text-body-md text-on-surface-variant mt-2 leading-relaxed">
          {isTalent
            ? 'Configure AI-assisted job matching, cover letter drafting, and automated resume skill parsing.'
            : 'Configure AI recommendations and platform intelligence settings.'}
        </p>
      </div>

      {/* Right Controls Column */}
      <div className="lg:col-span-8 flex flex-col divide-y divide-surface-variant">
        <AIToggleRow
          icon="psychology"
          label="AI Job Matching"
          description="Personalized job recommendations scored against your profile and work preferences"
          enabled={value.enableAiMatching}
          onToggle={() => onChange({ ...value, enableAiMatching: !value.enableAiMatching })}
        />

        <AIToggleRow
          icon="auto_fix_high"
          label="AI Tailored Cover Letters"
          description="Synthesize personalized cover letter drafts tailored to job descriptions"
          enabled={value.enableCoverLetter}
          onToggle={() => onChange({ ...value, enableCoverLetter: !value.enableCoverLetter })}
        />

        <AIToggleRow
          icon="document_scanner"
          label="AI Resume Skill Extractor"
          description="Extract structured skills and job experience directly from your uploaded CV"
          enabled={value.enableResumeParsing}
          onToggle={() => onChange({ ...value, enableResumeParsing: !value.enableResumeParsing })}
        />
      </div>
    </div>
  )
}
