"use client";

import { use, useEffect, useState } from "react";
import Link from "next/link";
import { useAuth } from "@/lib/auth";
import { useSessionGate } from "@/components/SessionGuard";
import { ApiRequestError, MyCourseSummary, getMyCourses } from "@/lib/api";
import { defaultLocale, getDictionary, isLocale } from "@/lib/i18n";

export default function MyCoursesPage({
  params
}: {
  params: Promise<{ locale: string }>;
}) {
  const { locale: rawLocale } = use(params);
  const locale = isLocale(rawLocale) ? rawLocale : defaultLocale;
  const dictionary = getDictionary(locale);
  const t = dictionary.student;

  const { session } = useAuth();
  const [courses, setCourses] = useState<MyCourseSummary[] | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!session) return;
    getMyCourses()
      .then(setCourses)
      .catch((err) => setError(err instanceof ApiRequestError ? err.message : t.coursesLoadError));
  }, [session, t.coursesLoadError]);

  const gate = useSessionGate({ locale });
  if (!gate.ok) return gate.fallback;

  return (
    <main className="cf-container">
      <h1>{t.myCourses}</h1>
      {error && <p className="notice err">{error}</p>}

      {courses !== null && courses.length === 0 && !error && (
        <>
          <p className="muted">{t.noCourses}</p>
          <Link className="btn secondary" href={`/${locale}/catalog`}>
            {dictionary.nav.catalog}
          </Link>
        </>
      )}

      {courses?.map((course) => (
        <div key={course.courseId} className="card" style={{ marginBottom: "0.75rem" }}>
          <h3>{course.title}</h3>
          {course.description && <p className="muted">{course.description}</p>}
          <p className="muted">
            {course.cohortName} · {new Date(course.cohortStartDate).toLocaleDateString(locale)} –{" "}
            {new Date(course.cohortEndDate).toLocaleDateString(locale)}
          </p>
          <Link className="btn" href={`/${locale}/my-courses/${course.courseId}`}>
            {t.goToCourse}
          </Link>
        </div>
      ))}
    </main>
  );
}
