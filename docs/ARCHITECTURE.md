# CodeForge Academy — Architecture

> Read this together with `SRS.md` (what we're building) and `DATABASE.md` (the data
> model). This document describes how the system is built and why.

## 1. Guiding Principles

- **Modular monolith**, not microservices. Right-sized for the ~500-student year-one
  target; module boundaries stay clean (see §4) so pieces can be extracted later if the
  academy grows or a multi-tenancy decision is made.
- **Clean Architecture** with strict inward dependency direction:
  `Domain ← Application ← Infrastructure ← Api`. Application never references EF Core
  or ASP.NET directly — it depends on abstractions (`ICodeForgeDbContext`,
  `IEmailSender`, `IFileStorageService`, …) that Infrastructure implements.
- **CQRS via MediatR**, organized as vertical slices by feature (not by technical
  layer). Every use case is a `Command`/`Query` + `Validator` + `Handler` triplet. See
  `CODING_STANDARDS.md` for the exact pattern.
- **Isolate volatile/external concerns behind interfaces**: notifications (email now,
  WhatsApp later), file storage (local disk now, object storage + signed URLs later),
  code execution (Python auto-grader, engine TBD), video (external links only, never
  embedded).
- **Bilingual and public-facing from day one** — culture resolution and CORS are
  foundation concerns, not afterthoughts.

## 2. System Components

| Component | Technology | Notes |
|---|---|---|
| API | ASP.NET Core 8 (`src/CodeForge.Api`) | Controllers are thin: build a MediatR request, `await _sender.Send(...)`, return `Ok(...)`. No try/catch — see §5. |
| Application | .NET 8 class library (`src/CodeForge.Application`) | MediatR handlers, FluentValidation validators, DTOs, service interfaces. No infra dependencies. |
| Infrastructure | .NET 8 class library (`src/CodeForge.Infrastructure`) | EF Core + Npgsql, JWT, BCrypt, email senders, file storage, database seeding. |
| Domain | .NET 8 class library (`src/CodeForge.Domain`) | POCO entities only, grouped by domain in `Entities/Entities.cs`. Anemic by design — validation/business rules live in Application. |
| Database | PostgreSQL | See `DATABASE.md`. |
| Frontend | Next.js 15 (App Router), `frontend/` | Locale-prefixed routes (`/en`, `/ar`), RTL-aware, calls the API directly from the browser (no BFF layer). |
| Tests | xUnit + FluentAssertions, `tests/CodeForge.UnitTests` | Backend only so far; grows alongside handlers. |

## 3. Cross-Cutting Infrastructure (built in Phase 0)

- **Exception handling** — `src/CodeForge.Api/Middleware/ExceptionHandlingMiddleware.cs`
  is the single place exceptions become HTTP responses. Handlers throw typed
  exceptions (`ValidationException`, `UnauthorizedAccessException`,
  `KeyNotFoundException`, `InvalidOperationException`); the middleware maps them to a
  `ProblemDetails` JSON envelope (400/401/404/400/500). **`UnauthorizedAccessException`
  always maps to 401** — there is currently no distinct "authenticated but forbidden"
  403 path; introduce a dedicated `ForbiddenException` if that's needed later.
  Controllers must never catch exceptions themselves.
- **Secrets** — never committed. Local dev uses .NET User Secrets
  (`UserSecretsId=codeforge-api-secrets`); production uses environment variables.
  `appsettings.json` only holds structure/placeholders. The app fails fast at startup
  if `JwtSettings:Secret` or the connection string is missing.
- **Refresh & reset tokens** — hashed at rest (SHA-256 via
  `IJwtTokenGenerator.HashToken`). Only the hash is stored; the plaintext token is
  returned to the client (login/refresh) or emailed (password reset) and never
  persisted.
- **Email** — `IEmailSender` abstraction; `SmtpEmailSender` when `EmailSettings:Enabled`
  is true and a host is configured, otherwise `LoggingEmailSender` (dev fallback that
  logs instead of sending). Forgot-password sends a reset link by email; the API never
  returns the token.
- **Admin bootstrap** — `DatabaseSeeder.SeedAsync` runs at startup, idempotently
  seeding one super-admin from `AdminSeed:Email`/`Password`/`FullName` config. No-op if
  those aren't configured; never overwrites an existing account.
- **Rate limiting** — ASP.NET Core's built-in limiter. A generous global per-IP window
  (100 req/min) plus two named policies: `RateLimitPolicies.Auth` (10/min, applied to
  login/refresh/forgot-password/reset-password) and `RateLimitPolicies.PublicSubmit`
  (5/min, applied to anonymous public submissions like enrollment requests and the lead
  form).
- **Localization** — `AddLocalization` + `UseRequestLocalization`, resolving `en`/`ar`
  from the `Accept-Language` header. Resource files are added incrementally as
  API-returned messages need translation; the pipeline is ready now.
- **CORS** — config-driven allowed-origins list (`Cors:AllowedOrigins` in
  `appsettings.json`; defaults to `http://localhost:3000` for dev).
- **EF migrations** — `dotnet ef migrations add <Name> --project src/CodeForge.Infrastructure --startup-project src/CodeForge.Api`.
  The historical `InitialCreate` migration was hand-authored without its
  Designer/snapshot companions; this was fixed in Phase 0 by regenerating those files
  under the same migration id (no schema or data change). Do not hand-edit generated
  migration files except to add raw SQL EF cannot express (e.g. the GIN index on
  `activity_logs.metadata` — see `DATABASE.md`).

## 4. Backend Module Map

Vertical slices under `src/CodeForge.Application/<Feature>/`:

| Module | Status | Notes |
|---|---|---|
| Authentication | ✅ built (Phase 0 hardened) | Login, RefreshToken, ForgotPassword, ResetPassword, ChangePassword, GetCurrentUser. |
| Courses | ✅ built | Admin CRUD, publish/archive, instructor assignment; public catalog via `CatalogController`. |
| EnrollmentRequests | ✅ built (Phase 1) | Submit/list/approve/reject, cohort-based, coupon + capacity aware. |
| Tracks | ✅ built (Phase 1) | Course bundles; admin CRUD; track-level enrollment. |
| Cohorts | ✅ built (Phase 1) | Batch scheduling, capacity, cutoff, lifecycle, "await next batch". |
| Coupons | ✅ built (Phase 1) | Admin CRUD; percent/fixed; validation at enrollment. |
| Enrollments | ✅ built (Phase 1) | Admin cancel/refund action. |
| Leads | ✅ built (Phase 1) | Public contact form + "notify me" (reuses lead capture). |
| Modules & Sessions | ✅ built (Phase 2) | Renamed/merged from Sections+Lectures+LiveSessions — see `DATABASE.md` §6. Live/in-person/pre-recorded sessions, admin+instructor authoring. |
| Materials | ✅ built (Phase 2) | Renamed from Resources; files/text/links attached to a module or session. |
| Announcements | ✅ built (Phase 2) | Schema existed, Application/Api built this phase. Platform-wide (admin) or course-scoped (instructor). |
| MyCourses (student) | ✅ built (Phase 2) | Enrollment-gated course-content read view + dashboard "upcoming items". |
| Attendance | ⏳ Phase 3 | Manual per-session marking + reporting. |
| Assessments | ⏳ Phase 3 | Quizzes (exists in schema, not yet in Application), Assignments (new), Exams (new), auto-grader integration. |
| Certificates | ⏳ Phase 4 | Two-tier completion logic. |
| Notifications | ⏳ Phase 5 | Channel-agnostic dispatch; email now (Phase 0), WhatsApp later. |
| Reports/Analytics | ⏳ Phase 4 | Admin + instructor dashboards. |
| Leads | 🚧 Phase 1 | Public contact form; reused for "notify me about next batch." |

Legend: ✅ done · 🚧 in progress this phase · ⏳ not started.

## 5. API Conventions

See `API_CONVENTIONS.md` for the full reference (routes, auth, error shape, pagination,
file uploads, rate limiting, versioning stance).

## 6. Frontend Architecture

- **Next.js App Router**, locale-prefixed routes under `frontend/app/[locale]/...`.
  `middleware.ts` redirects any un-prefixed path to the default locale (`en`).
- `frontend/lib/i18n.ts` — dependency-free dictionary lookup + `{token}` interpolation.
  Add new UI strings to both `en` and `ar` in the same change.
- `frontend/lib/api.ts` — a single `apiFetch` wrapper around `fetch`; typed errors via
  `ApiRequestError` carrying the API's `ProblemDetails` shape. All API calls go through
  this, never raw `fetch`.
- `frontend/lib/auth.tsx` — React context holding the session (tokens + user), backed
  by `localStorage` for this phase. Token refresh rotation and httpOnly-cookie storage
  are a later hardening pass, not yet implemented.
- **Brand** — see `docs/assets/brand-guide.png` and the CSS custom properties in
  `frontend/app/globals.css` (`--bg`, `--card`, `--fg`, `--muted`, `--accent`,
  `--accent-2`). Do not hardcode colors in components; use the CSS variables so theme
  changes stay centralized.
- **No BFF layer** — the browser calls the ASP.NET API directly using
  `NEXT_PUBLIC_API_BASE_URL`. This is acceptable at current scale; revisit if
  server-only secrets or response aggregation become necessary.

## 7. Deferred / Open Architectural Decisions

- **Python auto-grader engine** (Judge0 vs. custom Docker runner vs. managed service) —
  deferred to Phase 3.
- **Multi-tenancy posture** (single academy vs. future franchises) — undecided;
  flagged as a risk in `SRS.md`. Current schema and code assume a single tenant; avoid
  decisions that would make multi-tenancy materially harder without discussing first.
- **Hosting/deployment target** — not yet chosen; decide before Phase 5.
- **Recording storage upgrade** (external links → private storage + signed URLs) —
  planned but not scheduled to a specific phase yet.
