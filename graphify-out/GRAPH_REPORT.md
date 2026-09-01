# Graph Report - win32_migration_poc  (2026-08-31)

## Corpus Check
- 73 files · ~40,332 words
- Verdict: corpus is large enough that graph structure adds value.

## Summary
- 621 nodes · 1083 edges · 58 communities (47 shown, 11 thin omitted)
- Extraction: 95% EXTRACTED · 5% INFERRED · 0% AMBIGUOUS · INFERRED: 55 edges (avg confidence: 0.83)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `a92466a4`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- Repository
- DesktopClient/main.cpp
- ApiControllerBase
- DesktopClientTests
- .SendAsync
- 001_schema.sql
- DesktopClient.UiTests.csproj
- knowledge/index.md
- checkEligibility
- DllMain
- ApiTests.ps1
- Connected Stage Mission
- Rule Distribution
- Summary
- API Surface
- Build and Run
- Build and Test Flow
- Data Model
- Failure and Recovery Behavior
- Tool Lending Modernization Reference Application
- demo.md
- Design Document: Service-Owned Client Business Rules
- Requirements: Service-Owned Client Business Rules
- Tasks
- Program
- CachedConnectedFeatureEvaluator
- JsonFileFeatureSnapshotSource
- Application Service
- Summary
- DESIGN.md
- FlagEvaluationRecord
- Summary
- build-test-deploy.md
- RuleComparisonRecord
- ConnectedTelemetryMetrics
- IConnectedTelemetrySink
- ConnectedTelemetry.cs
- JsonDiagnosticConnectedTelemetrySink
- ThrowingTelemetrySink
- constraints.md

## God Nodes (most connected - your core abstractions)
1. `Program` - 35 edges
2. `RuleComparisonRecord` - 25 edges
3. `ConnectedFeatureSnapshot` - 24 edges
4. `CachedConnectedFeatureEvaluator` - 19 edges
5. `ConnectedCapability` - 18 edges
6. `FeatureSnapshotLoadResult` - 18 edges
7. `FlagEvaluationRecord` - 18 edges
8. `DesktopClientTests` - 17 edges
9. `InMemoryConnectedTelemetrySink` - 16 edges
10. `Repository` - 15 edges

## Surprising Connections (you probably didn't know these)
- `Windows Server 2019 Setup` --conceptually_related_to--> `Application Service`  [INFERRED]
  docs/windows-server-2019-setup.md → README.md
- `Windows Server 2019 Setup` --conceptually_related_to--> `Database`  [INFERRED]
  docs/windows-server-2019-setup.md → README.md
- `Eaglesoft POC North Star Architecture` --conceptually_related_to--> `Application Service`  [INFERRED]
  docs/north-star-architecture.md → README.md
- `Eaglesoft POC North Star Architecture` --conceptually_related_to--> `Database`  [INFERRED]
  docs/north-star-architecture.md → README.md
- `FixedClock` --implements--> `IConnectedFeatureClock`  [EXTRACTED]
  tests/AppServer.FeatureTests/Program.cs → src/AppServer/ConnectedFeatures.cs

## Import Cycles
- None detected.

## Communities (58 total, 11 thin omitted)

### Community 0 - "Repository"
Cohesion: 0.07
Nodes (37): ToolLending.AppServer.FeatureTests, ToolLending.AppServer, List, NpgsqlConnection, NpgsqlTransaction, DateTime, Guid, IList (+29 more)

### Community 1 - "DesktopClient/main.cpp"
Cohesion: 0.17
Nodes (33): HINSTANCE, HWND, LPARAM, LPWSTR, LRESULT, AddTool(), AddUser(), Checkout() (+25 more)

### Community 2 - "ApiControllerBase"
Cohesion: 0.17
Nodes (16): ApiController, HttpGet, HttpPost, IHttpActionResult, Route, Func, Guid, ApiControllerBase (+8 more)

### Community 3 - "DesktopClientTests"
Cohesion: 0.17
Nodes (10): Application, AutomationElement, ToolLending.DesktopClient.UiTests, SetUp, TearDown, Test, DesktopClientTests, LegacyCredentialStartupTests (+2 more)

### Community 4 - ".SendAsync"
Cohesion: 0.11
Nodes (12): CancellationToken, DelegatingHandler, HttpRequestMessage, HttpResponseMessage, IAppBuilder, IDisposable, ServiceBase, ApiKeyHandler (+4 more)

### Community 5 - "001_schema.sql"
Cohesion: 0.26
Nodes (9): audit_log, idempotency_records, loans, members, reservations, tools, checkout_tool(), reserve_tool() (+1 more)

### Community 6 - "DesktopClient.UiTests.csproj"
Cohesion: 0.15
Nodes (10): net8.0-windows, FlaUI.Core (5.0.0), FlaUI.UIA3 (5.0.0), Microsoft.NET.Test.Sdk (17.14.1), NUnit (4.3.2), NUnit3TestAdapter (5.0.0), Microsoft.NET.Sdk, DesktopClient (+2 more)

### Community 7 - "knowledge/index.md"
Cohesion: 0.15
Nodes (8): Durable constraint, Summary, Transaction and rule boundaries, Terms, Hotspots, Risks, Evidence gaps, Open questions

### Community 8 - "checkEligibility"
Cohesion: 0.29
Nodes (12): CheckoutEligibilityReasonV1(), CheckoutLimit(), CheckoutEligibilityReasonCode, wchar_t, IsEligibleForCheckout(), MaximumLoanDays(), tier_is(), check() (+4 more)

### Community 9 - "DllMain"
Cohesion: 0.33
Nodes (5): BOOL, HMODULE, LPVOID, DWORD, DllMain()

### Community 19 - "Summary"
Cohesion: 0.40
Nodes (4): Reconciliation disposition, Summary, Wave 0 update disposition, Wave 1 update disposition

### Community 38 - "Design Document: Service-Owned Client Business Rules"
Cohesion: 0.05
Nodes (38): API contract and integration tests, AppServer feature evaluation module, Architecture, Capabilities API, Capability bootstrap and endpoint selection, Capability model, Checkout decision API, Checkout rule evaluator (+30 more)

### Community 39 - "Requirements: Service-Owned Client Business Rules"
Cohesion: 0.06
Nodes (32): A. Service-owned rule contract, Assumptions, Assumptions, dependencies, and change control, B. Parent gate and migration modes, C. Contract and client behavior, Change control, D. Network and failure behavior, Data and provenance minimums (+24 more)

### Community 40 - "Tasks"
Cohesion: 0.14
Nodes (13): 1. Baseline and characterization, 2. Independent foundations, 3. Service rule and additive API contracts, 4. Client transport and capability routing, 5. Checkout mode integration, 6. Cross-boundary verification and operations, 7. Connected gate and reconciliation, Execution Contract (+5 more)

### Community 41 - "Program"
Cohesion: 0.09
Nodes (26): Exception, ConnectedFeatureSnapshot, CheckoutTargets, ConfigurationVersion, Enabled, ExpiresAt, IssuedAt, ParentTargets (+18 more)

### Community 42 - "CachedConnectedFeatureEvaluator"
Cohesion: 0.06
Nodes (42): IReadOnlyList, DateTimeOffset, Guid, IEnumerable, TimeSpan, CachedConnectedFeatureEvaluator, CheckoutRuleMode, Compare (+34 more)

### Community 43 - "JsonFileFeatureSnapshotSource"
Cohesion: 0.09
Nodes (23): CheckoutDocument, ConnectedDocument, SnapshotDocument, CheckoutDocument, RuleMode, Targets, ConnectedDocument, Checkout (+15 more)

### Community 44 - "Application Service"
Cohesion: 0.47
Nodes (11): Architecture and Rule Ownership, Legacy Shared Credential Model, Eaglesoft POC North Star Architecture, Testing Baseline and Modernization Gates, Windows 11 Pro Development Setup, Windows Server 2019 Setup, Application Service, Checkout Request Flow (+3 more)

### Community 45 - "Summary"
Cohesion: 0.40
Nodes (4): Boundaries and dependencies, Entry points, Graph evidence, Summary

### Community 46 - "DESIGN.md"
Cohesion: 0.17
Nodes (9): Business-rule migration mode, Evaluation boundary, Implemented-state evidence, Summary, Architecture, Evidence confidence, Ownership implication, Responsibilities (+1 more)

### Community 47 - "FlagEvaluationRecord"
Cohesion: 0.13
Nodes (15): DateTimeOffset, Guid, IList, FlagEvaluationRecord, CohortKeyHash, ConfigurationVersion, CorrelationId, EffectiveValue (+7 more)

### Community 48 - "Summary"
Cohesion: 0.50
Nodes (4): Contract behavior, Current network limitation, Migration compatibility constraint, Summary

### Community 49 - "build-test-deploy.md"
Cohesion: 0.50
Nodes (3): Change-sensitive scenarios, Summary, Verification layers

### Community 50 - "RuleComparisonRecord"
Cohesion: 0.13
Nodes (15): RuleComparisonRecord, CohortKeyHash, ConfigurationVersion, CorrelationId, Duration, InputIdentityHash, LegacyAllowed, LegacyContractVersion (+7 more)

### Community 52 - "ConnectedTelemetryMetrics"
Cohesion: 0.24
Nodes (3): Dictionary, TimeSpan, ConnectedTelemetryMetrics

### Community 53 - "IConnectedTelemetrySink"
Cohesion: 0.27
Nodes (4): Action, IConnectedTelemetrySink, SafeConnectedTelemetrySink, FailureCount

### Community 54 - "ConnectedTelemetry.cs"
Cohesion: 0.17
Nodes (11): IDictionary, ConnectedDurationSummary, Count, MaximumMilliseconds, MinimumMilliseconds, TotalMilliseconds, ConnectedTelemetryMetricsSnapshot, Counters (+3 more)

### Community 55 - "JsonDiagnosticConnectedTelemetrySink"
Cohesion: 0.33
Nodes (4): JObject, JsonDiagnosticConnectedTelemetrySink, Metrics, TextWriter

### Community 56 - "ThrowingTelemetrySink"
Cohesion: 0.39
Nodes (3): TimeSpan, ThrowingTelemetrySink, Calls

## Knowledge Gaps
- **220 isolated node(s):** `NativeRules`, `DesktopClient`, `NativeRulesTests`, `idempotency_records`, `audit_log` (+215 more)
  These have ≤1 connection - possible missing edges or undocumented components.
- **11 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `ToolLending.AppServer` connect `Repository` to `CachedConnectedFeatureEvaluator`, `ApiControllerBase`, `.SendAsync`, `ConnectedTelemetry.cs`?**
  _High betweenness centrality (0.190) - this node is a cross-community bridge._
- **Why does `ConnectedTelemetryRedaction` connect `ConnectedTelemetry.cs` to `ConnectedTelemetryMetrics`?**
  _High betweenness centrality (0.070) - this node is a cross-community bridge._
- **Why does `Program` connect `Program` to `Repository`, `FlagEvaluationRecord`, `ConnectedTelemetryMetrics`, `IConnectedTelemetrySink`, `JsonDiagnosticConnectedTelemetrySink`, `ThrowingTelemetrySink`?**
  _High betweenness centrality (0.068) - this node is a cross-community bridge._
- **What connects `NativeRules`, `DesktopClient`, `NativeRulesTests` to the rest of the system?**
  _220 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `Repository` be split into smaller, more focused modules?**
  _Cohesion score 0.07402031930333818 - nodes in this community are weakly interconnected._
- **Should `.SendAsync` be split into smaller, more focused modules?**
  _Cohesion score 0.10952380952380952 - nodes in this community are weakly interconnected._
- **Should `Design Document: Service-Owned Client Business Rules` be split into smaller, more focused modules?**
  _Cohesion score 0.05263157894736842 - nodes in this community are weakly interconnected._