# Architecture

## Project structure

```
backend/
  src/
    Domain/          Entities (Customer, LoanApplication), the rule engine + rules,
                      the blacklist / restricted-states abstractions, the US state
                      map, SSN helpers, the request/decision value types. No
                      dependency on anything else in the solution.
    Application/      The use case (SubmitLoanApplicationHandler) and the interfaces
                      it depends on (repositories, outbox writer, unit of work,
                      external service), plus AddApplication() (rules, engine,
                      handler registration). Depends only on Domain.
    Infrastructure/   EF Core DbContext, repositories, the outbox table + its
                      BackgroundService processor, the HTTP client that calls the
                      external service, the config-backed blacklist and
                      restricted states, plus AddInfrastructure() (DbContext, repos,
                      outbox, HTTP client). Depends on Domain + Application,
                      implements their interfaces.
    Api/              ASP.NET Core host: a thin controller, request/response DTOs,
                      and Program.cs (the composition root — calls AddApplication() /
                      AddInfrastructure(), CORS, the global error handler).
  tests/
    Domain.Tests/         Rule engine unit tests.
    Application.Tests/    Use case unit tests, against in-memory fakes.
    Infrastructure.Tests/ Outbox processor tests, against a real SQLite file.
    Api.Tests/             End-to-end HTTP tests via WebApplicationFactory + SQLite.
frontend/              Next.js App Router: the form, /success, /denied.
mock-external-service/ Express app simulating the partner HTTP service.
docker-compose.yml     One-command run of all three, containerized (see README).
run.sh / run.ps1       One-command run of all three, no containers (bash / Windows
                       PowerShell; see README).
```

Dependencies point inward (Api → Application → Domain, Infrastructure →
Application + Domain). Domain has no package references at all; Application only
references Domain (plus the DI abstractions package, for AddApplication()). Nothing
in Domain or Application knows EF Core, ASP.NET Core, or
HTTP exist.

## The rule engine

```csharp
public interface IDenyRule
{
    DenyReason? Evaluate(LoanApplicationRequest request);
}
```

`LoanRuleEngine` takes `IEnumerable<IDenyRule>` through its constructor (resolved by
DI from every `IDenyRule` registered in `AddApplication()`), runs them in registration
order, and returns the first non-null `DenyReason` — or an approved `LoanDecision` if
none fire. `DenyReason` is a `(Code, Message)` record, not a closed enum, so a new
rule can introduce its own reason without touching a shared type.

Current rules (`Domain/Rules`):

- `RestrictedStateRule` — denies when the state is in `IRestrictedStates`, implemented by
  `ConfigurationRestrictedStates` (reads `EligibilityRules:RestrictedStates` from
  configuration; defaults to `["NY"]`, case-insensitive).
- `BlacklistedSsnRule` — denies when the SSN is in `IBlacklist`, implemented by
  `ConfigurationBlacklist` (reads `Blacklist:Ssns` from configuration).

Both read typed options (`BlacklistOptions`, `EligibilityRulesOptions`) validated on
startup, so a bad value (e.g. `"XX"` as a restricted state, or a malformed blacklisted
SSN) stops the app from booting instead of being silently ignored.

**To add a new rule:** create a class implementing `IDenyRule` in `Domain/Rules`, then
register it in `AddApplication()` (`Application/ApplicationServiceCollectionExtensions.cs`):

```csharp
services.AddSingleton<IDenyRule, YourNewRule>();
```

No existing rule, and nothing in `LoanRuleEngine`, needs to change.

Which rules exist is deliberately a code decision, not configuration: an explicit
registration is reviewed and compiler-checked, and a config typo can't silently turn
off a lending rule. Only the data a rule uses (restricted states, blacklisted SSNs)
is configurable. `DenyRuleRegistrationTests` guards the registered set and order.

**Input normalization & validation.** The API rejects (400) a malformed SSN (9 digits,
dashes optional, not starting with 9 — that range is for ITINs), a state that is not a
US state or DC, and a ZIP that is not `#####` or `#####-####`. Before any rule runs,
`LoanApplicationRequest.Normalized()` (called by the handler) upper-cases the state and
strips the SSN to digits only, so the blacklist, the unique-SSN lookup, the database
and the external service all see one canonical form.

## Returning customer & the transaction

`SubmitLoanApplicationHandler` (`Application/UseCases`) looks up the customer by SSN:

- **New SSN** → `new Customer(...)` and `new LoanApplication(...)`, both added to
  their repositories; outbox event type `Created`.
- **Existing SSN** → `customer.UpdateFrom(...)` and
  `application.UpdateRequestedAmount(...)` mutate the existing rows in place (no
  second insert); outbox event type `Updated`.

`Customer.Id` and `LoanApplication.Id` are `Guid`s generated **in the constructor**,
not by the database. That one choice is what makes the transaction simple: because
the real IDs are already known in memory, the handler can build the complete
`ExternalCustomerPayload` (including `CustomerId`/`ApplicationId`) and enqueue the
outbox message *before* calling `SaveChangesAsync` — so the Customer, the
LoanApplication, and the OutboxMessage are all staged on the same `DbContext` and
persisted by a **single `SaveChangesAsync` call**. EF Core wraps that call in one
implicit transaction. If anything fails — a constraint violation, a disk error,
whatever — nothing in that call is written: no half-saved customer, no orphan
application, no outbox row pointing at data that was never saved. There's no manual
`BeginTransaction`/`Commit` anywhere; the "transactional outbox" here is just "one
DbContext, one SaveChanges."

This is covered at two levels: `Application.Tests` uses a fake unit of work that only
makes staged writes visible after a successful "save," and throws on command,
proving the handler never partially persists. `Infrastructure.Tests` goes one level
deeper and proves it against a **real SQLite database**: it stages a Customer, a
LoanApplication, and an OutboxMessage in one `SaveChangesAsync` call alongside a
second customer that violates the unique SSN index, and asserts that after the call
throws `DbUpdateException`, none of the three — not even the otherwise-valid ones —
were written.

## Background event → external service

`OutboxProcessor` (`Infrastructure/Outbox`, a `BackgroundService`) polls
`OutboxMessages` where `Status == Pending`, oldest first, every
`Outbox:PollingIntervalSeconds` (default 5s). It runs in the host's background, never
inside the HTTP request that answers the form.

For each pending message it deserializes the stored JSON payload and calls:

- `IExternalCustomerService.NotifyNewCustomerAsync` → `POST /customers` for a
  `Created` event.
- `IExternalCustomerService.NotifyUpdatedCustomerAsync` → `PUT /customers/{ssn}` for
  an `Updated` event.

On success the message is marked `Processed`. On failure, `Attempts` is incremented;
once it reaches `Outbox:MaxAttempts` (default 5) the message is marked `Failed` and
left alone, otherwise it stays `Pending` for the next poll. This is intentionally the
simplest retry that satisfies "don't retry forever" — a counter and a cutoff, no
backoff curve, no dead-letter queue, no external retry library.

**Why `POST` for new / `PUT` for update, instead of one endpoint:** it mirrors the
semantics a real partner API would likely already expose, keeps the mock trivial (it
never has to inspect the body to decide insert-vs-overwrite), and gives the two event
types distinct, greppable log lines in the mock. The payload shape is identical for
both (`ExternalCustomerPayload`: `CustomerId`, `Ssn`, `FirstName`, `LastName`,
`Street`, `City`, `State`, `Zip`, `CompanyName`, `ApplicationId`,
`RequestedAmount`), sent as camelCase JSON (the default for
`HttpClient.PostAsJsonAsync`/`PutAsJsonAsync`).

`IExternalCustomerService` is the replaceable seam: `ExternalCustomerServiceClient`
(an `HttpClient`-backed implementation) is the only one that exists, but swapping the
transport — a queue, a different protocol — means implementing that interface again,
not touching `SubmitLoanApplicationHandler`.

## Trade-offs / what was left out

- **No real message broker.** The outbox table + polling `BackgroundService` already
  gives "the event survives a crash between save and send, and is eventually
  delivered" — the property that mattered for this exercise. A broker (SQS,
  RabbitMQ, Kafka) would be the right call at real throughput or with multiple
  consumers; it would be pure overhead here.
- **No idempotency key on the mock's endpoints.** The mock stores by SSN, so a retried
  delivery just overwrites the same record — harmless for this flow, but not a
  general idempotency guarantee if the mock were a real, stateful partner service.
- **No EF Core migrations.** `Database.EnsureCreated()` at startup instead. Fine for
  SQLite in a take-home; a real project would want migrations (and a review step for
  the generated SQL) in `Infrastructure`.
- **No handling of concurrent submissions with the same new SSN.** The unique SSN index
  guarantees there is never a duplicate customer, but if two requests for a brand-new
  SSN race, the loser fails the insert and gets a generic `500`. A real system would
  catch the `DbUpdateException` and retry as a returning customer; skipped here because
  the window is tiny and the data stays consistent either way.
- **No authentication.** Explicitly out of scope per the brief.
- **Docker Compose builds production bundles, not dev servers.** `docker-compose.yml`
  runs `dotnet publish`, `next build`/`next start`, and the mock as-is — there's no
  hot reload inside containers. It's meant for "run the whole thing with one command
  to see the flow," not for day-to-day development; see the README for the plain
  `dotnet run`/`npm run dev` commands used while actively working on the code.
- **SSN stored as plain text in SQLite.** A real system would encrypt or tokenize it
  at rest; left as-is here so the persistence layer stays focused on the actual
  exercise (transactional outbox + returning-customer logic).
- **Frontend tests are narrow.** Jest + React Testing Library cover the formatters,
  the validation schema and the form component; Playwright covers the main flows in a
  real browser, but with the backend mocked at the network layer. There is no
  automated test of frontend + real backend together — the backend's `Api.Tests`
  suite covers the HTTP contract on its side, and the full stack was verified by hand.
