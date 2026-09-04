# Connected checkout operations

This guide covers configuration, observation, rollback, and verification for the Connected-stage
checkout rule migration. It uses synthetic POC values only. Deployment does not authorize rollout:
keep `connected.enabled` false until a separate human promotion decision is recorded.

## Ownership and retained components

The application service owns feature evaluation and the versioned checkout-decision contract. The
Win32 client retains required-field and input-shape validation, confirmation, accessible feedback,
and the Legacy/compare NativeRules path. PostgreSQL remains the only owner of workflow data, locked
transactional rules, idempotency records, and business audit records. No database schema change is
required, and no cloud projection becomes workflow authority.

## Service configuration

Configure these `AppServer.exe.config` application settings outside the repository:

| Setting | Safe/default behavior |
| --- | --- |
| `ConnectedFeatureSnapshotPath` | Empty; parent disabled and Legacy selected |
| `ConnectedFeatureRefreshSeconds` | `30`; accepted range 5-300 seconds |
| `ConnectedFeatureMaxAgeSeconds` | `300`; accepted range 30-3600 seconds |
| `ConnectedCapabilityLifetimeSeconds` | `30`; accepted range 5 seconds through the refresh interval |
| `ConnectedEnvironment` | `local` |
| `ConnectedPracticeKey` | Empty; server-owned targeting input, never returned or logged raw |
| `ConnectedDeploymentRing` | `default` |

Invalid numeric values use the documented safe defaults. The service reads a versioned local JSON
snapshot into a process-local cache; it does not contact an external provider on each operation.
Publish a snapshot by writing a complete temporary file on the same volume and atomically replacing
the configured path. Never update the live file in place.

Start with a disabled synthetic snapshot:

```json
{
  "schemaVersion": 1,
  "configurationVersion": "connected-poc-disabled-1",
  "issuedAt": "2026-09-04T00:00:00Z",
  "expiresAt": "2026-09-05T00:00:00Z",
  "connected": {
    "enabled": false,
    "targets": {
      "environments": ["connected-test"],
      "practiceKeys": [],
      "rings": [],
      "minimumClientVersion": "1.0.0"
    },
    "checkout": {
      "ruleMode": "legacy",
      "targets": {
        "environments": [],
        "practiceKeys": [],
        "rings": []
      }
    }
  }
}
```

Missing, malformed, future-issued, expired, inaccessible, or too-old snapshots disable the parent.
A missing, invalid, expired, or target-missed child selects Legacy. Environment, practice, ring, and
minimum-client-version targeting can only narrow a true parent; they cannot enable a false parent.

## Desktop transport configuration

The desktop uses `TOOL_LENDING_LEGACY_BASE_URL` for Legacy and the following external settings for
Connected routing:

| Variable | Requirement |
| --- | --- |
| `TOOL_LENDING_CONNECTED_BASE_URL` | Absolute HTTPS URL; HTTP and embedded credentials are rejected |
| `TOOL_LENDING_CONNECTED_CREDENTIAL_FILE` | Readable, non-empty file required with a Connected URL |
| `TOOL_LENDING_RESOLVE_TIMEOUT_MS` | Default 5000; range 100-60000 |
| `TOOL_LENDING_CONNECT_TIMEOUT_MS` | Default 5000; range 100-60000 |
| `TOOL_LENDING_SEND_TIMEOUT_MS` | Default 10000; range 100-120000 |
| `TOOL_LENDING_RECEIVE_TIMEOUT_MS` | Default 15000; range 100-120000 |

Windows certificate-chain and hostname validation remain enabled. Keep credentials outside source
control and grant only the desktop identity read access. PostgreSQL stays private and is never
configured in the desktop.

## Mode behavior and compatibility

| Client/service combination | Result |
| --- | --- |
| Old client with current service | Existing `/api/v1` workflows continue; no capability call required |
| Version 1 client with parent false or unsafe snapshot | Legacy endpoint and NativeRules path |
| Version 1 client with current compare capability | Connected decision is observed; Legacy result controls the UI |
| Version 1 client with current service capability | Service decision controls eligibility; PostgreSQL still controls commit |
| Unsupported capability or decision schema | Client rejects it and returns to Legacy or stops the attempt safely |
| Stale configuration version | `409 CAPABILITY_STALE`; client invalidates cached routing evidence |

The checkout command contract, `TLxxx` errors, generated identifiers, and idempotency behavior do
not change by mode. Compare records one normalized observation and performs exactly one business
write. Service decisions are read-only and are never displayed as checkout success.

## Telemetry and audit

The diagnostic stream emits `connected.flag_evaluation` and rule-comparison JSON records. Review
the flag key, safe value, reason, configuration version, timestamp, correlation ID, hashed cohort
key, mode, normalized Legacy/service reasons, match value, outcome, and duration. In-process metrics
are bounded by name/mode/outcome. The sink excludes credentials, authorization headers, member
names, raw targeting values, and request bodies; telemetry failure cannot fail or retry a business
command. PostgreSQL `audit_log` remains the separate business record.

Before promotion, agree on an observation window and dataset outside this implementation plan.
Record match/mismatch counts by configuration version and investigate every mismatch; task
completion alone is not rollout approval.

## Rollback and recovery

Prefer changing `connected.checkout.rule-mode` to `legacy`; disable `connected.enabled` for the
global circuit breaker. Publish the new snapshot atomically and verify the capability reason and
configuration version. With default settings, service refresh (30 seconds) plus client capability
lifetime (30 seconds) gives a maximum convergence target of 60 seconds for newly evaluated work.
No service restart, client deployment, or data migration is required.

An in-flight checkout keeps one idempotency key. If a write response is ambiguous, replay only that
same key; never issue a new unrelated write. A later PostgreSQL rejection is the final result. After
rollback, compare database state and business audit records with the expected Legacy outcome.

Remove `connected.checkout.rule-mode` and its dead path only after supported clients use the service
policy, the Connected equivalence gate passes, rollback evidence is retained, and maintainers
explicitly approve NativeRules retirement. Retain the `connected.enabled` circuit breaker throughout
the Connected stage.

## Verification

Local Release verification requires PostgreSQL and the API console:

```powershell
Set-Location C:\src\win32_migration_poc
Set-ExecutionPolicy -Scope Process Bypass -Force
.\scripts\Build.ps1 -Configuration Release
.\scripts\Reset-Demo.ps1 -Force
.\artifacts\x86\Release\AppServer.exe --console
```

In another terminal, run:

```powershell
.\scripts\Run-Tests.ps1 -Configuration Release
.\scripts\Reset-Demo.ps1 -Force
.\scripts\Run-UiTests.ps1 -Configuration Release
```

FlaUI requires an interactive, unlocked desktop. Reset before FlaUI so its seeded member remains
eligible. For a Connected environment, install a trusted certificate, configure the external HTTPS
URL and credential file, keep PostgreSQL private, then run the same contract/scenario matrix against
that endpoint. TLS, local/Connected parity, latency, interruption, and rollback evidence belong to
task 7.1 and must not be inferred from local unit success.
