import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow
} from "@/components/ui/table";
import { Badge } from "@/components/ui/badge";
import type { MyCourseGrades } from "@/lib/api";
import { formatCatalogNumber } from "@/lib/format";
import type { Dictionary, Locale } from "@/lib/i18n";

// The legacy page's grades toggle, rebuilt on §3's light-lane table treatment
// (dashboard/ProgressSummary.tsx established the pattern). Kept rather than
// deferred to surface #7 (gradebook) — per-assessment scores appear nowhere
// else in the app; the dashboard shows only a passed/total count.
export type GradesState =
  | { status: "loading" }
  | { status: "error" }
  | { status: "ready"; grades: MyCourseGrades };

type Props = {
  state: GradesState;
  locale: Locale;
  t: Dictionary["courseContent"];
};

export function GradesPanel({ state, locale, t }: Props) {
  return (
    <section className="flex flex-col gap-4">
      <h2 className="text-h2 text-text">{t.gradesTitle}</h2>

      {state.status === "loading" ? (
        <div className="flex flex-col gap-3 rounded-card border border-border bg-surface p-5">
          <div className="h-4 w-32 animate-pulse rounded-control bg-surface-2" />
          <div className="h-4 w-full animate-pulse rounded-control bg-surface-2" />
          <div className="h-4 w-full animate-pulse rounded-control bg-surface-2" />
        </div>
      ) : state.status === "error" ? (
        // A failed grades call degrades only this section (§5 partial-failure
        // rule) — the rest of the page is unaffected.
        <p className="text-body text-text-muted">{t.loadError}</p>
      ) : (
        <div className="flex flex-col gap-5 rounded-card border border-border bg-surface p-5">
          <p className="text-body text-text">
            {t.gradesAttendance}:{" "}
            {formatCatalogNumber(Math.round(state.grades.attendanceRate), locale)}%
          </p>

          <div className="flex flex-col gap-2">
            <h3 className="text-h3 text-text">{t.gradesAssessmentsHeading}</h3>
            {state.grades.assessments.length === 0 ? (
              <p className="text-body text-text-muted">{t.gradesNoAssessments}</p>
            ) : (
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>{t.gradesAssessmentsHeading}</TableHead>
                    <TableHead numeric>{t.gradesScore}</TableHead>
                    <TableHead>{t.gradesStatus}</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {state.grades.assessments.map((assessment) => (
                    <TableRow key={assessment.assessmentId}>
                      <TableCell className="text-text">{assessment.title}</TableCell>
                      <TableCell numeric>
                        {assessment.bestScore != null
                          ? formatCatalogNumber(assessment.bestScore, locale)
                          : "—"}
                      </TableCell>
                      <TableCell>
                        {assessment.passed === true ? (
                          <Badge variant="success">{t.gradesPassed}</Badge>
                        ) : assessment.passed === false ? (
                          <Badge variant="danger">{t.gradesFailed}</Badge>
                        ) : (
                          "—"
                        )}
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            )}
          </div>

          <div className="flex flex-col gap-2">
            <h3 className="text-h3 text-text">{t.gradesAssignmentsHeading}</h3>
            {state.grades.assignments.length === 0 ? (
              <p className="text-body text-text-muted">{t.gradesNoAssignments}</p>
            ) : (
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>{t.gradesAssignmentsHeading}</TableHead>
                    <TableHead numeric>{t.gradesScore}</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {state.grades.assignments.map((assignment) => (
                    <TableRow key={assignment.assignmentId}>
                      <TableCell className="text-text">{assignment.title}</TableCell>
                      <TableCell numeric>
                        {assignment.finalScore != null
                          ? formatCatalogNumber(assignment.finalScore, locale)
                          : "—"}
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            )}
          </div>
        </div>
      )}
    </section>
  );
}
