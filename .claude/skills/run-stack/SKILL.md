---
name: run-stack
description: Start the backend API, the mock external service and the frontend with run.ps1, exercise the main flows (approved, NY denial, blacklist denial, returning customer, validation errors) against the real API, and shut everything down. Use when asked to run the app, check that the three servers come up, or demo the flows.
---

Windows only (uses `run.ps1`). Run from the repo root.

1. **Check the ports first** (5080, 4000, 3000) with `Get-NetTCPConnection -State Listen`.
   If something is already listening, tell the user and ask before reusing or killing it.
   Only stop processes you started.
2. **Start:** launch `.\run.ps1` in the background, e.g.
   `Start-Process powershell -ArgumentList '-NoProfile','-ExecutionPolicy','Bypass','-File','.\run.ps1' -PassThru -WindowStyle Minimized`
   and keep the returned PID. Wait about 45 seconds (first run installs dependencies).
3. **Health:** `GET http://localhost:4000/customers` and `GET http://localhost:3000`
   must return 200; the API is up when a POST to `http://localhost:5080/api/applications`
   answers (there is no Swagger page).
4. **Flows.** POST JSON to `http://localhost:5080/api/applications` with fields
   `firstName, lastName, street, city, state, zip, companyName, requestedAmount, ssn`.
   Use SSNs that do not exist yet in `backend/src/Api/loan_applications.db` (or ask the user to
   delete the local DB) so "created" vs "updated" is meaningful.

   | Case | Input | Expected |
   |---|---|---|
   | Approved | state `ca`, ssn `314-15-9265` | 200 `Approved` |
   | Returning customer | same SSN without dashes `314159265`, other amount | same `applicationId` |
   | Denied NY | state `NY` | 200 `Denied`, "Applications from New York are not eligible." |
   | Denied blacklist | ssn `777-77-7777` | 200 `Denied` |
   | ITIN SSN | ssn `923-45-6789` | 400 |
   | Bad state / ZIP | state `ZZ`; zip `9000` | 400 |

5. **Mock received it:** after ~8 seconds, `GET http://localhost:4000/customers` should
   list the customer with the digits-only SSN and `lastAction` `updated` for the returning
   one (`created` for a brand-new SSN).
6. **Stop** everything you started: `taskkill /PID <run.ps1 pid> /T /F`, then check that
   ports 5080, 4000 and 3000 are free.

Report which flows passed and anything unexpected. This does not test typing in the
browser (live SSN/ZIP formatting): say so, and point to `npm test` / `npm run test:e2e`
for that.
