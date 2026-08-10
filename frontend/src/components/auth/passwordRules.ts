/** Mirrors ASP.NET Core Identity's default password policy. */
export const PASSWORD_RULES = [
  { label: 'At least 6 characters', test: (p: string) => p.length >= 6 },
  { label: 'One uppercase letter', test: (p: string) => /[A-Z]/.test(p) },
  { label: 'One lowercase letter', test: (p: string) => /[a-z]/.test(p) },
  {
    label: 'One number or symbol',
    test: (p: string) => /[0-9]/.test(p) || /[^A-Za-z0-9]/.test(p),
  },
] as const

/** Number of rules met (0-4) — drives the strength bar. */
export const passwordStrength = (password: string): number =>
  PASSWORD_RULES.reduce((n, rule) => n + (rule.test(password) ? 1 : 0), 0)

/** True when the password satisfies every rule (Yup schema test). */
export const meetsPasswordRules = (password: string): boolean =>
  PASSWORD_RULES.every((rule) => rule.test(password))
