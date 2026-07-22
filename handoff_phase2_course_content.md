# Session Handoff — Phase 2 (Course Content) Complete

**Date:** 2026-07-18
**Status at handoff:** Phases 0, 1, 2 complete and verified. Phase 3 is next.

---

## 0. FIRST THING TO DO IN THE NEW SESSION

Read these governing docs before touching anything (this is the standing rule for this repo):

- `docs/SRS.md` — product requirements (from the discovery interview)
- `docs/ARCHITECTURE.md` — Clean Architecture + CQRS layout, module status table
- `docs/DATABASE.md` — schema, business rules, migration history
- `docs/API_CONVENTIONS.md` — endpoint/error/validation conventions
- `docs/CODING_STANDARDS.md` — patterns to match
- `docs/IMPLEMENTATION_ROADMAP.md` — phase plan + what's done/next

These are the source of truth. If this handoff ever disagrees with them, the docs win (and they were updated at the end of this session, so they're current).

---

## 1. What this project is

**CodeForge Academy** — a live-cohort programming LMS (student portal + instructor portal). ASP.NET Core 8 backend (Clean Architecture, CQRS via MediatR + FluentValidation, EF Core + PostgreSQL) and a Next.js 15 / React 19 frontend (bilingual EN/AR, LTR/RTL). Egypt-based, EGP currency. Delivery is **live cohorts** (batches with schedules), not self-paced video.

Solution: `CodeForge.slnx` → `src/CodeForge.{Domain,Application,Infrastructure,Api}`. Frontend: `frontend/`. Tests: `tests/CodeForge.UnitTests` (xUnit).

---

## 2. How to run

**Backend** (from repo root):
```
dotnet run --project src/CodeForge.Api
```
→ `http://localhost:5205` (Swagger at `/swagger`). Needs local Postgres running.

**Frontend**:
```
cd frontend && npm run dev
```
→ `http://localhost:3000`

**Config / secrets** live in **`dotnet user-secrets`** (project `src/CodeForge.Api`), NOT in appsettings (those keys are intentionally blank — moved out during Phase 0 hardening, never committed). View them with:
```
dotnet user-secrets list --project src/CodeForge.Api
```
Keys present: `ConnectionStrings:DefaultConnection`, `JwtSettings:Secret`, `AdminSeed:Email`, `AdminSeed:Password`, `AdminSeed:FullName`. The dev DB password is a throwaway (`123`) — rotate before any shared/prod use.

**Test accounts:**
- Admin: `admin@codeforge.academy` / `ChangeMe!Admin123`
- Student (enrolled in "Python Fundamentals"): `student2@example.com` / `Student2Pass!23`
- `student1@example.com` exists but its enrollment was cancelled/refunded during testing — use student2 for enrolled-student views.

**Useful URLs:** `/en/catalog`, `/en/login`, `/en/instructor` (admin/instructor), `/en/dashboard` + `/en/my-courses/{courseId}` (student).

---

## 3. What was built THIS session (Phase 2 — Course Content)

Renamed/merged the dormant Phase-0 scaffolding (`Section`/`Lecture`/`Resource`/`LiveSession`/`LectureProgress` — all had zero rows and zero Application code) into a coherent live-cohort content model. **This was a clean rename/merge, not a data migration** (nothing to preserve). See `docs/DATABASE.md` §6 for the full mapping.

**Data model changes** (migration `20260717222216_AddModulesSessionsMaterials`, applied to dev DB):
- `sections` → `modules` (+ `description`)
- `lectures` + `live_sessions` → **`sessions`** (one type-discriminated table: `live` / `in_person` / `recorded_lesson`, with `scheduled_at`, `join_link`, `location`, `video_url`, `instructor_id`, `order_index`)
- `resources` → `materials` (type `file`/`text`/`link`, + `order_index`; `chk_resource_target` → `chk_material_target`)
- `lecture_progress` → `session_progress`
- `live_sessions` table dropped

**Backend modules added** (all vertical slices under `src/CodeForge.Application/`):
- `Modules/` — CRUD + reorder
- `Sessions/` — CRUD + reorder, type-specific validation (live⇒scheduledAt+joinLink; in_person⇒scheduledAt+location; recorded_lesson⇒videoUrl)
- `Materials/` — file/text/link, attached to a module XOR a session
- `Announcements/` — first real Application/Api layer (schema already existed); platform-wide (admin) or course-scoped (instructor)
- `MyCourses/` — **enrollment-gated** student read views: `GET /my-courses/{courseId}/content` and `GET /my-courses/upcoming-items`

**Controllers added:** `ModulesController`, `SessionsController`, `MaterialsController`, `AnnouncementsController`, `MyCoursesController`. Old `SectionsController` + `Sections/` module deleted.

**Shared helpers introduced (reuse these, don't re-duplicate):**
- `src/CodeForge.Application/Common/CourseContentAuthorization.cs` — `EnsureCanManage` / `EnsureCanView` (admin always; instructor if assigned; student if actively enrolled — view only). Requires `course.Instructors` (and for view, `course.Enrollments`) loaded.
- `src/CodeForge.Application/Common/ActivityLogFactory.cs` — generic activity-log builder (the Courses module has its own older entity-typed one; leave it).
- `src/CodeForge.Application/Materials/Common/MaterialParentResolver.cs` — resolves module-or-session down to the owning Course for auth.
- `IFileStorageService.SaveCourseMaterialAsync` added alongside the existing payment-proof method (`LocalFileStorageService`, saves to `wwwroot/uploads/materials`).

**Frontend added:**
- `frontend/lib/api.ts` — extended `apiFetch`/`apiFetchForm` with `token`; added all Phase-2 bindings.
- `frontend/components/RoleNav.tsx` — role-aware nav (sign out, dashboard/instructor links), wired into `[locale]/layout.tsx`.
- Instructor: `/[locale]/instructor` (course list) → `/instructor/courses/[courseId]` (modules + announcements) → `/instructor/courses/[courseId]/modules/[moduleId]` (sessions + materials, type-aware forms).
- Student: `/[locale]/dashboard` (upcoming items) and `/[locale]/my-courses/[courseId]` (content view).
- i18n: `instructor` + `student` sections added to both `en` and `ar` in `frontend/lib/i18n.ts`.

**Verified end-to-end** via real API + real browser: all 3 session types, all 3 material types, module create through the UI, enrolled student sees content, non-enrolled student blocked (401 surfaced in UI). Not just typechecked.

---

## 4. Bug fixed this session (carried over from a crashed session)

`chk_enrollment_status` was a hand-authored CHECK constraint from the original `schema.sql` that only allowed `'active'`/`'expired'`. Phase 1 added `'cancelled'`/`'refunded'` to `EnrollmentStatuses`, which broke the cancel/refund flow with a 500. Fixed by migration `20260717193822_FixEnrollmentStatusCheckConstraint` (widens the constraint via raw SQL). Applied and confirmed working. **Lesson for future work:** several enum-like columns still have legacy hand-written CHECK constraints not expressed in the EF model — see `docs/DATABASE.md` §4a. Before adding a new allowed value to any status constant, add a migration to widen the matching DB constraint.

---

## 5. Current state / migrations

Applied migrations (in order):
1. `20260629194500_InitialCreate`
2. `20260717191353_AddTracksCohortsCoupons`
3. `20260717193822_FixEnrollmentStatusCheckConstraint`
4. `20260717222216_AddModulesSessionsMaterials`

`dotnet ef migrations has-pending-model-changes` → none. Full solution builds clean (0 warnings/errors). 11 unit tests pass.

**NOT under version control:** `git rev-parse` reports this is not a git repo (the `.git` dir is empty/broken). If you want history, `git init` first. Worth raising with the user before any commit-based workflow.

---

## 6. Honest state of things (tech debt — discussed with user, not yet actioned)

Roadmap-expected gaps (Phases 3–5, not debt): attendance, quizzes/assignments/exams, certificates, real notifications, admin reporting.

Actual shortcuts still open:
- **No real email** — password reset etc. are logged to console via a dev sender, not sent. (To get a student's temp/reset token during testing, `POST /auth/forgot-password` then grep the API console log for the `[DEV EMAIL] … token=` line and URL-decode it.)
- **No admin user-management UI/endpoints** — can't create instructors or reset passwords except via DB or the forgot-password-log trick. Real gap.
- **Payment proofs + materials** stored on local disk as public static files (`wwwroot/uploads/...`), not signed/private (SRS wants Cloudflare R2 signed URLs).
- **Thin tests** (11 total) and **no CI** — everything is verified by hand each session.
- **Secrets** are throwaway dev values.
- **Frontend is deliberately bare** (plain CSS, minimal UX polish) — this was an agreed tradeoff, not neglect.

User's phrasing at end of session: "everything feels half-assed." Agreed read: partly phase-boundary (fine), partly the real shortcuts above. Suggested highest-impact hardening if they want to pause feature work: **admin user management, real email sending, automated tests + CI.**

---

## 7. Where to pick up

Ask the user which direction they want:
- **(a)** Continue the roadmap → **Phase 3: Attendance & Assessments** (manual per-session attendance marking + reporting; then quizzes/assignments/exams with the Python auto-grader). See `docs/IMPLEMENTATION_ROADMAP.md`.
- **(b)** Pause and harden → admin user management + real email + tests/CI (addresses the "half-assed" feeling).

Follow the same working rhythm used all along: design into the docs first → build vertical slices matching existing patterns → build after each module → verify against the real running API/browser, not just typecheck → update the `docs/` files as plans change.

### Gotchas for the environment
- Shell is Git Bash on Windows. **`curl` file uploads (`-F file=@...`) need a Windows-style path**, not `/tmp` — use the scratchpad dir. `/tmp` paths fail with "Failed to open/read local data."
- Background `dotnet run` processes don't survive session restarts cleanly; a killed API shows as a "failed" background task — that's expected, not an error.
- Postgres must be running locally before `dotnet run`.
