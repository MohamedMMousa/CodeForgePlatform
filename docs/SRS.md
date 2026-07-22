# CodeForge Academy — Software Requirements Specification

> **Status:** Canonical, current requirements. This supersedes the earlier draft at
> `/CodeForge_SRS_v2.md` (kept in place as historical input only — do not treat it as
> current). These requirements were captured through a structured discovery interview
> and approved by the client on 2026-07-17.

## 1. Product Overview

CodeForge Academy is a **live, cohort-based programming education platform** for a new
academy launching digital-first (physical/branch operations planned for a later phase,
out of scope for this software today). It serves two audiences with the same platform:
**grade 8–12 students** and **college students**, learning programming through
**tracks** — flexible bundles of courses.

- **Region:** Egypt. **Currency:** EGP. **Timezone:** GMT+2 (single).
- **Scale target (year 1):** small, dozens up to ~500 students.
- **Languages:** Arabic and English, bilingual from launch (RTL + LTR).

## 2. Delivery Model

This is **not** a self-paced video library. A course runs as a schedule of **live
sessions** that students attend at scheduled times, hosted on an external tool
(Zoom / Google Meet / Teams) — the platform stores the join link and schedule only; it
does not embed video conferencing and does not integrate the Zoom/Meet API.

- Sessions can be **live (online)** or **in-person ("offline")**; both are attended and
  attendance is marked identically in the platform. In-person is a lightweight
  per-session flag + optional location, distinct from full branch/classroom management
  (deferred to the future physical phase).
- **Recordings** of live sessions exist for **revision only** — never the primary way to
  consume the course, and never a driver of progress. Launch approach: **login-gated
  external links** (e.g. unlisted YouTube), shown only to enrolled students within their
  access window; planned upgrade to private in-platform storage + signed URLs as the
  academy grows.
- Modules may also contain **standalone pre-recorded lessons** made ahead of time and
  never delivered live — a supplementary, partial self-paced component. These are not
  required for certification.
- **Course structure:** courses are organized into **modules**, each containing a mix of
  live sessions (+ their recordings), pre-recorded lessons, files, text, links,
  assignments, and quizzes.

## 3. Tracks, Courses, and Cohorts

- A **track** is a flexible bundle of related courses with **no enforced order**.
  Prerequisites between courses are guidance only ("recommended after X"), never
  enforced.
- Students may enroll and pay **per course** or **per track** (bundle price).
- Delivery runs as **recurring cohorts ("batches")**: each course runs as a batch with a
  defined start/end. A new batch opens each term.
  - **Enrollment window:** open until a configurable **cutoff**; students joining after
    the batch has started receive recordings of already-past sessions.
  - **Capacity:** each batch has a seat limit. When full or the enrollment window has
    closed, students see an **"await next batch"** state and may leave contact details
    to be notified when a new batch opens (reuses the lead-capture mechanism).
  - **Access window:** access runs for the batch's term **plus a configurable grace
    period** (a short revision window), then **full lock-out**. Students must enroll in
    a future batch to regain access — there is no renewal-in-place.

## 4. Users & Roles

- **Admin** — single super-admin permission level (no sub-admin tiers in this phase).
- **Instructor** — accounts are **admin-created only** (no instructor self-signup).
  Multiple instructors may be assigned to one course with **equal permissions**
  (co-teaching; no lead/TA distinction). Instructors **publish their own courses
  freely** — no admin content-approval gate.
- **Student** — visitors browse the public catalog without an account. An account is
  created **only when they enroll and pay** for a course or track. Every enrollment,
  including repeat enrollments by an existing student, goes through the **same**
  payment-proof + admin-approval process — no fast path, no admin bypass/scholarship
  path.
- **No parent accounts / no parent involvement**, even though the grade 8–12 segment are
  minors.

## 5. Authentication

- Email/password login (JWT access + refresh tokens).
- Password recovery via email (a reset link, not a token returned in the API response).
- **Google social login** — included only if it is low-effort to add; not a hard
  requirement.
- **2FA** — out of scope for this phase.

## 6. Payments & Pricing

- **Manual only:** student uploads payment proof; admin reviews and approves before
  access is granted. No online payment gateway in this phase.
- **Discounts:** admin-created coupon codes, percentage or fixed amount, applied at
  enrollment.
- **Refunds:** admin can cancel an enrollment, revoke access, and record it as refunded.
  Actual money movement happens offline; the platform only reflects the state change.
- Currency: EGP.

## 7. Assessments

- Types: **auto-graded quizzes** (MCQ), **code assignments**, and **formal exams**.
- **The instructor fully controls every assessment**, configured per-item: graded vs.
  practice, attempt limits, timing, and pass thresholds.
- **Exams:** basic controls only — timer, single attempt, question randomization,
  disabled copy/paste. **No proctoring** (no webcam/monitoring).
- **Code assignments:** auto-run against instructor-defined test cases for an initial
  score, **plus** manual instructor review/adjustment and feedback.
- **Auto-grader language:** Python first (single language at launch; engine choice
  deferred to the phase that implements it).
- **Grading scale:** percentage / points, with an optional pass threshold per
  assessment.
- **Deadlines are soft:** shown as guidance; late submissions are allowed and flagged,
  never hard-blocked.

## 8. Attendance

- Tracked and reported for every session (online or in-person).
- **Instructor manually marks attendance** — there is no Zoom/Meet API integration for
  automatic attendance capture.

## 9. Progress & Certification

- Progress is **not** based on video-watch completion (a live cohort model makes that
  meaningless) — it is derived from **attendance + assessment grades**.
- **Two-tier certificate**, thresholds (attendance % and pass mark) configurable
  **per course** with platform-wide defaults:
  - **Completion certificate** — attendance threshold **and** assessments passed.
  - **Participation certificate** — enrolled/attended but did not meet the bar.

## 10. Communication

- **Channel-agnostic notification layer.** Email is the day-one baseline (reliable, no
  approval friction). **WhatsApp is the primary target channel**, added via the
  official **WhatsApp Business Cloud API** (through a BSP) once business verification
  and message-template approval are complete — this requires a Meta-verified business, a
  dedicated number, pre-approved templates for business-initiated messages, and
  per-conversation cost. Unofficial WhatsApp automation is explicitly rejected (ToS
  violation / ban risk). Manual WhatsApp by admin is an acceptable interim measure.
- **One-way announcements only** (instructor/admin → students), platform-wide (admin) or
  course-scoped (instructor). **No** in-platform student↔instructor messaging/Q&A;
  questions go through WhatsApp/email outside the platform.
- The public **contact/lead form** is retained for visitor inquiries and admin
  follow-up, and is reused for "notify me about the next batch."

## 11. Calendar / Schedule

- A **simple "upcoming items" list** (sessions, deadlines, exams) on the dashboard —
  not a full calendar view.
- **Both admin and instructor** can create/edit a course's live-session schedule and
  join links; the instructor can also edit it when a schedule changes.

## 12. Reports & Analytics

- **Admin:** both business metrics (revenue, enrollment trends, best-selling
  courses/tracks) and academic metrics (completion rates, scores, attendance, at-risk
  students), roughly equal priority.
- **Instructor:** roster, attendance marking, grading, announcements, content
  authoring, plus performance analytics for their own courses (class averages,
  attendance rates, at-risk students).

## 13. Non-Functional Requirements

- Scale: ~500 students in year one.
- **Responsive website only** at launch — no native mobile app (possible future work).
- **Bilingual Arabic/English**, RTL + LTR, from launch.
- Standard security hardening: secrets never committed, refresh/reset tokens hashed at
  rest, rate limiting on public endpoints, centralized error handling, admin bootstrap
  without manual DB edits. (Tracked and delivered in Phase 0 — see
  `IMPLEMENTATION_ROADMAP.md`.)

## 14. Out of Scope (This Phase)

Multi-branch / multi-academy / franchise support (**undecided** long-term — flagged as a
future architectural driver, not a current requirement), native mobile apps, online
payment gateway integration, in-platform messaging/forums, additional auto-grader
languages, admin sub-roles, proctored exams, parent accounts, course prerequisite
enforcement, and cohort renewal-in-place.

## 15. Traceability

Full discovery Q&A, resolved ambiguities, risks, and assumptions are preserved in the
session record; the distilled, current state of every decision lives in this document
and in `ARCHITECTURE.md` / `DATABASE.md`. Update this file whenever a requirement
changes — do not let it drift from what is actually being built.
