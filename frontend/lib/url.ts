const SCHEME_PATTERN = /^[a-z][a-z0-9+.-]*:/i;

/**
 * A join link or video URL saved without a scheme (e.g. "zoom.com") resolves as a path
 * relative to the current page rather than an external site. New rows are normalized on
 * save (see UrlRules.NormalizeOrNull on the backend), but this covers rows saved before
 * that existed.
 */
export function externalHref(url: string): string {
  return SCHEME_PATTERN.test(url) ? url : `https://${url}`;
}
