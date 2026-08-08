// Server-only: resolves the signed-in user before first paint, from the httpOnly
// cf_access cookie. Only usable from Server Components/layouts — next/headers'
// cookies() throws if called from a Client Component.
//
// Reads cf_access as-is; does not refresh it — a Server Component can't persist a
// Set-Cookie from a refresh call, so doing that here would mint a rotated refresh
// token the browser never receives. That recovery happens client-side instead (see
// lib/auth.tsx); this function's job is only to tell the client whether recovery is
// even worth attempting, via `canRecover` — cf_refresh outliving cf_access is exactly
// the "access token expired mid-session" case. See docs/ARCHITECTURE.md §6.

import { cache } from "react";
import { cookies } from "next/headers";
import type { components } from "./api-schema";

export type ServerSession = components["schemas"]["CurrentUserResponse"];

export interface ServerSessionResult {
  session: ServerSession | null;
  /** True when cf_access is gone but cf_refresh is still live — a client-side
   * refresh (lib/auth.tsx) has a real chance of restoring the session. */
  canRecover: boolean;
}

// Direct to the API, not through the /api/* proxy — this already runs on the Next
// server, so routing through Next's own rewrite would just be a pointless extra hop.
const API_INTERNAL_URL = process.env.API_INTERNAL_URL ?? "http://localhost:5205";

/** Wrapped in React's cache() so one render pass makes at most one call, no matter
 * how many Server Components ask for the session. */
export const getServerSession = cache(async (): Promise<ServerSessionResult> => {
  const cookieStore = await cookies();
  const accessToken = cookieStore.get("cf_access")?.value;
  if (!accessToken) {
    return { session: null, canRecover: cookieStore.has("cf_refresh") };
  }

  try {
    const response = await fetch(`${API_INTERNAL_URL}/auth/me`, {
      headers: { Cookie: `cf_access=${accessToken}` },
      cache: "no-store"
    });
    if (!response.ok) {
      return { session: null, canRecover: cookieStore.has("cf_refresh") };
    }
    return { session: (await response.json()) as ServerSession, canRecover: false };
  } catch {
    // A transient backend hiccup shouldn't crash every page render — treat it the
    // same as "not signed in" and let the client-side session take over.
    return { session: null, canRecover: cookieStore.has("cf_refresh") };
  }
});
