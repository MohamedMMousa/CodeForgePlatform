"use client";

import { use, useEffect, useRef, useState } from "react";
import Link from "next/link";
import { useAuth } from "@/lib/auth";
import {
  AnswerInput,
  ApiRequestError,
  AttemptAssessment,
  AttemptResult,
  getAssessmentForAttempt,
  startAttempt,
  submitAttempt
} from "@/lib/api";
import { defaultLocale, getDictionary, isLocale } from "@/lib/i18n";

export default function AssessmentAttemptPage({
  params
}: {
  params: Promise<{ locale: string; courseId: string; assessmentId: string }>;
}) {
  const { locale: rawLocale, courseId, assessmentId } = use(params);
  const locale = isLocale(rawLocale) ? rawLocale : defaultLocale;
  const t = getDictionary(locale).student;

  const { session } = useAuth();
  const [assessment, setAssessment] = useState<AttemptAssessment | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [attemptId, setAttemptId] = useState<string | null>(null);
  const [answers, setAnswers] = useState<Record<string, string>>({});
  const [result, setResult] = useState<AttemptResult | null>(null);
  const [starting, setStarting] = useState(false);
  const [submitting, setSubmitting] = useState(false);
  const [secondsLeft, setSecondsLeft] = useState<number | null>(null);
  const submittingRef = useRef(false);

  function onError(err: unknown) {
    setError(err instanceof ApiRequestError ? err.message : t.loadError);
  }

  function load() {
    if (!session) return;
    getAssessmentForAttempt(assessmentId, session.accessToken).then(setAssessment).catch(onError);
  }

  useEffect(load, [session, assessmentId]); // eslint-disable-line react-hooks/exhaustive-deps

  useEffect(() => {
    if (!attemptId || secondsLeft === null || result) return;
    if (secondsLeft <= 0) {
      onSubmit();
      return;
    }
    const timer = setTimeout(() => setSecondsLeft((s) => (s === null ? null : s - 1)), 1000);
    return () => clearTimeout(timer);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [secondsLeft, attemptId, result]);

  async function onStart() {
    if (!session || !assessment) return;
    setStarting(true);
    try {
      const res = await startAttempt(assessmentId, session.accessToken);
      setAttemptId(res.attemptId);
      if (assessment.timeLimitMinutes) {
        setSecondsLeft(assessment.timeLimitMinutes * 60);
      }
    } catch (err) {
      onError(err);
    } finally {
      setStarting(false);
    }
  }

  async function onSubmit() {
    if (!session || !attemptId || submittingRef.current) return;
    submittingRef.current = true;
    setSubmitting(true);
    try {
      const answerList: AnswerInput[] = Object.entries(answers).map(([questionId, selectedOptionId]) => ({
        questionId,
        selectedOptionId
      }));
      const res = await submitAttempt(attemptId, answerList, session.accessToken);
      setResult(res);
      setSecondsLeft(null);
    } catch (err) {
      onError(err);
    } finally {
      setSubmitting(false);
      submittingRef.current = false;
    }
  }

  const preventCopyPaste = assessment?.disableCopyPaste
    ? {
        onCopy: (e: React.ClipboardEvent) => e.preventDefault(),
        onPaste: (e: React.ClipboardEvent) => e.preventDefault(),
        onCut: (e: React.ClipboardEvent) => e.preventDefault()
      }
    : {};

  if (!session) {
    return (
      <main className="container">
        <p className="notice err">
          <Link href={`/${locale}/login`}>{getDictionary(locale).home.signIn}</Link>
        </p>
      </main>
    );
  }

  return (
    <main className="container">
      <Link href={`/${locale}/my-courses/${courseId}`}>{t.back}</Link>
      {error && <p className="notice err">{error}</p>}
      {assessment === null && !error && <p className="muted">…</p>}

      {assessment && !attemptId && !result && (
        <div className="card" style={{ marginTop: "1rem" }}>
          <span className="badge">{assessment.type === "quiz" ? t.quiz : t.exam}</span>
          <h1>{assessment.title}</h1>
          {assessment.timeLimitMinutes && <p>{t.timeLimit}: {assessment.timeLimitMinutes} min</p>}
          <p>
            {t.attemptsUsed}: {assessment.attemptsUsed} {t.of} {assessment.maxAttempts ?? "∞"}
          </p>
          {assessment.maxAttempts != null && assessment.attemptsUsed >= assessment.maxAttempts ? (
            <p className="notice err">{t.noAttemptsLeft}</p>
          ) : (
            <button className="btn" onClick={onStart} disabled={starting}>
              {t.startAttempt}
            </button>
          )}
        </div>
      )}

      {assessment && attemptId && !result && (
        <div style={{ marginTop: "1rem" }} {...preventCopyPaste}>
          <h1>{assessment.title}</h1>
          {secondsLeft !== null && (
            <p className="notice">
              {t.timeLimit}: {Math.floor(secondsLeft / 60)}:{String(secondsLeft % 60).padStart(2, "0")}
            </p>
          )}
          {assessment.questions.map((q, i) => (
            <div key={q.id} className="card" style={{ marginBottom: "0.75rem" }}>
              <strong>{i + 1}. {q.questionText}</strong>
              {q.options.map((o) => (
                <div key={o.id}>
                  <label>
                    <input
                      type="radio"
                      name={q.id}
                      checked={answers[q.id] === o.id}
                      onChange={() => setAnswers((prev) => ({ ...prev, [q.id]: o.id }))}
                    />{" "}
                    {o.optionText}
                  </label>
                </div>
              ))}
            </div>
          ))}
          <button className="btn" onClick={onSubmit} disabled={submitting}>
            {t.submitAttempt}
          </button>
        </div>
      )}

      {result && (
        <div className="card" style={{ marginTop: "1rem" }}>
          <h1>{t.yourResults}</h1>
          <p>{t.score}: {result.score ?? "—"}%</p>
          {result.passed !== null && <p>{result.passed ? t.passed : t.failed}</p>}
          {result.answers.map((a, i) => (
            <div key={a.questionId} className="card" style={{ marginBottom: "0.5rem" }}>
              <strong>{i + 1}. {a.questionText}</strong>
              <ul>
                {a.options.map((o) => (
                  <li
                    key={o.id}
                    style={{
                      fontWeight: o.id === a.selectedOptionId ? 700 : 400,
                      color: o.isCorrect ? "#34d399" : undefined
                    }}
                  >
                    {o.optionText}
                    {o.id === a.selectedOptionId ? " ←" : ""}
                    {o.isCorrect ? ` (${t.passed})` : ""}
                  </li>
                ))}
              </ul>
            </div>
          ))}
        </div>
      )}
    </main>
  );
}
