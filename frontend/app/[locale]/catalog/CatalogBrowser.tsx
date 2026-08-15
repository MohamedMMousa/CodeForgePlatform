"use client";

import { useEffect, useMemo, useRef, useState } from "react";
import { Input } from "@/components/ui/input";
import { Button } from "@/components/ui/button";
import type { CourseListItem } from "@/lib/api";
import { format, getCategoryLabel, type Dictionary, type Locale } from "@/lib/i18n";
import { cn } from "@/lib/utils";
import { CourseCard } from "./CourseCard";
import { formatCatalogNumber, normalizeSearchText } from "./format";

const SEARCH_PARAM = "q";
const CATEGORY_PARAM = "category";

// Owns the category chips, the search box, and the filtered grid — all
// client-side over the one already-fetched course array (page.tsx makes a
// single list call; no card and no filter here ever hits the API again).
export function CatalogBrowser({
  courses,
  locale,
  t
}: {
  courses: CourseListItem[];
  locale: Locale;
  t: Dictionary;
}) {
  const categories = useMemo(() => {
    const set = new Set<string>();
    for (const course of courses) {
      if (course.category) set.add(course.category);
    }
    // Sort by the localized label a reader actually sees, not the raw
    // (always-English) value — otherwise the Arabic chip row would order
    // itself by English alphabetization underneath translated labels.
    return Array.from(set).sort((a, b) =>
      getCategoryLabel(a, locale).localeCompare(getCategoryLabel(b, locale), locale)
    );
  }, [courses, locale]);

  const [activeCategory, setActiveCategory] = useState<string | null>(null);
  const [search, setSearch] = useState("");

  // Restores filter state from the URL once, after mount. Not a lazy
  // useState initializer: window.location doesn't exist during SSR, and
  // reading it there would make the first client render diverge from the
  // server-rendered HTML.
  const restoredFromUrl = useRef(false);
  useEffect(() => {
    const params = new URLSearchParams(window.location.search);
    const q = params.get(SEARCH_PARAM);
    const category = params.get(CATEGORY_PARAM);
    if (q) setSearch(q);
    if (category) setActiveCategory(category);
    restoredFromUrl.current = true;
  }, []);

  // Reflects filter state into the URL bar via the History API directly —
  // never Next.js's router, which would re-fetch this route's RSC payload
  // (and re-hit the API) on every keystroke. replaceState, not pushState,
  // so typing doesn't spam browser history. Gated on the mount-time restore
  // above having already run, or this would fire first with the empty
  // initial state and overwrite a shared link's query string before it had
  // a chance to be read back.
  useEffect(() => {
    if (!restoredFromUrl.current) return;
    const params = new URLSearchParams(window.location.search);
    if (search) params.set(SEARCH_PARAM, search);
    else params.delete(SEARCH_PARAM);
    if (activeCategory) params.set(CATEGORY_PARAM, activeCategory);
    else params.delete(CATEGORY_PARAM);
    const qs = params.toString();
    window.history.replaceState(
      null,
      "",
      `${window.location.pathname}${qs ? `?${qs}` : ""}`
    );
  }, [search, activeCategory]);

  const filtered = useMemo(() => {
    const query = normalizeSearchText(search);
    return courses.filter((course) => {
      if (activeCategory && course.category !== activeCategory) return false;
      if (!query) return true;
      const haystack = normalizeSearchText(`${course.title} ${course.description ?? ""}`);
      return haystack.includes(query);
    });
  }, [courses, activeCategory, search]);

  const noCategories = categories.length === 0;
  const hasActiveFilter = activeCategory !== null || search.trim().length > 0;

  function clearFilters() {
    setSearch("");
    setActiveCategory(null);
  }

  return (
    <div className="flex flex-col gap-6">
      <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
        <div
          className="flex flex-wrap gap-2"
          role="group"
          aria-label={t.catalog.allCategories}
        >
          <CategoryChip
            label={t.catalog.allCategories}
            active={activeCategory === null}
            disabled={noCategories}
            onClick={() => setActiveCategory(null)}
          />
          {categories.map((category) => (
            <CategoryChip
              key={category}
              label={getCategoryLabel(category, locale)}
              active={activeCategory === category}
              onClick={() => setActiveCategory(category)}
            />
          ))}
        </div>
        <Input
          type="search"
          value={search}
          onChange={(event) => setSearch(event.target.value)}
          placeholder={t.catalog.searchPlaceholder}
          aria-label={t.catalog.searchPlaceholder}
          className="sm:max-w-xs"
        />
      </div>

      {/* Flagged, not hidden: a course list with no category data anywhere
          means the chip row can't do anything, so it's disabled and explained
          rather than silently dropped. */}
      {noCategories ? (
        <p className="text-meta text-text-muted">{t.catalog.categoriesUnavailable}</p>
      ) : null}

      {courses.length === 0 ? (
        <p className="text-body text-text-muted">{t.catalog.empty}</p>
      ) : filtered.length === 0 ? (
        <div className="flex flex-col items-start gap-3">
          <p className="text-body text-text-muted">{t.catalog.noSearchResults}</p>
          {hasActiveFilter ? (
            <Button variant="ghost" size="sm" onClick={clearFilters}>
              {t.catalog.clearFilters}
            </Button>
          ) : null}
        </div>
      ) : (
        <>
          {hasActiveFilter ? (
            <p className="text-meta text-text-muted">
              {format(t.catalog.filterResultCount, {
                count: formatCatalogNumber(filtered.length, locale),
                total: formatCatalogNumber(courses.length, locale)
              })}
            </p>
          ) : null}
          <div className="grid grid-cols-1 gap-5 sm:grid-cols-2 lg:grid-cols-3">
            {filtered.map((course) => (
              <CourseCard key={course.id} course={course} locale={locale} t={t} />
            ))}
          </div>
        </>
      )}
    </div>
  );
}

function CategoryChip({
  label,
  active,
  disabled,
  onClick
}: {
  label: string;
  active: boolean;
  disabled?: boolean;
  onClick: () => void;
}) {
  return (
    <button
      type="button"
      onClick={onClick}
      disabled={disabled}
      aria-pressed={active}
      className={cn(
        "rounded-pill border px-3 py-1 text-label transition-colors",
        "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent focus-visible:ring-offset-2 focus-visible:ring-offset-bg",
        active
          ? "border-accent bg-accent text-accent-ink font-semibold"
          : "border-border-strong bg-transparent text-text-secondary hover:bg-surface-2",
        disabled && "cursor-not-allowed opacity-50"
      )}
    >
      {label}
    </button>
  );
}
