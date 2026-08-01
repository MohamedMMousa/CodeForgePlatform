// Deliberately has no "use client"/"use server" directive: lib/auth.tsx (client)
// and app/[locale]/layout.tsx (server) both need toSession, and a function
// exported from a "use client" module can't be called from a Server Component.

import type { AuthResponse, CurrentUserResponse } from "./api";

export interface Session {
  userId: string;
  email: string;
  fullName: string;
  role: string;
  mustChangePassword: boolean;
}

export type SessionSource = Pick<
  AuthResponse | CurrentUserResponse,
  "userId" | "email" | "fullName" | "role" | "mustChangePassword"
>;

export function toSession(source: SessionSource): Session {
  return {
    userId: source.userId,
    email: source.email,
    fullName: source.fullName,
    role: source.role,
    mustChangePassword: source.mustChangePassword
  };
}
