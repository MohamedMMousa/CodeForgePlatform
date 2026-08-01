"use client";

import { use, useEffect, useState } from "react";
import Link from "next/link";
import { useAuth } from "@/lib/auth";
import {
  ApiRequestError,
  CourseListItem,
  InstructorDashboard,
  getAllCourses,
  getAssignedCourses,
  getInstructorDashboard
} from "@/lib/api";
import { defaultLocale, getDictionary, isLocale } from "@/lib/i18n";
import { Pagination } from "@/components/Pagination";

const PAGE_SIZE = 20;

export default function InstructorCoursesPage({
  params
}: {
  params: Promise<{ locale: string }>;
}) {
  const { locale: rawLocale } = use(params);
  const locale = isLocale(rawLocale) ? rawLocale : defaultLocale;
  const dictionary = getDictionary(locale);
  const t = dictionary.instructor;
  const ta = dictionary.analytics;

  const { session } = useAuth();
  const [courses, setCourses] = useState<CourseListItem[] | null>(null);
  const [totalCount, setTotalCount] = useState(0);
  const [page, setPage] = useState(1);
  const [dashboard, setDashboard] = useState<InstructorDashboard | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!session) return;
    const load = session.role === "admin" ? getAllCourses : getAssignedCourses;
    load({ page, pageSize: PAGE_SIZE })
      .then((result) => {
        setCourses(result.items);
        setTotalCount(result.totalCount);
      })
      .catch((err) => setError(err instanceof ApiRequestError ? err.message : t.loadError));
    if (session.role === "instructor") {
      getInstructorDashboard().then(setDashboard).catch(() => {});
    }
  }, [session, page, t.loadError]);

  if (!session || (session.role !== "admin" && session.role !== "instructor")) {
    return (
      <main className="container">
        <p className="notice err">{t.signInRequired}</p>
      </main>
    );
  }

  return (
    <main className="container">
      {session.role === "admin" && (
        <p>
          <Link className="btn secondary" href={`/${locale}/admin/analytics`}>
            {ta.title}
          </Link>
        </p>
      )}

      {dashboard && (
        <div style={{ marginBottom: "1.5rem" }}>
          <h2>{ta.instructorDashboard}</h2>
          <div style={{ display: "flex", flexWrap: "wrap", gap: "0.75rem" }}>
            <div className="card"><strong>{dashboard.assignedCourses}</strong> · {ta.assignedCourses}</div>
            <div className="card"><strong>{dashboard.totalActiveStudents}</strong> · {ta.totalActiveStudents}</div>
            <div className="card"><strong>{dashboard.certificatesIssued}</strong> · {ta.certificatesIssued}</div>
          </div>
          {dashboard.courses.length > 0 && (
            <div style={{ overflowX: "auto", marginTop: "0.75rem" }}>
              <table className="table">
                <thead>
                  <tr>
                    <th>{ta.course}</th>
                    <th>{ta.students}</th>
                    <th>{ta.assessments}</th>
                    <th>{ta.passRate}</th>
                    <th>{ta.certificates}</th>
                  </tr>
                </thead>
                <tbody>
                  {dashboard.courses.map((c) => (
                    <tr key={c.courseId}>
                      <td><Link href={`/${locale}/instructor/courses/${c.courseId}`}>{c.title}</Link></td>
                      <td>{c.activeEnrollments}</td>
                      <td>{c.assessments}</td>
                      <td>{c.assessmentPassRate}%</td>
                      <td>{c.certificatesIssued}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </div>
      )}

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
      <Pagination
        t={dictionary}
        page={page}
        pageSize={PAGE_SIZE}
        totalCount={totalCount}
        onPageChange={setPage}
      />
    </main>
  );
}
