"use client";

import { use, useEffect, useState } from "react";
import Link from "next/link";
import { useAuth } from "@/lib/auth";
import { useSessionGate } from "@/components/SessionGuard";
import {
  ApiRequestError,
  EnrollmentRequestDetail,
  approveEnrollmentRequest,
  cancelEnrollment,
  downloadAuthenticatedFile,
  getEnrollmentRequestById,
  rejectEnrollmentRequest
} from "@/lib/api";
import { defaultLocale, getDictionary, isLocale } from "@/lib/i18n";

export default function AdminEnrollmentRequestDetailPage({
  params
}: {
  params: Promise<{ locale: string; id: string }>;
}) {
  const { locale: rawLocale, id } = use(params);
  const locale = isLocale(rawLocale) ? rawLocale : defaultLocale;
  const t = getDictionary(locale).admin;

  const { session } = useAuth();
  const [request, setRequest] = useState<EnrollmentRequestDetail | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [rejectionReason, setRejectionReason] = useState("");
  const [busy, setBusy] = useState(false);
  const [cancelBusyId, setCancelBusyId] = useState<string | null>(null);
  const [cancelReason, setCancelReason] = useState("");
  const [markRefunded, setMarkRefunded] = useState(false);

  function onError(err: unknown) {
    setError(err instanceof ApiRequestError ? err.message : t.loadError);
  }

  function load() {
    if (!session) return;
    getEnrollmentRequestById(id).then(setRequest).catch(onError);
  }

  useEffect(load, [session, id]); // eslint-disable-line react-hooks/exhaustive-deps

  const gate = useSessionGate({ locale, roles: ["admin"], bare: true });
  if (!gate.ok) return gate.fallback;

  async function onApprove() {
    if (!session) return;
    setBusy(true);
    try {
      await approveEnrollmentRequest(id);
      load();
    } catch (err) {
      onError(err);
    } finally {
      setBusy(false);
    }
  }

  async function onReject() {
    if (!session || !rejectionReason.trim()) return;
    setBusy(true);
    try {
      await rejectEnrollmentRequest(id, rejectionReason.trim());
      load();
    } catch (err) {
      onError(err);
    } finally {
      setBusy(false);
    }
  }

  function onDownloadProof() {
    if (!request || !session) return;
    downloadAuthenticatedFile(request.paymentProofDownloadUrl).catch(onError);
  }

  async function onCancelEnrollment(enrollmentId: string) {
    if (!session || !cancelReason.trim()) return;
    setCancelBusyId(enrollmentId);
    try {
      await cancelEnrollment(enrollmentId, cancelReason.trim(), markRefunded);
      setCancelReason("");
      setMarkRefunded(false);
    } catch (err) {
      onError(err);
    } finally {
      setCancelBusyId(null);
    }
  }

  return (
    <>
      <p>
        <Link href={`/${locale}/admin/enrollment-requests`}>← {t.backToRequests}</Link>
      </p>
      {error && <p className="notice err">{error}</p>}
      {!request && !error && <p className="muted">…</p>}

      {request && (
        <>
          <h2>{request.applicantName}</h2>
          <table className="table" style={{ maxWidth: "40rem" }}>
            <tbody>
              <tr><td>{t.applicantEmailLabel}</td><td>{request.applicantEmail}</td></tr>
              <tr><td>{t.applicantPhoneLabel}</td><td>{request.applicantPhone ?? "—"}</td></tr>
              <tr><td>{t.courseOrTrack}</td><td>{request.courseTitle ?? request.trackTitle ?? "—"}</td></tr>
              <tr><td>{t.paymentMethod}</td><td>{request.paymentMethod}</td></tr>
              <tr>
                <td>{t.paymentProof}</td>
                <td>
                  <button className="btn secondary" onClick={onDownloadProof}>
                    {t.downloadPaymentProof}
                  </button>
                </td>
              </tr>
              <tr><td>{t.originalPrice}</td><td>{request.originalPrice.toLocaleString(locale)} EGP</td></tr>
              <tr><td>{t.couponCode}</td><td>{request.couponCode ?? "—"}</td></tr>
              <tr><td>{t.discountAmount}</td><td>{request.discountAmount.toLocaleString(locale)} EGP</td></tr>
              <tr><td>{t.finalPrice}</td><td>{request.finalPrice.toLocaleString(locale)} EGP</td></tr>
              <tr><td>{t.status}</td><td><span className="badge">{request.status}</span></td></tr>
              {request.rejectionReason && (
                <tr><td>{t.rejectionReasonLabel}</td><td>{request.rejectionReason}</td></tr>
              )}
              {request.reviewedByName && (
                <>
                  <tr><td>{t.reviewedBy}</td><td>{request.reviewedByName}</td></tr>
                  <tr><td>{t.reviewedAt}</td><td>{request.reviewedAt && new Date(request.reviewedAt).toLocaleString(locale)}</td></tr>
                </>
              )}
            </tbody>
          </table>

          {request.targetCohorts.length > 0 && (
            <>
              <h3 style={{ marginTop: "1rem" }}>{t.targetCohorts}</h3>
              <ul>
                {request.targetCohorts.map((c) => (
                  <li key={c.cohortId}>{c.courseTitle} — {c.cohortName}</li>
                ))}
              </ul>
            </>
          )}

          {request.status === "pending" && (
            <div style={{ marginTop: "1.5rem", display: "flex", flexDirection: "column", gap: "0.75rem", maxWidth: "24rem" }}>
              <button className="btn" disabled={busy} onClick={onApprove}>
                {busy ? t.approving : t.approve}
              </button>
              <label>
                {t.rejectionReasonLabel}
                <textarea value={rejectionReason} onChange={(e) => setRejectionReason(e.target.value)} />
              </label>
              <button className="btn secondary" disabled={busy || !rejectionReason.trim()} onClick={onReject}>
                {busy ? t.rejecting : t.reject}
              </button>
            </div>
          )}

          {request.resultingEnrollmentIds.length > 0 && (
            <>
              <h3 style={{ marginTop: "1.5rem" }}>{t.resultingEnrollments}</h3>
              <div style={{ display: "flex", flexDirection: "column", gap: "0.5rem", maxWidth: "28rem" }}>
                <label>
                  {t.cancelReasonLabel}
                  <input value={cancelReason} onChange={(e) => setCancelReason(e.target.value)} />
                </label>
                <label>
                  <input type="checkbox" checked={markRefunded} onChange={(e) => setMarkRefunded(e.target.checked)} />{" "}
                  {t.markAsRefunded}
                </label>
                {request.resultingEnrollmentIds.map((eid) => (
                  <button
                    key={eid}
                    className="btn secondary"
                    disabled={cancelBusyId === eid || !cancelReason.trim()}
                    onClick={() => onCancelEnrollment(eid)}
                  >
                    {t.cancelEnrollment} ({eid.slice(0, 8)})
                  </button>
                ))}
              </div>
            </>
          )}
        </>
      )}
    </>
  );
}
