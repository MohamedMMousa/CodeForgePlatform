import { NextRequest, NextResponse } from "next/server";
import { defaultLocale, locales, type Locale } from "./lib/i18n";

// Screens that show an "unauthenticated" branch today when session is null — see
// each page's own `!session` guard. Kept as a flat list rather than inferred from
// routing so it's obvious at a glance which paths this gates.
const PROTECTED_PREFIXES = [
  "/dashboard",
  "/my-courses",
  "/my-certificates",
  "/change-password",
  "/instructor",
  "/admin"
];

const ACCESS_COOKIE = "cf_access";
const REFRESH_COOKIE = "cf_refresh";
const CSRF_COOKIE = "cf_csrf";
const CSRF_HEADER = "X-CSRF-Token";

function stripLocale(pathname: string, locale: Locale): string {
  const rest = pathname.slice(`/${locale}`.length);
  return rest === "" ? "/" : rest;
}

function isProtectedPath(routePath: string): boolean {
  return PROTECTED_PREFIXES.some(
    (prefix) => routePath === prefix || routePath.startsWith(`${prefix}/`)
  );
}

function redirectToLogin(request: NextRequest, locale: Locale, sessionExpired = false): NextResponse {
  const url = request.nextUrl.clone();
  url.pathname = `/${locale}/login`;
  url.search = sessionExpired ? "?sessionExpired=1" : "";
  const response = NextResponse.redirect(url);
  response.cookies.delete(ACCESS_COOKIE);
  response.cookies.delete(REFRESH_COOKIE);
  response.cookies.delete(CSRF_COOKIE);
  return response;
}

/** Set-Cookie strings look like "name=value; Path=/; ...". Extracts just the value
 * for the named cookie, or undefined if it isn't one of them. */
function extractCookieValue(setCookieHeaders: string[], name: string): string | undefined {
  for (const header of setCookieHeaders) {
    const [pair] = header.split(";");
    const separatorIndex = pair.indexOf("=");
    if (separatorIndex === -1) continue;
    if (pair.slice(0, separatorIndex).trim() === name) {
      return pair.slice(separatorIndex + 1);
    }
  }
  return undefined;
}

/** Sets `name=value` within a `Cookie:` request-header string (a different format
 * from Set-Cookie — just `a=1; b=2` pairs), replacing any existing value. */
function withCookie(cookieHeader: string, name: string, value: string): string {
  const kept = cookieHeader
    .split(";")
    .map((part) => part.trim())
    .filter((part) => part.length > 0 && !part.startsWith(`${name}=`));
  kept.push(`${name}=${value}`);
  return kept.join("; ");
}

export async function middleware(request: NextRequest) {
  const { pathname } = request.nextUrl;

  const hasLocale = locales.some(
    (locale) => pathname === `/${locale}` || pathname.startsWith(`/${locale}/`)
  );
  if (!hasLocale) {
    const url = request.nextUrl.clone();
    url.pathname = `/${defaultLocale}${pathname === "/" ? "" : pathname}`;
    return NextResponse.redirect(url);
  }

  const locale = locales.find(
    (l) => pathname === `/${l}` || pathname.startsWith(`/${l}/`)
  ) as Locale;
  const routePath = stripLocale(pathname, locale);
  const protectedPath = isProtectedPath(routePath);

  if (request.cookies.has(ACCESS_COOKIE)) {
    return NextResponse.next();
  }

  const refreshToken = request.cookies.get(REFRESH_COOKIE)?.value;
  if (!refreshToken) {
    return protectedPath ? redirectToLogin(request, locale) : NextResponse.next();
  }

  // Access cookie missing (expired, or never set this session) but a refresh cookie
  // is present: refresh server-side so the page renders signed-in on first paint
  // instead of flashing signed-out — this is what actually kills the flash across an
  // access-token expiry, not just on first load. Relies on the backend's rotation
  // grace window (RefreshTokenRotationPolicy) so concurrent navigations/prefetches
  // presenting the same refresh token all resolve safely rather than racing.
  const csrfToken = request.cookies.get(CSRF_COOKIE)?.value;
  const apiOrigin = process.env.API_INTERNAL_URL ?? "http://localhost:5205";
  const refreshResponse = await fetch(`${apiOrigin}/auth/refresh-token`, {
    method: "POST",
    headers: {
      Cookie: `${REFRESH_COOKIE}=${refreshToken}${csrfToken ? `; ${CSRF_COOKIE}=${csrfToken}` : ""}`,
      ...(csrfToken ? { [CSRF_HEADER]: csrfToken } : {})
    }
  });

  if (!refreshResponse.ok) {
    return protectedPath ? redirectToLogin(request, locale, true) : NextResponse.next();
  }

  const setCookies = refreshResponse.headers.getSetCookie();
  const newAccessToken = extractCookieValue(setCookies, ACCESS_COOKIE);

  // Patching the forwarded request's Cookie header (not just the response) is what
  // lets THIS render see the refreshed token — Set-Cookie on the response only
  // updates the browser for its *next* request, and a plain NextResponse.next()
  // would otherwise still carry the stale/expired cookie into the layout's
  // cookies() read, flashing signed-out on the very request meant to fix it.
  const requestHeaders = new Headers(request.headers);
  if (newAccessToken) {
    requestHeaders.set("cookie", withCookie(requestHeaders.get("cookie") ?? "", ACCESS_COOKIE, newAccessToken));
  }

  const response = NextResponse.next({ request: { headers: requestHeaders } });
  for (const setCookie of setCookies) {
    response.headers.append("Set-Cookie", setCookie);
  }
  return response;
}

export const config = {
  // Skip Next internals, the API proxy, and static assets.
  matcher: ["/((?!_next|api|favicon.ico|.*\\..*).*)"]
};
