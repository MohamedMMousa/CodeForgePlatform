"use client";

// Single source of truth for the "is this page allowed to render yet" check that used
// to be copy-pasted as `if (!session ...) return <fallback/>` at 17 call sites. A hook
// rather than a wrapper component: several of those pages read `session.role` in their
// own JSX (e.g. instructor/courses/[courseId]/page.tsx), so the early-return has to
// happen inline in the page's own render, not inside a wrapper whose `children` would
// already be constructed (and would throw) before the wrapper could block them. The
// early-return also keeps TypeScript's narrowing of `session` intact downstream.
//
// The three fallback shapes below are exactly what the 17 sites rendered before this
// existed — this only removes the duplication, not the behavior. See lib/auth.tsx for
// the `status` values this switches on.

import Link from "next/link";
import { useAuth, Session } from "@/lib/auth";
import { getDictionary, Locale } from "@/lib/i18n";

export interface SessionGateOptions {
  locale: Locale;
  /** Roles allowed beyond "any signed-in user". Staff-only pages pass this and render
   * the instructor/admin sign-in notice; omitted (student-only pages) renders a
   * fallback that links straight to /login, matching what those pages did before. */
  roles?: readonly string[];
  /** Pages that render inside admin/layout.tsx are already inside its
   * `<main className="container">` — pass true to render a bare `<p>` instead of a
   * second nested `<main>`. */
  bare?: boolean;
}

export type SessionGateResult =
  | { ok: true; session: Session }
  | { ok: false; fallback: React.ReactNode };

export function useSessionGate({ locale, roles, bare }: SessionGateOptions): SessionGateResult {
  const { session, status } = useAuth();
  const roleAllowed = !roles || (session !== null && roles.includes(session.role));

  if (session && roleAllowed) {
    return { ok: true, session };
  }

  if (status === "recovering") {
    return { ok: false, fallback: wrap(bare, <RecoveringNotice locale={locale} />) };
  }

  const fallback = roles ? (
    <p className="notice err">{getDictionary(locale).instructor.signInRequired}</p>
  ) : (
    <p className="notice err">
      <Link href={`/${locale}/login`}>{getDictionary(locale).home.signIn}</Link>
    </p>
  );

  return { ok: false, fallback: wrap(bare, fallback) };
}

function RecoveringNotice({ locale }: { locale: Locale }) {
  return (
    <p className="notice" aria-live="polite">
      {getDictionary(locale).nav.restoringSession}
    </p>
  );
}

function wrap(bare: boolean | undefined, node: React.ReactNode): React.ReactNode {
  return bare ? node : <main className="container">{node}</main>;
}
