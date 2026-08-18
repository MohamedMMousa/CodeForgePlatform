import * as React from "react";

import { cn } from "@/lib/utils";

// Same token language as input.tsx (§3), swapping the fixed control height
// for a resizable min-height and the body type scale for `font-mono
// text-code` (§2.1) — this is a long-form/code control, not a single-line
// field. `dir` is an ordinary passthrough, not defaulted here: most
// textareas in the app (contact form, admin/instructor free text) must stay
// direction-neutral. Callers that render code pass `dir="ltr"` explicitly.
function Textarea({ className, ...props }: React.ComponentProps<"textarea">) {
  return (
    <textarea
      data-slot="textarea"
      className={cn(
        "min-h-48 w-full min-w-0 resize-y rounded-control border border-border bg-surface px-3 py-2",
        "text-code font-mono text-text transition-colors outline-none",
        "placeholder:text-text-muted",
        "focus-visible:border-accent focus-visible:ring-2 focus-visible:ring-accent focus-visible:ring-offset-2 focus-visible:ring-offset-bg",
        "disabled:pointer-events-none disabled:opacity-55",
        "aria-invalid:border-danger aria-invalid:focus-visible:ring-danger",
        className
      )}
      {...props}
    />
  );
}

export { Textarea };
