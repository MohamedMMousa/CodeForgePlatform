# CodeForge Academy

A live-cohort programming LMS for a new coding academy (Egypt, EGP, GMT+2), targeting
grade 8–12 and college students. Courses run as recurring batches ("cohorts") of live
sessions; students enroll per-course or per-track, pay manually, and get admin-reviewed
access. Bilingual — Arabic (RTL) and English (LTR) throughout.

Two-tier certificates (completion vs. participation), quizzes/exams, code assignments
with an auto-grader hook, attendance tracking, admin/instructor analytics dashboards,
a channel-agnostic notification system, and a full admin console are all built — see
[What's built](#whats-built) below.

## Stack

| Layer | Technology |
|---|---|
| Backend | ASP.NET Core 8, Clean Architecture (`Domain ← Application ← Infrastructure ← Api`), CQRS via MediatR + FluentValidation |
| Database | PostgreSQL, EF Core 8 (Npgsql) |
| Frontend | Next.js 15 (App Router), React 19, locale-prefixed routes (`/en`, `/ar`) |
| Tests | xUnit + FluentAssertions (`tests/CodeForge.UnitTests`) |

The browser calls the API through a same-origin Next.js proxy (`/api/*` → the API), not
directly — see `docs/ARCHITECTURE.md` §6. This keeps auth cookies first-party.

## Repository layout

```
src/
  CodeForge.Domain/          POCO entities only (Entities/Entities.cs)
  CodeForge.Application/     MediatR handlers, validators, DTOs — one folder per feature
  CodeForge.Infrastructure/  EF Core, JWT, email, notifications, file storage
  CodeForge.Api/             Thin controllers + Program.cs wiring
frontend/
  app/[locale]/...           Next.js App Router pages, locale-prefixed
  lib/api.ts                 Typed fetch wrapper — every API call goes through this
  lib/i18n.ts                Bilingual dictionary (en/ar), one section per feature area
tests/
  CodeForge.UnitTests/       Pure-logic unit tests, mirrors the src/ feature folders
docs/                        Governing reference docs — read these before making changes
```

## Getting started

### Prerequisites

- .NET 8 SDK
- Node.js 18+
- PostgreSQL running locally (or reachable), with an empty database created (e.g. `codeforge_db`)

### 1. Backend

Configure secrets with .NET User Secrets — **not** `appsettings.json`, which only
holds structure/placeholders and is safe to commit. The API fails fast at startup if
these are missing:

```bash
cd src/CodeForge.Api
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=codeforge_db;Username=postgres;Password=<yours>"
dotnet user-secrets set "JwtSettings:Secret" "<a long random string>"
dotnet user-secrets set "AdminSeed:Email" "admin@codeforge.academy"
dotnet user-secrets set "AdminSeed:Password" "<a strong password>"
dotnet user-secrets set "AdminSeed:FullName" "Platform Administrator"
```

Apply migrations and run:

```bash
dotnet ef database update --project src/CodeForge.Infrastructure --startup-project src/CodeForge.Api
dotnet run --project src/CodeForge.Api
```

The API starts at **http://localhost:5205** (Swagger UI at `/swagger`) and seeds the
admin account from `AdminSeed:*` on first run.

### 2. Frontend

```bash
cd frontend
npm install
echo "API_INTERNAL_URL=http://localhost:5205" > .env.local
npm run dev
```

Runs at **http://localhost:3000**. The browser only ever talks to this origin — Next.js
proxies `/api/*` to `API_INTERNAL_URL` server-side (`next.config.mjs`), which is what
lets auth cookies stay first-party. `API_INTERNAL_URL` is server-only (never
`NEXT_PUBLIC_`); in production it must be set on the frontend host to the real API URL,
or every `/api/*` call breaks.

### 3. Run tests

```bash
dotnet test CodeForge.slnx
cd frontend && npx tsc --noEmit
```

### Optional: auth token-leak / cache-posture check

`scripts/check-token-leak.mjs` asserts that authenticated pages leak no token material
and stay non-cacheable, and that no `app/[locale]` route is prerendered. It needs a
running stack and credentials, so it is not part of `scripts/verify.mjs`. Run it against
a **production** build — a dev server serializes cookie values into the RSC payload via
React's dev-only debug channel (harmless, and explained in `docs/ARCHITECTURE.md` §6),
so the script refuses to certify one:

```bash
cd frontend && npm run build && npx next start -p 3001
```

```bash
CHECK_EMAIL=admin@codeforge.academy CHECK_PASSWORD=<pw> node scripts/check-token-leak.mjs --base-url=http://localhost:3001
```

`--build-only` runs just the prerender assertions against `.next`, with no server needed.

### Optional: real email / WhatsApp

By default the API logs outgoing notifications instead of sending them
(`LoggingEmailSender`) — fine for local dev. To send real email, set
`EmailSettings:Enabled = true` plus SMTP host/credentials via User Secrets.
WhatsApp is wired into the notification catalog but not implemented against a real
provider yet — `WhatsAppSettings:Enabled` stays `false` until that's built (see
`docs/ARCHITECTURE.md` §7).

## What's built

| Area | Status |
|---|---|
| Auth, courses, tracks, cohorts, coupons, enrollment requests | ✅ |
| Course content — modules, sessions, materials, announcements | ✅ |
| Attendance, quizzes/exams, code assignments, gradebook | ✅ |
| Certificates (two-tier) + admin/instructor analytics | ✅ |
| Notifications (event catalog, email live, WhatsApp stubbed) | ✅ |
| Admin console (courses/tracks/cohorts/coupons/requests/users) | ✅ |
| Self-hosted auto-grader, recording storage, load testing, hosting decision | ⏳ deferred |

See `docs/IMPLEMENTATION_ROADMAP.md` for the full phase-by-phase breakdown and
`handoff_phase*.md` files at the repo root for detailed session-by-session notes.

## Documentation map

Read these, in this order, before making non-trivial changes:

1. **`docs/SRS.md`** — product requirements: what the platform needs to do and why.
2. **`docs/ARCHITECTURE.md`** — how the system is built: layering, module status,
   cross-cutting concerns (auth, notifications, file storage), open decisions.
3. **`docs/DATABASE.md`** — schema and the business rules encoded in it.
4. **`docs/API_CONVENTIONS.md`** — route/error/pagination/auth conventions every
   endpoint follows.
5. **`docs/CODING_STANDARDS.md`** — the CQRS-triplet pattern, i18n rules, testing
   philosophy, and other conventions to match.
6. **`docs/IMPLEMENTATION_ROADMAP.md`** — phase plan, what's done, what's next.

`handoff_*.md` files at the repo root are point-in-time session notes (bugs found,
decisions made, verification performed) — useful for context on *why* something
looks the way it does, but the numbered docs above are the source of truth for
*current* state.

## Key conventions (see `docs/CODING_STANDARDS.md` for the full list)

- Every use case is a `Command`/`Query` + `Validator` + `Handler` triplet under
  `Application/<Feature>/<UseCase>/`.
- Controllers are thin: build a MediatR request, `await _sender.Send(...)`, return
  `Ok(...)`. No try/catch in controllers or handlers — `ExceptionHandlingMiddleware`
  maps typed exceptions to HTTP status codes centrally.
- Volatile/external concerns (email/WhatsApp, file storage, code execution) sit
  behind an interface in `Application`, implemented in `Infrastructure`, so the
  concrete provider can change without touching business logic.
- Every new user-facing string is added to **both** `en` and `ar` in
  `frontend/lib/i18n.ts` in the same change.
- "Compute, don't store" for anything derivable — e.g. cohort seat availability and
  attendance rate are computed at read time, never persisted redundantly.
