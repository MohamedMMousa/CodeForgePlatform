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

## Phase 3 — Attendance & Assessments

Manual attendance marking + reporting; quizzes, assignments (+ Python auto-grader —
engine choice deferred to this phase), exams (basic controls, no proctoring); grading
+ gradebook. Frontend: attendance UI, quiz/exam taking, submission + grading screens.

## Phase 4 — Certificates & Analytics

Two-tier completion logic (attendance % + pass mark, configurable per course with
platform defaults); admin business + academic dashboards; instructor analytics.
Frontend: dashboards, certificate view/download.

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
