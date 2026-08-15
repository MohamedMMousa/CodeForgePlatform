---
name: CodeForge Academy
description: Live, cohort-based programming LMS for Egypt — bilingual Arabic/English, RTL-first, orange-disciplined.
colors:
  bg-dark: "#111827"
  surface-dark: "#1F2937"
  surface-2-dark: "#26334A"
  border-dark: "#2B3444"
  border-strong-dark: "#3A4557"
  text-dark: "#FFFFFF"
  text-secondary-dark: "#D1D5DB"
  text-muted-dark: "#9AA4B2"
  accent-text-dark: "#F97316"
  success-dark: "#34D399"
  success-border-dark: "#235C46"
  success-soft-dark: "rgba(52, 211, 153, 0.08)"
  warning-dark: "#FB923C"
  warning-border-dark: "#6B3D16"
  warning-soft-dark: "rgba(251, 146, 60, 0.08)"
  danger-dark: "#F87171"
  danger-border-dark: "#7A2E3D"
  danger-soft-dark: "rgba(248, 113, 113, 0.08)"
  bg-light: "#F7F7F8"
  surface-light: "#FFFFFF"
  surface-2-light: "#F3F4F6"
  border-light: "#E5E7EB"
  border-strong-light: "#D1D5DB"
  text-light: "#111827"
  text-secondary-light: "#4B5563"
  text-muted-light: "#6B7280"
  accent-text-light: "#C2560C"
  success-light: "#15803D"
  success-border-light: "#BBF7D0"
  success-soft-light: "#F0FDF4"
  warning-light: "#B45309"
  warning-border-light: "#FED7AA"
  warning-soft-light: "#FFF7ED"
  danger-light: "#B91C1C"
  danger-border-light: "#FECACA"
  danger-soft-light: "#FEF2F2"
  accent: "#F97316"
  accent-hover: "#EA6B0F"
  accent-ink: "#111827"
typography:
  display:
    fontFamily: "Cairo, sans-serif"
    fontSize: "38px"
    fontWeight: 700
    lineHeight: 1.2
  h1:
    fontFamily: "Cairo, sans-serif"
    fontSize: "28px"
    fontWeight: 700
    lineHeight: 1.25
  h2:
    fontFamily: "Cairo, sans-serif"
    fontSize: "22px"
    fontWeight: 700
    lineHeight: 1.3
  h3:
    fontFamily: "Cairo, sans-serif"
    fontSize: "18px"
    fontWeight: 600
    lineHeight: 1.4
  body-lg:
    fontFamily: "Cairo, sans-serif"
    fontSize: "16px"
    fontWeight: 400
    lineHeight: 1.7
  body:
    fontFamily: "Cairo, sans-serif"
    fontSize: "15px"
    fontWeight: 400
    lineHeight: 1.7
  label:
    fontFamily: "Cairo, sans-serif"
    fontSize: "13px"
    fontWeight: 600
    lineHeight: 1.5
  meta:
    fontFamily: "Cairo, sans-serif"
    fontSize: "12px"
    fontWeight: 600
    lineHeight: 1.4
  eyebrow:
    fontFamily: "Cairo, sans-serif"
    fontSize: "11px"
    fontWeight: 700
    lineHeight: 1.4
    letterSpacing: "0.06em"
  code:
    fontFamily: "JetBrains Mono, ui-monospace, monospace"
    fontSize: "13px"
    fontWeight: 500
    lineHeight: 1.6
rounded:
  control: "8px"
  card: "12px"
  pill: "999px"
spacing:
  "1": "4px"
  "2": "8px"
  "3": "12px"
  "4": "16px"
  "5": "20px"
  "6": "24px"
  "8": "32px"
  "10": "40px"
  "12": "48px"
  "16": "64px"
components:
  button-primary:
    backgroundColor: "{colors.accent}"
    textColor: "{colors.accent-ink}"
    rounded: "{rounded.control}"
    padding: "10px 20px"
  button-primary-hover:
    backgroundColor: "{colors.accent-hover}"
  button-secondary-dark:
    backgroundColor: "transparent"
    textColor: "{colors.text-dark}"
    rounded: "{rounded.control}"
  button-secondary-light:
    backgroundColor: "transparent"
    textColor: "{colors.text-light}"
    rounded: "{rounded.control}"
  button-ghost-dark:
    backgroundColor: "transparent"
    textColor: "{colors.accent-text-dark}"
    rounded: "{rounded.control}"
  button-ghost-light:
    backgroundColor: "transparent"
    textColor: "{colors.accent-text-light}"
    rounded: "{rounded.control}"
  card-dark:
    backgroundColor: "{colors.surface-dark}"
    rounded: "{rounded.card}"
  card-light:
    backgroundColor: "{colors.surface-light}"
    rounded: "{rounded.card}"
---

# Design System: CodeForge Academy

## Overview

**Creative North Star: "Confident but calm"** — `docs/DESIGN_LANGUAGE.md` §1's own opening
principle, not an invented metaphor. Orange is a disciplined accent, not a theme; type and
spacing do the heavy lifting. If a screen feels loud, remove orange before adding it — never
widen orange's role to compensate for a screen feeling flat or unfinished.

This is one brand doing two jobs on two surfaces, not one inconsistent theme. Dark
(`--bg` #111827 / `--surface` #1F2937) is the **shop window**: Landing, Catalog, Course detail
— the marketing/browse surfaces a first-time visitor sees before signing in. Light
(`--bg` #F7F7F8 / `--surface` #FFFFFF) is for **reading and work**: the student dashboard,
lesson view, assignments, gradebook, certificates, auth, and the admin console — everything
that happens after sign-in. The same tokens, components, and Cairo typeface run through both;
only the `data-theme` scope changes, per-surface, never per-viewer preference.

Arabic is not a translation layer bolted onto an English design. Every surface must look
equally intentional in Arabic (RTL) and English (LTR); if it only looks right in one
direction, it isn't done. States (empty, loading, "almost full," "awaiting next batch," "not
started") are designed, not afterthoughts — the CTA follows the state.

**Key Characteristics:**
- One disciplined accent color, four sanctioned uses only.
- Two surface lanes (dark shop-window, light reading/work), both deliberate.
- Cairo unifies Arabic and Latin type into one visual system, not two side-by-side ones.
- Every layout decision is logical-property-driven so RTL is a mirror, not a rebuild.
- Flat by default; elevation exists only for floating layers.

## Colors

The palette is small and functional on purpose: a near-black/white neutral ramp per surface
lane, one orange accent shared by both lanes, and three semantic status colors (success,
warning, danger) that also shift value per lane for contrast.

### Primary

- **Accent Orange** (`#F97316` / dark text: `#F97316`, light text: `#C2560C`): buttons, price,
  active state. The single accent in the whole system — see the Disciplined Accent Rule below.
  Hover/pressed state is `#EA6B0F` in both lanes. Ink placed *on* a solid orange fill is always
  `#111827` (`--accent-ink`), never white — see the Accent-on-Orange Rule.

### Neutral — Dark lane (shop window: Landing, Catalog, Course detail)

- **Background** (`#111827`): page background.
- **Surface** (`#1F2937`): cards, panels.
- **Surface Raised** (`#26334A`): hover / raised surface.
- **Border** (`#2B3444`) / **Border Strong** (`#3A4557`): hairline dividers and card borders /
  secondary-button outline and emphasis.
- **Text** (`#FFFFFF`) / **Text Secondary** (`#D1D5DB`) / **Text Muted** (`#9AA4B2`): primary,
  secondary, and meta/muted text.

### Neutral — Light lane (reading & work: dashboard, lessons, assignments, gradebook, auth, admin)

- **Background** (`#F7F7F8`): page background.
- **Surface** (`#FFFFFF`): cards, inputs, panels.
- **Surface Raised** (`#F3F4F6`): subtle fills, table zebra.
- **Border** (`#E5E7EB`) / **Border Strong** (`#D1D5DB`): hairlines and input borders /
  emphasis borders.
- **Text** (`#111827`) / **Text Secondary** (`#4B5563`) / **Text Muted** (`#6B7280`): primary,
  secondary, and meta/hint text.

### Status (semantic, same meaning in both lanes)

- **Success** — dark `#34D399` (border `#235C46`, soft `rgba(52,211,153,.08)`); light
  `#15803D` (bg `#F0FDF4`, border `#BBF7D0`). Open / seats available / passed / complete.
- **Warning** — dark `#FB923C` (border `#6B3D16`, soft `rgba(251,146,60,.08)`); light `#B45309`
  (bg `#FFF7ED`, border `#FED7AA`). Almost full / in progress / attention.
- **Danger** — dark `#F87171` (border `#7A2E3D`, soft `rgba(248,113,113,.08)`); light `#B91C1C`
  (bg `#FEF2F2`, border `#FECACA`). Rejected / failed / revoked / cancelled.

### Named Rules

**The Disciplined Accent Rule.** Orange is used in exactly four places: price, the primary
button fill, the active/selected state (e.g. an active filter chip), and the brand wordmark's
"Forge." Nowhere else. If a screen feels loud, remove orange before adding it — never expand
orange's footprint to fix a screen that feels flat.

**The Two Surfaces Rule.** Dark sells, light reads. Dark is the deliberate shop-window brand
surface (Landing, Catalog, Course detail); light is for reading and work (everything after
sign-in). This is a confirmed, permanent split by *surface*, never a per-viewer dark-mode
toggle — a light panel can sit inside a dark page by re-scoping `data-theme` on that subtree,
but the app has no global light/dark preference switch.

**The Accent-on-Orange Rule.** Primary buttons are orange fill with dark ink (`#111827`), never
white text, in both lanes — white-on-orange fails contrast and reads louder than intended. When
orange is used as *text* on a surface (ghost buttons), light mode darkens it to `#C2560C` to
hold contrast on white; dark mode keeps the full `#F97316` since it's already legible there.

## Typography

**Display Font:** Cairo (400/600/700), with `"Segoe UI", system-ui, -apple-system, Tahoma,
Arial, sans-serif` fallback.
**Body Font:** Cairo — the same family for every UI role in both scripts.
**Label/Mono Font:** JetBrains Mono, with `ui-monospace, monospace` fallback — code only.

**Character:** Cairo handles Arabic and Latin with shared visual DNA, so a bilingual page reads
as one design, not an English design with an Arabic translation bolted on. JetBrains Mono never
appears outside code (snippets, submissions, test output) and is always LTR, even inside an
RTL page.

### Hierarchy

- **Display** (700, 38px, 1.2 line-height): landing hero headline only.
- **Headline / H1** (700, 28px, 1.25): page title.
- **H2** (700, 22px, 1.3): section heading.
- **Title / H3** (600, 18px, 1.4): card title, subsection.
- **Body Large** (400, 16px, 1.7): lead paragraph.
- **Body** (400, 15px, 1.7 — 1.8 in Arabic for extra vertical breathing room): default body
  text.
- **Label** (600, 13px, 1.5): form labels, small UI text.
- **Meta** (600, 12px, 1.4): muted metadata, captions.
- **Eyebrow** (700, 11px, 1.4, letter-spacing 0.06em, uppercase): small section labels — Latin
  only. Arabic has no letter case, so an Arabic eyebrow uses normal-weight small text instead
  of faux-caps.
- **Code** (500, 13px, 1.6, JetBrains Mono): code only.

Numerals are Western digits (0–9) everywhere, **including all Arabic UI** — prices, dates,
counts, grades, session numbers. Never Eastern-Arabic numerals (٠–٩).

### Named Rules

**The Shared Type Rule.** Cairo is the UI typeface for both scripts, always. There is no
Arabic-specific or English-specific font swap anywhere in the UI layer.
**The Code-Stays-Code Rule.** JetBrains Mono renders code and only code. It is never used for
UI copy, and code itself never mirrors — it stays LTR and left-aligned even inside an RTL page.
**The Western Numerals Rule.** Every numeral in the product — including Arabic surfaces — uses
Western digits. No Eastern-Arabic numeral glyphs anywhere.

## Layout

Logical properties only: `ms-`/`me-`, `ps-`/`pe-`, `start`/`end`, `text-start`. Never hardcoded
`left`/`right` or `ml-`/`mr-` — this is what lets one codebase mirror correctly rather than
needing a parallel RTL layout. Direction is set at the layout root per locale (`dir="rtl"` for
Arabic, `dir="ltr"` for English).

Spacing scale (px, use these steps only): **4, 8, 12, 16, 20, 24, 32, 40, 48, 64.** Off-scale
spacing is a review finding, not a compile error.

Directional icons (arrows, chevrons, progress) flip with direction; non-directional icons
(calendar, video, user) do not. Code blocks stay LTR and left-aligned inside RTL pages, wrapped
so the surrounding layout doesn't reorder them.

### Named Rules

**The Logical-Only Rule.** No layout decision is ever expressed in `left`/`right` terms. Every
directional property is logical, so RTL is a mirror of LTR, not a second implementation.

## Elevation & Depth

Flat by default. Shadows exist only for floating layers — menus, dialogs, toasts — and stay
subtle even there. No decorative shadows on cards; a card's separation from its background
comes from its fill color and hairline border, not a drop shadow.

### Shadow Vocabulary

- **Overlay** (`box-shadow: 0 10px 30px rgba(0, 0, 0, 0.28)`): the one shadow value in the
  system, used only for floating layers (dialogs, menus, toasts).

### Named Rules

**The Flat-by-Default Rule.** Surfaces are flat at rest. The single overlay shadow exists only
for content that floats above the page — never for a card, panel, or anything in normal flow.

## Shapes

Radius is role-based, not size-based: **controls** (buttons, inputs, chips) `8px`;
**cards/panels** `12px`; **pills/badges** `999px` (fully rounded). Hairline borders are `0.5px`
on the dark lane and `1px` on the light lane, where `0.5px` would be too faint against a white
background.

## Components

### Buttons

Three levels, and never more than one primary button per screen.

- **Shape:** radius `8px` (`--radius-control`).
- **Primary:** `--accent` fill, `--accent-ink` text, weight 700 — the single most important
  action on the screen.
- **Secondary:** transparent fill, `--border-strong` outline, primary-text color, weight 600 —
  alternate/neutral actions.
- **Ghost:** text-only in `--accent` (dark) / `--accent-text` (light), weight 600 — low-emphasis
  links-as-actions.
- **Danger:** destructive actions use `--danger`. Inline destructive actions (a row/card-level
  "Delete") use an outline/ghost treatment; the final confirm inside a dialog uses a solid
  `--danger` fill with light ink. Danger only counts as the screen's one primary when the
  screen's main action is itself destructive.

### Badges

- **Style:** pill shape (`999px` radius), `label`-size text, soft-tinted background with a
  matching border.
- **Semantic meaning, fixed across both lanes:** success (green) = open / seats available /
  passed / complete. Warning (amber) = almost full / in progress / attention. Neutral (gray) =
  awaiting next batch / not started / draft.

### Cards / Containers

- **Corner Style:** `12px` radius.
- **Background:** `--surface` fill with a `--border` hairline — no shadow.
- **Internal Padding:** `16–20px`.
- **State drives the CTA:** a bookable cohort gets a primary "Enroll"; an unscheduled one gets
  a neutral badge + secondary "Notify me" — never a live orange button on something that can't
  be bought yet.
- **Course card anatomy:** eyebrow (level · track) + status badge on the top row → title (H3)
  → one-line muted description → meta rows (start date, sessions, schedule) with
  non-directional icons → footer (price in accent + action button).

### Inputs (light lane)

- **Style:** `--surface` fill, `--border` stroke, `8px` radius, label above in the `label`
  token, optional hint below in `meta`.
- **Focus:** a clear ring in `--accent`.
- **Error:** `--danger`.

### Tables / Gradebook (light lane)

- **Style:** uppercase `meta`-size column headers in `--text-muted`, hairline row separators
  (`--border`), generous row padding, numbers Western and end-aligned per direction. Optional
  `--surface-2` zebra striping.

### Navigation

- **Style:** wordmark ("Code" in primary text color, "Forge" in accent) leads on the start
  side; nav links in `--text-muted`; language toggle + Sign in on the end side; hairline bottom
  border. Mirrors fully in RTL — the wordmark, link order, and end-side controls all flip.

## Do's and Don'ts

### Do:
- **Do** use Cairo for every piece of UI text, in both Arabic and English — one shared typeface
  is what makes bilingual pages feel like one design.
- **Do** reserve JetBrains Mono strictly for code, and keep code LTR even inside an RTL page.
- **Do** treat the dark lane (`#111827` bg / `#1F2937` surface) as the deliberate shop-window
  surface for Landing, Catalog, and Course detail.
- **Do** use the light lane for every surface after sign-in: dashboard, lessons, assignments,
  gradebook, certificates, auth, admin.
- **Do** confine orange to exactly four uses: price, primary CTA fill, the active/selected
  state, and the brand wordmark's "Forge."
- **Do** use `--accent-ink` (`#111827`), never white, as the text/icon color on any solid
  orange fill.
- **Do** use logical properties (`ms-`/`me-`, `ps-`/`pe-`, `start`/`end`) for every directional
  layout decision.
- **Do** use Western digits (0–9) for every numeral shown anywhere, including Arabic UI.

### Don't:
- **Don't** suggest swapping Cairo or JetBrains Mono for a different typeface family — this is
  a confirmed, non-negotiable brand commitment, not an open design choice.
- **Don't** dilute, soften, tint, or expand the accent orange beyond price / primary CTA /
  active state / brand wordmark. If a screen feels loud, remove orange before adding it — never
  widen orange's role to compensate for a screen that feels flat or unfinished.
- **Don't** treat the dark Landing/Catalog/Course-detail surfaces as "dark mode" that needs a
  light toggle, a lighter default, or "fixing" toward the light palette — they are the
  deliberate shop-window brand surface, functionally distinct from the light reading/work
  surfaces, not an incomplete or inconsistent theme.
- **Don't** put white text on a solid orange fill — always `--accent-ink`.
- **Don't** hardcode `left`/`right`/`ml-`/`mr-` anywhere that must mirror in RTL.
- **Don't** use Eastern-Arabic numerals (٠–٩) anywhere, including Arabic UI.
- **Don't** add a decorative shadow to a card or any element in normal flow — shadows exist
  only for floating layers (menus, dialogs, toasts).
- **Don't** invent a level·track eyebrow split, a thumbnail image, or any course-card field
  the catalog list endpoint doesn't actually return — card anatomy is bound to real data only.
