# Architecture and rule ownership

```text
Win32 x86 client
  |-- Lending, Add user, and Add tool tabs
  |-- UI validation and confirmation
  |-- NativeRules.dll (eligibility and tier limits)
  |-- practice-shared Legacy credential read from local/SMB file
  |-- externally configured Legacy endpoint and optional HTTPS Connected endpoint
  |
  +---- HTTP/JSON + X-Api-Key ----> .NET Framework 4.8 Windows service
                                      |-- authorization
                                      |-- idempotency
                                      |-- workflow orchestration
                                      |-- error mapping and audit context
                                      |
                                      +---- Npgsql ----> PostgreSQL 15
                                                          |-- row locking
                                                          |-- availability
                                                          |-- conflicts
                                                          |-- overdue blocks
                                                          |-- fees and writes
```

## Why the rules are distributed

This split is intentional. It demonstrates the maintenance and scaling constraints identified in the source assessment: users can encounter rules in a client, native library, service, and database. Client and DLL checks improve feedback but are never authoritative. Stored procedures repeat critical checks under row locks so a custom caller cannot bypass them.

| Rule | UI | Native DLL | Service | Database |
|---|---:|---:|---:|---:|
| Required fields/date shape | Primary | | DTO validation | |
| Member and tool identifiers | Displays generated value | | Omits ID from create contract | Identity columns |
| Tier checkout limit | Display | Primary precheck | Loads member state | Authoritative |
| Maximum loan duration | Display | Primary precheck | Pass-through | Authoritative |
| API authorization | | | Authoritative | |
| Idempotent writes | | | Coordinates key | Authoritative unique record |
| Tool availability/conflicts | Display only | | Error translation | Authoritative with locks |
| Overdue-member block | Warning | Eligibility precheck | Loads status | Authoritative |
| Late fee | Display result | | Orchestration | Authoritative calculation |
| Audit trail | | | Supplies actor/request | Authoritative insert |

## Deployment constraints

- One site equals one service and one database.
- Windows 11 Pro x64 is supported as a development and demonstration host; Windows Server 2019 remains the deployment reference.
- All components are installed on or communicate over a local wired LAN with the server.
- Legacy clients may read one practice-shared API credential from a common SMB file. This models
  the assessed exposure; it does not provide user identity or per-workstation authorization.
- No multi-tenant partitioning, WAN hosting, failover, or centralized management is implemented.
- The client and native DLL are x86. The service also targets x86 to demonstrate 32-bit dependency pressure.
- PostgreSQL is the source of truth. Direct table access by clients is unsupported.

## Failure behavior

Database exceptions use stable `TLxxx` SQLSTATE codes. The API maps expected business failures to HTTP 409, validation failures to 400, authentication failures to 401, and unexpected failures to 500 with a correlation ID. A failed transaction writes no partial business state. Service restarts are safe because idempotency records live in PostgreSQL.

The additive checkout-decision route is read-only. It evaluates current server-owned feature state,
loads current member facts, and returns an advisory allow or deny result. Compare mode records the
normalized NativeRules and service results, while service mode returns only the service result.
Neither mode writes a loan, idempotency record, or business audit entry. A stale capability returns
`409 CAPABILITY_STALE`; a database read failure returns `503 DECISION_UNAVAILABLE`.

The WinHTTP transport accepts an external Legacy URL and an optional Connected URL. Connected URLs
must use HTTPS and supply their own credential file; they cannot inherit the compiled local-demo
credential. Windows performs its normal certificate-chain and hostname validation. Resolve,
connect, send, and receive timeouts are explicitly bounded. Transport failures are classified as
configuration, timeout, unavailable, authentication, authorization, validation, conflict, or
unexpected. The client caches only a current schema 1 service capability and selects Connected for
`compare` or `service`. Missing, malformed, unsupported, future-dated, expired, disabled, and failed
capabilities select Legacy. The process-local cache is refreshed on the first product request after
expiry, so a service rollback needs no client restart or deployment.
