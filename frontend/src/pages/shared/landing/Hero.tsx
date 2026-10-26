import { useState, useEffect, useRef } from 'react'
import { useNavigate } from 'react-router-dom'
import { fetchHeroSuggestions } from '../../../api/jobs.ts'
import { useAuthUser } from '../../../hooks/useAuthUser.ts'
import type { HeroRoleSuggestion } from '../../../types'

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

  const currentPlaceholder = `Search role (e.g. "${PLACEHOLDER_ROLES[placeholderIndex]}")...`
  const totalPositionsFormatted = totalPositions.toLocaleString()

  return (
    <section className="relative w-full overflow-hidden bg-surface py-16 sm:py-20 lg:py-24">
      {/* Subtle ambient glow in center */}
      <div className="pointer-events-none absolute top-1/2 left-1/2 h-[480px] w-[760px] -translate-x-1/2 -translate-y-1/2 rounded-full bg-gradient-to-tr from-blue-100/30 via-sky-50/40 to-orange-100/20 blur-3xl" />

      <div className="relative z-10 mx-auto flex max-w-5xl flex-col items-center px-6 text-center lg:px-8">
        {/* Main Bold Headline */}
        <h1 className="mb-5 max-w-4xl text-[44px] font-extrabold tracking-tight text-[#1A2B3C] sm:text-[56px] sm:leading-[64px] lg:text-[64px] lg:leading-[72px]">
          Find your dream job. Instantly on{' '}
          <span className="inline-block whitespace-nowrap">
            <span className="text-[#1A73E8]">Sera</span>
            <span className="text-[#FF6600]">Go</span>
          </span>
        </h1>

        {/* Subheadline */}
        <p className="mb-10 max-w-2xl text-[18px] font-normal leading-relaxed text-[#5F6368] sm:text-[20px]">
          Search thousands of jobs across Ethiopia, or let our AI match you perfectly.
        </p>

        {/* Central Search & Interactive Floating Dropdown Container (~800px max width) */}
        <div ref={containerRef} className="relative z-30 flex w-full max-w-[800px] flex-col items-center">
          {/* Massive Pill Search Bar */}
          <form
            onSubmit={handleSubmit}
            className="flex w-full items-center justify-between gap-3 rounded-full border border-slate-200/80 bg-white p-2 pl-6 shadow-[0_20px_40px_-15px_rgba(0,0,0,0.07),0_4px_12px_rgba(0,0,0,0.03)] transition-all focus-within:border-blue-400 focus-within:ring-4 focus-within:ring-blue-100/60 sm:p-2.5 sm:pl-6"
          >
            <div className="flex min-w-0 flex-1 items-center gap-3.5">
              <span className="material-symbols-outlined shrink-0 text-[26px] text-[#1A73E8]">
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
                className="w-full border-0 bg-transparent p-0 text-base font-medium leading-normal text-[#1A2B3C] placeholder-slate-400 focus:outline-none focus:ring-0 sm:text-lg"
              />
              {isLoading && (
                <div className="mr-2 h-4 w-4 shrink-0 animate-spin rounded-full border-2 border-slate-300 border-t-[#1A73E8]" />
              )}
            </div>

            <button
              type="submit"
              className="inline-flex shrink-0 items-center justify-center gap-2 rounded-full bg-[#1A73E8] px-6 py-3.5 text-[15px] font-semibold text-white shadow-md shadow-blue-500/25 transition-all hover:bg-[#155fc0] hover:shadow-lg hover:shadow-blue-500/35 cursor-pointer sm:px-8 sm:py-4"
            >
              <span>Search</span>
              <span className="material-symbols-outlined text-[18px]">arrow_forward</span>
            </button>
          </form>

          {/* Floating Predictive Dropdown Card (only rendered when user has typed something) */}
          {isOpen && searchInput.trim().length > 0 && suggestions.length > 0 && (
            <div className="mt-3 w-full rounded-2xl bg-gradient-to-b from-blue-200/70 via-slate-200/50 to-orange-100/50 p-[1px] shadow-[0_25px_50px_-12px_rgba(0,0,0,0.12),0_8px_24px_rgba(0,0,0,0.04)] animate-in fade-in-50 duration-200">
              <div className="w-full divide-y divide-slate-100 overflow-hidden rounded-[15px] bg-white text-left">
                {suggestions.map((item, index) => {
                  const isSelected = index === selectedIndex
                  return (
                    <button
                      key={`${item.title}-${index}`}
                      type="button"
                      onClick={() => handleSelectRole(item.title)}
                      className={`group flex w-full items-center justify-between p-4 sm:px-6 text-left transition-colors cursor-pointer ${
                        isSelected ? 'bg-blue-50/70' : 'hover:bg-slate-50/80'
                      }`}
                    >
                      <div className="min-w-0 pr-4">
                        <div className="flex items-center gap-2">
                          <span className="truncate text-[15px] font-semibold text-[#1A2B3C] transition-colors group-hover:text-[#1A73E8] sm:text-base">
                            {item.title}
                          </span>
                        </div>
                        <div className="mt-0.5 flex items-center gap-1.5 text-xs text-slate-500 sm:text-sm">
                          <span>{item.location}</span>
                          <span>•</span>
                          <span className="font-medium text-slate-700">
                            {item.openingsCount} openings
                          </span>
                        </div>
                      </div>

                      <div className="ml-3 flex shrink-0 items-center gap-2">
                        <span className="inline-flex items-center rounded-full border border-orange-200/60 bg-orange-50 px-2.5 py-1 text-xs font-bold text-[#FF6B00] shadow-xs">
                          {item.matchPercentage}% match
                        </span>
                        <span
                          className={`material-symbols-outlined text-[18px] transition-all ${
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
                <div className="flex items-center justify-between bg-slate-50/60 px-6 py-3 text-xs text-slate-500">
                  <button
                    type="button"
                    onClick={() => executeSearch(searchInput)}
                    className="flex items-center gap-1.5 transition-colors hover:text-slate-800 cursor-pointer"
                  >
                    <span className="inline-block rounded border border-slate-200 bg-white px-1.5 py-0.5 font-mono text-[11px] text-slate-600 shadow-2xs">
                      Enter ↵
                    </span>
                    <span>to view all {totalPositionsFormatted}+ live positions</span>
                  </button>

                  <button
                    type="button"
                    onClick={() => navigate('/jobs')}
                    className="cursor-pointer text-[11px] font-medium text-blue-600 hover:underline"
                  >
                    Explore advanced filters
                  </button>
                </div>
              </div>
            </div>
          )}

          {/* Secondary Action Directly Below Search Area */}
          <div className="mt-7">
            <button
              type="button"
              onClick={handleCvClick}
              className="group inline-flex items-center gap-2 rounded-full border border-transparent px-4 py-2 text-sm font-medium text-slate-700 transition-all hover:border-slate-200 hover:bg-white hover:text-[#1A2B3C] hover:shadow-sm cursor-pointer"
            >
              <span className="text-base text-[#FF6B00] transition-transform group-hover:scale-110">
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
