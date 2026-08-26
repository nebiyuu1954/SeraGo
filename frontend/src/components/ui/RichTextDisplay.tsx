import { cn } from '../../lib/cn'

interface RichTextDisplayProps {
  /** HTML string to render. Falls back to plain-text rendering for non-HTML content. */
  html: string
  /** Optional class on the outer wrapper. */
  className?: string
}

/**
 * Renders HTML content from the rich text editor with proper styling.
 * Handles both HTML (from Tiptap) and plain text (legacy content).
 */
export default function RichTextDisplay({ html, className }: RichTextDisplayProps) {
  if (!html || !html.trim()) return null

  // Detect if content is HTML (has tags) or plain text
  const isHtml = /<[a-z][\s\S]*>/i.test(html)

  if (!isHtml) {
    // Plain text fallback — preserve newlines
    return (
      <p className={cn('whitespace-pre-line font-body-md text-body-md leading-relaxed text-on-surface', className)}>
        {html}
      </p>
    )
  }

  return (
    <div
      className={cn(
        // Base text
        'font-body-md text-body-md leading-relaxed text-on-surface',
        // Headings
        '[&>h1]:font-headline-xl [&>h1]:text-headline-xl [&>h1]:text-on-surface [&>h1]:mt-6 [&>h1]:mb-3',
        '[&>h2]:font-headline-lg [&>h2]:text-headline-lg [&>h2]:text-on-surface [&>h2]:mt-5 [&>h2]:mb-2',
        '[&>h3]:font-headline-md [&>h3]:text-headline-md [&>h3]:text-on-surface [&>h3]:mt-4 [&>h3]:mb-2',
        // Paragraphs
        '[&>p]:mb-3',
        // Lists
        '[&>ul]:list-disc [&>ul]:pl-6 [&>ul]:mb-3 [&>ul]:space-y-1',
        '[&>ol]:list-decimal [&>ol]:pl-6 [&>ol]:mb-3 [&>ol]:space-y-1',
        '[&>li]:font-body-md [&>li]:text-body-md [&>li]:leading-relaxed',
        // Nested lists
        '[&_ul]:list-disc [&_ul]:pl-6 [&_ul]:space-y-1',
        '[&_ol]:list-decimal [&_ol]:pl-6 [&_ol]:space-y-1',
        // Blockquote
        '[&>blockquote]:border-l-4 [&>blockquote]:border-primary/30 [&>blockquote]:pl-4 [&>blockquote]:italic [&>blockquote]:text-on-surface-variant [&>blockquote]:my-4',
        // Links
        '[&_a]:text-primary [&>_a]:underline [&>_a]:underline-offset-2 [&>_a]:hover:text-primary/80',
        // Bold / Italic
        '[&_strong]:font-semibold',
        '[&_em]:italic',
        // Horizontal rule
        '[&>hr]:border-surface-variant [&>hr]:my-6',
        // Code
        '[&_code]:bg-surface-container-low [&>_code]:px-1.5 [&>_code]:py-0.5 [&>_code]:rounded [&>_code]:font-mono [&>_code]:text-sm',
        className,
      )}
      dangerouslySetInnerHTML={{ __html: html }}
    />
  )
}
