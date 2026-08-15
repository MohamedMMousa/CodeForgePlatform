import { Calendar, CalendarCheck, Clock, Users } from "lucide-react";
import {
  Card,
  CardAction,
  CardContent,
  CardHeader,
  CardTitle
} from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import type { CohortInfo } from "@/lib/api";
import { format, getSeatsLeftTemplate, type Dictionary, type Locale } from "@/lib/i18n";
import { cn } from "@/lib/utils";
import { formatCatalogDate, formatCatalogNumber } from "../../format";
import { getCohortDisplayState } from "./cohorts";

// One batch, presented as information. No enroll button lives here on purpose:
// the API takes no cohort id (SubmitEnrollmentRequestCommandHandler resolves the
// batch itself via FindOpenCohortAsync), so a per-card CTA would be one of N
// identical links that all enroll you in the earliest bookable batch regardless
// of which was clicked. The page's single primary names that batch instead.
export function CohortCard({
  cohort,
  isSelected,
  locale,
  t
}: {
  cohort: CohortInfo;
  isSelected: boolean;
  locale: Locale;
  t: Dictionary;
}) {
  const state = getCohortDisplayState(cohort);

  return (
    <Card
      // Accent border is the sanctioned "active/selected" use of orange (§1's
      // four uses), marking the one batch an enrollment actually joins.
      className={cn(isSelected && "border-accent")}
    >
      <CardHeader>
        <CardAction>
          {state === "almost-full" ? (
            <Badge variant="warning">
              {format(getSeatsLeftTemplate(cohort.seatsLeft, t), {
                count: formatCatalogNumber(cohort.seatsLeft, locale)
              })}
            </Badge>
          ) : state === "open" ? (
            <Badge variant="success">{t.catalog.statusOpen}</Badge>
          ) : state === "full" ? (
            <Badge variant="neutral">{t.courseDetail.full}</Badge>
          ) : (
            <Badge variant="neutral">{t.courseDetail.enrollmentClosed}</Badge>
          )}
        </CardAction>
        <CardTitle>{cohort.name}</CardTitle>
        {isSelected ? (
          <span className="text-meta text-accent-text">{t.courseDetail.youWillJoin}</span>
        ) : null}
      </CardHeader>
      <CardContent className="flex flex-col gap-2 text-meta text-text-muted">
        {/* Calendar, CalendarCheck, Clock and Users are all non-directional
            (§2.5) — they must not flip in RTL. */}
        <MetaRow icon={<Calendar aria-hidden="true" className="size-4 shrink-0" />}>
          {format(t.courseDetail.startsOn, {
            date: formatCatalogDate(cohort.startDate, locale)
          })}
        </MetaRow>
        <MetaRow icon={<CalendarCheck aria-hidden="true" className="size-4 shrink-0" />}>
          {format(t.courseDetail.endsOn, {
            date: formatCatalogDate(cohort.endDate, locale)
          })}
        </MetaRow>
        {/* A closing date only means something while the batch is still open. */}
        {cohort.isAcceptingEnrollment ? (
          <MetaRow icon={<Clock aria-hidden="true" className="size-4 shrink-0" />}>
            {format(t.courseDetail.enrollmentClosesOn, {
              date: formatCatalogDate(cohort.enrollmentCutoffDate, locale)
            })}
          </MetaRow>
        ) : null}
        {/* Seats only in the `open` state — the almost-full badge already carries
            the exact count, and repeating it would say the same thing twice. */}
        {state === "open" ? (
          <MetaRow icon={<Users aria-hidden="true" className="size-4 shrink-0" />}>
            {format(getSeatsLeftTemplate(cohort.seatsLeft, t), {
              count: formatCatalogNumber(cohort.seatsLeft, locale)
            })}
          </MetaRow>
        ) : null}
      </CardContent>
    </Card>
  );
}

function MetaRow({
  icon,
  children
}: {
  icon: React.ReactNode;
  children: React.ReactNode;
}) {
  return (
    <span className="flex items-center gap-2">
      {icon}
      <span>{children}</span>
    </span>
  );
}
