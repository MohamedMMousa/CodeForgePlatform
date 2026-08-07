import { NextRequest, NextResponse } from "next/server";
import { defaultLocale, locales } from "./lib/i18n";

// Locale-prefix routing only. Used to also do protected-route redirects and
// server-side refresh-token/cookie-patching here (see git history — round 5 of the
// __dirname/MIDDLEWARE_INVOCATION_FAILED incident, docs/ARCHITECTURE.md §3/§6) —
// stripped to rule out middleware.ts's own logic as the source once Sentry was
// definitively excluded (rounds 1-4 removed every Sentry code path from the edge
// bundle; the crash persisted regardless). Not a security regression: the API
// enforces auth on every request regardless of what the frontend does, and each
// protected page already guards on `!session` client-side (e.g.
// app/[locale]/dashboard/page.tsx, app/[locale]/admin/layout.tsx) and renders a
// sign-in prompt instead of protected content — confirmed by reading them, not
// assumed. The real behavioral cost: without middleware refreshing an expired
// access token before the server render, getServerSession() (lib/session.ts) has
// nothing to read and returns null, and there's no automatic client-side recovery
// (lib/auth.tsx's AuthProvider deliberately has no hydration effect — see its own
// comment), so a protected page shows its signed-out fallback for the rest of that
// session rather than momentarily. Accepted for now; not being fixed here.
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
  // Skip Next internals, the API proxy, and static assets.
  matcher: ["/((?!_next|api|favicon.ico|.*\\..*).*)"]
};
