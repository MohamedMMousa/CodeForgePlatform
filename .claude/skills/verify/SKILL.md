---
name: verify
description: Run the project's full green-check — dotnet build, dotnet test, tsc --noEmit, next build — and report pass/fail per step. Use after any code change to this repo, before telling the user something is done, and whenever asked to verify, check, or confirm that a change builds and passes.
---

# verify

Run this after **every** change to CodeForge, before reporting back. Never hand over a diff
you have not run.

```bash
node scripts/verify.mjs
```

Runs, in order:

| Step | Command |
|---|---|
| `dotnet build` | `dotnet build CodeForge.slnx` |
| `dotnet test` | `dotnet test CodeForge.slnx --no-build` (69 tests, xUnit) |
| `tsc --noEmit` | `npx tsc --noEmit` in `frontend/` |
| `next build` | `npm run build` in `frontend/` |

Every step runs even after an earlier one fails, so one run surfaces all the red at once.
Exit 0 means all green; exit 1 means at least one step failed, and the tail of each failing
step's output is printed.

## Options

| Flag | Effect |
|---|---|
| `--quick` | Skip `next build` — use mid-loop, not for the final check |
| `--backend` | `dotnet build` + `dotnet test` only |
| `--frontend` | `tsc --noEmit` + `next build` only |

Always finish with a **full** `node scripts/verify.mjs` before reporting done.

## Reporting

Be concise. One line per step, then the actual failure output if anything is red:

```
PASS  dotnet build   8.2s
PASS  dotnet test    1.4s
FAIL  tsc --noEmit   2.1s
```

**Fix everything red before reporting back.** A partial pass is not done.

## What the script cannot check

`verify.mjs` proves the code compiles and the unit tests pass. It does not prove the change is
correct or complete. Before calling a change done, also confirm by hand:

- **Bilingual** — any new user-facing string exists in **both** `en` and `ar` in
  `frontend/lib/i18n.ts`. English-only is an incomplete change.
- **Arabic / RTL** — any UI change is viewed at `/ar`, not just `/en`. No design or
  accessibility tool in this repo checks RTL; it is on you.
- **End-to-end** — a user-visible flow is exercised against the running API (Swagger or the
  browser), per `docs/CODING_STANDARDS.md` §6.

## Notes

- Do **not** run `npm run lint`. It is `next lint` with no ESLint installed and no config, so
  it prompts interactively and hangs.
- A post-edit hook (`.claude/settings.json`) already runs `tsc --noEmit` automatically after
  any edit to a `frontend/**/*.ts(x)` file, so frontend type errors surface immediately. It
  does not replace a full `verify` run.
