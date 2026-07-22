"use client";

import { use, useEffect, useState } from "react";
import Link from "next/link";
import { useAuth } from "@/lib/auth";
import {
  AdminAcademicDashboard,
  AdminBusinessDashboard,
  ApiRequestError,
  getAdminAcademicDashboard,
  getAdminBusinessDashboard
} from "@/lib/api";
import { defaultLocale, getDictionary, isLocale } from "@/lib/i18n";

function Stat({ label, value }: { label: string; value: string | number }) {
  return (
    <div className="card" style={{ minWidth: "10rem" }}>
      <div style={{ fontSize: "1.75rem", fontWeight: 700 }}>{value}</div>
      <div className="muted">{label}</div>
    </div>
  );
}

const MONTHS = ["", "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"];

export default function AdminAnalyticsPage({
  params
}: {
  params: Promise<{ locale: string }>;
}) {
  const { locale: rawLocale } = use(params);
  const locale = isLocale(rawLocale) ? rawLocale : defaultLocale;
  const ta = getDictionary(locale).analytics;

  const { session } = useAuth();
  const [business, setBusiness] = useState<AdminBusinessDashboard | null>(null);
  const [academic, setAcademic] = useState<AdminAcademicDashboard | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!session || session.role !== "admin") return;
    getAdminBusinessDashboard(session.accessToken).then(setBusiness).catch(onError);
    getAdminAcademicDashboard(session.accessToken).then(setAcademic).catch(onError);
    function onError(err: unknown) {
      setError(err instanceof ApiRequestError ? err.message : ta.loadError);
    }
  }, [session, ta.loadError]);

  if (!session || session.role !== "admin") {
    return (
      <main className="container">
        <p className="notice err">{getDictionary(locale).instructor.signInRequired}</p>
      </main>
    );
  }

  return (
    <main className="container">
      <h1>{ta.title}</h1>
      {error && <p className="notice err">{error}</p>}

      <h2 style={{ marginTop: "1.5rem" }}>{ta.businessDashboard}</h2>
      {business && (
        <>
          <div className="grid" style={{ display: "flex", flexWrap: "wrap", gap: "0.75rem" }}>
            <Stat label={ta.totalStudents} value={business.totalStudents} />
            <Stat label={ta.publishedCourses} value={business.publishedCourses} />
            <Stat label={ta.publishedTracks} value={business.publishedTracks} />
            <Stat label={ta.activeEnrollments} value={business.activeEnrollments} />
            <Stat label={ta.pendingRequests} value={business.pendingEnrollmentRequests} />
            <Stat label={ta.totalRevenue} value={`${business.totalRevenue.toLocaleString(locale)} EGP`} />
            <Stat label={ta.totalLeads} value={business.totalLeads} />
            <Stat label={ta.uncontactedLeads} value={business.uncontactedLeads} />
            <Stat label={ta.openCohorts} value={business.openCohorts} />
          </div>

          {business.topCoursesByRevenue.length > 0 && (
            <div style={{ marginTop: "1rem", overflowX: "auto" }}>
              <h3>{ta.topCoursesByRevenue}</h3>
              <table className="table">
                <thead>
                  <tr>
                    <th>{ta.course}</th>
                    <th>{ta.revenue}</th>
                    <th>{ta.approvedRequests}</th>
                  </tr>
                </thead>
                <tbody>
                  {business.topCoursesByRevenue.map((c) => (
                    <tr key={c.courseId}>
                      <td>{c.title}</td>
                      <td>{c.revenue.toLocaleString(locale)} EGP</td>
                      <td>{c.approvedRequests}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}

          {business.enrollmentsByMonth.length > 0 && (
            <div style={{ marginTop: "1rem" }}>
              <h3>{ta.enrollmentsByMonth}</h3>
              <div style={{ display: "flex", gap: "0.5rem", alignItems: "flex-end", height: "8rem" }}>
                {business.enrollmentsByMonth.map((m) => {
                  const max = Math.max(...business.enrollmentsByMonth.map((x) => x.count), 1);
                  return (
                    <div key={`${m.year}-${m.month}`} style={{ textAlign: "center" }}>
                      <div
                        title={`${m.count}`}
                        style={{
                          width: "2rem",
                          height: `${(m.count / max) * 100}%`,
                          minHeight: "2px",
                          background: "var(--accent)",
                          borderRadius: "3px"
                        }}
                      />
                      <div className="muted" style={{ fontSize: "0.75rem" }}>{MONTHS[m.month]}</div>
                    </div>
                  );
                })}
              </div>
            </div>
          )}
        </>
      )}

      <h2 style={{ marginTop: "2rem" }}>{ta.academicDashboard}</h2>
      {academic && (
        <>
          <div className="grid" style={{ display: "flex", flexWrap: "wrap", gap: "0.75rem" }}>
            <Stat label={ta.certificatesIssued} value={academic.certificatesIssued} />
            <Stat label={ta.completionCerts} value={academic.completionCertificates} />
            <Stat label={ta.participationCerts} value={academic.participationCertificates} />
            <Stat label={ta.revokedCerts} value={academic.revokedCertificates} />
            <Stat label={ta.totalAssessments} value={academic.totalAssessments} />
            <Stat label={ta.submittedAttempts} value={academic.submittedAttempts} />
            <Stat label={ta.passRate} value={`${academic.assessmentPassRate}%`} />
            <Stat label={ta.totalAssignments} value={academic.totalAssignments} />
            <Stat label={ta.totalSubmissions} value={academic.totalSubmissions} />
          </div>

          <div style={{ marginTop: "1rem", overflowX: "auto" }}>
            <h3>{ta.perCourse}</h3>
            <table className="table">
              <thead>
                <tr>
                  <th>{ta.course}</th>
                  <th>{ta.students}</th>
                  <th>{ta.assessments}</th>
                  <th>{ta.attempts}</th>
                  <th>{ta.passRate}</th>
                  <th>{ta.certificates}</th>
                </tr>
              </thead>
              <tbody>
                {academic.courses.length === 0 && (
                  <tr>
                    <td colSpan={6} className="muted">{ta.noData}</td>
                  </tr>
                )}
                {academic.courses.map((c) => (
                  <tr key={c.courseId}>
                    <td>
                      <Link href={`/${locale}/instructor/courses/${c.courseId}`}>{c.title}</Link>
                    </td>
                    <td>{c.activeEnrollments}</td>
                    <td>{c.assessments}</td>
                    <td>{c.submittedAttempts}</td>
                    <td>{c.assessmentPassRate}%</td>
                    <td>{c.certificatesIssued}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </>
      )}
    </main>
  );
}
