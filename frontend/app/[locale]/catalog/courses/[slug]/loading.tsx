// Route-level loading state (§5: every state is designed, not incidental).
//
// This file exists because catalog/loading.tsx — a six-card GRID skeleton — also
// covers this nested route, so without it the detail page flashes a catalog-shaped
// placeholder before resolving into something completely different. The footprint
// below matches this page's real anatomy: back link, header, enrollment panel,
// overview, then a two-column cohort grid.
export default function CourseDetailLoading() {
  return (
    <main className="mx-auto flex w-full max-w-4xl flex-col gap-10 ps-5 pe-5 py-10">
      <div className="h-4 w-32 animate-pulse rounded-control bg-surface-2" />

      <div className="flex flex-col gap-3">
        <div className="h-3 w-24 animate-pulse rounded-control bg-surface-2" />
        <div className="h-9 w-80 max-w-full animate-pulse rounded-control bg-surface-2" />
        <div className="h-7 w-32 animate-pulse rounded-control bg-surface-2" />
      </div>

      <div className="flex flex-col gap-4 rounded-card border border-border bg-surface p-6">
        <div className="h-12 w-56 max-w-full animate-pulse rounded-control bg-surface-2" />
        <div className="h-4 w-40 animate-pulse rounded-control bg-surface-2" />
        <div className="h-4 w-48 animate-pulse rounded-control bg-surface-2" />
      </div>

      <div className="flex flex-col gap-3">
        <div className="h-6 w-48 animate-pulse rounded-control bg-surface-2" />
        <div className="h-4 w-full animate-pulse rounded-control bg-surface-2" />
        <div className="h-4 w-full animate-pulse rounded-control bg-surface-2" />
        <div className="h-4 w-2/3 animate-pulse rounded-control bg-surface-2" />
      </div>

      <div className="flex flex-col gap-4">
        <div className="h-6 w-40 animate-pulse rounded-control bg-surface-2" />
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
          {Array.from({ length: 2 }).map((_, index) => (
            <div
              key={index}
              className="flex flex-col gap-3 rounded-card border border-border bg-surface p-5"
            >
              <div className="flex items-start justify-between gap-2">
                <div className="h-5 w-32 animate-pulse rounded-control bg-surface-2" />
                <div className="h-5 w-24 animate-pulse rounded-pill bg-surface-2" />
              </div>
              <div className="h-4 w-40 animate-pulse rounded-control bg-surface-2" />
              <div className="h-4 w-36 animate-pulse rounded-control bg-surface-2" />
              <div className="h-4 w-44 animate-pulse rounded-control bg-surface-2" />
            </div>
          ))}
        </div>
      </div>
    </main>
  );
}
