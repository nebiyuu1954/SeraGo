import AccordionSection from '../../../../components/ui/AccordionSection.tsx'
import type { AISettings as AISettingsType } from '../../../../types/settings.ts'
import type { RequiredRole } from '../../../../hooks'

const toggleClass = (enabled: boolean) =>
  `relative inline-flex h-5 w-9 shrink-0 cursor-pointer rounded-full transition-colors ${enabled ? 'bg-primary' : 'bg-surface-variant'}`

interface AIFeaturesSettingsProps {
  role: RequiredRole
  value: AISettingsType
  onChange: (next: AISettingsType) => void
}

function Toggle({
  label,
  description,
  enabled,
  onToggle,
}: {
  label: string
  description: string
  enabled: boolean
  onToggle: () => void
}) {
  return (
    <div className="flex items-center justify-between gap-4 py-3">
      <div className="min-w-0">
        <div className="flex items-center gap-2">
          <p className="font-label-md text-label-md text-on-surface">{label}</p>
          <span className="inline-flex items-center gap-1 rounded-full bg-surface-container-low px-2 py-0.5 text-label-xs font-medium text-on-surface-variant">
            Coming soon
          </span>
        </div>
        <p className="font-label-sm text-label-sm text-on-surface-variant/70">
          {description}
        </p>
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
          className={`inline-block h-4 w-4 transform rounded-full bg-white shadow-sm transition-transform mt-0.5 ${enabled ? 'translate-x-4' : 'translate-x-0.5'}`}
        />
      </button>
    </div>
  )
}

function Divider() {
  return <div className="border-t border-surface-variant" />
}

export default function AIFeaturesSettings({ role, value, onChange }: AIFeaturesSettingsProps) {
  const isTalent = role === 'Talent'

  const enabledCount = Object.values(value).filter(Boolean).length
  const totalCount = isTalent ? 3 : 0

  return (
    <AccordionSection
      title="AI Features"
      description="AI-powered tools to help you find or fill roles"
      icon="auto_awesome"
      completion={{ filled: enabledCount, total: totalCount }}
    >
      {isTalent && (
        <>
          <Toggle
            label="AI job matching"
            description="Get personalized job recommendations based on your profile and skills"
            enabled={value.enableAiMatching}
            onToggle={() => onChange({ ...value, enableAiMatching: !value.enableAiMatching })}
          />
          <Divider />
          <Toggle
            label="AI cover letter"
            description="Generate tailored cover letters when applying to jobs"
            enabled={value.enableCoverLetter}
            onToggle={() => onChange({ ...value, enableCoverLetter: !value.enableCoverLetter })}
          />
          <Divider />
          <Toggle
            label="AI resume parsing"
            description="Automatically extract skills and experience from your resume"
            enabled={value.enableResumeParsing}
            onToggle={() => onChange({ ...value, enableResumeParsing: !value.enableResumeParsing })}
          />
        </>
      )}
    </AccordionSection>
  )
}
