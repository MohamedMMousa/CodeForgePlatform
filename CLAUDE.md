# CodeForge Academy

A **live-cohort** programming LMS for Egypt/MENA — not a self-paced course site. Tracks →
cohorts → enrolled students, manual payment with admin review, coupons, attendance, quizzes and
code assignments, a gradebook, two-tier certificates, and channel-agnostic notifications.

**Bilingual Arabic/English with RTL is a first-class product requirement**, not a later polish
pass. Backend is .NET 8 / Clean Architecture + CQRS; frontend is Next.js 15 App Router + React 19.

## Commands

```bash
node scripts/verify.mjs            # build + test + lint + typecheck + next build (the `verify` skill)
node scripts/verify.mjs --quick    # same, minus next build, for tight loops

dotnet build CodeForge.slnx
dotnet test CodeForge.slnx
npx tsc --noEmit                   # from frontend/
npm run build                      # from frontend/
npm run lint                       # from frontend/ — eslint, flat config in eslint.config.mjs

dotnet run --project src/CodeForge.Api      # :5205, Swagger at /swagger
npm --prefix frontend run dev               # :3000

dotnet ef database update --project src/CodeForge.Infrastructure --startup-project src/CodeForge.Api

node scripts/generate-api-types.mjs         # regenerate frontend/lib/api-schema.d.ts; needs the API running

node scripts/check-token-leak.mjs --build-only   # auth token-leak + cache-posture guard
# full run needs a running stack + CHECK_EMAIL/CHECK_PASSWORD, against a PROD build (see README)

node scripts/check-rate-limit.mjs                # proves /leads rate limiting fires + partitions correctly
# needs a running API with Proxy__TrustForwardedFor=true (see README)
```

Backend secrets come from .NET User Secrets, never `appsettings.json`. The API fails fast at
startup if they're missing; see `README.md`.

## Backend conventions

- **Layering is one-way:** `Domain ← Application ← Infrastructure ← Api`. Application never
  touches EF Core types — only `ICodeForgeDbContext`.
- **CQRS triplet, one folder per use case:** `Application/<Feature>/<UseCase>/` containing
  `<UseCase>Command.cs` (or `Query`), `<UseCase>CommandValidator.cs`, `<UseCase>CommandHandler.cs`.
  Shared DTOs and mapping for a feature go in `Application/<Feature>/Common/` — reuse them,
  don't re-map per handler.
- **Thin controllers:** build a MediatR request, `await _sender.Send(...)`, return. No logic.
- **No try/catch** in handlers or controllers. Throw typed exceptions (`KeyNotFoundException`,
  `InvalidOperationException`, `UnauthorizedAccessException`) and let
  `ExceptionHandlingMiddleware` map them to status codes centrally.
- **Volatile concerns behind interfaces:** email/WhatsApp, file storage, code execution are
  interfaces in `Application`, implemented in `Infrastructure`, so the provider can change
  without touching business logic.
- **Compute, don't store.** Anything derivable is computed at read time — cohort seat
  availability, attendance rate, certificate eligibility. Never persist it redundantly.
- Roles and statuses are `const string` in `Application/Common/Constants/`. Never hardcode them.

## Frontend conventions

- **Every route lives under `app/[locale]/`.** The middleware assumes locale-prefixed routes.
- **Bilingual rule:** every user-facing string goes into `frontend/lib/i18n.ts` under **both**
  `en` and `ar` **in the same change**. A change that adds English-only copy is incomplete.
  Never hardcode strings in JSX.
- **All network access goes through `apiFetch` in `frontend/lib/api.ts`.** Never raw `fetch`.
  Catch `ApiRequestError` and read `.info.status` / `.info.errors` for form-level errors.
- Server components by default; `"use client"` only for interactivity (forms, auth).
- **Styling** is Tailwind v4 + shadcn/ui, reading from the design tokens in
  `app/globals.css`. `docs/DESIGN_LANGUAGE.md` is the source of truth for every colour,
  size, and radius; when it and the code disagree, it wins. Tailwind's default palette is
  deliberately removed, so `bg-blue-500` does not compile — use the token utilities
  (`bg-surface`, `text-text-muted`, `text-accent-text`, `rounded-card`, …).
  Restyled primitives live in `components/ui/` (button, badge, card, input, select,
  table, dialog). **Surfaces are migrated one at a time** — pages predating the design
  system still use the legacy classes (`.card`, `.btn`, `.field`) kept at the bottom of
  `globals.css`, alongside a temporary preflight-compat block. Delete from both as each
  surface is rebuilt.
- **RESOLVED — the `container`/`grid`/`table` utility collision.** The legacy block in
  `globals.css` used to define plain classes named `.container`, `.grid`, and `.table`,
  unlayered so it beat `@layer utilities` no matter the specificity — a component using
  the `grid` utility silently inherited the catalog grid's `grid-template-columns` and
  gap (this clipped a card's status badge before it was found). Fixed by renaming the
  legacy definitions to `.cf-container`/`.cf-grid`/`.cf-table` and updating every legacy
  `className` reference to match — a pure rename, same computed styles, nothing
  restyled. `components/ui/` is free to use the real `grid`/`table`/`container`
  utilities now. Moving the legacy block into a cascade layer instead was tried and
  reverted: Tailwind emits its own `.container` utility because that bare name was in
  legacy JSX, so layering handed every legacy page Tailwind's container (max-width
  1280px, no auto margin) in place of the 960px centred one.
- **Light/dark is per surface, not per viewer.** `data-theme` on any element re-scopes
  every token below it; `app/[locale]/layout.tsx` seeds the document `dark`. Dark is the
  shop window (landing, catalog, course detail), light is for reading and work.
- **Logical properties only** — `ms-`/`me-`, `ps-`/`pe-`, `start`/`end`, `text-start`.
  Never `left`/`right`/`ml-`/`mr-`. The two deliberate exceptions are both commented
  where they live: code blocks are pinned LTR, and the dialog's centring transform has
  no logical equivalent so it carries an `rtl:` pair.
- **Radix needs `DirectionProvider`, not just `dir` on `<html>`.** It resolves direction
  from React context, and its floating layers portal to `document.body`. Without the
  provider (mounted in the root layout) every select panel and dialog stamps itself
  `dir="ltr"` inside an otherwise correct Arabic page.

## Definition of done

1. `node scripts/verify.mjs` is fully green. Fix everything red before reporting back — never
   hand over a diff that was not run.
2. New user-facing strings exist in both `en` and `ar`.
3. Any UI change is checked in **Arabic/RTL as well as English**. No design or accessibility
   tooling in this repo covers RTL; that check is manual.
4. User-visible flows are exercised end-to-end against the running API.
5. `docs/` updated if the change affects what those docs describe.

## Docs

Read in this order before non-trivial changes: `docs/SRS.md` (requirements) →
`docs/ARCHITECTURE.md` (layering, module status, cross-cutting concerns) → `docs/DATABASE.md`
(schema + encoded business rules) → `docs/API_CONVENTIONS.md` (routes, errors, auth) →
`docs/CODING_STANDARDS.md` (full convention list) → `docs/IMPLEMENTATION_ROADMAP.md` (phases).

**Do not trust these:** `CodeForge_SRS_v2.md` and `ERD_CodeForge_v2.md` at the repo root are
stale and contradict what shipped — they list certificates, the Arabic toggle, and coupon codes
as excluded from MVP, and all three are built. `docs/SRS.md` is the real requirements document.
Root `handoff_*.md` files are point-in-time session notes, useful for *why* something looks the
way it does, but never authoritative on *current* state.
