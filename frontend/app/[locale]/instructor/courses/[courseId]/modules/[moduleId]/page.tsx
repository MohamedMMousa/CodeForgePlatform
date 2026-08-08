"use client";

import { use, useEffect, useState } from "react";
import Link from "next/link";
import { useAuth } from "@/lib/auth";
import { useSessionGate } from "@/components/SessionGuard";
import {
  ApiRequestError,
  AssessmentInput,
  AssessmentItem,
  AssessmentResults,
  AssessmentType,
  AssignmentInput,
  AssignmentItem,
  AssignmentSubmissions,
  MaterialItem,
  MaterialType,
  OptionInput,
  QuestionDto,
  RosterEntry,
  SessionInput,
  SessionItem,
  SessionRoster,
  SessionType,
  StudentSubmission,
  TestCaseDto,
  addTestCase,
  createAssessment,
  createAssignment,
  createModuleMaterial,
  createQuestion,
  createSession,
  createSessionMaterial,
  deleteAssessment,
  deleteAssignment,
  deleteMaterial,
  deleteQuestion,
  deleteSession,
  deleteTestCase,
  downloadAuthenticatedFile,
  getAssessmentById,
  getAssessmentResults,
  getAssignmentById,
  getModuleAssessments,
  getModuleAssignments,
  getModuleMaterials,
  getModuleSessions,
  getSessionMaterials,
  getSessionRoster,
  getSubmissionsForGrading,
  gradeSubmission,
  markAttendance
} from "@/lib/api";
import { defaultLocale, getDictionary, isLocale } from "@/lib/i18n";

const emptySession: SessionInput = { type: "live", title: "" };
const emptyAssessment: AssessmentInput = {
  type: "quiz",
  title: "",
  isPractice: false,
  randomizeQuestions: false,
  disableCopyPaste: false
};
const emptyAssignment: AssignmentInput = { title: "", description: "", isPractice: false };

type Dict = ReturnType<typeof getDictionary>["instructor"];

export default function InstructorModulePage({
  params
}: {
  params: Promise<{ locale: string; courseId: string; moduleId: string }>;
}) {
  const { locale: rawLocale, courseId, moduleId } = use(params);
  const locale = isLocale(rawLocale) ? rawLocale : defaultLocale;
  const t = getDictionary(locale).instructor;

  const { session } = useAuth();
  const [sessions, setSessions] = useState<SessionItem[] | null>(null);
  const [moduleMaterials, setModuleMaterials] = useState<MaterialItem[] | null>(null);
  const [assessments, setAssessments] = useState<AssessmentItem[] | null>(null);
  const [assignments, setAssignments] = useState<AssignmentItem[] | null>(null);
  const [error, setError] = useState<string | null>(null);

  const [form, setForm] = useState<SessionInput>(emptySession);
  const [saving, setSaving] = useState(false);

  const [expandedSession, setExpandedSession] = useState<string | null>(null);
  const [sessionMaterials, setSessionMaterials] = useState<Record<string, MaterialItem[]>>({});
  const [expandedAttendance, setExpandedAttendance] = useState<string | null>(null);

  function onError(err: unknown) {
    setError(err instanceof ApiRequestError ? err.message : t.loadError);
  }

  function reload() {
    if (!session) return;
    getModuleSessions(moduleId).then(setSessions).catch(onError);
    getModuleMaterials(moduleId).then(setModuleMaterials).catch(onError);
    getModuleAssessments(moduleId).then(setAssessments).catch(onError);
    getModuleAssignments(moduleId).then(setAssignments).catch(onError);
  }

  useEffect(reload, [session, moduleId]); // eslint-disable-line react-hooks/exhaustive-deps

  async function onAddSession(e: React.FormEvent) {
    e.preventDefault();
    if (!session) return;
    setSaving(true);
    try {
      await createSession(moduleId, form);
      setForm(emptySession);
      reload();
    } catch (err) {
      onError(err);
    } finally {
      setSaving(false);
    }
  }

  async function onDeleteSession(id: string) {
    if (!session || !confirm(t.confirmDelete)) return;
    await deleteSession(id).catch(onError);
    reload();
  }

  async function toggleSessionMaterials(id: string) {
    if (expandedSession === id) {
      setExpandedSession(null);
      return;
    }
    setExpandedSession(id);
    setExpandedAttendance(null);
    if (!session || sessionMaterials[id]) return;
    const materials = await getSessionMaterials(id).catch((err) => {
      onError(err);
      return [] as MaterialItem[];
    });
    setSessionMaterials((prev) => ({ ...prev, [id]: materials }));
  }

  function toggleAttendance(id: string) {
    setExpandedAttendance(expandedAttendance === id ? null : id);
    setExpandedSession(null);
  }

  async function onDeleteMaterial(id: string, forSessionId: string | null) {
    if (!session) return;
    await deleteMaterial(id).catch(onError);
    if (forSessionId) {
      const materials = await getSessionMaterials(forSessionId);
      setSessionMaterials((prev) => ({ ...prev, [forSessionId]: materials }));
    } else {
      reload();
    }
  }

  const gate = useSessionGate({ locale, roles: ["admin", "instructor"] });
  if (!gate.ok) return gate.fallback;

  return (
    <main className="container">
      <Link href={`/${locale}/instructor/courses/${courseId}`}>{t.modules}</Link>
      {error && <p className="notice err">{error}</p>}

      <h1>{t.sessions}</h1>
      {sessions === null && <p className="muted">…</p>}
      {sessions !== null && sessions.length === 0 && <p className="muted">{t.noSessions}</p>}
      {sessions?.map((s) => (
        <div key={s.id} className="card" style={{ marginBottom: "0.75rem" }}>
          <span className="badge">{s.type}</span>
          <h3>{s.title}</h3>
          {s.description && <p className="muted">{s.description}</p>}
          {s.scheduledAt && <p>{t.scheduledAt}: {new Date(s.scheduledAt).toLocaleString(locale)}</p>}
          {s.joinLink && (
            <p>
              {t.joinLink}: <a href={s.joinLink} target="_blank" rel="noreferrer">{s.joinLink}</a>
            </p>
          )}
          {s.location && <p>{t.location}: {s.location}</p>}
          {s.videoUrl && (
            <p>
              {t.videoUrl}: <a href={s.videoUrl} target="_blank" rel="noreferrer">{s.videoUrl}</a>
            </p>
          )}
          <div style={{ display: "flex", gap: "0.5rem", marginTop: "0.5rem", flexWrap: "wrap" }}>
            <button className="btn secondary" onClick={() => toggleSessionMaterials(s.id)}>
              {t.materials} ({s.materialCount})
            </button>
            {(s.type === "live" || s.type === "in_person") && (
              <button className="btn secondary" onClick={() => toggleAttendance(s.id)}>
                {t.attendance}
              </button>
            )}
            <button className="btn secondary" onClick={() => onDeleteSession(s.id)}>
              {t.delete}
            </button>
          </div>

          {expandedSession === s.id && (
            <MaterialsPanel
              t={t}
              materials={sessionMaterials[s.id] ?? []}
              onDelete={(materialId) => onDeleteMaterial(materialId, s.id)}
              onCreate={async (input) => {
                await createSessionMaterial(s.id, input);
                const materials = await getSessionMaterials(s.id);
                setSessionMaterials((prev) => ({ ...prev, [s.id]: materials }));
                reload();
              }}
            />
          )}

          {expandedAttendance === s.id && (
            <AttendancePanel sessionId={s.id} t={t} onError={onError} />
          )}
        </div>
      ))}

      <div className="card" style={{ marginTop: "1.5rem" }}>
        <h3>{t.addSession}</h3>
        <form onSubmit={onAddSession}>
          <div className="field">
            <label>{t.sessionType}</label>
            <select
              value={form.type}
              onChange={(e) => setForm({ ...form, type: e.target.value as SessionType })}
            >
              <option value="live">{t.typeLive}</option>
              <option value="in_person">{t.typeInPerson}</option>
              <option value="recorded_lesson">{t.typeRecorded}</option>
            </select>
          </div>
          <div className="field">
            <label>{t.sessionTitle}</label>
            <input
              value={form.title}
              onChange={(e) => setForm({ ...form, title: e.target.value })}
              required
            />
          </div>
          <div className="field">
            <label>{t.sessionDescription}</label>
            <textarea
              value={form.description ?? ""}
              onChange={(e) => setForm({ ...form, description: e.target.value })}
              rows={2}
            />
          </div>

          {(form.type === "live" || form.type === "in_person") && (
            <div className="field">
              <label>{t.scheduledAt}</label>
              <input
                type="datetime-local"
                value={form.scheduledAt ?? ""}
                onChange={(e) => setForm({ ...form, scheduledAt: e.target.value })}
                required
              />
            </div>
          )}
          <div className="field">
            <label>{t.duration}</label>
            <input
              type="number"
              min={1}
              value={form.durationMinutes ?? ""}
              onChange={(e) => setForm({ ...form, durationMinutes: Number(e.target.value) || undefined })}
            />
          </div>

          {form.type === "live" && (
            <div className="field">
              <label>{t.joinLink}</label>
              <input
                value={form.joinLink ?? ""}
                onChange={(e) => setForm({ ...form, joinLink: e.target.value })}
                required
              />
            </div>
          )}
          {form.type === "in_person" && (
            <div className="field">
              <label>{t.location}</label>
              <input
                value={form.location ?? ""}
                onChange={(e) => setForm({ ...form, location: e.target.value })}
                required
              />
            </div>
          )}
          {form.type === "recorded_lesson" && (
            <div className="field">
              <label>{t.videoUrl}</label>
              <input
                value={form.videoUrl ?? ""}
                onChange={(e) => setForm({ ...form, videoUrl: e.target.value })}
                required
              />
            </div>
          )}

          <button className="btn" type="submit" disabled={saving}>
            {t.add}
          </button>
        </form>
      </div>

      <h1 style={{ marginTop: "2.5rem" }}>{t.materials}</h1>
      <MaterialsPanel
        t={t}
        materials={moduleMaterials ?? []}
        onDelete={(materialId) => onDeleteMaterial(materialId, null)}
        onCreate={async (input) => {
          await createModuleMaterial(moduleId, input);
          reload();
        }}
      />

      <h1 style={{ marginTop: "2.5rem" }}>{t.assessments}</h1>
      <AssessmentsPanel
        moduleId={moduleId}
        assessments={assessments ?? []}
        t={t}
        onError={onError}
        reload={reload}
      />

      <h1 style={{ marginTop: "2.5rem" }}>{t.assignments}</h1>
      <AssignmentsPanel
        moduleId={moduleId}
        assignments={assignments ?? []}
        t={t}
        locale={locale}
        onError={onError}
        reload={reload}
      />
    </main>
  );
}

// ---------------------------------------------------------------------------
// Materials (existing, unchanged)
// ---------------------------------------------------------------------------

function MaterialsPanel({
  t,
  materials,
  onDelete,
  onCreate
}: {
  t: Dict;
  materials: MaterialItem[];
  onDelete: (id: string) => void;
  onCreate: (input: {
    type: MaterialType;
    title: string;
    body?: string;
    linkUrl?: string;
    fileType?: string;
    file?: File;
  }) => Promise<void>;
}) {
  const { session } = useAuth();
  const [type, setType] = useState<MaterialType>("text");
  const [title, setTitle] = useState("");
  const [body, setBody] = useState("");
  const [linkUrl, setLinkUrl] = useState("");
  const [fileType, setFileType] = useState("pdf");
  const [file, setFile] = useState<File | null>(null);
  const [saving, setSaving] = useState(false);

  function onDownload(fileDownloadUrl: string) {
    if (!session) return;
    downloadAuthenticatedFile(fileDownloadUrl).catch(() => {});
  }

  async function onSubmit(e: React.FormEvent) {
    e.preventDefault();
    setSaving(true);
    try {
      await onCreate({
        type,
        title,
        body: type === "text" ? body : undefined,
        linkUrl: type === "link" ? linkUrl : undefined,
        fileType: type === "file" ? fileType : undefined,
        file: type === "file" && file ? file : undefined
      });
      setTitle("");
      setBody("");
      setLinkUrl("");
      setFile(null);
    } finally {
      setSaving(false);
    }
  }

  return (
    <div style={{ marginTop: "0.75rem" }}>
      {materials.length === 0 && <p className="muted">{t.noMaterials}</p>}
      {materials.map((m) => (
        <div key={m.id} className="card" style={{ marginBottom: "0.5rem" }}>
          <span className="badge">{m.type}</span> <strong>{m.title}</strong>
          {m.type === "text" && <p>{m.body}</p>}
          {m.type === "link" && (
            <p>
              <a href={m.linkUrl ?? "#"} target="_blank" rel="noreferrer">{m.linkUrl}</a>
            </p>
          )}
          {m.type === "file" && m.fileDownloadUrl && (
            <p>
              <button className="btn secondary" onClick={() => onDownload(m.fileDownloadUrl!)}>
                {t.downloadFile}
              </button>
            </p>
          )}
          <button className="btn secondary" onClick={() => onDelete(m.id)}>
            {t.delete}
          </button>
        </div>
      ))}

      <form onSubmit={onSubmit} className="card">
        <div className="field">
          <label>{t.materialType}</label>
          <select value={type} onChange={(e) => setType(e.target.value as MaterialType)}>
            <option value="text">{t.typeText}</option>
            <option value="link">{t.typeLink}</option>
            <option value="file">{t.typeFile}</option>
          </select>
        </div>
        <div className="field">
          <label>{t.materialTitle}</label>
          <input value={title} onChange={(e) => setTitle(e.target.value)} required />
        </div>
        {type === "text" && (
          <div className="field">
            <label>{t.materialBody}</label>
            <textarea value={body} onChange={(e) => setBody(e.target.value)} rows={3} required />
          </div>
        )}
        {type === "link" && (
          <div className="field">
            <label>{t.materialLink}</label>
            <input value={linkUrl} onChange={(e) => setLinkUrl(e.target.value)} required />
          </div>
        )}
        {type === "file" && (
          <>
            <div className="field">
              <label>{t.materialFileType}</label>
              <select value={fileType} onChange={(e) => setFileType(e.target.value)}>
                <option value="pdf">PDF</option>
                <option value="ppt">PPT</option>
                <option value="zip">ZIP</option>
                <option value="other">Other</option>
              </select>
            </div>
            <div className="field">
              <label>{t.materialFile}</label>
              <input type="file" onChange={(e) => setFile(e.target.files?.[0] ?? null)} required />
            </div>
          </>
        )}
        <button className="btn" type="submit" disabled={saving}>
          {t.upload}
        </button>
      </form>
    </div>
  );
}

// ---------------------------------------------------------------------------
// Attendance
// ---------------------------------------------------------------------------

function AttendancePanel({
  sessionId,
  t,
  onError
}: {
  sessionId: string;
  t: Dict;
  onError: (err: unknown) => void;
}) {
  const [roster, setRoster] = useState<SessionRoster | null>(null);
  const [statuses, setStatuses] = useState<Record<string, string>>({});
  const [notes, setNotes] = useState<Record<string, string>>({});
  const [saving, setSaving] = useState(false);
  const [saved, setSaved] = useState(false);

  useEffect(() => {
    getSessionRoster(sessionId)
      .then((r) => {
        setRoster(r);
        const initialStatuses: Record<string, string> = {};
        const initialNotes: Record<string, string> = {};
        r.students.forEach((entry) => {
          initialStatuses[entry.studentId] = entry.status ?? "present";
          initialNotes[entry.studentId] = entry.notes ?? "";
        });
        setStatuses(initialStatuses);
        setNotes(initialNotes);
      })
      .catch(onError);
  }, [sessionId]); // eslint-disable-line react-hooks/exhaustive-deps

  async function onSave() {
    if (!roster) return;
    setSaving(true);
    setSaved(false);
    try {
      await markAttendance(
        sessionId,
        roster.students.map((entry: RosterEntry) => ({
          studentId: entry.studentId,
          status: (statuses[entry.studentId] ?? "present") as "present" | "absent" | "late" | "excused",
          notes: notes[entry.studentId] || undefined
        })),
      );
      setSaved(true);
    } catch (err) {
      onError(err);
    } finally {
      setSaving(false);
    }
  }

  if (!roster) return <p className="muted">…</p>;

  return (
    <div className="card" style={{ marginTop: "0.75rem" }}>
      <h4>{t.markAttendance}</h4>
      {roster.students.length === 0 && <p className="muted">{t.noRoster}</p>}
      {roster.students.map((entry) => (
        <div key={entry.studentId} style={{ display: "flex", gap: "0.5rem", alignItems: "center", marginBottom: "0.5rem" }}>
          <span style={{ minWidth: "10rem" }}>{entry.studentName}</span>
          <select
            value={statuses[entry.studentId] ?? "present"}
            onChange={(e) => setStatuses((prev) => ({ ...prev, [entry.studentId]: e.target.value }))}
          >
            <option value="present">{t.statusPresent}</option>
            <option value="absent">{t.statusAbsent}</option>
            <option value="late">{t.statusLate}</option>
            <option value="excused">{t.statusExcused}</option>
          </select>
          <input
            placeholder={t.manualFeedback}
            value={notes[entry.studentId] ?? ""}
            onChange={(e) => setNotes((prev) => ({ ...prev, [entry.studentId]: e.target.value }))}
            style={{ flex: 1 }}
          />
        </div>
      ))}
      {roster.students.length > 0 && (
        <button className="btn" onClick={onSave} disabled={saving}>
          {t.saveAttendance}
        </button>
      )}
      {saved && <p className="notice">{t.attendanceSaved}</p>}
    </div>
  );
}

// ---------------------------------------------------------------------------
// Assessments (quizzes + exams)
// ---------------------------------------------------------------------------

function AssessmentsPanel({
  moduleId,
  assessments,
  t,
  onError,
  reload
}: {
  moduleId: string;
  assessments: AssessmentItem[];
  t: Dict;
  onError: (err: unknown) => void;
  reload: () => void;
}) {
  const [form, setForm] = useState<AssessmentInput>(emptyAssessment);
  const [saving, setSaving] = useState(false);
  const [expanded, setExpanded] = useState<string | null>(null);

  async function onCreate(e: React.FormEvent) {
    e.preventDefault();
    setSaving(true);
    try {
      await createAssessment(moduleId, form);
      setForm(emptyAssessment);
      reload();
    } catch (err) {
      onError(err);
    } finally {
      setSaving(false);
    }
  }

  async function onDelete(id: string) {
    if (!confirm(t.confirmDelete)) return;
    await deleteAssessment(id).catch(onError);
    reload();
  }

  return (
    <div style={{ marginTop: "0.75rem" }}>
      {assessments.length === 0 && <p className="muted">{t.noAssessments}</p>}
      {assessments.map((a) => (
        <div key={a.id} className="card" style={{ marginBottom: "0.75rem" }}>
          <span className="badge">{a.type === "quiz" ? t.typeQuiz : t.typeExam}</span>
          <h3>{a.title}</h3>
          <p className="muted">
            {t.timeLimitMinutes}: {a.timeLimitMinutes ?? "—"} · {t.passScore}: {a.passScore ?? "—"} ·{" "}
            {t.maxAttempts}: {a.maxAttempts ?? t.unlimitedAttempts}
          </p>
          <div style={{ display: "flex", gap: "0.5rem" }}>
            <button className="btn secondary" onClick={() => setExpanded(expanded === a.id ? null : a.id)}>
              {t.questions} ({a.questionCount})
            </button>
            <button className="btn secondary" onClick={() => onDelete(a.id)}>
              {t.delete}
            </button>
          </div>
          {expanded === a.id && (
            <AssessmentDetailPanel assessmentId={a.id} t={t} onError={onError} onChange={reload} />
          )}
        </div>
      ))}

      <form onSubmit={onCreate} className="card">
        <h3>{t.addAssessment}</h3>
        <div className="field">
          <label>{t.assessmentType}</label>
          <select
            value={form.type}
            onChange={(e) => {
              const type = e.target.value as AssessmentType;
              setForm({ ...form, type, maxAttempts: type === "exam" ? 1 : form.maxAttempts });
            }}
          >
            <option value="quiz">{t.typeQuiz}</option>
            <option value="exam">{t.typeExam}</option>
          </select>
        </div>
        <div className="field">
          <label>{t.assessmentTitle}</label>
          <input value={form.title} onChange={(e) => setForm({ ...form, title: e.target.value })} required />
        </div>
        <div className="field">
          <label>{t.timeLimitMinutes}</label>
          <input
            type="number"
            min={1}
            value={form.timeLimitMinutes ?? ""}
            onChange={(e) => setForm({ ...form, timeLimitMinutes: Number(e.target.value) || undefined })}
          />
        </div>
        <div className="field">
          <label>{t.passScore}</label>
          <input
            type="number"
            min={0}
            max={100}
            value={form.passScore ?? ""}
            onChange={(e) => setForm({ ...form, passScore: Number(e.target.value) || undefined })}
          />
        </div>
        {form.type === "quiz" && (
          <div className="field">
            <label>{t.maxAttempts}</label>
            <input
              type="number"
              min={1}
              value={form.maxAttempts ?? ""}
              placeholder={t.unlimitedAttempts}
              onChange={(e) => setForm({ ...form, maxAttempts: Number(e.target.value) || undefined })}
            />
          </div>
        )}
        <div className="field">
          <label>
            <input
              type="checkbox"
              checked={form.isPractice}
              onChange={(e) => setForm({ ...form, isPractice: e.target.checked })}
            />{" "}
            {t.practiceMode}
          </label>
        </div>
        <div className="field">
          <label>
            <input
              type="checkbox"
              checked={form.randomizeQuestions}
              onChange={(e) => setForm({ ...form, randomizeQuestions: e.target.checked })}
            />{" "}
            {t.randomizeQuestions}
          </label>
        </div>
        <div className="field">
          <label>
            <input
              type="checkbox"
              checked={form.disableCopyPaste}
              onChange={(e) => setForm({ ...form, disableCopyPaste: e.target.checked })}
            />{" "}
            {t.disableCopyPaste}
          </label>
        </div>
        <button className="btn" type="submit" disabled={saving}>
          {t.add}
        </button>
      </form>
    </div>
  );
}

function AssessmentDetailPanel({
  assessmentId,
  t,
  onError,
  onChange
}: {
  assessmentId: string;
  t: Dict;
  onError: (err: unknown) => void;
  onChange: () => void;
}) {
  const [questions, setQuestions] = useState<QuestionDto[] | null>(null);
  const [results, setResults] = useState<AssessmentResults | null>(null);
  const [showResults, setShowResults] = useState(false);
  const [questionText, setQuestionText] = useState("");
  const [options, setOptions] = useState<OptionInput[]>([
    { optionText: "", isCorrect: true },
    { optionText: "", isCorrect: false }
  ]);
  const [saving, setSaving] = useState(false);

  function load() {
    getAssessmentById(assessmentId).then((d) => setQuestions(d.questions)).catch(onError);
  }

  useEffect(load, [assessmentId]); // eslint-disable-line react-hooks/exhaustive-deps

  async function onAddQuestion(e: React.FormEvent) {
    e.preventDefault();
    setSaving(true);
    try {
      await createQuestion(assessmentId, questionText, options);
      setQuestionText("");
      setOptions([{ optionText: "", isCorrect: true }, { optionText: "", isCorrect: false }]);
      load();
      onChange();
    } catch (err) {
      onError(err);
    } finally {
      setSaving(false);
    }
  }

  async function onDeleteQuestion(id: string) {
    if (!confirm(t.confirmDelete)) return;
    await deleteQuestion(id).catch(onError);
    load();
    onChange();
  }

  async function toggleResults() {
    setShowResults(!showResults);
    if (!showResults && !results) {
      await getAssessmentResults(assessmentId).then(setResults).catch(onError);
    }
  }

  return (
    <div style={{ marginTop: "0.75rem", paddingInlineStart: "1rem" }}>
      {questions === null && <p className="muted">…</p>}
      {questions !== null && questions.length === 0 && <p className="muted">{t.noQuestions}</p>}
      {questions?.map((q) => (
        <div key={q.id} className="card" style={{ marginBottom: "0.5rem" }}>
          <strong>{q.questionText}</strong>
          <ul>
            {q.options.map((o) => (
              <li key={o.id}>
                {o.optionText} {o.isCorrect && <span className="badge">{t.correctAnswer}</span>}
              </li>
            ))}
          </ul>
          <button className="btn secondary" onClick={() => onDeleteQuestion(q.id)}>
            {t.delete}
          </button>
        </div>
      ))}

      <form onSubmit={onAddQuestion} className="card">
        <h4>{t.addQuestion}</h4>
        <div className="field">
          <label>{t.questionText}</label>
          <input value={questionText} onChange={(e) => setQuestionText(e.target.value)} required />
        </div>
        {options.map((opt, i) => (
          <div key={i} style={{ display: "flex", gap: "0.5rem", alignItems: "center", marginBottom: "0.5rem" }}>
            <input
              type="radio"
              name="correct"
              checked={opt.isCorrect}
              onChange={() => setOptions((prev) => prev.map((o, j) => ({ ...o, isCorrect: j === i })))}
            />
            <input
              placeholder={`${t.option} ${i + 1}`}
              value={opt.optionText}
              onChange={(e) => {
                const value = e.target.value;
                setOptions((prev) => prev.map((o, j) => (j === i ? { ...o, optionText: value } : o)));
              }}
              required
              style={{ flex: 1 }}
            />
            {options.length > 2 && (
              <button
                type="button"
                className="btn secondary"
                onClick={() => setOptions((prev) => prev.filter((_, j) => j !== i))}
              >
                {t.removeOption}
              </button>
            )}
          </div>
        ))}
        {options.length < 10 && (
          <button
            type="button"
            className="btn secondary"
            onClick={() => setOptions((prev) => [...prev, { optionText: "", isCorrect: false }])}
          >
            {t.addOption}
          </button>
        )}
        <div style={{ marginTop: "0.75rem" }}>
          <button className="btn" type="submit" disabled={saving}>
            {t.add}
          </button>
        </div>
      </form>

      <button className="btn secondary" style={{ marginTop: "0.5rem" }} onClick={toggleResults}>
        {t.viewResults}
      </button>
      {showResults && results && (
        <table className="table" style={{ marginTop: "0.5rem" }}>
          <thead>
            <tr>
              <th>{t.assessmentTitle}</th>
              <th>{t.bestScore}</th>
              <th>{t.correctAnswer}</th>
            </tr>
          </thead>
          <tbody>
            {results.attempts.length === 0 && (
              <tr>
                <td colSpan={3} className="muted">{t.noSubmissions}</td>
              </tr>
            )}
            {results.attempts.map((att) => (
              <tr key={att.attemptId}>
                <td>{att.studentName}</td>
                <td>{att.score ?? "—"}</td>
                <td>{att.passed === null ? "—" : att.passed ? "✓" : "✗"}</td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </div>
  );
}

// ---------------------------------------------------------------------------
// Assignments (code, Python auto-grader)
// ---------------------------------------------------------------------------

function AssignmentsPanel({
  moduleId,
  assignments,
  t,
  locale,
  onError,
  reload
}: {
  moduleId: string;
  assignments: AssignmentItem[];
  t: Dict;
  locale: string;
  onError: (err: unknown) => void;
  reload: () => void;
}) {
  const [form, setForm] = useState<AssignmentInput>(emptyAssignment);
  const [saving, setSaving] = useState(false);
  const [expanded, setExpanded] = useState<string | null>(null);

  async function onCreate(e: React.FormEvent) {
    e.preventDefault();
    setSaving(true);
    try {
      await createAssignment(moduleId, form);
      setForm(emptyAssignment);
      reload();
    } catch (err) {
      onError(err);
    } finally {
      setSaving(false);
    }
  }

  async function onDelete(id: string) {
    if (!confirm(t.confirmDelete)) return;
    await deleteAssignment(id).catch(onError);
    reload();
  }

  return (
    <div style={{ marginTop: "0.75rem" }}>
      {assignments.length === 0 && <p className="muted">{t.noAssignments}</p>}
      {assignments.map((a) => (
        <div key={a.id} className="card" style={{ marginBottom: "0.75rem" }}>
          <h3>{a.title}</h3>
          <p className="muted">{a.description}</p>
          {a.dueAt && <p>{t.dueDate}: {new Date(a.dueAt).toLocaleString(locale)}</p>}
          <div style={{ display: "flex", gap: "0.5rem" }}>
            <button className="btn secondary" onClick={() => setExpanded(expanded === `tc-${a.id}` ? null : `tc-${a.id}`)}>
              {t.testCases} ({a.testCaseCount})
            </button>
            <button className="btn secondary" onClick={() => setExpanded(expanded === `sub-${a.id}` ? null : `sub-${a.id}`)}>
              {t.submissions}
            </button>
            <button className="btn secondary" onClick={() => onDelete(a.id)}>
              {t.delete}
            </button>
          </div>
          {expanded === `tc-${a.id}` && (
            <TestCasesPanel assignmentId={a.id} t={t} onError={onError} onChange={reload} />
          )}
          {expanded === `sub-${a.id}` && (
            <SubmissionsPanel assignmentId={a.id} t={t} locale={locale} onError={onError} />
          )}
        </div>
      ))}

      <form onSubmit={onCreate} className="card">
        <h3>{t.addAssignment}</h3>
        <div className="field">
          <label>{t.assignmentTitle}</label>
          <input value={form.title} onChange={(e) => setForm({ ...form, title: e.target.value })} required />
        </div>
        <div className="field">
          <label>{t.instructions}</label>
          <textarea
            value={form.description}
            onChange={(e) => setForm({ ...form, description: e.target.value })}
            rows={3}
            required
          />
        </div>
        <div className="field">
          <label>{t.dueDate}</label>
          <input
            type="datetime-local"
            value={form.dueAt ?? ""}
            onChange={(e) => setForm({ ...form, dueAt: e.target.value })}
          />
        </div>
        <div className="field">
          <label>{t.maxAttempts}</label>
          <input
            type="number"
            min={1}
            value={form.maxAttempts ?? ""}
            placeholder={t.unlimitedAttempts}
            onChange={(e) => setForm({ ...form, maxAttempts: Number(e.target.value) || undefined })}
          />
        </div>
        <div className="field">
          <label>{t.passScore}</label>
          <input
            type="number"
            min={0}
            max={100}
            value={form.passScore ?? ""}
            onChange={(e) => setForm({ ...form, passScore: Number(e.target.value) || undefined })}
          />
        </div>
        <div className="field">
          <label>
            <input
              type="checkbox"
              checked={form.isPractice}
              onChange={(e) => setForm({ ...form, isPractice: e.target.checked })}
            />{" "}
            {t.practiceMode}
          </label>
        </div>
        <button className="btn" type="submit" disabled={saving}>
          {t.add}
        </button>
      </form>
    </div>
  );
}

function TestCasesPanel({
  assignmentId,
  t,
  onError,
  onChange
}: {
  assignmentId: string;
  t: Dict;
  onError: (err: unknown) => void;
  onChange: () => void;
}) {
  const [testCases, setTestCases] = useState<TestCaseDto[] | null>(null);
  const [input, setInput] = useState("");
  const [expectedOutput, setExpectedOutput] = useState("");
  const [isHidden, setIsHidden] = useState(false);
  const [points, setPoints] = useState(1);
  const [saving, setSaving] = useState(false);

  function load() {
    getAssignmentById(assignmentId).then((d) => setTestCases(d.testCases)).catch(onError);
  }

  useEffect(load, [assignmentId]); // eslint-disable-line react-hooks/exhaustive-deps

  async function onAdd(e: React.FormEvent) {
    e.preventDefault();
    setSaving(true);
    try {
      await addTestCase(assignmentId, { input, expectedOutput, isHidden, points });
      setInput("");
      setExpectedOutput("");
      setIsHidden(false);
      setPoints(1);
      load();
      onChange();
    } catch (err) {
      onError(err);
    } finally {
      setSaving(false);
    }
  }

  async function onDelete(id: string) {
    if (!confirm(t.confirmDelete)) return;
    await deleteTestCase(id).catch(onError);
    load();
    onChange();
  }

  return (
    <div style={{ marginTop: "0.75rem", paddingInlineStart: "1rem" }}>
      {testCases === null && <p className="muted">…</p>}
      {testCases !== null && testCases.length === 0 && <p className="muted">{t.noTestCases}</p>}
      {testCases?.map((tc) => (
        <div key={tc.id} className="card" style={{ marginBottom: "0.5rem" }}>
          <p><strong>{t.testInput}:</strong> <code>{tc.input || "—"}</code></p>
          <p><strong>{t.expectedOutput}:</strong> <code>{tc.expectedOutput}</code></p>
          <p className="muted">
            {t.points}: {tc.points} {tc.isHidden && `· ${t.hidden}`}
          </p>
          <button className="btn secondary" onClick={() => onDelete(tc.id)}>
            {t.delete}
          </button>
        </div>
      ))}

      <form onSubmit={onAdd} className="card">
        <h4>{t.addTestCase}</h4>
        <div className="field">
          <label>{t.testInput}</label>
          <textarea value={input} onChange={(e) => setInput(e.target.value)} rows={2} />
        </div>
        <div className="field">
          <label>{t.expectedOutput}</label>
          <textarea value={expectedOutput} onChange={(e) => setExpectedOutput(e.target.value)} rows={2} required />
        </div>
        <div className="field">
          <label>{t.points}</label>
          <input type="number" min={1} value={points} onChange={(e) => setPoints(Number(e.target.value) || 1)} />
        </div>
        <div className="field">
          <label>
            <input type="checkbox" checked={isHidden} onChange={(e) => setIsHidden(e.target.checked)} /> {t.hidden}
          </label>
        </div>
        <button className="btn" type="submit" disabled={saving}>
          {t.add}
        </button>
      </form>
    </div>
  );
}

function SubmissionsPanel({
  assignmentId,
  t,
  locale,
  onError
}: {
  assignmentId: string;
  t: Dict;
  locale: string;
  onError: (err: unknown) => void;
}) {
  const [data, setData] = useState<AssignmentSubmissions | null>(null);
  const [grading, setGrading] = useState<Record<string, { score: string; feedback: string }>>({});

  function load() {
    getSubmissionsForGrading(assignmentId).then(setData).catch(onError);
  }

  useEffect(load, [assignmentId]); // eslint-disable-line react-hooks/exhaustive-deps

  async function onGrade(submissionId: string) {
    const entry = grading[submissionId];
    if (!entry || entry.score === "") return;
    await gradeSubmission(submissionId, Number(entry.score), entry.feedback || undefined).catch(onError);
    load();
  }

  if (!data) return <p className="muted">…</p>;

  return (
    <div style={{ marginTop: "0.75rem", paddingInlineStart: "1rem" }}>
      {data.submissions.length === 0 && <p className="muted">{t.noSubmissions}</p>}
      {data.submissions.map((s: StudentSubmission) => (
        <div key={s.submissionId} className="card" style={{ marginBottom: "0.5rem" }}>
          <strong>{s.studentName}</strong>{" "}
          <span className="muted">
            ({t.attempt} {s.attemptNumber}, {new Date(s.submittedAt).toLocaleString(locale)}
            {s.isLate ? `, ${t.late}` : ""})
          </span>
          <p>
            {t.autoScore}: {s.autoScore ?? "—"} ({s.autoGradingStatus}) · {t.finalScore}: {s.finalScore ?? "—"}{" "}
            {s.manualScore !== null && <span className="badge">{t.graded}</span>}
          </p>
          <div style={{ display: "flex", gap: "0.5rem", alignItems: "center" }}>
            <input
              type="number"
              min={0}
              max={100}
              placeholder={t.manualScore}
              value={grading[s.submissionId]?.score ?? ""}
              onChange={(e) =>
                setGrading((prev) => ({
                  ...prev,
                  [s.submissionId]: { score: e.target.value, feedback: prev[s.submissionId]?.feedback ?? "" }
                }))
              }
              style={{ width: "6rem" }}
            />
            <input
              placeholder={t.manualFeedback}
              value={grading[s.submissionId]?.feedback ?? ""}
              onChange={(e) =>
                setGrading((prev) => ({
                  ...prev,
                  [s.submissionId]: { score: prev[s.submissionId]?.score ?? "", feedback: e.target.value }
                }))
              }
              style={{ flex: 1 }}
            />
            <button className="btn secondary" onClick={() => onGrade(s.submissionId)}>
              {t.saveGrade}
            </button>
          </div>
        </div>
      ))}
    </div>
  );
}
