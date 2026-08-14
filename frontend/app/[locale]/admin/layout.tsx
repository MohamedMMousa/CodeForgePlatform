"use client";

import { use } from "react";
import Link from "next/link";
import { usePathname } from "next/navigation";
import { useSessionGate } from "@/components/SessionGuard";
import { defaultLocale, getDictionary, isLocale } from "@/lib/i18n";

export default function AdminLayout({
  children,
  params
}: {
  children: React.ReactNode;
  params: Promise<{ locale: string }>;
}) {
  const { locale: rawLocale } = use(params);
  const locale = isLocale(rawLocale) ? rawLocale : defaultLocale;
  const t = getDictionary(locale).admin;
  const pathname = usePathname();

  const gate = useSessionGate({ locale, roles: ["admin"] });
  if (!gate.ok) return gate.fallback;

  const tabs: { href: string; label: string }[] = [
    { href: `/${locale}/admin/courses`, label: t.navCourses },
    { href: `/${locale}/admin/tracks`, label: t.navTracks },
    { href: `/${locale}/admin/coupons`, label: t.navCoupons },
    { href: `/${locale}/admin/enrollment-requests`, label: t.navEnrollmentRequests },
    { href: `/${locale}/admin/users`, label: t.navUsers },
    { href: `/${locale}/admin/analytics`, label: t.navAnalytics }
  ];

  return (
    <main className="cf-container">
      <h1>{t.title}</h1>
      <nav
        style={{
          display: "flex",
          gap: "0.5rem",
          flexWrap: "wrap",
          marginBottom: "1.5rem",
          borderBottom: "1px solid var(--card)",
          paddingBottom: "0.75rem"
        }}
      >
        {tabs.map((tab) => (
          <Link
            key={tab.href}
            href={tab.href}
            className={`btn ${pathname?.startsWith(tab.href) ? "" : "secondary"}`}
          >
            {tab.label}
          </Link>
        ))}
      </nav>
      {children}
    </main>
  );
}
