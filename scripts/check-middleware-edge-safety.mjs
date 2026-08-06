#!/usr/bin/env node
// Guards against the bug that took production down: @sentry/nextjs's
// `autoInstrumentMiddleware` (default true) silently rewrites middleware.ts's
// compiled output to import @sentry/nextjs and wrap the handler
// (Sentry.wrapMiddlewareWithSentry) -- none of which appears in middleware.ts's own
// source. That injected code pulled real @sentry/core tracing/scope machinery into
// the Edge Runtime bundle Vercel builds for middleware, which has no Node globals at
// all, and it died at module load with "ReferenceError: __dirname is not defined" on
// every request. Fixed by setting `webpack.autoInstrumentMiddleware: false` in
// next.config.mjs (instrumentation.ts's onRequestError hook already covers
// middleware error capture without the auto-wrap).
//
// Two kinds of checks below, and they are NOT equally reliable -- said plainly so a
// future reader trusts the right one:
//
//   1. Sentry auto-wrap markers on middleware.js specifically. This is the check that
//      actually caught the regression: toggling autoInstrumentMiddleware on/off and
//      rebuilding locally flips these markers present/absent every time.
//   2. Generic Node-only globals (__dirname/__filename) on both edge bundles. Kept as
//      a defense-in-depth net for a *different* future regression of this shape, but
//      be honest about its limits: rebuilding locally with the actual bug present
//      (autoInstrumentMiddleware back on) did NOT reproduce a literal __dirname
//      string in this environment, even though the injected Sentry code was
//      confirmed present via check #1 and production demonstrably crashed on it.
//      Local `next build` isn't byte-identical to Vercel's edge bundling pipeline --
//      treat a pass here as "no known Node-global leak," not "definitely edge-safe."
//
// Needs a build to already exist (`npm run build`), same as
// check-token-leak.mjs --build-only — not part of `--quick` verify for the same
// reason `next build` itself isn't.

import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const repoRoot = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const serverDir = path.join(repoRoot, 'frontend', '.next', 'server');

// Deliberately NOT checking for require(...): Next.js's own edge runtime ships a
// require() compatibility shim for conditionally-probed Node builtins (its bundles
// legitimately contain things like require("node:async_hooks") guarded by feature
// detection that never actually executes under the edge sandbox — the shim's own
// error message is literally "The edge runtime does not support Node.js '...'
// module"). That pattern is normal and present in clean builds; flagging it would cry
// wolf — confirmed by testing it against this app's own known-clean build.
const NODE_ONLY_MARKERS = [
  { name: '__dirname', pattern: /\b__dirname\b/ },
  { name: '__filename', pattern: /\b__filename\b/ },
];

// Specific to middleware.js — the autoInstrumentMiddleware wrapping loader only ever
// targets middleware.ts/proxy.ts, never instrumentation.ts. withIsolationScope is a
// @sentry/core property-access name that survives minification (only local variable
// names get shortened); "Sentry Logger [" is a literal string from the SDK's own
// console logger. Neither appears anywhere in this app's own source.
const SENTRY_AUTO_WRAP_MARKERS = [
  { name: 'Sentry.withIsolationScope (auto-wrap marker)', pattern: /withIsolationScope/ },
  { name: 'Sentry console logger banner (auto-wrap marker)', pattern: /Sentry Logger \[/ },
];

const results = [];
const check = (name, ok, detail = '') => {
  results.push({ name, ok, detail });
  process.stdout.write(`  ${ok ? 'PASS' : 'FAIL'}  ${name}${detail && !ok ? `\n          ${detail}` : ''}\n`);
};

function checkBundle(bundleName, markers) {
  const bundlePath = path.join(serverDir, bundleName);

  if (!fs.existsSync(bundlePath)) {
    check(`${bundleName} exists`, false, `not found at ${bundlePath} — run \`npm run build\` in frontend/ first`);
    return;
  }

  const content = fs.readFileSync(bundlePath, 'utf8');

  for (const marker of markers) {
    const found = marker.pattern.test(content);
    check(
      `${bundleName} — no ${marker.name}`,
      !found,
      found ? `found "${marker.name}" in the compiled bundle` : '',
    );
  }
}

process.stdout.write('Edge Runtime bundle safety\n');
checkBundle('middleware.js', [...SENTRY_AUTO_WRAP_MARKERS, ...NODE_ONLY_MARKERS]);
checkBundle('edge-instrumentation.js', NODE_ONLY_MARKERS);

const failed = results.filter((r) => !r.ok);
process.stdout.write(
  failed.length === 0
    ? `\nAll ${results.length} checks passed.\n`
    : `\n${failed.length} of ${results.length} checks failed.\n`,
);
process.exit(failed.length === 0 ? 0 : 1);
