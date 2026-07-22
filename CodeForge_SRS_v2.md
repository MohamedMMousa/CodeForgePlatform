# CodeForge Academy â€” SRS v2.0 | Part 1: Requirements & User Stories

> **Version:** 2.0 | **Date:** June 2026 | **Changes:** Enrollment split, pricing, slugs, updated_at, leads, activity logs, archived status

---

## 1. Change Log (v1 â†’ v2)

| # | Change | Impact |
|---|---|---|
| 1 | Split EnrollmentRequest / Enrollment tables | DB, API, Services, User Flows |
| 2 | Added price + currency to courses | DB, API, UI |
| 3 | Added slug to courses | DB, API, Frontend routing |
| 4 | Added updated_at to all major tables | DB schema |
| 5 | Added Leads table | DB, Public site, Admin |
| 6 | Added ActivityLog table | DB, Services |
| 7 | Course status: added `archived` | DB, Admin workflows, API |

---

## 2. Roles (Unchanged)

| Role | Description |
|---|---|
| Admin | Full platform control |
| Instructor | Manages assigned courses only |
| Student | Consumes course content |

---

## 3. Functional Requirements (Updated)

### FR-AUTH (Authentication)
- FR-AUTH-01: System generates student credentials on enrollment approval.
- FR-AUTH-02: Students must change password on first login (`must_change_password = true`).
- FR-AUTH-03: JWT-based authentication (access + refresh tokens).
- FR-AUTH-04: Email-based password reset.
- FR-AUTH-05: Role-based access control (RBAC).
- FR-AUTH-06: All auth events logged to ActivityLog.

### FR-ENR (Enrollment â€” SPLIT into Request + Enrollment)
- FR-ENR-01: Visitors submit EnrollmentRequests without an account.
- FR-ENR-02: EnrollmentRequest holds: name, email, phone, course, payment method, payment proof URL.
- FR-ENR-03: EnrollmentRequest statuses: `pending`, `approved`, `rejected`.
- FR-ENR-04: Admin reviews and approves/rejects EnrollmentRequests.
- FR-ENR-05: On approval â†’ create student account (if not exists) â†’ create Enrollment record â†’ assign course â†’ send credentials email.
- FR-ENR-06: On rejection â†’ EnrollmentRequest status = `rejected`, rejection reason stored, no Enrollment created.
- FR-ENR-07: Enrollment statuses: `active`, `expired`.
- FR-ENR-08: Enrollment stores: student_id, course_id, access_expires_at, source_request_id.
- FR-ENR-09: Expired enrollments revoke student course access automatically.
- FR-ENR-10: Students can hold multiple active Enrollments (one per course).
- FR-ENR-11: A student submitting a second request for the same course should be flagged for admin review.

### FR-CRS (Course Management)
- FR-CRS-01: Admin creates, edits, soft-deletes courses.
- FR-CRS-02: Admin assigns one or more instructors to a course.
- FR-CRS-03: Course metadata: title, slug, description, thumbnail_url, category, price, currency, status.
- FR-CRS-04: Course statuses: `draft`, `published`, `archived`.
- FR-CRS-05: Only `published` courses are visible to the public.
- FR-CRS-06: `archived` courses are hidden from public but data is preserved; enrolled students retain access until expiry.
- FR-CRS-07: Slug must be unique, URL-safe, auto-generated from title but editable by admin.
- FR-CRS-08: Price and currency displayed on course detail and browse pages.

### FR-INS (Instructor â€” Content Management)
- FR-INS-01: Instructor views only their assigned courses.
- FR-INS-02: Instructor creates/edits/deletes sections (with order).
- FR-INS-03: Instructor creates/edits/deletes lectures (YouTube unlisted URL, description, order).
- FR-INS-04: Instructor uploads resources per lecture or section (PDF, PPT, ZIP) â†’ Cloudflare R2.
- FR-INS-05: Instructor creates MCQ quizzes (questions, options, correct answer, time limit, pass score, retake allowed).
- FR-INS-06: Instructor schedules live sessions (title, date/time, Zoom/Meet URL).
- FR-INS-07: Instructor creates course announcements.

### FR-STU (Student Experience)
- FR-STU-01: Student dashboard shows enrolled courses with progress.
- FR-STU-02: Student browses all published courses with price displayed.
- FR-STU-03: Student watches YouTube lectures embedded in platform (no raw URL exposed).
- FR-STU-04: Student downloads PDF/PPT resources (signed Cloudflare R2 URL).
- FR-STU-05: Student takes MCQ quizzes; score and pass/fail saved per attempt.
- FR-STU-06: Student views progress % per course (completed lectures / total lectures).
- FR-STU-07: Student marks lectures as completed â†’ logged to ActivityLog.
- FR-STU-08: Student views announcements (course-level + platform-wide).
- FR-STU-09: Student views upcoming live sessions; joins via link.
- FR-STU-10: Student cannot access content of expired enrollments.

### FR-ADM (Admin Management)
- FR-ADM-01: Admin performs CRUD on students, instructors, courses.
- FR-ADM-02: Admin reviews EnrollmentRequests with payment proof image viewer.
- FR-ADM-03: Admin approves/rejects EnrollmentRequests with optional reason.
- FR-ADM-04: Admin creates platform-wide announcements.
- FR-ADM-05: Admin views platform statistics dashboard.
- FR-ADM-06: Admin views and manages Leads (contact form submissions).
- FR-ADM-07: Admin views ActivityLog for auditing.
- FR-ADM-08: Admin can archive courses.

### FR-LEAD (Leads)
- FR-LEAD-01: Public contact/inquiry form captures: name, email, phone, message.
- FR-LEAD-02: Leads stored in Leads table (not linked to user account).
- FR-LEAD-03: Admin views leads list in dashboard.
- FR-LEAD-04: Admin can mark leads as contacted or delete them.

### FR-LOG (Activity Logging)
- FR-LOG-01: System logs user actions to ActivityLog (action, entity_type, entity_id, user_id).
- FR-LOG-02: Actions logged: Logged In, Logged Out, Completed Lecture, Started Quiz, Submitted Quiz, Joined Live Session, Downloaded Resource, Changed Password.
- FR-LOG-03: Admin can query ActivityLog filtered by user or action type.

---

## 4. Non-Functional Requirements

| ID | Requirement |
|---|---|
| NFR-01 | Responsive design â€” desktop and mobile |
| NFR-02 | JWT access token expires in 15min; refresh token in 7 days |
| NFR-03 | All API responses in JSON with standard error format |
| NFR-04 | Files served via Cloudflare R2 signed URLs (time-limited) |
| NFR-05 | YouTube embeds via iframe only; raw video URLs never exposed to client |
| NFR-06 | Passwords stored as bcrypt hash (cost factor â‰¥ 12) |
| NFR-07 | Standard error envelope: `{ statusCode, message, errors[] }` |
| NFR-08 | Transactional emails via SMTP (SendGrid / Mailgun) |
| NFR-09 | Pagination on all list endpoints (default page size: 20) |
| NFR-10 | Soft delete (`deleted_at`) for: users, courses, enrollment_requests |
| NFR-11 | All major tables have `created_at` and `updated_at` |
| NFR-12 | Course slugs are unique, indexed, URL-safe lowercase strings |
| NFR-13 | Expired enrollment access enforced server-side on every request |
| NFR-14 | ActivityLog is append-only (no update/delete endpoints) |

---

## 5. Updated User Stories

### Admin
- As an admin, I want to see a list of enrollment requests with status filter (pending/approved/rejected).
- As an admin, I want to approve an enrollment request, which automatically creates a student account and sends credentials.
- As an admin, I want to reject an enrollment request with a written reason, which triggers a rejection email.
- As an admin, I want to archive a course so it's hidden from new students but existing students retain access.
- As an admin, I want to view leads submitted from the contact form so I can follow up.
- As an admin, I want to view the activity log so I can audit student actions.
- As an admin, I want to see course price and currency when managing courses.

### Instructor
- As an instructor, I want to see only courses assigned to me.
- As an instructor, I want to create sections and lectures with YouTube unlisted links.
- As an instructor, I want to upload PDFs and PPTs attached to lectures.
- As an instructor, I want to build MCQ quizzes with a pass score and optional time limit.
- As an instructor, I want to schedule live sessions with Zoom or Google Meet links.
- As an instructor, I want to post announcements to my course students.

### Student
- As a student, I want to submit an enrollment request with payment proof before I have an account.
- As a student, I want to receive login credentials by email after my enrollment is approved.
- As a student, I want to be forced to change my password on first login.
- As a student, I want to see course prices before enrolling.
- As a student, I want to access a course via its readable URL (slug).
- As a student, I want to watch lectures embedded in the platform without seeing the raw YouTube URL.
- As a student, I want to download PDFs and PPTs from my courses.
- As a student, I want to take quizzes and see my score and pass/fail result.
- As a student, I want to see my progress percentage for each enrolled course.
- As a student, I want to view upcoming live sessions and join with a single click.
- As a student, I want to receive a notification email if my enrollment request is rejected.

### Visitor (Public)
- As a visitor, I want to browse available courses and see their prices.
- As a visitor, I want to submit an enrollment request for a course I want to join.
- As a visitor, I want to submit a contact/inquiry form (lead) if I have questions.
# CodeForge Academy â€” SRS v2.0 | Part 2: Database Schema

---

## 6. Fully Normalized PostgreSQL Database Schema

### 6.1 ERD Overview

```
leads                          users
  id                             id
  name                           email
  email                          password_hash
  phone                          full_name
  message                        phone
  created_at                     role (admin|instructor|student)
                                 is_active
                                 must_change_password
                                 created_at
                                 updated_at
                                 deleted_at

enrollment_requests            enrollments
  id                             id
  applicant_name                 student_id â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â–º users.id
  applicant_email                course_id â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â–º courses.id
  applicant_phone                source_request_id â”€â”€â–º enrollment_requests.id
  course_id â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â–º courses.id
  payment_method                 status (active|expired)
  payment_proof_url              access_expires_at
  status (pending|approved|      created_at
          rejected)              updated_at
  rejection_reason
  reviewed_by â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â–º users.id
  reviewed_at
  created_at
  updated_at

courses                        course_instructors
  id                             id
  title                          course_id â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â–º courses.id
  slug (unique)                  instructor_id â”€â”€â”€â”€â”€â”€â–º users.id
  description                    assigned_at
  thumbnail_url
  category
  price (NUMERIC 10,2)
  currency (VARCHAR 10)
  status (draft|published|
          archived)
  created_by â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â–º users.id
  created_at
  updated_at
  deleted_at

sections                       lectures
  id                             id
  course_id â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â–º courses.id
  title                          section_id â”€â”€â”€â”€â”€â”€â”€â”€â”€â–º sections.id
  order_index                    title
  created_at                     youtube_url
  updated_at                     description
                                 order_index
                                 duration_minutes
                                 created_at
                                 updated_at

resources                      lecture_progress
  id                             id
  lecture_id â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â–º lectures.id (nullable)
  section_id â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â–º sections.id (nullable)
  title                          student_id â”€â”€â”€â”€â”€â”€â”€â”€â”€â–º users.id
  file_url                       lecture_id â”€â”€â”€â”€â”€â”€â”€â”€â”€â–º lectures.id
  file_type (pdf|ppt|zip|other)  completed_at
  file_size_kb                   UNIQUE(student_id, lecture_id)
  created_at
  updated_at

quizzes                        quiz_questions
  id                             id
  course_id â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â–º courses.id
  title                          quiz_id â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â–º quizzes.id
  time_limit_minutes             question_text
  pass_score                     order_index
  allow_retake
  created_at
  updated_at

quiz_options                   quiz_attempts
  id                             id
  question_id â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â–º quiz_questions.id
  option_text                    quiz_id â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â–º quizzes.id
  is_correct                     student_id â”€â”€â”€â”€â”€â”€â”€â”€â”€â–º users.id
                                 score
                                 passed
                                 started_at
                                 submitted_at

quiz_answers                   live_sessions
  id                             id
  attempt_id â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â–º quiz_attempts.id
  question_id â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â–º quiz_questions.id
  selected_option_id â”€â”€â”€â”€â”€â”€â”€â”€â”€â–º quiz_options.id
                                 course_id â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â–º courses.id
                                 instructor_id â”€â”€â”€â”€â”€â”€â–º users.id
                                 title
                                 description
                                 session_url
                                 scheduled_at
                                 created_at
                                 updated_at

announcements                  password_reset_tokens
  id                             id
  course_id (nullable) â”€â”€â”€â”€â”€â”€â”€â–º courses.id
  author_id â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â–º users.id
  title                          user_id â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â–º users.id
  body                           token
  created_at                     expires_at
  updated_at                     used_at

activity_logs
  id
  user_id â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â–º users.id
  action
  entity_type
  entity_id
  metadata (JSONB, optional)
  created_at
```

---

### 6.2 Full Table Definitions

#### `users`
| Column | Type | Constraints |
|---|---|---|
| id | UUID | PK, DEFAULT gen_random_uuid() |
| email | VARCHAR(255) | UNIQUE, NOT NULL |
| password_hash | VARCHAR(255) | NOT NULL |
| full_name | VARCHAR(255) | NOT NULL |
| phone | VARCHAR(20) | |
| role | VARCHAR(20) | NOT NULL, CHECK IN ('admin','instructor','student') |
| is_active | BOOLEAN | NOT NULL, DEFAULT true |
| must_change_password | BOOLEAN | NOT NULL, DEFAULT false |
| created_at | TIMESTAMPTZ | NOT NULL, DEFAULT NOW() |
| updated_at | TIMESTAMPTZ | NOT NULL, DEFAULT NOW() |
| deleted_at | TIMESTAMPTZ | NULL |

#### `courses`
| Column | Type | Constraints |
|---|---|---|
| id | UUID | PK |
| title | VARCHAR(255) | NOT NULL |
| slug | VARCHAR(255) | UNIQUE, NOT NULL |
| description | TEXT | |
| thumbnail_url | VARCHAR(500) | |
| category | VARCHAR(100) | |
| price | NUMERIC(10,2) | NOT NULL, DEFAULT 0 |
| currency | VARCHAR(10) | NOT NULL, DEFAULT 'EGP' |
| status | VARCHAR(20) | NOT NULL, DEFAULT 'draft', CHECK IN ('draft','published','archived') |
| created_by | UUID | NOT NULL, FK â†’ users.id |
| created_at | TIMESTAMPTZ | NOT NULL, DEFAULT NOW() |
| updated_at | TIMESTAMPTZ | NOT NULL, DEFAULT NOW() |
| deleted_at | TIMESTAMPTZ | NULL |

#### `course_instructors`
| Column | Type | Constraints |
|---|---|---|
| id | UUID | PK |
| course_id | UUID | NOT NULL, FK â†’ courses.id ON DELETE CASCADE |
| instructor_id | UUID | NOT NULL, FK â†’ users.id ON DELETE CASCADE |
| assigned_at | TIMESTAMPTZ | NOT NULL, DEFAULT NOW() |
| UNIQUE | (course_id, instructor_id) | |

#### `enrollment_requests`
| Column | Type | Constraints |
|---|---|---|
| id | UUID | PK |
| applicant_name | VARCHAR(255) | NOT NULL |
| applicant_email | VARCHAR(255) | NOT NULL |
| applicant_phone | VARCHAR(20) | |
| course_id | UUID | NOT NULL, FK â†’ courses.id |
| payment_method | VARCHAR(100) | NOT NULL |
| payment_proof_url | VARCHAR(500) | NOT NULL |
| status | VARCHAR(20) | NOT NULL, DEFAULT 'pending', CHECK IN ('pending','approved','rejected') |
| rejection_reason | TEXT | NULL |
| reviewed_by | UUID | NULL, FK â†’ users.id |
| reviewed_at | TIMESTAMPTZ | NULL |
| created_at | TIMESTAMPTZ | NOT NULL, DEFAULT NOW() |
| updated_at | TIMESTAMPTZ | NOT NULL, DEFAULT NOW() |

#### `enrollments`
| Column | Type | Constraints |
|---|---|---|
| id | UUID | PK |
| student_id | UUID | NOT NULL, FK â†’ users.id |
| course_id | UUID | NOT NULL, FK â†’ courses.id |
| source_request_id | UUID | NULL, FK â†’ enrollment_requests.id |
| status | VARCHAR(20) | NOT NULL, DEFAULT 'active', CHECK IN ('active','expired') |
| access_expires_at | TIMESTAMPTZ | NULL |
| created_at | TIMESTAMPTZ | NOT NULL, DEFAULT NOW() |
| updated_at | TIMESTAMPTZ | NOT NULL, DEFAULT NOW() |
| UNIQUE | (student_id, course_id) | |

#### `sections`
| Column | Type | Constraints |
|---|---|---|
| id | UUID | PK |
| course_id | UUID | NOT NULL, FK â†’ courses.id ON DELETE CASCADE |
| title | VARCHAR(255) | NOT NULL |
| order_index | INT | NOT NULL |
| created_at | TIMESTAMPTZ | NOT NULL, DEFAULT NOW() |
| updated_at | TIMESTAMPTZ | NOT NULL, DEFAULT NOW() |

#### `lectures`
| Column | Type | Constraints |
|---|---|---|
| id | UUID | PK |
| section_id | UUID | NOT NULL, FK â†’ sections.id ON DELETE CASCADE |
| title | VARCHAR(255) | NOT NULL |
| youtube_url | VARCHAR(500) | NULL |
| description | TEXT | NULL |
| order_index | INT | NOT NULL |
| duration_minutes | INT | NULL |
| created_at | TIMESTAMPTZ | NOT NULL, DEFAULT NOW() |
| updated_at | TIMESTAMPTZ | NOT NULL, DEFAULT NOW() |

#### `resources`
| Column | Type | Constraints |
|---|---|---|
| id | UUID | PK |
| lecture_id | UUID | NULL, FK â†’ lectures.id ON DELETE CASCADE |
| section_id | UUID | NULL, FK â†’ sections.id ON DELETE CASCADE |
| title | VARCHAR(255) | NOT NULL |
| file_url | VARCHAR(500) | NOT NULL |
| file_type | VARCHAR(20) | NOT NULL, CHECK IN ('pdf','ppt','zip','other') |
| file_size_kb | INT | NULL |
| created_at | TIMESTAMPTZ | NOT NULL, DEFAULT NOW() |
| updated_at | TIMESTAMPTZ | NOT NULL, DEFAULT NOW() |
| CHECK | lecture_id IS NOT NULL OR section_id IS NOT NULL | |

#### `lecture_progress`
| Column | Type | Constraints |
|---|---|---|
| id | UUID | PK |
| student_id | UUID | NOT NULL, FK â†’ users.id |
| lecture_id | UUID | NOT NULL, FK â†’ lectures.id |
| completed_at | TIMESTAMPTZ | NOT NULL, DEFAULT NOW() |
| UNIQUE | (student_id, lecture_id) | |

#### `quizzes`
| Column | Type | Constraints |
|---|---|---|
| id | UUID | PK |
| course_id | UUID | NOT NULL, FK â†’ courses.id ON DELETE CASCADE |
| title | VARCHAR(255) | NOT NULL |
| time_limit_minutes | INT | NULL |
| pass_score | INT | NULL, CHECK (pass_score BETWEEN 0 AND 100) |
| allow_retake | BOOLEAN | NOT NULL, DEFAULT true |
| created_at | TIMESTAMPTZ | NOT NULL, DEFAULT NOW() |
| updated_at | TIMESTAMPTZ | NOT NULL, DEFAULT NOW() |

#### `quiz_questions`
| Column | Type | Constraints |
|---|---|---|
| id | UUID | PK |
| quiz_id | UUID | NOT NULL, FK â†’ quizzes.id ON DELETE CASCADE |
| question_text | TEXT | NOT NULL |
| order_index | INT | NOT NULL |

#### `quiz_options`
| Column | Type | Constraints |
|---|---|---|
| id | UUID | PK |
| question_id | UUID | NOT NULL, FK â†’ quiz_questions.id ON DELETE CASCADE |
| option_text | VARCHAR(500) | NOT NULL |
| is_correct | BOOLEAN | NOT NULL, DEFAULT false |

#### `quiz_attempts`
| Column | Type | Constraints |
|---|---|---|
| id | UUID | PK |
| quiz_id | UUID | NOT NULL, FK â†’ quizzes.id |
| student_id | UUID | NOT NULL, FK â†’ users.id |
| score | INT | NULL |
| passed | BOOLEAN | NULL |
| started_at | TIMESTAMPTZ | NOT NULL, DEFAULT NOW() |
| submitted_at | TIMESTAMPTZ | NULL |

#### `quiz_answers`
| Column | Type | Constraints |
|---|---|---|
| id | UUID | PK |
| attempt_id | UUID | NOT NULL, FK â†’ quiz_attempts.id ON DELETE CASCADE |
| question_id | UUID | NOT NULL, FK â†’ quiz_questions.id |
| selected_option_id | UUID | NULL, FK â†’ quiz_options.id |

#### `live_sessions`
| Column | Type | Constraints |
|---|---|---|
| id | UUID | PK |
| course_id | UUID | NOT NULL, FK â†’ courses.id ON DELETE CASCADE |
| instructor_id | UUID | NOT NULL, FK â†’ users.id |
| title | VARCHAR(255) | NOT NULL |
| description | TEXT | NULL |
| session_url | VARCHAR(500) | NOT NULL |
| scheduled_at | TIMESTAMPTZ | NOT NULL |
| created_at | TIMESTAMPTZ | NOT NULL, DEFAULT NOW() |
| updated_at | TIMESTAMPTZ | NOT NULL, DEFAULT NOW() |

#### `announcements`
| Column | Type | Constraints |
|---|---|---|
| id | UUID | PK |
| course_id | UUID | NULL, FK â†’ courses.id (NULL = platform-wide) |
| author_id | UUID | NOT NULL, FK â†’ users.id |
| title | VARCHAR(255) | NOT NULL |
| body | TEXT | NOT NULL |
| created_at | TIMESTAMPTZ | NOT NULL, DEFAULT NOW() |
| updated_at | TIMESTAMPTZ | NOT NULL, DEFAULT NOW() |

#### `password_reset_tokens`
| Column | Type | Constraints |
|---|---|---|
| id | UUID | PK |
| user_id | UUID | NOT NULL, FK â†’ users.id |
| token | VARCHAR(255) | NOT NULL, UNIQUE |
| expires_at | TIMESTAMPTZ | NOT NULL |
| used_at | TIMESTAMPTZ | NULL |
| created_at | TIMESTAMPTZ | NOT NULL, DEFAULT NOW() |

#### `leads`
| Column | Type | Constraints |
|---|---|---|
| id | UUID | PK |
| name | VARCHAR(255) | NOT NULL |
| email | VARCHAR(255) | NOT NULL |
| phone | VARCHAR(20) | NULL |
| message | TEXT | NULL |
| is_contacted | BOOLEAN | NOT NULL, DEFAULT false |
| created_at | TIMESTAMPTZ | NOT NULL, DEFAULT NOW() |

#### `activity_logs`
| Column | Type | Constraints |
|---|---|---|
| id | UUID | PK |
| user_id | UUID | NOT NULL, FK â†’ users.id |
| action | VARCHAR(100) | NOT NULL |
| entity_type | VARCHAR(100) | NULL |
| entity_id | UUID | NULL |
| metadata | JSONB | NULL |
| created_at | TIMESTAMPTZ | NOT NULL, DEFAULT NOW() |

**action enum values (enforced in app layer):**
`logged_in`, `logged_out`, `changed_password`, `completed_lecture`, `started_quiz`, `submitted_quiz`, `downloaded_resource`, `joined_live_session`
# CodeForge Academy â€” SRS v2.0 | Part 3: APIs, Roadmap, Design Review & Risks

---

## 7. Updated API Endpoint Specifications

> **Base URL:** `/api/v1` | Protected routes require `Authorization: Bearer <token>`

### 7.1 Auth
| Method | Endpoint | Access | Description |
|---|---|---|---|
| POST | /auth/login | Public | Returns JWT + refresh token |
| POST | /auth/refresh | Public | Refresh access token |
| POST | /auth/forgot-password | Public | Send reset email |
| POST | /auth/reset-password | Public | Reset with token |
| POST | /auth/change-password | All | Change password (first login enforcement) |
| GET | /auth/me | All | Current user profile |

### 7.2 Enrollment Requests (NEW â€” separated)
| Method | Endpoint | Access | Description |
|---|---|---|---|
| POST | /enrollment-requests | Public | Submit enrollment request + payment proof |
| GET | /enrollment-requests | Admin | List all requests (filter: status, course_id) |
| GET | /enrollment-requests/:id | Admin | Single request detail with payment proof URL |
| PUT | /enrollment-requests/:id/approve | Admin | Approve â†’ creates student account + enrollment + sends email |
| PUT | /enrollment-requests/:id/reject | Admin | Reject with reason â†’ sends rejection email |

**Approve request body:**
```json
{ "access_expires_at": "2026-12-31T23:59:59Z" }
```

**Reject request body:**
```json
{ "rejection_reason": "Payment proof could not be verified." }
```

### 7.3 Enrollments (NEW â€” separate resource)
| Method | Endpoint | Access | Description |
|---|---|---|---|
| GET | /enrollments | Admin | List all enrollments (filter: student_id, course_id, status) |
| GET | /enrollments/:id | Admin | Single enrollment detail |
| PUT | /enrollments/:id | Admin | Update enrollment (e.g., extend expiry, change status) |
| GET | /students/me/enrollments | Student | My active enrollments |

### 7.4 Courses
| Method | Endpoint | Access | Description |
|---|---|---|---|
| GET | /courses | Public | List published courses (with price, slug) |
| GET | /courses/:slug | Public | Course detail by slug (preview, shows price) |
| POST | /courses | Admin | Create course (auto-generate slug from title) |
| PUT | /courses/:id | Admin | Update course (price, currency, status, slug) |
| DELETE | /courses/:id | Admin | Soft delete |
| PUT | /courses/:id/archive | Admin | Archive course |
| POST | /courses/:id/instructors | Admin | Assign instructor |
| DELETE | /courses/:id/instructors/:userId | Admin | Remove instructor |
| GET | /courses/:slug/content | Student/Instructor | Full content (validates active enrollment) |

### 7.5 Sections
| Method | Endpoint | Access | Description |
|---|---|---|---|
| GET | /courses/:courseId/sections | Auth | List sections |
| POST | /courses/:courseId/sections | Instructor/Admin | Create section |
| PUT | /sections/:id | Instructor/Admin | Update section |
| DELETE | /sections/:id | Instructor/Admin | Delete section |
| PUT | /courses/:courseId/sections/reorder | Instructor/Admin | Reorder (send array of {id, order_index}) |

### 7.6 Lectures
| Method | Endpoint | Access | Description |
|---|---|---|---|
| GET | /sections/:sectionId/lectures | Auth | List lectures |
| POST | /sections/:sectionId/lectures | Instructor/Admin | Create lecture |
| PUT | /lectures/:id | Instructor/Admin | Update lecture |
| DELETE | /lectures/:id | Instructor/Admin | Delete lecture |
| POST | /lectures/:id/complete | Student | Mark completed â†’ writes lecture_progress + ActivityLog |

### 7.7 Resources
| Method | Endpoint | Access | Description |
|---|---|---|---|
| GET | /lectures/:lectureId/resources | Auth | List resources for lecture |
| GET | /sections/:sectionId/resources | Auth | List resources for section |
| POST | /lectures/:lectureId/resources | Instructor | Upload (multipart/form-data) |
| POST | /sections/:sectionId/resources | Instructor | Upload to section |
| DELETE | /resources/:id | Instructor/Admin | Delete resource |
| GET | /resources/:id/download | Student | Returns signed R2 URL (short TTL) + logs ActivityLog |

### 7.8 Quizzes
| Method | Endpoint | Access | Description |
|---|---|---|---|
| GET | /courses/:courseId/quizzes | Auth | List quizzes |
| POST | /courses/:courseId/quizzes | Instructor | Create quiz with questions + options |
| GET | /quizzes/:id | Auth | Quiz detail (questions without revealing correct answers to student) |
| PUT | /quizzes/:id | Instructor | Update quiz |
| DELETE | /quizzes/:id | Instructor/Admin | Delete |
| POST | /quizzes/:id/attempts | Student | Submit attempt â†’ returns score + pass/fail |
| GET | /quizzes/:id/attempts | Student | My past attempts |
| GET | /quizzes/:id/attempts/:attemptId | Student | Attempt detail with answers |

### 7.9 Live Sessions
| Method | Endpoint | Access | Description |
|---|---|---|---|
| GET | /courses/:courseId/live-sessions | Auth | List sessions for course |
| POST | /courses/:courseId/live-sessions | Instructor | Schedule session |
| PUT | /live-sessions/:id | Instructor | Update session |
| DELETE | /live-sessions/:id | Instructor/Admin | Delete |
| GET | /live-sessions/upcoming | Student | Upcoming across all enrolled courses |
| POST | /live-sessions/:id/join | Student | Log join to ActivityLog; return session_url |

### 7.10 Announcements
| Method | Endpoint | Access | Description |
|---|---|---|---|
| GET | /announcements | Student | All announcements (platform + enrolled courses), sorted by date |
| GET | /courses/:courseId/announcements | Auth | Course announcements |
| POST | /courses/:courseId/announcements | Instructor | Create course announcement |
| POST | /announcements | Admin | Create platform-wide announcement (course_id = null) |
| DELETE | /announcements/:id | Admin/Instructor | Delete |

### 7.11 Progress
| Method | Endpoint | Access | Description |
|---|---|---|---|
| GET | /students/me/progress | Student | Progress summary across all enrolled courses |
| GET | /courses/:courseId/progress | Student | Progress % for one course |

### 7.12 Leads
| Method | Endpoint | Access | Description |
|---|---|---|---|
| POST | /leads | Public | Submit contact/inquiry form |
| GET | /leads | Admin | List all leads |
| PUT | /leads/:id/contacted | Admin | Mark lead as contacted |
| DELETE | /leads/:id | Admin | Delete lead |

### 7.13 Activity Logs
| Method | Endpoint | Access | Description |
|---|---|---|---|
| GET | /activity-logs | Admin | List logs (filter: user_id, action, date range) |
| GET | /students/:id/activity | Admin | Activity logs for specific student |

### 7.14 Admin Management
| Method | Endpoint | Access | Description |
|---|---|---|---|
| GET | /admin/students | Admin | List students (paginated) |
| GET | /admin/students/:id | Admin | Student detail + enrollments |
| PUT | /admin/students/:id | Admin | Update student |
| DELETE | /admin/students/:id | Admin | Soft delete |
| GET | /admin/instructors | Admin | List instructors |
| POST | /admin/instructors | Admin | Create instructor account |
| PUT | /admin/instructors/:id | Admin | Update instructor |
| GET | /admin/stats | Admin | Platform statistics |

---

## 8. Updated Development Roadmap

### Phase 0 â€” Foundation (Week 1â€“2)
- [ ] ASP.NET Core 8 project scaffolding + solution structure
- [ ] PostgreSQL schema + EF Core migrations (all v2 tables)
- [ ] JWT auth (access 15min + refresh 7d) + RBAC middleware
- [ ] SMTP email service abstraction (SendGrid adapter)
- [ ] Cloudflare R2 service (upload + signed URL generation)
- [ ] Global exception middleware + standard error envelope
- [ ] Soft delete query filter on EF Core

### Phase 1 â€” Enrollment Flow (Week 3â€“4)
- [ ] Public enrollment request form (Next.js) with R2 proof upload
- [ ] Admin enrollment request list with payment proof image viewer
- [ ] Approve flow: create user â†’ create enrollment â†’ send credentials email
- [ ] Reject flow: update status â†’ send rejection email with reason
- [ ] First-login password change enforcement
- [ ] Password reset via email token

### Phase 2 â€” Course Content (Week 5â€“7)
- [ ] Admin: Course CRUD with slug, price, currency, status (draft/published/archived)
- [ ] Admin: Instructor assignment to courses
- [ ] Instructor: Section + lecture management (order, YouTube URL)
- [ ] Instructor: Resource upload (PDF/PPT/ZIP) â†’ R2 â†’ signed download
- [ ] Student: Browse published courses (slug URL routing in Next.js)
- [ ] Student: Course content view (enrollment + expiry validated server-side)
- [ ] Student: Watch embedded YouTube lecture
- [ ] Student: Download resources (signed R2 URL + ActivityLog)
- [ ] Student: Mark lecture complete â†’ progress calculation

### Phase 3 â€” Learning Tools (Week 8â€“9)
- [ ] Instructor: MCQ quiz builder (questions + options)
- [ ] Student: Quiz player (timed, submit answers)
- [ ] Student: Quiz results + attempt history
- [ ] Instructor: Live session scheduling
- [ ] Student: Upcoming sessions view + join logging
- [ ] Announcements (course-level + platform-wide)

### Phase 4 â€” Admin Dashboard (Week 10â€“11)
- [ ] Admin dashboard: platform stats
- [ ] Admin: Student + instructor management
- [ ] Admin: Leads list and contact status
- [ ] Admin: ActivityLog viewer (filter by user/action)
- [ ] Admin: Course archive workflow

### Phase 5 â€” Polish & Launch (Week 12â€“13)
- [ ] Mobile responsive audit + fixes
- [ ] Security hardening: input sanitization, signed URL TTL tuning, RBAC audit
- [ ] Enrollment expiry background job (daily check â†’ set status = expired)
- [ ] Leads contact form on public site
- [ ] Docker Compose setup (API + PostgreSQL)
- [ ] End-to-end testing of critical flows

---

## 9. Design Review Notes â€” Architectural Decisions

### 9.1 EnrollmentRequest vs Enrollment (Fixed)
**Previously:** A single table tried to be both a request and a record, causing NULL-heavy rows and unclear state.
**Now:** Clean separation. `enrollment_requests` tracks the approval workflow. `enrollments` is only created on approval and tracks active access. `source_request_id` on enrollments links back for audit purposes.

### 9.2 Slug Design
- Auto-generated from title on course creation (e.g., "Python Fundamentals" â†’ `python-fundamentals`).
- Admin can override the slug.
- Uniqueness enforced at DB level with a UNIQUE index.
- Next.js frontend routes: `/courses/[slug]` instead of `/courses/[id]`.
- API also supports `/courses/:slug` for public endpoints.

### 9.3 Price / Currency
- `NUMERIC(10,2)` chosen over FLOAT to avoid floating-point rounding errors on financial data.
- Currency stored as string (e.g., `EGP`, `USD`) for multi-currency display flexibility.
- No payment processing logic in the system (external only).

### 9.4 Activity Log Design
- Append-only table (no update/delete API endpoints).
- `metadata JSONB` column allows flexible extra context (e.g., quiz score, resource filename) without schema changes.
- `entity_type` + `entity_id` pattern enables future analytics queries per entity.

### 9.5 Resource Attachment Model
- Resources can attach to a lecture OR a section.
- DB CHECK constraint enforces at least one of `lecture_id` / `section_id` is not NULL.
- This enables section-level resource libraries (e.g., reference docs that span multiple lectures).

### 9.6 YouTube URL Security
- Backend never returns the raw `youtube_url` to students via API directly.
- Instead, backend returns an embed-safe iframe URL constructed server-side.
- Frontend renders only the iframe; browser DevTools will show the embed URL, not the unlisted link.
- Note: This is soft protection â€” determined users can still find the URL. True protection requires a licensed video hosting solution (V2 consideration: Bunny.net).

---

## 10. Risks & Scalability Considerations

| Risk | Severity | Mitigation |
|---|---|---|
| YouTube unlisted URL leakage | Medium | Server-side embed URL construction; inform academy this is soft protection |
| R2 signed URL abuse (sharing links) | Medium | Set short TTL (5â€“15 min) on download URLs; log downloads in ActivityLog |
| Enrollment expiry not enforced in real-time | Low | Daily background job (Hangfire or hosted service) sets `status = expired`; server validates on every content request |
| Duplicate enrollment requests (same student, same course) | Medium | Detect by applicant_email + course_id on submission; warn admin; enforce UNIQUE on enrollments(student_id, course_id) |
| Single admin bottleneck for approvals | Low | Future V2: allow multiple admin roles or delegated approval |
| Slug conflicts on course rename | Low | Never auto-update slug after creation; admin must explicitly change it |
| JSONB metadata on activity_logs unindexed | Low | Add GIN index on metadata if analytics queries become frequent |
| PostgreSQL connection pool exhaustion under load | Low | Use PgBouncer in front of PostgreSQL in production |
| No rate limiting on public endpoints | High | Add rate limiting middleware on `/enrollment-requests` and `/leads` before launch to prevent spam |
| No CSRF protection on form submissions | Medium | Use SameSite=Strict cookies or CSRF tokens for state-changing public endpoints |

---

## 11. MVP Scope (Updated)

### âœ… Included
- Enrollment request form + admin approval workflow (split tables)
- Auto account creation + credentials email
- First-login password change
- Course CRUD with slug, price, currency
- Sections, lectures, resources (upload + download)
- YouTube embedded lectures
- MCQ quizzes with scoring
- Progress tracking
- Live sessions with join link
- Announcements
- Leads contact form
- ActivityLog for key events
- Admin management dashboard

### âŒ Excluded from MVP
- Discussion forums
- Student-to-instructor messaging
- Certificates of completion
- Course ratings and reviews
- In-app notification bell
- Instructor analytics per-student
- Arabic/English UI toggle
- Coupon/discount codes

---

## 12. Version 2 Features

| Feature | Priority | Notes |
|---|---|---|
| In-app notification system | High | Bell icon, unread count |
| Discussion forum per course | High | Student/instructor Q&A |
| Certificate of completion | High | Auto PDF on 100% progress |
| Student-instructor messaging | Medium | Private DMs |
| Course ratings & reviews | Medium | Star rating + comment |
| Instructor analytics dashboard | Medium | Per-student progress view |
| Arabic/English UI toggle | Medium | RTL support |
| Video hosting (Bunny.net) | Medium | Replace YouTube unlisted for true protection |
| Coupon / discount codes | Low | Admin creates codes for enrollment |
| Mobile app (React Native) | Low | Same API |
| Assignment submission | Low | Students upload, instructor grades |
| Bulk enrollment import | Low | CSV upload for batch enrollments |
| Multi-admin / delegated approval | Low | Admin hierarchy |
