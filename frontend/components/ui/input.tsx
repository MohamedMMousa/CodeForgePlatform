import * as React from "react";

import { cn } from "@/lib/utils";

// DESIGN_LANGUAGE.md §3 — --surface fill, --border, radius 8, a clear focus
// ring in --accent, and --danger for the error state. The label above and the
// hint below are the caller's job (`label` and `meta` tokens respectively);
// this is the control only.
function Input({ className, type, ...props }: React.ComponentProps<"input">) {
  return (
    <input
      type={type}
      data-slot="input"
      className={cn(
        "h-10 w-full min-w-0 rounded-control border border-border bg-surface px-3 py-2",
        "text-body text-text transition-colors outline-none",
        "placeholder:text-text-muted",
        "focus-visible:border-accent focus-visible:ring-2 focus-visible:ring-accent focus-visible:ring-offset-2 focus-visible:ring-offset-bg",
        "disabled:pointer-events-none disabled:opacity-55",
        "aria-invalid:border-danger aria-invalid:focus-visible:ring-danger",
        "file:me-3 file:rounded-control file:border-0 file:bg-surface-2 file:px-3 file:py-1 file:text-label file:text-text",
        className
      )}
      {...props}
    />
  );
}

export { Input };
