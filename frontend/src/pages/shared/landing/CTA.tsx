/**
 * ★ CTA LAYOUT KNOBS ★
 *   • CTA_MAX_WIDTH_PX = max width of the dark card, centered.
 *   • CTA_PAD_Y_PX     = vertical padding inside the dark card (px).
 */
const CTA_MAX_WIDTH_PX = 960
const CTA_PAD_Y_PX = 112

export default function CTA() {
  return (
    <section className="bg-surface">
      <div className="mx-auto w-full max-w-container-max px-margin-mobile pb-24 md:px-margin-desktop md:pb-32">
        <div
          className="relative mx-auto overflow-hidden rounded-[2rem] bg-primary px-6 text-center md:px-16"
          style={{
            maxWidth: CTA_MAX_WIDTH_PX,
            paddingTop: CTA_PAD_Y_PX,
            paddingBottom: CTA_PAD_Y_PX,
          }}
        >
          {/* Soft glow behind the headline */}
          <div className="pointer-events-none absolute -top-40 left-1/2 h-72 w-[42rem] -translate-x-1/2 rounded-full bg-white/20 blur-3xl" />

          <div className="relative">
            <div className="inline-flex items-center gap-2 rounded-full border border-white/30 px-4 py-1.5">
              <span className="h-2 w-2 rounded-full bg-green-500" />
              <span className="font-label-md text-label-md text-surface-container-lowest/80">
                SeraGo
              </span>
            </div>

            <h2 className="mx-auto mt-6 max-w-3xl font-display-lg-mobile text-display-lg-mobile tracking-tight text-surface-container-lowest md:font-display-lg md:text-display-lg">
              Only jobs that <span className="text-accent">matter to you</span>.
            </h2>
            <p className="mx-auto mt-5 max-w-xl font-body-lg text-body-lg text-surface-container-lowest/70">
              Every job site in one place, matched to you by AI — start
              searching or start hiring in minutes.
            </p>

            <div className="mt-10 flex flex-col items-center justify-center gap-4 sm:flex-row">
              <button
                type="button"
                className="rounded bg-accent px-8 py-4 font-label-md text-label-md text-on-accent shadow-sm transition-all hover:opacity-90 active:scale-95"
              >
                Start Applying
              </button>
              <button
                type="button"
                className="rounded border border-white/40 px-8 py-4 font-label-md text-label-md text-surface-container-lowest transition-colors hover:bg-white/10"
              >
                For Employers
              </button>
            </div>
          </div>
        </div>
      </div>
    </section>
  )
}
