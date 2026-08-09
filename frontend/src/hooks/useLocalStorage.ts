import { useEffect, useState } from 'react'

/**
 * State that is persisted to localStorage and stays in sync across
 * writes. Falls back gracefully when storage is unavailable.
 */
export function useLocalStorage<T>(key: string, initialValue: T) {
  const [value, setValue] = useState<T>(() => {
    try {
      const stored = window.localStorage.getItem(key)
      return stored !== null ? (JSON.parse(stored) as T) : initialValue
    } catch {
      return initialValue
    }
  })

  useEffect(() => {
    try {
      window.localStorage.setItem(key, JSON.stringify(value))
    } catch {
      // Storage unavailable (private mode, quota) — ignore.
    }
  }, [key, value])

  return [value, setValue] as const
}
