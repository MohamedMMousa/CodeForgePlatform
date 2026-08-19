"use client";

import { useLayoutEffect, useState } from "react";
import { formatCatalogNumber } from "@/lib/format";
import type { Locale } from "@/lib/i18n";
import { prefersReducedMotion } from "./useReducedMotion";

const DURATION_MS = 800;

// Ease-out cubic: fast start, gentle settle — reads as arriving, not launching.
function easeOut(t: number): number {
  return 1 - Math.pow(1 - t, 3);
}

/**
 * Counts a number up from 0 to `value` **once**, then rests. Formats every
 * frame with `formatCatalogNumber` so Arabic keeps Western digits (§2.1).
 *
 * Reduced-motion / no-JS: `useState(value)` seeds the **final** number, so the
 * server render, the first client render, and a JS-disabled client all show the
 * real value with no hydration mismatch. Only after mount, in a *layout* effect
 * (before paint, so there's no final→0 flash), does it reset to 0 and ramp up —
 * and only when motion is allowed and `start` is true.
 */
export function CountUp({
  value,
  locale,
  suffix,
  className,
  start = true
}: {
  value: number;
  locale: Locale;
  /** Rendered immediately after the number, e.g. "%". Not animated. */
  suffix?: string;
  className?: string;
  /** Gate the ramp on an external trigger (e.g. a parent Reveal). */
  start?: boolean;
}) {
  const [display, setDisplay] = useState(value);

  useLayoutEffect(() => {
    if (!start) return;
    // Leave the final value in place under reduced motion (and re-assert it in
    // case a prior run left a partial value).
    if (prefersReducedMotion()) {
      setDisplay(value);
      return;
    }
    // No permanent "has started" ref guard: it would let React Strict Mode's
    // dev-only mount→cleanup→remount cancel the first rAF and then block the
    // remount from restarting, freezing the number at 0. The effect deps
    // ([start, value]) already make this run once per real trigger.
    let raf = 0;
    // Anchor the start to the first *painted* frame, not to mount time. rAF is
    // paused while the tab is hidden/not compositing; anchoring at mount would
    // make a tab opened in the background skip straight to the final value on
    // focus (elapsed time already exceeds the duration) instead of animating.
    let t0: number | null = null;
    const tick = (now: number) => {
      if (t0 === null) t0 = now;
      const progress = Math.min((now - t0) / DURATION_MS, 1);
      setDisplay(Math.round(easeOut(progress) * value));
      if (progress < 1) raf = requestAnimationFrame(tick);
    };
    setDisplay(0);
    raf = requestAnimationFrame(tick);
    return () => cancelAnimationFrame(raf);
  }, [start, value]);

  return (
    <span className={className}>
      {formatCatalogNumber(display, locale)}
      {suffix}
    </span>
  );
}
