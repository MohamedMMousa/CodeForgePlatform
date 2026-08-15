import type { CohortInfo } from "@/lib/api";
import type { Dictionary } from "@/lib/i18n";

// Mirrors CohortAvailabilityDefaults.AlmostFullSeatsThreshold (3) on the backend
// and the same threshold the catalog card applies, so one course can never read
// "almost full" on the grid and plain "open" on the page it links to.
export const ALMOST_FULL_SEATS_THRESHOLD = 3;

export type CohortDisplayState = "open" | "almost-full" | "full" | "closed";

export function getCohortDisplayState(cohort: CohortInfo): CohortDisplayState {
  // `isAcceptingEnrollment` is the API's own bookability flag — CohortMapping.ToDto
  // computes it as `status == open && now <= cutoff && enrolled < capacity`, the
  // exact predicate CohortAvailability.FindOpenCohortAsync selects on. Never
  // re-derive that here: a frontend copy that drifted would promise a seat the
  // enrollment path then rejects.
  if (cohort.isAcceptingEnrollment) {
    return cohort.seatsLeft <= ALMOST_FULL_SEATS_THRESHOLD ? "almost-full" : "open";
  }
  return cohort.seatsLeft === 0 ? "full" : "closed";
}

export interface PartitionedCohorts {
  /** Cohorts worth showing as cards — bookable or upcoming-but-not-bookable. */
  forwardLooking: CohortInfo[];
  /** Forward-looking cohorts a visitor could actually enroll in right now. */
  bookable: CohortInfo[];
  /** The cohort an enrollment request would actually land in, or null. */
  selected: CohortInfo | null;
  /** Completed batches only — the "previously run" trust signal. */
  completedCount: number;
}

/**
 * Splits the endpoint's full non-draft cohort list into what the sell page shows.
 * `now` is injected rather than read here so the caller decides the clock (and so
 * this stays a pure function).
 */
export function partitionCohorts(cohorts: CohortInfo[], now: Date): PartitionedCohorts {
  const nowMs = now.getTime();
  const forwardLooking: CohortInfo[] = [];
  let completedCount = 0;

  for (const cohort of cohorts) {
    // Cancelled batches are excluded outright: a shop window must never advertise
    // a batch that isn't happening. A cancelled batch also never ran, so it must
    // not count toward "previously run" either — that would be a false claim.
    if (cohort.status === "cancelled") continue;

    if (cohort.status === "completed") {
      completedCount += 1;
      continue;
    }

    // Stale-open: still flagged `open` by an admin who never closed it out, but its
    // end date has already passed. Not forward-looking, so no card — and NOT counted
    // as previously-run either, since only an explicit `completed` status is real
    // evidence the batch ran. Undercounting here is honest; overclaiming isn't.
    if (new Date(cohort.endDate).getTime() < nowMs) continue;

    forwardLooking.push(cohort);
  }

  // The API returns cohorts already ordered by startDate, and FindOpenCohortAsync
  // picks the earliest bookable one by that same ordering — so the first bookable
  // entry here is exactly the cohort an enrollment request lands in. Preserve the
  // server's order rather than re-sorting: an independently-applied tiebreaker
  // could disagree with the backend when two cohorts share a start date.
  const bookable = forwardLooking.filter((cohort) => cohort.isAcceptingEnrollment);

  return {
    forwardLooking,
    bookable,
    selected: bookable[0] ?? null,
    completedCount
  };
}

/**
 * Picks the right plural form for the "previously run" footnote. Arabic needs four:
 * singular, dual (مرتين), 3–10 (جمع قلة — مرات) and 11+ (تمييز مفرد — مرة). English
 * collapses the last three into "times", so the same branches serve both.
 */
export function getPreviouslyRunTemplate(count: number, t: Dictionary): string {
  if (count === 1) return t.courseDetail.previouslyRunOnce;
  if (count === 2) return t.courseDetail.previouslyRunTwice;
  if (count <= 10) return t.courseDetail.previouslyRunFew;
  return t.courseDetail.previouslyRunMany;
}
