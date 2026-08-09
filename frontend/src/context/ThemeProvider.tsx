import { useCallback, useEffect, useMemo } from 'react'
import type { ReactNode } from 'react'
import { useLocalStorage } from '../hooks'
import { ThemeContext, type Theme } from './theme.ts'

/**
 * Manages the app theme, persists it to localStorage, and reflects it
 * on the `<html>` element via the `dark` class (Tailwind dark variant).
 */
export function ThemeProvider({ children }: { children: ReactNode }) {
  const [theme, setTheme] = useLocalStorage<Theme>('serago-theme', 'light')

  useEffect(() => {
    document.documentElement.classList.toggle('dark', theme === 'dark')
  }, [theme])

  const toggleTheme = useCallback(() => {
    setTheme((current) => (current === 'dark' ? 'light' : 'dark'))
  }, [setTheme])

  const value = useMemo(
    () => ({ theme, setTheme, toggleTheme }),
    [theme, setTheme, toggleTheme],
  )

  return <ThemeContext.Provider value={value}>{children}</ThemeContext.Provider>
}
