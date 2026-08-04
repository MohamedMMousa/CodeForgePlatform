# CodeForge Academy — Deploy Runbook (Vercel + Render + Neon, free tier)

This is the human/agent handoff for go-live. Everything in this doc is **yours to
execute** — account creation, secret entry, the production migration, and the deploy
itself. Code and config were prepared and landed CI-green in a separate series of
commits; this doc is the step-by-step for what happens next. Follow the sections in
order — later steps depend on values (URLs, connection strings) produced by earlier
ones.

Read `docs/ARCHITECTURE.md` §3 and §7 first if you haven't — this doc assumes that
context (the `/api/*` proxy topology, `Database:AutoMigrate`, the rate limiter) rather
than re-explaining it.

---

## ⚠️ The two things most likely to go wrong

**1. `API_INTERNAL_URL` unset on Vercel.** Three separate frontend code paths read
it — `next.config.mjs`, `lib/api.ts`, `middleware.ts` — each with a `localhost:5205`
fallback that is silently wrong in production. Unset it, and every `/api/*` call
500s: nobody can log in, nothing loads. Set it explicitly in the Vercel step below,
and confirm it after deploy before doing anything else.

**2. The production migration is a manual step, forever.** `Database:AutoMigrate`
defaults to `false` and **stays** `false` in production — this was a deliberate Phase 1
decision (auto-migrate-on-boot in production is dangerous: no review, no rollback
point, runs unattended on every deploy). Render will **not** apply schema changes for
you. **You run `dotnet ef database update` against the Neon connection string
yourself**, as its own explicit step, every time the schema changes — including the
very first deploy. See Step 3.

---

## Accounts needed

| Account | For | Free tier |
|---|---|---|
| [Neon](https://neon.tech) | Postgres | 1 project, generous storage, branching included |
| [Cloudflare](https://dash.cloudflare.com) (R2) | Object storage — payment proofs, course materials, DB backups | 10 GB storage, no egress fee |
| [Render](https://render.com) | API hosting | 1 web service, spins down after 15 min idle |
| [Vercel](https://vercel.com) | Frontend hosting | Hobby plan |
| [Sentry](https://sentry.io) | Error monitoring | 5k errors/month |

GitHub Actions secrets (Settings → Secrets and variables → Actions on the repo) aren't
a new account — just repo configuration, in Step 6.

---

## Step 1 — Neon (Postgres)

1. Create a Neon project. Note the **direct** (non-pooled) connection string — pg_dump
   and `dotnet ef` both want this, not the pooled/pgbouncer one the app itself could
   use at runtime.
2. Neon hands you a URI:
   ```
   postgresql://neondb_owner:AbC123XyZ@ep-cool-forest-12345.us-east-2.aws.neon.tech/neondb?sslmode=require
   ```
   **Npgsql (the .NET driver) does not accept this URI form.** Convert it to key/value
   format before using it anywhere in the .NET app (Render env var, local testing):
   ```
   Host=ep-cool-forest-12345.us-east-2.aws.neon.tech;Database=neondb;Username=neondb_owner;Password=AbC123XyZ;SSL Mode=Require
   ```
   `pg_dump`/`pg_restore`/`psql` (used in Step 6 and for restores) accept the **URI
   form directly** — no conversion needed there. Keep both forms handy; you'll use the
   URI for `NEON_DATABASE_URL` (Step 6) and the key/value form for
   `ConnectionStrings__DefaultConnection` (Step 4).
3. Note which Postgres major version your project landed on (Neon's default has moved
   before — 17 → 18 in June 2026 — and may move again). `.github/workflows/backup.yml`
   installs a specific `postgresql-client-<N>` version; if it doesn't match or exceed
   your project's version, bump the version in that workflow.

## Step 2 — Cloudflare R2 (object storage)

1. Enable R2 in the Cloudflare dashboard, create one bucket (e.g. `codeforge-prod`).
2. Create an R2 API token (Account → R2 → Manage API Tokens) with **Object Read &
   Write** on that bucket. Note: Access Key ID, Secret Access Key, and your R2 account
   ID (visible in the R2 dashboard URL or account settings — it's the subdomain in
   `https://<account-id>.r2.cloudflarestorage.com`).
3. This one bucket serves two purposes: application uploads (`Storage__R2*` on
   Render, Step 4) and database backups (`R2_*` Actions secrets, Step 6). Same
   account, same bucket is fine — the app writes under `payment-proofs/` and
   `materials/` prefixes, backups go in the bucket root as `codeforge-<timestamp>.dump`.

## Step 3 — Run the production migration

**This is yours. Do this before deploying the API**, so the schema exists before
anything tries to query it.

```bash
dotnet ef database update \
  --project src/CodeForge.Infrastructure \
  --startup-project src/CodeForge.Api \
  --connection "Host=<neon-host>;Database=<db>;Username=<user>;Password=<password>;SSL Mode=Require"
```

This applies every migration, including `AddRefreshTokenRotationGraceFields` from the
auth phase. Run it again, the same way, any time a future change adds a migration —
`Database:AutoMigrate` will never do this for you in production.

## Step 4 — Render (API)

1. New → Blueprint, point at this repo. Render reads `render.yaml` and proposes the
   `codeforge-api` web service (Docker runtime, free plan, Frankfurt region,
   `/health` as the health check path).
2. Render will prompt for every `sync: false` variable in `render.yaml`. Fill in:

   | Variable | Value |
   |---|---|
   | `ConnectionStrings__DefaultConnection` | Key/value connection string from Step 1 |
   | `JwtSettings__Secret` | A real, long, random secret — **not** the CI throwaway in `ci.yml`. Generate one: `openssl rand -base64 48` |
   | `AdminSeed__Email` | The real first admin's email |
   | `AdminSeed__Password` | The real first admin's password — `mustChangePassword` is enforced, so this is only used for the very first login before being forced to change |
   | `Cors__AllowedOrigins__0` | `https://<your-app>.vercel.app` — you won't have this until Step 5; come back and set it after |
   | `EmailSettings__FrontendBaseUrl` | Same Vercel URL — builds the password-reset link. Also come back after Step 5 |
   | `Storage__R2AccountId` | From Step 2 |
   | `Storage__R2Bucket` | From Step 2 |
   | `Storage__R2AccessKeyId` | From Step 2 |
   | `Storage__R2SecretAccessKey` | From Step 2 |
   | `Sentry__Dsn` | From your Sentry project (backend project — Sentry projects are per-platform; create a .NET project) |

   (`AdminSeed__FullName` isn't listed — it defaults to `"Platform Administrator"` in
   code; only set it if you want something else.)

   Everything else (`ASPNETCORE_ENVIRONMENT`, `Database__AutoMigrate=false`,
   `Proxy__TrustForwardedFor=true`, `Storage__Provider=R2`, `Sentry__Environment`, the
   `Proxy__TrustedProxyHopCount`/`Proxy__EnableDiagnostics`/`Sentry__EnableTestEndpoint`
   toggles) is already inlined in `render.yaml` with safe defaults — nothing more to
   set there.
3. Deploy. First build takes several minutes (multi-stage Docker build). Confirm
   `https://<your-api>.onrender.com/health` returns healthy before moving on.
4. **Cold starts are accepted, not fixed, in this phase.** Render's free plan spins
   the service down after ~15 minutes of no traffic; the next request pays a ~30–60s
   cold-start penalty while it spins back up. For a live-cohort app with scheduled
   class times, this mostly shows up as a slow first request before a session starts.
   A keep-warm ping (e.g. a scheduled GitHub Actions job hitting `/health` every 10
   minutes) is a documented option if this becomes a real problem — **not** set up
   now. Weigh it against Render's shared 750 free instance-hours/month across the
   whole account: keeping one service always warm burns roughly 720 of those hours by
   itself.

## Step 5 — Vercel (frontend)

1. Import the repo. **Root Directory: `frontend`** — the monorepo layout means Vercel
   must be told the frontend isn't at the repo root.
2. **Environment variables** (Project Settings → Environment Variables):

   | Variable | Value | Notes |
   |---|---|---|
   | `API_INTERNAL_URL` | `https://<your-api>.onrender.com` | **The footgun from the top of this doc.** Server-only — do NOT prefix with `NEXT_PUBLIC_`, or it leaks into browser JS for no benefit (the browser never talks to this origin directly; see `ARCHITECTURE.md` §6) |
   | `NEXT_PUBLIC_SENTRY_DSN` | From a **second** Sentry project (JavaScript/Next.js — separate from the API's) | Public by design; Sentry DSNs only allow submitting events, not reading data |
   | `SENTRY_ORG` / `SENTRY_PROJECT` / `SENTRY_AUTH_TOKEN` | From Sentry, org settings → Auth Tokens | Optional — only enables source-map upload at build time. Skip if you don't need de-minified stack traces yet |
3. **Node version**: `frontend/package.json`'s `engines.node: "24.x"` is a hint, not
   enforcement — Vercel resolves its actual build Node version from **Project
   Settings → General → Node.js Version**, a separate dashboard setting. Set it to
   **24.x** there explicitly. (Local dev on this machine may run a newer Node —
   that's a deliberate skew; CI and Vercel both pin the LTS, dev doesn't need to.)
4. Deploy. Once it's live, **go back to Render (Step 4)** and fill in
   `Cors__AllowedOrigins__0` and `EmailSettings__FrontendBaseUrl` with the real
   `https://<your-app>.vercel.app` URL, then redeploy the API so both take effect.

## Step 6 — GitHub Actions secrets (backups)

Repo → Settings → Secrets and variables → Actions → New repository secret:

| Secret | Value |
|---|---|
| `NEON_DATABASE_URL` | The **URI** form from Step 1 (not key/value — `pg_dump` wants the URI) |
| `R2_ENDPOINT` | `https://<account-id>.r2.cloudflarestorage.com` |
| `R2_BUCKET` | From Step 2 |
| `R2_ACCESS_KEY_ID` | From Step 2 |
| `R2_SECRET_ACCESS_KEY` | From Step 2 |

Once set, manually trigger `.github/workflows/backup.yml` once via Actions →
Backup → Run workflow, and watch it succeed, **before** trusting the daily schedule
unattended — see the restore section below for why, and for the restore drill this
same run feeds into.

---

## Post-deploy verification

Work through this in order; each step assumes the previous one passed.

1. **`/health` returns healthy on Render.** `curl https://<your-api>.onrender.com/health`
2. **Login over real HTTPS sets httpOnly cookies.** This is the actual test of the
   cookie topology decided in the auth phase — Vercel and Render are different sites,
   and the `/api/*` proxy is what makes `SameSite=Lax` cookies work anyway. Log in
   through the deployed frontend, inspect cookies in devtools: `cf_access`/
   `cf_refresh` should be `httpOnly`, `Secure`, `SameSite=Lax`.
3. **Full auth flow.** Login → forced change-password on the first admin login
   (`mustChangePassword`) → logout → let the access token expire and reload a
   protected page (confirms the refresh-on-expiry path, not just cookie presence).
4. **Measure the real proxy chain.** Temporarily set `Proxy__EnableDiagnostics=true`
   on Render and redeploy. While logged in as admin, hit
   `GET https://<your-api>.onrender.com/diagnostics/client-ip` — it returns the raw
   `X-Forwarded-For` header, the socket peer, and what the rate limiter currently
   resolves. Compare the header's entry count to what `Proxy__TrustedProxyHopCount`
   currently skips (default `0`, meaning "trust the rightmost entry only"). If the
   resolved IP isn't your own real IP, adjust `Proxy__TrustedProxyHopCount` until it
   is — see `CodeForge.Api/RateLimiting/ClientIpResolver.cs` for the exact
   right-to-left counting. Set `Proxy__EnableDiagnostics` back to `false` once done.
5. **Rate limit fires.** `node scripts/check-rate-limit.mjs --base-url=https://<your-api>.onrender.com`
   (needs `Proxy__TrustForwardedFor=true`, already the render.yaml default, and
   `--hops` matching whatever you set `Proxy__TrustedProxyHopCount` to in step 4).
6. **Sentry receives a test error from both.**
   - API: temporarily set `Sentry__EnableTestEndpoint=true`, redeploy, `POST` to
     `https://<your-api>.onrender.com/diagnostics/sentry-test` while logged in as
     admin (expect a `500`), confirm the event lands in the .NET Sentry project, then
     set the flag back to `false`.
   - Frontend: temporarily set `SENTRY_TEST_ENABLED=true` on Vercel (note: **not**
     `NEXT_PUBLIC_`-prefixed — see `frontend/app/[locale]/sentry-test/page.tsx` for
     why) and redeploy. Visit `/en/sentry-test`, click both buttons, confirm two
     events land in the JavaScript Sentry project, then unset the variable and
     redeploy again.
7. **A backup runs and a restore works.** If you haven't already from Step 6, trigger
   `backup.yml` manually and confirm a `codeforge-<timestamp>.dump` object appears in
   the R2 bucket. Then rehearse a restore — into a **Neon branch**, not the primary
   database, so this proves nothing about live data:
   ```bash
   # Create a branch from the Neon console/CLI first, get its connection string, then:
   pg_restore --clean --if-exists --no-owner --no-privileges \
     --dbname="<neon-branch-connection-uri>" \
     codeforge-<timestamp>.dump
   ```
   This exact command was rehearsed against a local database during development
   (dumped, restored into a scratch database, confirmed row counts and actual row
   data matched) — see the commit history for `.github/workflows/backup.yml`. Confirm
   the branch has the expected tables and row counts, then delete the branch.
8. **Upload survives a redeploy.** Submit an enrollment request with a payment proof
   through the live frontend, redeploy the Render service (Manual Deploy → Deploy
   latest commit — this recreates the container), then confirm an admin can still
   open that payment proof. This is the check that R2 storage actually fixed the
   ephemeral-disk problem it was built for.

Report what passed and what didn't — don't mark this done from documentation alone.

---

## Complete environment variable reference

**Render (API)** — `__` is the .NET config nesting separator.

| Variable | Set by | Purpose |
|---|---|---|
| `ASPNETCORE_ENVIRONMENT` | render.yaml | `Production` — also what keeps Swagger UI off |
| `Database__AutoMigrate` | render.yaml | `false`, explicit — migrations are always a manual step (Step 3) |
| `Proxy__TrustForwardedFor` | render.yaml | `true` — makes per-IP rate limiting real behind Vercel's proxy |
| `Proxy__TrustedProxyHopCount` | render.yaml, you tune | Starts at `0`; set from the Step-4 diagnostic measurement |
| `Proxy__EnableDiagnostics` | render.yaml, you toggle | Temporarily `true` to reach `/diagnostics/client-ip`, then `false` |
| `Storage__Provider` | render.yaml | `R2` — Render's free tier has no persistent disk |
| `Sentry__Environment` | render.yaml | `production` |
| `Sentry__EnableTestEndpoint` | render.yaml, you toggle | Temporarily `true` to reach `/diagnostics/sentry-test`, then `false` |
| `ConnectionStrings__DefaultConnection` | you (Step 4) | Neon, key/value form |
| `JwtSettings__Secret` | you (Step 4) | Real signing key — app refuses to start without it |
| `AdminSeed__Email` / `AdminSeed__Password` | you (Step 4) | First admin; forced password change on first login |
| `Cors__AllowedOrigins__0` | you (Step 4, after Step 5) | `https://<app>.vercel.app` |
| `EmailSettings__FrontendBaseUrl` | you (Step 4, after Step 5) | Same Vercel URL — password-reset links |
| `Storage__R2AccountId` / `R2Bucket` / `R2AccessKeyId` / `R2SecretAccessKey` | you (Step 4) | Upload storage |
| `Sentry__Dsn` | you (Step 4) | Backend error monitoring |

**Vercel (frontend)**

| Variable | Purpose |
|---|---|
| `API_INTERNAL_URL` | **The footgun.** Server-only, `https://<api>.onrender.com` |
| `NEXT_PUBLIC_SENTRY_DSN` | Browser + server/edge error capture (frontend's own Sentry project) |
| `SENTRY_ORG` / `SENTRY_PROJECT` / `SENTRY_AUTH_TOKEN` | Optional — source-map upload only |
| `SENTRY_TEST_ENABLED` | Temporarily `true` for verification step 6, then unset. **Not** `NEXT_PUBLIC_`-prefixed — see the frontend Sentry commit for why that matters |
| Node.js Version (dashboard setting, not an env var) | `24.x`, set in Project Settings |

**GitHub Actions secrets**

| Secret | Purpose |
|---|---|
| `NEON_DATABASE_URL` | Direct (non-pooled) Neon URI — `pg_dump` wants a direct connection |
| `R2_ENDPOINT` / `R2_BUCKET` / `R2_ACCESS_KEY_ID` / `R2_SECRET_ACCESS_KEY` | Backup destination |

---

## Deferred (explicitly, not oversights)

- **Python auto-grader and load testing** — need a paid always-on container; out of
  scope for this free-tier go-live. See `ARCHITECTURE.md` §7.
- **Direct-to-origin rate-limit bypass.** A caller who knows the `*.onrender.com` URL
  can still forge `X-Forwarded-For` to dodge the per-client rate limit — the limiter
  is spam friction, not a security boundary, and no positional header-parsing scheme
  closes that gap entirely. If it becomes a real problem, the cheap fix is Cloudflare
  (free tier) in front of the Render origin, rejecting non-Cloudflare traffic. See
  `CodeForge.Api/RateLimiting/ClientIpResolver.cs`.
- **Vercel Static IPs / Secure Compute** — would let the rate limiter trust
  `KnownProxies` network ranges instead of positional header parsing, but is
  Pro/Enterprise-only ($100/mo extra). Revisit if the plan ever changes.
