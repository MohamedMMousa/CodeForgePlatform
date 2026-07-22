"use client";

import { use, useEffect, useState } from "react";
import Link from "next/link";
import { useAuth } from "@/lib/auth";
import {
  AnnouncementItem,
  ApiRequestError,
  CourseCertificateCandidates,
  CourseDetail,
  CourseGradebook,
  CertificateTier,
  ModuleItem,
  createAnnouncement,
  createModule,
  deleteModule,
  getAnnouncements,
  getCourseById,
  getCourseCertificateCandidates,
  getCourseGradebook,
  getCourseModules,
  issueCertificate,
  revokeCertificate,
  updateCourse
} from "@/lib/api";
import { defaultLocale, getDictionary, isLocale } from "@/lib/i18n";

export default function InstructorCoursePage({
  params
}: {
  params: Promise<{ locale: string; courseId: string }>;
}) {
  const { locale: rawLocale, courseId } = use(params);
  const locale = isLocale(rawLocale) ? rawLocale : defaultLocale;
  const t = getDictionary(locale).instructor;
  const tc = getDictionary(locale).certificates;

  const { session } = useAuth();
  const isAdmin = session?.role === "admin";
  const [modules, setModules] = useState<ModuleItem[] | null>(null);
  const [announcements, setAnnouncements] = useState<AnnouncementItem[] | null>(null);
  const [error, setError] = useState<string | null>(null);

  const [newModuleTitle, setNewModuleTitle] = useState("");
  const [newModuleDescription, setNewModuleDescription] = useState("");
  const [savingModule, setSavingModule] = useState(false);

  const [newAnnTitle, setNewAnnTitle] = useState("");
  const [newAnnBody, setNewAnnBody] = useState("");
  const [postingAnn, setPostingAnn] = useState(false);

  const [gradebook, setGradebook] = useState<CourseGradebook | null>(null);
  const [showGradebook, setShowGradebook] = useState(false);

  const [candidates, setCandidates] = useState<CourseCertificateCandidates | null>(null);
  const [showCerts, setShowCerts] = useState(false);
  const [busyEnrollment, setBusyEnrollment] = useState<string | null>(null);
  const [courseDetail, setCourseDetail] = useState<CourseDetail | null>(null);
  const [thresholdInput, setThresholdInput] = useState("");
  const [savingThreshold, setSavingThreshold] = useState(false);

  function reload() {
    if (!session) return;
    getCourseModules(courseId, session.accessToken).then(setModules).catch(onError);
    getAnnouncements(session.accessToken, courseId).then(setAnnouncements).catch(onError);
  }

  function onError(err: unknown) {
    setError(err instanceof ApiRequestError ? err.message : t.loadError);
  }

  useEffect(reload, [session, courseId]); // eslint-disable-line react-hooks/exhaustive-deps

  async function onAddModule(e: React.FormEvent) {
    e.preventDefault();
    if (!session) return;
    setSavingModule(true);
    try {
      await createModule(courseId, { title: newModuleTitle, description: newModuleDescription || undefined }, session.accessToken);
      setNewModuleTitle("");
      setNewModuleDescription("");
      reload();
    } catch (err) {
      onError(err);
    } finally {
      setSavingModule(false);
    }
  }

  async function onDeleteModule(id: string) {
    if (!session || !confirm(t.confirmDelete)) return;
    await deleteModule(id, session.accessToken).catch(onError);
    reload();
  }

  async function onPostAnnouncement(e: React.FormEvent) {
    e.preventDefault();
    if (!session) return;
    setPostingAnn(true);
    try {
      await createAnnouncement({ courseId, title: newAnnTitle, body: newAnnBody }, session.accessToken);
      setNewAnnTitle("");
      setNewAnnBody("");
      reload();
    } catch (err) {
      onError(err);
    } finally {
      setPostingAnn(false);
    }
  }

  async function toggleGradebook() {
    if (!session) return;
    setShowGradebook(!showGradebook);
    if (!showGradebook && !gradebook) {
      await getCourseGradebook(courseId, session.accessToken).then(setGradebook).catch(onError);
    }
  }

  function reloadCandidates() {
    if (!session) return;
    getCourseCertificateCandidates(courseId, session.accessToken).then(setCandidates).catch(onError);
  }

  async function toggleCerts() {
    if (!session) return;
    setShowCerts(!showCerts);
    if (!showCerts && !candidates) {
      reloadCandidates();
      if (isAdmin) {
        await getCourseById(courseId, session.accessToken)
          .then((c) => {
            setCourseDetail(c);
            setThresholdInput(c.completionAttendanceThreshold?.toString() ?? "");
          })
          .catch(onError);
      }
    }
  }

  async function onIssue(enrollmentId: string, tier: CertificateTier) {
    if (!session) return;
    setBusyEnrollment(enrollmentId);
    try {
      await issueCertificate(enrollmentId, tier, session.accessToken);
      reloadCandidates();
    } catch (err) {
      onError(err);
    } finally {
      setBusyEnrollment(null);
    }
  }

  async function onRevoke(enrollmentId: string, certificateId: string) {
    if (!session) return;
    setBusyEnrollment(enrollmentId);
    try {
      await revokeCertificate(certificateId, null, session.accessToken);
      reloadCandidates();
    } catch (err) {
      onError(err);
    } finally {
      setBusyEnrollment(null);
    }
  }

  async function onSaveThreshold(e: React.FormEvent) {
    e.preventDefault();
    if (!session || !courseDetail) return;
    setSavingThreshold(true);
    try {
      const parsed = thresholdInput.trim() === "" ? null : Number(thresholdInput);
      const updated = await updateCourse(
        courseId,
        {
          title: courseDetail.title,
          slug: courseDetail.slug,
          description: courseDetail.description,
          thumbnailUrl: courseDetail.thumbnailUrl,
          category: courseDetail.category,
          price: courseDetail.price,
          currency: courseDetail.currency,
          completionAttendanceThreshold: parsed
        },
        session.accessToken
      );
      setCourseDetail(updated);
      reloadCandidates();
    } catch (err) {
      onError(err);
    } finally {
      setSavingThreshold(false);
    }
  }

  if (!session || (session.role !== "admin" && session.role !== "instructor")) {
    return (
      <main className="container">
        <p className="notice err">{t.signInRequired}</p>
      </main>
    );
  }

  return (
    <main className="container">
      <Link href={`/${locale}/instructor`}>{t.myCourses}</Link>
      {error && <p className="notice err">{error}</p>}

      <h1>{t.modules}</h1>
      {modules === null && <p className="muted">…</p>}
      {modules !== null && modules.length === 0 && <p className="muted">{t.noModules}</p>}
      <div className="grid">
        {modules?.map((m) => (
          <div key={m.id} className="card">
            <h3>
              <Link href={`/${locale}/instructor/courses/${courseId}/modules/${m.id}`}>
                {m.title}
              </Link>
            </h3>
            {m.description && <p className="muted">{m.description}</p>}
            <p className="muted">{m.sessionCount} {t.sessions.toLowerCase()}</p>
            <button className="btn secondary" onClick={() => onDeleteModule(m.id)}>
              {t.delete}
            </button>
          </div>
        ))}
      </div>

      <div className="card" style={{ marginTop: "1.5rem" }}>
        <h3>{t.addModule}</h3>
        <form onSubmit={onAddModule}>
          <div className="field">
            <label>{t.moduleTitle}</label>
            <input value={newModuleTitle} onChange={(e) => setNewModuleTitle(e.target.value)} required />
          </div>
          <div className="field">
            <label>{t.moduleDescription}</label>
            <textarea
              value={newModuleDescription}
              onChange={(e) => setNewModuleDescription(e.target.value)}
              rows={2}
            />
          </div>
          <button className="btn" type="submit" disabled={savingModule}>
            {t.add}
          </button>
        </form>
      </div>

      <h1 style={{ marginTop: "2.5rem" }}>{t.gradebook}</h1>
      <button className="btn secondary" onClick={toggleGradebook}>
        {showGradebook ? t.cancel : t.gradebook}
      </button>
      {showGradebook && gradebook && (
        <div style={{ overflowX: "auto", marginTop: "0.75rem" }}>
          <table className="table">
            <thead>
              <tr>
                <th>{t.moduleTitle}</th>
                <th>{t.attendanceRate}</th>
                {gradebook.students[0]?.assessments.map((a) => (
                  <th key={a.assessmentId}>{a.title}</th>
                ))}
                {gradebook.students[0]?.assignments.map((a) => (
                  <th key={a.assignmentId}>{a.title}</th>
                ))}
              </tr>
            </thead>
            <tbody>
              {gradebook.students.length === 0 && (
                <tr>
                  <td colSpan={2} className="muted">{t.noRoster}</td>
                </tr>
              )}
              {gradebook.students.map((row) => (
                <tr key={row.studentId}>
                  <td>{row.studentName}</td>
                  <td>{row.attendanceRate}%</td>
                  {row.assessments.map((a) => (
                    <td key={a.assessmentId}>{a.bestScore ?? "—"}</td>
                  ))}
                  {row.assignments.map((a) => (
                    <td key={a.assignmentId}>{a.finalScore ?? "—"}</td>
                  ))}
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      <h1 style={{ marginTop: "2.5rem" }}>{tc.title}</h1>
      <button className="btn secondary" onClick={toggleCerts}>
        {showCerts ? t.cancel : tc.candidates}
      </button>
      {showCerts && (
        <div style={{ marginTop: "0.75rem" }}>
          {isAdmin && courseDetail && (
            <form className="card" onSubmit={onSaveThreshold} style={{ marginBottom: "1rem" }}>
              <div className="field">
                <label>{tc.attendanceMet} — {tc.recommendedTier} ≥ (%)</label>
                <input
                  type="number"
                  min={0}
                  max={100}
                  value={thresholdInput}
                  placeholder="75"
                  onChange={(e) => setThresholdInput(e.target.value)}
                />
              </div>
              <button className="btn" type="submit" disabled={savingThreshold}>
                {t.saveGrade}
              </button>
            </form>
          )}
          {candidates === null && <p className="muted">…</p>}
          {candidates !== null && candidates.candidates.length === 0 && (
            <p className="muted">{t.noRoster}</p>
          )}
          {candidates !== null && candidates.candidates.length > 0 && (
            <div style={{ overflowX: "auto" }}>
              <table className="table">
                <thead>
                  <tr>
                    <th>{tc.student}</th>
                    <th>{tc.attendanceRate}</th>
                    <th>{tc.assessmentsMet}</th>
                    <th>{tc.recommendedTier}</th>
                    <th>{tc.status}</th>
                    {isAdmin && <th></th>}
                  </tr>
                </thead>
                <tbody>
                  {candidates.candidates.map((c) => {
                    const cert = c.existingCertificate;
                    const tierLabel = (tier: CertificateTier) =>
                      tier === "completion" ? tc.completion : tc.participation;
                    return (
                      <tr key={c.enrollmentId}>
                        <td>{c.studentName}</td>
                        <td>
                          {c.attendanceRate}% / {c.attendanceThreshold}%
                          {c.attendanceMet ? " ✓" : ""}
                        </td>
                        <td>{c.assessmentsPassed ? tc.yes : tc.no} ({c.requiredAssessmentCount})</td>
                        <td>
                          <span className="badge">{tierLabel(c.recommendedTier)}</span>
                        </td>
                        <td>
                          {cert ? (
                            cert.isRevoked ? (
                              <span className="muted">{tc.revoked}</span>
                            ) : (
                              <>
                                <span className="badge">{tierLabel(cert.tier)}</span>
                                <br />
                                <span className="muted">{cert.serialNumber}</span>
                              </>
                            )
                          ) : (
                            <span className="muted">—</span>
                          )}
                        </td>
                        {isAdmin && (
                          <td>
                            {!cert && (
                              <div style={{ display: "flex", gap: "0.4rem", flexWrap: "wrap" }}>
                                <button
                                  className="btn"
                                  disabled={busyEnrollment === c.enrollmentId}
                                  onClick={() => onIssue(c.enrollmentId, c.recommendedTier)}
                                >
                                  {tc.issue} — {tierLabel(c.recommendedTier)}
                                </button>
                                {c.recommendedTier === "completion" && (
                                  <button
                                    className="btn secondary"
                                    disabled={busyEnrollment === c.enrollmentId}
                                    onClick={() => onIssue(c.enrollmentId, "participation")}
                                  >
                                    {tc.participation}
                                  </button>
                                )}
                              </div>
                            )}
                            {cert && !cert.isRevoked && (
                              <button
                                className="btn secondary"
                                disabled={busyEnrollment === c.enrollmentId}
                                onClick={() => onRevoke(c.enrollmentId, cert.id)}
                              >
                                {tc.revoke}
                              </button>
                            )}
                          </td>
                        )}
                      </tr>
                    );
                  })}
                </tbody>
              </table>
            </div>
          )}
        </div>
      )}

      <h1 style={{ marginTop: "2.5rem" }}>{t.announcements}</h1>
      {announcements !== null && announcements.length === 0 && (
        <p className="muted">{t.noAnnouncements}</p>
      )}
      {announcements?.map((a) => (
        <div key={a.id} className="card" style={{ marginBottom: "0.75rem" }}>
          <strong>{a.title}</strong>
          <p>{a.body}</p>
          <span className="muted">{a.authorName} · {new Date(a.createdAt).toLocaleString(locale)}</span>
        </div>
      ))}

      <div className="card" style={{ marginTop: "1rem" }}>
        <h3>{t.addAnnouncement}</h3>
        <form onSubmit={onPostAnnouncement}>
          <div className="field">
            <label>{t.announcementTitle}</label>
            <input value={newAnnTitle} onChange={(e) => setNewAnnTitle(e.target.value)} required />
          </div>
          <div className="field">
            <label>{t.announcementBody}</label>
            <textarea value={newAnnBody} onChange={(e) => setNewAnnBody(e.target.value)} rows={3} required />
          </div>
          <button className="btn" type="submit" disabled={postingAnn}>
            {t.post}
          </button>
        </form>
      </div>
    </main>
  );
}
