// Node-runtime-only Sentry init. Deliberately NOT in instrumentation.ts (Next.js's
// conventional hook) or exporting onRequestError from there: that file is compiled
// twice by Next.js, once per runtime, and any reference to @sentry/nextjs anywhere in
// it -- even behind a NEXT_RUNTIME runtime check, even via a dynamic import() inside
// that check -- ended up present in the compiled edge-instrumentation.js too
// (confirmed directly: rebuilt and grepped the compiled output, found
// Sentry.withIsolationScope, the SDK's console logger string, and its
// __sentry_template_string__ marker still there). That's what broke the Edge Runtime
// bundle middleware.ts invokes, on top of the separate autoInstrumentMiddleware
// auto-wrap issue (see next.config.mjs) -- twice, in production.
//
// This file is imported once, for its side effect, from the root layout
// (app/[locale]/layout.tsx) -- a Server Component, which this app never compiles for
// the edge runtime (nothing here opts into `export const runtime = "edge"`; only
// middleware.ts is edge, and it's forced there by the framework, and does not import
// this file). That guarantees, by construction rather than by hoping a bundler
// eliminates a dead branch, that nothing in this module's import graph is ever
// compiled into an edge bundle.
//
// Cost: edge/middleware-context errors are not sent to Sentry (already true since the
// autoInstrumentMiddleware fix) and instrumentation.ts's dedicated onRequestError
// hook no longer exists. Route handlers, server components, and server actions are
// still covered -- withSentryConfig's autoInstrumentAppDirectory/
// autoInstrumentServerFunctions (both still on, untouched) wrap them to call
// Sentry.captureException directly, and only need Sentry.init() to have run first,
// which this file guarantees on every request (the root layout renders on all of
// them). Browser errors (instrumentation-client.ts) are unaffected either way.
import * as Sentry from "@sentry/nextjs";

const dsn = process.env.NEXT_PUBLIC_SENTRY_DSN;

if (dsn) {
  Sentry.init({
    dsn,
    environment: process.env.NODE_ENV,
    tracesSampleRate: 0,
    sendDefaultPii: false,
    beforeSend(event) {
      if (event.request) {
        delete event.request.cookies;
        if (event.request.headers) {
          for (const header of ["Cookie", "Authorization", "X-CSRF-Token"]) {
            delete event.request.headers[header];
          }
        }
      }
      return event;
    }
  });
}
