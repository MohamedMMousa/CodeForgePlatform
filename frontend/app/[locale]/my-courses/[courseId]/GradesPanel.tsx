import Link from "next/link";
import { ArrowRight } from "lucide-react";
import type { MyCourseGrades } from "@/lib/api";
import { formatCatalogNumber } from "@/lib/format";
import { format as interpolate, type Dictionary, type Locale } from "@/lib/i18n";

// The compact grades summary on the course-content page (surface #5). The FULL
// per-item tables now live on the dedicated gradebook surface (#7,
// ./gradebook/page.tsx) — canonical in one place so they can't drift — and this
// panel is a scannable teaser that links there: attendance rate + passed/total
// counts for assessments and assignments.
export type GradesState =
  | { status: "loading" }
  | { status: "error" }
  | { status: "ready"; grades: MyCourseGrades };

type Props = {
  state: GradesState;
  courseId: string;
  locale: Locale;
  t: Dictionary["courseContent"];
};

function passedCount(items: { passed?: boolean | null }[]): number {
  return items.filter((item) => item.passed === true).length;
}

export function GradesPanel({ state, courseId, locale, t }: Props) {
  return (
    <section className="flex flex-col gap-4">
      <h2 className="text-h2 text-text">{t.gradesTitle}</h2>

      {state.status === "loading" ? (
        <div className="flex flex-col gap-3 rounded-card border border-border bg-surface p-5">
          <div className="h-4 w-32 animate-pulse rounded-control bg-surface-2" />
          <div className="h-4 w-48 animate-pulse rounded-control bg-surface-2" />
          <div className="h-4 w-40 animate-pulse rounded-control bg-surface-2" />
        </div>
      ) : state.status === "error" ? (
        // A failed grades call degrades only this section (§5) — the rest of the
        // page is unaffected.
        <p className="text-body text-text-muted">{t.loadError}</p>
      ) : (
        <div className="flex flex-col gap-4 rounded-card border border-border bg-surface p-5">
          <dl className="flex flex-col gap-2">
            <div className="flex items-baseline justify-between gap-4">
              <dt className="text-body text-text-secondary">{t.gradesAttendance}</dt>
              <dd className="text-body tabular-nums text-text">
                {formatCatalogNumber(Math.round(state.grades.attendanceRate), locale)}%
              </dd>
            </div>
            <div className="flex items-baseline justify-between gap-4">
              <dt className="text-body text-text-secondary">{t.gradesAssessmentsHeading}</dt>
              <dd className="text-body tabular-nums text-text">
                {interpolate(t.gradesPassedCount, {
                  passed: formatCatalogNumber(passedCount(state.grades.assessments), locale),
                  total: formatCatalogNumber(state.grades.assessments.length, locale)
                })}
              </dd>
            </div>
            <div className="flex items-baseline justify-between gap-4">
              <dt className="text-body text-text-secondary">{t.gradesAssignmentsHeading}</dt>
              <dd className="text-body tabular-nums text-text">
                {interpolate(t.gradesPassedCount, {
                  passed: formatCatalogNumber(passedCount(state.grades.assignments), locale),
                  total: formatCatalogNumber(state.grades.assignments.length, locale)
                })}
              </dd>
            </div>
          </dl>

          <Link
            href={`/${locale}/my-courses/${courseId}/gradebook`}
            // Inside a card, so accent-text is AA (§2.3, unlike on --bg).
            className="inline-flex w-fit items-center gap-2 text-label !text-accent-text hover:underline focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent focus-visible:ring-offset-2 focus-visible:ring-offset-surface"
          >
            {t.gradesViewFull}
            <ArrowRight aria-hidden="true" className="size-4 shrink-0 rtl:rotate-180" />
          </Link>
        </div>
      )}
    </section>
  );
}
