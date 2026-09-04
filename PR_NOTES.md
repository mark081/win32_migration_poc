# Connected checkout rule migration

These notes summarize the current `connected` branch for pull-request review. They describe the
implemented scope, retained ownership, verification completed so far, and the environment evidence
that remains before promotion.

## Summary

- Adds the service-owned checkout decision contract beneath the mandatory `connected.enabled`
  circuit breaker.
- Supports Legacy, compare, and service routing while keeping PostgreSQL as the only authoritative
  workflow writer and final business-rule decision maker.
- Keeps NativeRules in the Legacy and compare paths; service mode does not make a native checkout
  eligibility decision.
- Adds safe capability caching, telemetry, external Connected endpoint and credential settings,
  bounded timeouts, stable failure presentation, and same-key ambiguous-write replay.
- Adds cross-boundary tests and Connected operations guidance. No production runtime code changed
  in the final cross-boundary verification wave.

## Verification completed

- Release build passed with zero errors and the six existing warnings.
- Native/transport, 34 service feature, PostgreSQL, and API integration checks passed.
- FlaUI passed 9 tests with 0 failures and 0 skips in an active, unlocked RDP session.
- Strict brownfield OKF v0.2 validation and `git diff --check` passed.
- Graphify was refreshed to 871 nodes and 1,703 edges.

## Ownership and rollout

- Existing PostgreSQL data, routines, transaction locking, audit records, and idempotency remain
  authoritative.
- Deployment alone does not activate Connected behavior. Missing, invalid, stale, or unavailable
  parent configuration safely selects Legacy behavior.
- Compare/service activation and retirement of NativeRules or the child flag require a separate
  human decision after the Connected environment gate passes.

## Remaining Connected environment evidence

Task 7.1 requires a separately provisioned synthetic Connected environment. The current host has
no Connected HTTPS endpoint or credential-file setting, so local/Connected parity, certificate and
hostname validation, representative latency, network/write interruption, and rollback convergence
have not yet been run.

Configure the environment through:

- `TOOL_LENDING_CONNECTED_BASE_URL`
- `TOOL_LENDING_CONNECTED_CREDENTIAL_FILE`

After that environment is available, run the same contract and scenario matrix against the local
and Connected endpoints and retain the normalized state, audit, latency, failure-correlation, and
rollback evidence required by task 7.1.
