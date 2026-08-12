import { Button } from '../../../components/ui/Button.tsx'

export default function NotFoundPage() {
  return (
    <section className="relative flex min-h-[70svh] items-center justify-center overflow-hidden px-4 py-24">
      <div className="relative text-center">
        <p className="font-display-lg text-[120px] font-black tracking-tighter text-primary sm:text-[160px]">
          404
        </p>
        <h1 className="mt-4 font-headline-md text-headline-md font-semibold tracking-tight text-on-surface sm:text-headline-lg">
          This role doesn’t exist
        </h1>
        <p className="mx-auto mt-3 max-w-md text-pretty text-on-surface-variant">
          The page you’re looking for has moved on — but the right job for you
          is still out there.
        </p>
        <Button to="/" className="mt-8">
          Take me home
        </Button>
      </div>
    </section>
  )
}
