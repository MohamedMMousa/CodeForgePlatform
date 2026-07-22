"use client";

import Link from "next/link";
import { useAuth } from "@/lib/auth";
import { Dictionary } from "@/lib/i18n";

export function RoleNav({ locale, t }: { locale: string; t: Dictionary }) {
  const { session, signOut } = useAuth();

  if (!session) {
    return <Link href={`/${locale}/login`}>{t.home.signIn}</Link>;
  }

  const isStaff = session.role === "admin" || session.role === "instructor";

  return (
    <>
      {session.role === "student" && (
        <Link href={`/${locale}/dashboard`}>{t.student.dashboard}</Link>
      )}
      {isStaff && (
        <Link href={`/${locale}/instructor`}>{t.instructor.title}</Link>
      )}
      <span className="muted">{session.fullName}</span>
      <button className="btn secondary" onClick={() => signOut()}>
        {t.nav.signOut}
      </button>
    </>
  );
}
