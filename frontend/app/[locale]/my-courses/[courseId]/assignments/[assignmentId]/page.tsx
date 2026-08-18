"use client";

import { use, useEffect, useState } from "react";
import Link from "next/link";
import { ArrowLeft } from "lucide-react";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Textarea } from "@/components/ui/textarea";
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
  AssignmentForSubmission,
  SubmissionResult,
  SubmissionSummary,
  getAssignmentForSubmission,
  getMySubmissions,
  getSubmissionResult,
  submitAssignment
} from "@/lib/api";
import { defaultLocale, getDictionary, isLocale, type Dictionary, type Locale } from "@/lib/i18n";
import { formatCatalogNumber, formatDateTime } from "@/lib/format";
import { AssignmentDetailSkeleton } from "../../skeletons";

// Surface #6: auto-grading has no working engine (DeferredCodeExecutionService
// always throws, see ARCHITECTURE.md §7) and is not rendered anywhere on this
// page — no score, no status, no test-results panel. The only real result is
// manual grading, read via GetSubmissionResultQuery (previously a dead export,
// wired here). Passed is tri-state and only ever comes from the backend's
// compute-don't-store calculator: null means "no verdict yet", never a fake pass.
type ContentErrorKind = "not-found" | "access-denied" | "generic";
type HistoryState = "loading" | "error" | SubmissionSummary[];

function errorTitle(kind: ContentErrorKind, t: Dictionary["courseContent"]): string {
  if (kind === "not-found") return t.notFoundTitle;
  if (kind === "access-denied") return t.accessDeniedTitle;
  return t.loadError;
}

function errorHint(kind: ContentErrorKind, t: Dictionary["courseContent"]): string | null {
  if (kind === "not-found") return t.notFoundHint;
  if (kind === "access-denied") return t.accessDeniedHint;
  return null;
}

export default function AssignmentSubmissionPage({
  params
}: {
  params: Promise<{ locale: string; courseId: string; assignmentId: string }>;
}) {
  const { locale: rawLocale, courseId, assignmentId } = use(params);
  const locale = isLocale(rawLocale) ? rawLocale : defaultLocale;
  const dictionary = getDictionary(locale);
  const t = dictionary.student;
  const tc = dictionary.courseContent;

  const { session } = useAuth();
  const [assignment, setAssignment] = useState<AssignmentForSubmission | null>(null);
  const [errorKind, setErrorKind] = useState<ContentErrorKind | null>(null);
  const [history, setHistory] = useState<HistoryState>("loading");
  const [latestResult, setLatestResult] = useState<SubmissionResult | null>(null);
  const [code, setCode] = useState("");
  const [submitting, setSubmitting] = useState(false);
  const [submitError, setSubmitError] = useState<string | null>(null);
  const [reloadKey, setReloadKey] = useState(0);

  useEffect(() => {
    if (!session) return;
    let active = true;

    setErrorKind(null);
    setAssignment(null);
    setHistory("loading");
    setLatestResult(null);

    getAssignmentForSubmission(assignmentId)
      .then((data) => {
        if (!active) return;
        setAssignment(data);
      })
      .catch((err) => {
        if (!active) return;
        if (err instanceof ApiRequestError && err.info.status === 404) {
          setErrorKind("not-found");
        } else if (err instanceof ApiRequestError && (err.info.status === 401 || err.info.status === 403)) {
          setErrorKind("access-denied");
        } else {
          setErrorKind("generic");
        }
      });

    getMySubmissions(assignmentId)
      .then(async (submissions) => {
        if (!active) return;
        setHistory(submissions);
        if (submissions.length === 0) return;
        try {
          // Server-ordered by AttemptNumber descending — [0] is the latest.
          // Only the latest gets the full graded-result detail; every other
          // past attempt stays summary-only, so this is one extra call, not N+1.
          const result = await getSubmissionResult(submissions[0].submissionId);
          if (active) setLatestResult(result);
        } catch {
          // Degrades this section only (§5 partial-failure rule) — the brief
          // and submission form still work without the result detail.
        }
      })
      .catch(() => {
        if (!active) return;
        setHistory("error");
      });

    return () => {
      active = false;
    };
  }, [session, assignmentId, reloadKey]);

  async function onSubmit(e: React.FormEvent) {
    e.preventDefault();
    if (!session) return;
    setSubmitting(true);
    setSubmitError(null);
    try {
      await submitAssignment(assignmentId, code);
      setReloadKey((key) => key + 1);
    } catch (err) {
      setSubmitError(err instanceof ApiRequestError ? err.message : tc.loadError);
    } finally {
      setSubmitting(false);
    }
  }

  const gate = useSessionGate({ locale });
  if (!gate.ok) return gate.fallback;

  const loading = !errorKind && assignment === null;
  const attemptsExhausted =
    assignment != null && assignment.maxAttempts != null && assignment.attemptsUsed >= assignment.maxAttempts;
  const pastAttempts = Array.isArray(history) ? history.slice(1) : [];

  return (
    <main data-theme="light" className="min-h-screen bg-bg [&_:is(h1,h2,h3,h4,p,ul,ol)]:m-0">
      <div className="mx-auto flex w-full max-w-3xl flex-col gap-8 ps-5 pe-5 py-10">
        <Link
          href={`/${locale}/my-courses/${courseId}`}
          className="inline-flex w-fit items-center gap-2 text-label !text-text-muted hover:!text-text focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent focus-visible:ring-offset-2 focus-visible:ring-offset-bg"
        >
          <ArrowLeft aria-hidden="true" className="size-4 shrink-0 rtl:rotate-180" />
          {tc.backToCourse}
        </Link>

        {errorKind ? (
          <div className="flex flex-col items-start gap-4 rounded-card border border-danger-border bg-danger-soft p-6">
            <div className="flex flex-col gap-1">
              <h1 className="text-h3 text-danger">{errorTitle(errorKind, tc)}</h1>
              {errorHint(errorKind, tc) ? (
                <p className="text-body text-danger">{errorHint(errorKind, tc)}</p>
              ) : null}
            </div>
            <Button variant="secondary" onClick={() => setReloadKey((key) => key + 1)}>
              {tc.retry}
            </Button>
          </div>
        ) : loading || !assignment ? (
          <AssignmentDetailSkeleton />
        ) : (
          <>
            <div className="flex flex-col items-start gap-4 rounded-card border border-border bg-surface p-6">
              <div className="flex flex-wrap items-center gap-2">
                <Badge variant="neutral">{t.assignment}</Badge>
              </div>

              <h1 className="text-h1 text-text">{assignment.title}</h1>

              <div className="flex flex-col gap-1">
                <h2 className="text-h3 text-text">{t.instructions}</h2>
                <p className="text-body text-text-secondary">{assignment.description}</p>
              </div>

              {assignment.dueAt ? (
                <p className="text-meta text-text-muted">
                  {t.dueDate}: {formatDateTime(assignment.dueAt, locale)}
                </p>
              ) : null}

              <p className="text-meta text-text-muted">
                {t.attemptsUsed}: {formatCatalogNumber(assignment.attemptsUsed, locale)} {t.of}{" "}
                {assignment.maxAttempts != null ? formatCatalogNumber(assignment.maxAttempts, locale) : "∞"}
              </p>

              {assignment.sampleTestCases.length > 0 ? (
                <div className="flex w-full flex-col gap-2">
                  <h3 className="text-h3 text-text">{t.sampleTests}</h3>
                  <div className="flex flex-col gap-2">
                    {assignment.sampleTestCases.map((tcase) => (
                      <div
                        key={tcase.id}
                        className="rounded-control border border-border bg-surface-2 px-3 py-2"
                      >
                        <p className="text-body text-text-secondary">
                          <code>{tcase.input || "—"}</code> {"→"} <code>{tcase.expectedOutput}</code>
                        </p>
                      </div>
                    ))}
                  </div>
                </div>
              ) : null}
            </div>

            {history === "loading" ? (
              <div className="h-24 w-full animate-pulse rounded-card bg-surface-2" aria-hidden="true" />
            ) : history === "error" ? (
              <p className="text-body text-text-muted">{tc.loadError}</p>
            ) : (
              <>
                {latestResult ? (
                  <ResultPanel result={latestResult} locale={locale} t={t} />
                ) : history.length > 0 ? (
                  <div className="h-16 w-full animate-pulse rounded-card bg-surface-2" aria-hidden="true" />
                ) : null}

                {pastAttempts.length > 0 ? (
                  <section className="flex flex-col gap-3">
                    <h2 className="text-h2 text-text">{t.pastAttempts}</h2>
                    <Table>
                      <TableHeader>
                        <TableRow>
                          <TableHead>{t.attempt}</TableHead>
                          <TableHead>{t.submittedOn}</TableHead>
                          <TableHead numeric>{t.score}</TableHead>
                        </TableRow>
                      </TableHeader>
                      <TableBody>
                        {pastAttempts.map((s) => (
                          <TableRow key={s.submissionId}>
                            <TableCell className="text-text">
                              <span className="flex items-center gap-2">
                                {formatCatalogNumber(s.attemptNumber, locale)}
                                {s.isLate ? <Badge variant="warning">{t.late}</Badge> : null}
                              </span>
                            </TableCell>
                            <TableCell>{formatDateTime(s.submittedAt, locale)}</TableCell>
                            <TableCell numeric>
                              {s.finalScore != null ? formatCatalogNumber(s.finalScore, locale) : "—"}
                            </TableCell>
                          </TableRow>
                        ))}
                      </TableBody>
                    </Table>
                  </section>
                ) : null}
              </>
            )}

            {attemptsExhausted ? (
              <p className="rounded-card border border-border bg-surface p-6 text-body text-text-secondary">
                {t.noAttemptsLeft}
              </p>
            ) : (
              <form
                onSubmit={onSubmit}
                className="flex flex-col items-start gap-4 rounded-card border border-border bg-surface p-6"
              >
                <div className="flex w-full flex-col gap-2">
                  <label htmlFor="assignment-code" className="text-label text-text">
                    {t.yourCode}
                  </label>
                  {/* Code is always LTR, even on this Arabic page (§2.1) — the one
                      deliberate direction exception here, same category as the
                      global code-block rule and the dialog transform. */}
                  <Textarea
                    id="assignment-code"
                    dir="ltr"
                    value={code}
                    onChange={(e) => setCode(e.target.value)}
                    maxLength={50000}
                    rows={14}
                    required
                  />
                </div>
                {submitError ? <p className="text-body text-danger">{submitError}</p> : null}
                <Button type="submit" disabled={submitting}>
                  {submitting ? t.submitting : t.submitAssignment}
                </Button>
              </form>
            )}
          </>
        )}
      </div>
    </main>
  );
}

function ResultPanel({
  result,
  locale,
  t
}: {
  result: SubmissionResult;
  locale: Locale;
  t: Dictionary["student"];
}) {
  const graded = result.gradedAt != null;

  return (
    <div className="flex flex-col items-start gap-4 rounded-card border border-border bg-surface p-6">
      <div className="flex flex-wrap items-center gap-2">
        <h2 className="text-h2 text-text">{t.yourResults}</h2>
        {result.passed === true ? <Badge variant="success">{t.passed}</Badge> : null}
        {result.passed === false ? <Badge variant="danger">{t.failed}</Badge> : null}
        {result.isLate ? <Badge variant="warning">{t.late}</Badge> : null}
      </div>

      {graded ? (
        <>
          {result.finalScore != null ? (
            <p className="text-h3 text-text">
              {t.score}: {formatCatalogNumber(result.finalScore, locale)}
            </p>
          ) : null}
          <p className="text-meta text-text-muted">
            {t.gradedOn}: {formatDateTime(result.gradedAt!, locale)}
          </p>
          {result.manualFeedback ? (
            <div className="flex flex-col gap-1">
              <h3 className="text-h3 text-text">{t.feedback}</h3>
              <p className="text-body text-text-secondary">{result.manualFeedback}</p>
            </div>
          ) : null}
        </>
      ) : (
        <p className="text-body text-text-secondary">{t.awaitingGrading}</p>
      )}

      <div className="flex w-full flex-col gap-1">
        <h3 className="text-h3 text-text">{t.yourCode}</h3>
        <pre className="w-full overflow-x-auto whitespace-pre-wrap rounded-control border border-border bg-surface-2 p-3 text-code">
          {result.code}
        </pre>
      </div>
    </div>
  );
}
