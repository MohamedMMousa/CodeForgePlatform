#!/usr/bin/env node
// Guards against the bug that took production down -- three times, before landing on
// a fix that could actually be verified rather than hoped for. @sentry/nextjs's SDK
// code ended up loaded into the Edge Runtime bundle Vercel builds for middleware.ts,
// which has no Node globals at all, and it died at module load with
// "ReferenceError: __dirname is not defined" on every request. Three separate
// injection paths, found and fixed one at a time:
//   1. `autoInstrumentMiddleware` (default true) silently rewrote middleware.ts's
//      compiled output to import @sentry/nextjs and wrap the handler
//      (Sentry.wrapMiddlewareWithSentry) -- fixed via
//      `webpack.autoInstrumentMiddleware: false` in next.config.mjs.
//   2. instrumentation.ts's unconditional, top-level `import * as Sentry from
//      "@sentry/nextjs"` plus `export const onRequestError = Sentry.captureRequestError`
//      referenced Sentry outside any runtime guard, so the edge compilation of that
//      file could never treat the import as dead code regardless of what was inside
//      register()'s `if` check.
//   3. Moving to a dynamic `import()` inside a `NEXT_RUNTIME !== "nodejs"` early
//      return did NOT fix it either -- rebuilt and grepped the compiled
//      edge-instrumentation.js, Sentry SDK content was still present. Whether
//      webpack actually dead-code-eliminates that guard for the edge compilation
//      pass could not be confirmed one way or the other from the minified output.
//      Rather than keep guessing at *how* to gate a Sentry reference inside a
//      dual-compiled file, instrumentation.ts was deleted entirely and Node-runtime
//      Sentry init moved to frontend/lib/sentry-node.ts, imported only from the root
//      layout (a Server Component this app never compiles for the edge runtime) --
//      a structural guarantee instead of a hoped-for optimization. See
//      lib/sentry-node.ts for the full reasoning.
//
// Three kinds of checks below, and they are NOT equally reliable -- said plainly so a
// future reader trusts the right ones:
//
//   1. Sentry SDK markers on middleware.js (and edge-instrumentation.js, if it still
//      exists -- see below). This is what actually caught both round-1 and round-2's
//      regressions: toggling either fix off/on and rebuilding locally flipped these
//      markers present/absent every time, for both bundles.
//   2. Generic Node-only globals (__dirname/__filename) on the same bundles. Kept as
//      a defense-in-depth net for a *different* future regression of this shape, but
//      be honest about its limits: rebuilding locally with either bug deliberately
//      reintroduced did NOT reproduce a literal __dirname string in this environment,
//      even though the injected Sentry code was directly confirmed present via check
//      #1 both times, and production demonstrably crashed on it both times. Local
//      `next build` isn't byte-identical to Vercel's edge bundling pipeline for this
//      failure mode -- treat a pass here as "no known Node-global leak," not
//      "definitely edge-safe."
//   3. edge-instrumentation.js existing at all. Since instrumentation.ts was deleted,
//      this file should NOT be produced by a clean build -- if it reappears, someone
//      re-added an instrumentation.ts, and its content needs the same scrutiny this
//      script already gives middleware.js.
//
// Needs a build to already exist (`npm run build`), same as
// check-token-leak.mjs --build-only — not part of `--quick` verify for the same
// reason `next build` itself isn't.

import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const repoRoot = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const frontendDir = path.join(repoRoot, 'frontend');
const serverDir = path.join(frontendDir, '.next', 'server');

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

// Distinctive @sentry/core / @sentry/nextjs property-access names and literal strings
// that survive minification (only local variable names get shortened) and don't
// appear anywhere in this app's own source. Present in EITHER bundle means Sentry's
// SDK — and whatever inside it produces __dirname on Vercel's real edge runtime — is
// reachable from the middleware invocation again.
const SENTRY_SDK_MARKERS = [
  { name: 'Sentry.withIsolationScope', pattern: /withIsolationScope/ },
  { name: 'Sentry console logger banner', pattern: /Sentry Logger \[/ },
  { name: 'Sentry template-string marker', pattern: /__sentry_template_string__/ },
];

const EDGE_BUNDLE_MARKERS = [...SENTRY_SDK_MARKERS, ...NODE_ONLY_MARKERS];

const results = [];
const check = (name, ok, detail = '') => {
  results.push({ name, ok, detail });
  process.stdout.write(`  ${ok ? 'PASS' : 'FAIL'}  ${name}${detail && !ok ? `\n          ${detail}` : ''}\n`);
};

function checkBundle(bundleName, markers, { requireExists }) {
  const bundlePath = path.join(serverDir, bundleName);

  if (!fs.existsSync(bundlePath)) {
    check(`${bundleName} exists`, !requireExists, requireExists ? `not found at ${bundlePath} — run \`npm run build\` in frontend/ first` : '');
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
checkBundle('middleware.js', EDGE_BUNDLE_MARKERS, { requireExists: true });

// instrumentation.ts was deleted on purpose — see the header comment. Its compiled
// edge output should not exist; if it does, someone brought the file back, and it
// needs the same content checks middleware.js gets, not a free pass.
const instrumentationSourceExists = fs.existsSync(path.join(frontendDir, 'instrumentation.ts'));
checkBundle('edge-instrumentation.js', EDGE_BUNDLE_MARKERS, { requireExists: instrumentationSourceExists });

const failed = results.filter((r) => !r.ok);
process.stdout.write(
  failed.length === 0
    ? `\nAll ${results.length} checks passed.\n`
    : `\n${failed.length} of ${results.length} checks failed.\n`,
);
process.exit(failed.length === 0 ? 0 : 1);
