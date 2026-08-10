interface SubmitButtonProps {
  loading: boolean
  loadingLabel: string
  idleLabel: string
}

/** Primary accent CTA for auth forms — shows an inline spinner while pending. */
export default function SubmitButton({
  loading,
  loadingLabel,
  idleLabel,
}: SubmitButtonProps) {
  return (
    <button
      type="submit"
      disabled={loading}
      className="flex w-full items-center justify-center gap-2 rounded-xl bg-accent py-3 font-label-md text-label-md font-semibold text-on-accent shadow-sm transition-all duration-200 hover:opacity-90 focus:outline-none focus:ring-2 focus:ring-accent/40 focus:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-70"
    >
      {loading && (
        <span
          aria-hidden="true"
          className="h-4 w-4 animate-spin rounded-full border-2 border-on-accent/40 border-t-on-accent"
        />
      )}
      {loading ? loadingLabel : idleLabel}
    </button>
  )
}
