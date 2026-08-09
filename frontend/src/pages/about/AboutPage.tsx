import { Badge } from '../../components/ui/Badge.tsx'
import { Button } from '../../components/ui/Button.tsx'
import { Container } from '../../components/ui/Container.tsx'

const stack = [
  { name: 'React 19', role: 'UI framework' },
  { name: 'Vite 8', role: 'Build tool & dev server' },
  { name: 'TypeScript', role: 'Typed JavaScript' },
  { name: 'Tailwind CSS v4', role: 'Styling' },
  { name: 'React Router 7', role: 'Client-side routing' },
  { name: 'ESLint + Prettier', role: 'Linting & formatting' },
]

export default function AboutPage() {
  return (
    <section className="py-20 sm:py-28">
      <Container>
        <div className="mx-auto max-w-3xl">
          <Badge>
            <span className="size-1.5 rounded-full bg-primary" />
            About SeraGo
          </Badge>

          <h1 className="mt-6 text-balance font-display-lg-mobile font-bold tracking-tight text-on-surface sm:text-display-lg">
            Only jobs that <span className="text-primary">matter to you</span>
          </h1>

          <p className="mt-6 text-pretty text-body-lg leading-relaxed text-on-surface-variant">
            SeraGo is the frontend of a growing job-matching platform. The goal
            is simple: help people find the roles that actually matter to them —
            and help companies find the people who matter to them.
          </p>
          <p className="mt-4 text-pretty leading-relaxed text-on-surface-variant">
            This repository contains the SeraGo frontend, built with a modern
            React + Vite stack. It sits alongside the{' '}
            <code className="rounded bg-surface-container-high px-1.5 py-0.5 font-mono text-sm text-on-surface-variant">
              backend/
            </code>{' '}
            folder in the SeraGo monorepo, and talks to the SeraGo API as
            features land.
          </p>

          <h2 className="mt-16 font-headline-lg text-headline-lg font-semibold tracking-tight text-on-surface">
            Tech stack
          </h2>
          <div className="mt-6 grid gap-4 sm:grid-cols-2">
            {stack.map((item) => (
              <div
                key={item.name}
                className="flex items-center justify-between rounded-xl border border-outline-variant bg-surface-container-lowest px-5 py-4 transition-colors hover:bg-surface-container-low"
              >
                <span className="font-medium text-on-surface">{item.name}</span>
                <span className="text-body-md text-on-surface-variant">
                  {item.role}
                </span>
              </div>
            ))}
          </div>

          <div className="mt-16 rounded-2xl border border-outline-variant bg-surface-container-low p-8 text-center">
            <h2 className="font-headline-md text-headline-md font-semibold text-on-surface">
              Want to contribute?
            </h2>
            <p className="mx-auto mt-2 max-w-md text-on-surface-variant">
              The project is just getting started — there’s plenty of room for
              new ideas, new matches, and new code.
            </p>
            <Button to="/" className="mt-6">
              Back to home
            </Button>
          </div>
        </div>
      </Container>
    </section>
  )
}
