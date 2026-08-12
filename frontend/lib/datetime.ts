// Bridges `<input type="datetime-local">` and the API's UTC timestamps.
//
// A datetime-local input's value is a *naive* wall-clock string — "2026-08-20T18:00", with
// no zone and no offset. Sent as-is it deserializes server-side into a DateTime with
// Kind=Unspecified, and Npgsql refuses to write a non-UTC DateTime to a `timestamptz`
// column ("only UTC is supported"), which surfaces as an opaque 500. Every datetime-local
// value must therefore be resolved to an absolute instant before it leaves the browser.
//
// The picked time is interpreted in the *browser's* timezone, which is what the read path
// already assumes — timestamps are rendered with `new Date(iso).toLocaleString(locale)`.

/**
 * Resolves a `datetime-local` value to a UTC ISO instant for the API.
 *
 * Empty, missing, and unparseable values all collapse to `null` — the wire format for
 * "no timestamp". Returning `null` rather than the string matters: `""` would serialize
 * as `{"dueAt":""}`, which fails JSON binding server-side and 400s.
 */
export function localInputToUtcIso(value: string | null | undefined): string | null {
  if (!value) return null;

  const parsed = new Date(value);
  if (Number.isNaN(parsed.getTime())) return null;

  return parsed.toISOString();
}

/**
 * Inverse of {@link localInputToUtcIso}: formats a UTC ISO instant for a `datetime-local`
 * input, in local time.
 *
 * Note this is deliberately not `iso.slice(0, 16)` — that yields the UTC wall-clock, which
 * the input then re-interprets as local time, shifting the value by the UTC offset on every
 * edit round-trip.
 */
export function utcIsoToLocalInput(iso: string | null | undefined): string {
  if (!iso) return "";

  const parsed = new Date(iso);
  if (Number.isNaN(parsed.getTime())) return "";

  const pad = (part: number) => String(part).padStart(2, "0");

  return (
    `${parsed.getFullYear()}-${pad(parsed.getMonth() + 1)}-${pad(parsed.getDate())}` +
    `T${pad(parsed.getHours())}:${pad(parsed.getMinutes())}`
  );
}
