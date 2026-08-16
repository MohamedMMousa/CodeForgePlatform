import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow
} from "@/components/ui/table";
import type { MyCourseGrades, MyCourseSummary } from "@/lib/api";
import { formatCatalogNumber } from "@/lib/format";
import type { Dictionary, Locale } from "@/lib/i18n";

// The real, already-aggregated half of DESIGN_LANGUAGE.md §4 #4's "progress":
// attendance rate and assessments passed, both computed server-side per course
// by GET /my-courses/{courseId}/grades. Lesson-level progress is deliberately
// absent — SessionProgress is written by nothing and read by nothing, so a
// percent-complete column here would be a fabricated number (ARCHITECTURE.md §7).
//
// This is also where §3's light-lane table treatment gets established for the
// gradebook surface (#7) to reuse: eyebrow column headers, hairline separators,
// numbers end-aligned per direction via the primitive's `numeric` prop.
type Props = {
  courses: MyCourseSummary[];
  /** null while the per-course calls are still in flight; a null *value* is one
   *  course whose call failed, which degrades its row rather than the section. */
  grades: Record<string, MyCourseGrades | null> | null;
  locale: Locale;
  t: Dictionary["dashboard"];
};

export function ProgressSummary({ courses, grades, locale, t }: Props) {
  return (
    <section className="flex flex-col gap-4">
      <h2 className="text-h2 text-text">{t.progress}</h2>

      <div className="rounded-card border border-border bg-surface">
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>{t.progressCourse}</TableHead>
              <TableHead numeric>{t.progressAttendance}</TableHead>
              <TableHead numeric>{t.progressAssessments}</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {courses.map((course) => {
              const row = grades?.[course.courseId];
              const passed = row?.assessments.filter((a) => a.passed === true).length;

              return (
                <TableRow key={course.courseId}>
                  <TableCell className="text-text">{course.title}</TableCell>

                  {grades === null ? (
                    <TableCell colSpan={2} numeric>
                      <span className="inline-block h-4 w-16 animate-pulse rounded-control bg-surface-2 align-middle" />
                    </TableCell>
                  ) : row ? (
                    <>
                      <TableCell numeric>
                        {formatCatalogNumber(Math.round(row.attendanceRate), locale)}%
                      </TableCell>
                      <TableCell numeric>
                        {formatCatalogNumber(passed ?? 0, locale)}/
                        {formatCatalogNumber(row.assessments.length, locale)}
                      </TableCell>
                    </>
                  ) : (
                    <TableCell colSpan={2} numeric className="text-text-muted">
                      {t.progressUnavailable}
                    </TableCell>
                  )}
                </TableRow>
              );
            })}
          </TableBody>
        </Table>
      </div>
    </section>
  );
}
