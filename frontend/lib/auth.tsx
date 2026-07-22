"use client";

// Lightweight auth context for the shell: holds the signed-in session and tokens.
// Persistence is localStorage for now; hardened token handling (refresh rotation,
// httpOnly cookies) is a later-phase concern. This proves end-to-end auth wiring.

import { createContext, useContext, useEffect, useMemo, useState } from "react";
import { AuthResponse, login as apiLogin } from "./api";

interface Session {
  userId: string;
  email: string;
  fullName: string;
  role: string;
  accessToken: string;
  refreshToken: string;
  mustChangePassword: boolean;
}

interface AuthContextValue {
  session: Session | null;
  signIn: (email: string, password: string, locale?: string) => Promise<AuthResponse>;
  signOut: () => void;
}

const STORAGE_KEY = "codeforge.session";
const AuthContext = createContext<AuthContextValue | undefined>(undefined);

function toSession(auth: AuthResponse): Session {
  return {
    userId: auth.userId,
    email: auth.email,
    fullName: auth.fullName,
    role: auth.role,
    accessToken: auth.accessToken,
    refreshToken: auth.refreshToken,
    mustChangePassword: auth.mustChangePassword
  };
}

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [session, setSession] = useState<Session | null>(null);

  useEffect(() => {
    const raw =
      typeof window !== "undefined" ? window.localStorage.getItem(STORAGE_KEY) : null;
    if (raw) {
      try {
        setSession(JSON.parse(raw) as Session);
      } catch {
        window.localStorage.removeItem(STORAGE_KEY);
      }
    }
  }, []);

  const value = useMemo<AuthContextValue>(
    () => ({
      session,
      async signIn(email, password, locale) {
        const auth = await apiLogin(email, password, locale);
        const next = toSession(auth);
        setSession(next);
        window.localStorage.setItem(STORAGE_KEY, JSON.stringify(next));
        return auth;
      },
      signOut() {
        setSession(null);
        window.localStorage.removeItem(STORAGE_KEY);
      }
    }),
    [session]
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
