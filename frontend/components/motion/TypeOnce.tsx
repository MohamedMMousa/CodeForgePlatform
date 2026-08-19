"use client";

import { useLayoutEffect, useState } from "react";
import { cn } from "@/lib/utils";
import { prefersReducedMotion } from "./useReducedMotion";

const MS_PER_CHAR = 18;

/**
 * Types `code` out once on mount, then **rests as static code** — it never
 * re-runs and never loops. A caret shows only while typing and is removed the
 * moment the last character lands (nothing keeps animating after the effect
 * settles).
 *
 * Reduced-motion / no-JS: `useState(code)` seeds the **full** snippet, so the
 * server render, the first client render, and a JS-disabled client all show the
 * complete code. Only after mount, in a *layout* effect (so the reset-to-empty
 * lands before paint — no full-code flash), and only when motion is allowed,
 * does it clear and type up to full length.
 *
 * Rendered in `<pre><code>`, which globals.css pins to `direction: ltr;
 * text-align: left` + JetBrains Mono — so the snippet stays LTR even on the
 * Arabic page (§2.1, same category as surface #6's `Textarea dir="ltr"`).
 */
export function TypeOnce({
  code,
  className,
  ariaLabel
}: {
  code: string;
  className?: string;
  /** Accessible label; the live typing is decorative, so the <pre> is aria-hidden. */
  ariaLabel?: string;
}) {
  const [shown, setShown] = useState(code);
  const [typing, setTyping] = useState(false);

  useLayoutEffect(() => {
    if (prefersReducedMotion()) return; // full code stays put

    setShown("");
    setTyping(true);
    let i = 0;
    const id = window.setInterval(() => {
      i += 1;
      setShown(code.slice(0, i));
      if (i >= code.length) {
        window.clearInterval(id);
        setTyping(false);
      }
    }, MS_PER_CHAR);
    return () => window.clearInterval(id);
  }, [code]);

  return (
    <pre
      aria-label={ariaLabel}
      aria-hidden={ariaLabel ? undefined : true}
      className={cn("text-code overflow-x-auto", className)}
    >
      <code>
        {shown}
        {typing && (
          // Caret exists only while typing; the blink is motion-safe so reduced
          // motion never sees it move. Removed entirely once typing completes.
          <span
            aria-hidden="true"
            className="inline-block w-[0.5ch] -mb-[0.1em] bg-accent motion-safe:animate-pulse"
            style={{ height: "1.1em" }}
          />
        )}
      </code>
    </pre>
  );
}
