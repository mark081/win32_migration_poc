# AGENTS.md - Connected Stage

## Mission

This repository is the Connected-stage POC for evolving the Legacy Tool Lending application toward the architecture in `docs/north-star-architecture.md`.

The Connected objective is narrow and evidence-driven:

> Prove that the existing Win32 client can use a remotely hosted service securely and reliably without changing business outcomes or moving authoritative workflow data to a new system.

Preserve the Legacy baseline. Add controlled connectivity, governed contracts, reliable events, identity context, telemetry, and repeatable delivery. Do not start the Hybrid or SaaS rewrite early.

## Working environment

- Authoritative Connected workspace: `C:\src\win32_migration_poc` on the Windows remote host.
- Required branch: `connected`.
- Confirm branch and status before editing:

  ```powershell
  git branch --show-current
  git status --short --branch
  ```

- Do not modify `legacy` or `main`, merge between stages, push, or open a pull request unless the user explicitly requests it.
- Preserve user changes. Never discard, reset, or overwrite unrelated work.
- Do not commit build output, restored packages, credentials, local databases, logs, or machine-specific configuration.

## Sources of truth

Read these before making architectural or cross-cutting changes:

1. `docs/north-star-architecture.md` - architectural destination and durable decisions.
2. `docs/testing-evolution.md` - Legacy baseline and Connected promotion gate.
3. `docs/architecture.md` - current runtime, rule ownership, and data ownership.
4. `docs/legacy-shared-credential.md` - intentionally modeled credential exposure and retirement path.
5. `README.md` - supported build, run, deployment, and repository conventions.

If code and documentation disagree, investigate and update the affected documentation with the change. Do not silently redefine the architecture.

## Connected-stage boundaries

### What Connected must prove

- The current desktop workflows operate against a configurable remote API endpoint.
- Local and Connected endpoints satisfy the same versioned API contracts.
- Transport uses TLS with certificate and hostname validation.
- Endpoint addresses and secrets are externalized; no production secret is compiled into the client or repository.
- The practice-shared Legacy credential can be disabled without falling back to the demo key.
- Authentication and audit context can evolve toward explicit practice, user, and device identity.
- Timeouts, unavailability, interrupted requests, and retries produce understandable UI behavior.
- Retried writes remain idempotent across real network failures.
- Business changes produce reliable, replayable events suitable for an additive cloud consumer.
- Correlation IDs, structured logs, health checks, metrics, and audit records make failures diagnosable.
- Builds, tests, deployment, configuration, rollback, and recovery are repeatable.
- Legacy-local and Connected runs produce equivalent outcomes, database state, and audit evidence.

### What remains authoritative

During Connected:

| Concern | Authoritative owner |
| --- | --- |
| Tool, member, reservation, checkout, and return data | Existing PostgreSQL database |
| Locked transactional business rules | Existing PostgreSQL routines |
| API contracts, orchestration, idempotency coordination, and error mapping | Existing application service |
| Immediate operator validation and feedback | Win32 client and NativeRules prechecks |
| Audit business record | Existing PostgreSQL audit log |
| Connected telemetry and event projections | Additive only; never workflow authority |

There must be one authoritative writer for each business datum. Connected components may observe, project, or report Legacy state, but must not introduce uncontrolled dual writes.

### Explicit non-goals

Do not introduce any of the following as part of Connected work unless a separately approved experiment requires it:

- Big-bang rewrite or forced migration.
- React replacement of the complete Win32 application.
- Cloud ownership of transactional workflow data.
- Multi-tenant SaaS data partitioning.
- Full offline command processing or bidirectional conflict resolution; that is primarily a Hybrid concern.
- Microservices, Kubernetes, service mesh, cells, or multi-cloud operation.
- Direct client, analytics, partner, or cloud access to PostgreSQL tables.
- A speculative data lake, marketplace, or generic platform.
- Removal of the x86 client, native DLL, database routines, or Legacy tests before replacement evidence exists.

## Architecture rules

1. **Encapsulate before extracting.** Map existing callers, rules, stored routines, and data behavior; protect them with tests; expose them through governed contracts.
2. **Preserve business behavior.** Refactoring or connectivity changes must not alter documented outcomes unless the change is explicitly approved and baselined.
3. **Use versioned contracts.** Keep `/api/v1` behavior compatible. Add explicit request/response models, stable error codes, and contract tests for new behavior.
4. **Keep the desktop independent of the database.** The Win32 client communicates only through the API and never uses Npgsql, SQL, or database credentials.
5. **Make writes idempotent.** Every externally retryable write requires an idempotency key, durable deduplication, and tests for same-key replay and conflicting payloads.
6. **Publish reliably.** Business events must be committed atomically with the business transaction, normally through an outbox. Do not publish an event and update business state as unrelated operations.
7. **Keep events facts, not commands.** Include stable event ID, event type and version, occurred-at time, aggregate identity, practice context, correlation/request ID, and minimum necessary payload.
8. **Protect compatibility.** Support documented old-client/new-service version combinations and fail clearly when a combination is unsupported.
9. **Prefer a modular monolith.** Add internal modules and seams before adding deployable services. A new service requires evidence for independent scale, ownership, resilience, or deployment.
10. **Keep infrastructure replaceable.** Isolate cloud SDKs, identity providers, messaging, and telemetry exporters behind application contracts where practical.

## Security and privacy

- Never commit real API keys, passwords, certificates, tokens, connection strings, customer data, PHI, or PII.
- Keep PostgreSQL private. Do not open port 5432 to the internet.
- Bind public-facing behavior only to the intended interface and protect it with TLS, authentication, authorization, input validation, rate/size limits, and audit.
- Do not disable certificate validation to make a test pass.
- Do not log secrets, credential contents, authorization headers, or sensitive request bodies.
- Use least privilege for application, database, deployment, and cloud identities.
- Treat `X-Actor` as audit context, not trusted authentication, until a verified identity layer supplies it.
- Keep the Legacy shared-credential behavior visibly isolated and removable. A missing configured credential must fail closed.
- Use synthetic POC data only.

## Network and failure behavior

- Set explicit connect, request, and operation timeouts.
- Retry only operations known to be safe or protected by idempotency.
- Use bounded retries with backoff and jitter; never retry indefinitely in the UI thread.
- Distinguish validation, authentication, authorization, conflict, timeout, unavailable-service, and unexpected failures.
- Preserve correlation/request IDs across client, API, database audit, event, and telemetry boundaries.
- Never report success until the authoritative operation is committed.
- An ambiguous timeout after a write must be resolved by replaying the same idempotency key or querying operation status, not by issuing an unrelated new write.

## Testing requirements

The Legacy suites are permanent regression evidence during Connected work. Do not weaken or delete them merely because implementation boundaries change.

For every Connected change:

1. Identify affected scenarios in `docs/testing-evolution.md`.
2. Add a failing test or characterization test before changing behavior when practical.
3. Run the smallest relevant test during development.
4. Before declaring completion, run the applicable Connected gate.

Minimum Connected evidence includes:

- Clean Release build.
- Native, database, API, and applicable FlaUI Legacy tests still pass.
- Contract suite runs against both local and Connected endpoints.
- TLS certificate, hostname, and authentication coverage.
- Timeout, retry, service-unavailable, and interrupted-write coverage.
- Idempotent replay through a simulated network failure.
- External configuration and fail-closed credential coverage.
- UI error handling without corrupted local or server state.
- Representative read/write latency baseline.
- Equivalent normalized API results, database state, and audit records in both environments.

Standard commands:

```powershell
Set-Location C:\src\win32_migration_poc
Set-ExecutionPolicy -Scope Process Bypass -Force
.\scripts\Setup-Prerequisites.ps1
.\scripts\Build.ps1 -Configuration Release
.\scripts\Run-Tests.ps1 -Configuration Release
```

Run FlaUI tests only in an interactive, unlocked RDP session:

```powershell
.\scripts\Run-UiTests.ps1 -Configuration Release
```

An SSH session, Windows service session, disconnected RDP session, or locked desktop is not a valid FlaUI environment.

Do not claim a test passed unless it was run. Report skipped tests and the exact reason.

## Implementation conventions

- Preserve compatibility with the currently supported Windows Server, .NET Framework, C++ language level, x86 target, and PostgreSQL versions unless an approved change updates those constraints.
- Follow `.editorconfig`, `.clang-format`, `.csharpierrc.json`, and `PSScriptAnalyzerSettings.psd1`.
- Prefer existing scripts and patterns over one-off commands.
- Keep changes small and aligned to one vertical slice or one enabling capability.
- Avoid drive-by formatting and unrelated refactoring.
- Validate all input at the API boundary; client validation exists for usability, not authority.
- Parameterize every SQL value. Keep database transaction and locking behavior explicit.
- Use stable, actionable error contracts rather than returning raw exception details.
- Add dependencies only when the benefit and operational cost are documented.
- Generated IDs remain server/database assigned; clients do not choose persistence identities.

## Documentation and decision evidence

Update documentation in the same change when behavior, topology, ownership, trust boundaries, configuration, deployment, failure handling, or tests change.

Architecture-affecting changes must record:

- Connected objective and explicit non-goals.
- Components and contracts changed.
- Retained Legacy dependencies.
- Data and rule ownership before and after.
- Network, identity, and security boundaries.
- Timeout, retry, replay, rollback, and recovery behavior.
- Tests and measurable evidence.
- New trade-offs or operational burden.

Update the decision log in `docs/north-star-architecture.md` only for material North Star refinements. Routine implementation choices belong in a focused ADR or the relevant architecture document.

## Definition of done

A Connected change is complete only when:

- It advances a stated Connected objective.
- Scope and non-goals are clear.
- Legacy behavior remains baselined or an approved difference is documented.
- Data authority is unchanged or explicitly approved.
- Contracts, security, failure handling, and telemetry are implemented together.
- Applicable automated tests pass in both local and Connected configurations.
- Rollback or disablement is documented and practical.
- Documentation reflects the implemented system.
- No secrets, customer data, generated output, or machine-specific files are included.
- The working tree contains only intentional changes.

## Agent handoff

When reporting work, state:

- Branch and commit/worktree state.
- Connected capability advanced.
- Files changed.
- Data and rule ownership impact.
- Tests run and their results.
- Tests not run and why.
- Security, migration, rollback, and follow-up considerations.
