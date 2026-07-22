import Link from "next/link";
import { defaultLocale, getDictionary, isLocale } from "@/lib/i18n";
import { getPublishedCourses, getPublishedTracks } from "@/lib/api";

export default async function CatalogPage({
  params,
  searchParams
}: {
  params: Promise<{ locale: string }>;
  searchParams: Promise<{ q?: string }>;
}) {
  const { locale: rawLocale } = await params;
  const { q } = await searchParams;
  const locale = isLocale(rawLocale) ? rawLocale : defaultLocale;
  const t = getDictionary(locale).catalog;

  let courses: Awaited<ReturnType<typeof getPublishedCourses>> = [];
  let tracks: Awaited<ReturnType<typeof getPublishedTracks>> = [];
  let loadError = false;

  try {
    [courses, tracks] = await Promise.all([
      getPublishedCourses({ search: q }),
      getPublishedTracks({ search: q })
    ]);
  } catch {
    loadError = true;
  }

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

      {!loadError && tracks.length > 0 && (
        <section style={{ marginBottom: "2rem" }}>
          <h2>{t.tracksHeading}</h2>
          <div className="grid">
            {tracks.map((track) => (
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
        </section>
      )}

      {!loadError && (
        <section>
          <h2>{t.coursesHeading}</h2>
          {courses.length === 0 ? (
            <p className="muted">{t.empty}</p>
          ) : (
            <div className="grid">
              {courses.map((course) => (
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
          )}
        </section>
      )}
    </main>
  );
}
