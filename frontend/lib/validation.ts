// Client-side mirrors of backend validation rules. These exist to catch problems before
// a round-trip and to make the rules discoverable in the UI — the server remains the
// authority and re-checks everything.

/**
 * Mirrors `CourseValidationRules.IsValidSlug` in
 * `src/CodeForge.Application/Courses/Common/CourseValidationRules.cs`, which is the origin
 * of truth. Keep the two in step.
 */
export const SLUG_PATTERN = /^[a-z0-9]+(?:-[a-z0-9]+)*$/;

export function isValidSlug(value: string): boolean {
  return SLUG_PATTERN.test(value);
}

/**
 * Derives a slug from a title: strips diacritics (é → e), lowercases, and collapses every
 * run of non-`[a-z0-9]` into a single hyphen.
 *
 * Returns an empty string for titles with no Latin letters or digits at all — an Arabic
 * title has no representation under the backend's Latin-only slug rule. Callers must treat
 * "" as "no suggestion available" and leave the field alone rather than clearing it.
 */
export function slugify(title: string): string {
  return title
    .normalize("NFD")
    .replace(/\p{M}+/gu, "") // combining marks left behind by NFD
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, "-")
    .replace(/^-+|-+$/g, "");
}
