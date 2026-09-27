import { createContext, useContext } from 'react'

export interface SidebarContextValue {
  /**
   * Whether the desktop sidebar column is currently expanded (labels shown).
   * True when the sidebar is pinned, or when an unpinned sidebar has been
   * opened by hovering it.
   */
  sidebarOpen: boolean
  /**
   * Pinned sidebars stay expanded permanently. Unpinned sidebars are a
   * collapsed icon rail that expands on hover and collapses again once the
   * pointer leaves it.
   */
  pinned: boolean
  /** Flip the pinned state (the pin button at the top of the sidebar). */
  togglePinned: () => void
  /** Expand an unpinned sidebar — called when the pointer enters it. */
  expand: () => void
  /** Collapse an unpinned sidebar — called when the pointer leaves it. */
  collapse: () => void
  /**
   * Mobile slide-in drawer. Kept separate from the desktop column so the
   * two never leak into each other when the viewport crosses the md
   * breakpoint (e.g. resizing a desktop with an open sidebar to mobile).
   */
  drawerOpen: boolean
  setDrawerOpen: (open: boolean) => void
}

export const SidebarContext = createContext<SidebarContextValue | null>(null)

export function useSidebar(): SidebarContextValue {
  const context = useContext(SidebarContext)
  if (!context) {
    throw new Error('useSidebar must be used within a SidebarProvider')
  }
  return context
}
