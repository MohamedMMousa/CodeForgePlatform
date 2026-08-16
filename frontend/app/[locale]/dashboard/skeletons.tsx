// §5: loading is a designed state. The footprint below matches the real page
// anatomy — focal session panel, then a two-up course grid — so the layout does
// not jump when the data lands.
//
// Deliberately in-page rather than a route-level loading.tsx: this page is a
// client component whose data arrives from a useEffect, so a route loading file
// would resolve the moment the component mounted and vanish before the actual
// wait began.
export function DashboardSkeleton() {
  return (
    <div className="flex flex-col gap-10" aria-hidden="true">
      <div className="flex flex-col gap-4">
        <div className="h-6 w-40 animate-pulse rounded-control bg-surface-2" />
        <div className="flex flex-col gap-4 rounded-card border border-border bg-surface p-6">
          <div className="h-6 w-24 animate-pulse rounded-pill bg-surface-2" />
          <div className="h-5 w-64 max-w-full animate-pulse rounded-control bg-surface-2" />
          <div className="h-4 w-48 max-w-full animate-pulse rounded-control bg-surface-2" />
          <div className="h-7 w-72 max-w-full animate-pulse rounded-control bg-surface-2" />
          <div className="h-12 w-40 animate-pulse rounded-control bg-surface-2" />
        </div>
      </div>

      <div className="flex flex-col gap-4">
        <div className="h-6 w-32 animate-pulse rounded-control bg-surface-2" />
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
          {Array.from({ length: 2 }).map((_, index) => (
            <div
              key={index}
              className="flex flex-col gap-3 rounded-card border border-border bg-surface p-5"
            >
              <div className="h-5 w-40 max-w-full animate-pulse rounded-control bg-surface-2" />
              <div className="h-4 w-full animate-pulse rounded-control bg-surface-2" />
              <div className="h-4 w-36 animate-pulse rounded-control bg-surface-2" />
              <div className="h-8 w-32 animate-pulse rounded-control bg-surface-2" />
            </div>
          ))}
        </div>
      </div>
    </div>
  );
}
