"use client";

import { use, useEffect, useState } from "react";
import Link from "next/link";
import { useAuth } from "@/lib/auth";
import { ApiRequestError, CourseListItem, getAllCourses, getAssignedCourses } from "@/lib/api";
import { defaultLocale, getDictionary, isLocale } from "@/lib/i18n";

export default function InstructorCoursesPage({
  params
}: {
  params: Promise<{ locale: string }>;
}) {
  const { locale: rawLocale } = use(params);
  const locale = isLocale(rawLocale) ? rawLocale : defaultLocale;
  const t = getDictionary(locale).instructor;

  const { session } = useAuth();
  const [courses, setCourses] = useState<CourseListItem[] | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!session) return;
    const load = session.role === "admin" ? getAllCourses : getAssignedCourses;
    load(session.accessToken)
      .then(setCourses)
      .catch((err) => setError(err instanceof ApiRequestError ? err.message : t.loadError));
  }, [session, t.loadError]);

  if (!session || (session.role !== "admin" && session.role !== "instructor")) {
    return (
      <main className="container">
        <p className="notice err">{t.signInRequired}</p>
      </main>
    );
  }

  return (
    <main className="container">
      <h1>{t.myCourses}</h1>
      {error && <p className="notice err">{error}</p>}
      {courses === null && !error && <p className="muted">…</p>}
      {courses !== null && courses.length === 0 && <p className="muted">{t.noCourses}</p>}
      <div className="grid">
        {courses?.map((course) => (
          <Link
            key={course.id}
            className="card"
            href={`/${locale}/instructor/courses/${course.id}`}
          >
            <h3>{course.title}</h3>
            <p className="muted">{course.category ?? "—"}</p>
            <span className="badge">{course.status}</span>
          </Link>
        ))}
      </div>
    </main>
  );
}
