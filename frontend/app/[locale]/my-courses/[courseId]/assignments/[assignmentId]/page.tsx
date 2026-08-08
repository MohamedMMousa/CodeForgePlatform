"use client";

import { use, useEffect, useState } from "react";
import Link from "next/link";
import { useAuth } from "@/lib/auth";
import { useSessionGate } from "@/components/SessionGuard";
import {
  ApiRequestError,
  AssignmentForSubmission,
  SubmissionResult,
  SubmissionSummary,
  getAssignmentForSubmission,
  getMySubmissions,
  submitAssignment
} from "@/lib/api";
import { defaultLocale, getDictionary, isLocale } from "@/lib/i18n";

export default function AssignmentSubmissionPage({
  params
}: {
  params: Promise<{ locale: string; courseId: string; assignmentId: string }>;
}) {
  const { locale: rawLocale, courseId, assignmentId } = use(params);
  const locale = isLocale(rawLocale) ? rawLocale : defaultLocale;
  const t = getDictionary(locale).student;

  const { session } = useAuth();
  const [assignment, setAssignment] = useState<AssignmentForSubmission | null>(null);
  const [submissions, setSubmissions] = useState<SubmissionSummary[] | null>(null);
  const [code, setCode] = useState("");
  const [result, setResult] = useState<SubmissionResult | null>(null);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  function onError(err: unknown) {
    setError(err instanceof ApiRequestError ? err.message : t.loadError);
  }

  function reload() {
    if (!session) return;
    getAssignmentForSubmission(assignmentId).then(setAssignment).catch(onError);
    getMySubmissions(assignmentId).then(setSubmissions).catch(onError);
  }

  useEffect(reload, [session, assignmentId]); // eslint-disable-line react-hooks/exhaustive-deps

  async function onSubmit(e: React.FormEvent) {
    e.preventDefault();
    if (!session) return;
    setSubmitting(true);
    try {
      const res = await submitAssignment(assignmentId, code);
      setResult(res);
      reload();
    } catch (err) {
      onError(err);
    } finally {
      setSubmitting(false);
    }
  }

  const gate = useSessionGate({ locale });
  if (!gate.ok) return gate.fallback;

  return (
    <main className="container">
      <Link href={`/${locale}/my-courses/${courseId}`}>{t.back}</Link>
      {error && <p className="notice err">{error}</p>}
      {assignment === null && !error && <p className="muted">…</p>}

      {assignment && (
        <>
          <span className="badge">{t.assignment}</span>
          <h1>{assignment.title}</h1>
          <h3>{t.instructions}</h3>
          <p>{assignment.description}</p>
          {assignment.dueAt && <p className="muted">{t.dueDate}: {new Date(assignment.dueAt).toLocaleString(locale)}</p>}
          <p className="muted">
            {t.attemptsUsed}: {assignment.attemptsUsed} {t.of} {assignment.maxAttempts ?? "∞"}
          </p>

          {assignment.sampleTestCases.length > 0 && (
            <div className="card" style={{ marginBottom: "1rem" }}>
              <h4>{t.testResults}</h4>
              {assignment.sampleTestCases.map((tc) => (
                <div key={tc.id} style={{ marginBottom: "0.5rem" }}>
                  <p><code>{tc.input || "—"}</code> → <code>{tc.expectedOutput}</code></p>
                </div>
              ))}
            </div>
          )}

          {assignment.maxAttempts != null && assignment.attemptsUsed >= assignment.maxAttempts ? (
            <p className="notice err">{t.noAttemptsLeft}</p>
          ) : (
            <form onSubmit={onSubmit} className="card">
              <div className="field">
                <label>{t.yourCode}</label>
                <textarea
                  value={code}
                  onChange={(e) => setCode(e.target.value)}
                  rows={12}
                  style={{ fontFamily: "monospace" }}
                  required
                />
              </div>
              <button className="btn" type="submit" disabled={submitting}>
                {submitting ? t.submitting : t.submitAssignment}
              </button>
            </form>
          )}

          {result && (
            <div className="card" style={{ marginTop: "1rem" }}>
              <h3>{t.yourResults}</h3>
              {result.isLate && <p className="notice err">{t.late}</p>}
              {result.autoGradingStatus === "failed" && <p className="notice err">{t.gradingFailed}</p>}
              {result.autoGradingStatus === "completed" && (
                <p>{t.autoScore}: {result.autoScore ?? "—"}%</p>
              )}
              <h4>{t.testResults}</h4>
              {result.testResults.map((r) => (
                <p key={r.testCaseId}>
                  {r.isHidden ? t.hiddenTest : ""} {r.passed ? "✓" : "✗"}
                  {!r.isHidden && r.actualOutput && <> — <code>{r.actualOutput}</code></>}
                  {!r.isHidden && r.errorMessage && <> — <code>{r.errorMessage}</code></>}
                </p>
              ))}
            </div>
          )}

          {submissions && submissions.length > 0 && (
            <div style={{ marginTop: "1.5rem" }}>
              <h3>{t.attempt}</h3>
              {submissions.map((s) => (
                <p key={s.submissionId} className="muted">
                  {t.attempt} {s.attemptNumber} — {new Date(s.submittedAt).toLocaleString(locale)}
                  {s.isLate ? ` (${t.late})` : ""} — {t.score}: {s.finalScore ?? "—"}
                </p>
              ))}
            </div>
          )}
        </>
      )}
    </main>
  );
}
