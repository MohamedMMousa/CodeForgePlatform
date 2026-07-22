"use client";

import { use, useEffect, useState } from "react";
import { ApiRequestError, CertificateVerification, verifyCertificate } from "@/lib/api";
import { defaultLocale, getDictionary, isLocale } from "@/lib/i18n";

export default function VerifyCertificatePage({
  params
}: {
  params: Promise<{ locale: string }>;
}) {
  const { locale: rawLocale } = use(params);
  const locale = isLocale(rawLocale) ? rawLocale : defaultLocale;
  const tc = getDictionary(locale).certificates;

  const [code, setCode] = useState("");
  const [result, setResult] = useState<CertificateVerification | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function runVerify(value: string) {
    const trimmed = value.trim();
    if (!trimmed) return;
    setLoading(true);
    setError(null);
    try {
      const res = await verifyCertificate(trimmed);
      setResult(res);
    } catch (err) {
      setError(err instanceof ApiRequestError ? err.message : tc.loadError);
    } finally {
      setLoading(false);
    }
  }

  // Auto-verify when arriving with ?code=... (e.g. from a student's certificate link).
  useEffect(() => {
    const fromUrl = new URLSearchParams(window.location.search).get("code");
    if (fromUrl) {
      setCode(fromUrl);
      runVerify(fromUrl);
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const tierLabel = (tier: string | null) =>
    tier === "completion" ? tc.completion : tier === "participation" ? tc.participation : "";

  return (
    <main className="container">
      <h1>{tc.verifyTitle}</h1>
      <p className="muted">{tc.verifySubtitle}</p>

      <form
        onSubmit={(e) => {
          e.preventDefault();
          runVerify(code);
        }}
        style={{ display: "flex", gap: "0.5rem", flexWrap: "wrap", marginTop: "1rem" }}
      >
        <input
          value={code}
          onChange={(e) => setCode(e.target.value)}
          placeholder={tc.verifyPlaceholder}
          style={{ flex: 1, minWidth: "16rem" }}
        />
        <button className="btn" type="submit" disabled={loading}>
          {loading ? tc.verifying : tc.verifyButton}
        </button>
      </form>

      {error && <p className="notice err" style={{ marginTop: "1rem" }}>{error}</p>}

      {result && !result.found && (
        <p className="notice err" style={{ marginTop: "1rem" }}>{tc.verifyNotFound}</p>
      )}

      {result && result.found && (
        <div className="card" style={{ marginTop: "1rem", borderWidth: "2px" }}>
          <p className={result.isValid ? "notice ok" : "notice err"}>
            {result.isValid ? tc.verifyValid : tc.verifyRevoked}
          </p>
          <h2 style={{ margin: "0.5rem 0" }}>{result.courseTitle}</h2>
          <p><span className="badge">{tierLabel(result.tier)}</span></p>
          <p>{tc.student}: <strong>{result.studentName}</strong></p>
          <p className="muted">
            {tc.serial}: {result.serialNumber}
            {result.issuedAt && ` · ${tc.issuedOn}: ${new Date(result.issuedAt).toLocaleDateString(locale)}`}
          </p>
        </div>
      )}
    </main>
  );
}
