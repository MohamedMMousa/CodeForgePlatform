"use client";

// Auth context for the shell: holds the signed-in user's profile (not tokens — those
// live only in httpOnly cookies the server manages; see lib/session.ts). Seeded from
// a server-resolved initialSession so the first client render already matches what
// the server rendered — no hydration mismatch. `canRecover` (from lib/session.ts) says
// whether cf_refresh outlived cf_access server-side; when it did, the mount effect
// below attempts one silent client-side refresh so a page loaded right after token
// expiry can still recover without a manual re-login. See docs/ARCHITECTURE.md §6.

import { createContext, useContext, useEffect, useMemo, useState } from "react";
import { useRouter } from "next/navigation";
import {
  AuthResponse,
  ApiRequestError,
  SESSION_EXPIRED_EVENT,
  login as apiLogin,
  logout as apiLogout,
  getCurrentUser
} from "./api";
import { Session, toSession } from "./session-mapping";

export type { Session } from "./session-mapping";

/** "recovering" is transient — the mount effect resolves it to one of the other two
 * within the recovery timeout. Consumers that gate on session existence (see
 * components/SessionGuard.tsx) use this to show a neutral loading state instead of a
 * premature signed-out prompt. */
export type AuthStatus = "authenticated" | "recovering" | "unauthenticated";

const RECOVERY_TIMEOUT_MS = 10_000;

interface AuthContextValue {
  session: Session | null;
  status: AuthStatus;
  signIn: (email: string, password: string, locale?: string) => Promise<AuthResponse>;
  /** Re-derives the session from the server — e.g. after change-password rotates
   * the cookies — and refreshes server components so they see the new state too. */
  refreshSession: () => Promise<void>;
  signOut: () => Promise<void>;
}

const AuthContext = createContext<AuthContextValue | undefined>(undefined);

export function AuthProvider({
  initialSession,
  canRecover,
  children
}: {
  initialSession: Session | null;
  canRecover: boolean;
  children: React.ReactNode;
}) {
  const [session, setSession] = useState<Session | null>(initialSession);
  const [status, setStatus] = useState<AuthStatus>(() =>
    initialSession ? "authenticated" : canRecover ? "recovering" : "unauthenticated"
  );
  const router = useRouter();

  // Runs once, only for a page load that arrived with an expired access token but a
  // still-live refresh token (status seeded to "recovering" above). getCurrentUser
  // flows through apiFetch's existing 401-retry (lib/api.ts), which itself already
  // serializes concurrent refreshes behind one in-flight promise — nothing extra to
  // coordinate here. Bounded by RECOVERY_TIMEOUT_MS so a slow/unreachable API degrades
  // to the normal signed-out UI instead of leaving pages stuck on a loading state.
  useEffect(() => {
    if (status !== "recovering") return;
    let cancelled = false;
    getCurrentUser(undefined, AbortSignal.timeout(RECOVERY_TIMEOUT_MS))
      .then((current) => {
        if (cancelled) return;
        setSession(toSession(current));
        setStatus("authenticated");
        router.refresh();
      })
      .catch(() => {
        if (!cancelled) setStatus("unauthenticated");
      });
    return () => {
      cancelled = true;
    };
    // Deliberately empty: this must run exactly once, evaluating `status` as it stood
    // at mount — signIn/signOut/refreshSession never send status back to "recovering".
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  // A refresh attempt that got an explicit dead-token response (lib/api.ts) means the
  // session is gone; clear it in place rather than leaving stale signed-in state that
  // every subsequent request will just 401 against.
  useEffect(() => {
    function onSessionExpired() {
      setSession(null);
      setStatus("unauthenticated");
    }
    window.addEventListener(SESSION_EXPIRED_EVENT, onSessionExpired);
    return () => window.removeEventListener(SESSION_EXPIRED_EVENT, onSessionExpired);
  }, []);

  const value = useMemo<AuthContextValue>(
    () => ({
      session,
      status,
      async signIn(email, password, locale) {
        const auth = await apiLogin(email, password, locale);
        setSession(toSession(auth));
        setStatus("authenticated");
        router.refresh();
        return auth;
      },
      async refreshSession() {
        try {
          const current = await getCurrentUser();
          setSession(toSession(current));
          setStatus("authenticated");
        } catch (error) {
          if (error instanceof ApiRequestError && error.info.status === 401) {
            setSession(null);
            setStatus("unauthenticated");
          } else {
            throw error;
          }
        }
        router.refresh();
      },
      async signOut() {
        await apiLogout();
        setSession(null);
        setStatus("unauthenticated");
        router.refresh();
      }
    }),
    [session, status, router]
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
