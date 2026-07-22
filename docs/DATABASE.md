# CodeForge Academy — Database

PostgreSQL. Source of truth is the EF Core model
(`src/CodeForge.Domain/Entities/Entities.cs` +
`src/CodeForge.Infrastructure/Data/CodeForgeDbContext.cs`) and its migrations under
`src/CodeForge.Infrastructure/Data/Migrations/`. This document is a human-readable
summary — if it ever disagrees with the migration, the migration wins; fix this file.

The original design doc (`/ERD_CodeForge_v2.md` at the repo root) and hand-written
`/schema.sql` are historical inputs from before Phase 1 and are now superseded by this
file for anything they disagree on (tracks, cohorts, coupons, the enrollment shape).

## 1. Baseline Schema (Phase 0, unchanged except as noted)

18 tables across 6 domains — Identity & Auth (`users`, `password_reset_tokens`),
Course Structure (`courses`, `course_instructors`, `sections`, `lectures`,
`resources` — **`sections`/`lectures`/`resources` repurposed into
`modules`/`sessions`/`materials` in Phase 2, see §6**), Enrollment Flow
(`enrollment_requests`, `enrollments` — **extended in Phase 1, see §2**), Learning &
Progress (`quizzes`, `quiz_questions`, `quiz_options`, `quiz_attempts`,
`quiz_answers`, `lecture_progress` — **renamed `session_progress` in Phase 2**,
`live_sessions` — **dropped in Phase 2, merged into `sessions`**), Communication
(`announcements` — **Application/Api built in Phase 2**, `leads` — **extended in
Phase 1**), System Observability
(`activity_logs`). Field-level detail for these is unchanged from the original ERD
except where noted below.

## 2. Phase 1 Additions

### `tracks`
Flexible course bundles. Mirrors `courses`' lifecycle shape.

| Column | Type | Notes |
|---|---|---|
| id | uuid PK | |
| title, slug | text | slug unique |
| description, thumbnail_url | text? | |
| price | numeric | bundle price |
| currency | text | default `EGP` |
| status | text | `draft` / `published` / `archived` (`TrackStatuses`) |
| created_by_id | uuid FK → users | |
| created_at, updated_at, deleted_at | timestamptz | soft delete, same pattern as `courses` |

### `track_courses` (join)
| Column | Type | Notes |
|---|---|---|
| id | uuid PK | |
| track_id | uuid FK → tracks | |
| course_id | uuid FK → courses | |
| sort_order | int | display order within the track |
| created_at | timestamptz | |

Unique on `(track_id, course_id)`.

### `cohorts` (batches)
The recurring-batch concept the whole live-cohort model runs on. Always scoped to a
single course — a track has no cohort of its own; enrolling in a track enrolls the
student into the currently-open cohort of **every** course in that track (see §4).

| Column | Type | Notes |
|---|---|---|
| id | uuid PK | |
| course_id | uuid FK → courses | |
| name | text | e.g. "Batch 3 — Summer 2026" |
| start_date, end_date | timestamptz | |
| enrollment_cutoff_date | timestamptz | must be ≤ end_date |
| capacity | int | seat limit |
| grace_period_days | int | default 14; added to `end_date` for access expiry |
| status | text | `draft` / `open` / `cancelled` / `completed` (`CohortStatuses`) — admin-controlled lifecycle only; "full" and "closed-by-cutoff" are **computed**, not stored (see §4) |
| created_at, updated_at | timestamptz | |

### `coupons`
| Column | Type | Notes |
|---|---|---|
| id | uuid PK | |
| code | text | unique, stored normalized uppercase |
| type | text | `percent` / `fixed` (`CouponTypes`) |
| value | numeric | percent (0–100) or fixed EGP amount depending on type |
| is_active | bool | |
| valid_from, valid_until | timestamptz? | nullable = no bound |
| usage_limit | int? | null = unlimited |
| used_count | int | incremented at submission, decremented on rejection — see §4 |
| created_by_id | uuid FK → users | |
| created_at, updated_at | timestamptz | |

### `enrollment_requests` (extended)
| Column | Change |
|---|---|
| course_id | now **nullable** — set only for a single-course request |
| track_id | **new**, nullable FK → tracks — set only for a track-bundle request. Exactly one of `course_id` / `track_id` is set. |
| original_price | **new**, numeric — snapshot of course/track price at submission |
| coupon_id | **new**, nullable FK → coupons |
| coupon_code | **new**, nullable text — snapshot of what the applicant typed |
| discount_amount | **new**, numeric, default 0 |
| final_price | **new**, numeric — `original_price - discount_amount` |

### `enrollment_request_cohorts` (new join)
Links one request to the specific cohort(s) it will create enrollments in on approval —
one row for a single-course request, one row per course for a track request (all
resolved and locked in at submission time).

| Column | Type |
|---|---|
| id | uuid PK |
| enrollment_request_id | uuid FK → enrollment_requests |
| cohort_id | uuid FK → cohorts |

### `enrollments` (extended)
| Column | Change |
|---|---|
| cohort_id | **new**, required FK → cohorts |
| status | now includes `cancelled` and `refunded` in addition to `active` / `expired` |
| cancelled_at | **new**, nullable |
| cancellation_reason | **new**, nullable text |
| cancelled_by_id | **new**, nullable FK → users |

**Unique constraint changed:** was `UNIQUE(student_id, course_id)`; is now
**`UNIQUE(student_id, cohort_id)`**. A student legitimately re-enrolls in a *later*
cohort of the same course after a prior one expires (confirmed in `SRS.md` §3) — the
constraint now only prevents double-enrolling in the *same* batch.

### `leads` (extended)
| Column | Change |
|---|---|
| course_id | **new**, nullable FK → courses — set when a lead is submitted from an "await next batch" context, so admin can filter the waitlist per course. No separate waitlist table exists; this reuses the lead-capture mechanism per the SRS. |

## 3. Entity-Relationship Additions (Phase 1)

```
tracks 1───* track_courses *───1 courses
courses 1───* cohorts
cohorts 1───* enrollments
cohorts 1───* enrollment_request_cohorts *───1 enrollment_requests
enrollment_requests *───1 coupons (optional)
enrollment_requests *───0..1 courses, *───0..1 tracks (exactly one set)
leads *───0..1 courses (optional)
```

## 4. Key Business Rules (implemented in Application handlers, not the DB)

- **Cohort enrollment eligibility** (computed, not a stored flag):
  `status == Open AND now <= enrollment_cutoff_date AND active_enrollment_count < capacity`.
  No background job flips a cohort to "closed" or "full" — availability is always
  computed at read/write time. `seats_left = max(0, capacity - active_enrollment_count)`.
- **Access expiry:** set at approval time = `cohort.end_date + cohort.grace_period_days`.
- **Track bundle rule (Phase 1, strict):** a track is only bundle-enrollable when
  *every* constituent course currently has an open cohort. If any course lacks one,
  the bundle path is unavailable (students can still enroll in the individually-open
  courses one at a time). Revisit for partial-bundle enrollment in a later phase if
  this proves too strict operationally.
- **Coupon usage accounting:** `used_count` increments at **submission** (claims the
  slot immediately, preventing overselling during the pending-review window) and
  decrements on **rejection** (frees it back up). Approval does not change it — it was
  already counted.
- **Capacity is re-checked at approval**, not only at submission — multiple pending
  requests can target the same cohort; the seat is truly consumed only on approval. If
  the cohort has filled by the time an admin approves, approval fails with a clear
  error; the admin rejects and the applicant re-submits against a later batch.
- **Discount calculation** (`CouponCalculator`, `Application/Common/`): percent →
  `round(price * value / 100, 2)` clamped to `[0, price]`; fixed → `min(value, price)`
  clamped to `≥ 0`. `final_price = original_price - discount_amount`.

## 4a. Legacy Hand-Authored CHECK Constraints

The original `schema.sql` (pre-Phase 0) added several `CHECK` constraints directly in
SQL that were **never expressed in the EF model** via fluent config — EF doesn't know
they exist, so changing an enum-like column's allowed values in C# does **not**
update them, and inserts/updates with a new value fail at the database with a raw
Postgres `23514` error until the constraint is manually widened via a migration's raw
SQL. This bit us once already for `chk_enrollment_status` (fixed in
`20260717193822_FixEnrollmentStatusCheckConstraint` — see §5) when Phase 1 added
`cancelled`/`refunded` to `EnrollmentStatuses`.

**Known constraints of this kind still in the database** (from `schema.sql`):
`chk_user_role`, `chk_course_status`, `chk_resource_type`, `chk_resource_target`,
`chk_request_status`. **Before adding a new allowed value to any status/enum-like
constant tied to one of these columns, add a migration that
`ALTER TABLE ... DROP/ADD CONSTRAINT` to match** — don't assume the C# constants file
is the only place that needs updating.

`chk_quiz_pass_score` **used to be listed here as one of these legacy constraints,
but that was stale** — direct inspection during Phase 3 found it was never actually
carried from `schema.sql` into the EF migrations (the `quizzes` table had no CHECK
constraint at all in the real dev DB, only a plain `pass_score integer` column,
despite existing on the live DB from a prior manual `psql` run predating EF's model —
see §7). Phase 3 added it properly through EF's fluent config
(`entity.ToTable(t => t.HasCheckConstraint(...))`), with the migration guarding the
`ADD CONSTRAINT` behind a `DROP CONSTRAINT IF EXISTS` specifically because a
same-named constraint already existed unmanaged on disk — see
`20260722084131_AddAttendanceAssessments`. It is now a normal, EF-tracked constraint
and no longer belongs in this "hand-authored, untracked" list.

## 6. Phase 2 Additions — Modules, Sessions, Materials

The Phase 0 schema already had a `sections → lectures → resources` hierarchy plus a
separate, disconnected `live_sessions` table (both were dormant scaffolding — zero
Application/Api code, zero rows). Phase 2 **repurposes and merges** these to match the
live-cohort content model from `SRS.md` §2, rather than building a parallel structure:

- `sections` → **`modules`** (renamed, `description` column added)
- `lectures` + `live_sessions` → **`sessions`** (merged into one type-discriminated
  table — a module's content is a mix of live sessions, in-person sessions, and
  standalone pre-recorded lessons, all ordered together)
- `resources` → **`materials`** (renamed, gains a `type` discriminator and
  `order_index`)
- `lecture_progress` → **`session_progress`** (renamed; still optional/supplementary
  per `SRS.md` §2 — not required for certification, that comes from attendance +
  assessments in Phase 3/4)

No data existed in any of these tables at migration time — this was a straight
rename/merge, not a data migration.

### `modules`
| Column | Type | Notes |
|---|---|---|
| id | uuid PK | |
| course_id | uuid FK → courses | |
| title | text | |
| description | text? | **new** |
| order_index | int | |
| created_at, updated_at, deleted_at | timestamptz | soft delete, same pattern as `courses` |

### `sessions`
| Column | Type | Notes |
|---|---|---|
| id | uuid PK | |
| module_id | uuid FK → modules | |
| type | text | `live` / `in_person` / `recorded_lesson` (`SessionTypes`) |
| title | text | |
| description | text? | |
| order_index | int | |
| scheduled_at | timestamptz? | required for `live`/`in_person`, null for `recorded_lesson` |
| duration_minutes | int? | |
| join_link | text? | `live` only — external Zoom/Meet/Teams URL, never embedded (SRS §2) |
| location | text? | `in_person` only |
| video_url | text? | the video asset: primary content for `recorded_lesson`, or the post-session recording link for `live`/`in_person` once available (login-gated external link per SRS — see §2 "recording hosting") |
| instructor_id | uuid? FK → users | who scheduled it; nullable since admin can also create/edit (SRS §11 — both can manage) |
| created_at, updated_at | timestamptz | |

Business rule (Application-layer, not a DB constraint): `live`/`in_person` sessions
require `scheduled_at`; `live` additionally requires `join_link`; `in_person`
requires `location`; `recorded_lesson` requires `video_url`.

### `materials`
| Column | Type | Notes |
|---|---|---|
| id | uuid PK | |
| module_id | uuid? FK → modules | |
| session_id | uuid? FK → sessions | at least one of module_id/session_id set |
| type | text | `file` / `text` / `link` (`MaterialTypes`) — SRS §2 "files, text/notes, external links" |
| title | text | |
| order_index | int | **new** |
| body | text? | `text` type only |
| file_url | text? | `file` type only |
| file_type | text? | `file` type only — `pdf`/`ppt`/`zip`/`other` (unchanged allowed set) |
| file_size_kb | int? | `file` type only |
| link_url | text? | `link` type only |
| created_at, updated_at | timestamptz | |

Legacy constraint `chk_resource_target` (`lecture_id IS NOT NULL OR section_id IS NOT
NULL`) is replaced with `chk_material_target`
(`module_id IS NOT NULL OR session_id IS NOT NULL`) against the renamed columns —
see §4a for why this needs a hand-written migration statement, not just a C# change.

### `session_progress`
| Column | Type | Notes |
|---|---|---|
| id | uuid PK | |
| student_id | uuid FK → users | |
| session_id | uuid FK → sessions | renamed from `lecture_id` |
| completed_at | timestamptz | |

Unique `(student_id, session_id)`, unchanged semantics.

### Announcements (no schema change, Application/Api built in Phase 2)
`announcements` already existed with `course_id` nullable (null = platform-wide) and
`author_id`. Phase 2 adds the CQRS module and controller — see
`IMPLEMENTATION_ROADMAP.md`.

### "Upcoming items" (no new table — computed)
Per `SRS.md` §11, this is a simple query, not a calendar entity: upcoming `live`/
`in_person` sessions (`scheduled_at >= now`) for courses the student has an active
enrollment in, plus recent announcements (platform-wide + their enrolled courses),
sorted by date. No read/seen tracking in this phase.

## 7. Phase 3 Additions — Attendance, Assessments, Assignments

The `quizzes`/`quiz_questions`/`quiz_options`/`quiz_attempts`/`quiz_answers` cluster
already existed from `InitialCreate` (Phase 0) but was dormant — zero rows, zero
Application/Api code, and `Quiz.CourseId` predated the Phase 2 module system. Phase 3
altered it in place (safe, zero data) rather than building a parallel structure, and
added two fully new clusters: attendance and code assignments.

### `quizzes` (altered — now shared by quizzes *and* exams)
| Column | Change |
|---|---|
| course_id | **removed** |
| module_id | **new**, required FK → `modules` — matches Sessions/Materials attaching at the module level (SRS §2: "modules... contain... assignments and quizzes") |
| order_index | **new** — position within the module's assessment list |
| type | **new**, `quiz` / `exam` (`AssessmentTypes`) — merges what would otherwise be a near-duplicate `exams` table, mirroring how Phase 2 merged `live`/`in_person`/`recorded_lesson` into one `sessions` table |
| allow_retake | **removed**, replaced by `max_attempts` |
| max_attempts | **new**, int? — null = unlimited; exams are validated to exactly `1` |
| is_practice | **new**, bool — graded vs. practice (SRS §7) |
| randomize_questions | **new**, bool |
| disable_copy_paste | **new**, bool — frontend UX deterrent only, no server enforcement (SRS: "no proctoring") |
| pass_score | unchanged, now enforced by `chk_quiz_pass_score CHECK (pass_score IS NULL OR pass_score BETWEEN 0 AND 100)` — see §4a |

`quiz_attempts` gains `attempt_number int` (tracks which attempt this is, needed to
enforce `max_attempts`). `quiz_options` gains `order_index int` — added after
Phase 3 shipped when cross-query option ordering turned out to be non-deterministic
without it (EF doesn't guarantee navigation-collection order without an explicit
sort key); see `20260722095447_AddQuizOptionOrdering`.

**Cohort scoping (no schema change):** like Sessions/Materials, quizzes/exams/
assignments are course-content, shared across every cohort of that course — there is
no `cohort_id` anywhere in this cluster, matching the precedent already set by
`session_progress` in Phase 0/2.

### `attendance_records` (new)
| Column | Type | Notes |
|---|---|---|
| id | uuid PK | |
| session_id | uuid FK → sessions, cascade | attendance only applies to `live`/`in_person` sessions in practice, not enforced at the DB level |
| student_id | uuid FK → users, cascade | |
| status | text | `present` / `absent` / `late` / `excused` (`AttendanceStatuses`) |
| marked_by | uuid FK → users, restrict | instructor/admin who marked it |
| marked_at | timestamptz | |
| notes | text? | |
| created_at, updated_at | timestamptz | |

Unique `(session_id, student_id)` — marking is an upsert, not append-only.

**Attendance rate is computed, not stored** (same "compute, don't store" philosophy as
cohort seat availability, `DATABASE.md` §4): for a given enrollment, "sessions held"
= `live`/`in_person` sessions in the course whose `scheduled_at` falls within
`[cohort.start_date, cohort.end_date + cohort.grace_period_days]` and has already
happened (`scheduled_at <= now`). Rate = (present + late) ÷ (held − excused), so an
excused absence never counts against the student. See
`AttendanceRateCalculator` (`Application/Attendance/Common/`).

### `assignments` cluster (new — code assignments, Python auto-grader)
- **`assignments`**: `id, module_id (FK→modules, cascade), title, description
  (instructions), order_index, is_practice, max_attempts int?, due_at timestamptz?
  (soft deadline — SRS: late allowed+flagged, never blocked), pass_score int?
  (`chk_assignment_pass_score`, same shape as the quiz constraint), created_at,
  updated_at`.
- **`assignment_test_cases`**: `id, assignment_id (FK, cascade), input text (stdin),
  expected_output text, is_hidden bool (hidden cases count toward score but aren't
  shown to students as examples), points int, order_index int`.
- **`assignment_submissions`**: `id, assignment_id (FK, cascade), student_id (FK,
  cascade), code text, attempt_number int, submitted_at, is_late bool (computed at
  submission time vs. `due_at`), auto_score int?, auto_grading_status text
  (`pending`/`completed`/`failed`), manual_score int?, manual_feedback text?,
  final_score int? (`manual_score ?? auto_score`), graded_by (FK→users, nullable,
  set-null), graded_at?`.
- **`assignment_test_results`**: `id, submission_id (FK→assignment_submissions,
  cascade), test_case_id (FK→assignment_test_cases, restrict), passed bool,
  actual_output text?, error_message text?, execution_time_ms int?`.

### Auto-grader engine — deferred, not wired to a working service
`ICodeExecutionService` (`Application/Common/Interfaces/`) isolates the actual code
runner, per `ARCHITECTURE.md` §1's "isolate volatile/external concerns" principle.
Piston (`emkc.org`) was the initial choice (free, no Docker needed) and
`PistonCodeExecutionService` is fully implemented and registered in DI — but Piston's
public API **went whitelist-only on 2026-02-15** (confirmed via a direct 401 during
Phase 3 verification), so it cannot currently reach a real engine. The active
`ICodeExecutionService` implementation is `DeferredCodeExecutionService`, which throws
immediately; `SubmitAssignmentCommandHandler` catches this and sets
`auto_grading_status = 'failed'` without blocking the submission — instructors grade
manually via `GradeSubmission`, which SRS §7 already requires as a first-class path
regardless of auto-grading. Swap the DI registration back to
`PistonCodeExecutionService` once whitelisted, or replace with a self-hosted engine
once hosting is decided (Phase 5).

## 8. Migrations

History: `20260629194500_InitialCreate` (Phase 0, regenerated with proper
snapshot/Designer files — see `ARCHITECTURE.md` §3), then
`20260717191353_AddTracksCohortsCoupons` (Phase 1, everything in §2 — applied
cleanly to the dev DB; `has-pending-model-changes` confirms none outstanding), then
`20260717193822_FixEnrollmentStatusCheckConstraint` (widened the legacy
`chk_enrollment_status` constraint — see §4a), then a Phase 2 migration for the
`modules`/`sessions`/`materials`/`session_progress` rename+merge in §6.
Two `DropIndex` calls in that migration were converted to guarded
`DROP INDEX IF EXISTS` raw SQL because the live DB's index names for
`enrollments.source_request_id` / the old `(student_id, course_id)` unique index
didn't exactly match what the regenerated `InitialCreate` snapshot assumed — a
one-time consequence of the Phase 0 snapshot fix, harmless and now resolved. Then
three Phase 3 migrations: `20260722084131_AddAttendanceAssessments` (§7's main
schema, including the guarded `chk_quiz_pass_score` add), followed by
`20260722085233_AddAssessmentOrdering` (quiz/assignment `order_index`, discovered
missing after building the Assessments module) and
`20260722095447_AddQuizOptionOrdering` (discovered missing during end-to-end
verification, when option order proved non-deterministic across queries). Generate
further migrations with:

```
dotnet ef migrations add <Name> --project src/CodeForge.Infrastructure --startup-project src/CodeForge.Api
```

Raw SQL EF can't express as fluent config (e.g. the GIN index on
`activity_logs.metadata`) is appended by hand to the generated migration's `Up()` —
see the `InitialCreate` migration for the pattern.
