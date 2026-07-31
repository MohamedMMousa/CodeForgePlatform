import Link from "next/link";
import { defaultLocale, format, getDictionary, isLocale } from "@/lib/i18n";
import { getPublishedCourses, getPublishedTracks } from "@/lib/api";

const PAGE_SIZE = 20;

export default async function CatalogPage({
  params,
  searchParams
}: {
  params: Promise<{ locale: string }>;
  searchParams: Promise<{ q?: string; coursesPage?: string; tracksPage?: string }>;
}) {
  const { locale: rawLocale } = await params;
  const { q, coursesPage: coursesPageParam, tracksPage: tracksPageParam } = await searchParams;
  const locale = isLocale(rawLocale) ? rawLocale : defaultLocale;
  const t = getDictionary(locale).catalog;
  const tp = getDictionary(locale).pagination;

  const coursesPage = Math.max(1, Number(coursesPageParam) || 1);
  const tracksPage = Math.max(1, Number(tracksPageParam) || 1);

  let courses: Awaited<ReturnType<typeof getPublishedCourses>> = {
    items: [],
    page: 1,
    pageSize: PAGE_SIZE,
    totalCount: 0
  };
  let tracks: Awaited<ReturnType<typeof getPublishedTracks>> = {
    items: [],
    page: 1,
    pageSize: PAGE_SIZE,
    totalCount: 0
  };
  let loadError = false;

  try {
    [courses, tracks] = await Promise.all([
      getPublishedCourses({ search: q, page: coursesPage, pageSize: PAGE_SIZE }),
      getPublishedTracks({ search: q, page: tracksPage, pageSize: PAGE_SIZE })
    ]);
  } catch {
    loadError = true;
  }

  const pageHref = (overrides: { coursesPage?: number; tracksPage?: number }) => {
    const query = new URLSearchParams();
    if (q) query.set("q", q);
    const nextCoursesPage = overrides.coursesPage ?? coursesPage;
    const nextTracksPage = overrides.tracksPage ?? tracksPage;
    if (nextCoursesPage > 1) query.set("coursesPage", String(nextCoursesPage));
    if (nextTracksPage > 1) query.set("tracksPage", String(nextTracksPage));
    const qs = query.toString();
    return `/${locale}/catalog${qs ? `?${qs}` : ""}`;
  };

  const coursesTotalPages = Math.max(1, Math.ceil(courses.totalCount / courses.pageSize));
  const tracksTotalPages = Math.max(1, Math.ceil(tracks.totalCount / tracks.pageSize));

  return (
    <main className="container">
      <h1>{t.title}</h1>
      <p className="muted">{t.subtitle}</p>

      <form style={{ margin: "1.25rem 0" }}>
        <div className="field" style={{ maxWidth: 420 }}>
          <input type="search" name="q" placeholder={t.searchPlaceholder} defaultValue={q ?? ""} />
        </div>
      </form>

      {loadError && <div className="notice err">{t.loadError}</div>}

      {!loadError && tracks.items.length > 0 && (
        <section style={{ marginBottom: "2rem" }}>
          <h2>{t.tracksHeading}</h2>
          <div className="grid">
            {tracks.items.map((track) => (
              <Link
                key={track.id}
                href={`/${locale}/catalog/tracks/${track.slug}`}
                className="card"
              >
                <span className="badge">{t.trackBadge}</span>
                <h3>{track.title}</h3>
                {track.description && <p className="muted">{track.description}</p>}
                <p className="muted">
                  {t.coursesInTrack.replace("{count}", String(track.courseCount))}
                </p>
                <p className="price">
                  {track.price} {track.currency}
                </p>
              </Link>
            ))}
          </div>
          {tracksTotalPages > 1 && (
            <div className="pagination">
              <span className="muted">
                {format(tp.showingCount, {
                  count: `${(tracksPage - 1) * tracks.pageSize + 1}-${Math.min(tracksPage * tracks.pageSize, tracks.totalCount)}`,
                  total: tracks.totalCount
                })}
              </span>
              <div className="pagination-controls">
                <Link
                  href={pageHref({ tracksPage: tracksPage - 1 })}
                  className="btn secondary"
                  aria-disabled={tracksPage <= 1}
                  tabIndex={tracksPage <= 1 ? -1 : undefined}
                >
                  {tp.previous}
                </Link>
                <span className="muted">{format(tp.pageOf, { page: tracksPage, totalPages: tracksTotalPages })}</span>
                <Link
                  href={pageHref({ tracksPage: tracksPage + 1 })}
                  className="btn secondary"
                  aria-disabled={tracksPage >= tracksTotalPages}
                  tabIndex={tracksPage >= tracksTotalPages ? -1 : undefined}
                >
                  {tp.next}
                </Link>
              </div>
            </div>
          )}
        </section>
      )}

      {!loadError && (
        <section>
          <h2>{t.coursesHeading}</h2>
          {courses.items.length === 0 ? (
            <p className="muted">{t.empty}</p>
          ) : (
            <>
              <div className="grid">
                {courses.items.map((course) => (
                  <Link
                    key={course.id}
                    href={`/${locale}/catalog/courses/${course.slug}`}
                    className="card"
                  >
                    <h3>{course.title}</h3>
                    {course.description && <p className="muted">{course.description}</p>}
                    <p className="price">
                      {course.price} {course.currency}
                    </p>
                  </Link>
                ))}
              </div>
              {coursesTotalPages > 1 && (
                <div className="pagination">
                  <span className="muted">
                    {format(tp.showingCount, {
                      count: `${(coursesPage - 1) * courses.pageSize + 1}-${Math.min(coursesPage * courses.pageSize, courses.totalCount)}`,
                      total: courses.totalCount
                    })}
                  </span>
                  <div className="pagination-controls">
                    <Link
                      href={pageHref({ coursesPage: coursesPage - 1 })}
                      className="btn secondary"
                      aria-disabled={coursesPage <= 1}
                      tabIndex={coursesPage <= 1 ? -1 : undefined}
                    >
                      {tp.previous}
                    </Link>
                    <span className="muted">{format(tp.pageOf, { page: coursesPage, totalPages: coursesTotalPages })}</span>
                    <Link
                      href={pageHref({ coursesPage: coursesPage + 1 })}
                      className="btn secondary"
                      aria-disabled={coursesPage >= coursesTotalPages}
                      tabIndex={coursesPage >= coursesTotalPages ? -1 : undefined}
                    >
                      {tp.next}
                    </Link>
                  </div>
                </div>
              )}
            </>
          )}
        </section>
      )}
    </main>
  );
}
