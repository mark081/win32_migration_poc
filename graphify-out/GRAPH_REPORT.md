# Graph Report - win32_migration_poc  (2026-09-02)

## Corpus Check
- 78 files · ~53,252 words
- Verdict: corpus is large enough that graph structure adds value.

## Summary
- 799 nodes · 1497 edges · 73 communities (62 shown, 11 thin omitted)
- Extraction: 93% EXTRACTED · 7% INFERRED · 0% AMBIGUOUS · INFERRED: 107 edges (avg confidence: 0.83)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `f53a9842`
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
- CapabilityResponse
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
- ClientEndpointConfiguration
- JsonFileFeatureSnapshotSource
- Application Service
- Summary
- REQUIREMENTS.md
- FlagEvaluationRecord
- ConnectedFeatureSnapshot
- FeatureEvaluationContext
- RuleComparisonRecord
- ConnectedTelemetryMetrics
- SafeConnectedTelemetrySink
- ConnectedTelemetry.cs
- JsonDiagnosticConnectedTelemetrySink
- ThrowingTelemetrySink
- ConnectedFeatures.cs
- FeatureSnapshotLoadResult
- ConnectedCapability
- ConnectedFeatureTargets
- CapabilityService
- FeatureSnapshotLoadStatus
- Summary
- TargetsDocument
- CheckoutDecisionRequest
- Summary
- CheckoutDecisionResponse
- CheckoutDecisionsController
- hotspots-and-risks.md
- SnapshotDocument
- CheckoutDecisionService
- Summary

## God Nodes (most connected - your core abstractions)
1. `Program` - 60 edges
2. `RuleComparisonRecord` - 27 edges
3. `InMemoryConnectedTelemetrySink` - 25 edges
4. `ConnectedCapability` - 24 edges
5. `ConnectedFeatureSnapshot` - 24 edges
6. `FlagEvaluationRecord` - 20 edges
7. `CachedConnectedFeatureEvaluator` - 19 edges
8. `Repository` - 19 edges
9. `CheckoutDecisionService` - 18 edges
10. `FeatureSnapshotLoadResult` - 18 edges

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

## Communities (73 total, 11 thin omitted)

### Community 0 - "Repository"
Cohesion: 0.08
Nodes (35): List, NpgsqlConnection, NpgsqlTransaction, DateTime, Guid, IList, AuditDto, CheckoutRequest (+27 more)

### Community 1 - "DesktopClient/main.cpp"
Cohesion: 0.17
Nodes (33): HINSTANCE, HWND, LPARAM, LPWSTR, LRESULT, AddTool(), AddUser(), Checkout() (+25 more)

### Community 2 - "ApiControllerBase"
Cohesion: 0.18
Nodes (15): Func, Guid, HttpGet, HttpPost, IHttpActionResult, Route, ApiControllerBase, Actor (+7 more)

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
Cohesion: 0.14
Nodes (8): North Star Architecture, Summary, Terms, Evidence gaps, Open questions, Change-sensitive scenarios, Summary, Verification layers

### Community 8 - "CapabilityResponse"
Cohesion: 0.22
Nodes (9): DateTimeOffset, CapabilityResponse, ConfigurationVersion, ConnectedEnabled, CorrelationId, EvaluatedAt, ExpiresAt, Reason (+1 more)

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
Cohesion: 0.08
Nodes (26): LegacyCheckoutObservation, Allowed, ContractVersion, Reason, IList, InMemoryConnectedTelemetrySink, FlagEvaluations, Metrics (+18 more)

### Community 42 - "ClientEndpointConfiguration"
Cohesion: 0.06
Nodes (57): HINTERNET, INTERNET_PORT, ClassifyClientHttpStatus(), ClassifySystemError(), ClientEndpoint, basePath, host, port (+49 more)

### Community 43 - "JsonFileFeatureSnapshotSource"
Cohesion: 0.19
Nodes (11): CheckoutDocument, SnapshotDocument, CheckoutDocument, RuleMode, Targets, ConnectedDocument, Checkout, Enabled (+3 more)

### Community 44 - "Application Service"
Cohesion: 0.47
Nodes (11): Architecture and Rule Ownership, Legacy Shared Credential Model, Eaglesoft POC North Star Architecture, Testing Baseline and Modernization Gates, Windows 11 Pro Development Setup, Windows Server 2019 Setup, Application Service, Checkout Request Flow (+3 more)

### Community 45 - "Summary"
Cohesion: 0.40
Nodes (4): Boundaries and dependencies, Entry points, Graph evidence, Summary

### Community 46 - "REQUIREMENTS.md"
Cohesion: 0.16
Nodes (8): Architecture, Durable constraint, Summary, Transaction and rule boundaries, Contract behavior, Current network boundary, Migration compatibility constraint, Summary

### Community 47 - "FlagEvaluationRecord"
Cohesion: 0.20
Nodes (10): DateTimeOffset, Guid, FlagEvaluationRecord, CohortKeyHash, ConfigurationVersion, CorrelationId, EffectiveValue, FlagKey (+2 more)

### Community 48 - "ConnectedFeatureSnapshot"
Cohesion: 0.22
Nodes (9): ConnectedFeatureSnapshot, CheckoutRuleMode, CheckoutTargets, ConfigurationVersion, Enabled, ExpiresAt, IssuedAt, ParentTargets (+1 more)

### Community 49 - "FeatureEvaluationContext"
Cohesion: 0.40
Nodes (5): FeatureEvaluationContext, ClientVersion, DeploymentRing, Environment, PracticeKey

### Community 50 - "RuleComparisonRecord"
Cohesion: 0.12
Nodes (15): RuleComparisonRecord, CohortKeyHash, ConfigurationVersion, CorrelationId, Duration, InputIdentityHash, LegacyAllowed, LegacyContractVersion (+7 more)

### Community 52 - "ConnectedTelemetryMetrics"
Cohesion: 0.24
Nodes (3): Dictionary, TimeSpan, ConnectedTelemetryMetrics

### Community 53 - "SafeConnectedTelemetrySink"
Cohesion: 0.44
Nodes (3): Action, SafeConnectedTelemetrySink, FailureCount

### Community 54 - "ConnectedTelemetry.cs"
Cohesion: 0.17
Nodes (11): IDictionary, ConnectedDurationSummary, Count, MaximumMilliseconds, MinimumMilliseconds, TotalMilliseconds, ConnectedTelemetryMetricsSnapshot, Counters (+3 more)

### Community 55 - "JsonDiagnosticConnectedTelemetrySink"
Cohesion: 0.33
Nodes (4): JObject, JsonDiagnosticConnectedTelemetrySink, Metrics, TextWriter

### Community 56 - "ThrowingTelemetrySink"
Cohesion: 0.39
Nodes (3): TimeSpan, ThrowingTelemetrySink, Calls

### Community 57 - "ConnectedFeatures.cs"
Cohesion: 0.18
Nodes (10): CheckoutRuleMode, Compare, Legacy, Service, ConnectedFeatureConfiguration, ConnectedFeatureReasons, IConnectedFeatureClock, UtcNow (+2 more)

### Community 58 - "FeatureSnapshotLoadResult"
Cohesion: 0.24
Nodes (7): DateTimeOffset, FeatureSnapshotLoadResult, Snapshot, Status, IConnectedFeatureSnapshotSource, SequenceSource, Calls

### Community 59 - "ConnectedCapability"
Cohesion: 0.18
Nodes (13): CheckoutRuleMode, Guid, TimeSpan, CachedConnectedFeatureEvaluator, ConnectedCapability, CheckoutRuleMode, ConfigurationVersion, ConnectedEnabled (+5 more)

### Community 60 - "ConnectedFeatureTargets"
Cohesion: 0.29
Nodes (7): IReadOnlyList, IEnumerable, ConnectedFeatureTargets, Environments, MinimumClientVersion, PracticeKeys, Rings

### Community 61 - "CapabilityService"
Cohesion: 0.24
Nodes (7): Guid, HttpGet, IHttpActionResult, Route, CapabilitiesController, CapabilityApiReasons, CapabilityService

### Community 62 - "FeatureSnapshotLoadStatus"
Cohesion: 0.40
Nodes (5): FeatureSnapshotLoadStatus, Invalid, Loaded, Missing, SourceError

### Community 63 - "Summary"
Cohesion: 0.50
Nodes (4): Business-rule migration mode, Evaluation boundary, Implemented-state evidence, Summary

### Community 64 - "TargetsDocument"
Cohesion: 0.40
Nodes (5): TargetsDocument, Environments, MinimumClientVersion, PracticeKeys, Rings

### Community 65 - "CheckoutDecisionRequest"
Cohesion: 0.29
Nodes (7): DateTime, CheckoutDecisionRequest, CapabilityConfigurationVersion, ClientVersion, DueOn, LegacyObservation, MemberId

### Community 66 - "Summary"
Cohesion: 0.25
Nodes (8): Reconciliation disposition, Summary, Wave 0 update disposition, Wave 1 update disposition, Wave 2 update disposition, Wave 3 update disposition, Wave 4 update disposition, Wave 5 update disposition

### Community 67 - "CheckoutDecisionResponse"
Cohesion: 0.14
Nodes (13): Exception, CapabilityStaleException, CheckoutDecisionErrorCodes, CheckoutDecisionResponse, Allowed, CheckoutLimit, ConfigurationVersion, ContractVersion (+5 more)

### Community 68 - "CheckoutDecisionsController"
Cohesion: 0.18
Nodes (10): ApiController, HttpStatusCode, HttpPost, IHttpActionResult, Route, CheckoutDecisionErrorResponse, Code, CorrelationId (+2 more)

### Community 71 - "SnapshotDocument"
Cohesion: 0.29
Nodes (7): ConnectedDocument, SnapshotDocument, ConfigurationVersion, Connected, ExpiresAt, IssuedAt, SchemaVersion

### Community 72 - "CheckoutDecisionService"
Cohesion: 0.20
Nodes (6): Func, Guid, TimeSpan, CheckoutDecisionService, IConnectedFeatureEvaluator, IConnectedTelemetrySink

### Community 73 - "Summary"
Cohesion: 0.50
Nodes (4): Evidence confidence, Ownership implication, Responsibilities, Summary

## Knowledge Gaps
- **286 isolated node(s):** `NativeRules`, `DesktopClient`, `NativeRulesTests`, `idempotency_records`, `audit_log` (+281 more)
  These have ≤1 connection - possible missing edges or undocumented components.
- **11 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `ToolLending.AppServer` connect `ToolLending.AppServer` to `Repository`, `ApiControllerBase`, `CheckoutDecisionResponse`, `MemberEligibilityContext`, `ConnectedTelemetry.cs`, `ConnectedFeatures.cs`, `CapabilityService`?**
  _High betweenness centrality (0.093) - this node is a cross-community bridge._
- **Why does `Program` connect `Program` to `ToolLending.AppServer`, `ConnectedTelemetryMetrics`, `SafeConnectedTelemetrySink`, `JsonDiagnosticConnectedTelemetrySink`, `ThrowingTelemetrySink`, `FeatureSnapshotLoadResult`?**
  _High betweenness centrality (0.065) - this node is a cross-community bridge._
- **Why does `Repository` connect `Repository` to `Program`, `ApiControllerBase`, `MemberEligibilityContext`, `ToolLending.AppServer`?**
  _High betweenness centrality (0.037) - this node is a cross-community bridge._
- **Are the 2 inferred relationships involving `RuleComparisonRecord` (e.g. with `.Decide()` and `.RecordIncompleteComparison()`) actually correct?**
  _`RuleComparisonRecord` has 2 INFERRED edges - model-reasoned connections that need verification._
- **Are the 10 inferred relationships involving `InMemoryConnectedTelemetrySink` (e.g. with `.CapabilityApiMapsEveryEffectiveMode()` and `.CapabilityApiRejectsUnsafeVersions()`) actually correct?**
  _`InMemoryConnectedTelemetrySink` has 10 INFERRED edges - model-reasoned connections that need verification._
- **What connects `NativeRules`, `DesktopClient`, `NativeRulesTests` to the rest of the system?**
  _286 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `Repository` be split into smaller, more focused modules?**
  _Cohesion score 0.08163265306122448 - nodes in this community are weakly interconnected._