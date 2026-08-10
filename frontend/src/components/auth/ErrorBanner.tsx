/** Inline error banner shown above auth forms on failed submissions. */
export default function ErrorBanner({ message }: { message: string }) {
  return (
    <div
      role="alert"
      className="mb-5 flex items-start gap-2.5 rounded-xl border border-error/30 bg-error-container p-3.5 font-label-md text-label-md text-on-error-container"
    >
      <span
        aria-hidden="true"
        className="material-symbols-outlined mt-0.5 text-lg"
      >
        error
      </span>
      <span>{message}</span>
    </div>
  )
}
