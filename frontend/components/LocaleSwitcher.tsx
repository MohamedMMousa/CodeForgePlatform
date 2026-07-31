"use client";

import Link from "next/link";
import { usePathname, useSearchParams } from "next/navigation";
import { locales, type Locale } from "@/lib/i18n";

export function LocaleSwitcher({ locale, label }: { locale: Locale; label: string }) {
  const pathname = usePathname() ?? `/${locale}`;
  const searchParams = useSearchParams();
  const other = locale === "en" ? "ar" : "en";

  const segments = pathname.split("/");
  const rest = locales.includes(segments[1] as Locale)
    ? segments.slice(2).join("/")
    : segments.slice(1).join("/");
  const query = searchParams.toString();
  const href = `/${other}${rest ? `/${rest}` : ""}${query ? `?${query}` : ""}`;

  return <Link href={href}>{label}</Link>;
}
