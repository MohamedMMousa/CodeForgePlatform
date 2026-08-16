import type { SessionItem } from "@/lib/api";
import { daysUntil } from "@/lib/format";

// The core visual language of surface #5: session type + real-time state
// decides the badge and the single action, never the reverse (DESIGN_LANGUAGE.md
// §3 "state drives the CTA"). Pure data logic, no JSX/i18n — both the overview
// row and the detail page read the same treatment so they can never disagree.
//
// `recorded_lesson` has no schedule by design (ScheduledAt is null for it —
// SessionDto/Entities.cs), so it is neither "upcoming" nor "past"; it is
// "unscheduled" and its action is always "watch this lesson" when a VideoUrl
// exists. `live`/`in_person` sessions are timed against `now`, and a **past**
// live/in_person session's VideoUrl is its recording (SRS.md, DATABASE.md) —
// a real, backed piece of data distinct from the recorded_lesson case.
export type SessionTiming = "live-now" | "upcoming" | "past" | "unscheduled";

export type SessionAction =
  | { kind: "join"; url: string }
  | { kind: "watch"; url: string; variant: "recording" | "lesson" }
  | { kind: "none" };

export interface SessionTreatment {
  timing: SessionTiming;
  /** Only ever set while `timing === "upcoming"` — past day 1 the date itself
   *  is the clearer signal (same call as NextSessionPanel's proximityLabel). */
  proximity: "today" | "tomorrow" | null;
  action: SessionAction;
}

export function getSessionTreatment(
  session: SessionItem,
  now: Date = new Date()
): SessionTreatment {
  if (session.type === "recorded_lesson") {
    return {
      timing: "unscheduled",
      proximity: null,
      action: session.videoUrl
        ? { kind: "watch", url: session.videoUrl, variant: "lesson" }
        : { kind: "none" }
    };
  }

  if (!session.scheduledAt) {
    // Defensive only: the business rule requires scheduledAt for live/in_person
    // (DATABASE.md §6), but the DTO type is nullable.
    return { timing: "unscheduled", proximity: null, action: { kind: "none" } };
  }

  const start = new Date(session.scheduledAt).getTime();
  // No durationMinutes -> a zero-width window, i.e. "live now" never applies
  // without real data to support it; the session is simply upcoming until its
  // start instant, then past.
  const end = session.durationMinutes ? start + session.durationMinutes * 60_000 : start;
  const nowMs = now.getTime();

  const timing: SessionTiming =
    nowMs < start ? "upcoming" : nowMs <= end ? "live-now" : "past";

  const days = timing === "upcoming" ? daysUntil(session.scheduledAt, now) : null;
  const proximity = days === 0 ? "today" : days === 1 ? "tomorrow" : null;

  let action: SessionAction = { kind: "none" };
  if (session.type === "live" && (timing === "upcoming" || timing === "live-now")) {
    // No Join on a linkless session, even while live/upcoming (§3).
    action = session.joinLink ? { kind: "join", url: session.joinLink } : { kind: "none" };
  } else if (timing === "past") {
    // Applies to both live and in_person — the recording, when one exists.
    action = session.videoUrl
      ? { kind: "watch", url: session.videoUrl, variant: "recording" }
      : { kind: "none" };
  }

  return { timing, proximity, action };
}
