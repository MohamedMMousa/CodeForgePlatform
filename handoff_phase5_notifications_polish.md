# Session Handoff — Phase 5 (Notifications & Polish, partial) Complete

**Date:** 2026-07-22
**Status at handoff:** Phases 0–4 complete. Phase 5 partially complete — scoped down at
kickoff (see below). Remaining Phase 5 items and Phase 6+ are open.

---

## 0. FIRST THING TO DO IN THE NEW SESSION

Read these governing docs before touching anything (standing rule for this repo). All
were updated at the end of this session, so they're current:

- `docs/SRS.md` — product requirements
- `docs/ARCHITECTURE.md` — Clean Architecture + CQRS layout, module status table,
  §3 (notifications + private file storage — both new this session), §7 (open
  decisions, including the WhatsApp blocker)
- `docs/DATABASE.md` — schema, business rules, migrations (**unchanged this
  session** — Phase 5 added no schema)
- `docs/API_CONVENTIONS.md` — endpoint/error/validation conventions
- `docs/CODING_STANDARDS.md` — patterns to match
- `docs/IMPLEMENTATION_ROADMAP.md` — phase plan + what's done/next (Phase 5 marked
  🚧 PARTIAL, with an explanation of what was scoped out and why)

## 1. What this project is

**CodeForge Academy** — a live-cohort programming LMS. ASP.NET Core 8 backend (Clean
Architecture, CQRS via MediatR + FluentValidation, EF Core + PostgreSQL) and a
Next.js 15 / React 19 frontend (bilingual EN/AR, LTR/RTL). Egypt-based, EGP. Under git
on `main`; this session's work is in small reviewable commits.

Solution: `CodeForge.slnx` → `src/CodeForge.{Domain,Application,Infrastructure,Api}`.
Frontend: `frontend/`. Tests: `tests/CodeForge.UnitTests` (xUnit, **62 passing**, up
from 48 at the start of this session).

## 2. Why Phase 5 was scoped down (read this before assuming something is "missing")

The roadmap's Phase 5 bundles several independent items of very different weight:
WhatsApp integration, a notification event catalog, a recording storage upgrade, load
testing, and a hosting decision. Before building, I confirmed scope with the user via
three questions:

1. **A security finding took priority over the roadmap.** While scoping the phase I
   found `wwwroot/uploads` (payment proofs + course materials) was served via plain
   `app.UseStaticFiles()` — fully public, no auth, no expiry. The user chose to fix
   this (both payment proofs and materials) as part of Phase 5, ahead of anything on
   the original list.
2. **WhatsApp**: the user chose "build the catalog + channel abstraction, WhatsApp
   stays a stub" — i.e. build the real thing for email, keep WhatsApp as a registered
   but inactive channel, since real Meta Business credentials don't exist here (same
   blocker class as Piston in Phase 3).
3. **Recording storage, load testing, hosting decision**: the user chose to skip all
   three this session — recordings have no upload flow to protect yet, load testing
   needs a tooling decision, and hosting is a decision only the user can make.

So "Phase 5 complete" in this handoff means: the notification catalog + the uploads
security fix are done and verified. WhatsApp, recording storage, load testing, and
hosting remain genuinely open — see `ARCHITECTURE.md` §7.

## 3. What shipped this session

### A. Uploads security fix (found during scoping, not on the original roadmap)

- `LocalFileStorageService` (`src/CodeForge.Infrastructure/EnrollmentRequests/`) now
  stores files under `src/CodeForge.Api/PrivateStorage/{payment-proofs,materials}/`
  — **outside** `wwwroot`, gitignored, never reachable by static file middleware.
- `app.UseStaticFiles()` removed entirely from `Program.cs` — nothing in `wwwroot`
  needs static serving now.
- `IFileStorageService` (`src/CodeForge.Application/Common/Interfaces/`) — `Save*`
  methods now return an **opaque storage key**, not a URL. New `Open*Async` methods
  return `(Stream, ContentType)` for authenticated endpoints to stream back. Defends
  against path traversal (`Path.GetFileName` strips any directory component from a
  DB-supplied key before combining with the storage directory).
- New endpoints: `GET /materials/{id}/file` (enrollment-gated —
  `Materials/GetMaterialFile/`, reuses `MaterialParentResolver` +
  `CourseContentAuthorization.EnsureCanView`) and `GET /enrollment-requests/{id}/payment-proof`
  (admin-only — `EnrollmentRequests/GetPaymentProofFile/`).
- `MaterialDto.FileDownloadUrl` / `EnrollmentRequestDto.PaymentProofDownloadUrl` /
  `EnrollmentRequestDetailDto.PaymentProofDownloadUrl` now hold the **API path**
  (`/materials/{id}/file`), not a raw storage URL.
- Frontend: `downloadAuthenticatedFile(path, token)` in `frontend/lib/api.ts` — fetches
  with the Bearer token, opens a blob URL in a new tab. Plain `<a href>` can't carry
  auth, so both the instructor module page and student course-content page were
  switched from static links to a "Download" button using this helper.
- **Old exposed files were deleted** (`wwwroot/uploads/` removed) — they're
  unreachable either way now that static serving is gone, but the directory itself is
  gone too. Any pre-Phase-5 material/payment-proof DB rows now point to storage keys
  that don't exist on disk (never migrated) — downloading them 404s cleanly (verified,
  not a 500) rather than serving stale content.

### B. Notification event catalog

- `NotificationEventType` (`Application/Common/Notifications/`) — 4 constants:
  `EnrollmentApproved`, `EnrollmentRejected`, `CertificateIssued`, `AssignmentGraded`.
- `NotificationEvent` — a channel-agnostic record (event type, recipient
  email/name/phone, a string-keyed data dictionary).
- `INotificationChannel` / `INotificationDispatcher` (`Application/Common/Interfaces/`)
  — dispatcher fans an event out to every registered channel, catching and logging
  per-channel failures so **a notification failure never fails the business
  operation that triggered it**.
- `Infrastructure/Notifications/`: `EmailNotificationChannel` (wraps `IEmailSender`),
  `EmailNotificationTemplates` (pure static rendering, kept separate specifically so
  it's unit-testable without DI — see tests), `WhatsAppNotificationChannel` (no-ops
  with a log line while `WhatsAppSettings:Enabled` is false), `NotificationDispatcher`.
- Retired: `IEnrollmentNotificationService` / `LoggingEnrollmentNotificationService`
  (the old one-off interface) — deleted, replaced by the general catalog.
- Wired into 4 real triggers: `ApproveEnrollmentRequestCommandHandler`,
  `RejectEnrollmentRequestCommandHandler` (both pre-existing, previously used the
  retired interface), `IssueCertificateCommandHandler`, `GradeSubmissionCommandHandler`
  (both new — these didn't notify anyone before this session).
- `appsettings.json` gained a `WhatsAppSettings` section (`Enabled: false`,
  `PhoneNumberId`/`AccessToken` placeholders).

## 4. Verification done (real API + browser, not just build)

- Confirmed the old static upload path now 404s; the new endpoints require auth
  (401 anonymous) and correctly gate by role/enrollment (admin/enrolled-student 200,
  AdminOnly-policy 403 for a student on the payment-proof endpoint).
- Uploaded a real file via the API, downloaded it through curl (byte-for-byte
  correct) and through the actual instructor browser UI (clicked "Download",
  confirmed the network request succeeded, no console errors).
- Triggered all 4 notification events for real: submitted two fresh enrollment
  requests and approved one / rejected the other, issued a certificate, graded an
  assignment submission. Confirmed each rendered the correct subject + body in
  `LoggingEmailSender`'s dev-mode log output (course titles, temporary password,
  access expiry date, rejection reason, certificate tier/serial, score/feedback all
  correctly interpolated), and `WhatsAppNotificationChannel` logged a graceful
  "[WhatsApp not configured] Skipped ..." for every single event rather than failing
  anything.
- 62/62 unit tests pass (14 new: `EmailNotificationTemplatesTests`,
  `NotificationDispatcherTests`, `WhatsAppNotificationChannelTests`). Frontend
  typechecks clean (`npx tsc --noEmit`).

## 5. Known state / dev-DB leftovers

- Test artifacts from this session's verification remain in the dev DB (harmless,
  same pattern as prior phases): two new users (`notify.approve@example.com`,
  `notify.reject@example.com` — the latter's request was rejected, no account
  created for it), one new certificate (Data Structures, participation tier), one
  graded assignment submission (Student Two, "Sum Two Numbers", 80/100), and a test
  material ("Phase5 Test File") on the Python Fundamentals module. Delete if you want
  a clean slate.
- Pre-existing materials/payment-proofs from before this session (e.g. "Slides" on
  Module 1) now 404 on download since their physical files lived in the deleted
  `wwwroot/uploads` — expected, not a regression to chase.

## 6. Suggested next steps

- Resolve the hosting/deployment decision — it's the single blocker for the most
  things (self-hosted auto-grader, load testing, and eventually a production
  WhatsApp rollout all wait on it).
- If/when real WhatsApp Business Cloud API credentials exist: set
  `WhatsAppSettings:Enabled = true` + `PhoneNumberId`/`AccessToken`, then implement
  the actual HTTP call in `WhatsAppNotificationChannel.SendAsync` (currently throws
  `NotImplementedException` if enabled without a real implementation — caught by the
  dispatcher, but should be filled in before flipping the flag in production).
- Backfill the missing **admin console** (course/track/cohort/coupon/enrollment-request
  review CRUD UI) — still a gap from earlier phases; the new
  `GET /enrollment-requests/{id}/payment-proof` endpoint has no frontend consumer yet
  since there's no admin enrollment-review page at all.
- A branded server-rendered certificate PDF (currently browser print-to-PDF) —
  flagged as future polish since Phase 4.

## 7. How to run

- API: `dotnet run --project src/CodeForge.Api` → http://localhost:5205 (`/swagger`).
  Secrets in User Secrets (`codeforge-api-secrets`).
- Frontend: `cd frontend && npm run dev` → http://localhost:3000.
- Postgres localhost:5432, db `codeforge_db`. Admin `admin@codeforge.academy` /
  `AdminPass123!`; student `student2@example.com` / `Student2Pass!23`.
- `dotnet test CodeForge.slnx` (62 tests) and `cd frontend && npx tsc --noEmit` both
  green.
