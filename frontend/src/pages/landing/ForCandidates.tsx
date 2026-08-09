import { useEffect, useRef } from 'react'
import type { CSSProperties } from 'react'

/**
 * ★ MARQUEE SPEED KNOB ★
 * Seconds for one full loop of the feed. Lower = faster, higher = slower.
 * The loop is driven by JavaScript (requestAnimationFrame), so it always
 * runs — it can't be dropped by a browser like a CSS animation can.
 *   • 15 = fast (current)
 *   • 30 = smooth, clearly moving
 *   • 45 = gentle glide
 */
const MARQUEE_DURATION_S = 15

/**
 * ★ RIGHT SIDE LAYOUT KNOBS ★
 *   • RIGHT_COLUMN_SPAN = how many of the 12 grid columns the image feed
 *                         takes (1–11). Lower = more room for the left text.
 *                         7 = current.
 *   • CARD_WIDTH_PX     = width of each image card in px, capped at the
 *                         column width. Lower = narrower cards with gaps.
 *                         Experiment to balance the two sides.
 *   • FEED_WIDTH_PX     = width of the feed container (where the photos
 *                         land). 0 = fill the grid column; a number caps
 *                         it and centers it (e.g. 480). To go wider than
 *                         the column, raise RIGHT_COLUMN_SPAN first.
 *   • FEED_MIN_HEIGHT_PX = the feed's base height. The feed always fills
 *                         the WHOLE section — top border to bottom border,
 *                         edge to edge — and it can never be shorter than
 *                         this. The section height never grows to fit it
 *                         (the text column's own padding sets the section
 *                         height), so the text and button never move.
 *                         800 = current.
 */
const RIGHT_COLUMN_SPAN = 6
const CARD_WIDTH_PX = 250
const FEED_WIDTH_PX = 0
const FEED_MIN_HEIGHT_PX = 800

/**
 * ★ HEADLINE SPACING KNOB ★
 * Line height of the "More than Jobs / Build a career that / moves with
 * you" heading. Lower = the lines sit closer together.
 *   • 1.1 = current spacing
 *   • 1.0 = tighter
 *   • 0.9 = much tighter
 */
const HEADLINE_LINE_HEIGHT = 1.0

interface CollageCard {
  image: string
  label: string
  icon: string
  /** Card height — varied for a masonry feel. Must repeat with its
   *  duplicate so each loop set is identical and the loop stays seamless. */
  height: string
}

const collageCards: CollageCard[] = [
  {
    image:
      'https://lh3.googleusercontent.com/aida-public/AB6AXuA6cROpz1o8s6ux3yy913qB0HGSW6hSpbnKElDCykJu53XxOFXoiLo9mK06svDF0925UrWVU6u0XYfq-LY06Q-BqmUIqJAWz-RiOqhdsNToabGYMGyeWV96tdqRCGBlocofx1Uv1H2KPVKaX9bLqiVuG8iyY9rpfmi2uwrHdgzVZgPtPl7dzt9Zpbq-NHOvXfVs7F7NVZStLbviXVtE9gt9seHURhCLsPyEWfQ-c6rt9tSbabbzID1dJw',
    label: 'Creative Designer',
    icon: 'flag',
    height: 'h-80',
  },
  {
    image:
      'https://lh3.googleusercontent.com/aida-public/AB6AXuDt2eSQTMCvhF6QmDiUOEGCD_uiJJywHp7Cx0Aq7VJ5GN7AgPtKIPCHs8IBeW4_MLW3MuO8n42VU-rHJpntlPtYrURx6SsK0zkY9NxP1jxkWsQvwHzWVKs2AWbC2QNLaWjfj9psvovutlsvpk5cUo5qUEdjFXGTAk76nOVkYpn7n7o-AiYH8SFE3r4QESDkibzIgP61a_AYh8cBBb5hrMxVUDDL_Rxt0B9uIAhak0-a-Pz0XH0Bs2NV9Q',
    label: 'Business Professional',
    icon: 'briefcase',
    height: 'h-72',
  },
  {
    image:
      'https://lh3.googleusercontent.com/aida-public/AB6AXuCXCekrDO_livfnNEIeMukAzy02knkChsJOdbrGKqZCuwA9dZKHuYkkkwo8aVMW-sjNAylSMZquuvwK4CWXl6otzIkxs9QyiPwzyItaKqtFT2wsAzWlA8JzcVUzLT3yg1OSFl-sTJp-l6ZgFCwzUkyb1r7-XYHIhw5ODrktKG3LaBSxsc-wyQiM77ux61rOyBcXtJbnjy4UVSh0PtdyxZCaorKoV5xeHtKfh5i0vFDbp9TFwaMtUCWl5Q',
    label: 'Community Manager',
    icon: 'public',
    height: 'h-80',
  },
  {
    image:
      'https://lh3.googleusercontent.com/aida-public/AB6AXuDPfegEfR-pN8nEq_y4shCC7Mps3myWkCC9TDIlBFPTV-3IYAvNH5B4a2bN9B5faDOXpF8JPYIGIOV1sf75Cuj-bGK6uwy0-eg8o9SkP4lvTZLiBQEwGFTcKn5R39oLWCjdj3ziCa2ml8MkYlYpqc0da1CHVxOxCXv0EoPxRLuyYatclt9YMd_SId-O9ds2JNauEMlVYsZ8wWipcyunL62KHGTcQJtZDW7gyWffD4ifgOqioj2gupzYdg',
    label: 'Healthcare Professional',
    icon: 'health_and_safety',
    height: 'h-72',
  },
]

/** 2×2 feed: left column scrolls UP, right column scrolls DOWN. */
const leftColumn = collageCards.slice(0, 2)
const rightColumn = collageCards.slice(2, 4)

function collageCardMarkup(card: CollageCard, index: number) {
  return (
    <div
      key={index}
      tabIndex={0}
      aria-label={card.label}
      className={`relative mb-4 shrink-0 overflow-hidden rounded-xl border border-surface-variant bg-surface-container-low transition-transform duration-300 hover:-translate-y-1 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary ${card.height}`}
      style={{ width: `min(${CARD_WIDTH_PX}px, 100%)` }}
    >
      <img
        className="h-full w-full object-cover"
        src={card.image}
        alt=""
        loading="lazy"
      />
      <div className="absolute bottom-3 left-3 right-3 flex items-center justify-between rounded-full border border-surface-variant bg-surface-container-lowest/80 px-4 py-2 backdrop-blur-md">
        <span className="flex items-center gap-2">
          <span className="h-2 w-2 rounded-full bg-green-500" />
          <span className="font-label-sm text-label-sm text-on-surface">
            {card.label}
          </span>
        </span>
        <span className="material-symbols-outlined text-lg text-on-surface">
          {card.icon}
        </span>
      </div>
    </div>
  )
}

export default function ForCandidates() {
  const leftTrackRef = useRef<HTMLDivElement>(null)
  const rightTrackRef = useRef<HTMLDivElement>(null)

  // JS-driven marquee: updates the track transforms every frame. Left
  // column scrolls UP, right column scrolls DOWN, both looping seamlessly
  // (the content is duplicated, so wrapping at half the track height is
  // invisible). Always runs — no CSS animation to break.
  useEffect(() => {
    const left = leftTrackRef.current
    const right = rightTrackRef.current
    if (!left || !right) return

    let offset = 0
    let last = performance.now()
    let raf = 0

    // One set of cards = half the track (the track holds two copies).
    const measure = () => {
      left.dataset.setH = String(left.scrollHeight / 2)
      right.dataset.setH = String(right.scrollHeight / 2)
    }

    const tick = (now: number) => {
      const dt = (now - last) / 1000
      last = now

      const leftH = Number(left.dataset.setH) || 0
      const rightH = Number(right.dataset.setH) || 0
      if (leftH > 0 && rightH > 0) {
        // Advance the shared loop offset, wrapping at one set height.
        offset = (offset + (leftH / MARQUEE_DURATION_S) * dt) % leftH
        left.style.transform = `translateY(${-offset}px)`
        // Right column mirrors the loop but plays downward.
        right.style.transform = `translateY(${-rightH + offset}px)`
      }
      raf = requestAnimationFrame(tick)
    }

    measure()
    window.addEventListener('resize', measure)
    raf = requestAnimationFrame(tick)
    return () => {
      cancelAnimationFrame(raf)
      window.removeEventListener('resize', measure)
    }
  }, [])

  return (
    <section className="bg-surface">
      <div className="mx-auto w-full max-w-container-max px-margin-mobile md:px-margin-desktop">
        <div
          className="candidates-grid grid grid-cols-1 items-center gap-gutter lg:grid-cols-12"
          style={
            {
              '--left-span': 12 - RIGHT_COLUMN_SPAN,
              '--right-span': RIGHT_COLUMN_SPAN,
            } as CSSProperties
          }
        >
          {/* Text content */}
          <div className="candidates-left z-10 flex flex-col gap-8 py-24 md:py-32">
            <div className="inline-flex w-fit items-center gap-2 rounded-full bg-surface-container-low px-4 py-2">
              <span className="h-2 w-2 rounded-full bg-primary" />
              <span className="font-label-md text-label-md text-on-surface">
                All in one HR platform for growing teams
              </span>
            </div>
            <h2
              className="font-display-lg-mobile text-display-lg-mobile text-on-surface md:font-display-lg md:text-display-lg"
              style={{ lineHeight: HEADLINE_LINE_HEIGHT }}
            >
              More than Jobs
              <br />
              <span className="text-accent">Build a career</span> that
              <br />
              moves with you
            </h2>
            <p className="max-w-lg font-body-lg text-body-lg text-on-surface-variant">
              Create a dynamic professional profile, discover meaningful
              opportunities, and access tools that help you grow, learn, and
              thrive in the modern workforce.
            </p>
            <div className="pt-4">
              <button
                type="button"
                className="rounded bg-accent px-8 py-4 font-label-md text-label-md text-on-accent shadow-sm transition-all hover:opacity-90 active:scale-95"
              >
                Start Applying for jobs
              </button>
            </div>
          </div>

          {/* Right: 2×2 infinite feed — left column scrolls UP, right DOWN */}
          <div
            className="candidates-right relative mx-auto overflow-hidden rounded-xl"
            style={
              {
                width:
                  FEED_WIDTH_PX > 0 ? `min(${FEED_WIDTH_PX}px, 100%)` : '100%',
                minHeight: FEED_MIN_HEIGHT_PX,
                alignSelf: 'stretch',
              } as CSSProperties
            }
          >
            <div className="absolute inset-0 grid grid-cols-2 gap-4">
              {/* Left column — moves up */}
              <div className="relative overflow-hidden">
                <div
                  ref={leftTrackRef}
                  className="flex flex-col items-center will-change-transform"
                >
                  {[...leftColumn, ...leftColumn].map(collageCardMarkup)}
                </div>
              </div>
              {/* Right column — moves down */}
              <div className="relative overflow-hidden">
                <div
                  ref={rightTrackRef}
                  className="flex flex-col items-center will-change-transform"
                >
                  {[...rightColumn, ...rightColumn].map(collageCardMarkup)}
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </section>
  )
}
