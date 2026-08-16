// §5: loading is a designed state, matching the real anatomy of each page so
// layout doesn't jump when data lands. In-page rather than a route
// `loading.tsx` — both pages are client components whose data arrives from a
// `useEffect` (same reasoning as dashboard/skeletons.tsx).

export function CourseContentSkeleton() {
  return (
    <div className="flex flex-col gap-8" aria-hidden="true">
      <div className="h-8 w-72 max-w-full animate-pulse rounded-control bg-surface-2" />
      <div className="flex gap-8">
        <div className="hidden w-56 shrink-0 flex-col gap-2 sm:flex">
          <div className="h-4 w-20 animate-pulse rounded-control bg-surface-2" />
          {Array.from({ length: 3 }).map((_, index) => (
            <div key={index} className="h-8 w-full animate-pulse rounded-control bg-surface-2" />
          ))}
        </div>
        <div className="flex min-w-0 flex-1 flex-col gap-6">
          {Array.from({ length: 2 }).map((_, moduleIndex) => (
            <div key={moduleIndex} className="flex flex-col gap-3">
              <div className="h-6 w-48 max-w-full animate-pulse rounded-control bg-surface-2" />
              {Array.from({ length: 2 }).map((_, sessionIndex) => (
                <div
                  key={sessionIndex}
                  className="flex flex-col gap-2 rounded-card border border-border bg-surface p-4"
                >
                  <div className="h-5 w-20 animate-pulse rounded-pill bg-surface-2" />
                  <div className="h-5 w-56 max-w-full animate-pulse rounded-control bg-surface-2" />
                  <div className="h-4 w-40 max-w-full animate-pulse rounded-control bg-surface-2" />
                </div>
              ))}
            </div>
          ))}
        </div>
      </div>
    </div>
  );
}

export function SessionDetailSkeleton() {
  return (
    <div className="flex flex-col gap-6" aria-hidden="true">
      <div className="h-4 w-40 animate-pulse rounded-control bg-surface-2" />
      <div className="flex flex-col items-start gap-4 rounded-card border border-border bg-surface p-6">
        <div className="h-6 w-24 animate-pulse rounded-pill bg-surface-2" />
        <div className="h-7 w-72 max-w-full animate-pulse rounded-control bg-surface-2" />
        <div className="h-5 w-52 max-w-full animate-pulse rounded-control bg-surface-2" />
        <div className="h-10 w-40 animate-pulse rounded-control bg-surface-2" />
      </div>
      <div className="flex flex-col gap-3">
        <div className="h-6 w-32 animate-pulse rounded-control bg-surface-2" />
        <div className="h-16 w-full animate-pulse rounded-card bg-surface-2" />
      </div>
    </div>
  );
}
