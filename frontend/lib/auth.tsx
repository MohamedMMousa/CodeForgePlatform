"use client";

// Auth context for the shell: holds the signed-in user's profile (not tokens — those
// live only in httpOnly cookies the server manages; see lib/session.ts). Seeded from
// a server-resolved initialSession so the first client render already matches what
// the server rendered — there is no hydration effect here on purpose. That avoided
// the sign-in flash back when middleware.ts refreshed an expired access token before
// the server render happened; middleware no longer does that (see
// docs/ARCHITECTURE.md §6), so initialSession is simply whatever lib/session.ts read
// from cf_access as-is. No effect has been added to compensate — see the ARCHITECTURE.md
// note on why not, for now.

import { createContext, useContext, useMemo, useState } from "react";
import { useRouter } from "next/navigation";
import { AuthResponse, ApiRequestError, login as apiLogin, logout as apiLogout, getCurrentUser } from "./api";
import { Session, toSession } from "./session-mapping";

export type { Session } from "./session-mapping";

interface AuthContextValue {
  session: Session | null;
  signIn: (email: string, password: string, locale?: string) => Promise<AuthResponse>;
  /** Re-derives the session from the server — e.g. after change-password rotates
   * the cookies — and refreshes server components so they see the new state too. */
  refreshSession: () => Promise<void>;
  signOut: () => Promise<void>;
}

const AuthContext = createContext<AuthContextValue | undefined>(undefined);

export function AuthProvider({
  initialSession,
  children
}: {
  initialSession: Session | null;
  children: React.ReactNode;
}) {
  const [session, setSession] = useState<Session | null>(initialSession);
  const router = useRouter();

  const value = useMemo<AuthContextValue>(
    () => ({
      session,
      async signIn(email, password, locale) {
        const auth = await apiLogin(email, password, locale);
        setSession(toSession(auth));
        router.refresh();
        return auth;
      },
      async refreshSession() {
        try {
          const current = await getCurrentUser();
          setSession(toSession(current));
        } catch (error) {
          if (error instanceof ApiRequestError && error.info.status === 401) {
            setSession(null);
          } else {
            throw error;
          }
        }
        router.refresh();
      },
      async signOut() {
        await apiLogout();
        setSession(null);
        router.refresh();
      }
    }),
    [session, router]
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthContextValue {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error("useAuth must be used within an AuthProvider.");
  }
  return context;
}
