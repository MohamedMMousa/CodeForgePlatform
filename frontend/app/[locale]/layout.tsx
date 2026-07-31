import type { Metadata } from "next";
import "../globals.css";
import { AuthProvider } from "@/lib/auth";
import { dir, getDictionary, isLocale, locales, defaultLocale } from "@/lib/i18n";
import Image from "next/image";
import Link from "next/link";
import { RoleNav } from "@/components/RoleNav";
import { PasswordChangeGate } from "@/components/PasswordChangeGate";

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
  const other = locale === "en" ? "ar" : "en";

  return (
    <html lang={locale} dir={dir(locale)}>
      <body>
        <AuthProvider>
          <PasswordChangeGate locale={locale} />
          <header className="topbar">
            <Link className="brand" href={`/${locale}`}>
              <Image src="/logo.svg" alt="" width={32} height={32} />
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
              <Link href={`/${other}`}>{t.nav.switchTo}</Link>
            </nav>
          </header>
          {children}
        </AuthProvider>
      </body>
    </html>
  );
}
