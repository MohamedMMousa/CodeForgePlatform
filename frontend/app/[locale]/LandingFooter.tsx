import Link from "next/link";
import { format } from "@/lib/i18n";
import type { Dictionary, Locale } from "@/lib/i18n";

// Static footer chrome — not scroll-triggered content, so no Reveal here.
// Every link is a real route (no dead links): catalog, verify, contact, sign
// in — reusing the existing nav/certificates/home dictionary keys rather than
// duplicating their translations under `landing`.
export function LandingFooter({ locale, t }: { locale: Locale; t: Dictionary }) {
  const year = new Date().getFullYear();

  return (
    <footer className="border-t border-border bg-bg">
      <div className="mx-auto flex w-full max-w-6xl flex-col gap-6 ps-5 pe-5 py-10 sm:flex-row sm:items-start sm:justify-between">
        <div className="flex flex-col gap-2">
          <span className="font-bold text-text">
            {locale === "en" ? (
              <>
                Code<span className="text-accent">Forge</span>
              </>
            ) : (
              t.appName
            )}
          </span>
          <p className="max-w-sm text-meta text-text-muted">{t.landing.footerTagline}</p>
        </div>

        {/* `!` beats globals.css's legacy unlayered `a { color: accent-2 }`
            default-link rule — same fix as ShopNav/CourseCard/button.tsx. */}
        <nav aria-label={t.landing.footerNavLabel} className="flex flex-wrap gap-x-6 gap-y-2 text-label">
          <Link
            href={`/${locale}/catalog`}
            className="!text-text-muted hover:!text-accent-text focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent focus-visible:ring-offset-2 focus-visible:ring-offset-bg"
          >
            {t.nav.catalog}
          </Link>
          <Link
            href={`/${locale}/verify`}
            className="!text-text-muted hover:!text-accent-text focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent focus-visible:ring-offset-2 focus-visible:ring-offset-bg"
          >
            {t.certificates.verifyButton}
          </Link>
          <Link
            href={`/${locale}/contact`}
            className="!text-text-muted hover:!text-accent-text focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent focus-visible:ring-offset-2 focus-visible:ring-offset-bg"
          >
            {t.nav.contact}
          </Link>
          <Link
            href={`/${locale}/login`}
            className="!text-text-muted hover:!text-accent-text focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent focus-visible:ring-offset-2 focus-visible:ring-offset-bg"
          >
            {t.home.signIn}
          </Link>
        </nav>
      </div>
      <div className="border-t border-border">
        <p className="mx-auto w-full max-w-6xl ps-5 pe-5 py-4 text-meta text-text-muted">
          {format(t.landing.footerCopyright, { year })}
        </p>
      </div>
    </footer>
  );
}
