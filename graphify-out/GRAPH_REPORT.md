# Graph Report - win32_migration_poc  (2026-09-02)

## Corpus Check
- 75 files · ~47,975 words
- Verdict: corpus is large enough that graph structure adds value.

## Summary
- 691 nodes · 1233 edges · 67 communities (57 shown, 10 thin omitted)
- Extraction: 94% EXTRACTED · 6% INFERRED · 0% AMBIGUOUS · INFERRED: 70 edges (avg confidence: 0.83)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `5fc24fa1`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- Repository
- DesktopClient/main.cpp
- ApiControllerBase
- DesktopClientTests
- ToolLending.AppServer
- 001_schema.sql
- DesktopClient.UiTests.csproj
- knowledge/index.md
- checkEligibility
- DllMain
- ApiTests.ps1
- Connected Stage Mission
- Rule Distribution
- MemberEligibilityContext
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
- ConnectedCapability
- JsonFileFeatureSnapshotSource
- Application Service
- Summary
- REQUIREMENTS.md
- FlagEvaluationRecord
- Summary
- build-test-deploy.md
- RuleComparisonRecord
- ConnectedTelemetryMetrics
- IConnectedTelemetrySink
- ConnectedTelemetry.cs
- JsonDiagnosticConnectedTelemetrySink
- ThrowingTelemetrySink
- FeatureEvaluationContext
- FeatureSnapshotLoadResult
- CachedConnectedFeatureEvaluator
- ConnectedFeatureTargets
- CapabilityResponse
- ConnectedFeatures.cs
- Summary
- ConnectedFeatureSnapshot
- CheckoutRuleMode
- Summary

## God Nodes (most connected - your core abstractions)
1. `Program` - 50 edges
2. `RuleComparisonRecord` - 25 edges
3. `ConnectedFeatureSnapshot` - 24 edges
4. `ConnectedCapability` - 21 edges
5. `CachedConnectedFeatureEvaluator` - 19 edges
6. `FlagEvaluationRecord` - 19 edges
7. `FeatureSnapshotLoadResult` - 18 edges
8. `InMemoryConnectedTelemetrySink` - 18 edges
9. `Repository` - 17 edges
10. `DesktopClientTests` - 17 edges

## Surprising Connections (you probably didn't know these)
- `Windows Server 2019 Setup` --conceptually_related_to--> `Application Service`  [INFERRED]
  docs/windows-server-2019-setup.md → README.md
- `Windows Server 2019 Setup` --conceptually_related_to--> `Database`  [INFERRED]
  docs/windows-server-2019-setup.md → README.md
- `Eaglesoft POC North Star Architecture` --conceptually_related_to--> `Application Service`  [INFERRED]
  docs/north-star-architecture.md → README.md
- `Eaglesoft POC North Star Architecture` --conceptually_related_to--> `Database`  [INFERRED]
  docs/north-star-architecture.md → README.md
- `FixedBusinessDateClock` --implements--> `IBusinessDateClock`  [EXTRACTED]
  tests/AppServer.FeatureTests/Program.cs → src/AppServer/CheckoutRules.cs

## Import Cycles
- None detected.

## Communities (67 total, 10 thin omitted)

### Community 0 - "Repository"
Cohesion: 0.08
Nodes (35): List, NpgsqlConnection, NpgsqlTransaction, DateTime, Guid, IList, AuditDto, CheckoutRequest (+27 more)

### Community 1 - "DesktopClient/main.cpp"
Cohesion: 0.17
Nodes (33): HINSTANCE, HWND, LPARAM, LPWSTR, LRESULT, AddTool(), AddUser(), Checkout() (+25 more)

### Community 2 - "ApiControllerBase"
Cohesion: 0.18
Nodes (15): HttpPost, Func, Guid, HttpGet, IHttpActionResult, Route, ApiControllerBase, Actor (+7 more)

### Community 3 - "DesktopClientTests"
Cohesion: 0.17
Nodes (10): Application, AutomationElement, ToolLending.DesktopClient.UiTests, SetUp, TearDown, Test, DesktopClientTests, LegacyCredentialStartupTests (+2 more)

### Community 4 - "ToolLending.AppServer"
Cohesion: 0.10
Nodes (14): CancellationToken, ToolLending.AppServer.FeatureTests, ToolLending.AppServer, DelegatingHandler, HttpRequestMessage, HttpResponseMessage, IAppBuilder, IDisposable (+6 more)

### Community 5 - "001_schema.sql"
Cohesion: 0.26
Nodes (9): audit_log, idempotency_records, loans, members, reservations, tools, checkout_tool(), reserve_tool() (+1 more)

### Community 6 - "DesktopClient.UiTests.csproj"
Cohesion: 0.15
Nodes (10): net8.0-windows, FlaUI.Core (5.0.0), FlaUI.UIA3 (5.0.0), Microsoft.NET.Test.Sdk (17.14.1), NUnit (4.3.2), NUnit3TestAdapter (5.0.0), Microsoft.NET.Sdk, DesktopClient (+2 more)

### Community 7 - "knowledge/index.md"
Cohesion: 0.17
Nodes (7): North Star Architecture, Summary, Terms, Hotspots, Risks, Evidence gaps, Open questions

### Community 8 - "checkEligibility"
Cohesion: 0.29
Nodes (12): CheckoutEligibilityReasonV1(), CheckoutLimit(), CheckoutEligibilityReasonCode, wchar_t, IsEligibleForCheckout(), MaximumLoanDays(), tier_is(), check() (+4 more)

### Community 9 - "DllMain"
Cohesion: 0.33
Nodes (5): BOOL, HMODULE, LPVOID, DWORD, DllMain()

### Community 19 - "MemberEligibilityContext"
Cohesion: 0.12
Nodes (21): DateTime, CheckoutDecision, Allowed, CheckoutLimit, MaximumLoanDays, Reason, CheckoutDecisionReasons, CheckoutRuleEvaluator (+13 more)

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
Nodes (21): Exception, CapabilityService, IConnectedFeatureSnapshotSource, Action, DateTime, DateTimeOffset, Guid, IEnumerable (+13 more)

### Community 42 - "ConnectedCapability"
Cohesion: 0.22
Nodes (9): ConnectedCapability, CheckoutRuleMode, ConfigurationVersion, ConnectedEnabled, CorrelationId, EvaluatedAt, ExpiresAt, Reason (+1 more)

### Community 43 - "JsonFileFeatureSnapshotSource"
Cohesion: 0.09
Nodes (22): CheckoutDocument, ConnectedDocument, CheckoutDocument, RuleMode, Targets, ConnectedDocument, Checkout, Enabled (+14 more)

### Community 44 - "Application Service"
Cohesion: 0.47
Nodes (11): Architecture and Rule Ownership, Legacy Shared Credential Model, Eaglesoft POC North Star Architecture, Testing Baseline and Modernization Gates, Windows 11 Pro Development Setup, Windows Server 2019 Setup, Application Service, Checkout Request Flow (+3 more)

### Community 45 - "Summary"
Cohesion: 0.40
Nodes (4): Boundaries and dependencies, Entry points, Graph evidence, Summary

### Community 46 - "REQUIREMENTS.md"
Cohesion: 0.18
Nodes (8): Architecture, Evidence confidence, Ownership implication, Responsibilities, Summary, Durable constraint, Summary, Transaction and rule boundaries

### Community 47 - "FlagEvaluationRecord"
Cohesion: 0.13
Nodes (15): DateTimeOffset, Guid, IList, FlagEvaluationRecord, CohortKeyHash, ConfigurationVersion, CorrelationId, EffectiveValue (+7 more)

### Community 48 - "Summary"
Cohesion: 0.40
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
Cohesion: 0.29
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

### Community 57 - "FeatureEvaluationContext"
Cohesion: 0.21
Nodes (7): ConnectedFeatureConfiguration, FeatureEvaluationContext, ClientVersion, DeploymentRing, Environment, PracticeKey, IConnectedFeatureEvaluator

### Community 58 - "FeatureSnapshotLoadResult"
Cohesion: 0.29
Nodes (5): SnapshotDocument, DateTimeOffset, FeatureSnapshotLoadResult, Snapshot, Status

### Community 59 - "CachedConnectedFeatureEvaluator"
Cohesion: 0.40
Nodes (4): CheckoutRuleMode, Guid, TimeSpan, CachedConnectedFeatureEvaluator

### Community 60 - "ConnectedFeatureTargets"
Cohesion: 0.29
Nodes (7): IReadOnlyList, IEnumerable, ConnectedFeatureTargets, Environments, MinimumClientVersion, PracticeKeys, Rings

### Community 61 - "CapabilityResponse"
Cohesion: 0.11
Nodes (16): ApiController, DateTimeOffset, Guid, HttpGet, IHttpActionResult, Route, CapabilitiesController, CapabilityApiReasons (+8 more)

### Community 62 - "ConnectedFeatures.cs"
Cohesion: 0.18
Nodes (10): ConnectedFeatureReasons, FeatureSnapshotLoadStatus, Invalid, Loaded, Missing, SourceError, IConnectedFeatureClock, UtcNow (+2 more)

### Community 63 - "Summary"
Cohesion: 0.50
Nodes (4): Business-rule migration mode, Evaluation boundary, Implemented-state evidence, Summary

### Community 64 - "ConnectedFeatureSnapshot"
Cohesion: 0.22
Nodes (9): ConnectedFeatureSnapshot, CheckoutRuleMode, CheckoutTargets, ConfigurationVersion, Enabled, ExpiresAt, IssuedAt, ParentTargets (+1 more)

### Community 65 - "CheckoutRuleMode"
Cohesion: 0.50
Nodes (4): CheckoutRuleMode, Compare, Legacy, Service

### Community 66 - "Summary"
Cohesion: 0.29
Nodes (6): Reconciliation disposition, Summary, Wave 0 update disposition, Wave 1 update disposition, Wave 2 update disposition, Wave 3 update disposition

## Knowledge Gaps
- **246 isolated node(s):** `NativeRules`, `DesktopClient`, `NativeRulesTests`, `idempotency_records`, `audit_log` (+241 more)
  These have ≤1 connection - possible missing edges or undocumented components.
- **10 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `ToolLending.AppServer` connect `ToolLending.AppServer` to `Repository`, `ApiControllerBase`, `MemberEligibilityContext`, `ConnectedTelemetry.cs`, `CapabilityResponse`, `ConnectedFeatures.cs`?**
  _High betweenness centrality (0.138) - this node is a cross-community bridge._
- **Why does `Program` connect `Program` to `ToolLending.AppServer`, `FlagEvaluationRecord`, `ConnectedTelemetryMetrics`, `IConnectedTelemetrySink`, `JsonDiagnosticConnectedTelemetrySink`, `ThrowingTelemetrySink`?**
  _High betweenness centrality (0.118) - this node is a cross-community bridge._
- **Why does `JsonFileFeatureSnapshotSource` connect `JsonFileFeatureSnapshotSource` to `FeatureEvaluationContext`, `FeatureSnapshotLoadResult`, `ConnectedFeatures.cs`, `Program`?**
  _High betweenness centrality (0.039) - this node is a cross-community bridge._
- **What connects `NativeRules`, `DesktopClient`, `NativeRulesTests` to the rest of the system?**
  _246 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `Repository` be split into smaller, more focused modules?**
  _Cohesion score 0.08163265306122448 - nodes in this community are weakly interconnected._
- **Should `ToolLending.AppServer` be split into smaller, more focused modules?**
  _Cohesion score 0.09666666666666666 - nodes in this community are weakly interconnected._
- **Should `MemberEligibilityContext` be split into smaller, more focused modules?**
  _Cohesion score 0.12 - nodes in this community are weakly interconnected._