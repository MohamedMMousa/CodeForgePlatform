# CodeForge Academy — Implementation Roadmap

Vertical-slice phases: within each phase, backend endpoints are built first, then the
matching frontend screens immediately after, so every phase ends in something that can
actually be clicked through end-to-end. See `ARCHITECTURE.md` for the module map and
`DATABASE.md` for schema detail.

## Phase 0 — Foundation & Hardening — ✅ COMPLETE

EF migration baseline regenerated (snapshot/Designer fixed, no data change) · central
exception middleware (`ProblemDetails` envelope) · secrets moved to User Secrets +
rotated · refresh/reset tokens hashed at rest · real email sender (SMTP + dev-logging)
· forgot-password no longer leaks the token · idempotent admin seed · rate limiting
(global + `Auth`/`PublicSubmit` policies) · i18n scaffolding (en/ar culture
resolution) · CORS · xUnit test project (11 tests) · Next.js bilingual (RTL/LTR) app
shell with working login, verified end-to-end through the browser against the live
API and database.

## Phase 1 — Catalog & Cohort Enrollment — ✅ COMPLETE

**Goal:** a visitor can browse the public catalog (courses and tracks), see which
batches are open, submit an enrollment request (with an optional coupon) against a
specific course or an entire track bundle, and land in "await next batch" when nothing
is open — matching `SRS.md` §3. Admin can manage tracks, cohorts, and coupons, and
approve/reject/refund enrollments against the correct batch.

### Backend

| Module | Endpoints | Notes |
|---|---|---|
| **Tracks** | `POST/PUT /tracks`, `PUT /tracks/{id}/publish\|archive`, `DELETE /tracks/{id}`, `POST/DELETE /tracks/{id}/courses/{courseId}`, `GET /tracks`, `GET /tracks/{id}` | Admin-only; mirrors `CoursesController`. |
| **Cohorts** | `POST /courses/{courseId}/cohorts`, `PUT /cohorts/{id}`, `PUT /cohorts/{id}/open\|cancel\|complete`, `GET /courses/{courseId}/cohorts`, `GET /cohorts/{id}` | Admin-only. Lifecycle is `draft → open → completed`, or `→ cancelled`. "Full"/"cutoff passed" are computed, not stored — see `DATABASE.md` §4. |
| **Coupons** | `POST/PUT /coupons`, `PUT /coupons/{id}/deactivate`, `GET /coupons`, `GET /coupons/{id}`, `POST /coupons/validate` | Admin-only except `validate`, which is public (rate-limited) for the enroll form's live discount preview. |
| **Leads** | `POST /leads` (public, optional `courseId` for "notify me"), `GET /leads` (admin), `PUT /leads/{id}/mark-contacted` (admin) | New module — the `leads` table existed in the schema with no Application/Api code until now. |
| **Catalog** (extend) | `GET /catalog/tracks`, `GET /catalog/tracks/{slug}`, `GET /catalog/courses/{slug}` extended with an open-cohorts list (seats left, cutoff, start date) | Public, drives the "Upcoming batches" / "await next batch" UI. |
| **EnrollmentRequests** (rework) | `Submit` now takes `courseId` **or** `trackId` + optional `couponCode`; resolves the target open cohort(s) at submission and snapshots pricing. `Approve`/`Reject` updated for cohort-based, possibly-multi-course creation and coupon usage accounting. | See `DATABASE.md` §4 for the exact business rules. |
| **Enrollments** (new action) | `PUT /enrollments/{id}/cancel` (admin) | Cancels/refunds — sets `status`, `cancelled_at`, `cancellation_reason`, `cancelled_by_id`. |

Data model: see `DATABASE.md` §2 for the full column-level design
(`tracks`, `track_courses`, `cohorts`, `coupons`, `enrollment_request_cohorts`, plus
the `enrollment_requests` / `enrollments` / `leads` extensions).

### Frontend

- Public **catalog** (`/[locale]/catalog`) — courses + tracks, search, brand-styled
  cards.
- **Course/track detail** pages — description, upcoming batches with seats-left
  badges, "Enroll in this batch" or "await next batch" + notify-me form when nothing
  is open.
- **Enrollment flow** — applicant details, coupon code entry with live discount
  preview (`POST /coupons/validate`), payment-proof upload, submit.
- **Contact/lead page** (`/[locale]/contact`) — the standalone public lead form.

### Verified (end-to-end, via API + live browser)

Course/track CRUD + publish; cohort create/open, capacity + cutoff computed
correctly; coupon create/validate/apply, usage increments at submission and
decrements on rejection; **course enrollment** (submit → target-cohort resolution →
approve → single Enrollment) and **track bundle enrollment** (submit → resolves
one open cohort per constituent course → approve → one Enrollment per course, all
from a single request/payment) both confirmed working; cohort-full correctly
triggers the "no open batch" rejection driving the frontend's await-next-batch UI;
enrollment cancel/refund correctly frees the cohort seat; leads/notify-me flow
verified through the actual browser end-to-end (form → API → DB), including the
coupon discount preview updating live in the enroll form. Bilingual (en/ar) RTL
rendering confirmed on the catalog.

**Bug found and fixed during verification:** a legacy hand-authored Postgres
`CHECK` constraint (`chk_enrollment_status`, predating the EF model — see
`DATABASE.md` §4a) rejected the new `cancelled`/`refunded` enrollment statuses;
fixed via migration `20260717193822_FixEnrollmentStatusCheckConstraint`.

**Not verified:** the payment-proof file upload could not be driven through
browser automation (no file-picker tool support in this environment) — that
specific step was instead verified via direct multipart API calls against the
same endpoint the frontend targets, which succeeded.

### Out of scope for Phase 1 (later phases)

Modules/content authoring (Phase 2), attendance/assessments (Phase 3), certificates/
analytics (Phase 4), WhatsApp notifications (Phase 5), admin dashboards for the new
modules beyond basic CRUD, partial track-bundle enrollment (see `DATABASE.md` §4
"strict rule" note), background jobs to auto-transition cohort status.

## Phase 2 — Course Content — ✅ COMPLETE

**Goal:** instructors (and admins) can build out a course's content — modules
containing live/in-person/pre-recorded sessions and materials — and enrolled
students can view it: browse modules, see session schedule/join links, watch
recordings and pre-recorded lessons, download materials, read announcements, and
see an "upcoming items" list on their dashboard. Matches `SRS.md` §2, §10, §11.

Data model: see `DATABASE.md` §6 — `sections`/`lectures`/`resources` are renamed
and merged into `modules`/`sessions`/`materials` (both were dormant, zero-content
scaffolding, so this is a clean rename/merge, not a data migration), and
`live_sessions` is dropped (merged into `sessions`' `live` type).

**Verified end-to-end** via the real API + a real browser session: module/session/
material CRUD (all three session types and all three material types) as admin;
enrollment-gated read access for students (`/my-courses/{id}/content`,
`/my-courses/upcoming-items`) confirmed both for an enrolled student (sees content)
and a non-enrolled one (blocked with a 401 surfaced correctly in the UI); the
instructor authoring UI and student course view were driven live in-browser, not
just typechecked.

### Backend

| Module | Endpoints | Notes |
|---|---|---|
| **Modules** (rework of Sections) | `POST/PUT /courses/{courseId}/modules`, `PUT /modules/{id}`, `DELETE /modules/{id}`, `PUT /courses/{courseId}/modules/reorder`, `GET /courses/{courseId}/modules`, `GET /modules/{id}` | Admin + assigned instructor. Mirrors the existing `SectionsController` pattern. |
| **Sessions** | `POST /modules/{moduleId}/sessions`, `PUT /sessions/{id}`, `DELETE /sessions/{id}`, `PUT /modules/{moduleId}/sessions/reorder`, `GET /modules/{moduleId}/sessions`, `GET /sessions/{id}` | Admin + assigned instructor (SRS §11 — both can manage schedule). Type-specific validation (`live` needs `scheduledAt`+`joinLink`, `in_person` needs `scheduledAt`+`location`, `recorded_lesson` needs `videoUrl`). |
| **Materials** | `POST /modules/{moduleId}/materials`, `POST /sessions/{sessionId}/materials`, `PUT /materials/{id}`, `DELETE /materials/{id}`, `GET` per parent | Admin + assigned instructor for mutations; enrolled students for read (access-gated — see below). |
| **Announcements** | `POST /announcements` (admin: any; instructor: own courses only), `PUT/DELETE /announcements/{id}`, `GET /announcements?courseId=` | `courseId` null = platform-wide. Read is public-ish (any authenticated user); write is role/ownership-gated. |
| **Student course content** (new read endpoints) | `GET /my-courses/{courseId}/content` (modules + sessions + materials, only if the student has an active enrollment in that course), `GET /my-courses/upcoming-items` | Access-gated on active `Enrollment`, not just role — a key Phase 2 authorization concern distinct from admin/instructor CRUD. |

### Frontend

- **Instructor authoring** (`/[locale]/instructor/courses/[id]`) — manage modules,
  add/edit sessions (type-aware form), attach materials, post course-scoped
  announcements. Simple forms, consistent with existing admin UI (per your
  confirmation — no rich editor/drag-and-drop this phase).
- **Student course view** (`/[locale]/my-courses/[id]`) — module/session list,
  join-link/location display for upcoming sessions, recording + pre-recorded
  lesson playback (external video links), material downloads, announcements.
- **Student dashboard "upcoming items"** widget.

### Out of scope for Phase 2 (later phases)

Attendance marking (Phase 3), quizzes/assignments/exams (Phase 3), certificates
(Phase 4), read/seen tracking on announcements, full calendar view (SRS explicitly
scoped this to a simple list), private/signed recording storage (still external
links per SRS — upgrade planned but unscheduled).

## Phase 3 — Attendance & Assessments — ✅ COMPLETE

**Goal:** instructors can mark per-session attendance and see a course attendance
report; author quizzes and formal exams (MCQ, timed, attempt-limited) and code
assignments (instructor test cases, Python auto-grading + manual override); students
attend/take/submit and see their own results; both roles see a gradebook combining
attendance rate with assessment/assignment scores. Matches `SRS.md` §7, §8, §9 (partial
— full two-tier certificate logic is Phase 4).

Data model: see `DATABASE.md` §7 — the dormant `quizzes` cluster (Phase 0 schema, zero
Application/Api code) is altered in place to serve both quizzes and exams (`Type`
discriminator, reattached to `module_id` instead of `course_id`), mirroring how
Phase 2 merged session types; `attendance_records` and the `assignments` cluster are
fully new.

### Backend

| Module | Endpoints | Notes |
|---|---|---|
| **Attendance** | `PUT /sessions/{id}/attendance` (bulk roster upsert), `GET /sessions/{id}/attendance` (roster + current marks), `GET /courses/{id}/attendance-report` (instructor/admin, per-student rate), `GET /my-courses/{id}/attendance` (student) | Admin + assigned instructor for marking; rate computed from the enrollment's cohort date window, never stored (`AttendanceRateCalculator`). |
| **Assessments** (quiz + exam) | `POST/PUT/DELETE /assessments`, `PUT /modules/{id}/assessments/reorder`, `POST/PUT/DELETE` on `/assessments/{id}/questions`, `PUT .../questions/reorder`, `GET /modules/{id}/assessments` \| `/assessments/{id}` (manage view, correct answers), `GET /assessments/{id}/attempt` (student view, answers stripped + optional randomization), `POST /assessments/{id}/attempts` (start), `PUT /attempts/{id}/submit` (auto-grades MCQ), `GET /assessments/{id}/my-attempts` \| `/attempts/{id}` \| `/assessments/{id}/results` | Exams are validated to `max_attempts = 1` at creation. A stale in-progress attempt only blocks a new start while the quiz's own time limit hasn't elapsed — otherwise an abandoned attempt would permanently lock a student out. |
| **Assignments** | `POST/PUT/DELETE /assignments`, `PUT /modules/{id}/assignments/reorder`, `POST/PUT/DELETE` on `/assignments/{id}/test-cases`, `PUT .../test-cases/reorder`, `GET /modules/{id}/assignments` \| `/assignments/{id}` (manage) \| `/assignments/{id}/submission` (student, hidden test cases excluded), `POST /assignments/{id}/submissions` (auto-runs via `ICodeExecutionService`), `PUT /submissions/{id}/grade` (manual override), `GET /assignments/{id}/my-submissions` \| `/submissions/{id}` \| `/assignments/{id}/submissions` (instructor grading queue) | Auto-grading failure (currently the default state — see below) never blocks a submission from saving; it just leaves `auto_grading_status = 'failed'` and manual grading as the only score source. |
| **Gradebook** | `GET /my-courses/{id}/grades` (student), `GET /courses/{id}/gradebook` (instructor/admin, full roster) | Shares a `GradebookCalculator` that aggregates best quiz/exam score+pass and best assignment final score per student. |

### Frontend

- **Instructor** (`/[locale]/instructor/courses/[id]/modules/[moduleId]`) — attendance
  marking panel per session, assessment authoring (type-aware form + question/option
  builder, single-correct-answer enforced via radio group), assignment authoring
  (test-case builder), submission grading (manual score + feedback). Course-level
  gradebook table on the course page.
- **Student** — quiz/exam-taking flow (`/my-courses/[id]/assessments/[assessmentId]`,
  countdown timer with client-side auto-submit, disable-copy-paste UX deterrent,
  correct-answer reveal only after submission), assignment submission
  (`/my-courses/[id]/assignments/[assignmentId]`, code textarea, sample test cases,
  auto-graded results with hidden-case results limited to pass/fail), a "my grades"
  toggle on the course view.

### Verified (end-to-end, via API + live browser)

Attendance marking persisted and reflected in the gradebook; quiz creation +
question/option authoring + student attempt + correct MCQ auto-grading; exam creation
+ single-attempt enforcement (confirmed a second attempt is rejected) + auto-grading;
assignment creation + test cases + student code submission + graceful auto-grading
failure + instructor manual override, all reflected correctly in both the instructor
gradebook and the student's own grades view; bilingual (en/ar) rendering confirmed on
the new student-facing pages.

**Bugs found and fixed during verification** (see `handoff_phase3_attendance_assessments.md`
for full detail): `QuizOption` had no explicit ordering column, so option order was
non-deterministic across queries (fixed, `AddQuizOptionOrdering` migration);
`SubmitAttemptCommandHandler`/`UpdateQuestionCommandHandler`/
`SubmitAssignmentCommandHandler` added new child entities via a navigation collection
on an already-tracked parent, which EF Core's graph-tracking heuristic misjudged as
Modified instead of Added — this crashed **every** quiz/exam submission with a 500
before the fix (explicit `DbSet.AddRange` now used instead); an abandoned
in-progress attempt had no expiry and would have permanently locked a student out of
retaking an assessment (fixed — only blocks while the quiz's time limit hasn't
elapsed); a React stale-closure race in the option editor could scramble option order
under rapid edits (fixed with functional `setState`).

**Auto-grader status:** Piston's public API went whitelist-only mid-phase (see
`ARCHITECTURE.md` §7) — assignments currently rely on 100% manual grading, which was
always a required path per `SRS.md` §7 regardless of auto-grading availability.

### Out of scope for Phase 3 (later phases)

Two-tier certificate logic and thresholds (Phase 4, consumes the attendance rate and
pass/fail computed here), admin cross-course analytics dashboards (Phase 4), a
working auto-grader engine (blocked on Piston whitelisting, self-hosting, or a
different provider), multiple-correct-answer MCQ (single-correct only), additional
auto-grader languages (Python only, per SRS).

## Phase 4 — Certificates & Analytics — ✅ COMPLETE

**Goal:** the academy can issue two-tier credentials and see how the business and the
teaching are doing. Admin reviews who is eligible per course and issues a Completion or
Participation certificate (stored, with a serial + public verification code); students see
and print/verify their certificates; anyone can verify a certificate by its code without
logging in. Admins get business + academic dashboards, instructors get analytics for their
own courses. Matches `SRS.md` §9 (certification) and the reporting intent of §12.

Data model: see `DATABASE.md` §7b — one nullable `courses.completion_attendance_threshold`
column (null = platform default 75) and a new `certificates` table. Analytics adds **no**
tables; every dashboard number is computed read-side.

**Eligibility rule (confirmed with the user):** Completion = attendance rate ≥ the course
threshold **AND** every non-practice assessment passed (each by its own pass score);
otherwise Participation. `CertificateEligibilityCalculator` is the single source of truth,
fed by `CourseEligibilityEvaluator`, which reuses the exact `AttendanceRateCalculator` +
`GradebookCalculator` logic the gradebook uses — so a certificate can never disagree with
the gradebook. **Issuance is admin-reviewed**, not automatic.

### Backend

| Module | Endpoints | Notes |
|---|---|---|
| **Certificates** | `GET /courses/{id}/certificate-candidates` (admin/assigned instructor — roster with computed recommended tier + any issued cert), `POST /certificates` (admin, issue; optional tier override), `PUT /certificates/{id}/revoke` (admin), `GET /my-certificates` (student), `GET /certificates/{id}` (owner/admin/instructor), `GET /certificates/verify/{code}` (**public**) | Certifiable enrollments are `active`/`expired`, never cancelled/refunded. One certificate per enrollment (unique). Metrics snapshotted at issue time. Revoke keeps the row for audit; the code still verifies but as invalid. |
| **Analytics** | `GET /analytics/admin/business` (admin), `GET /analytics/admin/academic` (admin), `GET /analytics/instructor` (instructor, own courses) | All read-side aggregation over existing tables; pure `AnalyticsCalculator` for pass-rate math. The per-course threshold is set through the existing admin `PUT /courses/{id}` (extended with `completionAttendanceThreshold`). |

### Frontend

- **Admin** (`/[locale]/admin/analytics`) — business overview (students, published
  courses/tracks, active enrollments, pending requests, approved revenue, leads, open
  cohorts, top-courses-by-revenue table, a simple enrollments-by-month bar chart) and
  academic overview (certificates by tier, assessment pass rate, per-course table).
- **Admin/instructor** (on the existing instructor course page) — certificate candidate
  roster with attendance-vs-threshold, assessments-passed, recommended tier, and (admin
  only) Issue / Revoke buttons plus an inline per-course attendance-threshold config.
- **Instructor** (`/[locale]/instructor`) — analytics summary + per-course table above the
  course list.
- **Student** (`/[locale]/my-certificates`) — printable certificate cards with serial,
  verification code, and a link into the verifier.
- **Public** (`/[locale]/verify`) — verify a certificate by code (auto-verifies from a
  `?code=` link); reachable from the nav signed-out.

### Verified (end-to-end, via API + live browser)

Both admin dashboards render real seeded data (revenue, per-course pass rates,
certificate counts). Full certificate lifecycle confirmed: candidate roster computes the
correct tier (verified both a Completion case — attendance bar met *and* all assessments
passed — and a Participation case where one assessment was failed), admin issues a
certificate, the public verify-by-code endpoint returns it as valid, a duplicate issue is
rejected (400), the student sees it under "my certificates", and after revocation the same
code verifies as invalid. Authorization boundaries spot-checked (student→business 403,
admin→instructor-dashboard 403 by the `InstructorOnly` policy, student→issue 403).
Bilingual (en/ar, RTL) confirmed on the public verify page.

**Bug found and fixed during verification:** the business dashboard's
top-courses-by-revenue query grouped approved enrollment requests by a *joined*
`Course.Title`, which Npgsql cannot translate (runtime 400). Fixed by grouping on the
scalar `course_id` and attaching titles in a second query.

### Out of scope for Phase 4 (later phases)

WhatsApp notifications and a certificate-issued notification event (Phase 5), PDF
certificate generation (current "view/print" uses the browser's print-to-PDF; a branded
server-rendered PDF is a later polish), a per-course numeric "minimum average score" knob
(the confirmed rule is all-assessments-passed, so it would be dead config today),
franchise/multi-tenant analytics scoping, and any working Python auto-grader (still blocked
on the Phase 3 Piston/hosting decision).

## Phase 5 — Notifications & Polish — 🚧 PARTIAL

**Scoped down at kickoff** (see `handoff_phase5_notifications_polish.md`): WhatsApp
integration is blocked on real Meta Business credentials (same class of blocker as
Piston in Phase 3), and load testing / the hosting decision aren't buildable without
a hosting target. What actually shipped this session: a real notification event
catalog behind a channel-agnostic abstraction (WhatsApp wired as a ready-but-inactive
channel), and a security fix found while scoping the phase — uploaded files were
served as fully public static content with no auth.

### Fixed: private file storage (found during scoping, not on the original roadmap)

`wwwroot/uploads` (payment proofs + course materials) was served via plain
`app.UseStaticFiles()` — no auth, no expiry. Anyone with a guessed/leaked URL could
view another applicant's payment proof. Fixed: `LocalFileStorageService` now stores
files under `PrivateStorage/` outside `wwwroot`; `app.UseStaticFiles()` is removed
entirely. Access goes through `GET /materials/{id}/file` (enrollment-gated, same rule
as viewing the material) and `GET /enrollment-requests/{id}/payment-proof`
(admin-only). See `docs/ARCHITECTURE.md` §3.

### Backend

| Module | Endpoints | Notes |
|---|---|---|
| **Notifications** | (no new HTTP endpoints — internal dispatch triggered by existing actions) | `INotificationDispatcher.DispatchAsync` fans a `NotificationEvent` out to every `INotificationChannel`; never throws to the caller. `EmailNotificationChannel` fully works; `WhatsAppNotificationChannel` no-ops while `WhatsAppSettings:Enabled` is false. Wired into 4 triggers: `ApproveEnrollmentRequest`, `RejectEnrollmentRequest` (both pre-existing, previously only logged via the retired `IEnrollmentNotificationService`), `IssueCertificate` and `GradeSubmission` (new — these didn't notify anyone before). |
| **File downloads** | `GET /materials/{id}/file`, `GET /enrollment-requests/{id}/payment-proof` | See the security fix above. |

### Frontend

- `downloadAuthenticatedFile` helper in `frontend/lib/api.ts` — fetches with the
  Bearer token and opens a blob URL, since plain `<a href>` links can't carry
  authentication. Instructor module page and student course-content page both use it
  for material downloads instead of direct static links.

### Verified (end-to-end, via API + live browser)

Confirmed the old public static path now 404s and the new endpoints require auth
(401 anonymous, 200 for admin/enrolled student); uploaded a real file and downloaded
it byte-for-byte correct through both curl and the actual instructor UI (Download
button → blob opens, no console errors). Triggered all 4 notification events for
real (two fresh enrollment requests approved/rejected, a certificate issued, an
assignment graded) and confirmed each rendered the correct subject/body in
`LoggingEmailSender`'s dev output, with `WhatsAppNotificationChannel` logging a
graceful skip every time rather than failing the request.

### Out of scope for Phase 5 (deferred further)

WhatsApp Business Cloud API itself (blocked on real credentials — see
`ARCHITECTURE.md` §7), recording storage upgrade (no upload flow exists for
recordings yet, unlike payment proofs/materials — nothing to protect), load testing
and the hosting/deployment decision (both need a target environment that hasn't been
chosen), bilingual/accessibility audit beyond what's already spot-checked per phase.

## Phase 6 — Admin Console — ✅ COMPLETE

Not on the original roadmap (which stopped at Phase 5). Added at the user's request
after Phase 5: admin-facing CRUD endpoints for courses/tracks/cohorts/coupons/
enrollment-requests had existed since Phase 0/1, but with no frontend at all — admins
could only reach them through Swagger. Scoped via `AskUserQuestion` at kickoff: build
the missing Users backend (no way existed to create an instructor account through the
product, despite `SRS.md` requiring admin-created-only instructor accounts) and
organize the new pages under a single `/admin` layout with a sub-nav rather than one
flat top-bar link per page.

### Backend

| Module | Endpoints | Notes |
|---|---|---|
| **Users** (new) | `GET /users`, `POST /users/instructors`, `PUT /users/{id}/deactivate`, `PUT /users/{id}/reactivate` | All `AdminOnly`. `CreateInstructorCommand` generates a temp password (`MustChangePassword = true`) and dispatches a new `InstructorAccountCreated` notification event through the Phase 5 catalog. Self-deactivation is explicitly guarded (`InvalidOperationException` → 400) so an admin can't lock themselves out. |

Every other endpoint used this phase (Courses/Tracks/Cohorts/Coupons/EnrollmentRequests/
Enrollments admin actions) already existed — see `ARCHITECTURE.md` §4 for the updated
per-module notes.

### Frontend

- `frontend/app/[locale]/admin/layout.tsx` — new. Role-gates once (instead of every
  page re-implementing the guard) and renders a tab-strip linking every admin page.
  `RoleNav.tsx` now shows one "Admin" link instead of growing per page.
- `admin/courses` — list (status filter) + create + publish/archive/delete. Cohort
  and instructor-assignment management were deliberately **not** built as a separate
  page — both are course-scoped, and the existing `instructor/courses/[courseId]`
  page already had one admin-only block (the completion-attendance-threshold editor).
  Two more admin-only panels (Instructors, Cohorts) were added there instead of
  duplicating course-content management in a second page.
- `admin/tracks` (list + create) and `admin/tracks/[trackId]` (edit + course
  membership add/remove).
- `admin/coupons` — list/create/edit/deactivate, single page (no nesting needed).
- `admin/enrollment-requests` (list, status filter) and
  `admin/enrollment-requests/[id]` (detail, approve/reject, payment-proof download
  via the Phase 5 `downloadAuthenticatedFile` helper, cancel any resulting
  enrollment — the only place `PUT /enrollments/{id}/cancel` is exposed; there is no
  standalone enrollment browser).
- `admin/users` — list (role/active filter) + create-instructor form +
  deactivate/reactivate.
- Every new page follows the codebase's established per-page pattern: plain
  `useState`/`useEffect` (no React Query), hand-rolled `<table className="table">`,
  `confirm()` before deletes — no new shared table/modal component was introduced.
- i18n: one new `admin` dictionary section (`frontend/lib/i18n.ts`), ~140 keys,
  en + ar.
- Fixed a pre-existing dead-code bug while wiring the Instructors panel:
  `CourseInstructorEntry`'s field names (`instructorName`/`instructorEmail`) never
  matched the backend's actual camelCase JSON (`fullName`/`email`) — harmless before
  since nothing rendered `course.instructors`, now corrected since it's the data
  source for instructor assignment.

### Verified (end-to-end, via API + live browser)

Created a course, an instructor account, and a cohort through the UI; assigned the
new instructor to the course; opened the cohort (draft → open, Complete became
available); added/removed a course on a track; created and edited a coupon;
submitted a fresh enrollment request via the public API and approved it through the
admin UI (downloaded its payment proof first, then cancelled the resulting
enrollment); deactivated and reactivated a user; confirmed the self-deactivation
guard surfaces its error correctly in the UI. Confirmed role gating both at the
frontend (`/admin/*` shows the sign-in-required notice for a student session) and
independently at the API (`AdminOnly` policy returns 403 for a student token on
`GET /users` and `POST /courses`). No console or server errors throughout.
69/69 unit tests pass (7 new — `CreateInstructorCommandValidatorTests` +
one `EmailNotificationTemplatesTests` case for `InstructorAccountCreated`).
Frontend typechecks clean throughout.

## Hardening Pass (post-Phase 6) — mustChangePassword enforcement + generated API client

Not a numbered phase — a targeted fix requested directly by the user, independent of
the roadmap's feature sequence. Two unrelated items, landed as separate commits.

**mustChangePassword enforcement.** The flag was set correctly at seed/instructor-
creation time but nothing acted on it — a user could use the entire API forever on a
temp/seed password. Fixed with a JWT claim (`JwtTokenGenerator`) plus a global,
fail-closed MVC filter (`PasswordChangeRequiredFilter`) that blocks every authenticated
endpoint unless explicitly opted out (`[AllowPendingPasswordChange]`, currently only
`POST /auth/change-password` and `GET /auth/me`). Maps to `403` with a
`code: "password_change_required"` extension. `ChangePasswordCommandHandler` now mints
a fresh token pair after clearing the flag, so the caller resumes normal access without
a second login. Frontend: `PasswordChangeGate` (mounted globally) redirects to a new
`/change-password` page whenever the session or a live 403 says so. Full details:
`ARCHITECTURE.md` §3/§6, `API_CONVENTIONS.md` §3/§4.

Verified end-to-end via a fresh instructor account (created through the real
`admin/users` "Add instructor" flow, temp password read from the dev email log): forced
redirect on login, direct navigation to an admin/instructor page bounced back, a
protected endpoint returned `403`/`password_change_required` via direct API call,
`GET /auth/me` and `POST /auth/change-password` stayed reachable, and submitting the
form landed on the role's normal page with a working session — repeated in Arabic/RTL
with a second fresh account. 78/78 backend unit tests pass (7 new).

**Generated API client (kills DTO drift at the source).** `frontend/lib/api.ts`
hand-mirrored every backend DTO (~90 `interface`s) and had already drifted silently —
see the `CourseInstructorEntry` fix noted in Phase 6 above; `CourseInstructorInfo` (the
public catalog's mirror of the same `CourseInstructorDto`) had the identical bug and was
still live (confirmed dead code, though — nothing renders `PublicCourseDetail.instructors`).

Discovered mid-implementation: Swashbuckle had **no response-body schemas at all** —
every action returned bare `Task<IActionResult>` with no `[ProducesResponseType(typeof(...))]`,
so the generated OpenAPI doc only ever had the ~55 request-body schemas ASP.NET Core
infers automatically from `[FromBody]`/`[FromForm]` parameters. Fixed by adding an
explicit `[ProducesResponseType(typeof(ResponseDto), StatusCodes.Status200OK)]` to every
action across all 22 controllers (~150 actions; skipped only the two file-download
actions) — purely additive metadata, no behavior change. Combined with the nullability
fix (`SupportNonNullableReferenceTypes()` +
`RequireNonNullablePropertiesSchemaFilter` in `src/CodeForge.Api/Swagger/`), the schema
count went from 55 to 142 and every property's `required`/`nullable` now matches the C#
DTO exactly. **New controller actions must add this attribute** — nothing else makes
Swashbuckle emit a response schema.

Types are generated via `openapi-typescript`, producing `frontend/lib/api-schema.d.ts`
from the API's own `swagger.json` (`node scripts/generate-api-types.mjs`, run manually
against the running dev API; both `openapi.json` and the generated `.d.ts` are
committed so `tsc`/`next build`/CI never need a live API). `lib/api.ts`'s hand-written
interfaces were replaced with aliases into the generated `components["schemas"]`, kept
under their original exported names so call sites were untouched; `apiFetch` itself is
unchanged. Five union type aliases (`SessionType`, `MaterialType`, `AssessmentType`,
`AttendanceStatus`, `CertificateTier`) stay hand-written since the backend types those
fields as plain `string` — a handful of call sites that fed a schema's now-widened
`string` field into a function still expecting the narrow union needed an explicit
`as CertificateTier`-style cast, since the UI can't know without one that the backend
only ever emits the constrained values. A CI job that regenerates and fails on
`git diff` is a good Phase 1 addition once the API can boot in CI (it currently requires
User Secrets + a live database) — not built here. Full details: `ARCHITECTURE.md` §6.

Verified: `dotnet build`/`dotnet test` green (78/78) throughout the controller-annotation
pass; `npx tsc --noEmit` and `npm run build` both clean after the `lib/api.ts` swap
(10 call-site fixes needed — optional/nullable field widening and the tier casts above).
Drift-guard proved directly: renamed `CourseInstructorDto.FullName` → `DisplayName`,
rebuilt, regenerated, and `tsc` failed at the exact consuming line
(`instructor/courses/[courseId]/page.tsx`) with "Property 'fullName' does not exist";
reverted and confirmed clean again. Browser-checked the catalog, course-detail, and
admin course-detail pages (the last one after assigning a real instructor, to exercise
`CourseInstructorDto` live) — correct rendering, no console errors.

## Operational Readiness (post-Phase 6)

Not a numbered phase — the platform was feature-complete through Phase 6 but had never
left the developer's laptop: no health probe, no CI, no bound on list-endpoint response
sizes, and a broken linter. Four items, landed as separate, independently-revertable
commits.

**Health checks.** `GET /health` (liveness) and `GET /health/ready` (readiness — checks
Postgres via `AddNpgSql`) are terminal middleware registered immediately after
`ExceptionHandlingMiddleware` and before HTTPS redirection, the rate limiter, and auth —
so neither is rate-limited, redirected, or subject to `PasswordChangeRequiredFilter`,
and neither needs `[AllowAnonymous]`. A host's restart probe should target `/health`
only: the host's response to a failed probe is to restart the instance, and restarting
cannot fix a database outage — wiring readiness to the restart trigger would turn a
transient Postgres blip into a restart storm. `/health/ready` exists for
`docker compose`'s `depends_on: condition: service_healthy` and for humans/monitoring.
See `ARCHITECTURE.md` §3.

**Pagination.** Applied to the 12 list endpoints whose result sets grow without bound
(`/users`, `/courses`, `/tracks`, `/coupons`, `/leads`, `/enrollment-requests`,
`/catalog/courses`, `/catalog/tracks`, `/announcements`, `/courses/{id}/cohorts`,
`/my-certificates`, `/instructor/courses`), returning `{ items, page, pageSize,
totalCount }` per `API_CONVENTIONS.md` §6. The 9 bounded, `OrderIndex`-ordered child
collections a course-content page renders as a complete tree (module sessions/
materials/assessments/assignments, course modules/instructors, `my-attempts`,
`my-submissions`, `sessions/{id}/materials`) stay bare arrays — paging them would mean
either N extra round-trips or a broken partial view, for a size that's bounded by
course design rather than by user growth. Every handler with a non-unique sort key
(`CreatedAt`, `StartDate`, `FullName`) got an `Id` tiebreaker so `Skip`/`Take` can't
duplicate or drop rows across pages. Frontend: 12 `lib/api.ts` wrappers now return
`PagedResult<T>`; a new `components/Pagination.tsx` (the first shared UI primitive in
this codebase) renders Showing X-Y of Z / Previous / Page N of M / Next, wired into
every affected page with the page reset to 1 on filter change; new `pagination` i18n
namespace in both `en` and `ar`.

**ESLint.** `next lint` was broken (no ESLint installed, no config — dropped into an
interactive setup prompt and hung). Replaced with ESLint 9's flat config, bridged to
`eslint-config-next@15.1.6` (which predates Next's native flat-config export) via
`FlatCompat`. `npm run lint` now runs `eslint .`; added as a step in
`scripts/verify.mjs` ahead of `tsc --noEmit`, so the pre-commit hook, CI, and a human
all run the identical gate. All 8 initial findings fixed (two props left unused by the
pagination change, six pre-existing `useEffect(load, [...])` sites needing the
established `eslint-disable` convention for a closure-refreshed-every-render
dependency, one `<img>` swapped for `next/image`).

**CI** (`.github/workflows/ci.yml`, two jobs — see `ARCHITECTURE.md` §3 for the
job-by-job description). `verify` runs `node scripts/verify.mjs` directly rather than
restating its steps, so CI can't drift from what a human/the hook actually run.
`drift-check` makes the API boot in CI for the first time: a `postgres:16` service
container, `dotnet ef database update`, then the API starts with a throwaway
`JwtSettings:Secret` and `AdminSeed` left unset (`DatabaseSeeder.SeedAsync` no-ops
without it), polls `/health`, regenerates `openapi.json`/`api-schema.d.ts`, and fails
on `git diff` — catching a backend DTO change merged without regenerating the frontend
types. The `impeccable` design-critique detector stays local-only (see `ARCHITECTURE.md`
§7 for why a gitignored bundle at a different version than its npm package would make a
poor CI gate).

**Verified:** `node scripts/verify.mjs` green throughout (`dotnet build`/`test`,
`eslint`, `tsc --noEmit`, `next build`). Pagination proved live, not just against the
thin dev dataset: 25 admin-created test coupons produced real two-page navigation with
correct `Showing`/`Page N of M` text and edge-correct `Next` disabling on the last
page, checked in both English and Arabic-RTL. `drift-check` proved directly on a
throwaway branch: renamed `CouponDto.Code` → `CouponCode` without regenerating, pushed,
watched the job fail with the exact diff in its log; regenerated and fixed the two
frontend call sites `tsc` flagged, pushed again, watched both jobs go green; closed the
branch without merging. `dotnet ef database update` and the API boot-with-unset-seed
path were also dry-run locally against a throwaway Postgres database before ever
trusting them in CI.

## Session Start Checklist

At the start of any session touching this codebase, read `SRS.md`, `ARCHITECTURE.md`,
`DATABASE.md`, `API_CONVENTIONS.md`, and `CODING_STANDARDS.md` before making changes.
Update the relevant doc(s) in the same change whenever a decision here turns out to be
wrong or a new one gets made — these must not drift from what's actually built.
