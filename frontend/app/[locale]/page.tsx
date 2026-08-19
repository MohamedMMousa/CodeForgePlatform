import { defaultLocale, getDictionary, isLocale } from "@/lib/i18n";
import { Hero } from "./Hero";

// Landing (DESIGN_LANGUAGE.md §4 #1) — dark shop-window surface. Inherits the
// root layout's data-theme="dark", so no per-surface theme attribute is needed.
// Part 1 is the hero; later parts add the content sections below it, so this
// stays a thin section-composer.
export default async function HomePage({
  params
}: {
  params: Promise<{ locale: string }>;
}) {
  const { locale: rawLocale } = await params;
  const locale = isLocale(rawLocale) ? rawLocale : defaultLocale;
  const t = getDictionary(locale);

  return (
    <main className="bg-bg">
      <Hero locale={locale} t={t} />
    </main>
  );
}
