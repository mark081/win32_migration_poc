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

### Mandatory Connected feature gate

All runtime behavior introduced for migration or as new Connected functionality must be protected by the top-level feature flag:

```text
connected.enabled
```

The repository state at Connected branch commit `b9a5125` is the behavioral baseline. After this point, deploying new code must not activate new runtime behavior by itself.

When `connected.enabled` is `false`, missing, expired, malformed, or cannot be evaluated, the application must preserve Legacy behavior. The safe fallback is always `false`.

This parent flag is a global circuit breaker for Connected behavior:

```text
effectiveConnectedFeature = connected.enabled AND childFeatureEvaluation
```

A child flag such as `connected.checkout.service-rules` or `connected.events.publish` must never enable its behavior while `connected.enabled` is false. Environment, practice, deployment ring, client version, or other cohort targeting may narrow the parent flag, but may not bypass it.

The flag applies to runtime behavior including:

- Migrated business-rule execution.
- New Connected API behavior or workflow routing.
- Remote endpoint selection.
- Reliable event and outbox publication.
- Additive cloud integrations, projections, or analytics feeds.
- Connected UI surfaces and capabilities.
- Connected-specific synchronization, retries, or telemetry behavior.

The following do not require runtime gating because they do not activate product behavior:

- Tests, documentation, formatting, and build tooling.
- Refactoring that is proven behaviorally equivalent.
- Bug and security fixes that intentionally preserve or strengthen the Legacy contract.
- The minimum flag-evaluation and capability plumbing needed to determine that `connected.enabled` is false.

Any other exception requires explicit user approval and documentation of why it cannot be safely gated.

The service is authoritative for evaluating `connected.enabled`. Clients may receive a capability decision from the service for presentation and routing, but must not independently turn Connected behavior on. A client-side decision is never authorization.

Feature evaluation must not make an external network call on every business operation. Use a provider SDK cache or versioned local snapshot, define refresh and expiry behavior, and emit evaluation reason and configuration version without logging sensitive targeting data.

`connected.enabled` must never disable authentication, authorization, tenant/practice isolation, input validation, audit, or other security controls.

### Migration flag pattern

Business-logic migrations should use a child mode beneath the parent gate:

```text
connected.enabled = false
    -> Legacy implementation

connected.enabled = true
connected.<workflow>.rule-mode = legacy | compare | service
```

- `legacy` keeps the existing client/native behavior.
- `compare` executes the new service rule in shadow mode, records normalized differences, and performs only one authoritative business write.
- `service` makes the service result authoritative while retaining a documented rollback path.

The public API contract should remain stable across modes. Flag values and migration internals should not leak into domain payloads unless the contract explicitly requires a capability response.

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
11. **Gate all Connected runtime behavior.** New or migratory runtime behavior is inactive unless `connected.enabled` evaluates true; child flags can only narrow the rollout.
12. **Keep evaluation authoritative and observable.** The service evaluates the parent flag, uses a safe false fallback, and records flag key, value, reason, configuration version, cohort key, and correlation ID without sensitive attributes.
13. **Remove temporary migration flags.** After a migration is proven, expanded, and the Legacy implementation is intentionally retired, remove the child flag and dead path. Retain the parent circuit breaker throughout the Connected stage.

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
- `connected.enabled=false` produces the Legacy baseline for every affected workflow.
- `connected.enabled=true` activates only the intended Connected behavior for the targeted cohort.
- A true child flag cannot activate behavior while `connected.enabled` is false.
- Missing, invalid, expired, unreachable, and timeout provider scenarios fall back to `false` without corrupting state.
- Flag changes, evaluation reasons, configuration versions, and rollout cohorts are observable and auditable without exposing sensitive targeting data.
- Compare mode records differences but performs exactly one authoritative business write.

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

## Code documentation standards

Code documentation must help a future engineer understand intent, constraints, ownership, and safe change boundaries. Do not add comments that merely translate syntax into English.

### General rules

- Every new source file must begin with a plain-text purpose block explaining why the file exists,
  what work belongs there, what work belongs elsewhere, and whether the code currently runs in the
  product, supports a later migration task, or exists only for tests.
- Document every class, interface, enum, delegate, constructor, and method, regardless of access
  level. This includes private helpers, interface members, process entry points, test classes, test
  methods, and test doubles. When existing code is materially changed, add or update this
  documentation for every type and method touched by the change.
- Use plain, ASCII-readable source comments such as `//`, `#`, or `--`. Do not use XML documentation,
  HTML-like tags, or other markup unless a required documentation generator cannot operate without
  it and the exception is explicitly approved.
- Write for an engineer who understands the programming language but is new to this application and
  its migration. Prefer familiar words, short sentences, and concrete descriptions of who calls the
  code, what it decides or changes, and what happens when it fails.
- Do not use unexplained acronyms, architecture shorthand, or specialist terms such as ABI,
  boundary, precheck, invariant, authoritative, cohort, or seam when ordinary language is clearer.
  When a technical term is necessary, explain it in the same comment the first time it appears.
- Prefer concrete wording such as "the desktop displays this reason" and "the database makes the
  final checkout decision" over abstract wording such as "the client consumes the diagnostic
  contract" or "the persistence layer enforces the transactional invariant."
- A documentation review must ask whether a new engineer can explain the code's purpose and safe
  change limits after reading the comment without first consulting an architecture glossary.
- Type documentation must explain the type's job, who uses it, and which component makes the final
  decision or writes the final data. Method documentation must explain its purpose, important
  inputs, return value or changes it makes, and what happens when it cannot complete normally.
  Constructor documentation must explain the state or required collaborators it sets up and any
  values it rejects, changes, or replaces with defaults.
- Document why code exists, the business rule or safety guarantee it protects, and any non-obvious
  compromise or failure behavior.
- Keep documentation next to the code or decision it explains. Put broader design reasoning in the
  appropriate architecture document or architecture decision record (ADR) and link to it rather
  than duplicating it.
- Update comments and documentation in the same change as the behavior. Correct or remove stale comments immediately.
- Use concise examples with synthetic data. Never include secrets, real customer data, PHI, PII, production hostnames, or production credentials.
- Reference scenario IDs from `docs/testing-evolution.md` when code implements or migrates a baselined business rule.
- Describe units, ranges, time zones, null behavior, ordering, retry safety, and error semantics wherever they are not obvious from the type system.
- Document compatibility constraints when a method, payload, event, or configuration value is consumed by older clients or deployments.

### C# and API contracts

- Use plain `//` documentation for public interfaces, extension points, externally consumed methods,
  and request/response models. Keep it immediately above the declaration so it remains readable in
  a basic text viewer.
- Document preconditions, postconditions, side effects, exceptions, idempotency behavior, authorization assumptions, and whether the operation participates in a transaction.
- For HTTP endpoints, document authentication, required headers, status codes, stable error codes, idempotency expectations, and observable side effects in the API documentation or contract tests.
- Explain which component performs each part of the work and which component makes the final
  business decision. Do not imply that a desktop or service check is final when PostgreSQL can
  still accept or reject the operation.

### C++ and Win32 code

- Document every type and function added or materially changed, including internal helpers. Add
  compatibility, resource ownership, lifetime, text encoding, and failure details when data or
  control passes between the Win32 UI, NativeRules DLL, HTTP API, feature decision, or another
  process.
- Preserve comments explaining intentional Automation IDs, accessibility labels, x86 constraints, Windows API lifetime rules, and ownership of handles or allocated memory.
- Explain why a client-side rule remains during migration and identify the condition under which it can be removed.

### PostgreSQL and persistence

- Document transaction boundaries, locking order, isolation assumptions, idempotency records, and concurrency behavior for non-trivial persistence code.
- Explain stored procedures and constraints in terms of the business invariant they enforce.
- Document custom SQLSTATE values and keep their mapping to stable API errors discoverable.
- State the authoritative writer and data owner when adding an outbox, projection, synchronization record, or migration table.

### PowerShell and operational scripts

- Add PowerShell comment-based help to reusable or operator-facing scripts, including synopsis, parameters, prerequisites, examples, outputs, exit/failure behavior, and rollback or cleanup where applicable.
- Examples must be safe to copy and must use development placeholders rather than real secrets.
- Explain required privilege, interactive desktop, service-account, network, or environment assumptions.

### Feature flags and migration seams

At the definition or primary evaluation point for every production flag, document:

- Flag key and purpose.
- Owner.
- Safe default and provider-failure behavior.
- Legacy behavior when disabled.
- Evaluation context and targeting scope.
- Telemetry and audit expectations.
- Rollback behavior.
- Expected removal condition.

Temporary migration code must identify the Legacy, compare, and service behavior it supports. `TODO` comments are acceptable only when they include an actionable removal condition or tracked decision; do not leave ownerless or indefinite migration markers.

Example:

```csharp
// Evaluates checkout eligibility in the service during the Connected migration.
//
// Parent gate: connected.enabled
// Safe fallback: Legacy
// Compare: records normalized differences but performs no second business write
// Removal: after supported clients use service policy and the Connected equivalence gate passes
// Scenarios: LOAN-001 and LOAN-003
```

### Review standard

Code review must reject a new or materially changed source file when its purpose block, any type
documentation, or any constructor or method documentation is missing. It must also reject
documentation that is misleading, merely translates syntax into English, describes behavior no
longer present, exposes sensitive information, or fails to explain a non-obvious migration,
security, concurrency, or ownership decision.

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
- All new or migratory runtime behavior is subordinate to `connected.enabled`.
- Disabled, enabled, child-enabled/parent-disabled, and provider-failure flag paths are tested.
- The flag owner, safe default, targeting context, rollback behavior, expected removal condition, and telemetry are documented.
- Public contracts and non-obvious migration, security, concurrency, feature-flag, compatibility, and business-rule decisions are documented at the appropriate code or architecture boundary.
- Documentation was reviewed for accuracy, and stale or redundant comments were corrected or removed.

## Agent handoff

When reporting work, state:

- Branch and commit/worktree state.
- Connected capability advanced.
- Files changed.
- Data and rule ownership impact.
- Tests run and their results.
- Tests not run and why.
- Security, migration, rollback, and follow-up considerations.
