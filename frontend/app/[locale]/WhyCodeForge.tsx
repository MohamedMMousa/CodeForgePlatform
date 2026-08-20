import { Video, Users, FolderGit2, Languages, Award, MapPin } from "lucide-react";
import { Reveal } from "@/components/motion/Reveal";
import type { Dictionary } from "@/lib/i18n";

// Verifiable differentiators only — no invented headcounts or stats
// (surface-1 prep report flagged this as a hard constraint). Each maps to a
// real, built product fact: live delivery, bilingual UI, and certificates are
// all shipped; "small groups" and "real projects" are product positioning
// without a specific number attached.
const CARDS = [
  { icon: Video, titleKey: "whyCard1Title", bodyKey: "whyCard1Body" },
  { icon: Users, titleKey: "whyCard2Title", bodyKey: "whyCard2Body" },
  { icon: FolderGit2, titleKey: "whyCard3Title", bodyKey: "whyCard3Body" },
  { icon: Languages, titleKey: "whyCard4Title", bodyKey: "whyCard4Body" },
  { icon: Award, titleKey: "whyCard5Title", bodyKey: "whyCard5Body" },
  { icon: MapPin, titleKey: "whyCard6Title", bodyKey: "whyCard6Body" }
] as const;

export function WhyCodeForge({ t }: { t: Dictionary }) {
  return (
    <section className="bg-bg">
      <div className="mx-auto w-full max-w-6xl ps-5 pe-5 py-16 lg:py-20">
        <Reveal className="flex flex-col gap-2 text-center">
          <h2 className="text-h2 text-text">{t.landing.whyHeading}</h2>
          <p className="mx-auto max-w-2xl text-body-lg text-text-secondary">
            {t.landing.whySubhead}
          </p>
        </Reveal>

        <div className="mt-10 grid grid-cols-1 gap-5 sm:grid-cols-2 lg:grid-cols-3">
          {CARDS.map((card, index) => {
            const Icon = card.icon;
            return (
              <Reveal key={card.titleKey} delayMs={(index % 3) * 80} className="h-full">
                {/* Hover-lift, DESIGN_LANGUAGE.md §2.4-honest: no shadow (cards
                    never get one) — raises via the token system's own
                    "hover/raised surface" (--surface-2) plus a border step-up
                    and a small translate. `transition-*` is neutralized by the
                    existing global prefers-reduced-motion rule (globals.css). */}
                <div className="flex h-full flex-col gap-3 rounded-card border border-border bg-surface p-5 transition-[background-color,border-color,transform] duration-200 ease-out hover:-translate-y-0.5 hover:border-border-strong hover:bg-surface-2">
                  <Icon aria-hidden="true" className="size-6 shrink-0 text-accent" />
                  <h3 className="text-h3 text-text">{t.landing[card.titleKey]}</h3>
                  <p className="text-body text-text-secondary">{t.landing[card.bodyKey]}</p>
                </div>
              </Reveal>
            );
          })}
        </div>
      </div>
    </section>
  );
}
