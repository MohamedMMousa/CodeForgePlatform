import type { Metadata } from "next";
import { Suspense } from "react";
import { Cairo, JetBrains_Mono } from "next/font/google";
import "../globals.css";
import "@/lib/sentry-node";
import { AuthProvider } from "@/lib/auth";
import { toSession } from "@/lib/session-mapping";
import { dir, getDictionary, isLocale, locales, defaultLocale } from "@/lib/i18n";
import { getServerSession } from "@/lib/session";
import Image from "next/image";
import Link from "next/link";
import { RoleNav } from "@/components/RoleNav";
import { PasswordChangeGate } from "@/components/PasswordChangeGate";
import { LocaleSwitcher } from "@/components/LocaleSwitcher";
import { DirectionProvider } from "@/components/DirectionProvider";

// Cairo is the UI typeface for both scripts, so bilingual pages share visual
// DNA rather than looking like a translation of an English design
// (DESIGN_LANGUAGE.md §2.1). Both subsets load in both locales: an Arabic page
// still renders the Latin "CodeForge" wordmark, and an English page still
// renders Arabic names.
const cairo = Cairo({
  subsets: ["latin", "arabic"],
  weight: ["400", "600", "700"],
  variable: "--font-cairo",
  display: "swap"
});

// Code only — snippets, submissions, test output. Latin subset is enough; a
// code block is always LTR (§2.1), and globals.css enforces that.
const jetbrainsMono = JetBrains_Mono({
  subsets: ["latin"],
  weight: ["400", "500", "700"],
  variable: "--font-jetbrains-mono",
  display: "swap"
});

export const metadata: Metadata = {
  title: "CodeForge Academy",
  description: "Live, cohort-based programming tracks. Build. Create. Launch."
};

export function generateStaticParams() {
  return locales.map((locale) => ({ locale }));
}

export default async function LocaleLayout({
  children,
  params
}: {
  children: React.ReactNode;
  params: Promise<{ locale: string }>;
}) {
  const { locale: rawLocale } = await params;
  const locale = isLocale(rawLocale) ? rawLocale : defaultLocale;
  const t = getDictionary(locale);
  const { session: serverSession, canRecover } = await getServerSession();
  const initialSession = serverSession ? toSession(serverSession) : null;

  return (
    // `dir` is resolved per locale here and nowhere else — rtl for ar, ltr for
    // en (§2.5). `data-theme` seeds the document dark, the shop-window default;
    // a light surface re-scopes every token by setting data-theme="light" on
    // its own subtree rather than toggling anything global.
    <html
      lang={locale}
      dir={dir(locale)}
      data-theme="dark"
      className={`${cairo.variable} ${jetbrainsMono.variable}`}
    >
      <body>
        <DirectionProvider dir={dir(locale)}>
          <AuthProvider initialSession={initialSession} canRecover={canRecover}>
            <PasswordChangeGate locale={locale} />
            <header className="topbar">
              <Link className="brand" href={`/${locale}`}>
                {/* Two theme-variant assets, toggled by the `dark`/`light`
                    custom variants in globals.css rather than JS — the same
                    per-surface mechanism the rest of the design system uses.
                    270x237 native size; 36x32 preserves that ratio at
                    roughly the old icon's scale. */}
                <Image
                  src="/logo-dark.png"
                  alt=""
                  width={36}
                  height={32}
                  className="hidden dark:block"
                />
                <Image
                  src="/logo-light.png"
                  alt=""
                  width={36}
                  height={32}
                  className="hidden light:block"
                />
                <span>
                  {locale === "en" ? (
                    <>
                      Code<span className="accent">Forge</span>
                    </>
                  ) : (
                    t.appName
                  )}
                  <div className="tagline">{t.tagline}</div>
                </span>
              </Link>
              <nav className="nav-links">
                <Link href={`/${locale}/catalog`}>{t.nav.catalog}</Link>
                <Link href={`/${locale}/contact`}>{t.nav.contact}</Link>
                <RoleNav locale={locale} t={t} />
                <Suspense fallback={null}>
                  <LocaleSwitcher locale={locale} label={t.nav.switchTo} />
                </Suspense>
              </nav>
            </header>
            {children}
          </AuthProvider>
        </DirectionProvider>
      </body>
    </html>
  );
}
