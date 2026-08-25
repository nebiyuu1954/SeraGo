interface UnsavedChangesDialogProps {
  open: boolean
  onSave: () => void
  onDiscard: () => void
  onCancel: () => void
  saving?: boolean
}

export default function UnsavedChangesDialog({
  open,
  onSave,
  onDiscard,
  onCancel,
  saving = false,
}: UnsavedChangesDialogProps) {
  if (!open) return null

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center">
      {/* Backdrop */}
      <div
        className="absolute inset-0 bg-inverse-surface/40 backdrop-blur-sm"
        onClick={onCancel}
      />

      {/* Dialog */}
      <div className="relative z-10 w-full max-w-md rounded-xl border border-surface-variant bg-surface-container-lowest p-6 shadow-xl">
        <h2 className="font-headline-md text-headline-md text-on-surface mb-2">
          Unsaved changes
        </h2>
        <p className="font-body-md text-body-md text-on-surface-variant mb-6">
          You have unsaved changes. Would you like to save them before leaving?
        </p>

        <div className="flex items-center justify-end gap-3">
          <button
            type="button"
            onClick={onCancel}
            className="px-4 py-2 rounded-lg font-label-md text-label-md text-on-surface-variant border border-outline-variant hover:bg-surface-container-low transition-colors"
          >
            Cancel
          </button>
          <button
            type="button"
            onClick={onDiscard}
            className="px-4 py-2 rounded-lg font-label-md text-label-md text-error border border-error/30 hover:bg-error-container/30 transition-colors"
          >
            Discard
          </button>
          <button
            type="button"
            onClick={onSave}
            disabled={saving}
            className="inline-flex items-center gap-2 px-4 py-2 rounded-lg font-label-md text-label-md font-medium text-on-accent bg-accent hover:opacity-90 transition-opacity disabled:pointer-events-none disabled:opacity-60"
          >
            {saving && (
              <span
                aria-hidden="true"
                className="h-4 w-4 animate-spin rounded-full border-2 border-on-accent/40 border-t-on-accent"
              />
            )}
            Save changes
          </button>
        </div>
      </div>
    </div>
  )
}
