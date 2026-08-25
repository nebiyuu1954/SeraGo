import { useEffect, useState } from 'react'
import { getPresignedDownloadUrl } from '../api/fileUpload.ts'

/**
 * Hook that resolves an R2 file key to a temporary presigned download URL.
 * If the value is already a full URL (starts with http), it's returned as-is.
 * If it's an R2 key, a presigned GET URL is generated via the API.
 *
 * @param fileKey - The R2 key or full URL
 * @param enabled - Whether to fetch the presigned URL (default: true)
 * @returns The resolved URL, or null while loading / on error
 */
export function usePresignedUrl(fileKey: string | null | undefined, enabled = true) {
  const [url, setUrl] = useState<string | null>(null)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    if (!fileKey || !enabled) {
      setUrl(fileKey && fileKey.startsWith('http') ? fileKey : null)
      return
    }

    // If it's already a full URL, use it directly
    if (fileKey.startsWith('http')) {
      setUrl(fileKey)
      return
    }

    // Otherwise, generate a presigned URL
    let cancelled = false
    setLoading(true)
    setError(null)

    getPresignedDownloadUrl(fileKey)
      .then((presignedUrl: string) => {
        if (!cancelled) {
          setUrl(presignedUrl)
        }
      })
      .catch((err: unknown) => {
        if (!cancelled) {
          setError(err instanceof Error ? err.message : 'Failed to get download URL')
        }
      })
      .finally(() => {
        if (!cancelled) {
          setLoading(false)
        }
      })

    return () => {
      cancelled = true
    }
  }, [fileKey, enabled])

  return { url, loading, error }
}
