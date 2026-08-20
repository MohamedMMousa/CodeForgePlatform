import { defaultLocale, getDictionary, isLocale } from "@/lib/i18n";
import { getPublishedCourses, type CourseListItem } from "@/lib/api";
import { Hero } from "./Hero";
import { HowItWorks } from "./HowItWorks";
import { WhyCodeForge } from "./WhyCodeForge";
import { Programs } from "./Programs";
import { LearningJourney } from "./LearningJourney";
import { LandingCta } from "./LandingCta";
import { LandingFooter } from "./LandingFooter";

// Landing (DESIGN_LANGUAGE.md §4 #1) — dark shop-window surface. Inherits the
// root layout's data-theme="dark", so no per-surface theme attribute is needed.
// Section order: Hero -> How it works -> Why CodeForge -> Programs ->
// Learning journey -> final CTA + footer. Programs is the only section that
// fetches; everything else is static i18n copy. Stays a thin composer.
const FEATURED_COURSES_PAGE_SIZE = 6; // more than FEATURED_COUNT so a slice still reads "featured", not "everything there is"

export default async function HomePage({
  params
}: {
  params: Promise<{ locale: string }>;
}) {
  const { locale: rawLocale } = await params;
  const locale = isLocale(rawLocale) ? rawLocale : defaultLocale;
  const t = getDictionary(locale);

  // Same try/catch-to-null pattern as catalog/page.tsx: a failed call degrades
  // this one section to hidden (Programs returns null on empty/failure)
  // rather than breaking the whole landing page.
  let courses: CourseListItem[] | null = null;
  try {
    const result = await getPublishedCourses({ pageSize: FEATURED_COURSES_PAGE_SIZE });
    courses = result.items;
  } catch {
    courses = null;
  }

  return (
    <main className="bg-bg">
      <Hero locale={locale} t={t} />
      <HowItWorks t={t} />
      <WhyCodeForge t={t} />
      <Programs courses={courses} locale={locale} t={t} />
      <LearningJourney t={t} />
      <LandingCta locale={locale} t={t} />
      <LandingFooter locale={locale} t={t} />
    </main>
  );
}
