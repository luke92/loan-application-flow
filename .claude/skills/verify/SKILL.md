---
name: verify
description: Run every check for this repo (backend tests, frontend lint, Jest, production build, optionally Playwright e2e) and report what passed or failed. Use before committing or when asked to verify, test everything, or check that nothing is broken.
---

Run the checks in this order and stop to report if one fails. Run from the repo root.

1. **Free the build.** If an `Api` process from this repo is running (it locks the DLLs on
   Windows), stop it first, but only if you started it or the user agrees:
   `Get-Process Api -ErrorAction SilentlyContinue`.
2. **Backend:** `cd backend` then `dotnet test`. Note the per-project totals
   (Domain, Application, Infrastructure, Api).
3. **Frontend**, from `frontend/`:
   - `npm run lint`
   - `npm test` (Jest)
   - `npm run build` (this also type-checks)
4. **E2E (only if asked, or if the form/UI changed):** `npm run test:e2e` from `frontend/`.
   Playwright reuses a dev server already on port 3000 or starts its own.

Report a short table: step, result, test counts. If something fails, show the first error
line and the file/test involved, then propose a fix without applying it unless asked.
Do not commit or push as part of this skill.

If the totals differ from what `README.md` states (backend 44, Jest 44, Playwright 6),
mention it so the docs can be updated.
