import { useCallback, useEffect, useState } from 'react'
import { useEditor, EditorContent } from '@tiptap/react'
import StarterKit from '@tiptap/starter-kit'
import Link from '@tiptap/extension-link'
import Placeholder from '@tiptap/extension-placeholder'
import { cn } from '../../lib/cn'

interface RichTextEditorProps {
  /** Current HTML value. */
  value: string
  /** Called when content changes with the new HTML string. */
  onChange: (html: string) => void
  /** Placeholder text shown when empty. */
  placeholder?: string
  /** Minimum height of the editor area. */
  minHeight?: string
  /** Disabled state. */
  disabled?: boolean
  /** Optional class on the outer wrapper. */
  className?: string
}

function ToolbarButton({
  onClick,
  active = false,
  disabled = false,
  children,
  title,
}: {
  onClick: () => void
  active?: boolean
  disabled?: boolean
  children: React.ReactNode
  title: string
}) {
  return (
    <button
      type="button"
      onClick={onClick}
      disabled={disabled}
      title={title}
      className={cn(
        'flex h-8 w-8 items-center justify-center rounded transition-colors',
        active
          ? 'bg-primary/10 text-primary'
          : 'text-on-surface-variant hover:bg-surface-container-low hover:text-on-surface',
        disabled && 'pointer-events-none opacity-40',
      )}
    >
      {children}
    </button>
  )
}

function ToolbarDivider() {
  return <div className="mx-1 h-6 w-px bg-surface-variant" />
}

export default function RichTextEditor({
  value,
  onChange,
  placeholder = 'Start writing…',
  minHeight = '120px',
  disabled = false,
  className,
}: RichTextEditorProps) {
  const editor = useEditor({
    extensions: [
      StarterKit.configure({
        heading: { levels: [1, 2, 3] },
      }),
      Link.configure({
        openOnClick: false,
        HTMLAttributes: { class: 'text-primary underline underline-offset-2 hover:text-primary/80' },
      }),
      Placeholder.configure({ placeholder }),
    ],
    content: value || '',
    editorProps: {
      attributes: {
        class: 'prose prose-sm max-w-none focus:outline-none min-h-[120px] px-4 py-3 font-body-md text-body-md text-on-surface',
        style: `min-height: ${minHeight}`,
      },
    },
    onUpdate: ({ editor: e }) => {
      onChange(e.getHTML())
    },
    editable: !disabled,
  })

  // Sync external value changes (e.g. Formik reset) into the editor.
  useEffect(() => {
    if (editor && value !== editor.getHTML()) {
      editor.commands.setContent(value || '', { emitUpdate: false })
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [value])

  const setLink = useCallback(() => {
    if (!editor) return
    const previousUrl = editor.getAttributes('link').href
    const url = window.prompt('Enter URL', previousUrl || 'https://')
    if (url === null) return // cancelled
    if (url === '') {
      editor.chain().focus().extendMarkRange('link').unsetLink().run()
      return
    }
    editor.chain().focus().extendMarkRange('link').setLink({ href: url }).run()
  }, [editor])

  if (!editor) return null

  const [expanded, setExpanded] = useState(false)

  return (
    <div className={cn('rounded-lg border border-outline-variant bg-surface-container-lowest overflow-hidden', className)}>
      {/* Toolbar */}
      <div className="flex flex-wrap items-center gap-0.5 border-b border-surface-variant px-2 py-1.5">
        {/* Text formatting */}
        <ToolbarButton
          onClick={() => editor.chain().focus().toggleBold().run()}
          active={editor.isActive('bold')}
          title="Bold"
        >
          <span className="material-symbols-outlined text-[18px]">format_bold</span>
        </ToolbarButton>
        <ToolbarButton
          onClick={() => editor.chain().focus().toggleItalic().run()}
          active={editor.isActive('italic')}
          title="Italic"
        >
          <span className="material-symbols-outlined text-[18px]">format_italic</span>
        </ToolbarButton>

        <ToolbarDivider />

        {/* Headings */}
        <ToolbarButton
          onClick={() => editor.chain().focus().toggleHeading({ level: 1 }).run()}
          active={editor.isActive('heading', { level: 1 })}
          title="Heading 1"
        >
          <span className="font-label-md text-label-md font-bold">H1</span>
        </ToolbarButton>
        <ToolbarButton
          onClick={() => editor.chain().focus().toggleHeading({ level: 2 }).run()}
          active={editor.isActive('heading', { level: 2 })}
          title="Heading 2"
        >
          <span className="font-label-md text-label-md font-bold">H2</span>
        </ToolbarButton>
        <ToolbarButton
          onClick={() => editor.chain().focus().toggleHeading({ level: 3 }).run()}
          active={editor.isActive('heading', { level: 3 })}
          title="Heading 3"
        >
          <span className="font-label-md text-label-md font-bold">H3</span>
        </ToolbarButton>

        <ToolbarDivider />

        {/* Lists */}
        <ToolbarButton
          onClick={() => editor.chain().focus().toggleBulletList().run()}
          active={editor.isActive('bulletList')}
          title="Bullet list"
        >
          <span className="material-symbols-outlined text-[18px]">format_list_bulleted</span>
        </ToolbarButton>
        <ToolbarButton
          onClick={() => editor.chain().focus().toggleOrderedList().run()}
          active={editor.isActive('orderedList')}
          title="Numbered list"
        >
          <span className="material-symbols-outlined text-[18px]">format_list_numbered</span>
        </ToolbarButton>

        <ToolbarDivider />

        {/* Block elements */}
        <ToolbarButton
          onClick={() => editor.chain().focus().toggleBlockquote().run()}
          active={editor.isActive('blockquote')}
          title="Quote"
        >
          <span className="material-symbols-outlined text-[18px]">format_quote</span>
        </ToolbarButton>
        <ToolbarButton
          onClick={() => editor.chain().focus().setHorizontalRule().run()}
          title="Horizontal rule"
        >
          <span className="material-symbols-outlined text-[18px]">horizontal_rule</span>
        </ToolbarButton>

        <ToolbarDivider />

        {/* Link */}
        <ToolbarButton
          onClick={setLink}
          active={editor.isActive('link')}
          title="Add link"
        >
          <span className="material-symbols-outlined text-[18px]">link</span>
        </ToolbarButton>
      </div>

      {/* Editor content */}
      <div
        className="overflow-y-auto transition-all duration-200"
        style={{ height: expanded ? '400px' : minHeight }}
      >
        <EditorContent editor={editor} />
      </div>

      {/* Footer: word count + expand toggle */}
      <div className="flex items-center justify-between border-t border-surface-variant px-4 py-1.5">
        <span className="font-label-sm text-label-sm text-on-surface-variant/50">
          {editor.storage.characterCount?.words?.() ?? editor.getText().split(/\s+/).filter(Boolean).length} words
        </span>
        <button
          type="button"
          onClick={() => setExpanded((prev) => !prev)}
          title={expanded ? 'Collapse editor' : 'Expand editor'}
          className="flex items-center gap-1 rounded px-2 py-1 font-label-sm text-label-sm text-on-surface-variant/60 transition-colors hover:bg-surface-container-low hover:text-on-surface-variant"
        >
          <span className="material-symbols-outlined text-[16px]">
            {expanded ? 'compress' : 'expand'}
          </span>
          {expanded ? 'Collapse' : 'Expand'}
        </button>
      </div>
    </div>
  )
}
