import type { ApplicationStatus } from '../types'

interface StatusBadgeResult {
  label: string
  /** Badge className for list/grid/detail views. */
  className: string
  /** Filter chip — active (selected) state. */
  chipActive: string
  /** Filter chip — inactive (unselected) state. */
  chipInactive: string
  /** Light banner background for grid cards. */
  bannerBg: string
}

/**
 * Universal status badge colors — used on both talent and recruiter sides.
 *
 * Palette:
 *   Pending    → Gray
 *   Reviewed   → Sky Blue
 *   Shortlisted → Orange
 *   Interview  → Amber
 *   Hired      → Emerald
 *   Rejected   → Rose
 */
export function statusBadge(status: ApplicationStatus): StatusBadgeResult {
  switch (status) {
    case 'pending':
      return {
        label: 'Pending',
        className: 'bg-gray-500 text-white',
        chipActive: 'bg-gray-600 border border-gray-600 text-white',
        chipInactive: 'bg-gray-500 border border-gray-500 text-white hover:bg-gray-600',
        bannerBg: 'bg-gray-100',
      }
    case 'reviewed':
      return {
        label: 'Reviewed',
        className: 'bg-sky-500 text-white',
        chipActive: 'bg-sky-600 border border-sky-600 text-white',
        chipInactive: 'bg-sky-500 border border-sky-500 text-white hover:bg-sky-600',
        bannerBg: 'bg-sky-100',
      }
    case 'shortlisted':
      return {
        label: 'Shortlisted',
        className: 'bg-orange-500 text-white',
        chipActive: 'bg-orange-600 border border-orange-600 text-white',
        chipInactive: 'bg-orange-500 border border-orange-500 text-white hover:bg-orange-600',
        bannerBg: 'bg-orange-100',
      }
    case 'interview':
      return {
        label: 'Interview',
        className: 'bg-amber-500 text-white',
        chipActive: 'bg-amber-600 border border-amber-600 text-white',
        chipInactive: 'bg-amber-500 border border-amber-500 text-white hover:bg-amber-600',
        bannerBg: 'bg-amber-100',
      }
    case 'hired':
      return {
        label: 'Hired',
        className: 'bg-emerald-500 text-white',
        chipActive: 'bg-emerald-600 border border-emerald-600 text-white',
        chipInactive: 'bg-emerald-500 border border-emerald-500 text-white hover:bg-emerald-600',
        bannerBg: 'bg-emerald-100',
      }
    case 'rejected':
      return {
        label: 'Rejected',
        className: 'bg-rose-500 text-white',
        chipActive: 'bg-rose-600 border border-rose-600 text-white',
        chipInactive: 'bg-rose-500 border border-rose-500 text-white hover:bg-rose-600',
        bannerBg: 'bg-red-100',
      }
    default:
      return {
        label: status,
        className: 'bg-gray-500 text-white',
        chipActive: 'bg-gray-600 border border-gray-600 text-white',
        chipInactive: 'bg-gray-500 border border-gray-500 text-white hover:bg-gray-600',
        bannerBg: 'bg-gray-100',
      }
  }
}

/** Pre-computed badge map for quick lookup by status string. */
const badgeMap = Object.fromEntries(
  (['pending', 'reviewed', 'shortlisted', 'interview', 'hired', 'rejected'] as ApplicationStatus[])
    .map((s) => [s, statusBadge(s)]),
)

/** Get chip classes for a status — returns generic styles for the "All" option. */
export function getChipClasses(
  status: ApplicationStatus | '',
  isActive: boolean,
): string {
  if (status === '') {
    return isActive
      ? 'bg-primary text-on-primary border border-primary'
      : 'bg-primary/70 text-on-primary border border-primary/70 hover:bg-primary'
  }
  const badge = badgeMap[status]
  if (!badge) {
    return isActive
      ? 'bg-primary text-on-primary border border-primary'
      : 'bg-primary/70 text-on-primary border border-primary/70 hover:bg-primary'
  }
  return isActive ? badge.chipActive : badge.chipInactive
}
