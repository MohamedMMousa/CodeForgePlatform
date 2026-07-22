# Session Handoff — Phase 4 (Certificates & Analytics) Complete

**Date:** 2026-07-22
**Status at handoff:** Phases 0, 1, 2, 3, 4 complete and verified. Phase 5 (Notifications
& Polish) is next.

---

## 0. FIRST THING TO DO IN THE NEW SESSION

Read these governing docs before touching anything (standing rule for this repo). All were
updated at the end of this session, so they're current:

- `docs/SRS.md` — product requirements
- `docs/ARCHITECTURE.md` — Clean Architecture + CQRS layout, module status table (Certificates + Analytics now ✅)
- `docs/DATABASE.md` — schema, business rules, migrations (§7b is the new Phase 4 section)
- `docs/API_CONVENTIONS.md` — endpoint/error/validation conventions
- `docs/CODING_STANDARDS.md` — patterns to match
- `docs/IMPLEMENTATION_ROADMAP.md` — phase plan + what's done/next (Phase 4 filled in)

---

## 1. What this project is

**CodeForge Academy** — a live-cohort programming LMS. ASP.NET Core 8 backend (Clean
Architecture, CQRS via MediatR + FluentValidation, EF Core + PostgreSQL) and a Next.js 15 /
React 19 frontend (bilingual EN/AR, LTR/RTL). Egypt-based, EGP. Delivery is **live cohorts**,
not self-paced video.

Solution: `CodeForge.slnx` → `src/CodeForge.{Domain,Application,Infrastructure,Api}`.
Frontend: `frontend/`. Tests: `tests/CodeForge.UnitTests` (xUnit, 48 passing).

Under git on `main`. This session's work is in small reviewable commits (schema → certificates
module → analytics module → tests → frontend → verification bugfix → docs).

## 2. What Phase 4 delivered

**Certificates** — two-tier (completion/participation), **admin-reviewed** issuance:
- Eligibility rule (confirmed with the user): Completion = attendance rate ≥ the course's
  threshold **AND** every non-practice assessment passed (each by its own pass score);
  otherwise Participation.
- Platform default attendance threshold is **75%** (`CertificateDefaults`); each course can
  override via `courses.completion_attendance_threshold` (set through the admin `PUT /courses/{id}`,
  now extended with `completionAttendanceThreshold`).
- `CertificateEligibilityCalculator` (pure, unit-tested) is the single source of truth;
  `CourseEligibilityEvaluator` feeds it by reusing the **exact** gradebook attendance +
  assessment-pass logic, so a certificate can never disagree with the gradebook.
- A certificate is a stored record per enrollment (unique), with a human serial
  (`CF-2026-XXXXXX`), an opaque `verification_code`, and **snapshotted** attendance/assessment
  metrics at issue time (later data edits don't rewrite issued certificates).
- Public verify-by-code endpoint returns a minimal privacy-conscious payload. Revoke keeps the
  row for audit; the code still verifies but as invalid.

**Analytics** — computed read-side, **no new tables**:
- Admin business dashboard (`/analytics/admin/business`): students, published courses/tracks,
  active enrollments, pending requests, approved revenue, leads, open cohorts, top-courses-by-
  revenue, enrollments-by-month.
- Admin academic dashboard (`/analytics/admin/academic`): certificates by tier, assessment pass
  rate, assignment/submission counts, per-course table.
- Instructor dashboard (`/analytics/instructor`): scoped to the caller's assigned courses.
- Pure `AnalyticsCalculator` for pass-rate math (unit-tested).

**Frontend:** admin analytics page (`/[locale]/admin/analytics`), instructor analytics summary
on `/[locale]/instructor`, per-course certificate candidate roster + issue/revoke + threshold
config on the instructor course page, student `/[locale]/my-certificates`, public
`/[locale]/verify`. Nav links added (incl. signed-out "Verify"). ~90 new i18n keys per language.

## 3. Key files

- Schema: `src/CodeForge.Domain/Entities/Entities.cs` (`Certificate` entity; `Course.CompletionAttendanceThreshold`),
  `src/CodeForge.Infrastructure/Data/CodeForgeDbContext.cs`, migration
  `20260722130455_AddCertificates`.
- Constants: `Application/Common/Constants/CertificateTiers.cs`, `CertificateDefaults.cs`.
- Certificates module: `src/CodeForge.Application/Certificates/` (Common has the calculator,
  evaluator, DTOs, mapping, code generator; use-case folders for GetCourseCertificateCandidates,
  IssueCertificate, RevokeCertificate, GetMyCertificates, GetCertificateById, VerifyCertificate),
  `src/CodeForge.Api/Controllers/CertificatesController.cs`.
- Analytics module: `src/CodeForge.Application/Analytics/` (Common: AnalyticsCalculator + DTOs;
  GetAdminBusinessDashboard, GetAdminAcademicDashboard, GetInstructorDashboard),
  `src/CodeForge.Api/Controllers/AnalyticsController.cs`.
- Course threshold plumbing: `Courses/UpdateCourse/*`, `Courses/Common/CourseDetailDto.cs` +
  `CourseMapping.cs`, `CoursesController.cs`.
- Tests: `tests/CodeForge.UnitTests/Certificates/`, `tests/CodeForge.UnitTests/Analytics/`.
- Frontend: `frontend/lib/api.ts` (+ `getCourseById`/`updateCourse`, certificates, analytics),
  `frontend/lib/i18n.ts` (`certificates` + `analytics` sections, en+ar),
  `frontend/app/[locale]/{admin/analytics,my-certificates,verify}/page.tsx`,
  `frontend/app/[locale]/instructor/page.tsx`,
  `frontend/app/[locale]/instructor/courses/[courseId]/page.tsx`,
  `frontend/components/RoleNav.tsx`.

## 4. Verification done (real API + browser, not just build)

Both admin dashboards render live seeded data. Full certificate lifecycle confirmed: candidate
tier computation (both Completion and Participation cases), issue, public verify (valid),
duplicate-issue rejection (400), student "my certificates" view, revoke → verify-as-invalid.
Authorization spot-checked (student→business 403, admin→instructor-dashboard 403 via
`InstructorOnly`, student→issue 403). Bilingual EN/AR (RTL) confirmed on the verify page.

**One real bug found + fixed in verification:** the revenue-by-course query grouped by a joined
`Course.Title`, which Npgsql can't translate (runtime 400) — fixed to group by scalar `course_id`
then attach titles in a second query.

## 5. Known state / gotchas

- **Dev DB has two leftover test certificates** from verification (Student Two — revoked; Track
  Student — valid participation) in "Python Fundamentals". Harmless; delete if you want a clean
  slate. The course's threshold was reset to `NULL` after testing.
- **Attendance is 0% for the seeded students** because the single seeded live session's
  `scheduled_at` predates the cohort `start_date` (documented since Phase 3 — it's correct
  behavior, not a bug). That's why the completion path was verified by temporarily setting the
  course threshold to 0, not by faking attendance.
- Certificate "download" is browser print-to-PDF (`window.print()`), not a server-rendered
  branded PDF — deliberately deferred (see roadmap "out of scope").
- Admin course-editing UI is still minimal (there are no dedicated `/admin/*` CRUD pages beyond
  the new analytics page); the threshold is edited inline on the instructor course page. A full
  admin console remains unbuilt from earlier phases.
- EF `10622` global-query-filter warnings persist (cosmetic, pre-existing).

## 6. Suggested next steps

- **Phase 5 (Notifications & Polish):** WhatsApp Business Cloud API on the existing channel-
  agnostic layer; a notification event catalog (a "certificate issued" event is a natural first);
  recording storage upgrade (external links → signed URLs); hosting/deployment decision — which
  also unblocks the **Phase 3 auto-grader** (self-host Piston/Judge0 once Docker/hosting exists).
- Or backfill the missing **admin console** (course/track/cohort/coupon CRUD UI) that earlier
  phases built on the backend but never fully surfaced in the frontend.
- Optionally a branded server-rendered PDF for certificates.

## 7. How to run

- API: `dotnet run --project src/CodeForge.Api` → http://localhost:5205 (`/swagger`). Secrets in
  User Secrets (`codeforge-api-secrets`).
- Frontend: `cd frontend && npm run dev` → http://localhost:3000.
- Postgres localhost:5432, db `codeforge_db`. Admin `admin@codeforge.academy` / `AdminPass123!`;
  student `student2@example.com` / `Student2Pass!23`.
- `dotnet test CodeForge.slnx` (48 tests) and `cd frontend && npx tsc --noEmit` both green.
