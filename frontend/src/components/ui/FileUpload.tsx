import { useCallback, useRef, useState } from 'react'
import { uploadFile } from '../../api/fileUpload.ts'
import { cn } from '../../lib/cn'

interface FileUploadProps {
  /** "avatar" for profile pictures (images), "resume" for PDFs */
  fileType: 'avatar' | 'resume'
  /** Current value (URL for avatars, key for resumes) */
  value?: string
  /** Called when upload completes with the new URL/key */
  onChange: (url: string) => void
  /** Called when upload fails */
  onError?: (error: string) => void
  /** Optional label */
  label?: string
  /** Optional hint text */
  hint?: string
  /** Whether the upload is disabled */
  disabled?: boolean
  /** Optional class name */
  className?: string
}

export default function FileUpload({
  fileType,
  value,
  onChange,
  onError,
  label,
  hint,
  disabled = false,
  className,
}: FileUploadProps) {
  const [isDragging, setIsDragging] = useState(false)
  const [isUploading, setIsUploading] = useState(false)
  const [preview, setPreview] = useState<string | null>(null)
  const [fileName, setFileName] = useState<string | null>(null)
  const inputRef = useRef<HTMLInputElement>(null)

  const acceptedTypes = fileType === 'avatar'
    ? 'image/jpeg,image/png,image/webp,image/gif'
    : 'application/pdf'

  const maxSizeMB = fileType === 'avatar' ? 5 : 10

  const handleFile = useCallback(async (file: File) => {
    if (file.size > maxSizeMB * 1024 * 1024) {
      onError?.(`File size must be less than ${maxSizeMB}MB`)
      return
    }

    if (fileType === 'avatar' && !file.type.startsWith('image/')) {
      onError?.('Please upload an image file')
      return
    }
    if (fileType === 'resume' && file.type !== 'application/pdf') {
      onError?.('Please upload a PDF file')
      return
    }

    if (fileType === 'avatar') {
      const reader = new FileReader()
      reader.onload = (e) => setPreview(e.target?.result as string)
      reader.readAsDataURL(file)
    } else {
      setFileName(file.name)
    }

    setIsUploading(true)
    try {
      const url = await uploadFile(file, fileType)
      onChange(url)
    } catch (err) {
      onError?.(err instanceof Error ? err.message : 'Upload failed')
      setPreview(null)
      setFileName(null)
    } finally {
      setIsUploading(false)
    }
  }, [fileType, onChange, onError, maxSizeMB])

  const handleDrop = useCallback((e: React.DragEvent) => {
    e.preventDefault()
    setIsDragging(false)
    const file = e.dataTransfer.files[0]
    if (file) handleFile(file)
  }, [handleFile])

  const handleDragOver = useCallback((e: React.DragEvent) => {
    e.preventDefault()
    setIsDragging(true)
  }, [])

  const handleDragLeave = useCallback((e: React.DragEvent) => {
    e.preventDefault()
    setIsDragging(false)
  }, [])

  const handleChange = useCallback((e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0]
    if (file) handleFile(file)
  }, [handleFile])

  const handleClick = useCallback(() => {
    inputRef.current?.click()
  }, [])

  const handleRemove = useCallback(() => {
    setPreview(null)
    setFileName(null)
    onChange('')
    if (inputRef.current) inputRef.current.value = ''
  }, [onChange])

  const currentImage = preview || (value && value.startsWith('http') ? value : null)
  const hasExistingValue = !!value && !preview && !fileName

  // ────── AVATAR MODE: circular profile picture with edit overlay ──────
  if (fileType === 'avatar') {
    return (
      <div className={cn('w-full', className)}>
        {label && (
          <label className="font-label-sm text-label-sm font-medium text-on-surface">
            {label}
          </label>
        )}

        <div className="mt-1.5 flex items-center gap-5">
          {/* Circular avatar preview */}
          <div className="relative shrink-0">
            <div
              onClick={handleClick}
              className={cn(
                'h-24 w-24 rounded-full border-2 border-surface-variant overflow-hidden bg-surface-container-low flex items-center justify-center cursor-pointer transition-all',
                isDragging && 'border-primary',
                !currentImage && !hasExistingValue && 'border-dashed',
              )}
            >
              {isUploading ? (
                <span className="h-6 w-6 animate-spin rounded-full border-2 border-primary/30 border-t-primary" />
              ) : currentImage ? (
                <img
                  src={currentImage}
                  alt="Profile picture"
                  className="h-full w-full object-cover"
                />
              ) : hasExistingValue ? (
                <img
                  src={value}
                  alt="Profile picture"
                  className="h-full w-full object-cover"
                />
              ) : (
                <span className="material-symbols-outlined text-3xl text-on-surface-variant/40">
                  person
                </span>
              )}
            </div>

            {/* Edit overlay icon */}
            {!isUploading && (
              <button
                type="button"
                onClick={handleClick}
                className="absolute bottom-0 right-0 flex h-8 w-8 items-center justify-center rounded-full bg-primary text-on-primary shadow-md transition-transform hover:scale-110"
              >
                <span className="material-symbols-outlined text-[16px]">edit</span>
              </button>
            )}

            {/* Remove button */}
            {(currentImage || hasExistingValue) && !isUploading && (
              <button
                type="button"
                onClick={(e) => { e.stopPropagation(); handleRemove() }}
                className="absolute top-0 right-0 flex h-6 w-6 items-center justify-center rounded-full bg-surface-container-high text-on-surface-variant shadow-sm transition-colors hover:bg-error-container hover:text-error"
              >
                <span className="material-symbols-outlined text-[14px]">close</span>
              </button>
            )}
          </div>

          {/* Upload text + hidden input */}
          <div className="flex flex-col gap-1">
            <input
              ref={inputRef}
              type="file"
              accept={acceptedTypes}
              onChange={handleChange}
              disabled={disabled || isUploading}
              className="hidden"
            />
            <button
              type="button"
              onClick={handleClick}
              disabled={disabled || isUploading}
              className="inline-flex items-center gap-1.5 rounded-lg border border-outline-variant bg-surface-container-lowest px-4 py-2 font-label-md text-label-md text-on-surface transition-colors hover:bg-surface-container-low disabled:opacity-50"
            >
              <span className="material-symbols-outlined text-[18px]">upload</span>
              {currentImage || hasExistingValue ? 'Change photo' : 'Upload photo'}
            </button>
            <p className="font-label-sm text-label-sm text-on-surface-variant/60">
              JPEG, PNG, WebP, or GIF · Max {maxSizeMB}MB
            </p>
          </div>
        </div>

        {hint && (
          <p className="mt-1.5 font-label-sm text-label-sm text-on-surface-variant/70">
            {hint}
          </p>
        )}
      </div>
    )
  }

  // ────── RESUME MODE: dashed upload zone ──────
  return (
    <div className={cn('w-full flex flex-col', className)}>
      {label && (
        <label className="font-label-sm text-label-sm font-medium text-on-surface">
          {label}
        </label>
      )}

      <div
        onDrop={handleDrop}
        onDragOver={handleDragOver}
        onDragLeave={handleDragLeave}
        onClick={handleClick}
        className={cn(
          'relative mt-1.5 flex-1 flex flex-col items-center justify-center rounded-lg border-2 border-dashed p-6 transition-colors cursor-pointer',
          isDragging
            ? 'border-primary bg-primary/5'
            : 'border-outline-variant hover:border-primary/50 hover:bg-surface-container-low',
          disabled && 'pointer-events-none opacity-50',
          isUploading && 'pointer-events-none opacity-70',
        )}
      >
        <input
          ref={inputRef}
          type="file"
          accept={acceptedTypes}
          onChange={handleChange}
          disabled={disabled || isUploading}
          className="hidden"
        />

        {isUploading ? (
          <div className="flex flex-col items-center gap-2">
            <span className="h-8 w-8 animate-spin rounded-full border-2 border-primary/30 border-t-primary" />
            <span className="font-label-sm text-label-sm text-on-surface-variant">
              Uploading...
            </span>
          </div>
        ) : fileName || hasExistingValue ? (
          <div className="flex flex-col items-center gap-2">
            <span className="material-symbols-outlined text-2xl text-on-surface-variant">
              description
            </span>
            <span className="font-label-sm text-label-sm text-on-surface-variant">
              {fileName || 'Resume uploaded'}
            </span>
            <span className="font-label-sm text-label-sm text-on-surface-variant">
              Click to change
            </span>
          </div>
        ) : (
          <div className="flex flex-col items-center gap-2">
            <span className="material-symbols-outlined text-2xl text-on-surface-variant">
              upload_file
            </span>
            <span className="font-label-sm text-label-sm text-on-surface-variant">
              Click or drag to upload PDF
            </span>
            <span className="font-label-sm text-label-sm text-on-surface-variant/60">
              Max {maxSizeMB}MB
            </span>
          </div>
        )}

        {/* Remove button */}
        {(fileName || hasExistingValue) && !isUploading && (
          <button
            type="button"
            onClick={(e) => { e.stopPropagation(); handleRemove() }}
            className="absolute top-2 right-2 rounded-full bg-surface-container-high p-1 text-on-surface-variant transition-colors hover:bg-error-container hover:text-error"
          >
            <span className="material-symbols-outlined text-sm">close</span>
          </button>
        )}
      </div>

      {hint && (
        <p className="mt-1.5 font-label-sm text-label-sm text-on-surface-variant/70">
          {hint}
        </p>
      )}
    </div>
  )
}
