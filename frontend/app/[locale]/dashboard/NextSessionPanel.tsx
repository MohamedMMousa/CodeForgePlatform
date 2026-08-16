import Link from "next/link";
import { Calendar, MapPin } from "lucide-react";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import type { UpcomingSession } from "@/lib/api";
import { format, type Dictionary, type Locale } from "@/lib/i18n";
import { daysUntil, formatDateTime } from "@/lib/format";
import { externalHref } from "@/lib/url";

// DESIGN_LANGUAGE.md §4 #4 — the dashboard's focal moment. For a live-cohort
// academy the one job this page has is "don't miss your next class", so this
// panel owns the top of the page and carries the screen's single primary (§3).
//
// Three states share one heading, because "Next live session — and here is why
// there isn't one" is coherent copy, where a heading that disappeared would
// leave the reader wondering whether the section failed to load:
//   session present   full card, Join primary when the link exists
//   enrolled, none    nothing scheduled yet — a normal state, not a failure
//   not enrolled      the first-run case, pointed at the catalog
type Props = {
  session: UpcomingSession | null;
  hasCourses: boolean;
  locale: Locale;
  t: Dictionary["dashboard"];
  /** Session-type labels (live / in person / recorded) already exist here and
   *  are shared with the my-courses pages — no reason to restate them. */
  student: Dictionary["student"];
};

function typeLabel(type: string, student: Dictionary["student"]): string {
  if (type === "live") return student.live;
  if (type === "in_person") return student.inPerson;
  return student.recordedLesson;
}

/** "Today"/"Tomorrow" only — past day 1 the date itself is the clearer signal,
 *  and a running "in 6 days" count adds urgency the surface doesn't want. */
function proximityLabel(iso: string, t: Dictionary["dashboard"]): string | null {
  const days = daysUntil(iso);
  if (days === 0) return t.today;
  if (days === 1) return t.tomorrow;
  return null;
}

export function NextSessionPanel({
  session,
  hasCourses,
  locale,
  t,
  student
}: Props) {
  const proximity = session ? proximityLabel(session.scheduledAt, t) : null;

  return (
    <section className="flex flex-col gap-4">
      <h2 className="text-h2 text-text">{t.nextSession}</h2>

      <div className="flex flex-col items-start gap-4 rounded-card border border-border bg-surface p-6">
        {session ? (
          <>
            <div className="flex flex-wrap items-center gap-2">
              <Badge variant="neutral">{typeLabel(session.type, student)}</Badge>
              {/* Warning is §3's "attention" meaning — the right register for a
                  class that starts today, and it stays off the badge entirely
                  once the date alone is enough. */}
              {proximity ? <Badge variant="warning">{proximity}</Badge> : null}
            </div>

            <div className="flex flex-col gap-1">
              <h3 className="text-h3 text-text">{session.title}</h3>
              <p className="text-meta text-text-muted">
                {session.courseTitle} · {session.moduleTitle}
              </p>
            </div>

            {/* The line the whole page exists to deliver, so it carries the
                heaviest type on the surface even though it isn't a heading.
                Calendar is non-directional (§2.5) — it must not flip in RTL. */}
            <p className="flex items-start gap-3 text-h2 text-text">
              <Calendar aria-hidden="true" className="mt-1 size-5 shrink-0 text-text-muted" />
              {formatDateTime(session.scheduledAt, locale)}
            </p>

            {/* State drives the CTA (§3): no join link, no orange button. */}
            {session.joinLink ? (
              <Button asChild size="lg">
                <a
                  href={externalHref(session.joinLink)}
                  target="_blank"
                  rel="noreferrer"
                >
                  {t.join}
                </a>
              </Button>
            ) : null}

            {session.location ? (
              <p className="flex items-center gap-2 text-meta text-text-muted">
                <MapPin aria-hidden="true" className="size-4 shrink-0" />
                {format(t.locatedAt, { location: session.location })}
              </p>
            ) : null}
          </>
        ) : (
          <>
            <h3 className="text-h3 text-text">
              {hasCourses ? t.noSessionScheduled : t.notEnrolled}
            </h3>
            <p className="text-body text-text-secondary">
              {hasCourses ? t.noSessionScheduledHint : t.notEnrolledHint}
            </p>
            {hasCourses ? null : (
              <Button asChild variant="secondary">
                <Link href={`/${locale}/catalog`}>{t.browseCatalog}</Link>
              </Button>
            )}
          </>
        )}
      </div>
    </section>
  );
}
