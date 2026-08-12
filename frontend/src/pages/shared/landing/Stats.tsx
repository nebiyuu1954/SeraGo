import { useEffect, useRef, useState } from 'react'

/**
 * ★ COUNT-UP KNOBS ★
 *   • COUNT_UP_MS    = how long the numbers take to count from 0 to their
 *                     final value (milliseconds). Lower = snappier, higher
 *                     = more dramatic. 3000 = current.
 *   • STAGGER_MS     = extra delay per stat so they count up one after
 *                     another (left → right). 0 = all at once.
 *   • GLOW_INTENSITY = strength of the soft glow around the numbers while
 *                     they're counting (0 = none, 1 = strongest).
 */
const COUNT_UP_MS = 3000
const STAGGER_MS = 150
const GLOW_INTENSITY = 0.35

/**
 * ★ HEADLINE & NUMBER KNOBS ★
 *   • HEADLINE_LINE_HEIGHT = line spacing of the headline (1.1 = current).
 *   • HEADLINE_GAP_PX      = space between the headline and the stats in
 *                            px (was 64). Lower = stats sit closer below.
 *   • STAT_NUMBER_SIZE_PX  = font size of the big stat numbers in px
 *                            (was 40). Lower = smaller, higher = bigger.
 */
const HEADLINE_LINE_HEIGHT = 1.1
const HEADLINE_GAP_PX = 40
const STAT_NUMBER_SIZE_PX = 48

interface Stat {
  /** Final number shown (counts up from 0 when the section scrolls into view). */
  value: number
  label: string
}

const stats: Stat[] = [
  { value: 15, label: 'Websites on SeraGo' },
  { value: 170, label: 'Jobs per day' },
  { value: 10000, label: 'Applicants per day' },
  { value: 100000, label: 'Overall users' },
]

/** Counts a number from 0 → `value` with a comma separator while `started`. */
function StatCell({
  value,
  label,
  started,
  delay,
}: {
  value: number
  label: string
  started: boolean
  delay: number
}) {
  const [display, setDisplay] = useState(0)

  // Soft glow while the number is still counting up; fades out when done.
  const glowing = started && display < value

  useEffect(() => {
    if (!started) return
    let raf = 0
    let start: number | null = null

    const tick = (now: number) => {
      if (start === null) start = now
      // Ease-out cubic: fast start, slow finish.
      const t = Math.min((now - start) / COUNT_UP_MS, 1)
      setDisplay(Math.round(value * (1 - Math.pow(1 - t, 3))))
      if (t < 1) raf = requestAnimationFrame(tick)
    }

    const timeout = window.setTimeout(() => {
      raf = requestAnimationFrame(tick)
    }, delay)
    return () => {
      window.clearTimeout(timeout)
      cancelAnimationFrame(raf)
    }
  }, [started, value, delay])

  return (
    <div className="flex flex-col items-center gap-3 text-center">
      <p
        className="font-display-lg-mobile text-display-lg-mobile font-bold tracking-tight text-on-surface"
        style={{
          fontSize: STAT_NUMBER_SIZE_PX,
          textShadow: glowing
            ? `0 0 12px rgba(26, 115, 232, ${GLOW_INTENSITY}), 0 0 32px rgba(26, 115, 232, ${GLOW_INTENSITY * 0.5})`
            : 'none',
          transition: 'text-shadow 0.8s ease',
        }}
      >
        {display.toLocaleString('en-US')}
        <span className="text-secondary">+</span>
      </p>
      <p className="max-w-[14ch] font-label-md text-label-md text-secondary">
        {label}
      </p>
    </div>
  )
}

export default function Stats() {
  const sectionRef = useRef<HTMLElement>(null)
  const [inView, setInView] = useState(false)

  // Re-animate the count-up every time the section scrolls into view.
  // Leaving view resets the numbers to 0, so each visit counts fresh.
  useEffect(() => {
    const section = sectionRef.current
    if (!section) return

    const observer = new IntersectionObserver(
      (entries) => {
        setInView(entries[0]?.isIntersecting ?? false)
      },
      { threshold: 0.4 },
    )
    observer.observe(section)
    return () => observer.disconnect()
  }, [])

  return (
    <section ref={sectionRef} className="bg-surface">
      <div className="mx-auto w-full max-w-container-max px-margin-mobile pt-16 pb-24 md:px-margin-desktop md:pt-24 md:pb-32">
        {/* Centered headline */}
        <h2
          className="mx-auto max-w-3xl text-center font-display-lg-mobile text-display-lg-mobile text-on-surface md:font-display-lg md:text-display-lg"
          style={{
            lineHeight: HEADLINE_LINE_HEIGHT,
            marginBottom: HEADLINE_GAP_PX,
          }}
        >
          SeraGo has all the <span className="text-accent">jobs</span> in{' '}
          <span className="text-accent">one place</span>
        </h2>

        {/* 4 stats — count up from 0 when scrolled into view */}
        <div className="grid grid-cols-2 gap-x-6 gap-y-12 md:grid-cols-4">
          {stats.map((stat, i) => (
            <StatCell
              // Remounts on every visibility flip, so each visit to the
              // section starts the count from 0 again.
              key={`${stat.label}-${inView ? 'in' : 'out'}`}
              value={stat.value}
              label={stat.label}
              started={inView}
              delay={i * STAGGER_MS}
            />
          ))}
        </div>
      </div>
    </section>
  )
}
