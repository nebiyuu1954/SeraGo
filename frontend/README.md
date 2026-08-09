# SeraGo Frontend

The frontend for the SeraGo job-matching platform (“The Pulse”). Built with **React 19**, **Vite 8**, **TypeScript**, **Tailwind CSS v4**, and **React Router 7**.

## Getting started

```bash
npm install          # install dependencies
cp .env.example .env # configure environment variables (optional)
npm run dev          # start the dev server (http://localhost:5173)
```

## Scripts

| Command                | Description                               |
| ---------------------- | ----------------------------------------- |
| `npm run dev`          | Start the Vite dev server                 |
| `npm run build`        | Typecheck (`tsc -b`) and production build |
| `npm run preview`      | Preview the production build              |
| `npm run lint`         | Lint with ESLint                          |
| `npm run format`       | Format all files with Prettier            |
| `npm run format:check` | Check formatting without writing          |

## Architecture

The codebase is organized into decoupled, single-purpose folders so it can
scale into a large project without tangling:

```
src/
  api/          Typed fetch client + endpoint registry (base URL from config)
  components/   Reusable UI: ui/ (Button, Badge, Container) and layout/ (Navbar,
                Footer)
  config/       Environment-driven configuration (VITE_* env vars)
  context/      React contexts (ThemeProvider) with their hooks
  hooks/        Reusable hooks (useLocalStorage, useMediaQuery)
  lib/          Utilities (cn — classname merge)
  pages/        Page-specific code, one folder per route:
                  landing/   Hero (the Pulse data hub) + JobListings section,
                             composed by LandingPage
                  about/     AboutPage
                  not-found/ NotFoundPage
  types/        Shared TypeScript contracts (API payloads, etc.)
```

Rules of thumb:

- **Pages** compose **reusable components** — they never define their own
  buttons, badges, or containers.
- **API calls** go through `src/api` — feature code never talks to `fetch`
  directly.
- **Configuration** lives in `src/config` and is driven by environment
  variables, so swapping hosts never requires code changes.
- **State shared across the app** goes in `src/context`; logic extracted from
  components goes in `src/hooks`.

## Configuration

All environment variables are read through `src/config/env.ts` and are
`VITE_`-prefixed (see `.env.example`):

| Variable            | Default                     | Purpose              |
| ------------------- | --------------------------- | -------------------- |
| `VITE_APP_NAME`     | `SeraGo`                    | App name (footer…)   |
| `VITE_APP_VERSION`  | `0.1.0`                     | App version (footer) |
| `VITE_API_BASE_URL` | `http://localhost:3000/api` | SeraGo API base URL  |

## Deploying to Vercel

1. Push the repository to GitHub.
2. In Vercel, create a new project and import the repo.
3. Set the **Root Directory** to `frontend`.
4. Add environment variables (e.g. `VITE_API_BASE_URL` pointing at your
   deployed API) under Settings → Environment Variables.
5. Deploy. `vercel.json` handles the SPA rewrite so deep links like
   `/about` work on refresh.

The app is fully static after build (`dist/`), so it can be moved to any
static host (Netlify, Cloudflare Pages, S3 + CDN, etc.) by keeping the
`VITE_*` environment variables in sync — no code changes required.
