interface BarDatum {
  label: string
  value: number
}

interface BarChartProps {
  data: BarDatum[]
  /** Pixel height of the plot area. */
  height?: number
  /** Bar fill color (Tailwind color token or CSS color). */
  color?: string
  /** Value formatter for tooltips / labels. */
  formatValue?: (v: number) => string
}

/**
 * Pure SVG vertical bar chart with hover tooltips and value labels.
 * No external charting library — same approach as PieChart.
 * Long labels (e.g. "2026-08") are thinned out on narrow sets so they
 * never overlap; small sets (top-jobs) show every label truncated.
 */
export default function BarChart({
  data,
  height = 180,
  color = 'var(--color-primary, #4f46e5)',
  formatValue = (v) => v.toLocaleString(),
}: BarChartProps) {
  if (data.length === 0 || data.every((d) => d.value === 0)) {
    return (
      <div
        className="flex items-center justify-center rounded-lg border-2 border-dashed border-surface-variant"
        style={{ height }}
      >
        <span className="font-label-sm text-label-sm text-on-surface-variant">No data</span>
      </div>
    )
  }

  const max = Math.max(...data.map((d) => d.value))
  const barGap = data.length > 20 ? 0.6 : 1.5

  // Thin x labels when there are too many to fit (e.g. 30 days).
  const labelEvery = data.length > 16 ? Math.ceil(data.length / 8) : 1
  const isShortLabels = data.every((d) => d.label.length <= 12)

  return (
    <div className="w-full">
      <div className="flex items-end" style={{ height }}>
        {data.map((d, i) => {
          const h = max === 0 ? 0 : (d.value / max) * (height - 24)
          return (
            <div
              key={d.label + i}
              className="group relative flex flex-1 flex-col items-center justify-end"
              style={{ marginRight: i < data.length - 1 ? barGap : 0 }}
            >
              {/* Tooltip */}
              <div className="pointer-events-none absolute bottom-full z-10 mb-1 hidden whitespace-nowrap rounded-md bg-surface-inverse px-2 py-1 font-label-xs text-[11px] text-on-surface-inverse shadow-md group-hover:block">
                <span className="font-semibold">{formatValue(d.value)}</span>
                {' · '}
                {d.label}
              </div>
              {/* Value on hover position — shown above the bar */}
              <div
                className="w-full rounded-t-sm transition-all group-hover:opacity-100"
                style={{
                  height: Math.max(h, d.value > 0 ? 2 : 0),
                  background: color,
                  opacity: 0.85,
                }}
                title={`${d.label}: ${formatValue(d.value)}`}
              />
            </div>
          )
        })}
      </div>
      {/* X axis labels */}
      <div className="mt-1 flex">
        {data.map((d, i) => (
          <div
            key={d.label + i}
            className="flex-1 overflow-hidden text-center font-label-xs text-[9px] text-on-surface-variant"
            style={{ marginRight: i < data.length - 1 ? barGap : 0 }}
          >
            {i % labelEvery === 0 ? (
              isShortLabels ? (
                <span className="block truncate">{d.label}</span>
              ) : (
                <span className="block">{d.label.slice(2)}</span> // "2026-08" → "26-08"
              )
            ) : (
              '\u00A0'
            )}
          </div>
        ))}
      </div>
    </div>
  )
}
