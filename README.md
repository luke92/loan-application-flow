# Loan Application Flow

> Demo video: `<PUBLIC_LINK>`

A small loan application flow: a Next.js form submits to a .NET API, a rule engine
decides approve/deny, approved applications are persisted (with a returning-customer
update path), and a background processor relays the result to a mock external HTTP
service via a transactional outbox.

See [`ARCHITECTURE.md`](./ARCHITECTURE.md) for how it's put together and the
trade-offs made along the way.

## Stack

- **Backend**: .NET (ASP.NET Core Web API) + EF Core + SQLite. Built and tested with
  the .NET 10 SDK (satisfies the ".NET 8+" requirement — no 8-specific APIs are used).
- **Frontend**: Next.js (App Router) + TypeScript + Tailwind CSS.
- **Mock external service**: Node + Express, in-memory store.

## Prerequisites

- .NET 8+ SDK ([download](https://dotnet.microsoft.com/download))
- Node.js 18+ and npm

## Running everything with one command

Two options, both start the backend, the mock service, and the frontend together.
Pick whichever fits what you're doing; the per-component commands further down are
what both of them run under the hood.

### Option A — Docker Compose

```bash
docker compose up --build
```

This builds and starts all three services together:

- Backend API → `http://localhost:5080` (SQLite file persisted in the
  `backend-data` named volume, so data survives restarts).
- Mock external service → `http://localhost:4000`.
- Frontend → `http://localhost:3000`.

Inside the Docker network the backend talks to the mock service as `http://mock:4000`
(set via the `ExternalService__BaseUrl` environment variable in
`docker-compose.yml`); the frontend's `NEXT_PUBLIC_API_BASE_URL` is still
`http://localhost:5080` because that fetch happens in the browser on your machine,
not inside the container network. Stop everything with `Ctrl+C`, or `docker compose
down` (add `-v` to also drop the persisted SQLite volume).

This builds production bundles for all three (`dotnet publish`, `next build`), so
there's no hot reload — it's meant for "see the whole flow running" rather than for
active development.

### Option B — `run.sh` (no Docker)

```bash
./run.sh
```

Starts the same three processes as "Running locally" below (`dotnet run`, `npm
start`, `npm run dev`) directly on your machine — no containers. It installs
`node_modules` for the mock and the frontend on first run if missing, copies
`frontend/.env.local.example` to `.env.local` if it isn't there yet, and interleaves
all three services' logs in one terminal, each line prefixed with `[backend]`,
`[mock]`, or `[frontend]`. Since the frontend runs via `next dev`, you get hot reload
— this is the one to use while actively working on the code. Press `Ctrl+C` once to
stop all three.

Requires the same prerequisites as running locally: the .NET SDK and Node.js/npm on
your `PATH`.

### Option C — `run.ps1` (Windows, no Docker)

```powershell
.\run.ps1
```

Same idea as `run.sh`, for Windows PowerShell. Needs the **.NET 10 SDK** and
Node.js/npm (`winget install Microsoft.DotNet.SDK.10 OpenJS.NodeJS.LTS`). It installs
`node_modules` on first run, creates `frontend/.env.local` if missing, and opens each
service (backend, mock, frontend) in its own PowerShell window. Press `Ctrl+C` in the
launching window to stop all three. If PowerShell blocks the script, run
`powershell -ExecutionPolicy Bypass -File .\run.ps1`.

## Running locally

If you'd rather run (or restart) just one part on its own, here's each command by
itself — this is what `run.sh` runs for you. In three separate terminals, in this
order (the frontend and the outbox processor both expect the other two to be
reachable, but nothing will crash if they aren't up yet — requests will just
fail/retry):

### 1. Backend API

```bash
cd backend
dotnet restore
dotnet run --project src/Api
```

Listens on `http://localhost:5080`. The SQLite file
(`backend/src/Api/loan_applications.db`) is created automatically on first run — see
[`ARCHITECTURE.md`](./ARCHITECTURE.md) for why there are no EF Core migrations.

### 2. Mock external service

```bash
cd mock-external-service
npm install
npm start
```

Listens on `http://localhost:4000`. Open `http://localhost:4000/customers` at any time
to see everything it has received (useful to confirm the `POST` → `PUT` sequence for a
returning customer).

### 3. Frontend

```bash
cd frontend
cp .env.local.example .env.local
npm install
npm run dev
```

Opens on `http://localhost:3000`.

## Running the tests

```bash
cd backend
dotnet test
```

Runs all four test projects (`Domain.Tests`, `Application.Tests`,
`Infrastructure.Tests`, `Api.Tests` — 39 tests total):

- **Domain.Tests** — rule engine: restricted states deny (configurable list,
  case-insensitive, message uses the full state name), blacklisted SSN denies, a valid
  application approves, and a newly-added rule doesn't break the existing ones; plus
  SSN normalization (digits only) and validity (9 digits, not starting with 9).
- **Application.Tests** — the use case: state/SSN are stored normalized (upper-case
  state, digits-only SSN), new customer, returning customer (same SSN
  twice → one customer, one application, `Updated` event), a denied request persists
  nothing, and a simulated save failure rolls everything back (against an in-memory
  fake that mimics "staged until SaveChanges" semantics).
- **Infrastructure.Tests** — the outbox processor (a successful delivery is marked
  `Processed`; a failing one retries and is marked `Failed` after the configured max
  attempts), plus a transactionality test against a **real SQLite database**: a
  Customer, a LoanApplication, and an OutboxMessage are staged in one
  `SaveChangesAsync` call alongside a second customer that violates the unique SSN
  index — the whole call throws `DbUpdateException`, and a follow-up query confirms
  nothing from that call was persisted, not even the otherwise-valid rows.
- **Api.Tests** — `WebApplicationFactory` + a real (temp-file) SQLite database,
  covering approved, denied (NY), denied (blacklist, with and without dashes), the
  returning-customer endpoint (same SSN with and without dashes → same customer), state
  stored upper-case regardless of input casing, and `400` for an invalid state, ZIP, or
  SSN (wrong format or starting with 9).

### Frontend tests

```bash
cd frontend
npm test            # Jest + React Testing Library (unit + component)
npm run test:e2e    # Playwright (end-to-end in a real browser)
```

- **Jest** (`npm test`, 44 tests) — `formatSSN` / `formatZip`, the validation schema
  (including the SSN-can't-start-with-9 rule), and the form component: live formatting,
  immediate ITIN error, digits-only ZIP, validation on submit, digits-only SSN in the API
  payload, and the approved / denied / network-error flows.
- **Playwright** (`npm run test:e2e`, 6 tests) — drives the real form in Chromium, with
  the backend mocked at the network layer, so only the frontend is needed (it starts
  `npm run dev` itself, or reuses one already running on port 3000). First time only:
  `npx playwright install chromium`.

## Test data

Blacklisted SSNs (`backend/src/Api/appsettings.json` → `Blacklist:Ssns`, stored digits-only;
entries and incoming SSNs are normalized, so dashes don't matter):

- `777-77-7777`
- `888-88-8888`

| Scenario | What to enter |
|---|---|
| **Approved** | Any valid data with `state` ≠ `NY` and an SSN that isn't blacklisted, e.g. state `CA`, SSN `123-45-6789`. |
| **Denied — New York** | `state: NY` (any SSN). |
| **Denied — blacklist** | `ssn: 777-77-7777` (any state other than NY). |
| **Returning customer** | Submit the form twice with the **same SSN** (e.g. `123-45-6789`) but a different requested amount. Both responses return the same `applicationId`; the mock service receives a `POST /customers` on the first submission and a `PUT /customers/123456789` on the second (check `GET http://localhost:4000/customers`). |

An SSN may not start with 9 (that range is reserved for ITINs): both the form and the API reject it.

Field formats enforced on both the client and the server: SSN must match
`###-##-####` in the form (which displays it with dashes but sends it to the API digits-only), state must be a 2-letter code, ZIP must be `#####` or `#####-####`.
The server is more lenient/strict in two ways: it also accepts an SSN without dashes
(`123456789`). SSNs are normalized to digits only (`123456789`) before the blacklist
check, customer lookup, and persistence, so the database and the external service never
see dashes, and it rejects state codes that aren't a real US state or DC (e.g. `ZZ`).

## What's missing / known limitations

- No authentication (explicitly out of scope).
- No EF Core migrations (`Database.EnsureCreated()` instead).
- No automated frontend tests (manual testing only — see
  [`ARCHITECTURE.md`](./ARCHITECTURE.md)).
