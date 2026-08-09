# SeraGo Design Standards — "Trust and Action"

This document is the single source of truth for the SeraGo visual system.
Any new feature **must** follow these rules so the product stays consistent.

> Implementation lives in `src/index.css` under the `@theme` block (Tailwind
> v4 — this **is** the app's centralized theme config; there is no
> `tailwind.config.ts`).

---

## 1. Color palette

| Role              | Color         | Hex       | Used for                                                                               |
| ----------------- | ------------- | --------- | -------------------------------------------------------------------------------------- |
| **Primary**       | Royal Blue    | `#1A73E8` | Headlines, the SeraGo wordmark, active states, trusted brand highlights, hover accents |
| **Accent**        | Action Orange | `#FF6600` | **Strictly primary calls-to-action** (Apply Now, Start Hiring, Get Started)            |
| **Background**    | Ash White     | `#F5F5F5` | Page and section backgrounds (`bg-surface`)                                            |
| **Surface/Cards** | Clean White   | `#FFFFFF` | Cards, accordions, inputs, navbar & footer (`bg-surface-container-lowest`)             |
| **Text**          | Charcoal      | `#333333` | Headings (`text-on-surface`) and body/secondary text (`text-secondary`)                |

### Token map (what you actually write in JSX)

| Utility (examples)                                  | Hex                               | Meaning                                       |
| --------------------------------------------------- | --------------------------------- | --------------------------------------------- |
| `bg-surface` / `bg-background`                      | `#F5F5F5`                         | Ash page background — **sections use this**   |
| `bg-surface-container-lowest`                       | `#FFFFFF`                         | Elevated surfaces: cards, nav, footer, inputs |
| `bg-surface-container-low`                          | `#F0F1F2`                         | Inset wells, active list rows, toggle tracks  |
| `bg-surface-container` / `-high` / `-highest`       | `#E9EAEC` / `#E0E2E4` / `#D5D7DA` | Deeper wells, subtle fills                    |
| `text-on-surface`                                   | `#333333`                         | Primary text (headings)                       |
| `text-secondary` / `text-on-surface-variant`        | `#5F6368`                         | Body copy, secondary info                     |
| `text-primary` / `bg-primary`                       | `#1A73E8`                         | Brand text + blue highlights & active states  |
| `bg-accent` / `text-on-accent`                      | `#FF6600` / `#FFF`                | **The CTA color — CTA buttons only**          |
| `border-surface-variant` / `border-outline-variant` | `#E4E6E8` / `#DADCE0`             | Hairline borders                              |
| `text-primary` hover via `hover:text-primary`       | —                                 | Standard link hover                           |

### Hard rules

1. **Orange is sacred.** `bg-accent` appears only on primary call-to-action
   buttons — never on headings, badges, icons, or decorative elements.
2. **Display/section headlines are Royal Blue** via `text-primary` (that's what
   "primary text" means here — e.g. "Discover Your Next Role"). Charcoal
   (`text-on-surface`) is for body copy and standard text, never black.
3. **Blue is the brand color** — wordmark, primary-text emphasis
   (`<span className="text-primary">`), active pills, and hover highlights.
4. **Page = ash, cards = white.** Every full-width section gets
   `bg-surface`; every card/panel inside gets `bg-surface-container-lowest`.
5. Small status dots may stay green (`bg-green-500`) — they are signals, not
   brand colors.

### Accessibility note (orange CTAs)

White text on `#FF6600` is ≈2.9:1, below the WCAG AA 4.5:1 target for
14px button labels. The palette fixes the accent at `#FF6600` (a brand
choice); if AA compliance is ever required, darken the CTA fill to
`#C2410C` (≈4.0:1) or `#D9480F` (≈3.4:1) in `--color-accent` — visually
still "action orange", just deeper.

---

## 2. Typography

Two families are loaded globally:

| Family             | Used for                 | Tokens                                                                                                              |
| ------------------ | ------------------------ | ------------------------------------------------------------------------------------------------------------------- |
| **Hanken Grotesk** | Display, headlines, body | `font-display-lg`, `font-display-lg-mobile`, `font-headline-md`, `font-headline-lg`, `font-body-md`, `font-body-lg` |
| **Geist**          | Labels / micro-UI        | `font-label-sm`, `font-label-md`                                                                                    |

### Type scale

| Token               | Size | Weight | Use                                                  |
| ------------------- | ---- | ------ | ---------------------------------------------------- |
| `display-lg`        | 64px | 700    | Page/section hero headings                           |
| `display-lg-mobile` | 40px | 700    | Mobile fallback for `display-lg`                     |
| `headline-lg`       | 32px | 600    | Sub-headings                                         |
| `headline-md`       | 24px | 600    | Card titles, questions                               |
| `body-lg`           | 18px | 400    | Section intros / lead paragraphs                     |
| `body-md`           | 16px | 400    | Default body text                                    |
| `label-md`          | 14px | 500    | Buttons, badges, nav links (letter-spacing `0.05em`) |
| `label-sm`          | 12px | 500    | Micro-labels, card metadata                          |

**Rules:** headings use tight `tracking-tight`/`tracking-tighter` on display
sizes; body text keeps generous `leading-relaxed` where multi-line. Never set
font sizes inline — prefer the tokens (or a ★ knob at the top of the file,
as the landing sections do).

---

## 3. Button standards

Central component: `src/components/ui/Button.tsx` (`<Button variant size …>`).

| Variant       | Classes                                                                                                    | Use                                         |
| ------------- | ---------------------------------------------------------------------------------------------------------- | ------------------------------------------- |
| **primary**   | `bg-accent text-on-accent hover:opacity-90`                                                                | The ONLY call-to-action. Orange.            |
| **secondary** | `border border-outline-variant bg-surface-container-lowest text-on-surface hover:bg-surface-container-low` | Outlined white button — non-primary actions |
| **ghost**     | `text-on-surface-variant hover:bg-surface-container-low`                                                   | Quiet inline actions                        |

| Size           | Padding     |
| -------------- | ----------- |
| `sm`           | `px-4 py-2` |
| `md` (default) | `px-6 py-3` |
| `lg`           | `px-8 py-4` |

**Shape & states**

- Border radius: `rounded` (0.125rem, the theme default) — small, crisp.
- Hover: primary fades (`hover:opacity-90`); secondary/ghost lighten the fill.
- Active: press feedback via `active:scale-95` on hero CTAs.
- Focus: `focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary`.
- Disabled: `disabled:pointer-events-none disabled:opacity-60`.
- Label: `font-label-md text-label-md` (Geist, 14px, weight 500, tracking `0.05em`).

**When to use which**

- **Orange (primary):** one clear action per view — "Apply Now", "Start
  Hiring", "Get Started", "Start Applying".
- **Blue:** brand accents and active toggles only, never as a button fill.
- **Outline (secondary):** supporting actions next to an orange CTA.

---

## 4. Component conventions

- **Section template:** `<section className="bg-surface">` → `Container`-like
  wrapper (`mx-auto w-full max-w-container-max px-margin-mobile md:px-margin-desktop`)
  → heading + content. `max-w-container-max` = 1280px.
- **Cards:** `rounded-lg/xl + border border-surface-variant + bg-surface-container-lowest + hover:border-outline-variant` (job cards, FAQ items).
- **Badges/pills:** `rounded-full border border-outline-variant bg-surface-container-low px-4 py-1.5 text-label-sm text-on-surface-variant`.
- **Tunable values:** expose user-facing sizes as named constants with a
  `★ KNOB ★` comment at the top of the file (e.g. `STEP_INTERVAL_MS`).
- **Icons:** Google Material Symbols Outlined (see `index.css`).
- **Radii:** use the theme scale (`rounded`, `rounded-lg`, `rounded-xl`,
  `rounded-full`) — do not invent new radius values inline.

---

## 5. Dark mode

The app ships a `dark` class toggle (ThemeProvider). The "Trust and Action"
palette is defined for the light theme; the dark variants currently reuse the
same tokens. When dark mode is designed, add a proper dark `@theme` block —
do not hardcode colors in components.
