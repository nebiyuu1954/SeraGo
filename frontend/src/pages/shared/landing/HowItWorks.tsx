import { useEffect, useState } from 'react'
import { cn } from '../../../lib/cn.ts'
import employer1 from '../../../assets/steps/employer1.png'
import employer2 from '../../../assets/steps/employer2.png'
import employer3 from '../../../assets/steps/employer3.png'
import applicant1 from '../../../assets/steps/applicant1.png'
import applicant2 from '../../../assets/steps/applicant2.png'
import applicant3 from '../../../assets/steps/applicant3.png'

/**
 * ★ LAYOUT KNOBS ★
 *   • SECTION_TOP_PAD_PX = space above the "How SeraGo Works" heading
 *                          (section top padding). Lower = section sits
 *                          higher on the page. 58 = current.
 *   • HEADING_MB_PX      = space between the heading and its subtitle.
 *                          24 = current.
 *   • SUBTITLE_MB_PX     = space between the subtitle and the toggle
 *                          buttons. 48 = current.
 *   • TOGGLE_MB_PX       = space between the toggle and the content below
 *                          it. 56 = current.
 */
const SECTION_TOP_PAD_PX = 58
const HEADING_MB_PX = 24
const SUBTITLE_MB_PX = 48
// Audience toggle is hidden for now (SeraGo is talent-only). Uncomment this
// together with the toggle block below to restore it.
// const TOGGLE_MB_PX = 56

/**
 * ★ MONITOR KNOBS ★
 * The screenshot is shown inside a monitor-style frame. These knobs scale
 * and move the WHOLE monitor (frame + image) together:
 *   • MONITOR_WIDTH_PX    = monitor max-width in px (capped by its column).
 *   • MONITOR_OFFSET_X_PX = move the monitor horizontally (positive =
 *                           right, negative = left). 0 = centered.
 *   • MONITOR_OFFSET_Y_PX = move the monitor vertically (positive = down,
 *                           negative = up). 0 = default.
 * Each mode still has its own image via `src` in the `content` object.
 */
const MONITOR_WIDTH_PX = 600
const MONITOR_OFFSET_X_PX = 0
const MONITOR_OFFSET_Y_PX = 0

/**
 * ★ STEP SLIDESHOW KNOB ★
 * STEP_INTERVAL_MS = how long (ms) each step image stays on screen before
 * the monitor advances to the next step. Lower = faster, higher = slower.
 * 3000 = 3 seconds per step. Applies to both Employers and Job Seekers.
 */
const STEP_INTERVAL_MS = 3000

/**
 * ★ STEP TEXT KNOB ★
 * STEP_TITLE_SIZE_PX = font size of the step titles (px). 26 = current
 * (a bit larger than the default 24px headline). Lower = smaller.
 */
const STEP_TITLE_SIZE_PX = 26

/**
 * ★ ACTIVE STEP KNOB ★
 * ACTIVE_STEP_SCALE = how much the ACTIVE step grows (circle + title)
 * while its image is showing. It returns to default when the highlight
 * moves on. 1 = no growth, 1.1 = 10% bigger, 1.2 = 20% bigger.
 */
const ACTIVE_STEP_SCALE = 1.1

/**
 * ★ ACTIVE STEP DESCRIPTION KNOB ★
 * The ACTIVE step shows a short description under its title (inactive
 * steps show none — it appears only when that step's turn comes).
 *   • STEP_DESC_SIZE_PX     = font size of the description (px).
 *   • STEP_DESC_MAX_WIDTH_PX = how wide the text can get before wrapping.
 */
const STEP_DESC_SIZE_PX = 16
const STEP_DESC_MAX_WIDTH_PX = 420

interface Step {
  title: string
  /** Short description — rendered only while this step is ACTIVE. */
  description: string
}

interface ModeContent {
  /** Three steps. First step is the featured (filled) circle; rest outlined. */
  steps: Step[]
  /** One image per step — the monitor cycles through them in order. */
  images: string[]
}

/** Content per audience. Each mode has its own steps + step images. */
const content: Record<'employers' | 'seekers', ModeContent> = {
  employers: {
    steps: [
      {
        title: 'Post jobs & AI-recommended talent',
        description:
          'Reach candidates aggregated across job websites, with AI helping prioritize who fits best.',
      },
      {
        title: 'Interview and shortlist top talent',
        description:
          'Spend less time sorting through unqualified applications and more time evaluating the strongest matches.',
      },
      {
        title: 'Offer and scale your team',
        description:
          'Move faster from shortlist to offer, then hire confidently as your team and company grow.',
      },
    ],
    images: [employer1, employer2, employer3],
  },
  seekers: {
    steps: [
      {
        title: 'Get matched & recommended jobs based on your profile',
        description:
          'The platform pulls roles from multiple job websites in one place, then recommends the best matches based on your profile.',
      },
      {
        title: 'Apply instantly',
        description:
          'Find the right role quickly and submit your application with less effort and fewer extra steps.',
      },
      {
        title: 'Get interviewed, receive offers, and build your career',
        description:
          "With better matching and streamlined applications, you're more likely to get interviews and offers — helping you progress faster.",
      },
    ],
    images: [applicant1, applicant2, applicant3],
  },
}

type Mode = keyof typeof content

/** Monitor + steps for the active mode. Remounted on toggle (key={mode}) so
 *  the slideshow always starts at step 1. The active step's number is
 *  highlighted to sync with the image currently on screen. */
function ModeSection({ mode }: { mode: Mode }) {
  const [stepIndex, setStepIndex] = useState(0)
  const [paused, setPaused] = useState(false)
  const { steps, images } = content[mode]

  // Pause while the image is hovered; resume (fresh interval) on leave.
  useEffect(() => {
    if (paused) return
    const id = window.setInterval(() => {
      setStepIndex((i) => (i + 1) % images.length)
    }, STEP_INTERVAL_MS)
    return () => window.clearInterval(id)
  }, [images, paused])

  return (
    <div
      className={cn(
        'flex flex-col items-center gap-12 px-4 lg:flex-row lg:gap-24',
        mode === 'seekers' && 'lg:flex-row-reverse',
      )}
    >
      {/* Illustration — monitor-style frame */}
      <div className="flex w-full justify-center lg:w-1/2">
        <div
          className="w-full"
          style={{
            maxWidth: MONITOR_WIDTH_PX,
            transform: `translate(${MONITOR_OFFSET_X_PX}px, ${MONITOR_OFFSET_Y_PX}px)`,
          }}
        >
          {/* Screen — dark bezel around the image */}
          <div className="rounded-2xl bg-on-surface p-2 shadow-xl">
            <img
              className="h-auto w-full rounded-lg bg-surface-container-lowest object-cover"
              src={images[stepIndex]}
              alt="A screenshot of the SeraGo job platform dashboard."
              loading="lazy"
              onMouseEnter={() => setPaused(true)}
              onMouseLeave={() => setPaused(false)}
            />
          </div>
          {/* Neck */}
          <div className="mx-auto h-8 w-6 bg-on-surface/80" />
          {/* Base */}
          <div className="mx-auto h-2.5 w-32 rounded-full bg-on-surface/80" />
        </div>
      </div>

      {/* Steps — active step's number fills to sync with the image */}
      <div className="flex w-full flex-col gap-10 lg:w-1/2">
        {steps.map((step, i) => (
          <div
            key={step.title}
            className="group flex items-center gap-6 transition-transform duration-300"
            style={
              i === stepIndex
                ? {
                    transform: `scale(${ACTIVE_STEP_SCALE})`,
                    transformOrigin: 'left center',
                  }
                : undefined
            }
          >
            <div
              className={cn(
                'flex h-12 w-12 flex-shrink-0 items-center justify-center rounded-full text-xl font-semibold transition-all duration-300 group-hover:scale-105',
                i === stepIndex
                  ? 'bg-primary text-on-primary shadow-md'
                  : 'border border-outline-variant bg-surface-container text-secondary',
              )}
            >
              {i + 1}
            </div>
            <div className="flex flex-col gap-1.5">
              <h3
                className="font-headline-md text-on-surface"
                style={{ fontSize: STEP_TITLE_SIZE_PX }}
              >
                {step.title}
              </h3>
              {i === stepIndex && step.description && (
                <p
                  className="step-desc-in font-body-md text-secondary"
                  style={{
                    fontSize: STEP_DESC_SIZE_PX,
                    maxWidth: STEP_DESC_MAX_WIDTH_PX,
                  }}
                >
                  {step.description}
                </p>
              )}
            </div>
          </div>
        ))}
      </div>
    </div>
  )
}

export default function HowItWorks() {
  // Audience toggle is hidden — job seekers is the only view shown. The
  // original state with its setter is kept (commented) below for when the
  // employer side returns.
  // const [mode, setMode] = useState<Mode>('employers')
  const [mode] = useState<Mode>('seekers')

  return (
    <section className="overflow-hidden bg-surface">
      <div
        className="mx-auto w-full max-w-container-max px-margin-mobile pb-24 md:px-margin-desktop md:pb-32"
        style={{ paddingTop: SECTION_TOP_PAD_PX }}
      >
        {/* Section header */}
        <div
          className="mx-auto max-w-3xl px-4 text-center"
          style={{ marginBottom: SUBTITLE_MB_PX }}
        >
          <h2
            className="font-display-lg-mobile text-display-lg-mobile tracking-tight text-on-surface md:font-display-lg md:text-display-lg"
            style={{ marginBottom: HEADING_MB_PX }}
          >
            How <span className="text-primary">Sera</span>
            <span className="text-accent">Go</span> Works
          </h2>
          <p className="font-body-lg text-body-lg leading-relaxed text-on-surface-variant">
            From job discovery to career growth, manage your professional
            journey in simple steps.
          </p>
        </div>

        {/*
          Audience toggle — hidden for now: SeraGo is talent-only (no
          employers), so only the "For Job Seekers" view is shown. Kept here,
          commented out, in case the employer side returns later.

          To restore: uncomment this block, restore TOGGLE_MB_PX above and the
          original `const [mode, setMode] = useState<Mode>('employers')`.
        */}
        {/**
        <div
          className="flex justify-center"
          style={{ marginBottom: TOGGLE_MB_PX }}
        >
          <div className="inline-flex items-center rounded-full border border-surface-variant bg-surface-container-low p-1">
            {(['employers', 'seekers'] as const).map((m) => (
              <button
                key={m}
                type="button"
                aria-pressed={mode === m}
                onClick={() => setMode(m)}
                className={cn(
                  'rounded-full px-6 py-2.5 font-label-md text-label-md transition-colors duration-200',
                  mode === m
                    ? 'bg-primary text-on-primary shadow-sm'
                    : 'text-secondary hover:text-primary',
                )}
              >
                {m === 'employers' ? 'For Employers' : 'For Job Seekers'}
              </button>
            ))}
          </div>
        </div>
        **/}

        {/* Main content — remounted on toggle so the slideshow restarts */}
        <ModeSection key={mode} mode={mode} />
      </div>
    </section>
  )
}
