"use client";

import { use, useEffect, useState } from "react";
import { BadgeCheck, HelpCircle, XCircle } from "lucide-react";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { ApiRequestError, CertificateVerification, verifyCertificate } from "@/lib/api";
import { defaultLocale, getDictionary, isLocale } from "@/lib/i18n";
import { formatCatalogDate } from "@/lib/format";
import { tierDisplay, verificationDisplay } from "@/lib/certificateDisplay";

// DESIGN_LANGUAGE.md §4 #8 — the PUBLIC certificate verify page. Genuinely
// unauthenticated (no useAuth/useSessionGate anywhere in this file — a stranger
// following a printed/shared code must be able to reach and use this with no
// sign-in). Trust utility, not a work surface: narrow centered column, one primary
// action (Verify), and a calm, document-like result rather than a system alert.
//
// TODO(i18n-review): this is the one page a stranger judges CodeForge's
// credibility on — the Arabic valid/revoked/not-found copy below warrants a
// dedicated native-speaker pass, mirroring the gradebook's i18n-review flag.
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

  const display = result ? verificationDisplay(result) : null;
  const tier = result?.tier ? tierDisplay(result.tier, tc) : null;

  return (
    <main data-theme="light" className="min-h-screen bg-bg [&_:is(h1,h2,h3,p)]:m-0">
      <div className="mx-auto flex w-full max-w-md flex-col gap-6 ps-5 pe-5 py-10">
        <div className="flex flex-col gap-2">
          <h1 className="text-h1 text-text">{tc.verifyTitle}</h1>
          <p className="text-body text-text-muted">{tc.verifySubtitle}</p>
        </div>

        <form
          onSubmit={(e) => {
            e.preventDefault();
            runVerify(code);
          }}
          className="flex flex-col gap-3 sm:flex-row"
        >
          <Input
            value={code}
            onChange={(e) => setCode(e.target.value)}
            placeholder={tc.verifyPlaceholder}
          />
          <Button type="submit" disabled={loading}>
            {loading ? tc.verifying : tc.verifyButton}
          </Button>
        </form>

        {error ? (
          <div className="rounded-card border border-danger-border bg-danger-soft p-4">
            <p className="text-body text-danger">{error}</p>
          </div>
        ) : null}

        {display?.kind === "not-found" ? (
          <div className="flex flex-col items-center gap-3 rounded-card border border-border bg-surface-2 p-8 text-center">
            <HelpCircle className="size-8 text-text-muted" aria-hidden="true" />
            <p className="text-body text-text-muted">{tc.verifyNotFound}</p>
          </div>
        ) : null}

        {display && result && (display.kind === "valid" || display.kind === "revoked") ? (
          <Card>
            <CardHeader
              className={
                display.kind === "valid"
                  ? "border-b border-success-border bg-success-soft"
                  : "border-b border-danger-border bg-danger-soft"
              }
            >
              <div className="flex items-center gap-2">
                {display.kind === "valid" ? (
                  <BadgeCheck className="size-5 text-success" aria-hidden="true" />
                ) : (
                  <XCircle className="size-5 text-danger" aria-hidden="true" />
                )}
                <Badge variant={display.variant}>
                  {display.kind === "valid" ? tc.valid : tc.revoked}
                </Badge>
              </div>
            </CardHeader>
            <CardContent className="flex flex-col gap-3 py-2">
              <p className="text-body text-text-secondary">
                {display.kind === "valid" ? tc.verifyValid : tc.verifyRevoked}
              </p>
              <p className="text-h2 font-bold text-text">{result.studentName}</p>
              <p className="text-body text-text-secondary">{result.courseTitle}</p>
              {tier ? <Badge variant={tier.variant}>{tier.label}</Badge> : null}
              <p className="text-meta text-text-muted">
                {tc.serial}: {result.serialNumber}
                {result.issuedAt
                  ? ` · ${tc.issuedOn}: ${formatCatalogDate(result.issuedAt, locale)}`
                  : null}
              </p>
            </CardContent>
          </Card>
        ) : null}
      </div>
    </main>
  );
}
