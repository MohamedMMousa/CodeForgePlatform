import * as React from "react";

import { cn } from "@/lib/utils";

// DESIGN_LANGUAGE.md §3 — --surface fill, --border hairline, radius 12,
// internal padding 16–20. Flat: §2.4 reserves shadows for floating layers, so a
// card never gets one.
//
// The anatomy the course card in §3 expects maps onto these slots:
//   CardHeader   eyebrow (level · track) + status badge, then the h3 title
//   CardContent  one-line muted description, then the meta rows
//   CardFooter   price in accent + the action button
function Card({
  className,
  size = "default",
  ...props
}: React.ComponentProps<"div"> & { size?: "default" | "sm" }) {
  return (
    <div
      data-slot="card"
      data-size={size}
      className={cn(
        "group/card flex flex-col overflow-hidden rounded-card border border-border bg-surface text-text",
        "gap-4 py-5 [--card-px:--spacing(5)]",
        "data-[size=sm]:gap-3 data-[size=sm]:py-4 data-[size=sm]:[--card-px:--spacing(4)]",
        className
      )}
      {...props}
    />
  );
}

function CardHeader({ className, ...props }: React.ComponentProps<"div">) {
  return (
    <div
      data-slot="card-header"
      className={cn(
        // `[display:grid]`, not the `grid` utility — no functional need to
        // change this now that the collision is resolved. The legacy
        // `.container`/`.grid`/`.table` classes in globals.css were renamed to
        // `.cf-container`/`.cf-grid`/`.cf-table` (see globals.css §6), so the
        // real Tailwind `grid` utility is safe to use here; this arbitrary
        // property is simply what was already here before that fix landed.
        "@container/card-header [display:grid] auto-rows-min items-start gap-2 px-(--card-px)",
        // `minmax(0,1fr)`, not `1fr`. A bare `1fr` is `minmax(auto,1fr)`, whose
        // auto floor is the eyebrow/title's min-content — so a long eyebrow
        // refuses to shrink and pushes the action column past the card, where
        // Card's overflow-hidden silently clips it. The status badge is the
        // thing that gets eaten, and only in whichever language has the longer
        // strings. Letting this track collapse to 0 means the auto track always
        // gets the badge's full width, and the eyebrow wraps instead.
        "has-data-[slot=card-action]:grid-cols-[minmax(0,1fr)_auto]",
        // Grid items default to min-width:auto, which would re-introduce the
        // same floor inside the track. Everything except the action needs that
        // floor removed so it can wrap — the action must keep it, or its own
        // `auto` track collapses below the badge and the badge overflows it.
        "[&>*:not([data-slot=card-action])]:min-w-0",
        className
      )}
      {...props}
    />
  );
}

// Real heading element, not a styled div — a screen reader's heading-navigation
// command must be able to jump card-to-card. Defaults to h3 (the level §3's
// course-card anatomy specifies); pass `as` to fit a different outline.
function CardTitle({
  className,
  as: Comp = "h3",
  ...props
}: React.ComponentProps<"h3"> & { as?: "h1" | "h2" | "h3" | "h4" | "h5" | "h6" }) {
  return (
    <Comp
      data-slot="card-title"
      className={cn("text-h3 text-text", className)}
      {...props}
    />
  );
}

function CardDescription({ className, ...props }: React.ComponentProps<"div">) {
  return (
    <div
      data-slot="card-description"
      className={cn("text-body text-text-muted", className)}
      {...props}
    />
  );
}

// Sits in the header's end column — `justify-self-end` is logical, so it lands
// on the right in English and the left in Arabic without an override.
function CardAction({ className, ...props }: React.ComponentProps<"div">) {
  return (
    <div
      data-slot="card-action"
      className={cn(
        "col-start-2 row-span-2 row-start-1 self-start justify-self-end",
        className
      )}
      {...props}
    />
  );
}

function CardContent({ className, ...props }: React.ComponentProps<"div">) {
  return (
    <div
      data-slot="card-content"
      className={cn("px-(--card-px) text-body", className)}
      {...props}
    />
  );
}

function CardFooter({ className, ...props }: React.ComponentProps<"div">) {
  return (
    <div
      data-slot="card-footer"
      className={cn(
        "flex items-center gap-3 px-(--card-px) pt-4 border-t border-border",
        className
      )}
      {...props}
    />
  );
}

export {
  Card,
  CardHeader,
  CardFooter,
  CardTitle,
  CardAction,
  CardDescription,
  CardContent
};
