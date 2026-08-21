import { Certificate } from "@/lib/api";
import { format, type Dictionary, type Locale } from "@/lib/i18n";
import { tierDisplay } from "@/lib/certificateDisplay";
import { formatCatalogDate } from "@/lib/format";

// The one on-paper artifact in the app — rendered only while my-certificates/page.tsx
// has a `printCert` set, so exactly one certificate is ever in the print DOM at a
// time (see the page's print effect). `hidden print:block` keeps it out of the
// screen entirely; the rest of the page is `print:hidden` (see page.tsx), so this is
// the only thing that reaches paper.
//
// Deliberately literal white/black colours, not --surface/--bg tokens: a printed
// certificate is a fixed physical document, not a themed screen surface — the same
// category of documented exception as the code-block LTR rule in globals.css.
export function CertificatePrintDocument({
  cert,
  locale,
  t
}: {
  cert: Certificate;
  locale: Locale;
  t: Dictionary["certificates"];
}) {
  const tier = tierDisplay(cert.tier, t);
  const verifyUrl =
    typeof window !== "undefined"
      ? `${window.location.origin}/${locale}/verify?code=${cert.verificationCode}`
      : "";

  return (
    <div className="hidden print:flex print:min-h-screen print:items-center print:justify-center">
      {/* Tailwind's default palette (including black/white) is stripped from
          this app's theme (CLAUDE.md — only token utilities resolve), so the
          literal paper colours below use arbitrary hex values, not `black`/
          `white` class names, which would silently fail to apply. */}
      <div className="flex w-full max-w-2xl flex-col items-center gap-6 border-4 border-double border-[#000] bg-[#fff] p-12 text-center text-[#000]">
        <p className="text-h3 font-bold">
          <span>Code</span>
          <span className="text-[#F97316]">Forge</span>
        </p>

        <h1 className="text-h1 font-bold">{format(t.printHeading, { tier: tier.label })}</h1>

        <p className="text-h2 font-bold">{cert.studentName}</p>
        <p className="text-body-lg">{cert.courseTitle}</p>

        <div className="flex flex-col gap-1 text-body">
          <p>
            {t.issuedOn}: {formatCatalogDate(cert.issuedAt, locale)}
          </p>
          <p>
            {t.serial}: {cert.serialNumber}
          </p>
        </div>

        <div className="flex flex-col gap-1 text-meta">
          <p>{t.printVerifyHint}</p>
          <p>{verifyUrl}</p>
        </div>
      </div>
    </div>
  );
}
