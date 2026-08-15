import Link from "next/link";
import { notFound } from "next/navigation";
import { ArrowLeft, Calendar, Clock } from "lucide-react";
import { Button } from "@/components/ui/button";
import { defaultLocale, format, getCategoryLabel, getDictionary, isLocale } from "@/lib/i18n";
import { ApiRequestError, getPublishedCourseDetail } from "@/lib/api";
import NotifyMeForm from "@/components/NotifyMeForm";
import { RetryButton } from "../../RetryButton";
import { formatCatalogDate, formatCatalogNumber, formatCohortMonthYear } from "../../format";
import { CohortCard } from "./CohortCard";
import { getPreviouslyRunTemplate, partitionCohorts } from "./cohorts";

// DESIGN_LANGUAGE.md §4 #3 — the dark shop-window sell page, bound strictly to what
// GET /catalog/courses/{slug} actually returns. There is deliberately no syllabus,
// instructor bio, level, or duration section: none of that data exists on the DTO or
// in the schema (see ARCHITECTURE.md §7's two deferred backend follow-ups).
//
// ShopNav is not rendered here — components/SiteHeader.tsx already swaps it in for
// /catalog and everything beneath it.

/** Blank-line-separated paragraphs; single newlines inside one are preserved by
 *  `whitespace-pre-line` at the render site. Returns [] for null/blank input, which
 *  is what keeps the whole overview section (heading included) from rendering. */
function toParagraphs(description: string | null | undefined): string[] {
  if (!description) return [];
  return description
    .split(/\n\s*\n/)
    .map((paragraph) => paragraph.trim())
    .filter((paragraph) => paragraph.length > 0);
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
    // Everything else gets the designed error state rather than an exception page
    // — same treatment as the catalog surface, reusing its retry control.
    return (
      <main className="mx-auto w-full max-w-4xl ps-5 pe-5 py-10">
        <div className="flex flex-col items-start gap-4 rounded-card border border-danger-border bg-danger-soft p-6">
          <p className="text-body text-danger">{t.catalog.loadError}</p>
          <RetryButton label={t.catalog.retry} />
        </div>
      </main>
    );
  }

  const { forwardLooking, bookable, selected, completedCount } = partitionCohorts(
    course.cohorts,
    new Date()
  );
  const paragraphs = toParagraphs(course.description);

  // Preserved contract: same query-param shape the previous page used. `price` must
  // stay the RAW number — EnrollForm parses it with Number(), so a grouped/localized
  // string (formatCatalogNumber) would arrive as NaN.
  const enrollHref =
    `/${locale}/enroll?courseId=${course.id}` +
    `&name=${encodeURIComponent(course.title)}` +
    `&price=${course.price}&currency=${encodeURIComponent(course.currency)}`;

  return (
    <main className="mx-auto flex w-full max-w-4xl flex-col gap-10 ps-5 pe-5 py-10">
      <div>
        {/* Arrow is directional (§2.5) — it flips with writing direction. */}
        <Link
          href={`/${locale}/catalog`}
          className="inline-flex items-center gap-2 text-label !text-text-muted hover:!text-accent-text focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent focus-visible:ring-offset-2 focus-visible:ring-offset-bg"
        >
          <ArrowLeft aria-hidden="true" className="size-4 shrink-0 rtl:rotate-180" />
          {t.courseDetail.back}
        </Link>
      </div>

      <header className="flex flex-col gap-3">
        {course.category ? (
          <span className="eyebrow text-text-muted">
            {getCategoryLabel(course.category, locale)}
          </span>
        ) : null}
        <h1 className="text-h1 text-text">{course.title}</h1>
        <p className="text-h2 text-accent">
          {formatCatalogNumber(course.price, locale)} {course.currency}
        </p>
      </header>

      {/* The screen's one primary (§3), and only when something is actually
          bookable. It names the batch the backend will really enroll into, because
          the enrollment API accepts no cohort id and always resolves the earliest
          bookable one itself. */}
      {selected ? (
        <section className="flex flex-col items-start gap-4 rounded-card border border-border bg-surface p-6">
          <Button asChild size="lg">
            <Link href={enrollHref}>
              {format(t.courseDetail.enrollInCohort, {
                cohort: formatCohortMonthYear(selected.startDate, locale)
              })}
            </Link>
          </Button>
          <div className="flex flex-col gap-2 text-meta text-text-muted">
            <span className="flex items-center gap-2">
              <Calendar aria-hidden="true" className="size-4 shrink-0" />
              {format(t.courseDetail.startsOn, {
                date: formatCatalogDate(selected.startDate, locale)
              })}
            </span>
            <span className="flex items-center gap-2">
              <Clock aria-hidden="true" className="size-4 shrink-0" />
              {format(t.courseDetail.enrollmentClosesOn, {
                date: formatCatalogDate(selected.enrollmentCutoffDate, locale)
              })}
            </span>
          </div>
        </section>
      ) : null}

      {/* Rendered only when there is real copy — a null, empty, or whitespace-only
          description produces no heading and no empty box. */}
      {paragraphs.length > 0 ? (
        <section className="flex flex-col gap-3">
          <h2 className="text-h2 text-text">{t.courseDetail.about}</h2>
          <div className="flex flex-col gap-3">
            {paragraphs.map((paragraph, index) => (
              <p
                key={index}
                className="text-body whitespace-pre-line text-text-secondary"
              >
                {paragraph}
              </p>
            ))}
          </div>
        </section>
      ) : null}

      <section className="flex flex-col gap-4">
        <h2 className="text-h2 text-text">{t.courseDetail.batches}</h2>
        {forwardLooking.length === 0 ? (
          <p className="text-body text-text-muted">{t.courseDetail.noUpcomingBatches}</p>
        ) : (
          <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
            {forwardLooking.map((cohort) => (
              <CohortCard
                key={cohort.id}
                cohort={cohort}
                isSelected={selected?.id === cohort.id}
                locale={locale}
                t={t}
              />
            ))}
          </div>
        )}
        {/* Footnote-level trust signal, completed batches only — never a card or a
            badge, and absent entirely at zero rather than reading "run 0 times". */}
        {completedCount > 0 ? (
          <p className="text-meta text-text-muted">
            {format(getPreviouslyRunTemplate(completedCount, t), {
              count: formatCatalogNumber(completedCount, locale)
            })}
          </p>
        ) : null}
      </section>

      {/* Preserved contract: renders under exactly the previous condition — no
          cohort is accepting enrollment — and keeps the #notify anchor the catalog
          cards deep-link to (/catalog/courses/{slug}#notify). */}
      {bookable.length === 0 ? (
        <section id="notify" className="flex flex-col gap-3 scroll-mt-6">
          <h2 className="text-h2 text-text">{t.courseDetail.notifyHeading}</h2>
          <p className="text-body text-text-secondary">{t.courseDetail.noBatches}</p>
          <NotifyMeForm locale={locale} courseId={course.id} />
        </section>
      ) : null}

      {course.instructors.length > 0 ? (
        <section className="flex flex-col gap-2">
          <h2 className="text-h2 text-text">
            {course.instructors.length === 1
              ? t.courseDetail.instructor
              : t.courseDetail.instructors}
          </h2>
          {/* Name only — that is the entire public instructor dataset. Email is on
              the DTO but deliberately not published on an anonymous page. Arabic
              separates list items with U+060C, not the Latin comma. */}
          <p className="text-body text-text-secondary">
            {course.instructors
              .map((instructor) => instructor.fullName)
              .join(locale === "ar" ? "، " : ", ")}
          </p>
        </section>
      ) : null}
    </main>
  );
}
