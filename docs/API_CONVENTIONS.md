# CodeForge Academy — API Conventions

Reference for anyone adding or consuming an endpoint. Keep new endpoints consistent
with what's here; update this file if a convention changes.

## 1. Base URL & Routing

- Dev API base: `http://localhost:5205` (see `src/CodeForge.Api/Properties/launchSettings.json`).
- Routes are lowercase, plural nouns, no trailing slash: `/courses`, `/enrollment-requests`, `/catalog/courses`.
- Nested resources use the natural path: `/courses/{courseId}/sections`,
  `/sections/{id}`.
- No API version prefix yet (single version). Introduce `/v2/...` only if a breaking
  change is unavoidable — prefer additive changes first.
- `GET /health` and `GET /health/ready` are unauthenticated, outside MVC (terminal
  middleware, not a controller), and exempt from `PasswordChangeRequiredFilter` and
  rate limiting by construction. `/health` is liveness (use for host restart probes);
  `/health/ready` also checks Postgres connectivity (use for `depends_on`/monitoring,
  never for a restart trigger). See `ARCHITECTURE.md` §3.

## 2. Controllers Are Thin

Every controller method builds a MediatR `IRequest`, sends it, and returns the result.
No business logic, no try/catch in controllers — see `CODING_STANDARDS.md` §2.

```csharp
[HttpPost("login")]
[EnableRateLimiting(RateLimitPolicies.Auth)]
[ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
public async Task<IActionResult> Login(LoginRequest request, CancellationToken ct)
    => await SendAuthRequest(new LoginCommand(request.Email, request.Password), ct);
```

Every action returning a body **must** carry `[ProducesResponseType(typeof(ResponseDto), StatusCodes.Status200OK)]`.
Actions return bare `IActionResult`, which erases the response type — without this
attribute Swashbuckle emits no schema for the response at all (discovered when building
the generated frontend TS client; see `ARCHITECTURE.md` §6 and
`IMPLEMENTATION_ROADMAP.md`'s hardening-pass entry). Skip it only for actions with no
JSON body (`NoContent()`, raw file downloads via `File(...)`).

## 3. Authentication & Authorization

- **JWT Bearer** in the `Authorization: Bearer <token>` header. Access tokens are
  short-lived (`JwtSettings:ExpiryMinutes`, default 15 min); refresh tokens rotate on
  use (`JwtSettings:RefreshTokenExpiryDays`, default 7 days).
- Named policies: `AdminOnly` (role `admin`), `InstructorOnly` (role `instructor`).
  Apply with `[Authorize(Policy = "AdminOnly")]`.
- Ad-hoc multi-role checks use `[Authorize(Roles = "admin,instructor")]` (see
  `SectionsController`) when a policy doesn't already exist for the combination.
- Public/anonymous endpoints are explicitly marked `[AllowAnonymous]` — don't rely on
  the absence of `[Authorize]`; be explicit.
- Role string constants live in `CodeForge.Application.Common.Constants.Roles` — never
  hardcode `"admin"` / `"instructor"` / `"student"` string literals.
- **`MustChangePassword` enforcement:** `PasswordChangeRequiredFilter`
  (`src/CodeForge.Api/Filters/`) is registered globally on `AddControllers` and rejects
  every authenticated request from a user whose token carries
  `must_change_password: "true"` (a claim embedded by `JwtTokenGenerator` at issue time),
  regardless of `[Authorize]`/policy/role — fail-closed, so a newly added endpoint is
  covered automatically. Opt an endpoint out with `[AllowPendingPasswordChange]`
  (currently only `POST /auth/change-password` and `GET /auth/me`); `[AllowAnonymous]`
  endpoints are always exempt. `POST /auth/change-password` mints a fresh token pair
  after clearing the flag, so the caller resumes normal access without a second login —
  see `ChangePasswordCommandHandler`.

## 4. Error Envelope

All errors go through `ExceptionHandlingMiddleware` and come back as
`application/problem+json`:

```json
// 400 — validation (FluentValidation)
{ "title": "Validation Failed", "status": 400, "errors": { "Email": ["'Email' must not be empty."] } }

// 401 — UnauthorizedAccessException
{ "title": "Unauthorized", "status": 401, "detail": "Invalid email or password." }

// 403 — PasswordChangeRequiredException (authenticated, but must change password first)
{ "title": "Forbidden", "status": 403, "detail": "...", "code": "password_change_required" }

// 404 — KeyNotFoundException
{ "title": "Not Found", "status": 404, "detail": "Course not found." }

// 400 — InvalidOperationException (business rule violation, not a validation error)
{ "title": "Bad Request", "status": 400, "detail": "Course is already published." }

// 500 — anything unexpected; message is never leaked to the client
{ "title": "Server Error", "status": 500, "detail": "An unexpected error occurred." }
```

**To signal an error from a handler, throw** — don't return a result object with an
error flag. Use `KeyNotFoundException` for "doesn't exist", `InvalidOperationException`
for "exists but the operation is invalid in this state", `UnauthorizedAccessException`
for "not authenticated, or the credentials are wrong" (maps to 401), and let
FluentValidation's `ValidationException` handle input-shape errors. `PasswordChangeRequiredException`
is the one dedicated custom exception in the repo, reserved for "authenticated but not
permitted until a specific action is taken" (maps to 403) — see `ARCHITECTURE.md` §3.

**The `code` extension field** appears only on errors the frontend must branch on
programmatically (not just display) — currently only `password_change_required`. Most
errors have no `code`; the frontend falls back to `title`/`detail` for display.

## 5. Validation

Every `Command`/`Query` that takes user input has a matching `Validator : AbstractValidator<T>`.
`ValidationBehavior<TRequest, TResponse>` (a MediatR pipeline behavior) runs validators
automatically before the handler executes — handlers can assume their input is valid.
Never validate manually inside a handler; add a validator instead.

## 6. Pagination

Implemented on the 12 list endpoints whose result sets grow without bound:
`GET /users`, `/courses`, `/tracks`, `/coupons`, `/leads`, `/enrollment-requests`,
`/catalog/courses`, `/catalog/tracks`, `/announcements`, `/courses/{id}/cohorts`,
`/my-certificates`, `/instructor/courses`. Query params are `?page=1&pageSize=20`
(SRS default page size 20, max 100 — see `PaginationDefaults` in
`Application/Common/Constants/`), returning:

```json
{ "items": [...], "page": 1, "pageSize": 20, "totalCount": 137 }
```

`PagedResult<T>` (`Application/Common/Models/PagedResult.cs`) is the shared envelope
type. Handlers must add a tiebreaker (`.ThenBy(x => x.Id)`) after any non-unique
`OrderBy`/`OrderByDescending` — otherwise `Skip`/`Take` can duplicate or drop rows
across pages when the primary sort key ties.

**Deliberately exempt** — bounded, `OrderIndex`-ordered child collections that the UI
renders as a complete tree, where paging would mean either N extra round-trips or a
broken partial view: a module's sessions/materials/assessments/assignments, a course's
modules/instructors, `assessments/{id}/my-attempts`, `assignments/{id}/my-submissions`,
`sessions/{id}/materials`. These stay bare arrays. Apply the pagination convention to
any *new* unbounded list endpoint; only exempt genuinely bounded child collections
following the same reasoning.

## 7. File Uploads

`multipart/form-data`, bound via `[FromForm]` to a plain request class with an
`IFormFile` property. Mark the action `[Consumes("multipart/form-data")]` and set an
explicit `[RequestSizeLimit(...)]` (10 MB for payment proofs). Validate content-type
allow-lists in the command validator, not the controller. See
`EnrollmentRequestsController.Submit` for the reference implementation.

## 8. Rate Limiting

Two named policies (`src/CodeForge.Api/RateLimiting/RateLimitPolicies.cs`) plus a
global per-IP fallback:

| Policy | Limit | Applied to |
|---|---|---|
| (global) | 100/min per IP | everything, as a floor |
| `Auth` | 10/min per IP | `/auth/login`, `/refresh-token`, `/forgot-password`, `/reset-password` |
| `PublicSubmit` | 5/min per IP | anonymous public submissions (enrollment request; add the lead form and any future public POST here) |

Exceeded limits return **429 Too Many Requests**. New anonymous public-writing
endpoints should get `PublicSubmit` by default unless there's a reason not to.

## 9. Localization

Send `Accept-Language: en` or `Accept-Language: ar` to influence server-resolved
culture (currently used for request culture resolution; API-returned message
translation is added incrementally — see `ARCHITECTURE.md` §3). The frontend's
`apiFetch` wrapper passes the active locale automatically.

## 10. CORS

Allowed origins are config-driven (`Cors:AllowedOrigins`), not hardcoded. Add a new
frontend origin (e.g. a staging domain) there, never in code.

## 11. Response Shape for Success

Success responses are the DTO directly (no envelope wrapper), `200 OK` for
reads/mutations that return data, `204 No Content` for mutations with nothing to
return (e.g. `ReorderSections`). Keep this consistent — don't introduce a `{ data: ... }`
wrapper.
