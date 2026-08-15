// Route-level loading state (§5: every state is designed, not incidental).
// Skeleton footprint matches CourseCard's real anatomy — header row, title,
// description line, meta line, footer — so the layout doesn't jump on swap.
export default function CatalogLoading() {
  return (
    <main className="mx-auto flex w-full max-w-6xl flex-col gap-10 ps-5 pe-5 py-10">
      <div className="flex flex-col gap-3">
        <div className="h-9 w-64 max-w-full animate-pulse rounded-control bg-surface-2" />
        <div className="h-5 w-96 max-w-full animate-pulse rounded-control bg-surface-2" />
      </div>
      <div className="grid grid-cols-1 gap-5 sm:grid-cols-2 lg:grid-cols-3">
        {Array.from({ length: 6 }).map((_, index) => (
          <div
            key={index}
            className="flex flex-col gap-4 rounded-card border border-border bg-surface p-5"
          >
            <div className="flex items-center justify-between gap-2">
              <div className="h-4 w-20 animate-pulse rounded-control bg-surface-2" />
              <div className="h-5 w-24 animate-pulse rounded-pill bg-surface-2" />
            </div>
            <div className="h-5 w-3/4 animate-pulse rounded-control bg-surface-2" />
            <div className="h-4 w-full animate-pulse rounded-control bg-surface-2" />
            <div className="h-4 w-1/2 animate-pulse rounded-control bg-surface-2" />
            <div className="mt-2 flex items-center justify-between border-t border-border pt-4">
              <div className="h-5 w-16 animate-pulse rounded-control bg-surface-2" />
              <div className="h-9 w-24 animate-pulse rounded-control bg-surface-2" />
            </div>
          </div>
        ))}
      </div>
    </main>
  );
}
