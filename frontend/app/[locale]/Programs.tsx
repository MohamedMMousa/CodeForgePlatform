import Link from "next/link";
import { Reveal } from "@/components/motion/Reveal";
import { CourseCard } from "./catalog/CourseCard";
import type { CourseListItem } from "@/lib/api";
import type { Dictionary, Locale } from "@/lib/i18n";

const FEATURED_COUNT = 3;

/**
 * The one data-driven landing section. `courses` is fetched server-side in
 * page.tsx (same try/catch pattern as catalog/page.tsx) so a failed call
 * degrades to `null` rather than throwing — this section then renders nothing
 * at all, never a broken shell. Reuses `CourseCard` as-is so the landing can
 * never show a state (badge/CTA/price) that disagrees with the catalog.
 */
export function Programs({
  courses,
  locale,
  t
}: {
  courses: CourseListItem[] | null;
  locale: Locale;
  t: Dictionary;
}) {
  if (!courses || courses.length === 0) return null;

  const featured = courses.slice(0, FEATURED_COUNT);

  return (
    <section className="bg-bg">
      <div className="mx-auto w-full max-w-6xl ps-5 pe-5 py-16 lg:py-20">
        <Reveal className="flex flex-col items-start gap-4 sm:flex-row sm:items-end sm:justify-between">
          <div className="flex flex-col gap-2">
            <h2 className="text-h2 text-text">{t.landing.programsHeading}</h2>
            <p className="text-body-lg text-text-secondary">{t.landing.programsSubhead}</p>
          </div>
          {/* `!` beats globals.css's legacy unlayered `a { color: accent-2 }`
              default-link rule — same fix as ShopNav/CourseCard/button.tsx. */}
          <Link
            href={`/${locale}/catalog`}
            className="shrink-0 font-semibold !text-accent-text hover:!text-accent-hover focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent focus-visible:ring-offset-2 focus-visible:ring-offset-bg"
          >
            {t.landing.programsViewAll}
          </Link>
        </Reveal>

        <div className="mt-8 grid grid-cols-1 gap-5 sm:grid-cols-2 lg:grid-cols-3">
          {featured.map((course, index) => (
            <Reveal key={course.id} delayMs={index * 80}>
              <CourseCard course={course} locale={locale} t={t} />
            </Reveal>
          ))}
        </div>
      </div>
    </section>
  );
}
