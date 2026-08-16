import Link from "next/link";
import {
  Card,
  CardContent,
  CardDescription,
  CardFooter,
  CardHeader,
  CardTitle
} from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { defaultLocale, format, getDictionary, isLocale } from "@/lib/i18n";
import { getPublishedCourses, getPublishedTracks } from "@/lib/api";
import { CatalogBrowser } from "./CatalogBrowser";
import { RetryButton } from "./RetryButton";
import { formatCatalogNumber } from "@/lib/format";

// Pilot-scale cap: the whole grid is driven by one list call so the category
// chips and the search box (CatalogBrowser) can filter a single consistent
// set client-side instead of chasing a moving page. 100 is also the API's
// max page size (PaginationDefaults, API_CONVENTIONS.md §6). This is a
// deliberate v1 limitation, not an oversight — replace with real pagination
// once the catalog outgrows one page. The "showing first N of M" notice
// below, plus the console.warn, is the signal that day has arrived; nothing
// past the cap disappears silently.
const CATALOG_PAGE_SIZE = 100;

export default async function CatalogPage({
  params
}: {
  params: Promise<{ locale: string }>;
}) {
  const { locale: rawLocale } = await params;
  const locale = isLocale(rawLocale) ? rawLocale : defaultLocale;
  const t = getDictionary(locale);

  let courses: Awaited<ReturnType<typeof getPublishedCourses>> | null = null;
  let tracks: Awaited<ReturnType<typeof getPublishedTracks>> | null = null;

  try {
    [courses, tracks] = await Promise.all([
      getPublishedCourses({ pageSize: CATALOG_PAGE_SIZE }),
      getPublishedTracks({ pageSize: CATALOG_PAGE_SIZE })
    ]);
  } catch {
    courses = null;
    tracks = null;
  }

  if (!courses || !tracks) {
    return (
      <main className="mx-auto w-full max-w-6xl ps-5 pe-5 py-10">
        <div className="flex flex-col items-start gap-4 rounded-card border border-danger-border bg-danger-soft p-6">
          <p className="text-body text-danger">{t.catalog.loadError}</p>
          <RetryButton label={t.catalog.retry} />
        </div>
      </main>
    );
  }

  if (courses.totalCount > courses.items.length) {
    console.warn(
      `[catalog] truncated to ${courses.items.length} of ${courses.totalCount} published courses — ` +
        `CATALOG_PAGE_SIZE=${CATALOG_PAGE_SIZE} is a pilot-scale cap (app/[locale]/catalog/page.tsx); ` +
        "introduce real pagination once the catalog outgrows one page."
    );
  }

  return (
    <main className="mx-auto flex w-full max-w-6xl flex-col gap-10 ps-5 pe-5 py-10">
      <div className="flex flex-col gap-2">
        <h1 className="text-h1">{t.catalog.title}</h1>
        <p className="text-body text-text-secondary">{t.catalog.subtitle}</p>
      </div>

      {tracks.items.length > 0 && (
        <section className="flex flex-col gap-3">
          <h2 className="text-h2">{t.catalog.tracksHeading}</h2>
          <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
            {tracks.items.map((track) => (
              // Deliberately subordinate to a course card, not a peer of one
              // (§3: one primary per screen) — `size="sm"` tightens padding,
              // the title drops from h3 to a plain semibold body size, the
              // description shrinks to `meta`, and the price keeps the accent
              // colour (an allowed use, §1) but at body weight instead of h3.
              // No badge, no primary button, ever.
              <Card key={track.id} size="sm" className="relative bg-surface-2">
                <CardHeader>
                  <span className="eyebrow text-text-muted">{t.catalog.trackBadge}</span>
                  <CardTitle className="text-body-lg font-semibold">
                    {/* `!` on the text colours: globals.css's legacy, unlayered
                        `a { color: var(--accent-2) }` beats any `@layer
                        utilities` class regardless of specificity — see
                        components/ShopNav.tsx for the full explanation. */}
                    <Link
                      href={`/${locale}/catalog/tracks/${track.slug}`}
                      className="!text-text after:absolute after:inset-0 hover:!text-accent-text focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent focus-visible:ring-offset-2 focus-visible:ring-offset-bg"
                    >
                      {track.title}
                    </Link>
                  </CardTitle>
                </CardHeader>
                <CardContent className="flex flex-col gap-1.5">
                  {track.description ? (
                    <CardDescription className="text-meta line-clamp-1">
                      {track.description}
                    </CardDescription>
                  ) : null}
                  <span className="text-meta text-text-muted">
                    {format(t.catalog.coursesInTrack, {
                      count: formatCatalogNumber(track.courseCount, locale)
                    })}
                  </span>
                </CardContent>
                <CardFooter className="justify-between">
                  <span className="text-body font-semibold text-accent">
                    {formatCatalogNumber(track.price, locale)} {track.currency}
                  </span>
                  <Button asChild variant="secondary" size="sm" className="relative z-10">
                    <Link href={`/${locale}/catalog/tracks/${track.slug}`}>
                      {t.catalog.viewTrack}
                    </Link>
                  </Button>
                </CardFooter>
              </Card>
            ))}
          </div>
        </section>
      )}

      <section className="flex flex-col gap-3">
        <h2 className="text-h2">{t.catalog.coursesHeading}</h2>
        <CatalogBrowser courses={courses.items} locale={locale} t={t} />
        {courses.totalCount > courses.items.length && (
          <p className="text-meta text-text-muted">
            {format(t.catalog.showingFirstOfTotal, {
              count: formatCatalogNumber(courses.items.length, locale),
              total: formatCatalogNumber(courses.totalCount, locale)
            })}
          </p>
        )}
      </section>
    </main>
  );
}
