import Link from "next/link";
import { defaultLocale, getDictionary, isLocale } from "@/lib/i18n";

export default async function HomePage({
  params
}: {
  params: Promise<{ locale: string }>;
}) {
  const { locale: rawLocale } = await params;
  const locale = isLocale(rawLocale) ? rawLocale : defaultLocale;
  const t = getDictionary(locale);

  return (
    <main className="cf-container">
      <div className="card">
        <h1>{t.home.welcome}</h1>
        <p className="muted">{t.home.description}</p>
        <p className="muted">{t.home.newHereHint}</p>
        <p style={{ display: "flex", gap: "0.75rem", flexWrap: "wrap" }}>
          <Link className="btn" href={`/${locale}/catalog`}>
            {t.home.browseCourses}
          </Link>
          <Link className="btn secondary" href={`/${locale}/login`}>
            {t.home.signIn}
          </Link>
        </p>
      </div>
    </main>
  );
}
