import * as React from "react";
import { cva, type VariantProps } from "class-variance-authority";
import { Slot } from "radix-ui";

import { cn } from "@/lib/utils";

// DESIGN_LANGUAGE.md §3 — pill shape, `label`-size text, soft-tinted background
// with a matching border. The meanings are fixed across both modes, so a badge
// means the same thing on the dark catalog and the light gradebook:
//   success  open / seats available / passed / complete
//   warning  almost full / in progress / attention
//   neutral  awaiting next batch / not started / draft
//   danger   rejected / failed / revoked / cancelled
const badgeVariants = cva(
  [
    "inline-flex w-fit shrink-0 items-center justify-center gap-1",
    "rounded-pill border px-3 py-1 text-meta whitespace-nowrap",
    "focus-visible:ring-2 focus-visible:ring-accent focus-visible:ring-offset-2 focus-visible:ring-offset-bg",
    "[&>svg]:pointer-events-none [&>svg]:size-3"
  ],
  {
    variants: {
      variant: {
        // --text-secondary, not --text-muted. On the light lane the neutral
        // badge is the one place a muted grey lands on --surface-2 rather than
        // on a page/card background: #6B7280 on #F3F4F6 is 4.39:1, under AA for
        // 12px text, with no large-text exemption available. --text-secondary
        // (#4B5563) takes the same pairing to 6.9:1. Dark is unaffected in kind
        // — #D1D5DB on #26334A was already passing and only gets lighter.
        neutral: "border-border bg-surface-2 text-text-secondary",
        success: "border-success-border bg-success-soft text-success",
        warning: "border-warning-border bg-warning-soft text-warning",
        danger: "border-danger-border bg-danger-soft text-danger"
      }
    },
    defaultVariants: {
      variant: "neutral"
    }
  }
);

function Badge({
  className,
  variant = "neutral",
  asChild = false,
  ...props
}: React.ComponentProps<"span"> &
  VariantProps<typeof badgeVariants> & { asChild?: boolean }) {
  const Comp = asChild ? Slot.Root : "span";

  return (
    <Comp
      data-slot="badge"
      data-variant={variant}
      className={cn(badgeVariants({ variant }), className)}
      {...props}
    />
  );
}

export { Badge, badgeVariants };
