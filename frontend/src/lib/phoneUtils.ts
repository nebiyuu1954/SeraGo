/**
 * Utilities for formatting and validating Ethiopian phone numbers.
 *
 * In Ethiopia:
 * - Country code: +251
 * - Mobile networks:
 *   - Ethio Telecom: starts with 9 (e.g. 0910101010 or 910101010)
 *   - Safaricom Ethiopia: starts with 7 (e.g. 0710101010 or 710101010)
 *
 * Users frequently enter either 10 digits with a leading zero ('0910101010'),
 * 9 digits without leading zero ('910101010'), or with spaces/dashes.
 */

/**
 * Normalizes an Ethiopian phone input to standard E.164 format (+251910101010).
 * Returns null if the number is not a valid Ethiopian mobile number.
 */
export function normalizeEthiopianPhone(input: string | null | undefined): string | null {
  if (!input) return null

  // Strip all non-digit characters
  const cleaned = input.replace(/\D/g, '')

  let local = cleaned

  // Strip country code if included
  if (local.startsWith('251')) {
    local = local.slice(3)
  }

  // Strip leading zero if included (09... or 07...)
  if (local.startsWith('0')) {
    local = local.slice(1)
  }

  // Must be 9 digits starting with 9 or 7
  if (local.length === 9 && (local.startsWith('9') || local.startsWith('7'))) {
    return `+251${local}`
  }

  return null
}

/**
 * Checks whether an input string is a valid Ethiopian mobile number.
 */
export function isValidEthiopianPhone(input: string | null | undefined): boolean {
  return normalizeEthiopianPhone(input) !== null
}

/**
 * Formats an Ethiopian phone number for human-friendly display, e.g. +251 91 123 4567.
 */
export function formatEthiopianPhoneDisplay(input: string | null | undefined): string {
  const normalized = normalizeEthiopianPhone(input)
  if (!normalized) return input ?? ''

  // normalized is +251XXXXXXXXX (13 chars: +251 + 9 digits)
  const code = normalized.slice(0, 4) // +251
  const prefix = normalized.slice(4, 6) // 91 or 71
  const mid = normalized.slice(6, 9) // 123
  const rest = normalized.slice(9) // 4567
  return `${code} ${prefix} ${mid} ${rest}`
}
