"use client";

import { use, useEffect, useState } from "react";
import Link from "next/link";
import { useAuth } from "@/lib/auth";
import {
  AnnouncementItem,
  ApiRequestError,
  CourseGradebook,
  ModuleItem,
  createAnnouncement,
  createModule,
  deleteModule,
  getAnnouncements,
  getCourseGradebook,
  getCourseModules
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

  const { session } = useAuth();
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
