import { Reveal } from "@/components/motion/Reveal";
import type { Dictionary } from "@/lib/i18n";

// Illustrative brand storytelling, not live data — no entity in the schema
// backs a generic "learning journey" (course content is per-course modules,
// see ARCHITECTURE.md §6's surface #5 note). Deliberately static.
const STEPS = [
  { titleKey: "journeyStep1Title", bodyKey: "journeyStep1Body" },
  { titleKey: "journeyStep2Title", bodyKey: "journeyStep2Body" },
  { titleKey: "journeyStep3Title", bodyKey: "journeyStep3Body" },
  { titleKey: "journeyStep4Title", bodyKey: "journeyStep4Body" },
  { titleKey: "journeyStep5Title", bodyKey: "journeyStep5Body" }
] as const;

export function LearningJourney({ t }: { t: Dictionary }) {
  return (
    <section className="bg-bg">
      <div className="mx-auto w-full max-w-3xl ps-5 pe-5 py-16 lg:py-20">
        <Reveal className="flex flex-col gap-2 text-center">
          <h2 className="text-h2 text-text">{t.landing.journeyHeading}</h2>
          <p className="text-body-lg text-text-secondary">{t.landing.journeySubhead}</p>
        </Reveal>

        {/* Vertical ladder: a start-side rail with numbered markers. `ps-*` on
            the rail and markers (not `pl-*`) so it mirrors correctly in RTL —
            the rail sits on the reading-start side in both directions. */}
        <ol className="relative mt-10 flex flex-col gap-8 ps-2">
          <div
            aria-hidden="true"
            className="absolute top-2 bottom-2 start-[19px] w-px bg-border"
          />
          {STEPS.map((step, index) => (
            <Reveal key={step.titleKey} as="li" delayMs={index * 100} className="relative ps-12">
              <span className="absolute start-0 top-0 flex size-10 shrink-0 items-center justify-center rounded-pill border border-border-strong bg-surface font-bold text-accent">
                {index + 1}
              </span>
              <h3 className="text-h3 text-text">{t.landing[step.titleKey]}</h3>
              <p className="mt-1 text-body text-text-secondary">{t.landing[step.bodyKey]}</p>
            </Reveal>
          ))}
        </ol>
      </div>
    </section>
  );
}
