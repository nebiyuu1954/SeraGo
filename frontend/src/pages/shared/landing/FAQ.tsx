import { useState } from 'react'
import { cn } from '../../../lib/cn.ts'

/**
 * ★ FAQ LAYOUT KNOBS ★
 *   • FAQ_MAX_WIDTH_PX     = max width of the accordion, centered.
 *   • FAQ_ITEM_GAP_PX      = vertical gap between accordion items.
 *   • ACCORDION_DEFAULT_OPEN = which item starts open (0-based index;
 *                          -1 = all closed). 0 = first item open.
 */
const FAQ_MAX_WIDTH_PX = 720
const FAQ_ITEM_GAP_PX = 16
const ACCORDION_DEFAULT_OPEN = 0

interface FaqItem {
  question: string
  answer: string
}

const faqItems: FaqItem[] = [
  {
    question: 'What is SeraGo?',
    answer:
      'SeraGo brings jobs from multiple job websites into one place and uses AI to match you with roles that actually fit your profile — no more hopping between sites.',
  },
  {
    question: 'Is SeraGo free for job seekers?',
    answer:
      'Yes. Finding and applying to jobs on SeraGo is completely free — we are built to make your job hunt faster, not charge you for it.',
  },
  {
    question: 'How does the AI matching work?',
    answer:
      'SeraGo aggregates listings from across the web, then ranks them against your profile and preferences so you see the roles most likely to be a good fit first.',
  },
  {
    question: 'Where do the job listings come from?',
    answer:
      'We pull listings from multiple job websites and combine them with roles posted directly on SeraGo, so you can search everything from one place.',
  },
  {
    question: 'How fast can I apply?',
    answer:
      'With aggregated listings, you can apply instantly from SeraGo — fewer extra steps and less effort than applying site by site.',
  },
  {
    question: 'Do I need an account to save or apply to jobs?',
    answer:
      'You need a free account to save jobs, apply, and track your applications. Creating one takes a minute and keeps everything in one place.',
  },
]

export default function FAQ() {
  const [openIndex, setOpenIndex] = useState<number | null>(
    ACCORDION_DEFAULT_OPEN,
  )

  return (
    <section className="bg-surface">
      <div className="mx-auto w-full max-w-container-max px-margin-mobile py-24 md:px-margin-desktop md:py-32">
        {/* Section header */}
        <div className="mx-auto mb-12 max-w-3xl text-center">
          <h2 className="font-display-lg-mobile text-display-lg-mobile tracking-tight text-on-surface md:font-display-lg md:text-display-lg">
            Frequently Asked Questions
          </h2>
          <p className="mt-4 font-body-lg text-body-lg leading-relaxed text-on-surface-variant">
            Everything you need to know about SeraGo.
          </p>
        </div>

        {/* Accordion */}
        <div
          className="mx-auto flex flex-col"
          style={{ maxWidth: FAQ_MAX_WIDTH_PX, gap: FAQ_ITEM_GAP_PX }}
        >
          {faqItems.map((item, i) => {
            const open = openIndex === i
            return (
              <div
                key={item.question}
                className={cn(
                  'overflow-hidden rounded-xl border transition-colors duration-300',
                  open
                    ? 'border-outline bg-surface-container-lowest shadow-sm'
                    : 'border-surface-variant bg-surface-container-lowest hover:border-outline',
                )}
              >
                <button
                  type="button"
                  aria-expanded={open}
                  onClick={() => setOpenIndex(open ? null : i)}
                  className="flex w-full items-center justify-between gap-6 px-6 py-5 text-left"
                >
                  <span className="font-headline-md text-headline-md text-on-surface">
                    {item.question}
                  </span>
                  <span
                    aria-hidden="true"
                    className={cn(
                      'material-symbols-outlined flex-shrink-0 text-on-surface transition-transform duration-300',
                      open && 'rotate-45',
                    )}
                  >
                    add
                  </span>
                </button>
                {/* Animated answer — grid-rows 0fr→1fr slides it open smoothly */}
                <div
                  className={cn(
                    'grid transition-all duration-300 ease-out',
                    open
                      ? 'grid-rows-[1fr] opacity-100'
                      : 'grid-rows-[0fr] opacity-0',
                  )}
                >
                  <div className="overflow-hidden">
                    <p className="px-6 pb-5 font-body-md text-body-md text-secondary">
                      {item.answer}
                    </p>
                  </div>
                </div>
              </div>
            )
          })}
        </div>
      </div>
    </section>
  )
}
