"use client";

import { use, useEffect, useState } from "react";
import Link from "next/link";
import { ArrowLeft } from "lucide-react";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { useAuth } from "@/lib/auth";
import { useSessionGate } from "@/components/SessionGuard";
import {
  ApiRequestError,
  MaterialItem,
  MyCourseContent,
  getMyCourseContent,
  getMyCourseGrades,
  getModuleMaterials
} from "@/lib/api";
import { defaultLocale, getDictionary, isLocale, type Dictionary, type Locale } from "@/lib/i18n";
import { GradesPanel, type GradesState } from "./GradesPanel";
import { MaterialList } from "./MaterialList";
import { ModuleNav } from "./ModuleNav";
import { SessionRow } from "./SessionRow";
import { CourseContentSkeleton } from "./skeletons";

// DESIGN_LANGUAGE.md §4 #5 — the enrolled student's course content / session-
// navigation space. NOT a lesson-reading surface: CodeForge is live-cohort
// (PRODUCT.md), there is no Lesson entity and no long-form body field, and
// SessionProgress/"mark complete" is vestigial Phase-0 schema with no reader
// or writer (ARCHITECTURE.md §7) — neither is built here, on purpose.
//
// Replaces the flat legacy dump at this route: module nav (start-side) +
// every module's sessions, quick links to assessments/assignments, module
// resources, and a rebuilt grades panel. Session type + real-time state
// drives each row's badges (see ./sessionState.ts); the row itself carries
// no action — the single primary lives on the session detail page.
type ContentErrorKind = "not-found" | "access-denied" | "generic";

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

function ModuleResources({
  resources,
  locale,
  t
}: {
  resources: MaterialItem[] | "error" | undefined;
  locale: Locale;
  t: Dictionary["courseContent"];
}) {
  if (resources === undefined) {
    return (
      <div className="h-16 w-full animate-pulse rounded-card bg-surface-2" aria-hidden="true" />
    );
  }
  if (resources === "error") {
    return <p className="text-body text-text-muted">{t.loadError}</p>;
  }
  return (
    <MaterialList materials={resources} emptyText={t.noResources} locale={locale} t={t} />
  );
}

export default function CourseContentPage({
  params
}: {
  params: Promise<{ locale: string; courseId: string }>;
}) {
  const { locale: rawLocale, courseId } = use(params);
  const locale = isLocale(rawLocale) ? rawLocale : defaultLocale;
  const dictionary = getDictionary(locale);
  const t = dictionary.courseContent;

  const { session } = useAuth();
  const [content, setContent] = useState<MyCourseContent | null>(null);
  const [errorKind, setErrorKind] = useState<ContentErrorKind | null>(null);
  const [moduleResources, setModuleResources] = useState<
    Record<string, MaterialItem[] | "error" | undefined>
  >({});
  const [gradesState, setGradesState] = useState<GradesState>({ status: "loading" });
  const [reloadKey, setReloadKey] = useState(0);

  // The page's spine. A failure here replaces the whole page with a designed
  // panel, branched by status so a genuinely-unenrolled student sees
  // "you don't have access" rather than the server's raw English detail —
  // CourseContentAuthorization throws UnauthorizedAccessException, which
  // ExceptionHandlingMiddleware maps to 401, not 403 (API_CONVENTIONS.md).
  useEffect(() => {
    if (!session) return;
    let active = true;

    setErrorKind(null);
    setContent(null);
    setModuleResources({});

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

  // One call per module, allSettled so one module's failure degrades only its
  // own resources block (§5 partial-failure rule).
  useEffect(() => {
    if (!content || content.modules.length === 0) return;
    let active = true;

    Promise.allSettled(content.modules.map((module) => getModuleMaterials(module.id))).then(
      (results) => {
        if (!active) return;
        const next: Record<string, MaterialItem[] | "error"> = {};
        content.modules.forEach((module, index) => {
          const result = results[index];
          next[module.id] = result.status === "fulfilled" ? result.value : "error";
        });
        setModuleResources(next);
      }
    );

    return () => {
      active = false;
    };
  }, [content]);

  // Its own failure state, entirely separate from content — a grades outage
  // never blocks the page a student came here for.
  useEffect(() => {
    if (!session) return;
    let active = true;
    setGradesState({ status: "loading" });

    getMyCourseGrades(courseId)
      .then((grades) => {
        if (!active) return;
        setGradesState({ status: "ready", grades });
      })
      .catch(() => {
        if (!active) return;
        setGradesState({ status: "error" });
      });

    return () => {
      active = false;
    };
  }, [session, courseId, reloadKey]);

  const gate = useSessionGate({ locale });
  if (!gate.ok) return gate.fallback;

  const loading = !errorKind && content === null;

  return (
    <main data-theme="light" className="min-h-screen bg-bg [&_:is(h1,h2,h3,h4,p,ul,ol)]:m-0">
      <div className="mx-auto flex w-full max-w-5xl flex-col gap-8 ps-5 pe-5 py-10">
        <Link
          href={`/${locale}/dashboard`}
          // hover:!text-text, not !text-accent-text (course-detail's dark-lane
          // pattern): this link sits directly on --bg, and §2.3's light-lane
          // rule makes accent-text AA-safe only on --surface, inside a card.
          className="inline-flex w-fit items-center gap-2 text-label !text-text-muted hover:!text-text focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent focus-visible:ring-offset-2 focus-visible:ring-offset-bg"
        >
          <ArrowLeft aria-hidden="true" className="size-4 shrink-0 rtl:rotate-180" />
          {t.backToDashboard}
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
          <CourseContentSkeleton />
        ) : (
          <>
            <h1 className="text-h1 text-text">{content.courseTitle}</h1>

            {content.modules.length === 0 ? (
              <div className="flex flex-col gap-2 rounded-card border border-border bg-surface p-6">
                <h2 className="text-h3 text-text">{t.emptyModulesTitle}</h2>
                <p className="text-body text-text-secondary">{t.emptyModulesHint}</p>
              </div>
            ) : (
              <div className="flex flex-col gap-8 sm:flex-row">
                <ModuleNav modules={content.modules} t={t} />

                <div className="flex min-w-0 flex-1 flex-col gap-10">
                  {content.modules.map((module) => {
                    const links = [
                      ...module.assessments.map((assessment) => ({
                        id: assessment.id,
                        title: assessment.title,
                        href: `/${locale}/my-courses/${courseId}/assessments/${assessment.id}`,
                        badge:
                          assessment.type === "exam"
                            ? dictionary.student.exam
                            : dictionary.student.quiz
                      })),
                      ...module.assignments.map((assignment) => ({
                        id: assignment.id,
                        title: assignment.title,
                        href: `/${locale}/my-courses/${courseId}/assignments/${assignment.id}`,
                        badge: dictionary.student.assignment
                      }))
                    ];

                    return (
                      <section
                        key={module.id}
                        id={`module-${module.id}`}
                        className="flex scroll-mt-6 flex-col gap-4"
                      >
                        <div className="flex flex-col gap-1">
                          <h2 className="text-h2 text-text">{module.title}</h2>
                          {module.description ? (
                            <p className="text-body text-text-secondary">{module.description}</p>
                          ) : null}
                        </div>

                        {module.sessions.length === 0 ? (
                          <p className="text-body text-text-muted">{t.emptySessionsInModule}</p>
                        ) : (
                          <div className="flex flex-col gap-3">
                            {module.sessions.map((moduleSession) => (
                              <SessionRow
                                key={moduleSession.id}
                                session={moduleSession}
                                courseId={courseId}
                                locale={locale}
                                t={t}
                                student={dictionary.student}
                              />
                            ))}
                          </div>
                        )}

                        {links.length > 0 ? (
                          <div className="flex flex-col">
                            {links.map((link) => (
                              <Link
                                key={link.id}
                                href={link.href}
                                className="flex items-center justify-between gap-3 border-b border-border py-2 !text-text-secondary last:border-0 hover:!text-text"
                              >
                                <span className="text-body">{link.title}</span>
                                <Badge variant="neutral">{link.badge}</Badge>
                              </Link>
                            ))}
                          </div>
                        ) : null}

                        <div className="flex flex-col gap-2">
                          <h3 className="text-h3 text-text">{t.resources}</h3>
                          <ModuleResources
                            resources={moduleResources[module.id]}
                            locale={locale}
                            t={t}
                          />
                        </div>
                      </section>
                    );
                  })}
                </div>
              </div>
            )}

            <GradesPanel state={gradesState} courseId={courseId} locale={locale} t={t} />
          </>
        )}
      </div>
    </main>
  );
}
