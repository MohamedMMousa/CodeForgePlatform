"use server";

import * as Sentry from "@sentry/nextjs";

// Gated the same way as the page itself (SENTRY_TEST_ENABLED — see page.tsx for why
// this is a plain server-only var, not NEXT_PUBLIC_) rather than behind admin auth —
// this whole page only exists while an operator has deliberately flipped the flag on
// for a short verification window, and that flag IS the access control.
export async function sendServerSentryTestError(): Promise<{ ok: boolean }> {
  if (!process.env.SENTRY_TEST_ENABLED) {
    return { ok: false };
  }

  Sentry.captureException(new Error("Sentry test error triggered via the frontend server action."));
  return { ok: true };
}
