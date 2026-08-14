import { clsx, type ClassValue } from "clsx";
import { extendTailwindMerge } from "tailwind-merge";

// The theme in app/globals.css replaces Tailwind's default palette and type
// scale with the DESIGN_LANGUAGE.md §2 tokens. tailwind-merge can't infer
// those, and without the declarations below it can't tell `text-body` (a size)
// from `text-text` (a colour) — so one would silently drop the other whenever a
// caller passes a className that overlaps a primitive's base classes.
const TEXT_SIZES = [
  "display",
  "h1",
  "h2",
  "h3",
  "body-lg",
  "body",
  "label",
  "meta",
  "eyebrow",
  "code"
] as const;

const COLORS = [
  "bg",
  "surface",
  "surface-2",
  "border",
  "border-strong",
  "text",
  "text-secondary",
  "text-muted",
  "accent",
  "accent-hover",
  "accent-ink",
  "accent-text",
  "success",
  "success-border",
  "success-soft",
  "warning",
  "warning-border",
  "warning-soft",
  "danger",
  "danger-border",
  "danger-soft",
  "danger-hover",
  "danger-ink",
  "scrim"
] as const;

const RADII = ["control", "card", "pill"] as const;

const twMerge = extendTailwindMerge({
  extend: {
    classGroups: {
      "font-size": [{ text: [...TEXT_SIZES] }],
      "text-color": [{ text: [...COLORS] }],
      "bg-color": [{ bg: [...COLORS] }],
      "border-color": [{ border: [...COLORS] }],
      "ring-color": [{ ring: [...COLORS] }],
      rounded: [{ rounded: [...RADII] }],
      shadow: [{ shadow: ["overlay"] }]
    }
  }
});

export function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs));
}
