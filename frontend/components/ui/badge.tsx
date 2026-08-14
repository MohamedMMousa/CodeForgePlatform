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
        neutral: "border-border bg-surface-2 text-text-muted",
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
