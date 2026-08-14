import * as React from "react";
import { cva, type VariantProps } from "class-variance-authority";
import { Slot } from "radix-ui";

import { cn } from "@/lib/utils";

// DESIGN_LANGUAGE.md §3 — four levels, and one primary per screen:
//   primary      accent fill, accent-ink text (never white — white-on-orange
//                fails contrast and reads louder than the brand wants), 700
//   secondary    transparent fill, border-strong outline, primary text, 600
//   ghost        text-only in --accent-text, 600
//   danger       outline in --danger. The *inline* destructive action — the
//                Delete button sitting in a table row or a card footer.
//   danger-solid --danger fill with --danger-ink. Reserved for the final
//                confirm inside a dialog, per §3. Using it inline would make a
//                destructive action compete with the screen's real primary.
//
// Danger does not consume the screen's one primary slot unless the screen's
// main action is itself destructive (§3).
const buttonVariants = cva(
  [
    "inline-flex shrink-0 items-center justify-center gap-2 whitespace-nowrap",
    "rounded-control border border-transparent transition-colors outline-none select-none",
    // §5: a visible focus state on every interactive element. The ring colour
    // is per-variant so a destructive control doesn't focus in brand orange.
    "focus-visible:ring-2 focus-visible:ring-offset-2 focus-visible:ring-offset-bg",
    // Disabled is its own neutral treatment, not the enabled fill at reduced
    // opacity. Dimming the accent fill against a dark page composites orange
    // toward brown, which reads as a deliberate (and wrong) colour rather than
    // as "inert". These carry a pseudo-class, so they outrank the variant's own
    // colours regardless of class order.
    "disabled:pointer-events-none disabled:border-border disabled:bg-surface-2",
    "disabled:text-text-muted disabled:opacity-70",
    "[&_svg]:pointer-events-none [&_svg]:shrink-0"
  ],
  {
    variants: {
      variant: {
        primary:
          "bg-accent text-accent-ink font-bold hover:bg-accent-hover focus-visible:ring-accent",
        secondary:
          "border-border-strong bg-transparent text-text font-semibold hover:bg-surface-2 focus-visible:ring-accent",
        ghost:
          "bg-transparent text-accent-text font-semibold hover:bg-surface-2 focus-visible:ring-accent",
        danger:
          "border-danger bg-transparent text-danger font-semibold hover:bg-danger-soft focus-visible:ring-danger",
        "danger-solid":
          "bg-danger text-danger-ink font-bold hover:bg-danger-hover focus-visible:ring-danger"
      },
      size: {
        sm: "h-8 px-3 text-label",
        default: "h-10 px-5 text-body",
        lg: "h-12 px-6 text-body-lg",
        "icon-sm": "size-8 [&_svg]:size-4",
        icon: "size-10 [&_svg]:size-5"
      }
    },
    defaultVariants: {
      variant: "primary",
      size: "default"
    }
  }
);

function Button({
  className,
  variant = "primary",
  size = "default",
  asChild = false,
  ...props
}: React.ComponentProps<"button"> &
  VariantProps<typeof buttonVariants> & {
    asChild?: boolean;
  }) {
  const Comp = asChild ? Slot.Root : "button";

  return (
    <Comp
      data-slot="button"
      data-variant={variant}
      data-size={size}
      className={cn(buttonVariants({ variant, size, className }))}
      {...props}
    />
  );
}

export { Button, buttonVariants };
