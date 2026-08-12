import { useEffect, useRef, useState } from 'react'
import { cn } from '../../../lib/cn.ts'
import afriworkLogo from '../../../assets/logos/afriwork.png'
import ethiojobsLogo from '../../../assets/logos/ethiojobs.png'
import ethioreporterjobsLogo from '../../../assets/logos/ethioreporterjobs.png'
import geezjobsLogo from '../../../assets/logos/geezjobs.png'
import hahuLogo from '../../../assets/logos/hahu.png'

interface HubNode {
  name: string
  logo: string
  className: string
  logoSize: number
}

interface HubLine {
  /** Static cable: hub center → node center. */
  d: string
  /** Energy beam path: node center → hub center (reversed). */
  reverseD: string
  width: number
}

/**
 * ★ LOGO SIZE KNOB ★
 * Change this ONE number to resize all 9 floating logos at once.
 *   • 1    = current size
 *   • 1.25 = 25% bigger
 *   • 1.5  = 50% bigger
 *   • 0.75 = 25% smaller
 */
const LOGO_SIZE_SCALE = 3.5

/**
 * ★ SEARCH BAR KNOBS ★
 * Tweak these three numbers to control the search bar under the SeraGo title.
 *   • SEARCH_BAR_WIDTH      = total bar width in px
 *   • SEARCH_BAR_HEIGHT     = total bar height in px (icon + icon size follow it)
 *   • SEARCH_BAR_MARGIN_TOP = gap between the SeraGo title and the bar in px
 *
 * Note: the bar is DECORATIVE (not clickable) — the tagline types itself in.
 */
const SEARCH_BAR_WIDTH = 450
const SEARCH_BAR_HEIGHT = 50
const SEARCH_BAR_MARGIN_TOP = -50

/**
 * ★ TYPEWRITER KNOBS ★
 * The tagline types itself in whenever the hero becomes visible — on first
 * load and again every time you scroll down and back up to the hero.
 *   • TYPEWRITER_TEXT      = the phrase that gets typed
 *   • TYPE_SPEED_MS        = ms per character (lower = faster typing)
 *   • TYPE_START_DELAY_MS  = pause after the hero appears, before typing starts
 */
const TYPEWRITER_TEXT = 'Cant Get a Job You never Applied for'
const TYPE_SPEED_MS = 55
const TYPE_START_DELAY_MS = 600

/**
 * ★ HUB VERTICAL POSITION KNOB ★
 * Moves ONLY the SeraGo wordmark + search bar up/down, in px.
 * The 9 logo cards stay exactly where they are.
 *   • negative value = move UP (e.g. -40 = 40px higher)
 *   • positive value = move DOWN
 *   • the SVG cables auto-reattach to the hub wherever it sits
 */
const HUB_OFFSET_Y = -130

/**
 * ★ HERO LAYOUT KNOBS ★
 * The arc uses FIXED pixel positions — the exact spots it had in the
 * original 900px container — so the logos + wordmark look is preserved
 * no matter what; these knobs only control empty space.
 *   • HUB_CONTAINER_HEIGHT = height of the Pulse box in px (fits the arc)
 *   • HUB_SHIFT_Y          = how far the whole scene is lifted up, in px
 *                            (was the -translate-y-35 class = 140px); its
 *                            flow space is cancelled so no gap appears
 *   • HUB_BASE_TOP         = the wordmark's anchored top in px; the
 *                            HUB_OFFSET_Y knob below adds on top of it
 */
const HUB_CONTAINER_HEIGHT = 560
const HUB_SHIFT_Y = 140
const HUB_BASE_TOP = 324

/** Real brand logos added in src/assets/logos — duplicated to fill all 9 nodes. */
const brandLogos = [
  { name: 'Afriwork', logo: afriworkLogo },
  { name: 'EthioJobs', logo: ethiojobsLogo },
  { name: 'Ethio Reporter Jobs', logo: ethioreporterjobsLogo },
  { name: 'GeezJobs', logo: geezjobsLogo },
  { name: 'Hahu', logo: hahuLogo },
]

/** The 9 orbit positions (top half) around the central wordmark.
 *  Tops are FIXED px (the exact spots from the original 900px container)
 *  so the arc look never changes; the container is sized to fit them.
 *  Cards are wide pills; logoSize is the base logo width in px, actual
 *  rendered width = logoSize × LOGO_SIZE_SCALE (height follows the
 *  logo's natural aspect ratio). */
const nodePositions = [
  { className: 'top-[90px] left-[10%] w-36 h-18', logoSize: 24 },
  { className: 'top-[45px] left-[30%] w-32 h-16', logoSize: 20 },
  {
    className: 'top-[72px] left-[50%] -translate-x-1/2 w-32 h-16',
    logoSize: 20,
  },
  { className: 'top-[45px] right-[30%] w-40 h-20', logoSize: 28 },
  { className: 'top-[90px] right-[10%] w-32 h-16', logoSize: 20 },
  { className: 'top-[270px] left-[5%] w-36 h-18', logoSize: 24 },
  { className: 'top-[270px] right-[5%] w-32 h-16', logoSize: 20 },
  { className: 'top-[450px] left-[5%] w-44 h-24', logoSize: 32 },
  { className: 'top-[450px] right-[5%] w-32 h-16', logoSize: 20 },
]

/** 9 nodes — the top half of the orbit around the hub. */
const nodes: HubNode[] = nodePositions.map((position, i) => {
  const brand = brandLogos[i % brandLogos.length]
  return { ...brand, ...position }
})

export default function Hero() {
  const containerRef = useRef<HTMLDivElement>(null)
  const hubRef = useRef<HTMLDivElement>(null)
  const [lines, setLines] = useState<HubLine[]>([])
  const [typedChars, setTypedChars] = useState(0)

  // Typewriter: types the tagline when the hero scrolls into view, resets
  // and replays every time you scroll away and back.
  useEffect(() => {
    const container = containerRef.current
    if (!container) return

    const prefersReducedMotion = window.matchMedia(
      '(prefers-reduced-motion: reduce)',
    ).matches

    let delayTimeout: ReturnType<typeof setTimeout> | undefined
    let typeInterval: ReturnType<typeof setInterval> | undefined

    const stopTyping = () => {
      if (delayTimeout) clearTimeout(delayTimeout)
      if (typeInterval) clearInterval(typeInterval)
    }

    const startTyping = () => {
      stopTyping()
      if (prefersReducedMotion) {
        setTypedChars(TYPEWRITER_TEXT.length)
        return
      }
      setTypedChars(0)
      delayTimeout = setTimeout(() => {
        typeInterval = setInterval(() => {
          setTypedChars((prev) => {
            if (prev >= TYPEWRITER_TEXT.length) {
              if (typeInterval) clearInterval(typeInterval)
              return prev
            }
            return prev + 1
          })
        }, TYPE_SPEED_MS)
      }, TYPE_START_DELAY_MS)
    }

    const observer = new IntersectionObserver(
      (entries) => {
        entries.forEach((entry) => {
          if (entry.isIntersecting) startTyping()
          else {
            stopTyping()
            setTypedChars(0)
          }
        })
      },
      { threshold: 0.3 },
    )

    observer.observe(container)
    return () => {
      observer.disconnect()
      stopTyping()
    }
  }, [])

  useEffect(() => {
    const container = containerRef.current
    const hub = hubRef.current
    if (!container || !hub) return

    const drawLines = () => {
      const nodeEls = container.querySelectorAll<HTMLElement>(
        '.hub-node[data-node="true"]',
      )
      const containerRect = container.getBoundingClientRect()
      const hubRect = hub.getBoundingClientRect()

      const hubCenterX = hubRect.left - containerRect.left + hubRect.width / 2
      const hubCenterY = hubRect.top - containerRect.top + hubRect.height / 2

      const next: HubLine[] = []
      nodeEls.forEach((node, i) => {
        const nodeRect = node.getBoundingClientRect()
        const nodeCenterX =
          nodeRect.left - containerRect.left + nodeRect.width / 2
        const nodeCenterY =
          nodeRect.top - containerRect.top + nodeRect.height / 2

        // Structured "net" pattern: varied line thicknesses.
        const width = i % 4 === 0 ? 3 : i % 2 === 0 ? 1 : 2
        next.push({
          d: `M ${hubCenterX} ${hubCenterY} L ${nodeCenterX} ${nodeCenterY}`,
          reverseD: `M ${nodeCenterX} ${nodeCenterY} L ${hubCenterX} ${hubCenterY}`,
          width,
        })
      })
      setLines(next)
    }

    drawLines()
    window.addEventListener('resize', drawLines)
    return () => window.removeEventListener('resize', drawLines)
  }, [])

  return (
    <section className="relative flex items-center justify-center overflow-hidden px-margin-mobile pb-10 pt-16 md:px-margin-desktop md:pb-16 md:pt-32">
      {/* The Pulse: central hub + surrounding nodes */}
      <div
        ref={containerRef}
        id="data-hub-container"
        className="relative w-full max-w-[1400px]"
        style={{
          height: HUB_CONTAINER_HEIGHT,
          transform: `translateY(-${HUB_SHIFT_Y}px)`,
          // Cancel the lift's flow space so no gap appears below the arc.
          marginBottom: -HUB_SHIFT_Y,
        }}
      >
        {/* Central hub wordmark */}
        <div
          ref={hubRef}
          id="central-hub"
          className="hub-node absolute left-1/2 z-20 flex -translate-x-1/2 flex-col items-center rounded-3xl bg-surface-container-lowest/90 px-8 py-4 font-display-lg text-[100px] font-black tracking-tighter text-primary md:text-[200px]"
          style={{
            top: HUB_BASE_TOP,
            transform: `translateY(${HUB_OFFSET_Y}px)`,
          }}
        >
          {/* One inline wrapper keeps Sera + Go on a single line — the
              flex-col parent would otherwise stack them as two items */}
          <span>
            Sera<span className="text-accent">Go</span>
          </span>
          {/* Decorative search bar — NOT clickable; the tagline types itself in */}
          <div
            aria-label={TYPEWRITER_TEXT}
            className="pointer-events-none flex select-none items-center rounded-full border border-outline-variant bg-surface-container-lowest shadow-sm"
            style={{
              width: `min(${SEARCH_BAR_WIDTH}px, 85vw)`,
              height: SEARCH_BAR_HEIGHT,
              marginTop: SEARCH_BAR_MARGIN_TOP,
            }}
          >
            {/* flex items-center → the typed text + caret sit dead-center vertically */}
            <span className="flex h-full w-full min-w-0 items-center whitespace-nowrap pl-6 pr-2 font-headline-md text-[16px] font-normal leading-none tracking-normal text-on-surface">
              {TYPEWRITER_TEXT.slice(0, typedChars)}
              {/* Blinking caret — fades out once typing is done */}
              <span
                aria-hidden="true"
                className={cn(
                  'typewriter-caret ml-0.5 inline-block h-[1em] w-[2px] bg-secondary',
                  typedChars >= TYPEWRITER_TEXT.length &&
                    'typewriter-caret--hidden',
                )}
              />
            </span>
            <span
              aria-hidden="true"
              className="mr-2 flex shrink-0 items-center justify-center rounded-full bg-primary text-on-primary"
              style={{
                width: SEARCH_BAR_HEIGHT - 16,
                height: SEARCH_BAR_HEIGHT - 16,
              }}
            >
              <span
                className="material-symbols-outlined"
                style={{ fontSize: Math.round(SEARCH_BAR_HEIGHT * 0.38) }}
              >
                search
              </span>
            </span>
          </div>
        </div>

        {/* Surrounding nodes — real brand logos */}
        {nodes.map((node, i) => (
          <div
            key={`${node.name}-${i}`}
            data-node="true"
            className={cn(
              'hub-node absolute z-20 flex items-center justify-center rounded-full border border-surface-container-highest bg-surface-container-lowest shadow-md',
              node.className,
            )}
          >
            <img
              src={node.logo}
              alt={node.name}
              style={{
                // Size by WIDTH only — height follows the logo's natural
                // aspect ratio, so wide brand logos render wide instead of
                // being squished into a square.
                width: node.logoSize * LOGO_SIZE_SCALE,
                height: 'auto',
              }}
              draggable={false}
            />
          </div>
        ))}

        {/* Connection lines */}
        <svg
          className="pointer-events-none absolute inset-0 z-10 h-full w-full"
          aria-hidden="true"
        >
          {lines.map((line, i) => (
            <path
              key={i}
              d={line.d}
              stroke="#E0E0E0"
              strokeWidth={line.width}
              fill="none"
              opacity="0.7"
            />
          ))}

          {/* Energy beams: a small dot travels from each logo card
              along its cable into SeraGo, staggered for a constant flow. */}
          {lines.map((line, i) => {
            const dur = 2.4 + (i % 4) * 0.35
            const delay = i * 0.2
            return (
              <circle
                key={`beam-${i}`}
                r="3.5"
                fill="#1a73e8"
                style={{
                  filter: 'drop-shadow(0 0 4px rgba(26, 115, 232, 0.55))',
                }}
              >
                <animateMotion
                  dur={`${dur}s`}
                  begin={`${delay}s`}
                  repeatCount="indefinite"
                  path={line.reverseD}
                />
                <animate
                  attributeName="opacity"
                  values="0;1;1;0"
                  keyTimes="0;0.05;0.88;1"
                  dur={`${dur}s`}
                  begin={`${delay}s`}
                  repeatCount="indefinite"
                />
              </circle>
            )
          })}
        </svg>
      </div>
    </section>
  )
}
