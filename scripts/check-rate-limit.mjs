#!/usr/bin/env node
// Demonstrates that the API's public-submit rate limiter actually protects a public
// write endpoint, and — the part that actually matters behind the Vercel -> Render
// proxy — that it partitions on the real client IP rather than collapsing every user
// into one bucket. Zero dependencies, same style as check-token-leak.mjs, and for the
// same reason not part of `node scripts/verify.mjs`: it needs a running API.
//
// See CodeForge.Api/RateLimiting/ClientIpResolver.cs for what's being exercised here.

const HELP = `Usage: node scripts/check-rate-limit.mjs [options]

Demonstrates the /leads public-submit rate limiter fires at the configured limit,
that a caller can't dodge it by stuffing extra entries into X-Forwarded-For, and that
distinct real clients get distinct buckets instead of collapsing into one.

  --base-url=<url>   API origin to test (default http://localhost:5205)
  --limit=<n>        expected RateLimiting:PublicSubmit:PermitLimit (default 5)
  --hops=<n>         must match the server's Proxy:TrustedProxyHopCount (default 1)
  --help             this message

REQUIRES the target API running with:
  Proxy__TrustForwardedFor=true
  Proxy__TrustedProxyHopCount=<same value as --hops>
Against the default (TrustForwardedFor=false), the resolver ignores X-Forwarded-For
entirely and every request in this script — all from the same machine — resolves to
this process's own loopback address regardless of what's sent, which would make every
assertion below trivially pass without proving anything about production behavior.

The three checks use disjoint synthetic identities (203.0.113.10/20/30+) so they don't
interfere with each other or need to wait out the rate-limit window between checks.

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

const baseUrl = (argValue('base-url') ?? 'http://localhost:5205').replace(/\/$/, '');
const limit = Number(argValue('limit') ?? 5);
const hops = Number(argValue('hops') ?? 1);

const results = [];
const check = (name, ok, detail = '') => {
  results.push({ name, ok, detail });
  process.stdout.write(`  ${ok ? 'PASS' : 'FAIL'}  ${name}${detail && !ok ? `\n          ${detail}` : ''}\n`);
};

function leadBody() {
  return JSON.stringify({
    name: 'Rate Limit Check',
    email: `rate-limit-check-${Math.random().toString(36).slice(2)}@example.com`,
    phone: null,
    message: 'automated check',
    courseId: null,
  });
}

// Builds an X-Forwarded-For chain that resolves to `trustedIp` under the server's
// configured hop count (ClientIpResolver reads index `1 + hops` from the right), with
// `extraLeftPadding` untrusted entries stuffed in front of it — standing in for a
// caller trying to control a position it doesn't actually own. `trustedIp` sits `hops`
// positions before the right edge, so the padding after it stands in for the hops we
// claim to trust (their exact values don't matter, only that there are `hops` of them
// and they parse as IPs).
function chainFor(trustedIp, extraLeftPadding = 0) {
  const trustedAndAfter = [trustedIp, ...Array.from({ length: hops }, (_, i) => `192.0.2.${100 + i}`)];
  const leftPadding = Array.from({ length: extraLeftPadding }, (_, i) => `198.51.100.${i + 1}`);
  return [...leftPadding, ...trustedAndAfter];
}

async function submitLead(forwardedForChain) {
  return fetch(`${baseUrl}/leads`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      'X-Forwarded-For': forwardedForChain.join(', '),
    },
    body: leadBody(),
  });
}

async function checkLimitFires() {
  process.stdout.write(`\nLimit fires at ${limit}/window (single identity)\n`);
  const chain = chainFor('203.0.113.10');

  for (let i = 0; i < limit; i++) {
    const res = await submitLead(chain);
    check(`request ${i + 1}/${limit} succeeds`, res.status === 200, `got HTTP ${res.status}`);
  }

  const rejected = await submitLead(chain);
  check(`request ${limit + 1} (over limit) is rejected`, rejected.status === 429, `got HTTP ${rejected.status}`);
  check('rejection carries Retry-After', rejected.headers.has('retry-after'));
}

async function checkPrependedPaddingIgnored() {
  process.stdout.write('\nEntries prepended to the left don\'t change the resolved bucket\n');
  const identity = '203.0.113.20';

  // Exhaust the bucket for this identity with the "clean" chain first.
  for (let i = 0; i < limit; i++) {
    await submitLead(chainFor(identity));
  }

  // Same identity, but with junk entries stuffed in front — the position an attacker
  // actually controls when connecting through the trusted hop(s), as opposed to the
  // position the resolver trusts.
  const res = await submitLead(chainFor(identity, 3));
  check(
    'still rejected with forged entries prepended',
    res.status === 429,
    `got HTTP ${res.status} — if this succeeded, the resolver read a position the ` +
      'caller controls instead of the trusted one, and partitioning is not real protection',
  );
}

async function checkDistinctIpsGetDistinctBuckets() {
  process.stdout.write('\n--distinct-ips: different trusted-position values get separate buckets\n');
  // One request each from (limit + 1) distinct identities. If partitioning were
  // broken — e.g. every request resolving to this process's own address, the original
  // bug this script exists to catch — the last of these would collide with the first
  // `limit` and get rejected instead of succeeding.
  let allSucceeded = true;
  for (let i = 0; i < limit + 1; i++) {
    const identity = `203.0.113.${30 + i}`;
    const res = await submitLead(chainFor(identity));
    if (res.status !== 200) {
      allSucceeded = false;
      check(`distinct identity ${i + 1}/${limit + 1} succeeds`, false, `got HTTP ${res.status} for ${identity}`);
    }
  }
  if (allSucceeded) {
    check(`all ${limit + 1} distinct identities succeed independently`, true);
  }
}

async function run() {
  process.stdout.write(`Target: ${baseUrl}  limit=${limit}  hops=${hops}\n`);

  try {
    await fetch(`${baseUrl}/health`);
  } catch (err) {
    check('reach the API', false, `${err} — is the server running at ${baseUrl}?`);
    return;
  }

  await checkLimitFires();
  await checkPrependedPaddingIgnored();
  await checkDistinctIpsGetDistinctBuckets();
}

await run();

const failed = results.filter((r) => !r.ok);
process.stdout.write(
  failed.length === 0
    ? `\nAll ${results.length} checks passed.\n`
    : `\n${failed.length} of ${results.length} checks failed.\n`,
);
process.exit(failed.length === 0 ? 0 : 1);
