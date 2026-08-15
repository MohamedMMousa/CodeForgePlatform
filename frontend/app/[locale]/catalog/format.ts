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
