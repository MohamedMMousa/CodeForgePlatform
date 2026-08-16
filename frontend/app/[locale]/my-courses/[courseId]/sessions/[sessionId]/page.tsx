"use client";

import { use, useEffect, useState } from "react";
import Link from "next/link";
import { ArrowLeft, Calendar, ChevronLeft, ChevronRight, MapPin } from "lucide-react";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { useAuth } from "@/lib/auth";
import { useSessionGate } from "@/components/SessionGuard";
import {
  ApiRequestError,
  MaterialItem,
  MyCourseContent,
  SessionItem,
  getMyCourseContent,
  getSessionMaterials
} from "@/lib/api";
import {
  defaultLocale,
  format,
  getDictionary,
  getDurationTemplate,
  isLocale,
  type Dictionary
} from "@/lib/i18n";
import { formatDateTime } from "@/lib/format";
import { externalHref } from "@/lib/url";
import { MaterialList } from "../../MaterialList";
import { getSessionTreatment } from "../../sessionState";
import { SessionDetailSkeleton } from "../../skeletons";

// The other half of surface #5: a single session's type-driven treatment
// (see ../sessionState.ts), its short plain-text description, materials, and
// prev/next through the course. Fetches the same content-tree call the
// overview uses and locates itself inside it — no getSessionById wrapper
// exists, and a bare SessionDto carries no course/module context anyway.
type ContentErrorKind = "not-found" | "access-denied" | "generic";

function typeLabel(type: string, student: Dictionary["student"]): string {
  if (type === "live") return student.live;
  if (type === "in_person") return student.inPerson;
  return student.recordedLesson;
}

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

export default function SessionDetailPage({
  params
}: {
  params: Promise<{ locale: string; courseId: string; sessionId: string }>;
}) {
  const { locale: rawLocale, courseId, sessionId } = use(params);
  const locale = isLocale(rawLocale) ? rawLocale : defaultLocale;
  const dictionary = getDictionary(locale);
  const t = dictionary.courseContent;

  const { session } = useAuth();
  const [content, setContent] = useState<MyCourseContent | null>(null);
  const [errorKind, setErrorKind] = useState<ContentErrorKind | null>(null);
  const [materials, setMaterials] = useState<MaterialItem[] | "error" | null>(null);
  const [reloadKey, setReloadKey] = useState(0);

  useEffect(() => {
    if (!session) return;
    let active = true;

    setErrorKind(null);
    setContent(null);

    getMyCourseContent(courseId)
      .then((data) => {
        if (!active) return;
        setContent(data);
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

    return () => {
      active = false;
    };
  }, [session, courseId, reloadKey]);

  const activeModule =
    content?.modules.find((module) => module.sessions.some((s) => s.id === sessionId)) ?? null;
  const activeSession: SessionItem | null =
    activeModule?.sessions.find((s) => s.id === sessionId) ?? null;

  // Skipped entirely at materialCount 0 — nothing to fetch, no empty-state
  // flash while a request that would return [] resolves.
  useEffect(() => {
    if (!activeSession) return;
    if (activeSession.materialCount === 0) {
      setMaterials([]);
      return;
    }
    let active = true;
    setMaterials(null);

    getSessionMaterials(activeSession.id)
      .then((data) => {
        if (!active) return;
        setMaterials(data);
      })
      .catch(() => {
        if (!active) return;
        setMaterials("error");
      });

    return () => {
      active = false;
    };
  }, [activeSession]);

  const gate = useSessionGate({ locale });
  if (!gate.ok) return gate.fallback;

  const loading = !errorKind && content === null;

  // Flattened in module -> session order, matching the overview's reading
  // order, so "next" always means "the next thing you'd open on that page".
  const flatSessions = content ? content.modules.flatMap((module) => module.sessions) : [];
  const currentIndex = activeSession
    ? flatSessions.findIndex((s) => s.id === activeSession.id)
    : -1;
  const previousSession = currentIndex > 0 ? flatSessions[currentIndex - 1] : null;
  const nextSession =
    currentIndex >= 0 && currentIndex < flatSessions.length - 1
      ? flatSessions[currentIndex + 1]
      : null;

  const treatment = activeSession ? getSessionTreatment(activeSession) : null;

  return (
    <main data-theme="light" className="min-h-screen bg-bg [&_:is(h1,h2,h3,h4,p,ul,ol)]:m-0">
      <div className="mx-auto flex w-full max-w-3xl flex-col gap-8 ps-5 pe-5 py-10">
        <Link
          href={`/${locale}/my-courses/${courseId}`}
          // hover:!text-text, not !text-accent-text — see the same note on
          // the overview page's back link (§2.3 light-lane accent rule).
          className="inline-flex w-fit items-center gap-2 text-label !text-text-muted hover:!text-text focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent focus-visible:ring-offset-2 focus-visible:ring-offset-bg"
        >
          <ArrowLeft aria-hidden="true" className="size-4 shrink-0 rtl:rotate-180" />
          {t.backToCourse}
        </Link>

        {errorKind ? (
          <div className="flex flex-col items-start gap-4 rounded-card border border-danger-border bg-danger-soft p-6">
            <div className="flex flex-col gap-1">
              <h1 className="text-h3 text-danger">{errorTitle(errorKind, t)}</h1>
              {errorHint(errorKind, t) ? (
                <p className="text-body text-danger">{errorHint(errorKind, t)}</p>
              ) : null}
            </div>
            <Button variant="secondary" onClick={() => setReloadKey((key) => key + 1)}>
              {t.retry}
            </Button>
          </div>
        ) : loading || !content ? (
          <SessionDetailSkeleton />
        ) : !activeSession || !activeModule || !treatment ? (
          <div className="flex flex-col items-start gap-4 rounded-card border border-border bg-surface p-6">
            <div className="flex flex-col gap-1">
              <h1 className="text-h3 text-text">{t.sessionNotFoundTitle}</h1>
              <p className="text-body text-text-secondary">{t.sessionNotFoundHint}</p>
            </div>
            <Button asChild variant="secondary">
              <Link href={`/${locale}/my-courses/${courseId}`}>{t.backToCourse}</Link>
            </Button>
          </div>
        ) : (
          <>
            <div className="flex flex-col items-start gap-4 rounded-card border border-border bg-surface p-6">
              <div className="flex flex-wrap items-center gap-2">
                <Badge variant="neutral">{typeLabel(activeSession.type, dictionary.student)}</Badge>
                {treatment.timing === "live-now" ? (
                  <Badge variant="success">{t.liveNow}</Badge>
                ) : null}
                {treatment.proximity === "today" ? (
                  <Badge variant="warning">{t.today}</Badge>
                ) : null}
                {treatment.proximity === "tomorrow" ? (
                  <Badge variant="warning">{t.tomorrow}</Badge>
                ) : null}
              </div>

              <div className="flex flex-col gap-1">
                <h1 className="text-h1 text-text">{activeSession.title}</h1>
                <p className="text-meta text-text-muted">
                  {activeModule.title} · {content.courseTitle}
                </p>
              </div>

              {activeSession.scheduledAt ? (
                <p className="flex items-start gap-3 text-h3 text-text">
                  <Calendar aria-hidden="true" className="mt-1 size-5 shrink-0 text-text-muted" />
                  {formatDateTime(activeSession.scheduledAt, locale)}
                  {activeSession.durationMinutes ? (
                    <span className="text-body text-text-muted">
                      · {getDurationAndCount(activeSession.durationMinutes, t)}
                    </span>
                  ) : null}
                </p>
              ) : null}

              {activeSession.type === "in_person" && activeSession.location ? (
                <p className="flex items-center gap-2 text-meta text-text-muted">
                  <MapPin aria-hidden="true" className="size-4 shrink-0" />
                  {format(t.locatedAt, { location: activeSession.location })}
                </p>
              ) : null}

              {activeSession.description ? (
                <p className="text-body text-text-secondary">{activeSession.description}</p>
              ) : null}

              {treatment.action.kind === "join" ? (
                <Button asChild size="lg">
                  <a href={externalHref(treatment.action.url)} target="_blank" rel="noreferrer">
                    {t.join}
                  </a>
                </Button>
              ) : treatment.action.kind === "watch" ? (
                <Button asChild variant="secondary" size="lg">
                  <a href={externalHref(treatment.action.url)} target="_blank" rel="noreferrer">
                    {treatment.action.variant === "recording" ? t.watchRecording : t.watchLesson}
                  </a>
                </Button>
              ) : null}
            </div>

            <section className="flex flex-col gap-4">
              <h2 className="text-h2 text-text">{t.materials}</h2>
              {materials === null ? (
                <div className="h-16 w-full animate-pulse rounded-card bg-surface-2" aria-hidden="true" />
              ) : materials === "error" ? (
                <p className="text-body text-text-muted">{t.loadError}</p>
              ) : (
                <MaterialList materials={materials} emptyText={t.noMaterials} locale={locale} t={t} />
              )}
            </section>

            {previousSession || nextSession ? (
              <nav
                aria-label={t.previous + " / " + t.next}
                className="flex items-center justify-between gap-4 border-t border-border pt-6"
              >
                {previousSession ? (
                  <Link
                    href={`/${locale}/my-courses/${courseId}/sessions/${previousSession.id}`}
                    className="flex items-center gap-2 text-body !text-text-secondary hover:!text-text focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent"
                  >
                    <ChevronLeft aria-hidden="true" className="size-4 shrink-0 rtl:rotate-180" />
                    <span className="flex flex-col items-start">
                      <span className="text-meta text-text-muted">{t.previous}</span>
                      <span className="truncate">{previousSession.title}</span>
                    </span>
                  </Link>
                ) : (
                  <span />
                )}

                {nextSession ? (
                  <Link
                    href={`/${locale}/my-courses/${courseId}/sessions/${nextSession.id}`}
                    className="flex items-center gap-2 text-end text-body !text-text-secondary hover:!text-text focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent"
                  >
                    <span className="flex flex-col items-end">
                      <span className="text-meta text-text-muted">{t.next}</span>
                      <span className="truncate">{nextSession.title}</span>
                    </span>
                    <ChevronRight aria-hidden="true" className="size-4 shrink-0 rtl:rotate-180" />
                  </Link>
                ) : (
                  <span />
                )}
              </nav>
            ) : null}
          </>
        )}
      </div>
    </main>
  );
}

function getDurationAndCount(minutes: number, t: Dictionary["courseContent"]): string {
  return format(getDurationTemplate(minutes, t), { count: minutes });
}
