"use client";

import { use, useEffect, useState } from "react";
import Link from "next/link";
import { useAuth } from "@/lib/auth";
import { ApiRequestError, EnrollmentRequestResult, getEnrollmentRequests } from "@/lib/api";
import { defaultLocale, getDictionary, isLocale } from "@/lib/i18n";

export default function AdminEnrollmentRequestsPage({
  params
}: {
  params: Promise<{ locale: string }>;
}) {
  const { locale: rawLocale } = use(params);
  const locale = isLocale(rawLocale) ? rawLocale : defaultLocale;
  const t = getDictionary(locale).admin;

  const { session } = useAuth();
  const [requests, setRequests] = useState<EnrollmentRequestResult[] | null>(null);
  const [statusFilter, setStatusFilter] = useState("pending");
  const [error, setError] = useState<string | null>(null);

  function onError(err: unknown) {
    setError(err instanceof ApiRequestError ? err.message : t.loadError);
  }

  useEffect(() => {
    if (!session) return;
    getEnrollmentRequests({ status: statusFilter || undefined }, session.accessToken)
      .then(setRequests)
      .catch(onError);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [session, statusFilter]);

  if (!session || session.role !== "admin") {
    return <p className="notice err">{getDictionary(locale).instructor.signInRequired}</p>;
  }

  const statusLabel = (s: string) =>
    s === "pending" ? t.requestStatusPending : s === "approved" ? t.requestStatusApproved : t.requestStatusRejected;

  return (
    <>
      <h2>{t.requestsTitle}</h2>
      {error && <p className="notice err">{error}</p>}

      <div style={{ marginBottom: "1rem" }}>
        <label>
          {t.status}{" "}
          <select value={statusFilter} onChange={(e) => setStatusFilter(e.target.value)}>
            <option value="">{t.all}</option>
            <option value="pending">{t.requestStatusPending}</option>
            <option value="approved">{t.requestStatusApproved}</option>
            <option value="rejected">{t.requestStatusRejected}</option>
          </select>
        </label>
      </div>

      {requests === null && !error && <p className="muted">…</p>}
      {requests !== null && requests.length === 0 && <p className="muted">{t.noRequests}</p>}
      {requests !== null && requests.length > 0 && (
        <div style={{ overflowX: "auto" }}>
          <table className="table">
            <thead>
              <tr>
                <th>{t.applicant}</th>
                <th>{t.courseOrTrack}</th>
                <th>{t.finalPrice}</th>
                <th>{t.status}</th>
                <th>{t.submittedAt}</th>
                <th>{t.actions}</th>
              </tr>
            </thead>
            <tbody>
              {requests.map((r) => (
                <tr key={r.id}>
                  <td>{r.applicantName}<br /><span className="muted">{r.applicantEmail}</span></td>
                  <td>{r.courseTitle ?? r.trackTitle ?? "—"}</td>
                  <td>{r.finalPrice.toLocaleString(locale)} EGP</td>
                  <td><span className="badge">{statusLabel(r.status)}</span></td>
                  <td>{new Date(r.createdAt).toLocaleDateString(locale)}</td>
                  <td>
                    <Link className="btn secondary" href={`/${locale}/admin/enrollment-requests/${r.id}`}>
                      {t.viewDetails}
                    </Link>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </>
  );
}
