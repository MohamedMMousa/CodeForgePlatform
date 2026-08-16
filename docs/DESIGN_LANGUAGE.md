# CodeForge Academy — Design Language

**Status:** Source of truth for the frontend design pass. Read this at the start of any design or build session, alongside `ARCHITECTURE.md` and `API_CONVENTIONS.md`. When this doc and the running code disagree, this doc wins — update it deliberately, don't let it drift.

**Scope:** How every CodeForge surface should look and behave visually. It defines tokens, type, components, and per-surface intent. It does **not** re-specify functionality — that lives in the architecture docs.

---

## 1. Principles

1. **Confident but calm.** Orange is a disciplined accent, not a theme. Type and spacing do the heavy lifting. If a screen feels loud, remove orange before adding it.
2. **RTL-first, bilingual parity.** Arabic is not a translation layer bolted onto an English design. Every surface must look equally intentional in Arabic (RTL) and English (LTR). If it only looks right in one direction, it isn't done.
3. **Dark sells, light reads.** Dark surfaces for the "shop window" (marketing/browse). Light surfaces for reading and work (inside the course). Same brand, two jobs.
4. **States are designed, not afterthoughts.** Empty, loading, "almost full", "awaiting next batch", "not started" — each gets a deliberate treatment. The CTA follows the state.
5. **One system, reused everywhere.** No page gets hand-styled off-token. Every color, size, and radius comes from the tokens below.

---

## 2. Foundations

### 2.1 Typography

- **UI typeface (both scripts):** **Cairo** — weights 400 / 600 / 700. Handles Arabic and Latin with shared visual DNA, so bilingual pages feel like one design.
- **Code typeface:** **JetBrains Mono** (fallback `ui-monospace, monospace`). Used only for code (snippets, submissions, test output). Code is **always LTR**, even inside an RTL page.
- **Numerals:** **Western digits (0–9) everywhere**, including all Arabic UI — prices, dates, counts, grades, session numbers. Never Eastern-Arabic numerals (٠–٩).

**Type scale** (name · size / line-height / weight · usage):

| Token | Size / LH / Weight | Usage |
|---|---|---|
| `display` | 38px / 1.2 / 700 | Landing hero headline only |
| `h1` | 28px / 1.25 / 700 | Page title |
| `h2` | 22px / 1.3 / 700 | Section heading |
| `h3` | 18px / 1.4 / 600 | Card title, subsection |
| `body-lg` | 16px / 1.7 / 400 | Lead paragraph |
| `body` | 15px / 1.7 (Arabic 1.8) / 400 | Default body text |
| `label` | 13px / 1.5 / 600 | Form labels, small UI text |
| `meta` | 12px / 1.4 / 600 | Muted metadata, captions |
| `eyebrow` | 11px / 700 / uppercase, letter-spacing .06em | Small Latin section labels |
| `code` | 13–14px / 1.6 / 500 (JetBrains Mono) | Code only |

**Arabic type notes:** bump body line-height to ~1.8 (Arabic needs more vertical breathing room). The **`eyebrow` uppercase treatment is Latin-only** — Arabic has no letter case, so Arabic eyebrows use normal-weight small text, not faux-caps. The **brand wordmark "CodeForge" stays in Latin** in both languages (it's a brand name, not UI copy).

### 2.2 Color — Dark (shop window: landing, catalog, course detail)

| Token | Hex | Usage |
|---|---|---|
| `--bg` | `#111827` | Page background |
| `--surface` | `#1F2937` | Cards, panels |
| `--surface-2` | `#26334A` | Hover / raised surface |
| `--border` | `#2B3444` | Hairline dividers, card borders |
| `--border-strong` | `#3A4557` | Secondary-button outline, emphasis |
| `--text` | `#FFFFFF` | Primary text |
| `--text-secondary` | `#D1D5DB` | Secondary text |
| `--text-muted` | `#9AA4B2` | Meta, muted labels |
| `--accent` | `#F97316` | Primary accent (buttons, price, active) |
| `--accent-hover` | `#EA6B0F` | Accent hover/pressed |
| `--accent-ink` | `#111827` | Text/icon on top of orange fills |
| `--accent-text` | `#F97316` | Orange as text (ghost buttons); mirrors light so ghost references one token in both modes |
| `--success` | `#34D399` | text · border `#235C46` · soft `rgba(52,211,153,.08)` |
| `--warning` | `#FB923C` | text · border `#6B3D16` · soft `rgba(251,146,60,.08)` |
| `--danger` | `#F87171` | text · border `#7A2E3D` · soft `rgba(248,113,113,.08)` |

### 2.3 Color — Light (reading & work: lessons, materials, assignments, gradebook, forms)

| Token | Hex | Usage |
|---|---|---|
| `--bg` | `#F7F7F8` | Page background |
| `--surface` | `#FFFFFF` | Cards, inputs, panels |
| `--surface-2` | `#F3F4F6` | Subtle fills, table zebra |
| `--border` | `#E5E7EB` | Hairlines, input borders |
| `--border-strong` | `#D1D5DB` | Emphasis borders |
| `--text` | `#111827` | Primary text |
| `--text-secondary` | `#4B5563` | Secondary text |
| `--text-muted` | `#6B7280` | Meta, hints |
| `--accent` | `#F97316` | Primary accent |
| `--accent-hover` | `#EA6B0F` | Accent hover/pressed |
| `--accent-ink` | `#111827` | Text/icon on orange fills |
| `--accent-text` | `#C2560C` | Orange **as text** on white (passes contrast) |
| `--success` | `#15803D` | bg `#F0FDF4` · border `#BBF7D0` |
| `--warning` | `#B45309` | bg `#FFF7ED` · border `#FED7AA` |
| `--danger` | `#B91C1C` | bg `#FEF2F2` · border `#FECACA` |

**Accent-on-orange rule:** primary buttons are orange fill with **dark ink (`--accent-ink`)**, not white text, in both modes — white-on-orange fails contrast and reads louder than we want. When orange is used as *text* on a white surface, use `--accent-text` (`#C2560C`), not `--accent`.

**Light-lane accent-on-background rule (found building surface #4):** `--accent-text` is only AA-safe on `--surface` (white, 4.54:1). On the light `--bg` (`#F7F7F8`) it drops to ~4.24:1 and fails. Orange-as-text must sit inside a card, never directly on the light page background. The dark lane has no equivalent restriction — `--accent-text` equals `--accent` there and is legible on `--bg` regardless.

### 2.4 Spacing, radius, elevation

- **Spacing scale (px):** 4, 8, 12, 16, 20, 24, 32, 40, 48, 64. Use these steps only.
- **Radius:** controls (buttons, inputs, chips) `8px`; cards/panels `12px`; pills/badges `999px`.
- **Borders:** hairline `0.5px` on dark, `1px` on light where 0.5px is too faint.
- **Elevation:** flat by default. Shadows only for floating layers (menus, dialogs, toasts), and only subtle. No decorative shadows on cards.

### 2.5 RTL & bilingual rules (non-negotiable)

- **Logical properties only.** Use `ms-/me-`, `ps-/pe-`, `start/end`, `text-start`. Never hardcode `left`/`right`, `ml-`/`mr-`. This is what makes one codebase mirror correctly.
- **Direction is set at the layout root** per locale (`dir="rtl"` for `ar`, `dir="ltr"` for `en`) under `app/[locale]/`.
- **Directional icons flip** with direction (arrows, chevrons, progress). Non-directional icons (calendar, video, user) do not.
- **Code blocks stay LTR** and left-aligned inside RTL pages; wrap them so the surrounding RTL layout doesn't reorder them.
- **Every user-facing string ships in both `en` and `ar`** in the same change. A surface with English-only strings is incomplete.

---

## 3. Components

One primary button per screen, maximum. Everything below reads from tokens.

- **Buttons — three levels.**
  - *Primary:* `--accent` fill, `--accent-ink` text, weight 700, radius 8. The single most important action on a screen.
  - *Secondary:* transparent fill, `--border-strong` outline, primary-text color, weight 600. Alternate/neutral actions.
  - *Ghost:* text-only in `--accent` (dark) / `--accent-text` (light), weight 600. Low-emphasis links-as-actions.
  - *Danger:* destructive actions (delete, deactivate, revoke). Uses `--danger`.
    Inline destructive actions use an outline/ghost in `--danger`; the final confirm
    in a dialog uses a solid `--danger` fill with light ink. Danger doesn't count as
    the screen's one primary unless the screen's main action is itself destructive.
- **Badges / status — semantic, consistent meanings across both modes.**
  - *Success (green):* open / seats available / passed / complete.
  - *Warning (amber):* almost full / in progress / attention.
  - *Neutral (gray):* awaiting next batch / not started / draft.
  - Pill shape, `label`-size text, soft-tinted background + matching border.
- **Cards.** `--surface` fill, `--border`, radius 12, internal padding 16–20. **State drives the CTA:** a bookable cohort shows a primary "Enroll"; an unscheduled one shows a neutral badge + secondary "Notify me" — never a live orange button on something you can't buy yet.
- **Course card anatomy:** eyebrow (level · track) + status badge on top row → title (`h3`) → one-line description (muted) → meta rows (start date, sessions, schedule) with non-directional icons → footer (price in accent + action button).
- **Inputs (light).** `--surface` fill, `--border`, radius 8, label above in `label` token, optional hint below in `meta`. Clear focus ring in `--accent`. Error state uses `--danger`.
- **Tables / gradebook (light).** Uppercase `meta` column headers in `--text-muted`, hairline row separators (`--border`), generous row padding, numbers Western and end-aligned per direction. Optional `--surface-2` zebra.
- **Nav.** Wordmark (Code white / Forge orange) leads on the start side; links in `--text-muted`; language toggle + Sign in on the end side; hairline bottom border. Mirrors fully in RTL.

---

## 4. Surface plans

Ordered by build priority. "Mode" = which surface family it belongs to.

| # | Surface | Mode | Intent |
|---|---|---|---|
| 1 | **Landing** | Dark | Hero (approved): pill (next cohort) → headline → subhead → primary "Browse cohorts" + secondary "How it works" → trust row. Solves the first-time-visitor dead-end. Below hero: how-it-works (the enrollment model, see note), featured cohorts, certificate/verify strip. |
| 2 | **Catalog** | Dark | Filter chips (track/level) + calm card grid. Card state drives CTA. |
| 3 | **Course detail** | Dark | Shop-window sell page: overview, syllabus/modules, instructor, cohort + price, enroll. Last dark surface before sign-in. |
| 4 | **Student dashboard** | Light | The home after sign-in: enrolled cohorts, next live session, progress, pending tasks. Calm, scannable. |
| 5 | **Course content / sessions** | Light | The enrolled student's content space: module nav (start-side), session list where the session **type** (live / recorded / in-person) drives the treatment, materials, and a session detail view. |
| 6 | **Assignments** | Light | Brief + code submission (JetBrains Mono, LTR) + status badge + auto-grade result when available. |
| 7 | **Gradebook** | Light | Table component: attendance + scores, Western numerals, clear pass/progress badges. |
| 8 | **Certificates / verify** | Light | Two-tier certificate view + public verify page. Trust-forward, clean. |
| 9 | **Auth (sign in / enrollment request)** | Light | **Encode the real enrollment model in the UI:** anonymous enrollment request → admin approves → account auto-created + temp password emailed. First-time users need a request path, not just a sign-in box. |
| 10 | **Admin console** | Light | Functional-first; apply tokens for consistency but lowest polish priority. Dense tables, forms, and the enrollment-request queue. |

**Enrollment-model note (product gap already identified):** nothing in the current UI explains that students *request* enrollment and get approved. Surfaces 1, 3, and 9 must make this path obvious to a first-time visitor.

---

## 5. Bilingual & accessibility QA checklist

Run per surface before calling it done:

- [ ] Renders correctly in **both `en` (LTR) and `ar` (RTL)** — layout mirrors, nothing overlaps or clips.
- [ ] **All strings present in `en` and `ar`.**
- [ ] **Western numerals** everywhere, including Arabic.
- [ ] No hardcoded `left`/`right` — logical properties only.
- [ ] Directional icons flip; non-directional icons don't.
- [ ] Code stays LTR inside RTL.
- [ ] Text contrast passes AA against its surface (esp. muted text and orange usage).
- [ ] Visible focus state on every interactive element.
- [ ] Every state has a treatment: empty, loading, error, "almost full", "awaiting", "not started".
- [ ] One primary button per screen.

---

## 6. Build sequencing & the Tailwind + shadcn adoption

**Decided: adopt Tailwind + shadcn/ui now, in this pass** — it's the locked target regardless, so build the foundation once instead of retrofitting later. It also directly serves the spec: accessible primitives with real focus states (§5), native logical-property support for RTL (§2.5), and CSS-variable theming that makes the light/dark toggle near-free.

**Sequence:**

1. **Fonts + tokens first.** Load Cairo (400/600/700) and JetBrains Mono; implement §2 as CSS custom properties (dark + light) and expose them in the Tailwind theme, so every component reads from tokens. Set up the light/dark switching mechanism (root class or data-attribute) now — the toggle UI can come later, both value sets already exist.
2. **Install and re-skin shadcn.** Add only the primitives needed (button, badge, card, input, select, table, dialog) and restyle each to these tokens **before** use, so nothing ships with library defaults.
3. **Then apply surface by surface**, in the priority order of §4, running the §5 checklist each time.

**Guardrails (non-negotiable):** (a) logical properties from line one — no `left`/`right`/`ml`/`mr`; (b) restyle every shadcn primitive to §2 tokens; (c) verify each Radix-based component (dropdown, popover, dialog) actually mirrors in RTL as it's adopted.

**Status:** steps 1 and 2 are done — see `IMPLEMENTATION_ROADMAP.md`'s "Design System — Foundation" entry. Step 3 is underway: surfaces #2 (**Catalog**), #3 (**Course detail**), #4 (**Student dashboard**), and #5 (**Course content / sessions**) are migrated — see `ARCHITECTURE.md` §6's migration notes for the nav mechanism used and the known limitations of each. Dashboard is the first surface built on the light lane (§2.3) — see the accent-on-background note there. Every other surface (Landing, and everything below Course content in §4) still uses its pre-design-system styling.

**Course detail shipped without its syllabus and instructor sections**, which §4 #3 lists. That is a data gap, not an omission: `GET /catalog/courses/{slug}` exposes no modules at all, and the `User` entity has no bio/photo/credentials columns, so both sections would have to be invented rather than designed. Both are recorded as deferred backend follow-ups in `ARCHITECTURE.md` §7, to decide when real course content is authored at launch. The instructor section that *did* ship is names only — the whole public dataset. Treat this paragraph as retired once those two follow-ups land.

**What guardrail (c) actually catches.** Setting `dir="rtl"` on `<html>` is *not* enough for Radix. It resolves direction from React context, not the DOM, and portals its floating layers to `document.body` — so a select panel or dialog renders `dir="ltr"` inside a correct Arabic page and mirrors the wrong way. `components/DirectionProvider.tsx` in the root layout is the fix. Nothing in the type checker or the build catches this; it only shows up by opening the component under `/ar` and looking at which side things land on. Re-run that check for every new Radix primitive.

**Two deliberate exceptions to guardrail (a)**, both commented where they live: code blocks are pinned `text-align: left` because code must not mirror, and the dialog's centring transform carries an `rtl:` pair because a transform has no logical equivalent.

---

## 7. Definition of done (design)

A surface is done when: the §5 checklist passes, it uses only tokens from §2, its components follow §3, and it matches its §4 intent. Functional "done" is still `verify.mjs` green, per the architecture docs. Design gate and functional gate are separate — both must pass.

**Surface #5's original row described a surface CodeForge doesn't have.** Earlier drafts of §4 #5 called for "lesson body (long Arabic reading)" and "mark-complete" — copied from a self-paced-course template before the live-cohort model was fully reflected here. `PRODUCT.md` is explicit that CodeForge is "explicitly not a self-paced video library," and `docs/SRS.md` states certification is derived from attendance + assessment grades, never video-watch completion. Neither a long-form reading body nor mark-complete has, or should have, backing: there is no `Lesson` entity and no body field on `Session` (only a short plain-text `Description` and a `VideoUrl`), and `SessionProgress` is Phase-0 schema with no writer and no reader anywhere in the application. Both are **intentionally not built, not deferred** — they contradict the product this is, not a gap waiting on backend work. §4's row above reflects the corrected intent: session-type-driven navigation, not a reading view.
