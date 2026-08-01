"use client";

import { use, useEffect, useState } from "react";
import Link from "next/link";
import { useAuth } from "@/lib/auth";
import { ApiRequestError, UpcomingItems, getUpcomingItems } from "@/lib/api";
import { defaultLocale, getDictionary, isLocale } from "@/lib/i18n";

export default function DashboardPage({
  params
}: {
  params: Promise<{ locale: string }>;
}) {
  const { locale: rawLocale } = use(params);
  const locale = isLocale(rawLocale) ? rawLocale : defaultLocale;
  const t = getDictionary(locale).student;

  const { session } = useAuth();
  const [data, setData] = useState<UpcomingItems | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!session) return;
    getUpcomingItems()
      .then(setData)
      .catch((err) => setError(err instanceof ApiRequestError ? err.message : t.loadError));
  }, [session, t.loadError]);

  if (!session) {
    return (
      <main className="container">
        <p className="notice err">
          <Link href={`/${locale}/login`}>{getDictionary(locale).home.signIn}</Link>
        </p>
      </main>
    );
  }

  function typeLabel(type: string) {
    if (type === "live") return t.live;
    if (type === "in_person") return t.inPerson;
    return t.recordedLesson;
  }

  return (
    <main className="container">
      <h1>{t.dashboard}</h1>
      {error && <p className="notice err">{error}</p>}

      <h2>{t.upcomingSessions}</h2>
      {data !== null && data.upcomingSessions.length === 0 && (
        <p className="muted">{t.noUpcoming}</p>
      )}
      {data?.upcomingSessions.map((s) => (
        <div key={s.sessionId} className="card" style={{ marginBottom: "0.75rem" }}>
          <span className="badge">{typeLabel(s.type)}</span>
          <h3>
            <Link href={`/${locale}/my-courses/${s.courseId}`}>{s.title}</Link>
          </h3>
          <p className="muted">{s.courseTitle} · {s.moduleTitle}</p>
          <p>{new Date(s.scheduledAt).toLocaleString(locale)}</p>
          {s.joinLink && (
            <a className="btn" href={s.joinLink} target="_blank" rel="noreferrer">
              {t.join}
            </a>
          )}
          {s.location && <p>{t.viewLocation}: {s.location}</p>}
        </div>
      ))}

      <h2 style={{ marginTop: "2rem" }}>{t.recentAnnouncements}</h2>
      {data !== null && data.recentAnnouncements.length === 0 && (
        <p className="muted">{t.noAnnouncements}</p>
      )}
      {data?.recentAnnouncements.map((a) => (
        <div key={a.id} className="card" style={{ marginBottom: "0.75rem" }}>
          <strong>{a.title}</strong>
          {a.courseTitle && <span className="badge">{a.courseTitle}</span>}
          <p>{a.body}</p>
          <span className="muted">{new Date(a.createdAt).toLocaleString(locale)}</span>
        </div>
      ))}
    </main>
  );
}
