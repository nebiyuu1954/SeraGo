/**
 * Splits a free-text job description into labeled sections.
 *
 * Source listings (EthioJobs, Afriwork, GeezJobs…) arrive as plain text with
 * the structure embedded in different ways, so the parser handles three
 * patterns:
 *
 *   1. Standalone heading lines — "Key Responsibilities", "How to Apply",
 *      "Job Overview" on their own line.
 *   2. Inline labeled values — "Education Qualification: BSc in Civil
 *      Engineering", "Salary: 8,652 birr", even several in one paragraph
 *      ("work place: Addis Ababa Salary: 8,652 birr").
 *   3. Amharic labels — "ደመወዝ:-" (salary), "ማሳሰቢያ:-" (note).
 *
 * The detail page then renders each section under its own labeled heading
 * instead of one wall of text. Only known labels are recognized, so ordinary
 * prose (e.g. "based on experience") is never mistaken for a section.
 */

export interface DescriptionSection {
  /** Canonical heading label — null for the intro before the first heading. */
  heading: string | null
  /** Material icon shown next to the heading. */
  icon: string
  /** The raw lines under this heading (newlines preserved). */
  content: string
}

interface HeadingRule {
  heading: string
  icon: string
  /** Full-line (anchored) pattern for standalone headings. */
  pattern: RegExp
  /** Unanchored keyword alternatives for inline "Label: value" detection. */
  keyword: RegExp
}

// Canonical sections. Standalone patterns match a whole line (optional
// trailing colon); keyword patterns find the same labels mid-line when they
// are followed by a separator (":", "-" or ":-").
const SECTION_RULES: HeadingRule[] = [
  {
    heading: 'About this job',
    icon: 'description',
    pattern:
      /^(job\s*(description|summary|details)?|position\s*(summary|overview)?|about\s+(the\s+)?(job|role|position|company|us|organization|organisation)|about\s+[A-Z][A-Za-z0-9&'’\- ]*|role\s*overview|overview|summary)\s*:?$/i,
    keyword:
      /(?:job\s*(?:description|summary|details)|position\s*(?:summary|overview)|about\s+(?:the\s+)?(?:job|role|position|company|us|organization|organisation)|role\s*overview|overview|summary)/i,
  },
  {
    heading: 'Duties & responsibilities',
    icon: 'checklist',
    pattern:
      /^((key|main|core|primary|major)\s+)?(duties|responsibilities)(\s+(and|&)\s+(duties|responsibilities))?\s*:?$/i,
    keyword:
      /(?:(?:key|main|core|primary|major)\s+)?(?:duties|responsibilities)(?:\s+(?:and|&)\s+(?:duties|responsibilities))?/i,
  },
  {
    heading: 'Education & qualifications',
    icon: 'school',
    pattern:
      /^(educational?\s+qualifications?|qualifications?|education|የትምህርት|የት\/ት\s+ደረጃ)\s*:?$/i,
    keyword:
      /education(?:al)?\s+qualifications?|qualifications?|education|የትምህርት|የት\/ት\s+ደረጃ/i,
  },
  {
    heading: 'Requirements',
    icon: 'fact_check',
    pattern:
      /^(((job|minimum|essential|basic|required|general|key|main)\s+)?requirements?|qualifications?\s*(and|&)?\s*requirements?)\s*:?$/i,
    keyword:
      /(?:job|minimum|essential|basic|required|general|key|main)?\s*requirements?|qualifications?\s*(?:and|&)?\s*requirements?/i,
  },
  {
    heading: 'Experience',
    icon: 'work_history',
    pattern: /^((work\s+)?experience|የስራ\s+ልምድ|የሥራ\s+ልምድ)\s*:?$/i,
    keyword: /(?:work\s+)?experience|የስራ\s+ልምድ|የሥራ\s+ልምድ/i,
  },
  {
    heading: 'Skills',
    icon: 'psychology',
    pattern:
      /^(required\s+)?(skills?|technical\s+skills?|competencies?|knowledge\s+and\s+skills)\s*:?$/i,
    keyword: /(?:required|technical)\s+skills?|skills?|competencies?/i,
  },
  {
    heading: 'Salary & compensation',
    icon: 'payments',
    pattern:
      /^(compensation\s+salary|salary(\s+(and|&)\s+(benefits?|compensation))?|compensation|remuneration|benefits?|ደመወዝ)\s*:?$/i,
    keyword:
      /compensation\s+salary|salary|compensation|remuneration|benefits?|ደመወዝ/i,
  },
  {
    heading: 'Vacancies',
    icon: 'group',
    pattern:
      /^(vacanc(?:y|ies)|number\s+of\s+(?:posts?|positions?)|no\.?\s*of\s+posts?|quantity)\s*:?$/i,
    keyword:
      /vacanc(?:y|ies)|number\s+of\s+(?:posts?|positions?)|no\.?\s*(?:of\s+)?(?:posts?|positions?)|quantity/i,
  },
  {
    heading: 'How to apply',
    icon: 'how_to_reg',
    pattern:
      /^(how\s+to\s+apply|to\s+apply|application\s*(procedure|instructions?|method|process|guidelines?)?|submission\s*(of\s+documents)?)\s*:?$/i,
    keyword:
      /how\s+to\s+apply|to\s+apply|application\s*(?:procedure|instructions?|method|process|guidelines?)?|submission\b/i,
  },
  {
    heading: 'Deadline',
    icon: 'event',
    pattern: /^(application\s+)?(deadline|closing\s+date)\s*:?$/i,
    keyword: /(?:application\s+)?deadline|closing\s+date/i,
  },
  {
    heading: 'Location & workplace',
    icon: 'location_on',
    pattern:
      /^(duty\s+station|work(ing)?\s*(place|location|address)|place\s+of\s+work|workplace|work\s+address|location|የስራ\s+ቦታ|የሥራ\s+ቦታ|ቦታ)\s*:?$/i,
    keyword:
      /duty\s+station|work(?:ing)?\s*(?:place|location|address)|place\s+of\s+work|workplace|work\s+address|location|የስራ\s+ቦታ|የሥራ\s+ቦታ|ቦታ/i,
  },
  {
    heading: 'Employment type',
    icon: 'schedule',
    pattern:
      /^(employment\s+type|contract\s*(type|terms?|duration)?|terms?\s+of\s+employment|duration|job\s+type)\s*:?$/i,
    keyword:
      /employment\s+type|contract\s*(?:type|terms?|duration)?|terms?\s+of\s+employment|duration|job\s+type/i,
  },
  {
    heading: 'Note',
    icon: 'info',
    pattern:
      /^(note|notice|important|general\s+notes?|additional\s+information|other\s+information|ማሳሰቢያ|ማስታወሻ)\s*:?$/i,
    keyword: /note|notice|important|ማሳሰቢያ|ማስታወሻ/i,
  },
  {
    heading: 'Contact',
    icon: 'call',
    pattern:
      /^(contact|for\s+(more\s+)?information|enquiries?|inquiries?|ለበለጠ\s+መረጃ)\s*:?$/i,
    keyword: /contact|for\s+(?:more\s+)?information|enquiries?|inquiries?|ለበለጠ\s+መረጃ/i,
  },
]

const MAX_HEADING_LENGTH = 70

/** Whole-line headings ("Key Responsibilities", "Salary:"). */
function detectStandaloneHeading(
  line: string,
): { heading: string; icon: string } | null {
  if (line.length > MAX_HEADING_LENGTH) return null
  for (const rule of SECTION_RULES) {
    if (rule.pattern.test(line)) {
      return { heading: rule.heading, icon: rule.icon }
    }
  }
  return null
}

interface InlineSplit {
  heading: string
  icon: string
  /** Character offset where the keyword starts. */
  start: number
  /** Character offset where the value starts (after the label + separator). */
  end: number
}

/**
 * Finds inline "Label: value" occurrences in a line. A keyword only counts
 * when it's followed by a separator and has a non-empty value after it — a
 * bare "Salary:" line (value on the next line) is left for the standalone
 * detector instead.
 */
function splitInlineLabels(line: string): InlineSplit[] {
  const matches: InlineSplit[] = []
  // "…company. HOW TO APPLY https://…" — the label and value on one line
  // with no separator (common on Afriwork). Only after a sentence boundary
  // (line start, newline or period), so "know how to apply for…" mid-sentence
  // is never caught.
  const applyNoSeparator = /(?:^|[\n.])\s*how\s+to\s+apply\s+(?=\S)/gi
  for (const m of line.matchAll(applyNoSeparator)) {
    if (m.index === undefined) continue
    const valueStart = m.index + m[0].length
    if (valueStart >= line.length) continue // no value on this line
    matches.push({
      heading: 'How to apply',
      icon: 'how_to_reg',
      start: m.index,
      end: valueStart,
    })
  }
  for (const rule of SECTION_RULES) {
    const re = new RegExp(
      `(?<![A-Za-z])(${rule.keyword.source})\\s*[:–\\-•]{1,2}\\s+`,
      'gi',
    )
    for (const m of line.matchAll(re)) {
      if (m.index === undefined) continue
      const valueStart = m.index + m[0].length
      if (valueStart >= line.length) continue // no value on this line
      matches.push({
        heading: rule.heading,
        icon: rule.icon,
        start: m.index,
        end: valueStart,
      })
    }
  }
  matches.sort((a, b) => a.start - b.start)
  // Drop overlaps (e.g. "Salary & compensation" matching inside a longer label).
  const kept: InlineSplit[] = []
  for (const m of matches) {
    const prev = kept[kept.length - 1]
    if (prev && m.start < prev.end) continue
    kept.push(m)
  }
  return kept
}

export interface LabelSegment {
  text: string
  /** true when this is a recognized keyword label (e.g. "Salary:") to bold. */
  isLabel: boolean
}

// Keyword + separator, used to bold labels that remain inside section content
// (e.g. "Salary: 8,652 birr" buried inside a Requirements paragraph). Only
// labels followed by ":", "-" or "–" are bolded — plain prose like
// "years of experience" is left alone.
const LABEL_HIGHLIGHT_RE = new RegExp(
  `(?<![A-Za-z])(?:${SECTION_RULES.map((r) => r.keyword.source).join('|')}|preferred)\\s*[:–\\-]{1,2}`,
  'gi',
)

const BULLET_MARKERS = /[•●▪·]/

/**
 * Detects a bulleted list encoded inline on a single line — e.g. HaHu's
 * "Deliver lectures - Prepare lesson notes - Advise students" or Afriwork's
 * "Key Requirements • BSc in Civil Engineering • 3 years experience".
 *
 * Returns the item texts, or null when the text is ordinary prose. Bullet
 * characters (•) are explicit markers and always count; hyphen/en-dash
 * separators only count when at least two are present, so a sentence like
 * "The role covers Addis Ababa - the capital - and Bahir Dar" is not
 * mistaken for a list.
 */
export function splitInlineList(text: string): string[] | null {
  const t = text.trim()
  if (!t) return null
  if (BULLET_MARKERS.test(t)) {
    const chunks = t
      .split(/\s*[•●▪·]\s*/)
      .map((c) => c.trim())
      .filter(Boolean)
    if (chunks.length >= 2) return chunks
    if (chunks.length === 1 && /^[•●▪·]/.test(t)) return chunks
    return null
  }
  const parts = t
    .split(/\s+[-–]\s+/)
    .map((c) => c.trim())
    .filter(Boolean)
  if (parts.length >= 3) return parts
  return null
}

// Money phrase like "monthly; 18,000 ETB (Net)" (with optional frequency
// qualifier before the amount and trailing details like "(Net)" after).
const EMPLOYMENT_MONEY_RE =
  /(?:monthly|annually|per\s+month|per\s+year|per\s+annum|net|gross)?\s*[;,]?\s*\d[\d,]+\.?\d*\s*(birr|etb|br|usd|eur|gbp)\b[^.\n]{0,25}/i
// "Quantity: 1", "Vacancies: 3", "Number of Posts 1(One)".
const VACANCIES_RE =
  /(?:quantity|vacanc(?:y|ies)|number\s+of\s+posts?|no\.?\s*(?:of\s+)?(?:posts?|positions?))\s*:?\s*(\d+)/i
// Whole-line job-type tokens that duplicate the overview's Job type fact.
const JOB_TYPE_TOKEN_RE =
  /^(full[- ]?time|part[- ]?time|contract|permanent|temporary|internship|freelance|onsite|remote|hybrid)\b/gi

/** Facts pulled out of the description for the overview sidebar. */
export interface OverviewExtras {
  /** Money phrase found in an employment-type blob, e.g. "monthly; 18,000 ETB (Net)". */
  salary: string | null
  /** Number of vacancies, e.g. "1" for "Quantity: 1". */
  vacancies: string | null
}

/**
 * Pulls Salary and Vacancies out of description sections so the overview
 * sidebar can show them as proper facts instead of them being buried in the
 * employment-type blob ("Full-time monthly; 18,000 ETB (Net) Quantity: 1").
 */
export function extractOverviewExtras(
  sections: DescriptionSection[],
): OverviewExtras {
  let salary: string | null = null
  let vacancies: string | null = null
  for (const s of sections) {
    if (s.heading !== 'Employment type') continue
    const m = s.content.match(EMPLOYMENT_MONEY_RE)
    if (m && !salary) salary = m[0].trim()
    const v = s.content.match(VACANCIES_RE)
    if (v && !vacancies) vacancies = v[1]
  }
  return { salary, vacancies }
}

/**
 * Strips the bits of an "Employment type" section that duplicate overview
 * facts (job type, salary, vacancies). Returns null when nothing remains.
 */
export function cleanEmploymentTypeSection(
  section: DescriptionSection,
): DescriptionSection | null {
  const kept = section.content.split('\n').filter((line) => {
    const t = line.trim()
    if (!t) return false
    if (JOB_TYPE_TOKEN_RE.test(t)) return false
    if (EMPLOYMENT_MONEY_RE.test(t)) return false
    if (VACANCIES_RE.test(t)) return false
    return true
  })
  if (kept.length === 0) return null
  return { ...section, content: kept.join('\n') }
}

/** Splits text into label vs. plain segments so callers can bold the labels. */
export function highlightLabels(text: string): LabelSegment[] {
  const segments: LabelSegment[] = []
  let last = 0
  for (const m of text.matchAll(LABEL_HIGHLIGHT_RE)) {
    if (m.index === undefined) continue
    if (m.index > last) {
      segments.push({ text: text.slice(last, m.index), isLabel: false })
    }
    segments.push({ text: m[0], isLabel: true })
    last = m.index + m[0].length
  }
  if (last < text.length) {
    segments.push({ text: text.slice(last), isLabel: false })
  }
  return segments
}

/** Splits a description into ordered sections (intro first when no heading). */
export function parseDescriptionSections(text: string): DescriptionSection[] {
  const sections: DescriptionSection[] = []
  let current: DescriptionSection | null = null

  const flush = () => {
    if (current && current.content.trim()) sections.push(current)
  }

  for (const line of text.split('\n')) {
    const trimmed = line.trim()
    if (!trimmed) {
      if (current) current.content += '\n'
      continue
    }

    // "• Skills:" — a bullet marker before a heading label (value on the
    // next line) shouldn't hide the heading, so detect on the marker-stripped
    // line while keeping the original for inline-label splitting below.
    const headingLine = trimmed.replace(/^[•●▪·]\s*/, '')
    const standalone = detectStandaloneHeading(headingLine)
    if (standalone) {
      flush()
      current = { heading: standalone.heading, icon: standalone.icon, content: '' }
      continue
    }

    const splits = splitInlineLabels(line)
    if (splits.length > 0) {
      let pos = 0
      for (const split of splits) {
        // A bullet prefix before a label ("• Education: BSc…") is just a
        // marker — drop it instead of leaving a stray bullet behind.
        const before = line.slice(pos, split.start).replace(/^\s*[•●▪·]\s*/, '')
        if (before.trim()) {
          if (!current) {
            current = { heading: null, icon: 'description', content: '' }
          }
          current.content += `${before}\n`
        }
        flush()
        current = { heading: split.heading, icon: split.icon, content: '' }
        pos = split.end
      }
      const tail = line.slice(pos)
      if (tail.trim()) {
        current!.content += `${tail}\n`
      }
      continue
    }

    if (!current) current = { heading: null, icon: 'description', content: '' }
    current.content += `${line}\n`
  }
  flush()
  return sections
}
