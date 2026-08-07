#!/usr/bin/env node
// Guards the Edge Runtime bundle Vercel builds for middleware.ts. It has no Node
// globals at all, so anything reaching it that touches `__dirname` kills the site:
// every request returns 500 MIDDLEWARE_INVOCATION_FAILED with
// "ReferenceError: __dirname is not defined", thrown at module load.
//
// THE ACTUAL CAUSE, found in round 6 after five wrong turns. Recorded in full
// because the wrong turns are what make the check list below make sense:
//
//   next/server -> (edge alias) next/dist/server/web/exports/index.js
//               -> ../spec-extension/user-agent
//               -> next/dist/compiled/ua-parser-js
//
// and that last file's single Node-global use is `__nccwpck_require__.ab =
// __dirname + "/"`. Importing ANYTHING from `next/server` in middleware.ts drags
// ua-parser-js into the edge bundle. That barrel is the only path: nothing else
// under next/dist/server/web requires spec-extension/user-agent, and adapter.js —
// present in every middleware bundle — never touches it. Fixed by writing
// middleware.ts against plain Web Request/Response with no `next/server` import at
// all; see its header comment for why that's supported rather than a hack.
//
// Rounds 1-5 blamed @sentry/nextjs and were wrong, though each found a real
// edge-unsafe Sentry mechanism worth keeping fixed: the `autoInstrumentMiddleware`
// loader rewriting middleware.ts's output; instrumentation.ts's unconditional
// top-level `import * as Sentry` (deleted, Node init moved to
// frontend/lib/sentry-node.ts); and `withSentryConfig` pushing a DefinePlugin plus
// the webpack plugin onto every pass including edge (wrapper removed from
// next.config.mjs entirely). Rounds 1-3 additionally never reached production —
// Vercel was connected to the wrong repo — which is what kept the false lead alive.
// The Sentry markers below are retained as regression cover for all of that.
//
// The checks are NOT equally reliable. Said plainly so a future reader trusts the
// right ones:
//
//   1. ua-parser-js markers on middleware.js. THE trustworthy check for this
//      failure. Unlike everything below it, this one has been observed to flip on
//      the real cause in a local build: before the round-6 fix the local bundle
//      contained UAParser, after it, it does not.
//   2. Sentry SDK markers. Also empirically verified — toggling round 1's and round
//      2's fixes off/on flipped these present/absent every time. Guards a genuine,
//      if ultimately unrelated, class of edge-unsafe injection.
//   3. Generic Node-only globals (__dirname/__filename). Defense in depth, and the
//      weakest of the three — do not read a pass here as "edge-safe." It never once
//      caught this bug despite the offending module sitting in the local bundle the
//      whole time, because webpack folds `__dirname` to a literal here (the local
//      bundle reads `g.ab = "//"`) while Vercel's build does not. That single
//      difference is why five consecutive locally-green fixes shipped broken.
//   4. edge-instrumentation.js existing at all. instrumentation.ts was deleted on
//      purpose, so a clean build should not produce this; if it reappears, someone
//      re-added the source file and its content needs the same scrutiny.
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

// The round-6 cause. `UAParser` is the library's own exported constructor name
// (assigned as a property: `f.UAParser = aa`), and 'Chromium' is a literal from its
// browser regex table — both survive minification, since only local variable names
// get shortened. Either present in middleware.js means something has imported from
// `next/server` again; see this file's header.
//
// Deliberately NOT matching ncc's `__nccwpck_require__` runtime, even though it is
// the thing that reads __dirname inside ua-parser: tested it against a known-clean
// build and it cries wolf. Next vendors its `cookie` parser through ncc too, that one
// IS in every clean middleware bundle via the adapter, and it is edge-safe precisely
// because it guards the access (`"undefined" != typeof __nccwpck_require__ && ...`)
// where ua-parser does not.
const UA_PARSER_MARKERS = [
  { name: 'ua-parser-js (UAParser)', pattern: /\bUAParser\b/ },
  { name: 'ua-parser-js (Chromium regex table)', pattern: /\bChromium\b/ },
];

// Distinctive @sentry/core / @sentry/nextjs property-access names and literal strings
// that survive minification and don't appear anywhere in this app's own source.
// Present in EITHER bundle means Sentry's SDK is reachable from the middleware
// invocation again.
const SENTRY_SDK_MARKERS = [
  { name: 'Sentry.withIsolationScope', pattern: /withIsolationScope/ },
  { name: 'Sentry console logger banner', pattern: /Sentry Logger \[/ },
  { name: 'Sentry template-string marker', pattern: /__sentry_template_string__/ },
];

const EDGE_BUNDLE_MARKERS = [...UA_PARSER_MARKERS, ...SENTRY_SDK_MARKERS, ...NODE_ONLY_MARKERS];

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
