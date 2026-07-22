import { NextRequest, NextResponse } from "next/server";
import { defaultLocale, locales } from "./lib/i18n";

// Ensures every route is locale-prefixed (/en/…, /ar/…). Requests without a known
// locale prefix are redirected to the default locale.
export function middleware(request: NextRequest) {
  const { pathname } = request.nextUrl;

  const hasLocale = locales.some(
    (locale) => pathname === `/${locale}` || pathname.startsWith(`/${locale}/`)
  );
  if (hasLocale) {
    return NextResponse.next();
  }

  const url = request.nextUrl.clone();
  url.pathname = `/${defaultLocale}${pathname === "/" ? "" : pathname}`;
  return NextResponse.redirect(url);
}

export const config = {
  // Skip Next internals and static assets.
  matcher: ["/((?!_next|api|favicon.ico|.*\\..*).*)"]
};
