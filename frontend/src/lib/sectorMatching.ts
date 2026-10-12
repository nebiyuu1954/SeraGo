import type { ParsedResumeProfile, SectorResponse } from '../types'

export const MAX_FOR_YOU_SECTORS = 2
export const MAX_RESUME_AUTOFILL_SECTORS = 1

/**
 * Rich domain ontology keywords for canonical sectors.
 * Allows accurate classification regardless of company names (e.g. "DAFTech Social ICT Solutions"
 * is classified as Technology & IT, not Social Science).
 */
export const SECTOR_DOMAIN_KEYWORDS: Record<string, string[]> = {
  'Technology & IT': [
    'software', 'developer', 'programmer', 'programming', 'web', 'frontend', 'front-end',
    'backend', 'back-end', 'fullstack', 'full-stack', 'full stack', 'engineer', 'engineering',
    'computer', 'computing', 'code', 'coding', 'it', 'ict', 'devops', 'cloud', 'database',
    'sql', 'python', 'javascript', 'typescript', 'react', 'angular', 'vue', 'node', 'django',
    'dotnet', '.net', 'net 8', 'c#', 'java', 'c++', 'golang', 'php', 'html', 'css', 'api',
    'rest', 'restful', 'docker', 'kubernetes', 'aws', 'azure', 'git', 'linux', 'cybersecurity',
    'network', 'sysadmin', 'machine learning', 'artificial intelligence', 'data science',
    'hilcoe', 'microfrontends', 'microfrontend', 'microservices', 'system architect', 'tech',
    'iis', 'nginx', 'webpack', 'monorepo', 'mediatr', 'dbeaver', 'swagger', 'postgresql',
    'postman', 'orm', 'entity framework', 'jwt', 'agile', 'scrum', 'qa automation', 'sdk',
  ],
  'Accounting & Finance': [
    'accountant', 'accounting', 'auditor', 'audit', 'tax', 'taxation', 'financial', 'finance',
    'bookkeeper', 'bookkeeping', 'payroll', 'ledger', 'cpa', 'acca', 'ifrs', 'gaap', 'treasury',
    'financial analyst', 'budget', 'cost control', 'reconciliation', 'accounts payable', 'accounts receivable',
  ],
  'Banking & Insurance': [
    'bank', 'banking', 'credit', 'loan', 'mortgage', 'underwriter', 'underwriting', 'insurance',
    'actuary', 'actuarial', 'claims', 'teller', 'branch manager', 'microfinance', 'interest rate',
    'forex', 'risk management', 'credit analysis', 'liquidity',
  ],
  'Healthcare': [
    'doctor', 'physician', 'nurse', 'nursing', 'medical', 'medicine', 'hospital', 'clinic',
    'clinical', 'patient', 'pharmacy', 'pharmacist', 'health', 'healthcare', 'pediatric',
    'surgeon', 'surgery', 'laboratory', 'radiology', 'dental', 'dentist', 'therapy', 'therapist',
    'midwife', 'pathology', 'optometry', 'public health', 'epidemiology',
  ],
  'Engineering & Construction': [
    'civil engineer', 'mechanical engineer', 'electrical engineer', 'structural engineer',
    'construction', 'contractor', 'site engineer', 'architect', 'architecture', 'surveyor',
    'autocad', 'building', 'hvac', 'plumbing', 'quantity surveyor', 'draftsman', 'structural design',
    'mep', 'concrete', 'heavy machinery',
  ],
  'Education & Training': [
    'teacher', 'teaching', 'lecturer', 'professor', 'instructor', 'pedagogy', 'curriculum',
    'school teacher', 'tutor', 'tutoring', 'education', 'trainer', 'academic', 'principal',
    'kindergarten', 'classroom', 'lesson plan', 'student assessment', 'faculty',
  ],
  'Legal': [
    'lawyer', 'attorney', 'legal', 'paralegal', 'advocate', 'prosecutor', 'judge', 'litigation',
    'solicitor', 'juris', 'law', 'counsel', 'compliance officer', 'contract law', 'court',
    'arbitration', 'intellectual property', 'notary',
  ],
  'Human Resources': [
    'human resources', 'hr', 'recruiter', 'recruitment', 'talent acquisition', 'onboarding',
    'employee relations', 'people operations', 'hr manager', 'personnel', 'talent management',
    'performance appraisal', 'staffing', 'labor law',
  ],
  'Sales & Marketing': [
    'sales', 'marketing', 'advertisement', 'advertising', 'digital marketing', 'seo', 'sem',
    'social media marketing', 'brand', 'branding', 'sales representative', 'account executive',
    'business development', 'telemarketing', 'b2b', 'b2c', 'merchandising', 'promoter',
    'lead generation', 'market research', 'campaign',
  ],
  'Design & Creative': [
    'graphic designer', 'ui designer', 'ux designer', 'ui/ux', 'creative director', 'animator',
    'animation', 'video editor', 'illustrator', 'photographer', 'fashion designer', 'art director',
    'visual designer', 'motion graphics', 'figma', 'photoshop', 'illustrator', 'after effects', 'typography',
  ],
  'Logistics & Transportation': [
    'logistics', 'transport', 'transportation', 'fleet', 'driver', 'warehouse', 'warehousing',
    'freight', 'cargo', 'shipping', 'distribution', 'customs clearing', 'dispatch', 'import export',
    'supply chain coordinator', 'transit', 'freight forwarding',
  ],
  'Procurement & Supply Chain': [
    'procurement', 'purchasing', 'supply chain', 'sourcing', 'vendor management', 'inventory control',
    'buyer', 'purchase order', 'tender', 'supplier evaluation', 'material management',
  ],
  'Customer Service & Support': [
    'customer service', 'call center', 'customer support', 'help desk', 'client service',
    'customer care', 'receptionist', 'front desk', 'guest service', 'inbound', 'outbound', 'ticketing',
  ],
  'Business & Administration': [
    'administration', 'administrative', 'executive assistant', 'office manager', 'operations manager',
    'general manager', 'management', 'secretary', 'clerk', 'business analyst', 'project management',
    'office administration', 'records management', 'data entry',
  ],
  'Media & Communication': [
    'journalist', 'journalism', 'reporter', 'news', 'editor', 'editorial', 'broadcasting',
    'media', 'communications', 'public relations', 'pr specialist', 'content creator', 'copywriter',
    'broadcaster', 'producer', 'press release',
  ],
  'Hospitality & Tourism': [
    'hotel', 'hospitality', 'restaurant', 'waiter', 'waitress', 'chef', 'cook', 'culinary',
    'bartender', 'catering', 'tourism', 'tour guide', 'travel agent', 'housekeeping', 'concierge',
    'resort', 'food and beverage', 'guest relations',
  ],
  'Manufacturing & Production': [
    'manufacturing', 'factory', 'production line', 'assembly line', 'plant operator',
    'quality control', 'qa/qc', 'industrial engineering', 'fabrication', 'machinist', 'cnc',
    'process engineer', 'maintenance engineer',
  ],
  'Agriculture & Natural Science': [
    'agriculture', 'agronomist', 'agronomy', 'farming', 'farmer', 'crop', 'livestock',
    'veterinary', 'veterinarian', 'botany', 'forestry', 'horticulture', 'soil science',
    'biology', 'chemist', 'chemistry', 'environmental science', 'irrigation', 'agribusiness',
  ],
  'Security & Protection': [
    'security guard', 'surveillance', 'safety officer', 'patrol', 'loss prevention',
    'security officer', 'cctv operator', 'bodyguard', 'access control', 'physical security', 'fire safety',
  ],
  'Beauty & Grooming': [
    'barber', 'hair stylist', 'hairdresser', 'salon', 'spa', 'makeup artist', 'esthetician',
    'cosmetology', 'beautician', 'manicurist', 'skincare', 'massage therapist',
  ],
  'Skilled & General Labor': [
    'carpenter', 'plumber', 'electrician', 'welder', 'mason', 'laborer', 'cleaner', 'janitor',
    'maintenance technician', 'mechanic', 'painter', 'handyman', 'facility maintenance',
  ],
  'Social Science & Community': [
    'social work', 'social worker', 'community development', 'sociology', 'humanitarian',
    'counselor', 'counseling', 'psychology', 'psychologist', 'anthropology', 'community outreach',
    'human rights', 'child protection', 'social welfare', 'civil society', 'ngo worker', 'community mobilization',
  ],
}

/**
 * Typical sample job titles / roles that belong to each sector.
 * Shown to users so they understand what opportunities a sector encompasses.
 */
export const SECTOR_SAMPLE_ROLES: Record<string, string[]> = {
  'Technology & IT': [
    'Software Engineering',
    'Data Science & Analytics',
    'AI & Machine Learning',
    'Full Stack Web Development',
    'Cloud & DevOps Engineering',
    'Mobile App Development',
  ],
  'Accounting & Finance': [
    'Senior Accountant',
    'Financial Analyst',
    'Internal / External Auditor',
    'Tax Consultant',
    'Finance Manager',
  ],
  'Banking & Insurance': [
    'Credit & Risk Analyst',
    'Loan Officer',
    'Insurance Underwriter',
    'Branch Operations Manager',
    'Treasury & Forex Specialist',
  ],
  'Healthcare': [
    'General Physician / Doctor',
    'Clinical Nurse',
    'Pharmacist',
    'Medical Laboratory Technologist',
    'Public Health Officer',
  ],
  'Engineering & Construction': [
    'Civil Engineer',
    'Site & Project Engineer',
    'Electrical Engineer',
    'Mechanical Engineer',
    'Architectural Designer',
  ],
  'Education & Training': [
    'University Lecturer',
    'Curriculum Specialist',
    'Secondary School Teacher',
    'Corporate Trainer',
    'Educational Consultant',
  ],
  'Legal': [
    'Legal Counsel / Attorney',
    'Corporate Legal Advisor',
    'Compliance Officer',
    'Paralegal Specialist',
    'Contract Specialist',
  ],
  'Human Resources': [
    'HR Generalist / Specialist',
    'Talent Acquisition / Recruiter',
    'People Operations Manager',
    'Employee Relations Officer',
  ],
  'Sales & Marketing': [
    'Digital Marketing Specialist',
    'Business Development Manager',
    'Brand & Communications Manager',
    'Sales Executive',
  ],
  'Design & Creative': [
    'UI / UX Designer',
    'Graphic Designer',
    'Art Director',
    'Motion Graphics & Video Editor',
    'Brand Identity Designer',
  ],
  'Logistics & Transportation': [
    'Logistics Coordinator',
    'Fleet Operations Manager',
    'Supply Chain Analyst',
    'Warehouse & Inventory Supervisor',
  ],
  'Procurement & Supply Chain': [
    'Procurement Officer',
    'Sourcing & Vendor Specialist',
    'Supply Chain Manager',
    'Purchasing Agent',
  ],
  'Customer Service & Support': [
    'Customer Support Representative',
    'Client Success Specialist',
    'Help Desk Support Specialist',
    'Call Center Team Lead',
  ],
  'Business & Administration': [
    'Operations Manager',
    'Executive Assistant',
    'Business Analyst',
    'Office Administration Lead',
  ],
  'Media & Communication': [
    'Communications Specialist',
    'Content Creator & Copywriter',
    'Journalist / Media Editor',
    'Public Relations (PR) Officer',
  ],
  'Hospitality & Tourism': [
    'Hotel Operations Manager',
    'Executive Chef',
    'Guest Relations Manager',
    'Tour & Travel Coordinator',
  ],
  'Manufacturing & Production': [
    'Production Operations Supervisor',
    'Quality Control (QA/QC) Engineer',
    'Plant Maintenance Engineer',
    'Industrial Process Engineer',
  ],
  'Agriculture & Natural Science': [
    'Agronomist / Crop Specialist',
    'Agricultural Extension Officer',
    'Environmental Scientist',
    'Veterinary Officer',
  ],
  'Security & Protection': [
    'Security Operations Supervisor',
    'Occupational Safety & Health Officer',
    'Surveillance & Loss Prevention Lead',
  ],
  'Beauty & Grooming': [
    'Senior Stylist & Hair Specialist',
    'Esthetician & Skincare Specialist',
    'Salon & Spa Manager',
  ],
  'Skilled & General Labor': [
    'Maintenance Electrician',
    'Plumber & Pipe Technician',
    'Facility Maintenance Technician',
    'Welder & Metal Fabricator',
  ],
  'Social Science & Community': [
    'Community Development Officer',
    'Humanitarian Program Officer',
    'Social Worker / Counselor',
    'Monitoring & Evaluation (M&E) Lead',
  ],
}

export function getSampleRolesForSector(sectorName?: string): string[] {
  if (!sectorName) return []
  return (
    SECTOR_SAMPLE_ROLES[sectorName] || [
      'Specialist',
      'Team Lead',
      'Coordinator',
      'Manager',
    ]
  )
}

/**
 * Calculates a match score for a sector against resume content.
 */
export function scoreSectorForResume(
  sectorName: string,
  parsed: ParsedResumeProfile,
): number {
  const keywords = SECTOR_DOMAIN_KEYWORDS[sectorName] || []
  if (!keywords.length) return 0

  const headline = (parsed.headline || '').toLowerCase()
  const profession = (parsed.currentProfession || '').toLowerCase()
  const skills = (parsed.skills || []).map((s) => s.toLowerCase()).join(' ')
  const education = (parsed.education || []).map((e) => `${e.degree || ''} ${e.institution || ''}`).join(' ').toLowerCase()
  const experience = (parsed.experience || []).map((e) => `${e.title || ''} ${e.description || ''}`).join(' ').toLowerCase()

  const fullText = `${headline} ${profession} ${skills} ${education} ${experience}`

  let score = 0

  for (const kw of keywords) {
    const kwLower = kw.toLowerCase()
    if (fullText.includes(kwLower)) {
      // Base points
      const points = kwLower.includes(' ') ? 3 : 2
      score += points

      // High value in headline or profession
      if (headline.includes(kwLower) || profession.includes(kwLower)) {
        score += 5
      }
      // High value in skills
      if (skills.includes(kwLower)) {
        score += 3
      }
      // High value in education
      if (education.includes(kwLower)) {
        score += 3
      }
    }
  }

  return score
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

  // 1. Score all sectors using domain keywords
  const rankedSectors = sectors
    .map((sector) => ({
      id: sector.id,
      name: sector.name,
      score: scoreSectorForResume(sector.name, parsed),
    }))
    .filter((s) => s.score > 0)
    .sort((a, b) => b.score - a.score)

  // If we have a strong keyword-based winner, that takes precedence over naive suggestions
  if (rankedSectors.length > 0) {
    return [rankedSectors[0].id]
  }

  // 2. Direct suggestedSectorIds from API as fallback
  if (parsed.suggestedSectorIds?.length) {
    const valid = parsed.suggestedSectorIds.find((id) => sectors.some((s) => s.id === id))
    if (valid) return [valid]
  }

  // 3. Direct name matching as fallback
  if (parsed.suggestedSectors?.length) {
    for (const name of parsed.suggestedSectors) {
      const match = sectors.find(
        (s) =>
          s.name.toLowerCase() === name.toLowerCase() ||
          s.slug.toLowerCase() === name.toLowerCase(),
      )
      if (match) return [match.id]
    }
  }

  return []
}
