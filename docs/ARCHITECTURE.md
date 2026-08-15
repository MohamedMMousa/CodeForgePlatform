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
- **Isolate volatile/external concerns behind interfaces**: notifications
  (`INotificationDispatcher` fans a `NotificationEvent` out to every registered
  `INotificationChannel` — `EmailNotificationChannel` is fully wired,
  `WhatsAppNotificationChannel` is registered but gated by `WhatsAppSettings:Enabled`
  since WhatsApp Business Cloud API needs credentials this environment doesn't have,
  same blocker class as Piston — see §4 and Phase 5's handoff), file storage
  (`IFileStorageService` — private local disk storage outside `wwwroot`, served only
  through authenticated endpoints; see §3), code execution (`ICodeExecutionService` —
  Piston was the Phase 3 choice, but its public API went whitelist-only mid-phase;
  `DeferredCodeExecutionService` is the active implementation for now,
  `PistonCodeExecutionService` stays registered and ready — see §4 and `DATABASE.md`
  §7), video (external links only, never embedded).
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
| Frontend | Next.js 15 (App Router), `frontend/` | Locale-prefixed routes (`/en`, `/ar`), RTL-aware. Browser calls go through a `/api/*` Next.js rewrite to the API (§6) — not a direct cross-origin call — so auth cookies stay first-party. |
| Tests | xUnit + FluentAssertions, `tests/CodeForge.UnitTests` | Backend only so far; grows alongside handlers. |

## 3. Cross-Cutting Infrastructure (built in Phase 0)

- **Exception handling** — `src/CodeForge.Api/Middleware/ExceptionHandlingMiddleware.cs`
  is the single place exceptions become HTTP responses. Handlers throw typed
  exceptions (`ValidationException`, `UnauthorizedAccessException`,
  `KeyNotFoundException`, `InvalidOperationException`, `PasswordChangeRequiredException`);
  the middleware maps them to a `ProblemDetails` JSON envelope (400/401/403/404/400/500).
  **`UnauthorizedAccessException` always maps to 401** (not authenticated / bad
  credentials). `PasswordChangeRequiredException` maps to 403 with an extra `code`
  extension (`"password_change_required"`) — the one "authenticated but forbidden until
  a specific action is taken" case in the repo; see the `MustChangePassword enforcement`
  bullet below and `API_CONVENTIONS.md` §3/§4. Controllers must never catch exceptions
  themselves.
- **`MustChangePassword` enforcement** — `PasswordChangeRequiredFilter`
  (`src/CodeForge.Api/Filters/PasswordChangeRequiredFilter.cs`) is a global MVC
  authorization filter (registered in `Program.cs` via `AddControllers(o => o.Filters.Add<...>())`)
  that reads a `must_change_password` claim `JwtTokenGenerator` embeds in every access
  token. It fails closed: every authenticated endpoint is blocked unless marked
  `[AllowAnonymous]` or `[AllowPendingPasswordChange]` — a new endpoint is protected the
  moment it's written, with no per-endpoint opt-in required. Because it reads
  `HttpContext.User` claims rather than the `Authorization` header/cookie directly, it
  survived the httpOnly-cookie migration (see below) unmodified — it was designed for
  exactly that. `POST /auth/change-password` (the only opted-out mutating endpoint,
  alongside the read-only `GET /auth/me`) clears the flag and mints a fresh token pair,
  set as cookies in the same response, so the caller resumes normal access without a
  second login — see `ChangePasswordCommandHandler`.
- **Session cookies, CSRF, and logout** — access and refresh tokens live only in
  httpOnly, Secure, `SameSite=Lax`, host-only (no `Domain=`) cookies (`cf_access`,
  `cf_refresh`), issued by `AuthCookieWriter` and read by `JwtBearerEvents.OnMessageReceived`
  (`Program.cs`) — the header still works as a fallback (Swagger, direct clients).
  This is safe as first-party/`SameSite=Lax` only because the frontend proxies `/api/*`
  to this API via a Next.js rewrite (`frontend/next.config.mjs`), so the browser only
  ever sees one origin; see `frontend/lib/session.ts` and `middleware.ts` below. A
  non-httpOnly `cf_csrf` cookie backs a double-submit check
  (`src/CodeForge.Api/Filters/CsrfProtectionFilter.cs`, global, same shape as
  `PasswordChangeRequiredFilter`): any unsafe request carrying `cf_access` or
  `cf_refresh` must echo `cf_csrf`'s value in `X-CSRF-Token`, or it 403s with
  `csrf_validation_failed`. `POST /auth/logout` (`AllowAnonymous` — it authenticates
  off the refresh cookie itself, not a `ClaimsPrincipal`, so it works even with an
  expired access token) clears all three cookies and revokes the refresh token; it
  never fails, so an already-signed-out client still gets a clean result.
- **Refresh token rotation, grace window, and reuse detection** —
  `RefreshTokenRotationPolicy` (pure function: `(currentHash, previousHash, rotatedAt,
  presentedHash, now) → Rotate | ReturnCurrent | Reuse | Invalid`) plus
  `RefreshTokenCommandHandler`'s atomic compare-and-swap
  (`IRefreshTokenRotationStore`, implemented in Infrastructure via EF's
  `ExecuteUpdateAsync` — that API lives in the Relational package, which Application
  doesn't reference, hence the interface) exist to make concurrent refreshes safe.
  Tokens rotate on every use; without a grace window, two same-origin requests racing
  to refresh the same expired access token would have the loser present an
  already-superseded token and get logged out — Next.js prefetch fan-out was the
  concrete trigger this was built for, via `middleware.ts`'s former refresh-on-expiry
  (removed — see §6 below); the policy itself is still live infrastructure protecting
  any other concurrent refresh path (e.g. two browser tabs). Within 30 seconds
  of a rotation, presenting the just-superseded token returns the same already-rotated
  pair instead of rotating again — concurrent requests converge on one token rather
  than racing. Outside the window, presenting a superseded token is treated as reuse
  and kills the whole session (both generations), which is stricter than before this
  existed. `PendingRefreshToken` (the current token's plaintext) is a deliberate,
  narrowly-scoped exception to "hashed at rest" — there is no way to hand a concurrent
  latecomer the exact token value a winner already received without it — and is
  overwritten on every subsequent rotation, so at most one superseded plaintext value
  ever lingers.
- **Secrets** — never committed. Local dev uses .NET User Secrets
  (`UserSecretsId=codeforge-api-secrets`); production uses environment variables.
  `appsettings.json` only holds structure/placeholders. The app fails fast at startup
  if `JwtSettings:Secret` or the connection string is missing.
- **Refresh & reset tokens** — hashed at rest (SHA-256 via
  `IJwtTokenGenerator.HashToken`). Only the hash is stored (see the rotation-grace
  exception above); the plaintext token is set as a cookie (login/refresh) or emailed
  (password reset), never returned in a JSON response body, and never persisted
  outside the hash/pending-plaintext columns.
- **Email** — `IEmailSender` abstraction; `SmtpEmailSender` when `EmailSettings:Enabled`
  is true and a host is configured, otherwise `LoggingEmailSender` (fallback that logs
  recipient/subject only — the body, which can carry reset tokens or temp passwords, is
  never logged). This fallback is also what Production binds when email is unconfigured;
  `Program.cs` logs `Critical` once at every boot in that state so it can't go unnoticed
  (loud rather than fail-fast during the pre-pilot period with no mail provider yet).
  Forgot-password sends a reset link by email; the API never returns the token.
- **Notifications** (Phase 5) — `INotificationDispatcher` fans a `NotificationEvent`
  (`NotificationEventType`: `EnrollmentApproved`/`Rejected`, `CertificateIssued`,
  `AssignmentGraded`) out to every registered `INotificationChannel`, isolating
  callers from delivery failures (logs and continues, never throws — a notification
  failure must never fail the business operation that triggered it).
  `EmailNotificationChannel` renders real templates (`EmailNotificationTemplates`,
  kept pure/testable) via `IEmailSender`. `WhatsAppNotificationChannel` is registered
  but no-ops while `WhatsAppSettings:Enabled` is false (the default — WhatsApp
  Business Cloud API needs a Meta-verified business, dedicated number, and
  pre-approved templates that don't exist here).
- **Private file storage** (Phase 5, R2 backend added at go-live) — `IFileStorageService`
  abstracts payment proofs and course materials behind an opaque storage key; there is
  **no** `app.UseStaticFiles()` in `Program.cs`. Files are served exclusively through
  authenticated endpoints (`GET /materials/{id}/file` — enrollment-gated via
  `CourseContentAuthorization.EnsureCanView`; `GET /enrollment-requests/{id}/payment-proof`
  — admin only) that stream the content back after checking authorization, never via
  a public link. (Prior to Phase 5, these were served as plain static files with no
  auth at all — found and fixed while scoping the phase.) Two implementations:
  `LocalFileStorageService` (default, `Storage:Provider=Local`) writes under
  `PrivateStorage/` on the container's own disk — fine for dev, but destroyed on every
  deploy/restart on a host with no persistent volume. `R2FileStorageService`
  (`Storage:Provider=R2`, what production actually runs — see `DEPLOY.md`) is the
  S3-compatible Cloudflare R2 equivalent, chosen specifically because Render's free
  tier has no persistent disk. Selecting R2 needed no repository/model changes,
  exactly the point of the interface boundary — see §1.
- **Error monitoring** — Sentry, both sides, each behind its own DSN-presence gate so
  an unset DSN (local dev, CI) leaves the SDK fully inert rather than partially
  initialized. `TracesSampleRate: 0` on both — the free plan's quota goes to errors,
  not performance traces. API: `Sentry.AspNetCore`, capture rides the existing
  `ExceptionHandlingMiddleware`'s `_logger.LogError(exception, ...)` call via Sentry's
  logging integration, because that middleware catches every exception and never
  rethrows — Sentry's own exception middleware would never see anything.

  Frontend: `@sentry/nextjs` via `instrumentation-client.ts` for the browser, and
  `lib/sentry-node.ts` (imported once, for its side effect, from the root
  `app/[locale]/layout.tsx`) for the Node server runtime. Both are plain SDK
  calls — `Sentry.init()`/`Sentry.captureException()` — with **no build-time Sentry
  webpack integration at all**: `next.config.mjs` does not wrap its config in
  `withSentryConfig`. **Read this in full before "restoring" any of it**, because the
  shape is counter-intuitive: this teardown happened while chasing an edge-runtime
  production crash that Sentry turned out **not** to have caused (it was
  `next/server` pulling in `ua-parser-js` — see the `middleware.ts` bullet in §6).
  Each removal was nonetheless a real edge-unsafe mechanism, verified by
  rebuild-and-grep at the time, and none is worth reinstating blind:
  `autoInstrumentMiddleware` had been auto-wrapping `middleware.ts`'s compiled output
  with `Sentry.wrapMiddlewareWithSentry`; `instrumentation.ts`'s Sentry reference
  stayed in the compiled edge bundle even behind a `NEXT_RUNTIME`-gated dynamic
  `import()`, so the file was deleted in favor of `lib/sentry-node.ts`'s
  root-layout-only import (a structural guarantee — only ever compiled for the Node
  runtime, since nothing in this app opts into `export const runtime = "edge"` except
  the framework-forced `middleware.ts`, which doesn't import it); and
  `withSentryConfig`'s webpack function pushes a `DefinePlugin` and, in any non-dev
  build, the full `@sentry/webpack-plugin` instance onto *every* webpack pass
  including edge, unconditionally, with no dependency on `autoInstrumentMiddleware` or
  any other flag this app could set. Reinstating build-time Sentry is a deliberate
  decision to make later with fresh verification, not a revert.

  **Cost, accepted explicitly:** no edge/middleware-context Sentry coverage (already
  true after the `instrumentation.ts` removal), and no more automatic
  `Sentry.captureException` wrapping for route handlers/server components/pages —
  that auto-wrap doesn't exist without `withSentryConfig`. What still works: browser
  errors (`instrumentation-client.ts`, self-contained), explicit
  `Sentry.captureException` calls (`app/global-error.tsx`, the `sentry-test` server
  action), and anything the Node runtime captures after `lib/sentry-node.ts`'s
  `Sentry.init()` has run, which the root layout guarantees on every request. Also
  lost: source-map upload / debug-ID stamping (was already a no-op — this
  environment never had `SENTRY_AUTH_TOKEN`/`SENTRY_ORG`/`SENTRY_PROJECT` set).

  Both sides scrub `Cookie`/`Authorization`/`X-CSRF-Token` from outgoing events
  explicitly, on top of `sendDefaultPii: false`. Each side has a gated test-emission
  endpoint (API: `POST /diagnostics/sentry-test`; frontend: `/[locale]/sentry-test`,
  gated on a plain server-only `SENTRY_TEST_ENABLED` — deliberately not
  `NEXT_PUBLIC_`-prefixed, since those get inlined as literal values into the bundle
  at build time regardless of `export const dynamic = "force-dynamic"`, confirmed the
  same way), off by default, meant to be flipped on for a few minutes during deploy
  verification and back off — see `DEPLOY.md`.
  `scripts/check-middleware-edge-safety.mjs` (wired into `verify.mjs`) greps the
  compiled edge bundles after every `next build` for ua-parser-js markers, Sentry SDK
  markers, and Node-only globals, so this class of regression fails locally instead of
  reaching production again. Its own header comment is the fullest write-up of the
  incident and, importantly, ranks how much each check can be trusted — the
  ua-parser-js and Sentry markers have both been observed to flip on a local rebuild;
  the `__dirname` check never caught the real bug and should not be read as proof of
  edge-safety.
- **Admin bootstrap** — `DatabaseSeeder.SeedAsync` runs at startup, idempotently
  seeding one super-admin from `AdminSeed:Email`/`Password`/`FullName` config. No-op if
  those aren't configured; never overwrites an existing account.
- **Health checks** — `GET /health` (liveness — process is up, no DB check) and
  `GET /health/ready` (readiness — Postgres reachable, via `AddNpgSql(...)` tagged
  `ready`). Both are registered as terminal `app.UseHealthChecks(...)` middleware
  immediately after `ExceptionHandlingMiddleware` and before HTTPS redirection, the
  rate limiter, and auth — so neither needs `[AllowAnonymous]`, is never rate-limited,
  and never runs through `PasswordChangeRequiredFilter`. A host's restart/liveness
  probe (e.g. Render) should target `/health`, never `/health/ready`: the host's
  response to a failed probe is to restart the instance, and restarting cannot fix a
  database outage — pointing the restart trigger at DB connectivity turns a transient
  Postgres blip into a restart storm. `/health/ready` is for `docker compose`
  `depends_on: condition: service_healthy` and for humans/monitoring.
- **Rate limiting** — ASP.NET Core's built-in limiter, windows now config-driven
  (`RateLimiting` section) rather than hardcoded, defaulting to a generous global
  per-IP window (100 req/min) plus two named policies: `RateLimitPolicies.Auth`
  (10/min, login/refresh/forgot-password/reset-password) and
  `RateLimitPolicies.PublicSubmit` (5/min, anonymous public submissions like
  enrollment requests and the lead form). Partition key is
  `ClientIpResolver.Resolve`, not the raw socket address: behind the production
  Vercel → Render topology, `Connection.RemoteIpAddress` is always Vercel's shared
  egress IP, which would collapse every user on the platform into one bucket. When
  `Proxy:TrustForwardedFor` is on (production only — off by default, matching local
  dev/CI/compose), the resolver reads `X-Forwarded-For` from the **right** end (the
  entry the trusted hop closest to us appended) rather than the left (the value the
  original caller claims, forgeable by anyone), with `Proxy:TrustedProxyHopCount`
  controlling how many entries to skip. **Production runs at 3, measured rather than
  assumed** via `GET /diagnostics/client-ip`: Cloudflare fronts Render in front of
  Vercel, so four entries arrive —
  `<real client>, <vercel egress 3.x>, <cloudflare 172.x/162.x>, <render internal 10.x>`
  — and only the first is stable, the rest rotating per request. At the previous value
  of `0` the partition key was a rotating private `10.x`, which gave every request its
  own bucket and **silently disabled rate limiting altogether** (12 consecutive public
  submissions passed against a 5/min policy). A resolved key in a private range or
  `127.0.0.1` is the signature of this being misconfigured. `Proxy:ClientIpHeader` is a
  second, opt-in source tried first when set — a named single-value header has no
  position to get wrong — but it is **empty by default and unused here**: measurement
  showed no usable `X-Real-IP` reaches this API. It also carries no evidence of *who*
  set it, so it is only as good as the guarantee that every path in overwrites it.
  Fails closed to the socket peer whenever the header is absent, malformed, or shorter
  than the claimed hop count. `OnRejected` now sets `Retry-After` and logs the resolved
  partition key — previously a 429 was invisible. Accepted gap: a caller hitting the
  public `*.onrender.com` origin directly (bypassing Vercel) can still forge whatever
  the resolver trusts — the header value now, the trusted position before — since no
  header-parsing scheme can close that entirely; the limiter is spam friction, not a
  security boundary. See §7 and
  `CodeForge.Api/RateLimiting/ClientIpResolver.cs`.
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
- **Containerization** (`Dockerfile`, `docker-compose.yml`, `Caddyfile`, `render.yaml`)
  — multi-stage build (`sdk:8.0` → `aspnet:8.0-jammy`; not Alpine, since the
  `CultureInfo("ar")`/`("en")` localization pipeline needs full ICU), runs as the
  non-root `app` user (uid 1654) the base image ships. The entrypoint binds
  `${PORT:-8080}` (shell-form + `exec`, so `dotnet` becomes PID 1 for correct SIGTERM
  handling) — Render injects its own `$PORT`; `docker-compose.yml` leaves it unset and
  gets the `:8080` default unchanged. Migrations are **not** baked into the image or
  run automatically in any environment except local compose:
  `Database:AutoMigrate` (`Program.cs`, off by default) gates a
  `Database.MigrateAsync()` call at startup, and only `docker-compose.yml` turns it
  on — production (Render) keeps it false and runs migrations as its own manual
  release step, the same way CI's `drift-check` job does via the `dotnet ef` CLI. See
  `DEPLOY.md` for the production migration step and the full Render/Vercel/Neon/R2
  provisioning sequence. Compose brings up Postgres → the API (`depends_on: condition:
  service_healthy`, gated on `/health`) → Caddy (same gate on the API), terminating TLS
  with Caddy's internal CA for local HTTPS testing — Caddy is **local dev only**;
  Render terminates TLS for the API and Vercel for the frontend in production, and
  neither runs Caddy. `app.UseForwardedHeaders(...)` (proto only — see the rate
  limiting bullet above for why `X-Forwarded-For` handling moved out of this
  middleware and into `ClientIpResolver`; `KnownNetworks`/`KnownProxies` stay cleared,
  safe because the container is never reachable except through the platform edge in
  front of it — Caddy locally, Render's edge in production) restores the real
  scheme from the proxy before HTTPS redirection sees the request. All local
  credentials come from `.env` (gitignored; `.env.example` is the committed template)
  — never baked into the image or `docker-compose.yml` itself; production secrets are
  set directly on Render/Vercel, never in this repo.
- **CI** (`.github/workflows/ci.yml`, two jobs) — **`verify`** installs frontend deps
  then runs `node scripts/verify.mjs` directly: CI does not restate build/test/lint/
  typecheck steps, it invokes the same script the pre-commit hook and a human run, so
  the three never drift apart. **`drift-check`** boots the API for real against a
  `postgres:16` service container — `dotnet ef database update` applies migrations,
  then the API starts with a throwaway `JwtSettings:Secret` and `AdminSeed` left
  unset (so `DatabaseSeeder` no-ops and never touches the database), polls `/health`
  until it's up, regenerates `openapi.json`/`api-schema.d.ts`
  (`node scripts/generate-api-types.mjs`), and fails the job on `git diff` — catching
  a backend DTO change merged without regenerating the frontend types. See §7 for why
  a third job (the `impeccable` design-critique detector) is deliberately not part of
  CI.

## 4. Backend Module Map

Vertical slices under `src/CodeForge.Application/<Feature>/`:

| Module | Status | Notes |
|---|---|---|
| Authentication | ✅ built (Phase 0 hardened) | Login, RefreshToken, ForgotPassword, ResetPassword, ChangePassword, GetCurrentUser. |
| Users | ✅ built (Phase 6) | `UsersController` — admin-only. CreateInstructor (temp password + notification), GetUsers (role/active/search filters), Deactivate/Reactivate (self-deactivation guarded). No self-signup for any role but student. |
| Courses | ✅ built | Admin CRUD, publish/archive, instructor assignment; public catalog via `CatalogController`. Admin frontend landed Phase 6 (`admin/courses`); instructor-assignment + cohort panels live on the existing course-detail page (admin-only). The public catalog **list** (`GET /catalog/courses`) carries a computed `nextCohort` summary per course (status `open`/`almost_full`, start date, seats left, or `null` when nothing is bookable) — `NextCohortSelector` (`Courses/Common/`), a pure function so `GetPublishedCoursesQueryHandler` can resolve it for a whole page in one extra set-based query (not per-course) and stays unit-testable without a database. `null` on this endpoint means "awaiting next batch"; the same field is always `null` on the admin/instructor list endpoints, which don't compute it. |
| EnrollmentRequests | ✅ built (Phase 1) | Submit/list/approve/reject, cohort-based, coupon + capacity aware. Admin review frontend (list/detail/approve/reject/payment-proof download/cancel resulting enrollments) landed Phase 6 (`admin/enrollment-requests`) — previously API-only. |
| Tracks | ✅ built (Phase 1) | Course bundles; admin CRUD; track-level enrollment. Admin frontend landed Phase 6 (`admin/tracks`). |
| Cohorts | ✅ built (Phase 1) | Batch scheduling, capacity, cutoff, lifecycle, "await next batch". Admin frontend landed Phase 6 as course-scoped panels on the course-detail page. |
| Coupons | ✅ built (Phase 1) | Admin CRUD; percent/fixed; validation at enrollment. Admin frontend landed Phase 6 (`admin/coupons`). |
| Enrollments | ✅ built (Phase 1) | Admin cancel/refund action. Frontend exposed Phase 6 on the enrollment-request detail page (per resulting enrollment) — no standalone enrollment browser exists. |
| Leads | ✅ built (Phase 1) | Public contact form + "notify me" (reuses lead capture). |
| Modules & Sessions | ✅ built (Phase 2) | Renamed/merged from Sections+Lectures+LiveSessions — see `DATABASE.md` §6. Live/in-person/pre-recorded sessions, admin+instructor authoring. |
| Materials | ✅ built (Phase 2) | Renamed from Resources; files/text/links attached to a module or session. |
| Announcements | ✅ built (Phase 2) | Schema existed, Application/Api built this phase. Platform-wide (admin) or course-scoped (instructor). |
| MyCourses (student) | ✅ built (Phase 2) | Enrollment-gated course-content read view + dashboard "upcoming items". |
| Attendance | ✅ built (Phase 3) | Manual per-session marking (roster upsert); rate computed from the enrollment's cohort date window, not stored. |
| Assessments | ✅ built (Phase 3) | Quizzes + exams share one type-discriminated table (`Quiz`, `Type` = quiz/exam), mirroring how Phase 2 merged session types. Question/option authoring, timed attempts, auto-graded MCQ scoring. |
| Assignments | ✅ built (Phase 3) | Code assignments with instructor-defined test cases; student code submission; auto-grader integration via `ICodeExecutionService` (currently deferred to manual grading — see §7); instructor manual score/feedback override. |
| Gradebook | ✅ built (Phase 3) | Per-student attendance rate + best assessment score/pass + assignment final score, for both the instructor's course roster and a student's own course view. Admin cross-course analytics stays out of scope, reserved for Phase 4. |
| Certificates | ✅ built (Phase 4) | Two-tier (completion/participation), admin-reviewed issuance with a stored serial + opaque verification code; eligibility reuses the gradebook's attendance + assessment-pass logic (`CourseEligibilityEvaluator`); per-course attendance threshold (null = platform default 75). Public verify-by-code endpoint. |
| Analytics | ✅ built (Phase 4) | Admin business dashboard (enrollments/revenue/leads/cohorts), admin academic dashboard (assessments/pass-rate/certificates per course), instructor dashboard scoped to assigned courses. All computed read-side — no new tables. |
| Notifications | ✅ built (Phase 5) | Event catalog + channel-agnostic dispatch (`INotificationDispatcher`/`INotificationChannel`). Email fully wired for 4 events (enrollment approved/rejected, certificate issued, assignment graded); WhatsApp channel registered but inactive pending real Business API credentials. |
| Admin Console (frontend) | ✅ built (Phase 6) | `frontend/app/[locale]/admin/layout.tsx` — a single "Admin" nav entry (`RoleNav.tsx`) opens a tab-strip sub-nav to Courses/Tracks/Coupons/Enrollment Requests/Users/Analytics, instead of one flat top-bar link per admin page. Ties together the CRUD listed in the rows above; every page follows the existing per-page `useState`/`useEffect` + plain-`<table>` convention (see `CODING_STANDARDS.md`) — no shared table/modal component was introduced. |

Legend: ✅ done · 🚧 in progress this phase · ⏳ not started.

## 5. API Conventions

See `API_CONVENTIONS.md` for the full reference (routes, auth, error shape, pagination,
file uploads, rate limiting, versioning stance).

## 6. Frontend Architecture

- **Next.js App Router**, locale-prefixed routes under `frontend/app/[locale]/...`.
  `middleware.ts` redirects any un-prefixed path to the default locale (`en`) and
  does **nothing else** — no auth resolution, no protected-route redirect, no
  server-side token refresh. It is also written against plain Web `Request`/`Response`
  and **must not import from `next/server`** — that constraint is load-bearing, not
  style. Both properties come out of a production incident where Vercel's Edge Runtime
  (which all Next.js middleware runs under, unconditionally) crashed every request with
  `ReferenceError: __dirname is not defined` at module load.
  **Root cause, confirmed by reading `next@15.5.22` itself:** for the edge compilation
  Next resolves `next/server` to `dist/server/web/exports/index.js`, which re-exports
  `userAgent` from `spec-extension/user-agent`, which requires
  `dist/compiled/ua-parser-js` — whose one Node-global use is
  `__nccwpck_require__.ab = __dirname + "/"`. That barrel is the *only* path by which
  ua-parser-js reaches a middleware bundle (nothing else under `dist/server/web`
  requires it, and `adapter.js` — in every middleware bundle — never touches it), so
  importing nothing from `next/server` removes it from the graph. Returning plain
  `Response` objects is supported by Next's own adapter, which accepts anything
  `instanceof Response` and treats a falsy return as "continue"; `NextResponse.redirect`
  is itself literally `new Response(null, { status: 307, headers: { Location } })`.
  **Why it took six rounds:** webpack folds `__dirname` to a literal locally (the local
  bundle reads `g.ab = "//"`) while Vercel's build does not, so five consecutive
  locally-green fixes shipped broken. Those rounds wrongly blamed `@sentry/nextjs` and
  removed, each independently verified as a real edge-unsafe mechanism but none of them
  the cause: the middleware auto-wrap; `instrumentation.ts`'s Sentry import (even
  dynamically imported behind a runtime check); and `withSentryConfig`, which pushes
  build plugins onto every webpack pass including edge. Rounds 1-3 additionally never
  reached production — Vercel was connected to the wrong repo — which kept the false
  lead alive. The auth logic was stripped in round 5 while isolating the crash; it
  turned out to be innocent, but the simplification stands.
  Dropping the auth logic is **not** a security regression: the API is and was always
  the real authorization boundary regardless of what middleware decided, and every
  protected page already gates through `useSessionGate()` (see the
  `components/SessionGuard.tsx` bullet below) instead of rendering protected content or
  data. The real cost: without middleware refreshing an expired access token before the
  server render, `getServerSession()` (`lib/session.ts`) has nothing to read and returns
  `session: null` — but it also reports whether `cf_refresh` outlived `cf_access`
  (`canRecover`), which is what lets the client recover on its own instead of staying
  signed-out for the rest of the page load. See the `lib/auth.tsx` bullet below. A
  Server Component still can't do this refresh itself: it can't persist the
  `Set-Cookie` a refresh would mint, so doing it there would just rotate the refresh
  token out from under the browser.
- **Proxy** — `frontend/next.config.mjs` rewrites `/api/:path*` to `API_INTERNAL_URL`
  (server-only env var, never bundled into browser JS; defaults to
  `http://localhost:5205`). The browser only ever talks to one origin; this is what
  makes `SameSite=Lax` cookies possible without a shared registrable domain between
  the frontend and API hosts (see the deployment note in §7). `frontend/lib/api.ts`'s
  `BASE_URL` is isomorphic — `"/api"` in the browser (resolved by the rewrite),
  `API_INTERNAL_URL` directly when called from a Server Component (which has no
  browser to resolve a relative URL against, and would otherwise take a pointless
  extra hop through the rewrite it's already running inside).
- `frontend/lib/i18n.ts` — dependency-free dictionary lookup + `{token}` interpolation.
  Add new UI strings to both `en` and `ar` in the same change.
- `frontend/lib/api.ts` — a single `apiFetch` wrapper around `fetch`; typed errors via
  `ApiRequestError` carrying the API's `ProblemDetails` shape. All API calls go through
  this, never raw `fetch`.
- **Generated API types** — `frontend/lib/api-schema.d.ts` is generated from the
  backend's own OpenAPI document via `node scripts/generate-api-types.mjs` (requires the
  dev API running; fetches `swagger.json`, writes `openapi.json`, runs
  `openapi-typescript`). `lib/api.ts`'s exported DTO types are aliases into
  `components["schemas"]`, not hand-mirrored — a backend field rename surfaces as a
  `tsc` error at every consuming call site instead of a silent runtime bug. Both
  `openapi.json` and `api-schema.d.ts` are committed, so `tsc`/`next build`/CI never
  need a live API; re-run the script manually whenever a backend DTO changes. **Every
  controller action must carry `[ProducesResponseType(typeof(ResponseDto), StatusCodes.Status200OK)]`**
  (see `AddSwaggerGen` in `Program.cs`, and `RequireNonNullablePropertiesSchemaFilter`
  in `src/CodeForge.Api/Swagger/`) — without it, Swashbuckle emits no response schema
  and the type is lost. A handful of union types (`SessionType`, `MaterialType`,
  `AssessmentType`, `AttendanceStatus`, `CertificateTier`) stay hand-written in
  `lib/api.ts` since the backend types those fields as plain `string`.
- `frontend/lib/auth.tsx` — React context holding the signed-in user's *profile*, not
  tokens; those live only in the httpOnly cookies the server manages. Seeded from a
  server-resolved `initialSession` prop (see `frontend/lib/session.ts`, wrapped in
  React `cache()`) so the first client render already matches what the server
  rendered — no hydration mismatch. Exposes `status: "authenticated" | "recovering" |
  "unauthenticated"` alongside `session`. `status` seeds to `"recovering"` when
  `initialSession` is `null` but `canRecover` says `cf_refresh` outlived `cf_access` —
  a mount effect then calls `getCurrentUser()` (bounded by a 10s
  `AbortSignal.timeout`, so an unreachable API degrades to `"unauthenticated"` instead
  of hanging) to silently restore the session without a manual re-login. That call
  flows through `lib/api.ts`'s existing `fetchWithAuthRetry`/`attemptRefresh` (401 →
  refresh → retry, already serialized behind one in-flight promise — see the
  `lib/api.ts` bullet), so this effect doesn't add any refresh logic of its own, only
  the trigger that was missing. On success it also calls `router.refresh()` so server
  components pick up the newly-set cookies. A second effect listens for the
  `codeforge:session-expired` window event (dispatched by `lib/api.ts` when a refresh
  attempt gets an explicit non-OK response — a genuinely dead refresh token, not a
  network blip) and clears the session in place. `toSession()`/the `Session` type live
  in `frontend/lib/session-mapping.ts` rather than `auth.tsx` itself, deliberately
  without a `"use client"`/`"use server"` directive: a function exported from a
  `"use client"` module can't be called from the Server Component layout that also
  needs it. `refreshSession()` (re-derives from `GET /auth/me`, then `router.refresh()`
  so server components see it too) is unchanged, still used by
  `change-password/page.tsx` after a cookie-rotating request.
- `frontend/components/SessionGuard.tsx` — `useSessionGate()`, the single place every
  protected page/layout gates rendering. A hook rather than a wrapper component: several
  pages read `session.role` in their own JSX, so the check has to be an inline
  early-return in the page's own render (keeping TypeScript's narrowing of `session`
  intact) rather than inside a wrapper whose `children` would already be constructed —
  and would throw — before the wrapper could block them. Renders one of three shapes
  depending on `status`/role match: the page's real content (`ok: true`), a neutral
  "restoring your session" notice while `status === "recovering"`, or the original
  sign-in fallback (role-restricted pages get the instructor/admin notice; open-to-any-
  signed-in-user pages get a link straight to `/login`) once resolved to
  `"unauthenticated"`. `components/RoleNav.tsx` reads `status` the same way, so the
  header doesn't flash a "Sign in" link during recovery either.
- **Cookie values in the dev RSC payload — dev-only, verified absent in production.**
  View-source on a protected page under `npm run dev` shows `cf_access`/`cf_refresh`
  values inside a `__next_f.push` block. This is **not** the app serializing them:
  `getServerSession()` forwards the cookie in a *request* header and returns only
  `await response.json()`, and `toSession()` narrows that to five scalar fields — no
  `Response`, no `Headers`, and no token ever enters the React tree. What you are
  seeing is React 19's dev-only server-component debug channel capturing the awaited
  return value of `cookies()` (the whole cookie jar) as `[name, {name, value}]`
  entries, tagged with `"env":"Server"` and a stack frame pointing at
  `session.ts`/`layout.tsx`. Two independent confirmations, so nobody has to
  re-investigate:
  1. *Empirical* — a production build (`npm run build && npx next start`) serves zero
     occurrences of either token, of the literals `cf_access`/`cf_refresh`, or even of
     `eyJ`, across page HTML, RSC flight payloads, and prefetch payloads, in both
     locales, while still rendering signed-in.
  2. *Structural* — `next/dist/compiled/react-server-dom-webpack/server.node.js`
     selects its implementation on a hard `process.env.NODE_ENV === 'production'`
     branch, and the production bundle contains **no** `forwardDebugInfo` (20
     occurrences in dev → 0), `emitDebugChunk` (9 → 0), or owner-stack code (7 → 0).
     The debug channel is absent from the production build, not merely disabled.

  There is therefore **no configuration flag to reach for** — none can suppress it in
  dev (the dev bundle *is* the mechanism) and none can enable it in production (the
  code isn't there). `experimental.browserDebugInfoInTerminal` is unrelated: it
  forwards *browser* console output to the dev terminal. Since the flash fix requires
  reading `cookies()` in a Server Component, the dev behaviour is inherent and is left
  alone. Treat it as dev hygiene rather than a vulnerability: the tokens are the
  developer's own, but avoid pasting dev view-source into issues or attaching dev HAR
  captures. `node scripts/check-token-leak.mjs` asserts all of this and deliberately
  refuses to certify a dev server rather than reporting a misleading pass.
- **Forced password change** — `components/PasswordChangeGate.tsx` (mounted in
  `app/[locale]/layout.tsx`) redirects to `/{locale}/change-password` whenever the
  session says `mustChangePassword`, and also on the `codeforge:password-change-required`
  `window` event `lib/api.ts` dispatches on any 403 carrying
  `password_change_required` — a backstop for a stale client-side session. The
  change-password page (`app/[locale]/change-password/page.tsx`) submits via
  `changePassword()` and calls `useAuth().refreshSession()`, so no second login is
  needed. See `API_CONVENTIONS.md` §3.
- **Design system** (foundation landed post-Phase 6) — Tailwind v4 + shadcn/ui, both
  reading from the tokens in `frontend/app/globals.css`. `docs/DESIGN_LANGUAGE.md` §2 is
  the source of truth for those values; this codebase implements it, it does not
  redefine it. Three things about the setup are load-bearing and non-obvious:

  1. **Tailwind's default colour palette is removed** (`--color-*: initial`). An
     off-token colour is a compile error rather than a review finding — the
     machine-checkable half of the doc's "no page gets hand-styled off-token" rule.
     Spacing is *not* enforced the same way: the primitives need fractional steps for
     icon sizing and hit-areas, so the ten-step scale is documented and reviewed, not
     compiled.
  2. **Light/dark is a `data-theme` attribute, not `prefers-color-scheme`.** The mode
     belongs to the surface, not the viewer (dark for marketing/browse, light for
     reading/work), and an attribute is what lets a light panel sit inside a dark page.
     The root layout seeds `dark`. This replaced an OS-preference media query, so the
     app is now deterministically dark until a surface opts into light.
  3. **Radix direction comes from `DirectionProvider`, not the DOM.** `dir` on `<html>`
     is necessary but not sufficient: Radix reads React context, and its floating layers
     portal to `document.body`. Without `components/DirectionProvider.tsx` (mounted in
     the root layout) every portalled primitive stamps `dir="ltr"` on itself and mirrors
     the wrong way in Arabic. Found by inspecting a real select panel under `/ar`, not
     by reading docs — re-verify this whenever a new Radix primitive is adopted.

  Restyled primitives live in `frontend/components/ui/`. `lib/utils.ts`'s `cn` is
  configured with the custom colour and type scales, without which tailwind-merge cannot
  tell `text-body` (a size) from `text-text` (a colour) and silently drops one.

  **Migration is per-surface and incomplete.** Pages built before this still use the
  legacy classes (`.card`, `.btn`, `.field`) at the bottom of `globals.css`, plus a
  temporary block restoring the heading/list/margin defaults Tailwind's preflight zeroes.

  That legacy block used to be **unlayered on purpose**, and three of its class names —
  `.container`, `.grid`, `.table` — were also Tailwind utility names. Unlayered CSS
  outranks `@layer utilities` regardless of specificity, so on any element carrying one
  of those names the legacy rule won and the utility silently lost. This produced a
  real bug: `CardHeader` used the `grid` utility and inherited the catalog grid's
  `repeat(auto-fill, minmax(260px, 1fr))` template and 1.25rem gap, which clipped the
  card's status badge — visibly in English, not in Arabic, purely because the English
  strings are longer.

  **RESOLVED.** The legacy definitions were renamed to `.cf-container`, `.cf-grid`, and
  `.cf-table`, and every legacy `className` reference was updated to match — a pure
  rename, verified rule-for-rule identical (`.cf-container { max-width: 960px; margin:
  0 auto; padding: 2.5rem 1.25rem; }` etc.), no styling changed on any pre-design-system
  page. `components/ui/` can now use the real `grid`/`table`/`container` Tailwind
  utilities without collision.

  Moving the block into a cascade layer instead was tried and reverted, and is worth
  recording so it isn't re-attempted: Tailwind emits its own `.container` utility
  because that bare class name appeared in legacy JSX, so layering handed every legacy
  page Tailwind's container (max-width 1280px, no auto margin) in place of the 960px
  centred one. The rename has no equivalent failure mode, since `cf-container` etc. are
  not Tailwind utility names.
  Both shrink as surfaces are rebuilt in the §4 priority order; when the compat block is
  empty the migration is done. See `docs/assets/brand-guide.png` for the original brand
  reference.

  **Catalog migration (surface #2, first surface rebuilt).** `app/[locale]/catalog/page.tsx`
  and its cards (`CourseCard.tsx`, `CatalogBrowser.tsx`) are on §2 tokens and the
  `components/ui/` primitives only — no legacy `cf-*`/`.card`/`.btn` classes remain on that
  page. `GET /catalog/courses` drives the whole grid from one server-side call
  (`CATALOG_PAGE_SIZE = 100`, a deliberate pilot-scale cap — a "showing first N of M" notice
  plus a `console.warn` fire if the real catalog ever exceeds it, so growth past the cap is
  visible rather than silently truncated); category chips and search filter that one set
  client-side. `catalog/courses/[slug]` and `catalog/tracks/[slug]` (the detail pages) were
  **not** restyled this pass — only their nav — pending surface #3 (Course detail).

  **Nav swap mechanism.** The legacy topbar is inlined in this layout's `<body>` (see above),
  so a nested route-group layout for `/catalog` could only ever *add* a nav, not remove the
  inherited one — making a new dark shop-window Nav (§3 "Nav") exclusive to that subtree the
  structurally "correct" way would mean moving every other route directory into a sibling
  group, a whole-app change that was deliberately deferred. Instead, `components/SiteHeader.tsx`
  (client, `usePathname()`) wraps the legacy topbar as `children` and swaps in
  `components/ShopNav.tsx` for `/catalog` and everything under it; every non-catalog route
  renders the exact same topbar JSX as before, now passed through unchanged. `ShopNav` is
  deliberately **not** session-aware (no `RoleNav`) — a signed-in visitor still sees "Sign in"
  on the catalog. Both the route-group deferral and the missing session-awareness are accepted
  v1 gaps, to revisit when the auth surfaces (§4 #9) or a later surface justify the bigger
  routing change.

## 7. Deferred / Open Architectural Decisions

- **`impeccable` design-critique detector as a CI job** — deferred, local pre-commit
  tool only. The bundled `.claude/skills/impeccable/` detector is gitignored (not
  vendored) and reports version `4.0.3`; the only installable-in-CI form is the npm
  package `impeccable`, currently at `3.5.0`. A CI job would therefore enforce a
  different ruleset than the one actually used locally — the worst failure mode for
  a gate (silent rule drift, not just staleness). Its findings are also design
  judgment, not correctness, and it would be the only CI job depending on a
  non-vendored third-party bundle. Revisit once the npm package catches up to the
  local bundle version, or if the detector is deliberately vendored into the repo.
- **Legacy unlayered `a { color: var(--accent-2) }` outranks the token system** —
  found and worked around, not fixed, during the catalog migration (§6). The rule
  lives in `globals.css`'s legacy section and is deliberately unlayered like the old
  `.container`/`.grid`/`.table` block (§6) — but unlike those, it isn't a bare
  class-name collision, it's a plain `a` element selector, so it silently wins
  against *any* `text-*` colour utility on *any* `<a>`/`Link` anywhere in the app,
  including new design-system surfaces, because unlayered CSS outranks
  `@layer utilities` regardless of specificity. This surfaced when the catalog
  became the first surface to render a `Link` needing a colour other than the
  legacy default (nav links, card title links, and every `Button asChild` wrapping
  a `Link`) — each rendered accent-2 orange instead of its intended token colour
  until fixed. **Current fix, intentionally scoped and temporary:** `!important`
  added to the contested `text-*` classes, only where the catalog surface actually
  renders a coloured `Link` (`components/ui/button.tsx`'s variants, `ShopNav.tsx`,
  `CourseCard.tsx`, the track card in `catalog/page.tsx`) — see the comments at each
  site. **Durable fix, deliberately not done now:** move the legacy `a { color }`
  rule into a cascade layer (or scope it to a `.cf-legacy`-style selector matching
  only pre-design-system pages), then remove every `!important` this workaround
  added. Do this as part of the route-group conversion session (§6's "Nav swap
  mechanism" note — the `(shop)`/`(app)` split already deferred there), not
  piecemeal — the same reasoning that kept the `.container`/`.grid`/`.table` rename
  isolated to its own pass: a global CSS-cascade change touches every legacy page at
  once, so it deserves its own dedicated, fully-verified session rather than being
  bundled into whatever surface happens to trip over it next.
- **Python auto-grader engine** — Piston (`emkc.org`) was chosen in Phase 3 (free,
  no Docker needed in this dev environment), and `PistonCodeExecutionService` was
  built and verified working. Mid-phase, Piston's public API went whitelist-only
  (2026-02-15) and is no longer usable without prior approval. Currently deferred to
  100% manual grading (`DeferredCodeExecutionService`, see `DATABASE.md` §7) until
  either Piston whitelists this use case, a self-hosted Judge0/Piston instance is
  stood up (Docker is available in this dev environment as of the operational-
  readiness work — see §3 below — so this is no longer blocked on that), or another
  engine is chosen.
- **Multi-tenancy posture** (single academy vs. future franchises) — undecided;
  flagged as a risk in `SRS.md`. Current schema and code assume a single tenant; avoid
  decisions that would make multi-tenancy materially harder without discussing first.
- **Hosting/deployment target** — decided and deployed: frontend on Vercel, API on
  Render (Docker), Postgres on Neon, object storage on Cloudflare R2 — all free tier,
  cold starts accepted. See `DEPLOY.md` for the full provisioning runbook. The
  `/api/*` proxy (§6) was chosen specifically so this decision didn't gate the auth
  model: it works whether the frontend and API end up on the same registrable domain
  or, as it turned out, different ones — `*.vercel.app` and `*.onrender.com` are both
  on the Public Suffix List and are cross-site to each other by definition, which
  would otherwise have forced `SameSite=None` cookies and a materially weaker CSRF
  posture. **Deploy-time requirement:** `API_INTERNAL_URL` must be set on Vercel,
  pointing at the Render API's real URL — it has a `localhost:5205` fallback that is
  correct for local dev only and will silently point the proxy nowhere useful (500s
  on every `/api/*` call) if left unset. This is the single most likely deploy
  mistake — see the top of `DEPLOY.md`.
- **Rate limiter's direct-to-origin gap** (accepted, not a bug) — `ClientIpResolver`
  (§3) trusts a proxy-set header (`X-Real-IP` in production, else a fixed position
  counted from the right of `X-Forwarded-For`); a caller who bypasses Vercel and hits
  the public `*.onrender.com` origin directly can still forge whichever of those the
  resolver reads — and the single-value header is the weaker of the two here, since it
  carries no positional evidence of who set it. Unchanged in kind, since Render doesn't
  publish stable
  inbound-edge IP ranges to validate against and Vercel's egress ranges are only
  pinnable via Static IPs/Secure Compute (Pro/Enterprise-only, $100/mo). Accepted:
  this limiter is spam friction on a public write endpoint, not a security boundary.
  Cheap closer if it becomes a real problem: put Cloudflare (free) in front of the
  Render origin and reject non-Cloudflare traffic — not done now.
- **Integration test coverage for the auth pipeline** — near-term backlog, not
  someday. `tests/CodeForge.UnitTests` is pure-unit (no `WebApplicationFactory`), so
  nothing exercises the real cookie/CSRF/`JwtBearer` pipeline end-to-end — cookie
  issuance, `OnMessageReceived` reading `cf_access`, `CsrfProtectionFilter`, the
  refresh rotation grace window, `PasswordChangeRequiredFilter` through the full HTTP
  stack. This gap was accepted for the httpOnly-cookie migration (verified instead via
  live browser/curl checks, not automated), but it is the single piece of this system
  most likely to break silently under a future change with nothing to catch it.
- **Recording storage upgrade** (external links → private storage + signed URLs) —
  still deferred; unlike payment proofs/materials (hardened in Phase 5), there is no
  upload flow for recordings today — they're external Zoom/YouTube/etc. links, so
  there's no local file to protect yet. Revisit once private video hosting is decided.
- **WhatsApp Business Cloud API** — the notification event catalog and channel
  abstraction are built (Phase 5) and email works for every event; WhatsApp itself
  needs a Meta-verified business, a dedicated number, and pre-approved message
  templates, none of which exist in this environment. `WhatsAppNotificationChannel`
  is registered and gated by `WhatsAppSettings:Enabled` (false) — flip it on and
  implement the actual Cloud API call once credentials exist.
