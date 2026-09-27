import { useCallback, useMemo, useState } from 'react'
import type { ReactNode } from 'react'
import { SidebarContext } from './sidebar.ts'

/**
 * Shares the dashboard sidebar's pinned/expanded state.
 *
 * Pinned → the desktop column stays expanded permanently. Unpinned → it's a
 * collapsed icon rail that expands on hover and collapses again when the
 * pointer leaves it. The pin button lives at the top-right of the sidebar.
 *
 * The mobile drawer is tracked separately and always starts closed.
 */
export function SidebarProvider({ children }: { children: ReactNode }) {
  const [pinned, setPinned] = useState(false)
  const [hoverOpen, setHoverOpen] = useState(false)
  const [drawerOpen, setDrawerOpen] = useState(false)

  const sidebarOpen = pinned || hoverOpen

  const togglePinned = useCallback(() => setPinned((p) => !p), [])
  const expand = useCallback(() => setHoverOpen(true), [])
  const collapse = useCallback(() => setHoverOpen(false), [])

  const value = useMemo(
    () => ({
      sidebarOpen,
      pinned,
      togglePinned,
      expand,
      collapse,
      drawerOpen,
      setDrawerOpen,
    }),
    [sidebarOpen, pinned, togglePinned, expand, collapse, drawerOpen],
  )

  return <SidebarContext.Provider value={value}>{children}</SidebarContext.Provider>
}
