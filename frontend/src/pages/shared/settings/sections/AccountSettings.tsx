import AccordionSection from '../../../../components/ui/AccordionSection.tsx'
import { LANGUAGE_OPTIONS } from '../../../../types/settings.ts'
import type { AccountSettings as AccountSettingsType } from '../../../../types/settings.ts'

const inputClass =
  'w-full rounded-lg border border-outline-variant bg-surface-container-lowest px-3.5 py-2.5 font-body-md text-body-md text-on-surface transition-colors placeholder:text-on-surface-variant/60 focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary'

interface AccountSettingsProps {
  value: AccountSettingsType
  onChange: (next: AccountSettingsType) => void
}

export default function AccountSettings({ value, onChange }: AccountSettingsProps) {
  return (
    <AccordionSection
      title="Account"
      description="Language and regional preferences"
      icon="settings"
      defaultOpen
      completion={{ filled: 1, total: 1 }}
    >
      {/* Language — placeholder for future i18n integration */}
      <div>
        <label
          htmlFor="settings-language"
          className="font-label-sm text-label-sm font-medium text-on-surface"
        >
          Language
        </label>
        <select
          id="settings-language"
          value={value.language}
          onChange={(e) => onChange({ ...value, language: e.target.value })}
          className={`mt-1.5 ${inputClass}`}
        >
          {LANGUAGE_OPTIONS.map((opt) => (
            <option key={opt.value} value={opt.value}>
              {opt.label}
            </option>
          ))}
        </select>
        <p className="mt-1.5 font-label-sm text-label-sm text-on-surface-variant/70">
          Interface language. More languages coming soon.
        </p>
      </div>
    </AccordionSection>
  )
}
