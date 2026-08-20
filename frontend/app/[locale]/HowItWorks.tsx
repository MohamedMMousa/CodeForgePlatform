import { Search, Send, BadgeCheck, Rocket } from "lucide-react";
import { Reveal } from "@/components/motion/Reveal";
import type { Dictionary } from "@/lib/i18n";

// DESIGN_LANGUAGE.md §4's enrollment-model note: nothing in the old UI told a
// first-time visitor that enrollment is a *request* the admin approves, not an
// instant checkout. This section is what closes that gap for the landing —
// see docs/DESIGN_LANGUAGE.md §4 and docs/ARCHITECTURE.md §6 for the record.
// The four steps mirror the real flow: browse (catalog) -> request (anonymous
// POST /enrollment-requests via /enroll) -> admin review/approve -> account
// auto-created + access. `id="how-it-works"` is the hero's secondary CTA target.
const STEPS = [
  { icon: Search, titleKey: "hiwStep1Title", bodyKey: "hiwStep1Body" },
  { icon: Send, titleKey: "hiwStep2Title", bodyKey: "hiwStep2Body" },
  { icon: BadgeCheck, titleKey: "hiwStep3Title", bodyKey: "hiwStep3Body" },
  { icon: Rocket, titleKey: "hiwStep4Title", bodyKey: "hiwStep4Body" }
] as const;

export function HowItWorks({ t }: { t: Dictionary }) {
  return (
    <section id="how-it-works" className="bg-bg">
      <div className="mx-auto w-full max-w-6xl ps-5 pe-5 py-16 lg:py-20">
        <Reveal className="flex flex-col gap-2 text-center" as="div">
          <h2 className="text-h2 text-text">{t.landing.hiwHeading}</h2>
          <p className="mx-auto max-w-2xl text-body-lg text-text-secondary">
            {t.landing.hiwSubhead}
          </p>
        </Reveal>

        <ol className="mt-10 grid grid-cols-1 gap-6 sm:grid-cols-2 lg:grid-cols-4">
          {STEPS.map((step, index) => {
            const Icon = step.icon;
            return (
              <Reveal key={step.titleKey} as="li" delayMs={index * 80} className="h-full">
                <div className="flex h-full flex-col gap-3 rounded-card border border-border bg-surface p-5">
                  <div className="flex items-center gap-3">
                    <span className="flex size-9 shrink-0 items-center justify-center rounded-pill bg-accent text-accent-ink font-bold">
                      {/* Western numerals (§2.1) — plain index, no locale formatting needed for 1-4. */}
                      {index + 1}
                    </span>
                    <Icon aria-hidden="true" className="size-5 shrink-0 text-accent" />
                  </div>
                  <h3 className="text-h3 text-text">{t.landing[step.titleKey]}</h3>
                  <p className="text-body text-text-secondary">{t.landing[step.bodyKey]}</p>
                </div>
              </Reveal>
            );
          })}
        </ol>
      </div>
    </section>
  );
}
