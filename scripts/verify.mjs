#!/usr/bin/env node
// Single source of truth for "is this change green?" — used by the `verify` skill,
// the post-edit hook (.claude/settings.json), and CI. Zero dependencies, Windows + Linux.

import { spawn } from 'node:child_process';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const repoRoot = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const frontendDir = path.join(repoRoot, 'frontend');

const STEPS = [
  { id: 'dotnet build', scope: 'backend', cwd: repoRoot, cmd: 'dotnet build CodeForge.slnx' },
  { id: 'dotnet test', scope: 'backend', cwd: repoRoot, cmd: 'dotnet test CodeForge.slnx --no-build' },
  { id: 'tsc --noEmit', scope: 'frontend', cwd: frontendDir, cmd: 'npx tsc --noEmit' },
  { id: 'next build', scope: 'frontend', cwd: frontendDir, cmd: 'npm run build', slow: true },
];

const HELP = `Usage: node scripts/verify.mjs [options]

  (no options)  dotnet build + dotnet test + tsc --noEmit + next build
  --quick       skip next build (the slow step) — for tight edit loops
  --backend     dotnet build + dotnet test only
  --frontend    tsc --noEmit + next build only
  --hook        post-edit hook mode; reads Claude Code hook JSON on stdin
  --help        this message

Exit 0 when everything passed, 1 otherwise (2 in --hook mode, which is the code
that feeds output back to the agent as blocking feedback).
`;

function run(step) {
  return new Promise((resolve) => {
    const startedAt = Date.now();
    const child = spawn(step.cmd, { cwd: step.cwd, shell: true });
    let output = '';
    child.stdout.on('data', (chunk) => (output += chunk));
    child.stderr.on('data', (chunk) => (output += chunk));
    child.on('error', (err) => resolve({ ...step, ok: false, output: String(err), ms: Date.now() - startedAt }));
    child.on('close', (code) =>
      resolve({ ...step, ok: code === 0, output, ms: Date.now() - startedAt }),
    );
  });
}

function tail(output, lines) {
  const kept = output.split(/\r?\n/).filter((l) => l.trim() !== '');
  return kept.slice(-lines).join('\n');
}

const seconds = (ms) => `${(ms / 1000).toFixed(1)}s`;

async function runSuite(steps) {
  const results = [];
  for (const step of steps) {
    process.stdout.write(`… ${step.id}\n`);
    results.push(await run(step));
  }

  const width = Math.max(...results.map((r) => r.id.length));
  process.stdout.write('\n');
  for (const r of results) {
    process.stdout.write(`  ${r.ok ? 'PASS' : 'FAIL'}  ${r.id.padEnd(width)}  ${seconds(r.ms)}\n`);
  }

  const failures = results.filter((r) => !r.ok);
  for (const f of failures) {
    process.stdout.write(`\n--- ${f.id} (last 30 lines) ---\n${tail(f.output, 30)}\n`);
  }

  process.stdout.write(failures.length === 0 ? '\nAll green.\n' : `\n${failures.length} step(s) failed.\n`);
  return failures.length === 0 ? 0 : 1;
}

// Post-edit hook: typecheck the frontend when, and only when, a frontend TS file changed.
async function runHook() {
  if (process.stdin.isTTY) process.exit(0);

  let raw = '';
  for await (const chunk of process.stdin) raw += chunk;

  let filePath;
  try {
    filePath = JSON.parse(raw)?.tool_input?.file_path;
  } catch {
    process.exit(0);
  }
  if (typeof filePath !== 'string' || filePath === '') process.exit(0);

  const relative = path.relative(frontendDir, path.resolve(repoRoot, filePath));
  const insideFrontend = relative !== '' && !relative.startsWith('..') && !path.isAbsolute(relative);
  if (!insideFrontend || !/\.(ts|tsx|mts|cts)$/.test(filePath)) process.exit(0);

  const result = await run(STEPS.find((s) => s.id === 'tsc --noEmit'));
  if (result.ok) process.exit(0);

  process.stderr.write(`Frontend typecheck failed after editing ${path.basename(filePath)}:\n\n`);
  process.stderr.write(`${tail(result.output, 30)}\n`);
  process.exit(2);
}

const args = process.argv.slice(2);

if (args.includes('--help')) {
  process.stdout.write(HELP);
  process.exit(0);
}

if (args.includes('--hook')) {
  await runHook();
} else {
  let steps = STEPS;
  if (args.includes('--backend')) steps = steps.filter((s) => s.scope === 'backend');
  if (args.includes('--frontend')) steps = steps.filter((s) => s.scope === 'frontend');
  if (args.includes('--quick')) steps = steps.filter((s) => !s.slow);
  process.exit(await runSuite(steps));
}
