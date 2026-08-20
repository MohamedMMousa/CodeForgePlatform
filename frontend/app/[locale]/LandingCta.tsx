import Link from "next/link";
import { Button } from "@/components/ui/button";
import { Reveal } from "@/components/motion/Reveal";
import type { Dictionary, Locale } from "@/lib/i18n";

// Closing action band — one primary per §3. "Sign in" is secondary here (an
// existing student's path), not competing with "Browse cohorts" (the new
// visitor's path, and the page's real primary).
export function LandingCta({ locale, t }: { locale: Locale; t: Dictionary }) {
  return (
    <section className="border-t border-border bg-bg">
      <div className="mx-auto w-full max-w-6xl ps-5 pe-5 py-16 lg:py-20">
        <Reveal className="flex flex-col items-center gap-6 text-center">
          <h2 className="text-h2 text-text">{t.landing.ctaHeading}</h2>
          <p className="max-w-xl text-body-lg text-text-secondary">{t.landing.ctaSubhead}</p>
          <div className="flex flex-wrap justify-center gap-3">
            <Button asChild size="lg">
              <Link href={`/${locale}/catalog`}>{t.landing.browseCohorts}</Link>
            </Button>
            <Button asChild variant="secondary" size="lg">
              <Link href={`/${locale}/login`}>{t.home.signIn}</Link>
            </Button>
          </div>
        </Reveal>
      </div>
    </section>
  );
}
