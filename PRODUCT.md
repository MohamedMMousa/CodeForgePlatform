# Product

<!-- impeccable:product-schema 1 -->

## Platform

web

## Users

Two primary segments on one platform: grade 8-12 students and college students in Egypt,
learning programming via tracks (flexible course bundles). Secondary: admins reviewing
enrollment/payment; instructors (admin-created only, equal co-teaching permissions, no
lead/TA split) running cohorts. No parent accounts/involvement, even for the minors segment.

## Product Purpose

A live, cohort-based programming education platform — explicitly not a self-paced video
library. Courses run as scheduled live (or in-person) sessions students attend; recordings
exist for revision only, never the primary consumption path. Tracks → cohorts ("batches") →
enrolled students, manual payment-proof + admin review, coupons, attendance, quizzes, code
assignments, gradebook, two-tier certificates, channel-agnostic notifications.

## Positioning

The live-cohort structure itself — real scheduled sessions, real seat capacity ("await next
batch" when full), admin-reviewed enrollment, attendance tracking — is the mechanism a generic
self-paced/on-demand platform can't truthfully claim.

## Operating Context

Region: Egypt (EGP, GMT+2 single timezone). Bilingual Arabic/English with RTL as first-class
from launch, not a later polish pass. Enrollment is request-based, not self-signup: anonymous
request → admin approves → account auto-created + temp password emailed — every enrollment
(including repeat) goes through the same payment-proof + admin-approval process, no fast path.
Manual payment-proof upload, no payment gateway integration. Live sessions hosted externally
(Zoom/Meet/Teams) — platform stores join link + schedule only, never embeds video. WhatsApp
notification channel built but inactive pending Meta Business API credentials.

## Capabilities and Constraints

Scale target year 1: dozens up to ~500 students. Code auto-grading currently deferred to
manual grading (Piston public API went whitelist-only mid-build). Single super-admin tier, no
sub-admin roles. Prerequisites between courses are guidance-only, never enforced.

## Brand Commitments

Name: "CodeForge Academy." The wordmark "CodeForge" stays Latin script in both languages
(brand name, not translated UI copy). Cairo as the shared Arabic/Latin UI typeface is a
confirmed, non-negotiable commitment.

## Evidence on Hand

Real seeded example courses (Python Fundamentals, Data Structures). No confirmed testimonials,
case studies, or press exist yet — future work must not fabricate these.

## Product Principles

Bilingual RTL parity is functional, not cosmetic. Live cohort capacity/scheduling truth
(open/almost-full/awaiting-next-batch) is product behavior the UI must represent accurately.
Manual-review payment/enrollment trust model fits the target market and regulatory comfort
level. Recordings and pre-recorded lessons are supplementary, never primary or
certification-driving.

## Accessibility & Inclusion

Bilingual Arabic (RTL) / English (LTR) parity is a first-class functional requirement, not a
nice-to-have.
