import { usePresignedUrl } from '../../hooks/usePresignedUrl.ts'
import { cn } from '../../lib/cn'

interface ResumeLinkProps {
  /** R2 key or full URL of the resume */
  resumeUrl: string | null
  /** Link text */
  children?: React.ReactNode
  /** Additional class names */
  className?: string
  /** Link props */
  [key: string]: unknown
}

/**
 * A link that downloads a resume. Handles both full URLs and R2 keys.
 * If the resume is stored as an R2 key, it generates a temporary presigned download URL.
 */
export default function ResumeLink({
  resumeUrl,
  children = 'View resume',
  className,
  ...props
}: ResumeLinkProps) {
  const { url, loading, error } = usePresignedUrl(resumeUrl)

  if (!resumeUrl) return null

  if (loading) {
    return (
      <span className={cn('inline-flex items-center gap-1.5 text-on-surface-variant/50', className)}>
        <span className="h-3 w-3 animate-spin rounded-full border border-primary/30 border-t-primary" />
        Loading...
      </span>
    )
  }

  if (error || !url) {
    return (
      <span className={cn('text-on-surface-variant/50', className)}>
        Resume unavailable
      </span>
    )
  }

  return (
    <a
      href={url}
      target="_blank"
      rel="noreferrer"
      className={className || 'text-primary hover:underline'}
      {...props}
    >
      {children}
    </a>
  )
}
