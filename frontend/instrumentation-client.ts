// Browser-side Sentry init — Next.js 15's replacement for sentry.client.config.ts,
// loaded automatically before any other client code. See instrumentation.ts for the
// server/edge counterpart and CodeForge.Api/Program.cs for the backend's own,
// separate Sentry instance (this frontend and the .NET API each report to their own
// Sentry project/DSN).
import * as Sentry from "@sentry/nextjs";

const dsn = process.env.NEXT_PUBLIC_SENTRY_DSN;

if (dsn) {
  Sentry.init({
    dsn,
    environment: process.env.NODE_ENV,
    // Free-tier quota goes to errors, not performance traces.
    tracesSampleRate: 0,
    sendDefaultPii: false,
    // Defense in depth: httpOnly cookies (cf_access/cf_refresh) are already invisible
    // to browser JS by construction, but scrub explicitly rather than relying on that
    // alone — matches the same header/cookie denylist the API applies server-side.
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

// Required export the build otherwise warns about. Only produces spans when tracing
// is active, which it isn't here (tracesSampleRate: 0 above) — so this stays inert,
// it just satisfies the SDK's build-time check for navigation instrumentation wiring.
export const onRouterTransitionStart = Sentry.captureRouterTransitionStart;
