import Link from "next/link";
import { Calendar } from "lucide-react";
import {
  Card,
  CardAction,
  CardContent,
  CardDescription,
  CardFooter,
  CardHeader,
  CardTitle
} from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import type { CourseListItem } from "@/lib/api";
import {
  format,
  getCategoryLabel,
  getSeatsLeftTemplate,
  type Dictionary,
  type Locale
} from "@/lib/i18n";
import { formatCatalogDate, formatCatalogNumber } from "@/lib/format";

// DESIGN_LANGUAGE.md §3 course-card anatomy, bound only to fields the catalog
// list endpoint actually returns — no thumbnail (text-only per §3), no
// level·track split (the eyebrow is `category` alone, that's all there is).
// State drives the CTA: nextCohort === null never gets the primary button.
export function CourseCard({
  course,
  locale,
  t
}: {
  course: CourseListItem;
  locale: Locale;
  t: Dictionary;
}) {
  const cohort = course.nextCohort;
  const isBookable = cohort !== null;
  const detailHref = `/${locale}/catalog/courses/${course.slug}`;
  const ctaHref = isBookable ? detailHref : `${detailHref}#notify`;

  return (
    // `relative` anchors the title's stretched-link overlay; `overflow-hidden`
    // (inherited from Card) is why the overlay is `inset-0` rather than
    // reaching past the card's own box.
    <Card className="relative">
      <CardHeader>
        {course.category ? (
          <span className="eyebrow text-text-muted">
            {getCategoryLabel(course.category, locale)}
          </span>
        ) : null}
        <CardAction>
          {cohort === null ? (
            <Badge variant="neutral">{t.catalog.statusAwaiting}</Badge>
          ) : cohort.status === "almost_full" ? (
            <Badge variant="warning">
              {format(getSeatsLeftTemplate(cohort.seatsLeft, t), {
                count: formatCatalogNumber(cohort.seatsLeft, locale)
              })}
            </Badge>
          ) : (
            <Badge variant="success">{t.catalog.statusOpen}</Badge>
          )}
        </CardAction>
        <CardTitle>
          {/* Stretched link: the whole card is clickable, but this is the only
              <a> in the card — the footer CTA below is a second, separate
              anchor raised above it with z-10, never nested inside it. */}
          {/* `!` on the text colours: globals.css's legacy, unlayered
              `a { color: var(--accent-2) }` default-link rule beats any
              `@layer utilities` class regardless of specificity — see
              components/ShopNav.tsx for the full explanation. Without it this
              title rendered accent-2 orange instead of --text. */}
          <Link
            href={detailHref}
            className="!text-text after:absolute after:inset-0 hover:!text-accent-text focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent focus-visible:ring-offset-2 focus-visible:ring-offset-bg"
          >
            {course.title}
          </Link>
        </CardTitle>
      </CardHeader>
      <CardContent className="flex flex-col gap-3">
        {course.description ? (
          <CardDescription className="line-clamp-1">{course.description}</CardDescription>
        ) : null}
        <div className="flex items-center gap-2 text-meta text-text-muted">
          {/* Calendar is non-directional (§2.5) — it does not flip in RTL. */}
          <Calendar aria-hidden="true" className="size-4 shrink-0" />
          {cohort ? (
            <span>
              {format(t.courseDetail.startsOn, {
                date: formatCatalogDate(cohort.startDate, locale)
              })}
            </span>
          ) : (
            <span>{t.catalog.datesAnnouncedSoon}</span>
          )}
        </div>
      </CardContent>
      <CardFooter className="justify-between">
        <span className="text-h3 text-accent">
          {formatCatalogNumber(course.price, locale)} {course.currency}
        </span>
        <Button
          asChild
          variant={isBookable ? "primary" : "secondary"}
          size="sm"
          className="relative z-10"
        >
          <Link href={ctaHref}>{isBookable ? t.catalog.enroll : t.catalog.notifyMeShort}</Link>
        </Button>
      </CardFooter>
    </Card>
  );
}
