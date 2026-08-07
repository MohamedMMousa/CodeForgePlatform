// Server-only: resolves the signed-in user before first paint, from the httpOnly
// cf_access cookie. Only usable from Server Components/layouts — next/headers'
// cookies() throws if called from a Client Component.
//
// Reads cf_access as-is; does not refresh it. middleware.ts used to refresh an
// expired access token before this ran, which is what let a protected page render
// correctly on first paint even right after token expiry. middleware no longer does
// that (see docs/ARCHITECTURE.md §6 for why — a production Edge Runtime incident),
// so an expired cf_access now means this returns null and the page renders
// signed-out for the rest of that session, not just flashes and recovers.

import { cache } from "react";
import { cookies } from "next/headers";
import type { components } from "./api-schema";

export type ServerSession = components["schemas"]["CurrentUserResponse"];

// Direct to the API, not through the /api/* proxy — this already runs on the Next
// server, so routing through Next's own rewrite would just be a pointless extra hop.
const API_INTERNAL_URL = process.env.API_INTERNAL_URL ?? "http://localhost:5205";

/** Wrapped in React's cache() so one render pass makes at most one call, no matter
 * how many Server Components ask for the session. */
export const getServerSession = cache(async (): Promise<ServerSession | null> => {
  const cookieStore = await cookies();
  const accessToken = cookieStore.get("cf_access")?.value;
  if (!accessToken) {
    return null;
  }

  try {
    const response = await fetch(`${API_INTERNAL_URL}/auth/me`, {
      headers: { Cookie: `cf_access=${accessToken}` },
      cache: "no-store"
    });
    if (!response.ok) {
      return null;
    }
    return (await response.json()) as ServerSession;
  } catch {
    // A transient backend hiccup shouldn't crash every page render — treat it the
    // same as "not signed in" and let the client-side session take over.
    return null;
  }
});
