"use client";

import * as React from "react";
import { useEffect, useRef, useState } from "react";
import { cn } from "@/lib/utils";
import { prefersReducedMotion } from "./useReducedMotion";

/**
 * Fades + rises its children in **once** when they first scroll into view, then
 * unobserves — it never re-triggers on re-scroll (DESIGN_LANGUAGE motion rule:
 * everything plays once and settles).
 *
 * Reduced-motion / no-JS safety is layered so content is never hidden from a
 * reader who can't see the animation:
 *  1. `motion-reduce:*` utilities force the settled state (visible, no offset,
 *     no transition) purely in CSS whenever the OS asks for reduced motion —
 *     this holds even if JS never runs, satisfying "content just present".
 *  2. The observer effect also reads `prefersReducedMotion()` directly and, if
 *     set, reveals immediately without ever constructing an observer.
 *
 * Accepted edge: a motion-*preferring* visitor with JS disabled sees the
 * pre-reveal hidden state (opacity-0). That's the standard IntersectionObserver
 * reveal tradeoff — reveal is a JS enhancement — and is acceptably rare.
 */
export function Reveal({
  children,
  className,
  as: Tag = "div",
  delayMs = 0
}: {
  children: React.ReactNode;
  className?: string;
  /** Element to render. Defaults to a div. */
  as?: React.ElementType;
  /** Optional stagger when several Reveals share a trigger region. */
  delayMs?: number;
}) {
  const ref = useRef<HTMLElement | null>(null);
  const [revealed, setRevealed] = useState(false);

  useEffect(() => {
    if (prefersReducedMotion() || typeof IntersectionObserver === "undefined") {
      // Reduced motion, or a browser without IO: skip straight to the settled
      // state so content is never trapped behind an observer that won't run.
      setRevealed(true);
      return;
    }
    const el = ref.current;
    if (!el) return;

    const observer = new IntersectionObserver(
      (entries) => {
        for (const entry of entries) {
          if (entry.isIntersecting) {
            setRevealed(true);
            observer.unobserve(entry.target);
          }
        }
      },
      // Trigger a touch before the element is fully on screen so the rise
      // reads as the section arriving, not catching up after it's already there.
      { threshold: 0.15, rootMargin: "0px 0px -8% 0px" }
    );
    observer.observe(el);
    return () => observer.disconnect();
  }, []);

  return (
    <Tag
      ref={ref}
      style={delayMs ? { transitionDelay: `${delayMs}ms` } : undefined}
      className={cn(
        "transition-[opacity,transform] duration-[450ms] ease-out will-change-[opacity,transform]",
        revealed ? "opacity-100 translate-y-0" : "opacity-0 translate-y-3",
        // CSS backstop: reduced motion → settled state regardless of JS.
        "motion-reduce:opacity-100 motion-reduce:translate-y-0 motion-reduce:transition-none",
        className
      )}
    >
      {children}
    </Tag>
  );
}
