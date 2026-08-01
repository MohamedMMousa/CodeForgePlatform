#!/usr/bin/env node
// Guards the two properties that keep auth tokens out of anything a browser, CDN,
// or log aggregator can retain. Zero dependencies, Windows + Linux, same style as
// verify.mjs — but this one needs a running stack, so it is deliberately NOT part
// of `node scripts/verify.mjs`.
//
// What it proves:
//   1. Authenticated HTML and RSC payloads contain no token material.
//   2. Authenticated responses are non-cacheable (`private`, `no-store`) and no
//      route under app/[locale] is prerendered to a shared static artifact.
//
// (2) is the one that actually protects production. Adding `export const revalidate`
// or `force-static` anywhere under app/[locale] would make an authenticated page
// cacheable and shared between users; these assertions fail loudly if that happens.
//
// See docs/ARCHITECTURE.md §3 for why (1) fails against a dev server by design.

import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const repoRoot = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const nextDir = path.join(repoRoot, 'frontend', '.next');

const HELP = `Usage: node scripts/check-token-leak.mjs [options]

Asserts that authenticated pages leak no token material and stay non-cacheable.

  --base-url=<url>   frontend origin to test (default http://localhost:3000)
  --email=<addr>     account to sign in as   (or env CHECK_EMAIL)
  --password=<pw>    its password            (or env CHECK_PASSWORD)
  --build-only       inspect .next artifacts only; no running server needed
  --help             this message

Run against a PRODUCTION server (npm run build && npx next start). A dev server
serializes cookie values into the RSC payload via React's dev-only debug channel;
that is expected and is not a production defect, so this script refuses to certify
a dev run rather than reporting a misleading pass.

Exit 0 when every check passed, 1 otherwise.
`;

const args = process.argv.slice(2);
const argValue = (name) => {
  const hit = args.find((a) => a.startsWith(`--${name}=`));
  return hit ? hit.slice(name.length + 3) : undefined;
};

if (args.includes('--help')) {
  process.stdout.write(HELP);
  process.exit(0);
}

const baseUrl = (argValue('base-url') ?? 'http://localhost:3000').replace(/\/$/, '');
const email = argValue('email') ?? process.env.CHECK_EMAIL;
const password = argValue('password') ?? process.env.CHECK_PASSWORD;
const buildOnly = args.includes('--build-only');

const LOCALES = ['en', 'ar'];
const PROTECTED_PATHS = ['/admin/courses', '/dashboard', '/my-certificates'];

const results = [];
const check = (name, ok, detail = '') => {
  results.push({ name, ok, detail });
  process.stdout.write(`  ${ok ? 'PASS' : 'FAIL'}  ${name}${detail && !ok ? `\n          ${detail}` : ''}\n`);
};

// ---------------------------------------------------------------- build artifacts

// A prerendered artifact for a locale route would mean one user's rendered session
// baked to disk and served to everyone — the failure mode `no-store` alone cannot
// catch, because the page would never reach the request path at all.
function checkBuildArtifacts() {
  process.stdout.write('\nBuild artifacts (.next)\n');

  if (!fs.existsSync(nextDir)) {
    check('.next exists', false, `not found at ${nextDir} — run \`npm run build\` in frontend/ first`);
    return;
  }

  // `next dev` overwrites .next with its own bookkeeping — an empty BUILD_ID and a
  // prerender manifest listing routes it never actually baked. Asserting against
  // that reports a failure that says nothing about production, so refuse instead.
  const buildId = fs.existsSync(path.join(nextDir, 'BUILD_ID'))
    ? fs.readFileSync(path.join(nextDir, 'BUILD_ID'), 'utf8').trim()
    : '';
  if (buildId === '' || fs.existsSync(path.join(nextDir, 'static', 'development'))) {
    check(
      '.next holds production build output',
      false,
      '.next currently holds `next dev` output. Run `npm run build` in frontend/ before\n' +
        '          these assertions mean anything about production.',
    );
    return;
  }

  const manifestPath = path.join(nextDir, 'prerender-manifest.json');
  if (!fs.existsSync(manifestPath)) {
    check('prerender-manifest.json readable', false, `missing at ${manifestPath}`);
    return;
  }

  let manifest;
  try {
    manifest = JSON.parse(fs.readFileSync(manifestPath, 'utf8'));
  } catch (err) {
    check('prerender-manifest.json parses', false, String(err));
    return;
  }

  const isLocaleRoute = (route) => /^\/(\[locale\]|en|ar)(\/|$)/.test(route);
  const prerendered = [
    ...Object.keys(manifest.routes ?? {}),
    ...Object.keys(manifest.dynamicRoutes ?? {}),
  ].filter(isLocaleRoute);

  check(
    'no app/[locale] route is prerendered or ISR-cached',
    prerendered.length === 0,
    prerendered.length ? `prerendered: ${prerendered.join(', ')} — did someone add \`revalidate\`/\`force-static\`?` : '',
  );

  const appDir = path.join(nextDir, 'server', 'app');
  const baked = [];
  if (fs.existsSync(appDir)) {
    const walk = (dir) => {
      for (const entry of fs.readdirSync(dir, { withFileTypes: true })) {
        const full = path.join(dir, entry.name);
        if (entry.isDirectory()) walk(full);
        else if (/\.(html|rsc)$/.test(entry.name)) {
          const rel = `/${path.relative(appDir, full).split(path.sep).join('/')}`;
          if (isLocaleRoute(rel)) baked.push(rel);
        }
      }
    };
    walk(appDir);
  }

  check(
    'no baked .html/.rsc emitted for a locale route',
    baked.length === 0,
    baked.length ? `baked: ${baked.slice(0, 8).join(', ')}` : '',
  );
}

// ---------------------------------------------------------------------- HTTP checks

function cookieHeaderFrom(setCookies) {
  return setCookies
    .map((c) => c.split(';')[0])
    .filter(Boolean)
    .join('; ');
}

function valueOf(setCookies, name) {
  for (const raw of setCookies) {
    const pair = raw.split(';')[0];
    const eq = pair.indexOf('=');
    if (eq !== -1 && pair.slice(0, eq).trim() === name) return pair.slice(eq + 1);
  }
  return undefined;
}

// Enforced in every mode: this is the production-facing property.
function assertNotCacheable(label, response) {
  const cacheControl = response.headers.get('cache-control') ?? '';
  const directives = cacheControl.toLowerCase();
  check(`${label} — Cache-Control present`, cacheControl !== '', 'no Cache-Control header at all');
  check(`${label} — Cache-Control: no-store`, directives.includes('no-store'), `got "${cacheControl}"`);
  check(`${label} — Cache-Control: private`, directives.includes('private'), `got "${cacheControl}"`);
  check(
    `${label} — not publicly/shared cacheable`,
    !/\bpublic\b/.test(directives) && !/\bs-maxage=(?!0\b)\d+/.test(directives),
    `got "${cacheControl}"`,
  );
}

// Enforced only against a production build — see the dev-channel note in the header.
function assertNoTokenMaterial(label, body, secrets) {
  check(`${label} — no access-token value`, !body.includes(secrets.access));
  check(`${label} — no refresh-token value`, !body.includes(secrets.refresh));
  check(`${label} — no "cf_access"/"cf_refresh" literal`, !body.includes('cf_access') && !body.includes('cf_refresh'));
  check(`${label} — no bare JWT ("eyJ")`, !body.includes('eyJ'));
}

async function run() {
  checkBuildArtifacts();
  if (buildOnly) return;

  if (!email || !password) {
    process.stdout.write('\nCredentials missing.\n');
    check('credentials supplied', false, 'pass --email/--password or set CHECK_EMAIL/CHECK_PASSWORD');
    return;
  }

  process.stdout.write(`\nSigning in at ${baseUrl}\n`);

  let login;
  try {
    login = await fetch(`${baseUrl}/api/auth/login`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ email, password }),
      redirect: 'manual',
    });
  } catch (err) {
    check('reach the frontend', false, `${err} — is the server running at ${baseUrl}?`);
    return;
  }

  check('login succeeds', login.status === 200, `got HTTP ${login.status}`);
  if (login.status !== 200) return;

  const setCookies = login.headers.getSetCookie();
  const access = valueOf(setCookies, 'cf_access');
  const refresh = valueOf(setCookies, 'cf_refresh');
  check('login sets cf_access + cf_refresh', Boolean(access && refresh));
  if (!access || !refresh) return;

  const secrets = { access, refresh };
  const cookie = cookieHeaderFrom(setCookies);

  // Commit 9's guarantee: tokens live only in Set-Cookie, never in a response body.
  assertNoTokenMaterial('login body', await login.text(), secrets);

  // Mode detection. The dev server's React debug channel serializes cookie values
  // into the flight payload; certifying that run would be a false negative.
  const probe = await fetch(`${baseUrl}/en/dashboard`, { headers: { cookie }, redirect: 'manual' });
  const probeBody = await probe.text();
  const isDevServer = probeBody.includes('webpack-internal:///');

  if (isDevServer) {
    process.stdout.write('\n');
    check(
      'target is a production build',
      false,
      'dev server detected (webpack-internal:/// in payload). Token assertions are only\n' +
        '          meaningful against `npm run build && npx next start`. Cache-posture results\n' +
        '          above still stand. Re-run against a production server to certify.',
    );
    return;
  }

  for (const locale of LOCALES) {
    for (const routePath of PROTECTED_PATHS) {
      const url = `${baseUrl}/${locale}${routePath}`;
      process.stdout.write(`\n${url}\n`);

      const html = await fetch(url, { headers: { cookie }, redirect: 'manual' });
      const htmlBody = await html.text();

      // Without this, a redirect to /login would trivially contain no tokens and
      // every assertion below would pass while proving nothing.
      check(`HTML — renders authenticated (200 + account visible)`, html.status === 200 && htmlBody.includes(email), `HTTP ${html.status}`);
      assertNotCacheable('HTML', html);
      assertNoTokenMaterial('HTML', htmlBody, secrets);

      const rsc = await fetch(url, { headers: { cookie, RSC: '1' }, redirect: 'manual' });
      const rscBody = await rsc.text();
      check('RSC — served', rsc.status === 200, `HTTP ${rsc.status}`);
      assertNotCacheable('RSC', rsc);
      assertNoTokenMaterial('RSC', rscBody, secrets);
    }
  }
}

await run();

const failed = results.filter((r) => !r.ok);
process.stdout.write(
  failed.length === 0
    ? `\nAll ${results.length} checks passed.\n`
    : `\n${failed.length} of ${results.length} checks failed.\n`,
);
process.exit(failed.length === 0 ? 0 : 1);
