"use client";

import { useEffect, useState } from "react";

const QUERY = "(prefers-reduced-motion: reduce)";

/**
 * Reactive `prefers-reduced-motion` for use in a component's *render* branch.
 *
 * Seeds `false` (motion allowed) so server render and the first client render
 * agree — matchMedia can't be read on the server — then corrects to the real
 * value in a mount effect and stays subscribed to `change`.
 *
 * NOTE: the motion primitives in this folder do NOT rely on this hook's async
 * state for their go/no-go decision. A primitive that must be correct on the
 * very first committed frame (CountUp, TypeOnce) reads
 * `prefersReducedMotion()` directly inside a `useLayoutEffect`, before paint.
 * This hook is for the reactive cases — a render that should re-run when the
 * user flips the OS setting mid-session.
 */
export function useReducedMotion(): boolean {
  const [reduced, setReduced] = useState(false);

  useEffect(() => {
    const mq = window.matchMedia(QUERY);
    setReduced(mq.matches);
    const onChange = () => setReduced(mq.matches);
    mq.addEventListener("change", onChange);
    return () => mq.removeEventListener("change", onChange);
  }, []);

  return reduced;
}

/**
 * Synchronous, one-shot read of the reduced-motion preference. Safe to call in
 * a layout effect on the client; returns `false` if `matchMedia` is somehow
 * unavailable (it never is in a browser, but guards against a non-DOM caller).
 */
export function prefersReducedMotion(): boolean {
  if (typeof window === "undefined" || typeof window.matchMedia !== "function") {
    return false;
  }
  return window.matchMedia(QUERY).matches;
}
