import { useState } from 'react'
import TextInput from './TextInput.tsx'
import type { TextInputProps } from './TextInput.tsx'
import { cn } from '../../lib/cn.ts'
import { PASSWORD_RULES, passwordStrength } from './passwordRules.ts'

export interface PasswordInputProps extends Omit<
  TextInputProps,
  'type' | 'icon' | 'suffix' | 'hint'
> {
  autoComplete: 'current-password' | 'new-password'
  /** Renders the strength bar + requirement checklist under the field. */
  showStrength?: boolean
}

export default function PasswordInput({
  autoComplete,
  showStrength = false,
  onFocus,
  ...rest
}: PasswordInputProps) {
  const [visible, setVisible] = useState(false)
  // The checklist appears once the user focuses the field (and starts typing),
  // so it doesn't shout requirements at someone just scanning the page.
  const [touched, setTouched] = useState(false)

  const strength = passwordStrength(rest.value)

  return (
    <TextInput
      {...rest}
      icon="lock"
      type={visible ? 'text' : 'password'}
      autoComplete={autoComplete}
      onFocus={() => {
        if (showStrength) setTouched(true)
        onFocus?.()
      }}
      className={cn(rest.className, 'pr-11')}
      suffix={
        <button
          type="button"
          onClick={() => setVisible((v) => !v)}
          aria-label={visible ? 'Hide password' : 'Show password'}
          aria-pressed={visible}
          className="absolute inset-y-0 right-0 flex w-11 items-center justify-center text-on-surface-variant transition-colors hover:text-primary"
        >
          <span className="material-symbols-outlined text-xl">
            {visible ? 'visibility_off' : 'visibility'}
          </span>
        </button>
      }
      hint={
        showStrength ? (
          <div className="mt-2">
            {/* Strength bar */}
            <div aria-hidden="true" className="flex gap-1">
              {PASSWORD_RULES.map((rule, index) => (
                <span
                  key={rule.label}
                  className={cn(
                    'h-1 flex-1 rounded-full transition-colors duration-300',
                    index < strength
                      ? strength <= 2
                        ? 'bg-error'
                        : strength === 3
                          ? 'bg-tertiary'
                          : 'bg-success'
                      : 'bg-surface-container-high',
                  )}
                />
              ))}
            </div>
            {touched && rest.value.length > 0 && (
              <div className="mt-2.5">
                <PasswordChecklist password={rest.value} />
              </div>
            )}
          </div>
        ) : undefined
      }
    />
  )
}

function PasswordChecklist({ password }: { password: string }) {
  return (
    <ul
      aria-label="Password requirements"
      className="grid grid-cols-1 gap-1 rounded-lg border border-outline-variant bg-surface-container-low p-3 sm:grid-cols-2"
    >
      {PASSWORD_RULES.map((rule) => {
        const met = rule.test(password)
        return (
          <li
            key={rule.label}
            className={cn(
              'flex items-center gap-1.5 font-label-sm text-label-sm transition-colors duration-200',
              met ? 'text-success' : 'text-on-surface-variant',
            )}
          >
            <span
              aria-hidden="true"
              className={cn(
                'material-symbols-outlined text-base',
                met ? 'font-semibold' : '',
              )}
            >
              {met ? 'check_circle' : 'radio_button_unchecked'}
            </span>
            {rule.label}
          </li>
        )
      })}
    </ul>
  )
}
