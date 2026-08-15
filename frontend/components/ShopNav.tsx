import Image from "next/image";
import Link from "next/link";
import { Suspense } from "react";
import { LocaleSwitcher } from "@/components/LocaleSwitcher";
import type { Dictionary, Locale } from "@/lib/i18n";

/**
 * The dark shop-window nav for the catalog subtree (DESIGN_LANGUAGE.md §3
 * "Nav" + §4 surface #2): brand on the start side, links + language toggle +
 * Sign in on the end side, hairline bottom border, fully mirrored in RTL via
 * logical properties only. Rendered in place of the legacy topbar for
 * `/catalog` and everything under it — see `components/SiteHeader.tsx` for
 * the swap mechanism.
 *
 * Known v1 limitation, accepted deliberately: this is static shop-window
 * chrome, not `RoleNav` — it never reads the session, so a signed-in visitor
 * still sees "Sign in" here. Revisit when the auth surfaces (§4 #9) are
 * rebuilt on the design system; until then the real signed-in experience
 * lives behind the legacy topbar everywhere else in the app.
 */
export function ShopNav({ locale, t }: { locale: Locale; t: Dictionary }) {
  return (
    <header className="border-b border-border bg-bg">
      <div className="mx-auto flex w-full max-w-6xl items-center justify-between gap-4 ps-5 pe-5 py-4">
        {/* `!` on every `text-*` here: globals.css's legacy section has an
            UNLAYERED `a { color: var(--accent-2) }` default-link-colour rule
            (pre-design-system). Unlayered CSS beats anything in
            `@layer utilities` regardless of specificity, so a plain
            `text-text` on this Link was silently losing and rendering
            accent-2 orange instead — every link in this nav, including the
            brand wordmark's "Code", read orange until this fix. See
            components/ui/button.tsx for the same fix on button-as-link. */}
        <Link href={`/${locale}`} className="flex items-center gap-2 !text-text">
          <Image src="/logo-dark.png" alt="" width={32} height={28} />
          <span className="font-bold">
            {locale === "en" ? (
              <>
                Code<span className="text-accent">Forge</span>
              </>
            ) : (
              t.appName
            )}
          </span>
        </Link>
        <nav className="flex items-center gap-5 text-label">
          <Link
            href={`/${locale}/catalog`}
            className="!text-text hover:!text-accent-text"
          >
            {t.nav.catalog}
          </Link>
          <Link
            href={`/${locale}/contact`}
            className="!text-text-muted hover:!text-accent-text"
          >
            {t.nav.contact}
          </Link>
          <Link
            href={`/${locale}/verify`}
            className="!text-text-muted hover:!text-accent-text"
          >
            {t.certificates.verifyButton}
          </Link>
          {/* LocaleSwitcher renders a bare, classless <Link> (shared with the
              legacy topbar, where inheriting the legacy orange is correct) —
              reskin its child anchor here without touching the shared
              component or any other surface that reuses it. */}
          <span className="[&_a]:!text-text-muted [&_a]:hover:!text-accent-text">
            <Suspense fallback={null}>
              <LocaleSwitcher locale={locale} label={t.nav.switchTo} />
            </Suspense>
          </span>
          {/* Orange stays disciplined to price/active-chip/wordmark (§1, §3) —
              the resting state here is neutral text; accent only surfaces as
              hover feedback, same as every other link in this nav. */}
          <Link
            href={`/${locale}/login`}
            className="font-semibold !text-text hover:!text-accent-text"
          >
            {t.home.signIn}
          </Link>
        </nav>
      </div>
    </header>
  );
}
