"use client";

import { usePathname } from "next/navigation";
import { ShopNav } from "@/components/ShopNav";
import type { Dictionary, Locale } from "@/lib/i18n";

/**
 * Swaps the whole header rather than the layout tree.
 *
 * The legacy topbar is inlined in `app/[locale]/layout.tsx`'s `<body>`, so a
 * nested route-group layout for `/catalog` could only ever ADD a nav there,
 * never remove the one the root layout already renders — making the new dark
 * shop-window Nav (DESIGN_LANGUAGE.md §3/§4 #2) exclusive to the catalog
 * subtree the "correct" way would mean moving every *other* route directory
 * (home, dashboard, admin, auth, …) into a sibling group. That's a whole-app
 * change; see `docs/ARCHITECTURE.md`'s catalog-migration note for why it's
 * deferred.
 *
 * This is the cheaper alternative: read the current path once, client-side,
 * and render `ShopNav` for `/catalog` and everything under it, or fall
 * through to `children` — the legacy topbar JSX, passed through unmodified
 * (still server-rendered; a Server Component can be handed to a Client
 * Component as `children` without itself becoming client code) — everywhere
 * else. Non-catalog routes get byte-identical output to before this
 * component existed.
 */
export function SiteHeader({
  locale,
  t,
  children
}: {
  locale: Locale;
  t: Dictionary;
  children: React.ReactNode;
}) {
  const pathname = usePathname() ?? `/${locale}`;
  const isCatalogRoute =
    pathname === `/${locale}/catalog` || pathname.startsWith(`/${locale}/catalog/`);

  if (isCatalogRoute) {
    return <ShopNav locale={locale} t={t} />;
  }

  return <>{children}</>;
}
