import { useCallback, useEffect, useState } from 'react'
import { fetchSavedJobs, getStoredAuthTokens, saveJob, unsaveJob } from '../api'

/**
 * Whether a set of jobs is saved + a toggle. Loads the user's saved jobs once
 * (a single GET /api/saved-jobs) and flips membership optimistically on
 * save/unsave, reverting if the API call fails.
 *
 * @param active  only load when the user is authenticated (e.g. pass
 *                auth.status === 'authenticated')
 */
export function useSavedJobs(active: boolean) {
  const [savedIds, setSavedIds] = useState<Set<string>>(new Set())
  const [ready, setReady] = useState(false)

  useEffect(() => {
    if (!active) return
    const tokens = getStoredAuthTokens()
    if (!tokens) return
    let cancelled = false
    fetchSavedJobs(tokens.accessToken)
      .then((data) => {
        if (cancelled) return
        setSavedIds(
          new Set(
            data.items
              .filter((item) => item.jobId !== null)
              .map((item) => item.jobId as string),
          ),
        )
      })
      .catch(() => {
        /* Non-fatal — the buttons just start unsaved. */
      })
      .finally(() => {
        if (!cancelled) setReady(true)
      })
    return () => {
      cancelled = true
    }
  }, [active])

  const isSaved = useCallback(
    (jobId: string) => savedIds.has(jobId),
    [savedIds],
  )

  const toggleSaved = useCallback(
    async (jobId: string) => {
      const tokens = getStoredAuthTokens()
      if (!tokens) return
      const wasSaved = savedIds.has(jobId)
      // Optimistic flip, then revert if the API disagrees.
      setSavedIds((prev) => {
        const next = new Set(prev)
        if (wasSaved) next.delete(jobId)
        else next.add(jobId)
        return next
      })
      try {
        if (wasSaved) await unsaveJob(jobId, tokens.accessToken)
        else await saveJob(jobId, tokens.accessToken)
      } catch {
        setSavedIds((prev) => {
          const next = new Set(prev)
          if (wasSaved) next.add(jobId)
          else next.delete(jobId)
          return next
        })
      }
    },
    [savedIds],
  )

  return { savedIds, ready, isSaved, toggleSaved }
}
