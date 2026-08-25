import { getStoredAuthTokens } from './auth.ts'
import { config } from '../config'

/**
 * Upload a file through the backend server (avoids CORS with R2).
 * Returns the URL to save in the database.
 */
export async function uploadFile(
  file: File,
  fileType: 'avatar' | 'resume',
): Promise<string> {
  const tokens = getStoredAuthTokens()
  if (!tokens) throw new Error('Not authenticated')

  const formData = new FormData()
  formData.append('file', file)
  formData.append('fileType', fileType)

  const response = await fetch(`${config.apiBaseUrl}/upload/file`, {
    method: 'POST',
    headers: {
      Authorization: `Bearer ${tokens.accessToken}`,
    },
    body: formData,
  })

  if (!response.ok) {
    const text = await response.text()
    let message = `Upload failed (${response.status})`
    try {
      const parsed = JSON.parse(text)
      message = parsed.message ?? message
    } catch { /* use default */ }
    throw new Error(message)
  }

  const data = await response.json()
  return data.data?.url ?? data.url
}

/**
 * Get a SigV4 presigned URL for downloading a private file (e.g., resumes).
 * Valid for 15 minutes.
 */
export async function getPresignedDownloadUrl(key: string): Promise<string> {
  const tokens = getStoredAuthTokens()
  if (!tokens) throw new Error('Not authenticated')

  const response = await fetch(`${config.apiBaseUrl}/upload/download`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      Authorization: `Bearer ${tokens.accessToken}`,
    },
    body: JSON.stringify({ key }),
  })

  if (!response.ok) throw new Error('Failed to get download URL')

  const data = await response.json()
  return data.data?.downloadUrl ?? data.downloadUrl
}
