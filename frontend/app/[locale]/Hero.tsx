import Link from "next/link";
import { Check } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Reveal } from "@/components/motion/Reveal";
import { CountUp } from "@/components/motion/CountUp";
import { TypeOnce } from "@/components/motion/TypeOnce";
import type { Dictionary, Locale } from "@/lib/i18n";

// A short, friendly, language-agnostic "lesson" snippet for the self-typing
// visual. Code stays LTR / JetBrains Mono in both locales (globals.css pins
// `pre`/`code`), so the same string is correct on the Arabic page. MENA-forward
// student names, kept Latin-transliterated because they live inside code.
const LESSON_SNIPPET = `function greet(name) {
  return \`Hello, \${name}!\`;
}

const cohort = ["Layla", "Omar", "Sara"];
cohort.forEach((student) => {
  console.log(greet(student));
});`;

// The staggered load-in is CSS-only and gated on `motion-safe:` — under
// prefers-reduced-motion the enter animation simply doesn't exist, so each item
// renders in its final, fully-present state with no delayed pop-in (not merely
// a duration-zeroed one). `fill-mode-backwards` holds the from-state through the
// delay so a later item never flashes in before its turn.
const RISE =
  "motion-safe:animate-in motion-safe:fade-in motion-safe:slide-in-from-bottom-3 " +
  "motion-safe:fill-mode-backwards motion-safe:duration-500 motion-safe:ease-out";

/**
 * Landing hero (DESIGN_LANGUAGE.md §4 #1) — dark shop-window surface, one
 * primary CTA. A server component that composes the client motion islands
 * (`TypeOnce`, `CountUp`, `Reveal`); the text/CTA load-in is pure CSS so it
 * needs no client JS and is covered by the reduced-motion gating above.
 */
export function Hero({ locale, t }: { locale: Locale; t: Dictionary }) {
  return (
    <section className="relative overflow-hidden bg-bg">
      {/* Decorative accent glow. Token-driven via color-mix(var(--accent)) so it
          stays on-palette; purely presentational, hidden from assistive tech. */}
      <div
        aria-hidden="true"
        className="pointer-events-none absolute inset-x-0 top-0 -z-0 h-80"
        style={{
          background:
            "radial-gradient(55% 80% at 50% 0%, color-mix(in oklab, var(--accent) 12%, transparent), transparent)"
        }}
      />
      <div className="relative mx-auto grid w-full max-w-6xl grid-cols-1 items-center gap-12 ps-5 pe-5 py-16 lg:grid-cols-2 lg:py-24">
        <div className="flex flex-col items-start gap-6">
          <span
            className={`inline-flex items-center gap-2 rounded-pill border border-border-strong bg-surface ps-3 pe-3 py-1 eyebrow text-text-secondary ${RISE}`}
          >
            <span aria-hidden="true" className="size-1.5 rounded-pill bg-accent" />
            {t.landing.eyebrow}
          </span>

          <h1 className={`text-display text-text ${RISE} motion-safe:delay-75`}>
            {t.landing.headline}
          </h1>

          <p
            className={`text-body-lg max-w-xl text-text-secondary ${RISE} motion-safe:delay-150`}
          >
            {t.landing.subhead}
          </p>

          <div className={`flex flex-wrap gap-3 ${RISE} motion-safe:delay-200`}>
            <Button asChild size="lg">
              <Link href={`/${locale}/catalog`}>{t.landing.browseCohorts}</Link>
            </Button>
            <Button asChild variant="secondary" size="lg">
              {/* Same-page anchor; the how-it-works section lands in part 2. */}
              <a href="#how-it-works">{t.landing.howItWorks}</a>
            </Button>
          </div>

          <ul
            className={`flex flex-wrap items-center gap-x-6 gap-y-2 text-label text-text-secondary ${RISE} motion-safe:delay-300`}
          >
            <li className="inline-flex items-center gap-2">
              <Check aria-hidden="true" className="size-4 shrink-0 text-accent" />
              <span>
                <span className="font-bold text-accent">
                  <CountUp value={100} locale={locale} suffix="%" />
                </span>{" "}
                {t.landing.trustLiveLabel}
              </span>
            </li>
            <li className="inline-flex items-center gap-2">
              <Check aria-hidden="true" className="size-4 shrink-0 text-accent" />
              <span>{t.landing.trustBilingual}</span>
            </li>
            <li className="inline-flex items-center gap-2">
              <Check aria-hidden="true" className="size-4 shrink-0 text-accent" />
              <span>{t.landing.trustCertificate}</span>
            </li>
          </ul>
        </div>

        {/* Exercises the Reveal primitive in part 1. Above the fold on desktop,
            so the observer fires on mount; below the trust row on mobile, where
            it reveals on scroll. */}
        <Reveal className="w-full">
          <div className="rounded-card border border-border bg-surface">
            <div className="flex items-center gap-2 border-b border-border ps-4 pe-4 py-3">
              <span aria-hidden="true" className="size-2.5 rounded-pill bg-border-strong" />
              <span aria-hidden="true" className="size-2.5 rounded-pill bg-border-strong" />
              <span aria-hidden="true" className="size-2.5 rounded-pill bg-border-strong" />
              <span className="ms-2 font-mono text-meta text-text-muted">lesson.js</span>
            </div>
            <div className="ps-4 pe-4 py-4">
              <TypeOnce
                code={LESSON_SNIPPET}
                ariaLabel={t.landing.codeSnippetLabel}
                className="text-text-secondary"
              />
            </div>
          </div>
        </Reveal>
      </div>
    </section>
  );
}
