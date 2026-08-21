// §5: loading is a designed state. Same idiom as dashboard/skeletons.tsx — the
// footprint below matches a real certificate card's anatomy so the layout doesn't
// jump when the data lands. In-page rather than a route-level loading.tsx for the
// same reason as the dashboard: data arrives from a useEffect, so a route loading
// file would resolve before the real wait began.
export function CertificateSkeleton() {
  return (
    <div className="flex flex-col gap-4" aria-hidden="true">
      {Array.from({ length: 2 }).map((_, index) => (
        <div
          key={index}
          className="flex flex-col gap-3 rounded-card border border-border bg-surface p-5"
        >
          <div className="flex items-start justify-between gap-2">
            <div className="h-5 w-48 max-w-full animate-pulse rounded-control bg-surface-2" />
            <div className="h-6 w-20 animate-pulse rounded-pill bg-surface-2" />
          </div>
          <div className="h-4 w-36 max-w-full animate-pulse rounded-control bg-surface-2" />
          <div className="h-4 w-56 max-w-full animate-pulse rounded-control bg-surface-2" />
          <div className="h-4 w-40 max-w-full animate-pulse rounded-control bg-surface-2" />
          <div className="flex gap-2 pt-1">
            <div className="h-8 w-20 animate-pulse rounded-control bg-surface-2" />
            <div className="h-8 w-20 animate-pulse rounded-control bg-surface-2" />
          </div>
        </div>
      ))}
    </div>
  );
}
