import { useEffect, useRef } from 'react'
import type { CSSProperties } from 'react'

/**
 * ★ MARQUEE SPEED KNOB ★
 * Pixels the cards travel per second — set to match the For Candidates
 * feed's visible speed (that feed moves a ~624px set every 15s ≈ 42 px/s),
 * so both sections glide at the same pace even though the employer rows
 * are wider. The loop is driven by JavaScript (requestAnimationFrame), so
 * it always runs — it can't be dropped by a browser like a CSS animation.
 *   • 42 = same as For Candidates (current)
 *   • 30 = slower
 *   • 60 = faster
 * Speed is per-pixel, so widening the cards (CARD_WIDTH_PX) never changes
 * how fast they appear to move.
 */
const MARQUEE_PX_PER_SEC = 42

/**
 * ★ IMAGE FEED LAYOUT KNOBS ★
 *   • IMAGE_COLUMN_SPAN = how many of the 12 grid columns the image feed
 *                         takes (1–11). Lower = more room for the right text.
 *                         6 = current.
 *   • CARD_WIDTH_PX     = width of each photo card in px. Cards keep this
 *                         width while rows scroll horizontally. Wider =
 *                         bigger photos (speed stays the same).
 *   • FEED_WIDTH_PX     = width of the feed container (where the photos
 *                         land). 0 = fill the grid column; a number caps
 *                         it and centers it (e.g. 480). To go wider than
 *                         the column, raise IMAGE_COLUMN_SPAN first.
 *   • FEED_MIN_HEIGHT_PX = the feed's base height. The feed always fills
 *                         the WHOLE section — top border to bottom border,
 *                         edge to edge — and it can never be shorter than
 *                         this. The section height never grows to fit it,
 *                         so the text and button never move.
 *   • LEFT BLEED: the feed always reaches the page's LEFT edge (no gap) —
 *                 the text is on the right, so it can only overflow
 *                 leftward. Set FEED_WIDTH_PX to a number to switch to a
 *                 plain capped width (no bleed).
 */
const IMAGE_COLUMN_SPAN = 6
const CARD_WIDTH_PX = 300
const FEED_WIDTH_PX = 0
const FEED_MIN_HEIGHT_PX = 600

/**
 * ★ HEADLINE SPACING KNOB ★
 * Line height of the "More than hires / Build a team that / grows with you"
 * heading. Lower = the lines sit closer together.
 *   • 1.1 = current spacing
 *   • 1.0 = tighter
 *   • 0.9 = much tighter
 */
const HEADLINE_LINE_HEIGHT = 1.0

interface CollageCard {
  image: string
  label: string
  icon: string
}

const collageCards: CollageCard[] = [
  {
    image:
      'https://lh3.googleusercontent.com/aida-public/AB6AXuA6cROpz1o8s6ux3yy913qB0HGSW6hSpbnKElDCykJu53XxOFXoiLo9mK06svDF0925UrWVU6u0XYfq-LY06Q-BqmUIqJAWz-RiOqhdsNToabGYMGyeWV96tdqRCGBlocofx1Uv1H2KPVKaX9bLqiVuG8iyY9rpfmi2uwrHdgzVZgPtPl7dzt9Zpbq-NHOvXfVs7F7NVZStLbviXVtE9gt9seHURhCLsPyEWfQ-c6rt9tSbabbzID1dJw',
    label: 'Creative Designer',
    icon: 'flag',
  },
  {
    image:
      'https://lh3.googleusercontent.com/aida-public/AB6AXuDt2eSQTMCvhF6QmDiUOEGCD_uiJJywHp7Cx0Aq7VJ5GN7AgPtKIPCHs8IBeW4_MLW3MuO8n42VU-rHJpntlPtYrURx6SsK0zkY9NxP1jxkWsQvwHzWVKs2AWbC2QNLaWjfj9psvovutlsvpk5cUo5qUEdjFXGTAk76nOVkYpn7n7o-AiYH8SFE3r4QESDkibzIgP61a_AYh8cBBb5hrMxVUDDL_Rxt0B9uIAhak0-a-Pz0XH0Bs2NV9Q',
    label: 'Business Professional',
    icon: 'briefcase',
  },
  {
    image:
      'https://lh3.googleusercontent.com/aida-public/AB6AXuCXCekrDO_livfnNEIeMukAzy02knkChsJOdbrGKqZCuwA9dZKHuYkkkwo8aVMW-sjNAylSMZquuvwK4CWXl6otzIkxs9QyiPwzyItaKqtFT2wsAzWlA8JzcVUzLT3yg1OSFl-sTJp-l6ZgFCwzUkyb1r7-XYHIhw5ODrktKG3LaBSxsc-wyQiM77ux61rOyBcXtJbnjy4UVSh0PtdyxZCaorKoV5xeHtKfh5i0vFDbp9TFwaMtUCWl5Q',
    label: 'Community Manager',
    icon: 'public',
  },
  {
    image:
      'https://lh3.googleusercontent.com/aida-public/AB6AXuDPfegEfR-pN8nEq_y4shCC7Mps3myWkCC9TDIlBFPTV-3IYAvNH5B4a2bN9B5faDOXpF8JPYIGIOV1sf75Cuj-bGK6uwy0-eg8o9SkP4lvTZLiBQEwGFTcKn5R39oLWCjdj3ziCa2ml8MkYlYpqc0da1CHVxOxCXv0EoPxRLuyYatclt9YMd_SId-O9ds2JNauEMlVYsZ8wWipcyunL62KHGTcQJtZDW7gyWffD4ifgOqioj2gupzYdg',
    label: 'Healthcare Professional',
    icon: 'health_and_safety',
  },
]

/** 3 horizontal marquee rows. Each row is a different rotation of the four
 *  cards, and alternates scroll direction (row 2 plays in reverse) for a
 *  unique triple-marquee look. Add/remove rows by editing this array. */
const rowSets: CollageCard[][] = [
  [collageCards[0], collageCards[1], collageCards[2], collageCards[3]],
  [collageCards[3], collageCards[2], collageCards[1], collageCards[0]],
  [collageCards[2], collageCards[0], collageCards[3], collageCards[1]],
]

function CollageCardView({ card }: { card: CollageCard }) {
  return (
    <div
      tabIndex={0}
      aria-label={card.label}
      className="relative h-full shrink-0 overflow-hidden rounded-xl border border-surface-variant bg-surface-container-low transition-transform duration-300 hover:-translate-y-1 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary"
      style={{ width: CARD_WIDTH_PX }}
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

/** One horizontal marquee row. The track holds two copies of the row's
 *  cards; the JS loop slides it sideways forever and wraps at exactly one
 *  set's width, so the loop is seamless. `reverse` plays it rightward. */
function MarqueeRow({
  cards,
  reverse,
}: {
  cards: CollageCard[]
  reverse: boolean
}) {
  const trackRef = useRef<HTMLDivElement>(null)

  useEffect(() => {
    const track = trackRef.current
    if (!track) return

    let offset = 0
    let last = performance.now()
    let raf = 0

    // One set's width = the offset of the first duplicated card, which is
    // exactly the seam where copy 2 starts (more precise than scrollWidth/2
    // because of the gaps between cards).
    const measure = () => {
      const firstDuplicate = track.children[cards.length] as
        HTMLElement | undefined
      track.dataset.setW = String(
        firstDuplicate ? firstDuplicate.offsetLeft : track.scrollWidth / 2,
      )
    }

    const tick = (now: number) => {
      const dt = (now - last) / 1000
      last = now

      const setW = Number(track.dataset.setW) || 0
      if (setW > 0) {
        offset = (offset + MARQUEE_PX_PER_SEC * dt) % setW
        // Forward rows slide left; reverse rows slide right (same pattern
        // the vertical candidates feed used, but on the X axis).
        track.style.transform = reverse
          ? `translateX(${-setW + offset}px)`
          : `translateX(${-offset}px)`
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
  }, [cards.length, reverse])

  return (
    <div className="relative flex-1 overflow-hidden">
      <div
        ref={trackRef}
        className="flex h-full w-max items-center gap-4 will-change-transform"
      >
        {[...cards, ...cards].map((card, i) => (
          <CollageCardView key={i} card={card} />
        ))}
      </div>
    </div>
  )
}

export default function ForEmployers() {
  return (
    <section className="overflow-x-clip bg-surface">
      <div className="mx-auto w-full max-w-container-max px-margin-mobile md:px-margin-desktop">
        <div
          className="employers-grid grid grid-cols-1 items-center gap-gutter lg:grid-cols-12"
          style={
            {
              '--feed-span': IMAGE_COLUMN_SPAN,
              '--text-span': 12 - IMAGE_COLUMN_SPAN,
            } as CSSProperties
          }
        >
          {/* Text content — rendered right of the feed on desktop, first on mobile */}
          <div className="employers-text z-10 flex flex-col gap-8 py-24 md:py-32">
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
              More than hires
              <br />
              <span className="text-accent">Build a team</span> that
              <br />
              grows with you
            </h2>
            <p className="max-w-lg font-body-lg text-body-lg text-on-surface-variant">
              Post a role once and reach qualified candidates across every job
              site — with AI helping you shortlist, interview, and hire the
              right fit faster.
            </p>
            <div className="pt-4">
              <button
                type="button"
                className="rounded bg-accent px-8 py-4 font-label-md text-label-md text-on-accent shadow-sm transition-all hover:opacity-90 active:scale-95"
              >
                Start Hiring
              </button>
            </div>
          </div>

          {/* Left: 3-row sideways marquee — rows 1 & 3 slide left, row 2 right */}
          <div
            className={`employers-feed relative mx-auto overflow-hidden rounded-xl${FEED_WIDTH_PX === 0 ? ' employers-feed-bleed' : ''}`}
            style={
              {
                ...(FEED_WIDTH_PX > 0
                  ? { width: `min(${FEED_WIDTH_PX}px, 100%)` }
                  : {}),
                minHeight: FEED_MIN_HEIGHT_PX,
                alignSelf: 'stretch',
              } as CSSProperties
            }
          >
            <div className="absolute inset-0 flex flex-col gap-4">
              {rowSets.map((cards, i) => (
                <MarqueeRow key={i} cards={cards} reverse={i % 2 === 1} />
              ))}
            </div>
          </div>
        </div>
      </div>
    </section>
  )
}
