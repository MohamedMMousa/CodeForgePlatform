# CodeForge Academy — Coding Standards

## 1. Backend Layering Rules

Dependency direction is one-way: `Domain ← Application ← Infrastructure ← Api`.

- **Domain** (`CodeForge.Domain`) — POCO entities only, in
  `Entities/Entities.cs`, grouped by domain comment banners. No methods, no
  validation, no references to any other project. Adding a new entity? Add it to the
  matching domain section in that file, don't create a new file per entity (matches
  the existing pattern).
- **Application** (`CodeForge.Application`) — references Domain only. All business
  logic and validation lives here, in MediatR handlers and FluentValidation
  validators. Never references EF Core types directly, only
  `ICodeForgeDbContext` (which exposes `DbSet<T>` for querying/tracking, hiding the
  concrete DbContext).
- **Infrastructure** (`CodeForge.Infrastructure`) — implements Application's
  interfaces (`ICodeForgeDbContext`, `IJwtTokenGenerator`, `IEmailSender`,
  `IFileStorageService`, …). This is the only project allowed to reference EF Core,
  Npgsql, BCrypt, SMTP, etc.
- **Api** (`CodeForge.Api`) — composition root + HTTP surface. Controllers are thin
  (see `API_CONVENTIONS.md` §2). `Program.cs` wires DI, middleware, auth, rate
  limiting, CORS, localization.

## 2. CQRS Vertical Slice Pattern

Every use case lives in its own folder under `Application/<Feature>/<UseCase>/`, with
exactly three files:

```
Application/Tracks/CreateTrack/
  CreateTrackCommand.cs        // public record CreateTrackCommand(...) : IRequest<TrackDto>;
  CreateTrackCommandValidator.cs
  CreateTrackCommandHandler.cs
```

- Commands mutate; Queries read. Both implement `IRequest<TResponse>`.
- Handlers implement `IRequestHandler<TCommand, TResponse>`, take
  `ICodeForgeDbContext` (and other interfaces) via constructor injection.
- Shared DTOs/mapping/constants for a feature go in `Application/<Feature>/Common/`
  (see `Courses/Common/CourseMapping.cs`, `Courses/Common/ActivityLogFactory.cs` for
  the pattern — reuse this, don't duplicate mapping logic per handler).
- **Never** put logic in the controller that belongs in a handler, and never put
  validation in a handler that belongs in a validator.
- Constants (roles, statuses) live in `Application/Common/Constants/` as static
  classes of `const string` — never hardcode status/role strings.

## 3. C# Style

- `Nullable` and `ImplicitUsings` are enabled everywhere — respect nullability, don't
  suppress warnings with `!` unless the non-null invariant is actually guaranteed
  (e.g. after an existence check).
- Prefer `async`/`await` all the way through; accept a `CancellationToken` on every
  handler `Handle` method and pass it to every awaited call.
- **No comments unless the WHY is non-obvious** (a workaround, a subtle invariant, a
  business-rule reason a reviewer wouldn't guess from the code). Never comment WHAT
  the code does — names should carry that. This matches the general project
  convention.
- Throw typed exceptions from handlers (`KeyNotFoundException`,
  `InvalidOperationException`, `UnauthorizedAccessException`) and let
  `ExceptionHandlingMiddleware` translate them — never catch-and-map inside a
  handler or controller.
- Records for DTOs/commands/queries (`public record CreateTrackCommand(...)`), classes
  for entities and services.

## 4. Testing

- `tests/CodeForge.UnitTests`, xUnit + FluentAssertions. Mirrors the source
  structure: `Authentication/`, `Middleware/`, and a new folder per feature area as
  it gains handlers worth unit-testing in isolation (pure logic — token hashing,
  exception mapping, validators — not full EF-backed handler tests, which need an
  integration-test project not yet set up).
- Test method names: `MethodOrScenario_ExpectedBehavior` (e.g.
  `HashToken_IsDeterministic_ForSameInput`).
- Run everything with `dotnet test CodeForge.slnx` from the repo root before
  considering a backend change done.

## 5. Frontend Standards

- **Locale-first routing** — every page lives under `app/[locale]/...`; never add a
  route outside that segment (the middleware assumes every real route is
  locale-prefixed).
- **i18n** — add every user-facing string to `lib/i18n.ts` in both `en` and `ar` in
  the same change. Use `format(template, values)` for `{token}` interpolation. Never
  hardcode English strings directly in JSX.
- **API calls** — always through `lib/api.ts`'s `apiFetch`, never raw `fetch`. Catch
  `ApiRequestError` to read `.info.status` / `.info.errors` for form-level error
  display.
- **Styling** — use the CSS custom properties from `globals.css`
  (`--bg`, `--card`, `--fg`, `--muted`, `--accent`, `--accent-2`, `--border`) and the
  existing utility classes (`.card`, `.btn`, `.field`, `.grid`, `.badge`, `.notice`).
  Don't hardcode hex colors in components — extend the CSS variables/classes instead
  so the brand stays centrally controlled.
- **Client vs. server components** — pages that only fetch and render are server
  components (`async function Page({ params })`); pages with interactivity
  (forms, auth) are `"use client"`. Keep this split; don't make everything a client
  component by default.
- **TypeScript** — `strict: true` is on; run `npx tsc --noEmit` before considering a
  frontend change done.

## 6. Definition of Done (either stack)

1. Builds clean (`dotnet build CodeForge.slnx` / `npx tsc --noEmit`), no new warnings.
2. Backend: relevant unit tests added/passing (`dotnet test CodeForge.slnx`).
3. If the change touches a user-visible flow, exercised end-to-end (API via
   Swagger/curl at minimum; through the actual browser UI when a frontend page is
   involved — see the project's `verify` skill).
4. Docs updated if the change affects `SRS.md`, `ARCHITECTURE.md`, `DATABASE.md`, or
   `API_CONVENTIONS.md` — don't let these drift from reality.
