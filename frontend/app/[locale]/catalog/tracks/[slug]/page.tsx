import Link from "next/link";
import { notFound } from "next/navigation";
import { defaultLocale, getDictionary, isLocale } from "@/lib/i18n";
import { ApiRequestError, getPublishedTrackDetail } from "@/lib/api";

export default async function TrackDetailPage({
  params
}: {
  params: Promise<{ locale: string; slug: string }>;
}) {
  const { locale: rawLocale, slug } = await params;
  const locale = isLocale(rawLocale) ? rawLocale : defaultLocale;
  const t = getDictionary(locale);

  let track;
  try {
    track = await getPublishedTrackDetail(slug);
  } catch (error) {
    if (error instanceof ApiRequestError && error.info.status === 404) {
      notFound();
    }
    throw error;
  }

  return (
    <main className="cf-container">
      <p>
        <Link href={`/${locale}/catalog`}>&larr; {t.courseDetail.back}</Link>
      </p>

      <span className="badge">{t.catalog.trackBadge}</span>
      <h1>{track.title}</h1>
      <p className="price">
        {track.price} {track.currency}
      </p>

      {track.description && (
        <div className="card" style={{ marginBottom: "1.5rem" }}>
          <h2>{t.courseDetail.about}</h2>
          <p>{track.description}</p>
        </div>
      )}

      <div className="card" style={{ marginBottom: "1.5rem" }}>
        <h2>{t.catalog.coursesHeading}</h2>
        <ul>
          {track.courses.map((course) => (
            <li key={course.courseId}>
              <Link href={`/${locale}/catalog/courses/${course.courseSlug}`}>
                {course.courseTitle}
              </Link>{" "}
              <span className="muted">
                ({course.coursePrice} {track.currency})
              </span>
            </li>
          ))}
        </ul>
      </div>

      {track.isBundleEnrollable ? (
        <Link
          className="btn"
          href={`/${locale}/enroll?trackId=${track.id}&name=${encodeURIComponent(track.title)}&price=${track.price}&currency=${track.currency}`}
        >
          {t.courseDetail.enrollInBatch}
        </Link>
      ) : (
        <div className="notice">{t.courseDetail.noBatches}</div>
      )}
    </main>
  );
}
