interface PieSlice {
  label: string
  value: number
  color: string
}

interface PieChartProps {
  slices: PieSlice[]
  size?: number
}

/**
 * Pure SVG pie chart with a center label and a legend below.
 * No external charting library — just SVG path math.
 */
export default function PieChart({ slices, size = 120 }: PieChartProps) {
  const total = slices.reduce((sum, s) => sum + s.value, 0)
  if (total === 0) {
    return (
      <div className="flex flex-col items-center gap-3">
        <div
          className="rounded-full border-2 border-dashed border-surface-variant"
          style={{ width: size, height: size }}
        />
        <span className="font-label-sm text-label-sm text-on-surface-variant">No data</span>
      </div>
    )
  }

  const cx = size / 2
  const cy = size / 2
  const r = size / 2 - 2 // small gap from edge

  // Build arc paths
  let cumulative = 0
  const paths = slices.map((slice) => {
    const startAngle = (cumulative / total) * 2 * Math.PI - Math.PI / 2
    cumulative += slice.value
    const endAngle = (cumulative / total) * 2 * Math.PI - Math.PI / 2

    const x1 = cx + r * Math.cos(startAngle)
    const y1 = cy + r * Math.sin(startAngle)
    const x2 = cx + r * Math.cos(endAngle)
    const y2 = cy + r * Math.sin(endAngle)

    // Use large-arc-flag when the slice is > 50%
    const largeArc = slice.value / total > 0.5 ? 1 : 0

    const d = `M ${cx} ${cy} L ${x1} ${y1} A ${r} ${r} 0 ${largeArc} 1 ${x2} ${y2} Z`

    return { ...slice, d }
  })

  return (
    <div className="flex flex-col items-center gap-3">
      <svg width={size} height={size} viewBox={`0 0 ${size} ${size}`}>
        {paths.map((p) => (
          <path key={p.label} d={p.d} fill={p.color} stroke="var(--color-surface-container-lowest, #fff)" strokeWidth="1.5" />
        ))}
        {/* Center circle for donut effect */}
        <circle cx={cx} cy={cy} r={r * 0.55} fill="var(--color-surface-container-lowest, #fff)" />
        {/* Total label in center */}
        <text x={cx} y={cy - 4} textAnchor="middle" className="fill-on-surface" fontSize="18" fontWeight="bold" fontFamily="inherit">
          {total.toLocaleString()}
        </text>
        <text x={cx} y={cy + 12} textAnchor="middle" className="fill-on-surface-variant" fontSize="10" fontFamily="inherit">
          total
        </text>
      </svg>

      {/* Legend */}
      <div className="flex flex-wrap justify-center gap-x-4 gap-y-1">
        {slices.map((s) => (
          <div key={s.label} className="flex items-center gap-1.5">
            <span className="inline-block h-2.5 w-2.5 rounded-full" style={{ backgroundColor: s.color }} />
            <span className="font-label-xs text-label-xs text-on-surface-variant">{s.label}</span>
            <span className="font-label-xs text-label-xs font-medium text-on-surface">{s.value}</span>
          </div>
        ))}
      </div>
    </div>
  )
}
