import { useEffect } from 'react'
import { useLocation } from 'react-router-dom'

const UTM_STORAGE_KEY = 'serago_utm_params'

export interface UTMParams {
  utm_source?: string
  utm_medium?: string
  utm_campaign?: string
  utm_term?: string
  utm_content?: string
  captured_at?: string
  landing_page?: string
}

/**
 * Grabs UTM parameters (and fallback 'from') from the URL and persists them to
 * both sessionStorage (current tab) and localStorage (cross-tab/later session)
 * so acquisition source is preserved even when client-side routing drops search params.
 */
export function useUTMTracking(): void {
  const location = useLocation()

  useEffect(() => {
    const searchParams = new URLSearchParams(location.search)

    const source = searchParams.get('utm_source') || searchParams.get('from')
    const medium = searchParams.get('utm_medium')
    const campaign = searchParams.get('utm_campaign')
    const term = searchParams.get('utm_term')
    const content = searchParams.get('utm_content')

    if (source || medium || campaign) {
      const utmPayload: UTMParams = {
        ...(source && { utm_source: source }),
        ...(medium && { utm_medium: medium }),
        ...(campaign && { utm_campaign: campaign }),
        ...(term && { utm_term: term }),
        ...(content && { utm_content: content }),
        captured_at: new Date().toISOString(),
        landing_page: location.pathname,
      }

      try {
        sessionStorage.setItem(UTM_STORAGE_KEY, JSON.stringify(utmPayload))
        localStorage.setItem(UTM_STORAGE_KEY, JSON.stringify(utmPayload))
      } catch (err) {
        console.warn('Unable to persist UTM parameters', err)
      }
    }
  }, [location.pathname, location.search])
}

/**
 * Retrieve saved UTM attribution parameters upon registration or conversion.
 */
export function getStoredUTMParams(): UTMParams | null {
  try {
    const raw = sessionStorage.getItem(UTM_STORAGE_KEY) || localStorage.getItem(UTM_STORAGE_KEY)
    return raw ? (JSON.parse(raw) as UTMParams) : null
  } catch {
    return null
  }
}

/**
 * Clear stored UTM parameters after successful registration.
 */
export function clearStoredUTMParams(): void {
  try {
    sessionStorage.removeItem(UTM_STORAGE_KEY)
    localStorage.removeItem(UTM_STORAGE_KEY)
  } catch {}
}
