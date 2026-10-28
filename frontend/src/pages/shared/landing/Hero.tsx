import { useState, useEffect, useRef } from 'react'
import type { CSSProperties } from 'react'
import { useNavigate } from 'react-router-dom'
import { fetchHeroSuggestions } from '../../../api/jobs.ts'
import { useAuthUser } from '../../../hooks/useAuthUser.ts'
import type { HeroRoleSuggestion } from '../../../types'

/**
 * ★ HERO TOP SPACING KNOBS ★
 * Controls the vertical distance (in pixels) from the top of the screen to the
 * "Find your dream job. Instantly on SeraGo" headline.
 *   • HERO_TOP_OFFSET_MOBILE_PX  = Distance on mobile/phone screens in px (e.g. 80, 96, 112)
 *   • HERO_TOP_OFFSET_DESKTOP_PX = Distance on desktop screens in px (e.g. 120, 140, 160)
 */
export const HERO_TOP_OFFSET_MOBILE_PX = 110
export const HERO_TOP_OFFSET_DESKTOP_PX = 110

const PLACEHOLDER_ROLES = [
  'Software Developer',
  'Senior Accountant',
  'Civil Engineer',
  'Data Analyst',
  'Marketing Specialist',
  'Graphic Designer',
]

export default function Hero() {
  const navigate = useNavigate()
  const auth = useAuthUser()

  const [searchInput, setSearchInput] = useState('')
  const [debouncedInput, setDebouncedInput] = useState('')
  const [placeholderIndex, setPlaceholderIndex] = useState(0)
  const [suggestions, setSuggestions] = useState<HeroRoleSuggestion[]>([])
  const [totalPositions, setTotalPositions] = useState<number>(3420)
  const [isLoading, setIsLoading] = useState(false)
  const [isOpen, setIsOpen] = useState(false)
  const [selectedIndex, setSelectedIndex] = useState<number>(-1)

  const containerRef = useRef<HTMLDivElement>(null)
  const inputRef = useRef<HTMLInputElement>(null)

  // Rotate placeholder text every 2.8s when input is empty
  useEffect(() => {
    if (searchInput.trim().length > 0) return
    const interval = setInterval(() => {
      setPlaceholderIndex((prev) => (prev + 1) % PLACEHOLDER_ROLES.length)
    }, 2800)
    return () => clearInterval(interval)
  }, [searchInput])

  // Debounce user keystrokes (250ms)
  useEffect(() => {
    const handler = setTimeout(() => {
      setDebouncedInput(searchInput.trim())
    }, 250)
    return () => clearTimeout(handler)
  }, [searchInput])

  // Fetch suggestions when user types
  useEffect(() => {
    if (!debouncedInput) {
      setSuggestions([])
      setIsOpen(false)
      setIsLoading(false)
      return
    }

    const controller = new AbortController()
    setIsLoading(true)

    fetchHeroSuggestions(debouncedInput, 5, controller.signal)
      .then((res) => {
        if (res?.suggestions && res.suggestions.length > 0) {
          setSuggestions(res.suggestions)
          setIsOpen(true)
        } else {
          setSuggestions([])
          setIsOpen(false)
        }
        if (res?.totalLivePositions) {
          setTotalPositions(res.totalLivePositions)
        }
        setSelectedIndex(-1)
      })
      .catch((err: unknown) => {
        if ((err as Error)?.name !== 'AbortError') {
          setSuggestions([])
        }
      })
      .finally(() => {
        setIsLoading(false)
      })

    return () => controller.abort()
  }, [debouncedInput])

  // Outside click closes dropdown
  useEffect(() => {
    const handleClickOutside = (e: MouseEvent) => {
      if (containerRef.current && !containerRef.current.contains(e.target as Node)) {
        setIsOpen(false)
      }
    }
    document.addEventListener('mousedown', handleClickOutside)
    return () => document.removeEventListener('mousedown', handleClickOutside)
  }, [])

  const executeSearch = (query: string) => {
    const trimmed = query.trim()
    if (trimmed) {
      navigate(`/jobs?q=${encodeURIComponent(trimmed)}`)
    } else {
      navigate('/jobs')
    }
  }

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault()
    if (selectedIndex >= 0 && selectedIndex < suggestions.length) {
      executeSearch(suggestions[selectedIndex].title)
    } else {
      executeSearch(searchInput)
    }
  }

  const handleSelectRole = (title: string) => {
    executeSearch(title)
  }

  const handleKeyDown = (e: React.KeyboardEvent<HTMLInputElement>) => {
    if (!isOpen) {
      if (e.key === 'ArrowDown' || e.key === 'ArrowUp') {
        if (suggestions.length > 0) {
          setIsOpen(true)
        }
        return
      }
    }

    if (e.key === 'ArrowDown') {
      e.preventDefault()
      setSelectedIndex((prev) => (prev < suggestions.length - 1 ? prev + 1 : 0))
    } else if (e.key === 'ArrowUp') {
      e.preventDefault()
      setSelectedIndex((prev) => (prev > 0 ? prev - 1 : suggestions.length - 1))
    } else if (e.key === 'Escape') {
      setIsOpen(false)
    }
  }

  const handleCvClick = () => {
    if (auth.status === 'authenticated') {
      navigate('/dashboard/talent')
    } else {
      navigate('/signup')
    }
  }

  const currentPlaceholder = `Search "${PLACEHOLDER_ROLES[placeholderIndex]}"...`
  const totalPositionsFormatted = totalPositions.toLocaleString()

  return (
    <section
      style={
        {
          '--hero-top-mobile': `${HERO_TOP_OFFSET_MOBILE_PX}px`,
          '--hero-top-desktop': `${HERO_TOP_OFFSET_DESKTOP_PX}px`,
        } as CSSProperties
      }
      className="relative w-full overflow-hidden bg-surface pt-[var(--hero-top-mobile)] md:pt-[var(--hero-top-desktop)] pb-14 sm:pb-20 lg:pb-24"
    >
      {/* Subtle ambient glow in center */}
      <div className="pointer-events-none absolute top-1/2 left-1/2 h-[480px] w-[760px] -translate-x-1/2 -translate-y-1/2 rounded-full bg-gradient-to-tr from-blue-100/30 via-sky-50/40 to-orange-100/20 blur-3xl" />

      <div className="relative z-10 mx-auto flex max-w-5xl flex-col items-center px-3.5 text-center sm:px-6 lg:px-8">
        {/* Main Bold Headline */}
        <h1 className="mb-3 max-w-4xl text-[30px] font-extrabold tracking-tight text-[#1A2B3C] leading-[36px] xs:text-[36px] xs:leading-[44px] sm:mb-5 sm:text-[52px] sm:leading-[60px] lg:text-[64px] lg:leading-[72px]">
          Find your dream job. Instantly on{' '}
          <span className="inline-block whitespace-nowrap">
            <span className="text-[#1A73E8]">Sera</span>
            <span className="text-[#FF6600]">Go</span>
          </span>
        </h1>

        {/* Subheadline */}
        <p className="mb-6 max-w-2xl text-[15px] font-normal leading-relaxed text-[#5F6368] sm:mb-10 sm:text-[18px] md:text-[20px]">
          Search thousands of jobs across Ethiopia, or let our AI match you perfectly.
        </p>

        {/* Central Search & Interactive Floating Dropdown Container (~800px max width) */}
        <div ref={containerRef} className="relative z-30 flex w-full max-w-[800px] flex-col items-center">
          {/* Responsive Pill Search Bar */}
          <form
            onSubmit={handleSubmit}
            className="flex w-full items-center justify-between gap-2 rounded-full border border-slate-200/80 bg-white p-1.5 pl-3.5 shadow-[0_15px_35px_-10px_rgba(0,0,0,0.06),0_4px_12px_rgba(0,0,0,0.03)] transition-all focus-within:border-blue-400 focus-within:ring-4 focus-within:ring-blue-100/60 xs:gap-2.5 xs:p-2 xs:pl-4 sm:gap-3 sm:p-2.5 sm:pl-6"
          >
            <div className="flex min-w-0 flex-1 items-center gap-2 sm:gap-3.5">
              <span className="material-symbols-outlined shrink-0 text-[20px] text-[#1A73E8] xs:text-[22px] sm:text-[26px]">
                search
              </span>
              <input
                ref={inputRef}
                type="text"
                autoComplete="off"
                value={searchInput}
                onChange={(e) => {
                  setSearchInput(e.target.value)
                  if (e.target.value.trim().length > 0) {
                    setIsOpen(true)
                  } else {
                    setIsOpen(false)
                  }
                }}
                onFocus={() => {
                  if (searchInput.trim().length > 0 && suggestions.length > 0) {
                    setIsOpen(true)
                  }
                }}
                onKeyDown={handleKeyDown}
                placeholder={currentPlaceholder}
                className="w-full min-w-0 border-0 bg-transparent p-0 text-sm font-medium leading-normal text-[#1A2B3C] placeholder-slate-400 focus:outline-none focus:ring-0 xs:text-[15px] sm:text-base md:text-lg"
              />
              {isLoading && (
                <div className="mr-1 h-3.5 w-3.5 shrink-0 animate-spin rounded-full border-2 border-slate-300 border-t-[#1A73E8] xs:mr-1.5 xs:h-4 xs:w-4" />
              )}
            </div>

            <button
              type="submit"
              aria-label="Search jobs"
              className="inline-flex shrink-0 items-center justify-center rounded-full bg-[#1A73E8] font-semibold text-white shadow-md shadow-blue-500/25 transition-all hover:bg-[#155fc0] hover:shadow-lg hover:shadow-blue-500/35 active:scale-95 cursor-pointer h-10 w-10 xs:h-11 xs:w-auto xs:gap-1.5 xs:px-4 xs:py-2.5 sm:h-auto sm:gap-2 sm:px-8 sm:py-4 sm:text-[15px]"
            >
              <span className="hidden xs:inline text-xs sm:text-[15px]">Search</span>
              <span className="material-symbols-outlined text-[18px] sm:text-[20px]">arrow_forward</span>
            </button>
          </form>

          {/* Floating Predictive Dropdown Card (only rendered when user has typed something) */}
          {isOpen && searchInput.trim().length > 0 && suggestions.length > 0 && (
            <div className="mt-2.5 w-full rounded-2xl bg-gradient-to-b from-blue-200/70 via-slate-200/50 to-orange-100/50 p-[1px] shadow-[0_25px_50px_-12px_rgba(0,0,0,0.12),0_8px_24px_rgba(0,0,0,0.04)] animate-in fade-in-50 duration-200 sm:mt-3">
              <div className="w-full divide-y divide-slate-100 overflow-hidden rounded-[15px] bg-white text-left">
                {suggestions.map((item, index) => {
                  const isSelected = index === selectedIndex
                  return (
                    <button
                      key={`${item.title}-${index}`}
                      type="button"
                      onClick={() => handleSelectRole(item.title)}
                      className={`group flex w-full items-center justify-between p-3 sm:p-4 sm:px-6 text-left transition-colors cursor-pointer ${
                        isSelected ? 'bg-blue-50/70' : 'hover:bg-slate-50/80'
                      }`}
                    >
                      <div className="min-w-0 pr-2 sm:pr-4 flex-1">
                        <div className="flex items-center gap-2">
                          <span className="truncate text-sm font-semibold text-[#1A2B3C] transition-colors group-hover:text-[#1A73E8] sm:text-base">
                            {item.title}
                          </span>
                        </div>
                        <div className="mt-0.5 flex items-center gap-1.5 text-xs text-slate-500 sm:text-sm">
                          <span className="truncate max-w-[120px] xs:max-w-none">{item.location}</span>
                          <span>•</span>
                          <span className="font-medium text-slate-700 whitespace-nowrap">
                            {item.openingsCount} openings
                          </span>
                        </div>
                      </div>

                      <div className="ml-2 flex shrink-0 items-center gap-1.5 sm:ml-3 sm:gap-2">
                        <span className="inline-flex items-center rounded-full border border-orange-200/60 bg-orange-50 px-2 py-0.5 text-[11px] font-bold text-[#FF6B00] shadow-xs sm:px-2.5 sm:py-1 sm:text-xs">
                          {item.matchPercentage}% match
                        </span>
                        <span
                          className={`material-symbols-outlined text-[16px] sm:text-[18px] transition-all ${
                            isSelected
                              ? 'translate-x-0.5 text-[#1A73E8]'
                              : 'text-slate-300 group-hover:translate-x-0.5 group-hover:text-[#1A73E8]'
                          }`}
                        >
                          chevron_right
                        </span>
                      </div>
                    </button>
                  )
                })}

                {/* Dropdown Footer */}
                <div className="flex flex-col gap-1.5 sm:flex-row sm:items-center sm:justify-between bg-slate-50/60 px-4 py-2.5 sm:px-6 sm:py-3 text-xs text-slate-500">
                  <button
                    type="button"
                    onClick={() => executeSearch(searchInput)}
                    className="flex items-center gap-1.5 transition-colors hover:text-slate-800 cursor-pointer"
                  >
                    <span className="inline-block rounded border border-slate-200 bg-white px-1.5 py-0.5 font-mono text-[10px] sm:text-[11px] text-slate-600 shadow-2xs">
                      Enter ↵
                    </span>
                    <span className="text-[11px] sm:text-xs">to view all {totalPositionsFormatted}+ live positions</span>
                  </button>

                  <button
                    type="button"
                    onClick={() => navigate('/jobs')}
                    className="cursor-pointer text-[11px] font-medium text-blue-600 hover:underline text-left sm:text-right"
                  >
                    Explore advanced filters
                  </button>
                </div>
              </div>
            </div>
          )}

          {/* Secondary Action Directly Below Search Area */}
          <div className="mt-4 sm:mt-7">
            <button
              type="button"
              onClick={handleCvClick}
              className="group inline-flex items-center justify-center gap-1.5 xs:gap-2 rounded-full border border-transparent px-3 py-1.5 xs:px-4 xs:py-2 text-xs xs:text-sm font-medium text-slate-700 transition-all hover:border-slate-200 hover:bg-white hover:text-[#1A2B3C] hover:shadow-sm cursor-pointer max-w-full text-center"
            >
              <span className="text-sm xs:text-base text-[#FF6B00] transition-transform group-hover:scale-110">
                ✨
              </span>
              <span>
                Or <strong className="font-semibold text-[#1A2B3C]">upload your CV</strong> for instant AI matching
              </span>
              <span className="font-bold text-[#1A73E8] transition-transform group-hover:translate-x-0.5">
                →
              </span>
            </button>
          </div>
        </div>
      </div>
    </section>
  )
}
