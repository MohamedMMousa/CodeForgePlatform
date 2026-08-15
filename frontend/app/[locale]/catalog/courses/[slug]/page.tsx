import Link from "next/link";
import { notFound } from "next/navigation";
import { defaultLocale, format, getDictionary, isLocale } from "@/lib/i18n";
import { ApiRequestError, getPublishedCourseDetail } from "@/lib/api";
import NotifyMeForm from "@/components/NotifyMeForm";

function formatDate(iso: string, locale: string) {
  return new Date(iso).toLocaleDateString(locale === "ar" ? "ar-EG" : "en-US", {
    year: "numeric",
    month: "short",
    day: "numeric"
  });
}

export default async function CourseDetailPage({
  params
}: {
  params: Promise<{ locale: string; slug: string }>;
}) {
  const { locale: rawLocale, slug } = await params;
  const locale = isLocale(rawLocale) ? rawLocale : defaultLocale;
  const t = getDictionary(locale);

  let course;
  try {
    course = await getPublishedCourseDetail(slug);
  } catch (error) {
    if (error instanceof ApiRequestError && error.info.status === 404) {
      notFound();
    }
    throw error;
  }

  const openCohorts = course.cohorts.filter((c) => c.isAcceptingEnrollment);

  return (
    <main className="cf-container">
      <p>
        <Link href={`/${locale}/catalog`}>&larr; {t.courseDetail.back}</Link>
      </p>

      <h1>{course.title}</h1>
      <p className="price">
        {course.price} {course.currency}
      </p>

      {course.description && (
        <div className="card" style={{ marginBottom: "1.5rem" }}>
          <h2>{t.courseDetail.about}</h2>
          <p>{course.description}</p>
        </div>
      )}

      <div className="card">
        <h2>{t.courseDetail.batches}</h2>

        {openCohorts.length === 0 ? (
          <>
            <p className="muted">{t.courseDetail.noBatches}</p>
            {/* id="notify" — the catalog card's "Notify me" CTA deep-links here
                (/catalog/courses/{slug}#notify) instead of reconstructing this
                form from URL params. */}
            <div id="notify" className="notice">
              <p>{t.courseDetail.awaitingNextBatch}</p>
              <NotifyMeForm locale={locale} courseId={course.id} />
            </div>
          </>
        ) : (
          <div className="cf-grid">
            {openCohorts.map((cohort) => (
              <div key={cohort.id} className="card">
                <span className={`badge ${cohort.seatsLeft <= 3 ? "full" : "open"}`}>
                  {cohort.seatsLeft === 1
                    ? t.courseDetail.seatsLeft_one
                    : format(t.courseDetail.seatsLeft, { count: cohort.seatsLeft })}
                </span>
                <h3>{cohort.name}</h3>
                <p className="muted">
                  {format(t.courseDetail.startsOn, { date: formatDate(cohort.startDate, locale) })}
                </p>
                <p className="muted">
                  {format(t.courseDetail.enrollmentClosesOn, {
                    date: formatDate(cohort.enrollmentCutoffDate, locale)
                  })}
                </p>
                <Link
                  className="btn"
                  href={`/${locale}/enroll?courseId=${course.id}&name=${encodeURIComponent(course.title)}&price=${course.price}&currency=${course.currency}`}
                >
                  {t.courseDetail.enrollInBatch}
                </Link>
              </div>
            ))}
          </div>
        )}
      </div>
    </main>
  );
}
