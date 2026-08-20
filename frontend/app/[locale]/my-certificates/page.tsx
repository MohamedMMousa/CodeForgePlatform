"use client";

import { use, useEffect, useState } from "react";
import Link from "next/link";
import { Award } from "lucide-react";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import {
  Card,
  CardAction,
  CardContent,
  CardFooter,
  CardHeader,
  CardTitle
} from "@/components/ui/card";
import { useAuth } from "@/lib/auth";
import { useSessionGate } from "@/components/SessionGuard";
import { ApiRequestError, Certificate, getMyCertificates } from "@/lib/api";
import { defaultLocale, format, getDictionary, isLocale } from "@/lib/i18n";
import { formatCatalogDate } from "@/lib/format";
import { tierDisplay } from "@/lib/certificateDisplay";
import { CertificateSkeleton } from "./CertificateSkeleton";
import { CertificatePrintDocument } from "./CertificatePrintDocument";

// DESIGN_LANGUAGE.md §4 #8 — the student's earned-certificates view, on the light
// lane (§2.3) like every reading/work surface since surface #4. Rebuild-in-place:
// useAuth()/useSessionGate({ locale }) and the getMyCertificates() contract are
// unchanged from before this restyle. Deliberately shows earned certificates only —
// no "you're eligible" teaser, since eligibility is intentionally not student-visible
// (ARCHITECTURE.md §7).
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
  const [reloadKey, setReloadKey] = useState(0);
  const [printCert, setPrintCert] = useState<Certificate | null>(null);

  useEffect(() => {
    if (!session) return;
    setError(null);
    getMyCertificates({ page, pageSize: PAGE_SIZE })
      .then((result) => {
        setCertificates(result.items);
        setTotalCount(result.totalCount);
      })
      .catch((err) => setError(err instanceof ApiRequestError ? err.message : tc.loadError));
  }, [session, page, reloadKey, tc.loadError]);

  // Printing a single certificate as a clean document rather than the whole page:
  // mount the print-only artifact for exactly one certificate, wait a frame so it's
  // painted, trigger the browser print dialog, then clear it on `afterprint` so a
  // second Print click can never bleed the previous certificate's data.
  useEffect(() => {
    if (!printCert) return;
    const clear = () => setPrintCert(null);
    window.addEventListener("afterprint", clear);
    const raf = requestAnimationFrame(() => window.print());
    return () => {
      window.removeEventListener("afterprint", clear);
      cancelAnimationFrame(raf);
    };
  }, [printCert]);

  const gate = useSessionGate({ locale });
  if (!gate.ok) return gate.fallback;

  const totalPages = Math.max(1, Math.ceil(totalCount / PAGE_SIZE));
  const showPagination = totalPages > 1;

  return (
    <main
      data-theme="light"
      className="min-h-screen bg-bg [&_:is(h1,h2,h3,p)]:m-0"
    >
      <div className="print:hidden">
        <div className="mx-auto flex w-full max-w-4xl flex-col gap-10 ps-5 pe-5 py-10">
          <h1 className="text-h1 text-text">{tc.myCertificates}</h1>

          {error ? (
            <div className="flex flex-col items-start gap-4 rounded-card border border-danger-border bg-danger-soft p-6">
              <p className="text-body text-danger">{error}</p>
              <Button variant="secondary" onClick={() => setReloadKey((key) => key + 1)}>
                {tc.retry}
              </Button>
            </div>
          ) : certificates === null ? (
            <CertificateSkeleton />
          ) : certificates.length === 0 ? (
            <div className="flex flex-col items-center gap-3 rounded-card border border-border bg-surface p-10 text-center">
              <Award className="size-8 text-text-muted" aria-hidden="true" />
              <p className="text-body text-text-muted">{tc.noCertificates}</p>
            </div>
          ) : (
            <>
              <div className="flex flex-col gap-4">
                {certificates.map((cert) => {
                  const tier = tierDisplay(cert.tier, tc);
                  return (
                    <Card
                      key={cert.id}
                      className={cert.isRevoked ? "opacity-60" : undefined}
                    >
                      <CardHeader>
                        <CardTitle>{cert.courseTitle}</CardTitle>
                        <CardAction className="flex gap-2">
                          <Badge variant={tier.variant}>{tier.label}</Badge>
                          {cert.isRevoked ? (
                            <Badge variant="danger">{tc.revoked}</Badge>
                          ) : null}
                        </CardAction>
                      </CardHeader>
                      <CardContent className="flex flex-col gap-1">
                        <p className="text-body text-text-secondary">
                          {tc.student}: <span className="font-semibold">{cert.studentName}</span>
                        </p>
                        <p className="text-meta text-text-muted">
                          {tc.serial}: {cert.serialNumber} · {tc.issuedOn}:{" "}
                          {formatCatalogDate(cert.issuedAt, locale)}
                        </p>
                        <p className="text-meta text-text-muted">
                          {tc.attendanceRate}: {cert.attendanceRate}% · {tc.assessmentsPassed}:{" "}
                          {cert.assessmentsPassed ? tc.yes : tc.no}
                        </p>
                        <p className="text-meta text-text-muted">
                          {tc.verificationCode}: <code>{cert.verificationCode}</code>
                        </p>
                      </CardContent>
                      <CardFooter>
                        <Button asChild variant="secondary" size="sm">
                          <Link href={`/${locale}/verify?code=${cert.verificationCode}`}>
                            {tc.view}
                          </Link>
                        </Button>
                        <Button variant="ghost" size="sm" onClick={() => setPrintCert(cert)}>
                          {tc.print}
                        </Button>
                      </CardFooter>
                    </Card>
                  );
                })}
              </div>

              {showPagination ? (
                <div className="flex flex-wrap items-center justify-between gap-4">
                  <span className="text-meta text-text-muted">
                    {format(dictionary.pagination.pageOf, { page, totalPages })}
                  </span>
                  <div className="flex gap-2">
                    <Button
                      variant="secondary"
                      size="sm"
                      disabled={page <= 1}
                      onClick={() => setPage((p) => p - 1)}
                    >
                      {dictionary.pagination.previous}
                    </Button>
                    <Button
                      variant="secondary"
                      size="sm"
                      disabled={page >= totalPages}
                      onClick={() => setPage((p) => p + 1)}
                    >
                      {dictionary.pagination.next}
                    </Button>
                  </div>
                </div>
              ) : null}
            </>
          )}
        </div>
      </div>

      {printCert ? (
        <CertificatePrintDocument cert={printCert} locale={locale} t={tc} />
      ) : null}
    </main>
  );
}
