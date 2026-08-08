"use client";

import { use, useEffect, useState } from "react";
import Link from "next/link";
import { useAuth } from "@/lib/auth";
import { useSessionGate } from "@/components/SessionGuard";
import {
  AdminUser,
  AnnouncementItem,
  ApiRequestError,
  CohortInfo,
  CourseCertificateCandidates,
  CourseDetail,
  CourseGradebook,
  CertificateTier,
  ModuleItem,
  assignInstructorToCourse,
  cancelCohort,
  completeCohort,
  createAnnouncement,
  createCohort,
  createModule,
  deleteModule,
  getAnnouncements,
  getCourseById,
  getCourseCertificateCandidates,
  getCourseCohortsAdmin,
  getCourseGradebook,
  getCourseModules,
  getUsers,
  issueCertificate,
  openCohort,
  removeInstructorFromCourse,
  revokeCertificate,
  updateCohort,
  updateCourse
} from "@/lib/api";
import { defaultLocale, getDictionary, isLocale } from "@/lib/i18n";
import { Pagination } from "@/components/Pagination";

const PAGE_SIZE = 20;

export default function InstructorCoursePage({
  params
}: {
  params: Promise<{ locale: string; courseId: string }>;
}) {
  const { locale: rawLocale, courseId } = use(params);
  const locale = isLocale(rawLocale) ? rawLocale : defaultLocale;
  const dictionary = getDictionary(locale);
  const t = dictionary.instructor;
  const tc = dictionary.certificates;
  const ta = dictionary.admin;

  const { session } = useAuth();
  const isAdmin = session?.role === "admin";
  const [modules, setModules] = useState<ModuleItem[] | null>(null);
  const [announcements, setAnnouncements] = useState<AnnouncementItem[] | null>(null);
  const [announcementsTotal, setAnnouncementsTotal] = useState(0);
  const [announcementsPage, setAnnouncementsPage] = useState(1);
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

  const [availableInstructors, setAvailableInstructors] = useState<AdminUser[]>([]);
  const [selectedInstructorId, setSelectedInstructorId] = useState("");
  const [assigningInstructor, setAssigningInstructor] = useState(false);

  const [cohorts, setCohorts] = useState<CohortInfo[] | null>(null);
  const [cohortsTotal, setCohortsTotal] = useState(0);
  const [cohortsPage, setCohortsPage] = useState(1);
  const [editingCohortId, setEditingCohortId] = useState<string | null>(null);
  const [cohortName, setCohortName] = useState("");
  const [cohortStartDate, setCohortStartDate] = useState("");
  const [cohortEndDate, setCohortEndDate] = useState("");
  const [cohortCutoffDate, setCohortCutoffDate] = useState("");
  const [cohortCapacity, setCohortCapacity] = useState("20");
  const [cohortGraceDays, setCohortGraceDays] = useState("14");
  const [savingCohort, setSavingCohort] = useState(false);
  const [cohortBusyId, setCohortBusyId] = useState<string | null>(null);

  function reload() {
    if (!session) return;
    getCourseModules(courseId).then(setModules).catch(onError);
    getAnnouncements(courseId, { page: announcementsPage, pageSize: PAGE_SIZE })
      .then((result) => {
        setAnnouncements(result.items);
        setAnnouncementsTotal(result.totalCount);
      })
      .catch(onError);
    if (isAdmin) {
      getCourseById(courseId)
        .then((c) => {
          setCourseDetail(c);
          setThresholdInput(c.completionAttendanceThreshold?.toString() ?? "");
        })
        .catch(onError);
      getCourseCohortsAdmin(courseId, { page: cohortsPage, pageSize: PAGE_SIZE })
        .then((result) => {
          setCohorts(result.items);
          setCohortsTotal(result.totalCount);
        })
        .catch(onError);
      getUsers({ role: "instructor", isActive: true, pageSize: 100 })
        .then((result) => setAvailableInstructors(result.items))
        .catch(() => {});
    }
  }

  function resetCohortForm() {
    setEditingCohortId(null);
    setCohortName("");
    setCohortStartDate("");
    setCohortEndDate("");
    setCohortCutoffDate("");
    setCohortCapacity("20");
    setCohortGraceDays("14");
  }

  function onEditCohort(c: CohortInfo) {
    setEditingCohortId(c.id);
    setCohortName(c.name);
    setCohortStartDate(c.startDate.slice(0, 16));
    setCohortEndDate(c.endDate.slice(0, 16));
    setCohortCutoffDate(c.enrollmentCutoffDate.slice(0, 16));
    setCohortCapacity(String(c.capacity));
    setCohortGraceDays(String(c.gracePeriodDays));
  }

  async function onSaveCohort(e: React.FormEvent) {
    e.preventDefault();
    if (!session) return;
    setSavingCohort(true);
    try {
      const input = {
        name: cohortName,
        startDate: new Date(cohortStartDate).toISOString(),
        endDate: new Date(cohortEndDate).toISOString(),
        enrollmentCutoffDate: new Date(cohortCutoffDate).toISOString(),
        capacity: Number(cohortCapacity),
        gracePeriodDays: Number(cohortGraceDays)
      };
      if (editingCohortId) {
        await updateCohort(editingCohortId, input);
      } else {
        await createCohort(courseId, input);
      }
      resetCohortForm();
      reload();
    } catch (err) {
      onError(err);
    } finally {
      setSavingCohort(false);
    }
  }

  async function onCohortLifecycle(id: string, action: "open" | "cancel" | "complete") {
    if (!session) return;
    setCohortBusyId(id);
    try {
      const fn = action === "open" ? openCohort : action === "cancel" ? cancelCohort : completeCohort;
      await fn(id);
      reload();
    } catch (err) {
      onError(err);
    } finally {
      setCohortBusyId(null);
    }
  }

  async function onAssignInstructor(e: React.FormEvent) {
    e.preventDefault();
    if (!session || !selectedInstructorId) return;
    setAssigningInstructor(true);
    try {
      await assignInstructorToCourse(courseId, selectedInstructorId);
      setSelectedInstructorId("");
      reload();
    } catch (err) {
      onError(err);
    } finally {
      setAssigningInstructor(false);
    }
  }

  async function onRemoveInstructor(instructorId: string) {
    if (!session) return;
    try {
      await removeInstructorFromCourse(courseId, instructorId);
      reload();
    } catch (err) {
      onError(err);
    }
  }

  function onError(err: unknown) {
    setError(err instanceof ApiRequestError ? err.message : t.loadError);
  }

  useEffect(reload, [session, courseId, announcementsPage, cohortsPage]); // eslint-disable-line react-hooks/exhaustive-deps

  async function onAddModule(e: React.FormEvent) {
    e.preventDefault();
    if (!session) return;
    setSavingModule(true);
    try {
      await createModule(courseId, { title: newModuleTitle, description: newModuleDescription || undefined });
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
    await deleteModule(id).catch(onError);
    reload();
  }

  async function onPostAnnouncement(e: React.FormEvent) {
    e.preventDefault();
    if (!session) return;
    setPostingAnn(true);
    try {
      await createAnnouncement({ courseId, title: newAnnTitle, body: newAnnBody });
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
      await getCourseGradebook(courseId).then(setGradebook).catch(onError);
    }
  }

  function reloadCandidates() {
    if (!session) return;
    getCourseCertificateCandidates(courseId).then(setCandidates).catch(onError);
  }

  function toggleCerts() {
    setShowCerts(!showCerts);
    if (!showCerts && !candidates) {
      reloadCandidates();
    }
  }

  async function onIssue(enrollmentId: string, tier: CertificateTier) {
    if (!session) return;
    setBusyEnrollment(enrollmentId);
    try {
      await issueCertificate(enrollmentId, tier);
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
      await revokeCertificate(certificateId, null);
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
          description: courseDetail.description ?? null,
          thumbnailUrl: courseDetail.thumbnailUrl ?? null,
          category: courseDetail.category ?? null,
          price: courseDetail.price,
          currency: courseDetail.currency,
          completionAttendanceThreshold: parsed
        },
      );
      setCourseDetail(updated);
      reloadCandidates();
    } catch (err) {
      onError(err);
    } finally {
      setSavingThreshold(false);
    }
  }

  const gate = useSessionGate({ locale, roles: ["admin", "instructor"] });
  if (!gate.ok) return gate.fallback;

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
                          <span className="badge">{tierLabel(c.recommendedTier as CertificateTier)}</span>
                        </td>
                        <td>
                          {cert ? (
                            cert.isRevoked ? (
                              <span className="muted">{tc.revoked}</span>
                            ) : (
                              <>
                                <span className="badge">{tierLabel(cert.tier as CertificateTier)}</span>
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
                                  onClick={() => onIssue(c.enrollmentId, c.recommendedTier as CertificateTier)}
                                >
                                  {tc.issue} — {tierLabel(c.recommendedTier as CertificateTier)}
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

      {isAdmin && (
        <>
          <h1 style={{ marginTop: "2.5rem" }}>{ta.instructorsTitle}</h1>
          {courseDetail && courseDetail.instructors.length === 0 && (
            <p className="muted">{ta.noInstructorsAssigned}</p>
          )}
          {courseDetail && courseDetail.instructors.length > 0 && (
            <table className="table">
              <thead>
                <tr>
                  <th>{ta.fullNameLabel}</th>
                  <th>{ta.emailLabel}</th>
                  <th>{ta.actions}</th>
                </tr>
              </thead>
              <tbody>
                {courseDetail.instructors.map((i) => (
                  <tr key={i.id}>
                    <td>{i.fullName}</td>
                    <td>{i.email}</td>
                    <td>
                      <button className="btn secondary" onClick={() => onRemoveInstructor(i.instructorId)}>
                        {ta.removeInstructor}
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
          <form onSubmit={onAssignInstructor} style={{ display: "flex", gap: "0.5rem", alignItems: "flex-end", marginTop: "0.75rem" }}>
            <label>
              {ta.selectInstructor}
              <select value={selectedInstructorId} onChange={(e) => setSelectedInstructorId(e.target.value)} required>
                <option value="" disabled>
                  {ta.selectInstructor}
                </option>
                {availableInstructors
                  .filter((ins) => !courseDetail?.instructors.some((ci) => ci.instructorId === ins.id))
                  .map((ins) => (
                    <option key={ins.id} value={ins.id}>
                      {ins.fullName} ({ins.email})
                    </option>
                  ))}
              </select>
            </label>
            <button className="btn" type="submit" disabled={assigningInstructor || !selectedInstructorId}>
              {ta.assignInstructor}
            </button>
          </form>

          <h1 style={{ marginTop: "2.5rem" }}>{ta.cohortsTitle}</h1>
          {cohorts !== null && cohorts.length === 0 && <p className="muted">{ta.noCohorts}</p>}
          {cohorts !== null && cohorts.length > 0 && (
            <div style={{ overflowX: "auto" }}>
              <table className="table">
                <thead>
                  <tr>
                    <th>{ta.cohortName}</th>
                    <th>{ta.startDate}</th>
                    <th>{ta.endDate}</th>
                    <th>{ta.capacity}</th>
                    <th>{ta.enrolledCount}</th>
                    <th>{ta.seatsLeft}</th>
                    <th>{ta.status}</th>
                    <th>{ta.actions}</th>
                  </tr>
                </thead>
                <tbody>
                  {cohorts.map((c) => (
                    <tr key={c.id}>
                      <td>{c.name}</td>
                      <td>{new Date(c.startDate).toLocaleDateString(locale)}</td>
                      <td>{new Date(c.endDate).toLocaleDateString(locale)}</td>
                      <td>{c.capacity}</td>
                      <td>{c.enrolledCount}</td>
                      <td>{c.seatsLeft}</td>
                      <td><span className="badge">{c.status}</span></td>
                      <td style={{ display: "flex", gap: "0.3rem", flexWrap: "wrap" }}>
                        <button className="btn secondary" onClick={() => onEditCohort(c)}>
                          {ta.edit}
                        </button>
                        {c.status === "draft" && (
                          <button
                            className="btn secondary"
                            disabled={cohortBusyId === c.id}
                            onClick={() => onCohortLifecycle(c.id, "open")}
                          >
                            {ta.openCohort}
                          </button>
                        )}
                        {(c.status === "draft" || c.status === "open") && (
                          <button
                            className="btn secondary"
                            disabled={cohortBusyId === c.id}
                            onClick={() => onCohortLifecycle(c.id, "cancel")}
                          >
                            {ta.cancelCohort}
                          </button>
                        )}
                        {c.status === "open" && (
                          <button
                            className="btn secondary"
                            disabled={cohortBusyId === c.id}
                            onClick={() => onCohortLifecycle(c.id, "complete")}
                          >
                            {ta.completeCohort}
                          </button>
                        )}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
              <Pagination
                t={dictionary}
                page={cohortsPage}
                pageSize={PAGE_SIZE}
                totalCount={cohortsTotal}
                onPageChange={setCohortsPage}
              />
            </div>
          )}

          <form
            className="card"
            onSubmit={onSaveCohort}
            style={{ display: "flex", flexDirection: "column", gap: "0.5rem", maxWidth: "24rem", marginTop: "0.75rem" }}
          >
            <label>
              {ta.cohortName}
              <input value={cohortName} onChange={(e) => setCohortName(e.target.value)} required />
            </label>
            <label>
              {ta.startDate}
              <input type="datetime-local" value={cohortStartDate} onChange={(e) => setCohortStartDate(e.target.value)} required />
            </label>
            <label>
              {ta.endDate}
              <input type="datetime-local" value={cohortEndDate} onChange={(e) => setCohortEndDate(e.target.value)} required />
            </label>
            <label>
              {ta.enrollmentCutoffDate}
              <input type="datetime-local" value={cohortCutoffDate} onChange={(e) => setCohortCutoffDate(e.target.value)} required />
            </label>
            <label>
              {ta.capacity}
              <input type="number" min="1" value={cohortCapacity} onChange={(e) => setCohortCapacity(e.target.value)} required />
            </label>
            <label>
              {ta.gracePeriodDays}
              <input type="number" min="0" value={cohortGraceDays} onChange={(e) => setCohortGraceDays(e.target.value)} required />
            </label>
            <div style={{ display: "flex", gap: "0.5rem" }}>
              <button className="btn" type="submit" disabled={savingCohort}>
                {savingCohort ? ta.saving : editingCohortId ? ta.save : ta.addCohort}
              </button>
              {editingCohortId && (
                <button type="button" className="btn secondary" onClick={resetCohortForm}>
                  {ta.cancel}
                </button>
              )}
            </div>
          </form>
        </>
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
      <Pagination
        t={dictionary}
        page={announcementsPage}
        pageSize={PAGE_SIZE}
        totalCount={announcementsTotal}
        onPageChange={setAnnouncementsPage}
      />

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
