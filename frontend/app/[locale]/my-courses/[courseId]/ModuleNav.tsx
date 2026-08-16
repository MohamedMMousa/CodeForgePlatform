import type { MyCourseModule } from "@/lib/api";
import type { Dictionary } from "@/lib/i18n";

// A jump list over every module, not a filter — every module always renders
// in the content column below, so each module's own empty state stays
// visible where it occurs rather than hiding behind a click. Sits start-side
// via source order + logical padding, so it mirrors in RTL for free; no
// scroll-spy (a deliberate non-goal — see the plan's structure note).
type Props = {
  modules: MyCourseModule[];
  t: Dictionary["courseContent"];
};

export function ModuleNav({ modules, t }: Props) {
  if (modules.length === 0) return null;

  return (
    <nav aria-label={t.modules} className="flex w-56 shrink-0 flex-col gap-1">
      <h2 className="eyebrow px-3 text-text-muted">{t.modules}</h2>
      <ul className="flex flex-col gap-1">
        {modules.map((module) => (
          <li key={module.id}>
            <a
              href={`#module-${module.id}`}
              className="block rounded-control px-3 py-2 text-body !text-text-secondary hover:bg-surface-2 hover:!text-text focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent"
            >
              {module.title}
            </a>
          </li>
        ))}
      </ul>
    </nav>
  );
}
