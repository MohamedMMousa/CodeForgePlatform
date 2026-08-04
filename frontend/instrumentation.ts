// Server/edge-side Sentry init — Next.js 15's instrumentation hook. register() runs
// once at process start; onRequestError runs per request-handling error (route
// handlers, server components, server actions, middleware) and is what actually
// reports most server-side failures — see the Sentry docs' explicit warning that
// register() alone is not enough. See instrumentation-client.ts for the browser
// counterpart.
import * as Sentry from "@sentry/nextjs";

function scrubBeforeSend(event: Sentry.ErrorEvent): Sentry.ErrorEvent {
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

export async function register() {
  const dsn = process.env.NEXT_PUBLIC_SENTRY_DSN;
  if (!dsn) {
    return;
  }

  if (process.env.NEXT_RUNTIME === "nodejs" || process.env.NEXT_RUNTIME === "edge") {
    Sentry.init({
      dsn,
      environment: process.env.NODE_ENV,
      tracesSampleRate: 0,
      sendDefaultPii: false,
      beforeSend: scrubBeforeSend
    });
  }
}

export const onRequestError = Sentry.captureRequestError;
