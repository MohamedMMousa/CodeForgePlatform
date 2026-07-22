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
    <main className="container">
      <div className="card">
        <h1>{t.home.welcome}</h1>
        <p className="muted">{t.home.description}</p>
        <p>
          <Link className="btn" href={`/${locale}/login`}>
            {t.home.signIn}
          </Link>
        </p>
      </div>
    </main>
  );
}
