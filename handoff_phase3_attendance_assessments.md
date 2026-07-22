# Session Handoff — Phase 3 (Attendance & Assessments) Complete

**Date:** 2026-07-22
**Status at handoff:** Phases 0, 1, 2, 3 complete and verified. Phase 4 is next.

---

## 0. FIRST THING TO DO IN THE NEW SESSION

Read these governing docs before touching anything (standing rule for this repo):

- `docs/SRS.md` — product requirements
- `docs/ARCHITECTURE.md` — Clean Architecture + CQRS layout, module status table
- `docs/DATABASE.md` — schema, business rules, migration history (§7 is the new
  Phase 3 section)
- `docs/API_CONVENTIONS.md` — endpoint/error/validation conventions
- `docs/CODING_STANDARDS.md` — patterns to match
- `docs/IMPLEMENTATION_ROADMAP.md` — phase plan + what's done/next (Phase 3 filled in
  this session)

These are the source of truth. All of them were updated at the end of this session, so
they're current.

---

## 1. What this project is

**CodeForge Academy** — a live-cohort programming LMS (student portal + instructor
portal). ASP.NET Core 8 backend (Clean Architecture, CQRS via MediatR +
FluentValidation, EF Core + PostgreSQL) and a Next.js 15 / React 19 frontend
(bilingual EN/AR, LTR/RTL). Egypt-based, EGP currency. Delivery is **live cohorts**
(batches with schedules), not self-paced video.

Solution: `CodeForge.slnx` → `src/CodeForge.{Domain,Application,Infrastructure,Api}`.
Frontend: `frontend/`. Tests: `tests/CodeForge.UnitTests` (xUnit).

**The project is now under git** (as of the prior session) — `main` branch, commits go
back to the Phase 0–2 snapshot. This session's work is broken into small, reviewable
commits (schema, then each module, then frontend, then bugfixes) rather than one big
end-of-session commit — normal `git log` archaeology works now.

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

Both are also wired into `.claude/launch.json` as `api-dev` (port 5205) and
`frontend-dev` (port 3000) for the Browser-pane preview tools — added this session.

**Config / secrets** live in **`dotnet user-secrets`** (project `src/CodeForge.Api`).
View them with:
```
dotnet user-secrets list --project src/CodeForge.Api
```
Keys present: `ConnectionStrings:DefaultConnection`, `JwtSettings:Secret`,
`AdminSeed:Email`, `AdminSeed:Password`, `AdminSeed:FullName`.

**Test accounts:**
- Admin: `admin@codeforge.academy` / `AdminPass123!` (password was changed this
  session from the originally-seeded `ChangeMe!Admin123` — `mustChangePassword` is
  now `false`)
- Student: `student2@example.com` / `Student2Pass!23` (enrolled in "Python
  Fundamentals")
- `student1@example.com` exists but its enrollment was cancelled/refunded — use
  student2 for enrolled-student views.

**Useful URLs:** `/en/instructor/courses/{courseId}/modules/{moduleId}` (instructor
attendance/assessment/assignment authoring), `/en/my-courses/{courseId}` (student
course view + grades toggle), `/en/my-courses/{courseId}/assessments/{id}` (quiz/exam
taking), `/en/my-courses/{courseId}/assignments/{id}` (assignment submission).

---

## 3. What was built THIS session (Phase 3 — Attendance & Assessments)

Full backend + frontend for: manual attendance marking/reporting, quizzes, exams,
code assignments with an auto-grader integration point, instructor manual grading,
and a gradebook for both roles. See `docs/IMPLEMENTATION_ROADMAP.md`'s Phase 3 section
for the full endpoint table — summary here:

**Data model** (`docs/DATABASE.md` §7): the dormant Phase 0 `quizzes` cluster (zero
rows, zero Application code, still keyed to `course_id`) was altered in place to serve
both quizzes and exams (`Type` discriminator, reattached to `module_id`) — three
migrations: `20260722084131_AddAttendanceAssessments` (main schema — `attendance_records`,
the `assignments` cluster, the `quizzes` alterations), `20260722085233_AddAssessmentOrdering`
(quiz/assignment `order_index`, found missing partway through), and
`20260722095447_AddQuizOptionOrdering` (quiz option `order_index`, found missing
during end-to-end verification).

**Backend modules added:**
- `Attendance/` — mark (bulk roster upsert), roster view, course report, student's
  own view. Rate computed from the enrollment's cohort date window, never stored.
- `Assessments/` — quiz+exam CRUD, question/option CRUD (single-correct-answer MCQ),
  reordering, the student attempt flow (start/submit/results), instructor results
  view.
- `Assignments/` — assignment/test-case CRUD, student submission (calls
  `ICodeExecutionService`), instructor manual grading, submission history.
- `Gradebook/` (+ `MyCourses/GetMyCourseGrades`) — per-student aggregate view for both
  roles.

**Controllers added:** `AttendanceController`, `AssessmentsController`,
`AssignmentsController`, `GradebookController`.

**Frontend added:** attendance/assessment/assignment authoring panels on the
instructor module page, a gradebook table on the instructor course page, a
quiz/exam-taking page and assignment-submission page under `my-courses/`, a "my
grades" toggle on the student course view. New `.table` CSS utility class (didn't
exist before).

**Verified end-to-end** via the real API + a real browser session: attendance marking
persisted correctly; quiz + exam authored (including question/option MCQ authoring)
and taken by a student with correct auto-grading; exam single-attempt enforcement
confirmed (second attempt rejected); assignment authored with test cases, submitted
by a student, gracefully marked `auto_grading_status: failed` (see §5), manually
graded by the instructor; gradebook reflected all of the above correctly for both
roles; bilingual (en/ar) rendering confirmed on every new student-facing page.

---

## 4. Real bugs found and fixed during end-to-end verification

This is the most important section for whoever picks this up next — these were all
caught by actually driving the app, not by typecheck/build, which is exactly why the
project's "verify end-to-end" convention exists.

1. **Every quiz/exam submission crashed with a 500.** `SubmitAttemptCommandHandler`
   added a new `QuizAnswer` via `attempt.Answers.Add(new QuizAnswer{...})` on an
   already-tracked `attempt`. EF Core's graph-tracking heuristic — apparently
   confused by the client-generated GUID key looking "non-default" — marked the new
   entity **Modified** instead of **Added**, so EF issued an `UPDATE` for a row that
   didn't exist yet (`DbUpdateConcurrencyException: expected 1 row, affected 0`).
   Root-caused via a temporary diagnostic catch block logging
   `ex.Entries.Select(e => $"{e.Entity.GetType().Name}:{e.State}")`. Fixed by adding
   the new entity via explicit `_context.QuizAnswers.AddRange(...)` before linking it
   into the navigation collection — unambiguous regardless of the parent's tracking
   state. The same at-risk pattern (new child added via `.Add()` on an
   already-tracked parent's collection) was proactively fixed in
   `UpdateQuestionCommandHandler` (options) and `SubmitAssignmentCommandHandler`
   (test results) even though only the quiz-attempt path was directly reproduced.
   **If you see `DbUpdateConcurrencyException` anywhere else, check for this exact
   pattern first.**

2. **`QuizOption` had no explicit ordering.** Cross-query option order was
   non-deterministic (Postgres doesn't guarantee row order without an `ORDER BY`, and
   EF doesn't add one to a plain `Include`d collection automatically) — the manage
   view and the student attempt view could show options in different orders for the
   same question. Grading correctness was never affected (`IsCorrect` stays attached
   to the right option text regardless of display order), but it's a real UX
   consistency bug. Fixed with a new `OrderIndex` column
   (`AddQuizOptionOrdering` migration) and explicit `.OrderBy(o => o.OrderIndex)`
   everywhere options are projected.

3. **An abandoned quiz attempt would permanently lock a student out.**
   `StartAttemptCommandHandler`'s "attempt already in progress" guard checked only
   `SubmittedAt == null`, with no expiry — if a student started an attempt and closed
   the browser without submitting, they could never start a new one. Fixed: the guard
   now only blocks while the quiz's own `TimeLimitMinutes` hasn't elapsed since the
   abandoned attempt's `StartedAt`. (This was actually *discovered* by the frontend's
   own client-side timer auto-submitting a genuinely-abandoned test attempt mid-session
   — confirming that feature works — but the server-side guard still needed the fix
   for the case where the browser tab is simply closed.)

4. **React stale-closure race in the option editor.** The instructor's MCQ
   question/option builder used `setOptions(options.map(...))` (closing over the
   `options` variable) instead of a functional update. Two option-text edits fired in
   quick succession (as automated browser tooling does) could each read the same
   stale `options` snapshot, scrambling which option ended up marked correct.
   Fixed by switching to `setOptions((prev) => prev.map(...))` throughout.

5. **Piston's public API is no longer usable.** Confirmed via a direct `curl` against
   `emkc.org/api/v2/piston/execute`: `{"message":"Public Piston API is now whitelist
   only as of 2/15/2026. Please contact EngineerMan on Discord..."}` — a 401. This
   invalidated the auto-grader engine choice made and confirmed with the user earlier
   in *this same session* (the engine choice was made before this policy change was
   discovered). Per a follow-up decision with the user, assignments now defer to
   manual grading: `DeferredCodeExecutionService` is the active
   `ICodeExecutionService`, throwing immediately so
   `SubmitAssignmentCommandHandler`'s existing catch block sets
   `auto_grading_status = 'failed'` without blocking the submission.
   `PistonCodeExecutionService` is kept fully implemented and registered (just not as
   the active interface binding) — swap it back in
   `Infrastructure/DependencyInjection.cs` if Piston ever whitelists this use case, or
   replace it with a self-hosted engine once Docker/hosting is available.

None of these were caught by `dotnet build`, `dotnet test`, or `npx tsc --noEmit` —
all green throughout. They only surfaced by actually running the flows through a real
browser session and real API calls. This is the strongest argument yet for this
project's "verify end-to-end, not just typecheck" convention.

---

## 5. Current state / migrations

Applied migrations (in order): `20260629194500_InitialCreate`,
`20260717191353_AddTracksCohortsCoupons`,
`20260717193822_FixEnrollmentStatusCheckConstraint`,
`20260717222216_AddModulesSessionsMaterials`, `20260722084131_AddAttendanceAssessments`,
`20260722085233_AddAssessmentOrdering`, `20260722095447_AddQuizOptionOrdering`.
`dotnet ef migrations has-pending-model-changes` → none. Full solution builds clean
(0 warnings/errors). 37 unit tests pass (11 pre-existing + 26 new this session:
`QuizGradingCalculator`, `AttendanceRateCalculator`, `AssignmentGradingCalculator`,
`CreateAssessmentCommandValidator`, `CreateQuestionCommandValidator`).

**Test data left in the dev DB** from verification (harmless, real-looking content,
not cleaned up — feel free to leave it or clear it): a "Python Basics Quiz" (1
question) and "Midterm Exam" (1 question, single attempt used) in Python
Fundamentals' Module 1, and a "Sum Two Numbers" assignment (2 test cases) in the same
module with two submissions from `student2@example.com` (one auto-grading-failed +
manually graded to 95, one an earlier partial state). Attendance was marked on the
"Live Kickoff" session for both enrolled students (though the attendance *rate*
computes to 0% for this specific test data, because that session's `scheduled_at`
predates the cohort's `start_date` — a test-data artifact confirming the cohort-window
filter works correctly, not a bug).

---

## 6. Honest state of things (tech debt — carried over + new)

Carried over from Phase 2 (still true, not actioned):
- No real email — password reset etc. still logged to console via a dev sender.
- No admin user-management UI/endpoints.
- Payment proofs + materials stored on local disk as public static files.
- No CI — everything verified by hand each session.
- Frontend is deliberately bare (plain CSS, minimal UX polish) — still an agreed
  tradeoff, not neglect.

New from this session:
- **No working auto-grader.** Assignments are 100% manually graded right now (see
  §4 point 5). This is a real, if temporary, gap — SRS wants auto-run + manual
  adjustment, and right now it's manual-only. Not a blocker (manual grading was
  always required regardless), but worth resolving before this feels "done."
- **Server-side quiz/exam time-limit enforcement is soft.** The frontend's countdown
  timer auto-submits client-side, but `SubmitAttemptCommandHandler` doesn't itself
  reject a submission that arrives well after the time limit elapsed — a student
  could, in principle, submit a very late answer via a direct API call. Given
  SRS explicitly says "no proctoring," this is a low-priority gap, not a security
  hole, but flagging it for awareness.
- **Attendance status set** (`present`/`absent`/`late`/`excused`) was a reasonable
  default chosen during planning, not something the user was asked to confirm
  explicitly — worth a quick sanity check with them if it matters.

---

## 7. Where to pick up

Ask the user which direction they want:
- **(a)** Continue the roadmap → **Phase 4: Certificates & Analytics** (two-tier
  completion logic consuming the attendance rate + assessment/assignment scores this
  phase computed; admin business + academic dashboards; instructor analytics). See
  `docs/IMPLEMENTATION_ROADMAP.md`.
- **(b)** Resolve the auto-grader gap first — either get Piston whitelisted (contact
  EngineerMan on Discord per their own error message, with a use-case justification),
  install Docker and self-host Judge0/Piston, or pick a different hosted engine.
- **(c)** Pause and harden — the Phase 2 hardening backlog (admin user management,
  real email, tests/CI) is still open and was deferred twice now.

Follow the same working rhythm used all along: design into the docs first → build
vertical slices matching existing patterns → build after each module → **verify
against the real running API/browser, not just typecheck** (this session is the
clearest possible argument for why — five real bugs, zero of them caught by
build/test/typecheck) → update the `docs/` files as plans change.

### Gotchas for the environment
- Shell is Git Bash on Windows; PowerShell is also available.
- Native browser `confirm()`/`alert()` dialogs **block the browser-automation tools
  used in this session** — clicking a "Delete" button (which calls `confirm(...)`)
  will hang `computer`/`navigate`/`get_page_text` indefinitely with no way to dismiss
  the dialog through the automation API. If this happens, open a **new tab**
  (`tabs_create`) rather than fighting the stuck one, and use direct `curl` calls
  against the API for any delete operations you need during testing.
- `dotnet build`/`dotnet ef` commands fail with file-lock errors
  (`MSB3027`/`MSB3021`) if the API dev server is currently running against the same
  `bin/` output — stop the `api-dev` preview server before rebuilding or generating
  migrations, then restart it.
- JWT access tokens expire in 15 minutes — re-`POST /auth/login` if `curl` calls
  start returning empty bodies or 401s partway through a long testing session.
- Postgres must be running locally before `dotnet run`.
