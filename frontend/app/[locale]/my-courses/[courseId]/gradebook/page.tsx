"use client";

import { use, useEffect, useState } from "react";
import Link from "next/link";
import { ArrowLeft } from "lucide-react";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow
} from "@/components/ui/table";
import { useAuth } from "@/lib/auth";
import { useSessionGate } from "@/components/SessionGuard";
import {
  ApiRequestError,
  MyAttendance,
  MyCourseGrades,
  getMyAttendance,
  getMyCourseGrades
} from "@/lib/api";
import {
  defaultLocale,
  format,
  getDictionary,
  isLocale,
  type Dictionary,
  type Locale
} from "@/lib/i18n";
import { formatCatalogDate, formatCatalogNumber } from "@/lib/format";
import { attendanceStatusDisplay } from "./attendanceStatus";

// DESIGN_LANGUAGE.md §4 #7 — the student gradebook, and the app's FIRST real
// table surface, so it's the reference implementation for every later table:
// components/ui/table.tsx already carries the full §3 treatment (eyebrow column
// headers, hairline separators, generous padding, opt-in --surface-2 zebra,
// numeric end-align per direction) — this page is the first to use all of it.
//
// Net-new route. Assembles two already-proven, per-course sources — one call
// each, each degrading only its own section (§5 partial-failure):
//   GET /my-courses/{id}/grades      → assessments + assignments (both badge
//                                       pass/fail; null Passed → "—", never fake)
//   GET /my-courses/{id}/attendance  → per-session rows + aggregate rate. This
//                                       endpoint was built but unused until here.
// The full per-item tables used to live in the content page's GradesPanel; they
// are canonical HERE now, and GradesPanel is a compact summary that links in.
//
// Deliberately NO overall "on-track / eligible to pass" course badge: the only
// pass-eligibility logic (CourseEligibilityEvaluator) is admin/certificate-scoped
// and not exposed to students, so a student-facing verdict would be invented.
// Per-item pass/fail + the attendance rate are the honest signals (ARCHITECTURE §7).

type ErrorKind = "not-found" | "access-denied" | "generic";

type GradesState =
  | { status: "loading" }
  | { status: "error"; kind: ErrorKind }
  | { status: "ready"; data: MyCourseGrades };

type AttendanceState =
  | { status: "loading" }
  | { status: "error" }
  | { status: "ready"; data: MyAttendance };

function classifyError(err: unknown): ErrorKind {
  if (err instanceof ApiRequestError && err.info.status === 404) return "not-found";
  if (err instanceof ApiRequestError && (err.info.status === 401 || err.info.status === 403)) {
    return "access-denied";
  }
  return "generic";
}

function PassBadge({
  passed,
  t
}: {
  passed: boolean | null | undefined;
  t: Dictionary["gradebook"];
}) {
  if (passed === true) return <Badge variant="success">{t.passed}</Badge>;
  if (passed === false) return <Badge variant="danger">{t.failed}</Badge>;
  // null → not attempted, or a practice item with no pass threshold. Never a verdict.
  // text-secondary, not text-muted: cells sit on zebra --surface-2 rows where
  // text-muted (#6B7280) drops below AA — the same fix as the neutral badge (§2.3).
  return <span className="text-text-secondary">—</span>;
}

function TableSkeleton({ rows = 3, cols }: { rows?: number; cols: number }) {
  return (
    <div className="rounded-card border border-border bg-surface p-4">
      <div className="flex flex-col gap-3">
        {Array.from({ length: rows }).map((_, rowIndex) => (
          <div key={rowIndex} className="flex gap-4">
            {Array.from({ length: cols }).map((_, colIndex) => (
              <div
                key={colIndex}
                className="h-4 flex-1 animate-pulse rounded-control bg-surface-2"
              />
            ))}
          </div>
        ))}
      </div>
    </div>
  );
}

function SectionShell({ title, children }: { title: string; children: React.ReactNode }) {
  return (
    <section className="flex flex-col gap-4">
      <h2 className="text-h2 text-text">{title}</h2>
      {children}
    </section>
  );
}

export default function GradebookPage({
  params
}: {
  params: Promise<{ locale: string; courseId: string }>;
}) {
  const { locale: rawLocale, courseId } = use(params);
  const locale: Locale = isLocale(rawLocale) ? rawLocale : defaultLocale;
  const dictionary = getDictionary(locale);
  const t = dictionary.gradebook;

  const { session } = useAuth();
  const [gradesState, setGradesState] = useState<GradesState>({ status: "loading" });
  const [attendanceState, setAttendanceState] = useState<AttendanceState>({ status: "loading" });
  const [reloadKey, setReloadKey] = useState(0);

  // Two independent calls. Grades drives the Assessments + Assignments sections;
  // attendance drives the Attendance section. Each fails on its own, so one
  // outage degrades only the section(s) it feeds (§5).
  useEffect(() => {
    if (!session) return;
    let active = true;
    setGradesState({ status: "loading" });

    getMyCourseGrades(courseId)
      .then((data) => {
        if (active) setGradesState({ status: "ready", data });
      })
      .catch((err) => {
        if (active) setGradesState({ status: "error", kind: classifyError(err) });
      });

    return () => {
      active = false;
    };
  }, [session, courseId, reloadKey]);

  useEffect(() => {
    if (!session) return;
    let active = true;
    setAttendanceState({ status: "loading" });

    getMyAttendance(courseId)
      .then((data) => {
        if (active) setAttendanceState({ status: "ready", data });
      })
      .catch(() => {
        if (active) setAttendanceState({ status: "error" });
      });

    return () => {
      active = false;
    };
  }, [session, courseId, reloadKey]);

  const gate = useSessionGate({ locale });
  if (!gate.ok) return gate.fallback;

  // Course title comes free from whichever payload resolved — no third call.
  const courseTitle =
    gradesState.status === "ready"
      ? gradesState.data.courseTitle
      : attendanceState.status === "ready"
        ? attendanceState.data.courseTitle
        : null;

  // Page-level fatal only when BOTH calls fail — otherwise the page renders and
  // the failed section shows its own inline error. Both endpoints share the same
  // EnsureCanView gate, so a genuinely-unenrolled student 401s on both and lands
  // here on access-denied.
  const bothFailed = gradesState.status === "error" && attendanceState.status === "error";
  const fatalKind = gradesState.status === "error" ? gradesState.kind : "generic";

  function fatalTitle(kind: ErrorKind): string {
    if (kind === "not-found") return t.notFoundTitle;
    if (kind === "access-denied") return t.accessDeniedTitle;
    return t.loadError;
  }
  function fatalHint(kind: ErrorKind): string | null {
    if (kind === "not-found") return t.notFoundHint;
    if (kind === "access-denied") return t.accessDeniedHint;
    return null;
  }

  return (
    // data-theme re-scopes every token below to §2.3 light; min-h-screen + bg-bg
    // paints the light ground so the root layout's dark body can't show through.
    <main data-theme="light" className="min-h-screen bg-bg [&_:is(h1,h2,h3,p)]:m-0">
      <div className="mx-auto flex w-full max-w-5xl flex-col gap-8 ps-5 pe-5 py-10">
        <Link
          href={`/${locale}/my-courses/${courseId}`}
          // Sits directly on --bg, where accent-text isn't AA (§2.3) — muted→text.
          className="inline-flex w-fit items-center gap-2 text-label !text-text-muted hover:!text-text focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent focus-visible:ring-offset-2 focus-visible:ring-offset-bg"
        >
          <ArrowLeft aria-hidden="true" className="size-4 shrink-0 rtl:rotate-180" />
          {t.backToCourse}
        </Link>

        {bothFailed ? (
          <div className="flex flex-col items-start gap-4 rounded-card border border-danger-border bg-danger-soft p-6">
            <div className="flex flex-col gap-1">
              <h1 className="text-h3 text-danger">{fatalTitle(fatalKind)}</h1>
              {fatalHint(fatalKind) ? (
                <p className="text-body text-danger">{fatalHint(fatalKind)}</p>
              ) : null}
            </div>
            <Button variant="secondary" onClick={() => setReloadKey((key) => key + 1)}>
              {t.retry}
            </Button>
          </div>
        ) : (
          <>
            <div className="flex flex-col gap-1">
              <h1 className="text-h1 text-text">{t.title}</h1>
              {courseTitle ? (
                <p className="text-body text-text-secondary">{courseTitle}</p>
              ) : null}
            </div>

            {/* Attendance */}
            <SectionShell title={t.attendanceHeading}>
              {attendanceState.status === "loading" ? (
                <TableSkeleton cols={3} />
              ) : attendanceState.status === "error" ? (
                <p className="text-body text-text-muted">{t.loadError}</p>
              ) : (
                <div className="flex flex-col gap-4">
                  <p className="text-body text-text">
                    {format(t.attendanceRate, {
                      rate: formatCatalogNumber(
                        Math.round(attendanceState.data.attendanceRate),
                        locale
                      )
                    })}
                  </p>
                  {attendanceState.data.sessions.length === 0 ? (
                    <p className="text-body text-text-muted">{t.noAttendance}</p>
                  ) : (
                    <div className="rounded-card border border-border bg-surface">
                      <Table zebra>
                        <TableHeader>
                          <TableRow>
                            <TableHead>{t.colSession}</TableHead>
                            <TableHead>{t.colDate}</TableHead>
                            <TableHead>{t.colStatus}</TableHead>
                          </TableRow>
                        </TableHeader>
                        <TableBody>
                          {attendanceState.data.sessions.map((s) => {
                            const display = attendanceStatusDisplay(s.status, t);
                            return (
                              <TableRow key={s.sessionId}>
                                <TableCell className="text-text">{s.sessionTitle}</TableCell>
                                <TableCell>{formatCatalogDate(s.scheduledAt, locale)}</TableCell>
                                <TableCell>
                                  {display.kind === "badge" ? (
                                    <Badge variant={display.variant}>{display.label}</Badge>
                                  ) : (
                                    // Muted, but text-secondary not text-muted: this cell
                                    // rides zebra --surface-2 rows where text-muted fails AA
                                    // (§2.3, same as the neutral-badge fix). Still clearly not
                                    // a badge, so an unmarked session never reads as absent.
                                    <span className="text-text-secondary">{display.label}</span>
                                  )}
                                </TableCell>
                              </TableRow>
                            );
                          })}
                        </TableBody>
                      </Table>
                    </div>
                  )}
                </div>
              )}
            </SectionShell>

            {/* Assessments */}
            <SectionShell title={t.assessmentsHeading}>
              {gradesState.status === "loading" ? (
                <TableSkeleton cols={5} />
              ) : gradesState.status === "error" ? (
                <p className="text-body text-text-muted">{t.loadError}</p>
              ) : gradesState.data.assessments.length === 0 ? (
                <p className="text-body text-text-muted">{t.noAssessments}</p>
              ) : (
                <div className="rounded-card border border-border bg-surface">
                  <Table zebra>
                    <TableHeader>
                      <TableRow>
                        <TableHead>{t.assessmentsHeading}</TableHead>
                        <TableHead>{t.colType}</TableHead>
                        <TableHead numeric>{t.colScore}</TableHead>
                        <TableHead numeric>{t.colAttempts}</TableHead>
                        <TableHead>{t.colResult}</TableHead>
                      </TableRow>
                    </TableHeader>
                    <TableBody>
                      {gradesState.data.assessments.map((a) => (
                        <TableRow key={a.assessmentId}>
                          <TableCell className="text-text">{a.title}</TableCell>
                          <TableCell>
                            <Badge variant="neutral">
                              {a.type === "exam" ? dictionary.student.exam : dictionary.student.quiz}
                            </Badge>
                          </TableCell>
                          <TableCell numeric>
                            {a.bestScore != null ? formatCatalogNumber(a.bestScore, locale) : "—"}
                          </TableCell>
                          <TableCell numeric>
                            {formatCatalogNumber(a.attemptsUsed, locale)}
                          </TableCell>
                          <TableCell>
                            <PassBadge passed={a.passed} t={t} />
                          </TableCell>
                        </TableRow>
                      ))}
                    </TableBody>
                  </Table>
                </div>
              )}
            </SectionShell>

            {/* Assignments */}
            <SectionShell title={t.assignmentsHeading}>
              {gradesState.status === "loading" ? (
                <TableSkeleton cols={3} />
              ) : gradesState.status === "error" ? (
                <p className="text-body text-text-muted">{t.loadError}</p>
              ) : gradesState.data.assignments.length === 0 ? (
                <p className="text-body text-text-muted">{t.noAssignments}</p>
              ) : (
                <div className="rounded-card border border-border bg-surface">
                  <Table zebra>
                    <TableHeader>
                      <TableRow>
                        <TableHead>{t.assignmentsHeading}</TableHead>
                        <TableHead numeric>{t.colScore}</TableHead>
                        <TableHead>{t.colResult}</TableHead>
                      </TableRow>
                    </TableHeader>
                    <TableBody>
                      {gradesState.data.assignments.map((a) => (
                        <TableRow key={a.assignmentId}>
                          <TableCell className="text-text">{a.title}</TableCell>
                          <TableCell numeric>
                            {a.finalScore != null ? formatCatalogNumber(a.finalScore, locale) : "—"}
                          </TableCell>
                          <TableCell>
                            <PassBadge passed={a.passed} t={t} />
                          </TableCell>
                        </TableRow>
                      ))}
                    </TableBody>
                  </Table>
                </div>
              )}
            </SectionShell>
          </>
        )}
      </div>
    </main>
  );
}
