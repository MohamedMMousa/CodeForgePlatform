import Link from "next/link";
import { ChevronRight } from "lucide-react";
import { Badge } from "@/components/ui/badge";
import type { SessionItem } from "@/lib/api";
import { format, getMaterialCountTemplate, type Dictionary, type Locale } from "@/lib/i18n";
import { formatCatalogNumber, formatDateTime } from "@/lib/format";
import { getSessionTreatment } from "./sessionState";

function typeLabel(type: string, student: Dictionary["student"]): string {
  if (type === "live") return student.live;
  if (type === "in_person") return student.inPerson;
  return student.recordedLesson;
}

type Props = {
  session: SessionItem;
  courseId: string;
  locale: Locale;
  t: Dictionary["courseContent"];
  student: Dictionary["student"];
};

// The whole row is the link — §3's one-primary rule means the action lives on
// the detail page, not here. This row only signals what matters at a glance:
// type, live-now/today/tomorrow, and whether there's anything to open.
export function SessionRow({ session, courseId, locale, t, student }: Props) {
  const treatment = getSessionTreatment(session);

  const metaLine =
    session.type === "in_person" && session.location
      ? session.location
      : session.scheduledAt
        ? formatDateTime(session.scheduledAt, locale)
        : null;

  return (
    <Link
      href={`/${locale}/my-courses/${courseId}/sessions/${session.id}`}
      className="flex items-center justify-between gap-4 rounded-card border border-border bg-surface p-4 !text-text transition-colors hover:bg-surface-2 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent focus-visible:ring-offset-2 focus-visible:ring-offset-bg"
    >
      <div className="flex min-w-0 flex-col gap-1">
        <div className="flex flex-wrap items-center gap-2">
          <Badge variant="neutral">{typeLabel(session.type, student)}</Badge>
          {treatment.timing === "live-now" ? (
            <Badge variant="success">{t.liveNow}</Badge>
          ) : null}
          {treatment.proximity === "today" ? <Badge variant="warning">{t.today}</Badge> : null}
          {treatment.proximity === "tomorrow" ? (
            <Badge variant="warning">{t.tomorrow}</Badge>
          ) : null}
        </div>
        <span className="truncate text-body font-semibold text-text">{session.title}</span>
        {metaLine ? <span className="text-meta text-text-muted">{metaLine}</span> : null}
        {session.materialCount > 0 ? (
          <span className="text-meta text-text-muted">
            {format(getMaterialCountTemplate(session.materialCount, t), {
              count: formatCatalogNumber(session.materialCount, locale)
            })}
          </span>
        ) : null}
      </div>
      {/* Directional (§2.5) — points toward reading direction, so it flips. */}
      <ChevronRight aria-hidden="true" className="size-5 shrink-0 text-text-muted rtl:rotate-180" />
    </Link>
  );
}
