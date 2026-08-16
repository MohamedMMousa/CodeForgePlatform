import type { Locale } from "@/lib/i18n";

// §2.1: Western digits (0–9) everywhere, including Arabic UI — prices, dates,
// counts, seat numbers. `ar-EG` alone renders Arabic-Indic digits (٠–٩); the
// `-u-nu-latn` Unicode extension keeps Arabic month names/grouping while
// forcing Latin digit glyphs.
function localeTag(locale: Locale): string {
  return locale === "ar" ? "ar-EG-u-nu-latn" : "en-GB";
}

// en-GB's short-month form for September is "Sept" (4 letters, a genuine
// Intl quirk — every other month is 3), which doesn't match "13 Sep 2026".
// Hardcoding the English abbreviations sidesteps that inconsistency entirely
// rather than fighting Intl's locale data for one month.
const EN_MONTHS_SHORT = [
  "Jan", "Feb", "Mar", "Apr", "May", "Jun",
  "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"
];

export function formatCatalogDate(iso: string, locale: Locale): string {
  const date = new Date(iso);
  if (locale === "en") {
    return `${date.getDate()} ${EN_MONTHS_SHORT[date.getMonth()]} ${date.getFullYear()}`;
  }
  return date.toLocaleDateString(localeTag(locale), {
    day: "numeric",
    month: "short",
    year: "numeric"
  });
}

export function formatCatalogNumber(value: number, locale: Locale): string {
  return value.toLocaleString(localeTag(locale));
}

// Month + year only — used to name the cohort in the course-detail enroll CTA
// ("Enroll in the March 2026 cohort"). Deliberately derived from the cohort's
// startDate rather than its admin-authored `name`, which is free text, never
// localized, and can be anything ("Batch 3"). Same Western-digit discipline as
// formatCatalogDate above.
export function formatCohortMonthYear(iso: string, locale: Locale): string {
  const date = new Date(iso);
  if (locale === "en") {
    return `${EN_MONTHS_SHORT[date.getMonth()]} ${date.getFullYear()}`;
  }
  return date.toLocaleDateString(localeTag(locale), {
    month: "long",
    year: "numeric"
  });
}

// Clock time only. `hourCycle: "h23"` rather than the locale default because a
// student reading "07:30" must not have to work out am/pm for a class they are
// trying not to miss, and because ar-EG's default 12-hour form appends Arabic
// day-period words that make the string considerably longer than its English
// equivalent in a layout where the two must balance.
export function formatTime(iso: string, locale: Locale): string {
  return new Date(iso).toLocaleTimeString(localeTag(locale), {
    hour: "2-digit",
    minute: "2-digit",
    hourCycle: "h23"
  });
}

/** Weekday + date + time, for the dashboard's next-session moment. The weekday
 *  is what makes a schedule readable at a glance ("Sunday 14 Sep 2026, 19:00"). */
export function formatDateTime(iso: string, locale: Locale): string {
  const date = new Date(iso);
  const weekday = date.toLocaleDateString(localeTag(locale), { weekday: "long" });
  return `${weekday} ${formatCatalogDate(iso, locale)}, ${formatTime(iso, locale)}`;
}

/** Calendar-days between `iso` and now, by local midnight rather than by elapsed
 *  hours — a session 20 hours away is "tomorrow" to a reader if it falls after
 *  midnight, and 0/1 are the only values the UI distinguishes. Egypt is a single
 *  timezone (GMT+2), so local-midnight bucketing needs no zone handling. */
export function daysUntil(iso: string, now: Date = new Date()): number {
  const target = new Date(iso);
  const targetMidnight = new Date(target.getFullYear(), target.getMonth(), target.getDate());
  const nowMidnight = new Date(now.getFullYear(), now.getMonth(), now.getDate());
  return Math.round((targetMidnight.getTime() - nowMidnight.getTime()) / 86_400_000);
}

// Normalizes text for client-side search matching so Arabic behaves: strips
// harakat (U+064B–U+0652) and tatweel (U+0640), folds letter variants that
// readers treat as equivalent (أ/إ/آ → ا, ة → ه, ى → ي), and lowercases for
// the Latin side. Safe no-op on plain Latin text.
export function normalizeSearchText(value: string): string {
  return value
    .toLowerCase()
    .normalize("NFKC")
    .replace(/[ً-ْـ]/g, "")
    .replace(/[إأآ]/g, "ا")
    .replace(/ة/g, "ه")
    .replace(/ى/g, "ي")
    .trim();
}
