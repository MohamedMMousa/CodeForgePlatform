"use client";

import { use, useEffect, useState } from "react";
import Link from "next/link";
import { useAuth } from "@/lib/auth";
import { ApiRequestError, Certificate, getMyCertificates } from "@/lib/api";
import { defaultLocale, getDictionary, isLocale } from "@/lib/i18n";
import { Pagination } from "@/components/Pagination";

const PAGE_SIZE = 20;

export default function MyCertificatesPage({
  params
}: {
  params: Promise<{ locale: string }>;
}) {
  const { locale: rawLocale } = use(params);
  const locale = isLocale(rawLocale) ? rawLocale : defaultLocale;
  const dictionary = getDictionary(locale);
  const tc = dictionary.certificates;

  const { session } = useAuth();
  const [certificates, setCertificates] = useState<Certificate[] | null>(null);
  const [totalCount, setTotalCount] = useState(0);
  const [page, setPage] = useState(1);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!session) return;
    getMyCertificates(session.accessToken, { page, pageSize: PAGE_SIZE })
      .then((result) => {
        setCertificates(result.items);
        setTotalCount(result.totalCount);
      })
      .catch((err) => setError(err instanceof ApiRequestError ? err.message : tc.loadError));
  }, [session, page, tc.loadError]);

  if (!session) {
    return (
      <main className="container">
        <p className="notice err">
          <Link href={`/${locale}/login`}>{getDictionary(locale).home.signIn}</Link>
        </p>
      </main>
    );
  }

  const tierLabel = (tier: string) => (tier === "completion" ? tc.completion : tc.participation);

  return (
    <main className="container">
      <h1>{tc.myCertificates}</h1>
      {error && <p className="notice err">{error}</p>}
      {certificates !== null && certificates.length === 0 && (
        <p className="muted">{tc.noCertificates}</p>
      )}

      {certificates?.map((cert) => (
        <div
          key={cert.id}
          className="card certificate-card"
          style={{
            marginBottom: "1rem",
            borderWidth: "2px",
            opacity: cert.isRevoked ? 0.6 : 1
          }}
        >
          <div className="muted" style={{ letterSpacing: "0.1em", textTransform: "uppercase", fontSize: "0.75rem" }}>
            {getDictionary(locale).appName} — {tc.verifiedCertificate}
          </div>
          <h2 style={{ margin: "0.5rem 0" }}>{cert.courseTitle}</h2>
          <p>
            <span className="badge">{tierLabel(cert.tier)}</span>
            {cert.isRevoked && <span className="badge" style={{ background: "var(--muted)" }}>{tc.revoked}</span>}
          </p>
          <p>{tc.student}: <strong>{cert.studentName}</strong></p>
          <p className="muted">
            {tc.serial}: {cert.serialNumber} · {tc.issuedOn}: {new Date(cert.issuedAt).toLocaleDateString(locale)}
          </p>
          <p className="muted">
            {tc.attendanceRate}: {cert.attendanceRate}% · {tc.assessmentsPassed}: {cert.assessmentsPassed ? tc.yes : tc.no}
          </p>
          <p className="muted">{tc.verificationCode}: <code>{cert.verificationCode}</code></p>
          <div style={{ display: "flex", gap: "0.5rem", marginTop: "0.5rem" }}>
            <Link className="btn secondary" href={`/${locale}/verify?code=${cert.verificationCode}`}>
              {tc.view}
            </Link>
            <button className="btn secondary" onClick={() => window.print()}>
              {tc.print}
            </button>
          </div>
        </div>
      ))}
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
