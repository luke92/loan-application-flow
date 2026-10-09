# Loan Application Flow

Next.js form -> .NET API (rule engine, SQLite, transactional outbox) -> mock external
HTTP service. See `README.md` (how to run, test data) and `ARCHITECTURE.md` (design and
trade-offs). `REQUIREMENTS.md` is the original take-home brief.

## Commands

```bash
# Backend (from backend/)
dotnet test

# Frontend (from frontend/)
npm run lint && npm test && npm run build
npm run test:e2e        # Playwright, backend mocked

# Run all three services on Windows (from the repo root)
.\run.ps1               # backend :5080, mock :4000, frontend :3000
```

Skills: `/verify` runs every check above; `/run-stack` starts the three services and
exercises the main flows.

## Rules for working here

- **Stop the backend before `dotnet build`/`dotnet test`.** A running `Api.exe` locks the
  DLLs on Windows and the build fails with MSB3027/MSB3021.
- **Commits:** separate commits per topic, plain messages, **no `Co-Authored-By` or any
  Claude/Anthropic attribution**. Do not push unless asked; the user owns commits and
  pushes.
- **Docs:** when behavior changes, update `README.md` and `ARCHITECTURE.md` in the same
  change (they must match the code: test counts, validation rules, config keys).
- **No `docker` here:** Docker Compose is provided but has not been run end to end.

## Architecture conventions

- Dependencies point inward: `Api -> Application -> Domain`, `Infrastructure ->
  Application + Domain`. Domain has no package references.
- New deny rule: a class implementing `IDenyRule` in `Domain/Rules`, registered in
  `AddApplication()` (`Application/ApplicationServiceCollectionExtensions.cs`). Which rules
  exist is code, not configuration; only the data they use (restricted states, blacklisted
  SSNs) is configurable. `DenyRuleRegistrationTests` guards the registered set and order.
- Input is normalized in `LoanApplicationRequest.Normalized()` (upper-case state,
  digits-only SSN) and validated on the request DTO. Nothing downstream should see an SSN
  with dashes.
- Customer/Application/OutboxMessage are saved in one `SaveChangesAsync` (the outbox is
  the transaction boundary); do not add a second save in the same use case.
- Keep it simple: this is a take-home that penalizes over-engineering. Do not add
  abstractions, options validation or patterns that nothing needs.

## Testing notes

- API tests use `WebApplicationFactory` with a temp SQLite file per test class. Read
  configuration lazily (not at service registration) or the factory's overrides are
  ignored and tests share one database.
- Frontend tests: Jest + React Testing Library (not Vitest). Playwright mocks the
  backend at the network layer.
