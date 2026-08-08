"use client";

import Link from "next/link";
import { useAuth } from "@/lib/auth";
import { Dictionary } from "@/lib/i18n";

export function RoleNav({ locale, t }: { locale: string; t: Dictionary }) {
  const { session, status, signOut } = useAuth();

  // While a post-expiry recovery is in flight (lib/auth.tsx), session is still null —
  // show only the always-public link rather than a "Sign in" link that would just
  // flash away the moment recovery succeeds.
  if (status === "recovering") {
    return <Link href={`/${locale}/verify`}>{t.certificates.verifyButton}</Link>;
  }

  if (!session) {
    return (
      <>
        <Link href={`/${locale}/verify`}>{t.certificates.verifyButton}</Link>
        <Link href={`/${locale}/login`}>{t.home.signIn}</Link>
      </>
    );
  }

  const isStaff = session.role === "admin" || session.role === "instructor";

  return (
    <>
      {session.role === "student" && (
        <>
          <Link href={`/${locale}/dashboard`}>{t.student.dashboard}</Link>
          <Link href={`/${locale}/my-certificates`}>{t.student.certificates}</Link>
        </>
      )}
      {isStaff && (
        <Link href={`/${locale}/instructor`}>{t.instructor.title}</Link>
      )}
      {session.role === "admin" && (
        <Link href={`/${locale}/admin/courses`}>{t.admin.title}</Link>
      )}
      <Link href={`/${locale}/verify`}>{t.certificates.verifyButton}</Link>
      <span className="muted">{session.fullName}</span>
      <button
        className="btn secondary"
        onClick={() => {
          // signOut() clears cookies, sets status to "unauthenticated", and calls
          // router.refresh(); a protected page then falls back to its own signed-out UI.
          void signOut();
        }}
      >
        {t.nav.signOut}
      </button>
    </>
  );
}
