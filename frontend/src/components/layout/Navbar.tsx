import { useState } from 'react'
import { Link } from 'react-router-dom'
import { Button } from '../ui/Button.tsx'

const navLinks = ['Platform', 'Solutions', 'Developers', 'Pricing']

export default function Navbar() {
  const [open, setOpen] = useState(false)

  return (
    <header className="sticky top-0 z-50 w-full bg-surface-container-lowest">
      <div className="mx-auto flex h-20 max-w-container-max items-center justify-between px-margin-mobile md:px-margin-desktop">
        <Link
          to="/"
          className="font-headline-md text-headline-md font-bold tracking-tight text-primary"
          aria-label="SeraGo home"
        >
          SeraGo
        </Link>

        {/* Desktop links */}
        <nav className="hidden gap-8 md:flex">
          {navLinks.map((link) => (
            <a
              key={link}
              href="#"
              className="font-body-md text-body-md text-secondary transition-colors duration-200 hover:text-primary"
            >
              {link}
            </a>
          ))}
        </nav>

        <div className="hidden items-center gap-4 md:flex">
          <Button to="/about" variant="primary">
            Get Started
          </Button>
        </div>

        {/* Mobile toggle */}
        <button
          type="button"
          className="text-primary md:hidden"
          onClick={() => setOpen((o) => !o)}
          aria-expanded={open}
          aria-label="Toggle navigation menu"
        >
          <span className="material-symbols-outlined">
            {open ? 'close' : 'menu'}
          </span>
        </button>
      </div>

      {/* Mobile menu */}
      {open && (
        <div className="border-t border-outline-variant px-margin-mobile pb-4 pt-2 md:hidden">
          {navLinks.map((link) => (
            <a
              key={link}
              href="#"
              onClick={() => setOpen(false)}
              className="block rounded-lg px-3 py-2.5 font-body-md text-body-md text-secondary transition-colors hover:text-primary"
            >
              {link}
            </a>
          ))}
          <Button
            to="/about"
            variant="primary"
            className="mt-2 w-full"
            onClick={() => setOpen(false)}
          >
            Get Started
          </Button>
        </div>
      )}
    </header>
  )
}
