import { defaultLocale, locales } from "./lib/i18n";

// Locale-prefix routing only, and deliberately written against plain Web APIs with
// **no `next/server` import**. Both of those are scars from the same production
// incident; they're separate decisions, so they're explained separately.
//
// 1. Why nothing is imported from "next/server" — this is load-bearing, do not
//    "tidy" it back to NextRequest/NextResponse.
//
//    Every request to the deployed site returned 500 MIDDLEWARE_INVOCATION_FAILED
//    with `ReferenceError: __dirname is not defined`, thrown at module load before
//    this file's own code ran. The cause is inside Next itself, not this app:
//    for the edge compilation Next resolves `next/server` to
//    next/dist/server/web/exports/index.js, which re-exports `userAgent` from
//    ../spec-extension/user-agent, which requires
//    next/dist/compiled/ua-parser-js — and that file's one and only use of a Node
//    global is `__nccwpck_require__.ab = __dirname + "/"`. The Edge Runtime has no
//    `__dirname`, so it dies on load. That barrel is the ONLY path by which
//    ua-parser-js reaches a middleware bundle (checked: nothing else under
//    dist/server/web requires spec-extension/user-agent, and adapter.js — which is
//    in every middleware bundle — never touches it). Importing nothing from
//    `next/server` removes it from the graph outright.
//
//    This never reproduced locally across five rounds of debugging because webpack
//    folds `__dirname` into a literal here — the local bundle contains the whole
//    UAParser blob but reads `g.ab = "//"`. Vercel's build does not do that
//    substitution. scripts/check-middleware-edge-safety.mjs now greps for the
//    module itself rather than for `__dirname`, which is why it can finally catch
//    this; see its header comment.
//
//    Returning plain Web objects is supported by Next, not a trick — from its own
//    source: dist/server/web/adapter.js accepts anything `instanceof Response`, and
//    treats a falsy return as "continue" by substituting NextResponse.next() from
//    its own module graph. NextResponse.redirect is itself literally
//    `new Response(null, { status: 307, headers: { Location } })`, so the redirect
//    below is behaviourally identical to what it replaced, not an approximation.
//
// 2. Why there is no auth logic here anymore. This file used to also do
//    protected-route redirects and server-side refresh-token/cookie-patching. That
//    was stripped while hunting the crash above (it turned out to be innocent, but
//    the simplification stands). Not a security regression: the API enforces auth on
//    every request regardless of what the frontend does, and each protected page
//    already guards on `!session` client-side (e.g. app/[locale]/dashboard/page.tsx,
//    app/[locale]/admin/layout.tsx) and renders a sign-in prompt instead of
//    protected content. The real behavioural cost, stated honestly: with no
//    middleware refresh ahead of the server render, getServerSession()
//    (lib/session.ts) has nothing to read once the access token expires, and there
//    is no automatic client-side recovery (lib/auth.tsx's AuthProvider has no
//    hydration effect, by design), so a protected page shows its signed-out
//    fallback for the rest of that session rather than momentarily. Accepted; see
//    docs/ARCHITECTURE.md §6.
export function middleware(request: Request) {
  // Carries the query string already — only the pathname is overwritten below.
  const url = new URL(request.url);
  const { pathname } = url;

  const hasLocale = locales.some(
    (locale) => pathname === `/${locale}` || pathname.startsWith(`/${locale}/`)
  );
  if (hasLocale) {
    // Falsy return === continue to the route. See adapter.js's `response ? response
    // : NextResponse.next()`.
    return;
  }

  url.pathname = `/${defaultLocale}${pathname === "/" ? "" : pathname}`;
  return new Response(null, {
    status: 307,
    headers: { Location: url.toString() }
  });
}

export const config = {
  // Skip Next internals, the API proxy, and static assets.
  matcher: ["/((?!_next|api|favicon.ico|.*\\..*).*)"]
};
