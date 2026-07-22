"use client";

import { use, useEffect, useState } from "react";
import Link from "next/link";
import { useAuth } from "@/lib/auth";
import {
  ApiRequestError,
  MaterialItem,
  MyCourseContent,
  getMyCourseContent,
  getSessionMaterials
} from "@/lib/api";
import { defaultLocale, getDictionary, isLocale } from "@/lib/i18n";

export default function MyCoursePage({
  params
}: {
  params: Promise<{ locale: string; courseId: string }>;
}) {
  const { locale: rawLocale, courseId } = use(params);
  const locale = isLocale(rawLocale) ? rawLocale : defaultLocale;
  const t = getDictionary(locale).student;

  const { session } = useAuth();
  const [content, setContent] = useState<MyCourseContent | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [expandedSession, setExpandedSession] = useState<string | null>(null);
  const [sessionMaterials, setSessionMaterials] = useState<Record<string, MaterialItem[]>>({});

  useEffect(() => {
    if (!session) return;
    getMyCourseContent(courseId, session.accessToken)
      .then(setContent)
      .catch((err) => setError(err instanceof ApiRequestError ? err.message : t.loadError));
  }, [session, courseId, t.loadError]);

  async function toggleMaterials(id: string) {
    if (expandedSession === id) {
      setExpandedSession(null);
      return;
    }
    setExpandedSession(id);
    if (!session || sessionMaterials[id]) return;
    const materials = await getSessionMaterials(id, session.accessToken).catch(() => []);
    setSessionMaterials((prev) => ({ ...prev, [id]: materials }));
  }

  function typeLabel(type: string) {
    if (type === "live") return t.live;
    if (type === "in_person") return t.inPerson;
    return t.recordedLesson;
  }

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
      <Link href={`/${locale}/dashboard`}>{t.back}</Link>
      {error && <p className="notice err">{error}</p>}
      {content === null && !error && <p className="muted">…</p>}

      {content && (
        <>
          <h1>{content.courseTitle}</h1>
          <h2>{t.courseContent}</h2>
          {content.modules.length === 0 && <p className="muted">{t.noModules}</p>}

          {content.modules.map((m) => (
            <div key={m.id} style={{ marginBottom: "2rem" }}>
              <h3>{m.title}</h3>
              {m.description && <p className="muted">{m.description}</p>}

              {m.sessions.map((s) => (
                <div key={s.id} className="card" style={{ marginBottom: "0.75rem" }}>
                  <span className="badge">{typeLabel(s.type)}</span>
                  <h4>{s.title}</h4>
                  {s.description && <p className="muted">{s.description}</p>}
                  {s.scheduledAt && <p>{new Date(s.scheduledAt).toLocaleString(locale)}</p>}

                  {s.type === "live" && s.joinLink && (
                    <a className="btn" href={s.joinLink} target="_blank" rel="noreferrer">
                      {t.join}
                    </a>
                  )}
                  {s.type === "in_person" && s.location && (
                    <p>{t.viewLocation}: {s.location}</p>
                  )}
                  {s.videoUrl && (
                    <a className="btn secondary" href={s.videoUrl} target="_blank" rel="noreferrer">
                      {t.watchVideo}
                    </a>
                  )}

                  {s.materialCount > 0 && (
                    <div style={{ marginTop: "0.5rem" }}>
                      <button className="btn secondary" onClick={() => toggleMaterials(s.id)}>
                        {t.materials} ({s.materialCount})
                      </button>
                      {expandedSession === s.id && (
                        <div style={{ marginTop: "0.5rem" }}>
                          {(sessionMaterials[s.id] ?? []).map((mat) => (
                            <div key={mat.id} className="card" style={{ marginBottom: "0.5rem" }}>
                              <span className="badge">{mat.type}</span> <strong>{mat.title}</strong>
                              {mat.type === "text" && <p>{mat.body}</p>}
                              {mat.type === "link" && (
                                <p>
                                  <a href={mat.linkUrl ?? "#"} target="_blank" rel="noreferrer">
                                    {mat.linkUrl}
                                  </a>
                                </p>
                              )}
                              {mat.type === "file" && (
                                <p>
                                  <a href={mat.fileUrl ?? "#"} target="_blank" rel="noreferrer">
                                    {mat.fileUrl}
                                  </a>
                                </p>
                              )}
                            </div>
                          ))}
                        </div>
                      )}
                    </div>
                  )}
                </div>
              ))}
            </div>
          ))}
        </>
      )}
    </main>
  );
}
