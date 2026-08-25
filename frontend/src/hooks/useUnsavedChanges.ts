import { useCallback, useEffect, useState } from 'react'
import { useBlocker } from 'react-router-dom'

interface UseUnsavedChangesOptions {
  /** Whether the form has unsaved changes. */
  isDirty: boolean
  /** Called when the user chooses to save. Should return a Promise that resolves when saving is complete. */
  onSave: () => Promise<void>
}

/**
 * Blocks react-router navigation when `isDirty` is true and shows a
 * confirmation dialog. Also handles the browser beforeunload event
 * (closing the tab / refreshing).
 */
export function useUnsavedChanges({ isDirty, onSave }: UseUnsavedChangesOptions) {
  const [dialogOpen, setDialogOpen] = useState(false)
  const [pendingNavigation, setPendingNavigation] = useState<(() => void) | null>(null)
  const [saving, setSaving] = useState(false)

  // Block react-router navigations when dirty
  const blocker = useBlocker(
    ({ currentLocation, nextLocation }) =>
      isDirty && currentLocation.pathname !== nextLocation.pathname,
  )

  // When the blocker triggers, open the dialog and stash the navigation callback
  useEffect(() => {
    if (blocker.state === 'blocked') {
      setDialogOpen(true)
      setPendingNavigation(() => blocker.proceed)
    }
  }, [blocker.state, blocker.proceed])

  // Browser beforeunload for tab close / refresh
  useEffect(() => {
    const handler = (e: BeforeUnloadEvent) => {
      if (isDirty) {
        e.preventDefault()
      }
    }
    window.addEventListener('beforeunload', handler)
    return () => window.removeEventListener('beforeunload', handler)
  }, [isDirty])

  const handleSave = useCallback(async () => {
    setSaving(true)
    try {
      await onSave()
      setDialogOpen(false)
      setSaving(false)
      // Proceed with the blocked navigation after saving
      pendingNavigation?.()
      setPendingNavigation(null)
    } catch {
      setSaving(false)
      // Stay on the page — dialog stays open so user can retry or discard
    }
  }, [onSave, pendingNavigation])

  const handleDiscard = useCallback(() => {
    setDialogOpen(false)
    // Proceed without saving
    pendingNavigation?.()
    setPendingNavigation(null)
  }, [pendingNavigation])

  const handleCancel = useCallback(() => {
    setDialogOpen(false)
    setPendingNavigation(null)
  }, [])

  return {
    dialogOpen,
    saving,
    handleSave,
    handleDiscard,
    handleCancel,
  }
}
