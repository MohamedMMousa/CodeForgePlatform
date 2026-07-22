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

## Phase 5 — Notifications & Polish

WhatsApp Business Cloud API integration (channel-agnostic layer already exists from
Phase 0 — email is the current default); notification event catalog; recording
storage upgrade (external links → private storage + signed URLs); bilingual/
accessibility polish; load testing; hosting/deployment decision.

## Session Start Checklist

At the start of any session touching this codebase, read `SRS.md`, `ARCHITECTURE.md`,
`DATABASE.md`, `API_CONVENTIONS.md`, and `CODING_STANDARDS.md` before making changes.
Update the relevant doc(s) in the same change whenever a decision here turns out to be
wrong or a new one gets made — these must not drift from what's actually built.
