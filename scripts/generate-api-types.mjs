#!/usr/bin/env node
// Regenerates frontend/lib/api-schema.d.ts from the backend's own OpenAPI document,
// so response/request DTO shapes come from the API instead of being hand-mirrored in
// frontend/lib/api.ts. Requires the dev API running locally (see CLAUDE.md commands).
//
// Both outputs are committed — tsc/next build/CI never need a running API; this script
// is run manually and deliberately whenever a backend DTO changes.

import { spawn } from 'node:child_process';
import { writeFile } from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const repoRoot = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const frontendDir = path.join(repoRoot, 'frontend');
const openApiPath = path.join(repoRoot, 'openapi.json');
const schemaOutPath = path.join(frontendDir, 'lib', 'api-schema.d.ts');
const swaggerUrl = process.env.CODEFORGE_API_URL ?? 'http://localhost:5205/swagger/v1/swagger.json';

function run(cmd, args, cwd) {
  return new Promise((resolve, reject) => {
    const child = spawn(cmd, args, { cwd, shell: true, stdio: 'inherit' });
    child.on('error', reject);
    child.on('close', (code) => (code === 0 ? resolve() : reject(new Error(`${cmd} exited ${code}`))));
  });
}

async function main() {
  process.stdout.write(`Fetching ${swaggerUrl} ...\n`);
  let response;
  try {
    response = await fetch(swaggerUrl);
  } catch (err) {
    throw new Error(
      `Could not reach the API at ${swaggerUrl}. Start it first: dotnet run --project src/CodeForge.Api\n${err.message}`,
    );
  }
  if (!response.ok) {
    throw new Error(`Swagger endpoint returned ${response.status} ${response.statusText}`);
  }

  const spec = await response.text();
  await writeFile(openApiPath, spec + '\n', 'utf8');
  process.stdout.write(`Wrote ${path.relative(repoRoot, openApiPath)}\n`);

  await run('npx', ['--yes', 'openapi-typescript', openApiPath, '-o', schemaOutPath], frontendDir);
  process.stdout.write(`Wrote ${path.relative(repoRoot, schemaOutPath)}\n`);
}

main().catch((err) => {
  process.stderr.write(`${err.message}\n`);
  process.exit(1);
});
