"use client";

import { use, useEffect, useState } from "react";
import Link from "next/link";
import { useAuth } from "@/lib/auth";
import {
  ApiRequestError,
  MaterialItem,
  MaterialType,
  SessionInput,
  SessionItem,
  SessionType,
  createModuleMaterial,
  createSession,
  createSessionMaterial,
  deleteMaterial,
  deleteSession,
  getModuleMaterials,
  getModuleSessions,
  getSessionMaterials
} from "@/lib/api";
import { defaultLocale, getDictionary, isLocale } from "@/lib/i18n";

const emptySession: SessionInput = { type: "live", title: "" };

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
  const [error, setError] = useState<string | null>(null);

  const [form, setForm] = useState<SessionInput>(emptySession);
  const [saving, setSaving] = useState(false);

  const [expandedSession, setExpandedSession] = useState<string | null>(null);
  const [sessionMaterials, setSessionMaterials] = useState<Record<string, MaterialItem[]>>({});

  function onError(err: unknown) {
    setError(err instanceof ApiRequestError ? err.message : t.loadError);
  }

  function reload() {
    if (!session) return;
    getModuleSessions(moduleId, session.accessToken).then(setSessions).catch(onError);
    getModuleMaterials(moduleId, session.accessToken).then(setModuleMaterials).catch(onError);
  }

  useEffect(reload, [session, moduleId]); // eslint-disable-line react-hooks/exhaustive-deps

  async function onAddSession(e: React.FormEvent) {
    e.preventDefault();
    if (!session) return;
    setSaving(true);
    try {
      await createSession(moduleId, form, session.accessToken);
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
    await deleteSession(id, session.accessToken).catch(onError);
    reload();
  }

  async function toggleSessionMaterials(id: string) {
    if (expandedSession === id) {
      setExpandedSession(null);
      return;
    }
    setExpandedSession(id);
    if (!session || sessionMaterials[id]) return;
    const materials = await getSessionMaterials(id, session.accessToken).catch((err) => {
      onError(err);
      return [] as MaterialItem[];
    });
    setSessionMaterials((prev) => ({ ...prev, [id]: materials }));
  }

  async function onDeleteMaterial(id: string, forSessionId: string | null) {
    if (!session) return;
    await deleteMaterial(id, session.accessToken).catch(onError);
    if (forSessionId) {
      const materials = await getSessionMaterials(forSessionId, session.accessToken);
      setSessionMaterials((prev) => ({ ...prev, [forSessionId]: materials }));
    } else {
      reload();
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
          <div style={{ display: "flex", gap: "0.5rem", marginTop: "0.5rem" }}>
            <button className="btn secondary" onClick={() => toggleSessionMaterials(s.id)}>
              {t.materials} ({s.materialCount})
            </button>
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
                await createSessionMaterial(s.id, input, session.accessToken);
                const materials = await getSessionMaterials(s.id, session.accessToken);
                setSessionMaterials((prev) => ({ ...prev, [s.id]: materials }));
                reload();
              }}
            />
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
          await createModuleMaterial(moduleId, input, session.accessToken);
          reload();
        }}
      />
    </main>
  );
}

function MaterialsPanel({
  t,
  materials,
  onDelete,
  onCreate
}: {
  t: ReturnType<typeof getDictionary>["instructor"];
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
  const [type, setType] = useState<MaterialType>("text");
  const [title, setTitle] = useState("");
  const [body, setBody] = useState("");
  const [linkUrl, setLinkUrl] = useState("");
  const [fileType, setFileType] = useState("pdf");
  const [file, setFile] = useState<File | null>(null);
  const [saving, setSaving] = useState(false);

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
          {m.type === "file" && (
            <p>
              <a href={m.fileUrl ?? "#"} target="_blank" rel="noreferrer">{m.fileUrl}</a>
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
