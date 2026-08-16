"use client";

import { use, useEffect, useState } from "react";
import Link from "next/link";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import {
  Card,
  CardContent,
  CardFooter,
  CardHeader,
  CardTitle
} from "@/components/ui/card";
import { useAuth } from "@/lib/auth";
import { useSessionGate } from "@/components/SessionGuard";
import {
  ApiRequestError,
  MyCourseGrades,
  MyCourseSummary,
  UpcomingItems,
  getMyCourseGrades,
  getMyCourses,
  getUpcomingItems
} from "@/lib/api";
import { defaultLocale, format, getDictionary, isLocale } from "@/lib/i18n";
import { formatCatalogDate, formatDateTime } from "@/lib/format";
import { NextSessionPanel } from "./NextSessionPanel";
import { ProgressSummary } from "./ProgressSummary";
import { DashboardSkeleton } from "./skeletons";

// DESIGN_LANGUAGE.md §4 #4 — the student dashboard, and the FIRST light-lane
// surface in the app (§1.3 "dark sells, light reads"). Everything below the
// legacy topbar is re-scoped to the §2.3 light tokens by the data-theme on
// <main>; every later reading/work surface should copy the mechanism from here.
//
// Bound strictly to data that exists. There is no progress-% and no pending-tasks
// section — DESIGN_LANGUAGE.md §4 #4 names both, but SessionProgress is written
// and read by nothing, and no endpoint aggregates assignments across a student's
// courses. Both are recorded as deferred backend follow-ups in ARCHITECTURE.md §7
// rather than invented here.
export default function DashboardPage({
  params
}: {
  params: Promise<{ locale: string }>;
}) {
  const { locale: rawLocale } = use(params);
  const locale = isLocale(rawLocale) ? rawLocale : defaultLocale;
  const dictionary = getDictionary(locale);
  const t = dictionary.dashboard;

  const { session } = useAuth();
  const [upcoming, setUpcoming] = useState<UpcomingItems | null>(null);
  const [courses, setCourses] = useState<MyCourseSummary[] | null>(null);
  const [grades, setGrades] = useState<Record<string, MyCourseGrades | null> | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [reloadKey, setReloadKey] = useState(0);

  // Both calls together: the page has nothing useful to show until each has
  // landed, so a single failure state covers them rather than two half-pages.
  useEffect(() => {
    if (!session) return;
    let active = true;

    setError(null);
    setUpcoming(null);
    setCourses(null);
    setGrades(null);

    Promise.all([getUpcomingItems(), getMyCourses()])
      .then(([items, list]) => {
        if (!active) return;
        setUpcoming(items);
        setCourses(list);
      })
      .catch((err) => {
        if (!active) return;
        setError(err instanceof ApiRequestError ? err.message : t.loadError);
      });

    return () => {
      active = false;
    };
  }, [session, reloadKey, t.loadError]);

  // Grades are per-course by design, so a cross-course summary costs one call
  // per enrolled course — a handful at this scale. allSettled, not all: one
  // course's failure degrades its own row (see ProgressSummary) instead of
  // taking down a section that is otherwise fine.
  useEffect(() => {
    if (!courses || courses.length === 0) return;
    let active = true;

    Promise.allSettled(courses.map((course) => getMyCourseGrades(course.courseId))).then(
      (results) => {
        if (!active) return;
        const next: Record<string, MyCourseGrades | null> = {};
        courses.forEach((course, index) => {
          const result = results[index];
          next[course.courseId] = result.status === "fulfilled" ? result.value : null;
        });
        setGrades(next);
      }
    );

    return () => {
      active = false;
    };
  }, [courses]);

  const gate = useSessionGate({ locale });
  if (!gate.ok) return gate.fallback;

  const loading = !error && (upcoming === null || courses === null);
  const nextSession = upcoming?.upcomingSessions[0] ?? null;
  const hasCourses = (courses?.length ?? 0) > 0;

  return (
    // data-theme re-scopes every token below it (§2.3); min-h-screen + bg-bg
    // paints the light ground across the viewport so the root layout's dark
    // body cannot show through under a short page. The inherited topbar stays
    // dark — the authenticated shell is deferred (ARCHITECTURE.md §7).
    //
    // The margin reset neutralizes globals.css's legacy preflight-restore block,
    // which re-adds margin-block to h1/h2/h3/p for pre-design-system pages. All
    // spacing here is flex-gap driven, so those margins would double up.
    <main
      data-theme="light"
      className="min-h-screen bg-bg [&_:is(h1,h2,h3,p)]:m-0"
    >
      <div className="mx-auto flex w-full max-w-4xl flex-col gap-10 ps-5 pe-5 py-10">
        <h1 className="text-h1 text-text">{t.title}</h1>

        {error ? (
          <div className="flex flex-col items-start gap-4 rounded-card border border-danger-border bg-danger-soft p-6">
            <p className="text-body text-danger">{error}</p>
            {/* Not the catalog's RetryButton: router.refresh() re-renders the
                route, which cannot re-run a client useEffect. Bumping the key
                re-runs the fetch that actually failed. */}
            <Button variant="secondary" onClick={() => setReloadKey((key) => key + 1)}>
              {t.retry}
            </Button>
          </div>
        ) : loading ? (
          <DashboardSkeleton />
        ) : (
          <>
            <NextSessionPanel
              session={nextSession}
              hasCourses={hasCourses}
              locale={locale}
              t={t}
              student={dictionary.student}
            />

            {/* Omitted entirely at zero courses rather than stacking a second
                empty state — the panel above already carries that message and
                the route to fixing it. */}
            {hasCourses ? (
              <section className="flex flex-col gap-4">
                <h2 className="text-h2 text-text">{t.myCourses}</h2>
                <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
                  {courses?.map((course) => (
                    <Card key={course.courseId}>
                      <CardHeader>
                        <CardTitle>{course.title}</CardTitle>
                      </CardHeader>
                      <CardContent className="flex flex-col gap-2">
                        {course.description ? (
                          <p className="line-clamp-2 text-body text-text-secondary">
                            {course.description}
                          </p>
                        ) : null}
                        <p className="text-meta text-text-muted">{course.cohortName}</p>
                        <p className="text-meta text-text-muted">
                          {format(t.cohortDates, {
                            start: formatCatalogDate(course.cohortStartDate, locale),
                            end: formatCatalogDate(course.cohortEndDate, locale)
                          })}
                        </p>
                      </CardContent>
                      <CardFooter>
                        {/* Secondary, not primary: the Join button in the panel
                            above is the screen's one primary (§3). */}
                        <Button asChild variant="secondary" size="sm">
                          <Link href={`/${locale}/my-courses/${course.courseId}`}>
                            {t.openCourse}
                          </Link>
                        </Button>
                      </CardFooter>
                    </Card>
                  ))}
                </div>
              </section>
            ) : null}

            {hasCourses && courses ? (
              <ProgressSummary
                courses={courses}
                grades={grades}
                locale={locale}
                t={t}
              />
            ) : null}

            {/* Quiet by construction — a hairline-separated list rather than
                cards, so it reads as the page's footnote and never competes
                with the session panel. */}
            <section className="flex flex-col gap-4">
              <h2 className="text-h2 text-text">{t.announcements}</h2>
              {upcoming && upcoming.recentAnnouncements.length === 0 ? (
                <p className="text-body text-text-muted">{t.noAnnouncements}</p>
              ) : (
                <ul className="flex flex-col gap-4">
                  {upcoming?.recentAnnouncements.map((announcement) => (
                    <li
                      key={announcement.id}
                      className="flex flex-col gap-2 border-b border-border pb-4 last:border-0 last:pb-0"
                    >
                      <div className="flex flex-wrap items-center gap-2">
                        <h3 className="text-body font-semibold text-text">
                          {announcement.title}
                        </h3>
                        {announcement.courseTitle ? (
                          <Badge variant="neutral">{announcement.courseTitle}</Badge>
                        ) : null}
                      </div>
                      <p className="text-body whitespace-pre-line text-text-secondary">
                        {announcement.body}
                      </p>
                      <p className="text-meta text-text-muted">
                        {formatDateTime(announcement.createdAt, locale)}
                      </p>
                    </li>
                  ))}
                </ul>
              )}
            </section>
          </>
        )}
      </div>
    </main>
  );
}
