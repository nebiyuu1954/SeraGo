import type { ParsedResumeProfile, SectorResponse } from '../types'

export const MAX_FOR_YOU_SECTORS = 2
export const MAX_RESUME_AUTOFILL_SECTORS = 1

const STOP_WORDS = new Set([
  'with', 'from', 'that', 'this', 'have', 'will', 'your', 'their', 'work',
  'experience', 'years', 'using', 'used', 'team', 'also', 'more', 'other',
  'into', 'skills', 'services', 'business', 'company', 'level', 'strong',
])

function tokenize(text: string): Set<string> {
  const words = text
    .toLowerCase()
    .split(/[^a-z0-9]+/)
    .filter((w) => w.length >= 3 && !STOP_WORDS.has(w))
  return new Set(words)
}

/**
 * Given a parsed resume profile and active sectors list, returns only the single highest
 * matching sector ID (max 1) for the candidate's "For You" job feed.
 */
export function matchSuggestedSectorIds(
  parsed: ParsedResumeProfile,
  sectors: SectorResponse[],
): string[] {
  if (!sectors.length) return []

  const resultIds: string[] = []

  const tryAdd = (id?: string) => {
    if (!id) return
    if (!resultIds.includes(id) && sectors.some((s) => s.id === id) && resultIds.length < MAX_RESUME_AUTOFILL_SECTORS) {
      resultIds.push(id)
    }
  }

  // 1. Direct suggestedSectorIds from API (highest first)
  if (parsed.suggestedSectorIds?.length) {
    for (const id of parsed.suggestedSectorIds) {
      tryAdd(id)
      if (resultIds.length >= MAX_RESUME_AUTOFILL_SECTORS) break
    }
  }

  if (resultIds.length >= MAX_RESUME_AUTOFILL_SECTORS) {
    return resultIds
  }

  // 2. Direct name matching from suggestedSectors
  if (parsed.suggestedSectors?.length) {
    for (const name of parsed.suggestedSectors) {
      const match = sectors.find(
        (s) =>
          s.name.toLowerCase() === name.toLowerCase() ||
          s.slug.toLowerCase() === name.toLowerCase(),
      )
      if (match) tryAdd(match.id)
    }
  }

  if (resultIds.length >= MAX_RESUME_AUTOFILL_SECTORS) {
    return resultIds
  }

  // 3. Match from currentIndustry
  if (parsed.currentIndustry) {
    const match = sectors.find(
      (s) =>
        s.name.toLowerCase() === parsed.currentIndustry?.toLowerCase() ||
        s.slug.toLowerCase() === parsed.currentIndustry?.toLowerCase(),
    )
    if (match) tryAdd(match.id)
  }

  if (resultIds.length >= MAX_RESUME_AUTOFILL_SECTORS) {
    return resultIds
  }

  // 4. Keyword heuristic based on headline, profession, skills
  const resumeText = [
    parsed.headline ?? '',
    parsed.currentProfession ?? '',
    parsed.skills.join(' '),
    ...parsed.experience.map((e) => `${e.title} ${e.company}`),
  ].join(' ')

  const resumeTokens = tokenize(resumeText)
  if (resumeTokens.size > 0) {
    const scored = sectors
      .filter((s) => !resultIds.includes(s.id))
      .map((sector) => {
        const sectorTokens = tokenize(sector.name)
        let matches = 0
        sectorTokens.forEach((token) => {
          if (resumeTokens.has(token)) matches += 1
        })
        return { id: sector.id, score: matches }
      })
      .filter((item) => item.score > 0)
      .sort((a, b) => b.score - a.score)

    for (const item of scored) {
      tryAdd(item.id)
      if (resultIds.length >= MAX_RESUME_AUTOFILL_SECTORS) break
    }
  }

  return resultIds
}
