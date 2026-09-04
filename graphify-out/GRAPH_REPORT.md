# Graph Report - win32_migration_poc  (2026-09-04)

## Corpus Check
- 83 files · ~60,740 words
- Verdict: corpus is large enough that graph structure adds value.

## Summary
- 871 nodes · 1703 edges · 73 communities (63 shown, 10 thin omitted)
- Extraction: 92% EXTRACTED · 8% INFERRED · 0% AMBIGUOUS · INFERRED: 144 edges (avg confidence: 0.84)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `e83f34f7`
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
- CapabilityCache
- DllMain
- ApiTests.ps1
- Connected Stage Mission
- Rule Distribution
- CheckoutRuleEvaluator
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
- IConnectedFeatureSnapshotSource
- CapabilityResponse
- RuleComparisonRecord
- ConnectedTelemetryMetrics
- SafeConnectedTelemetrySink
- ConnectedFeatureSnapshot
- JsonDiagnosticConnectedTelemetrySink
- ThrowingTelemetrySink
- ConnectedFeatures.cs
- FeatureSnapshotLoadResult
- ConnectedCapability
- ConnectedFeatureTargets
- Summary
- MemberEligibilityContext
- ConnectedTelemetry.cs
- Connected checkout operations
- InMemoryConnectedTelemetrySink
- Summary
- CheckoutDecisionResponse
- CheckoutDecisionRequest
- Summary
- SnapshotDocument
- CheckoutDecisionService
- Summary

## God Nodes (most connected - your core abstractions)
1. `Program` - 63 edges
2. `RuleComparisonRecord` - 27 edges
3. `InMemoryConnectedTelemetrySink` - 27 edges
4. `ConnectedCapability` - 24 edges
5. `ConnectedFeatureSnapshot` - 24 edges
6. `CheckoutDecisionService` - 20 edges
7. `FlagEvaluationRecord` - 20 edges
8. `Repository` - 20 edges
9. `CachedConnectedFeatureEvaluator` - 19 edges
10. `DesktopClientTests` - 19 edges

## Surprising Connections (you probably didn't know these)
- `checkTransportConfiguration()` --calls--> `ClassifyClientHttpStatus()`  [INFERRED]
  tests/NativeRulesTests/main.cpp → src/DesktopClient/ClientTransport.cpp
- `checkTransportConfiguration()` --calls--> `LoadClientEndpointConfiguration()`  [INFERRED]
  tests/NativeRulesTests/main.cpp → src/DesktopClient/ClientTransport.cpp
- `checkTransportConfiguration()` --calls--> `SendClientHttp()`  [INFERRED]
  tests/NativeRulesTests/main.cpp → src/DesktopClient/ClientTransport.cpp
- `checkFailurePresentation()` --calls--> `FormatClientHttpResult()`  [INFERRED]
  tests/NativeRulesTests/main.cpp → src/DesktopClient/ClientTransport.cpp
- `checkTransportConfiguration()` --calls--> `ClientEndpointConfiguration`  [INFERRED]
  tests/NativeRulesTests/main.cpp → src/DesktopClient/ClientTransport.h

## Import Cycles
- None detected.

## Communities (73 total, 10 thin omitted)

### Community 0 - "Repository"
Cohesion: 0.08
Nodes (35): List, NpgsqlConnection, NpgsqlTransaction, DateTime, Guid, IList, AuditDto, CheckoutRequest (+27 more)

### Community 1 - "DesktopClient/main.cpp"
Cohesion: 0.07
Nodes (71): HINSTANCE, HWND, LPARAM, LPWSTR, LRESULT, ClientUtcNow(), BuildCompareDecisionRequest(), BuildServiceDecisionRequest() (+63 more)

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

### Community 8 - "CapabilityCache"
Cohesion: 0.15
Nodes (25): CapabilityCache, configurationVersion, evaluatedAt, expiresAt, mode, valid, ClientRuleMode, string (+17 more)

### Community 9 - "DllMain"
Cohesion: 0.33
Nodes (5): BOOL, HMODULE, LPVOID, DWORD, DllMain()

### Community 19 - "CheckoutRuleEvaluator"
Cohesion: 0.18
Nodes (13): DateTime, CheckoutDecision, Allowed, CheckoutLimit, MaximumLoanDays, Reason, CheckoutDecisionReasons, CheckoutRuleEvaluator (+5 more)

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
Cohesion: 0.23
Nodes (8): Action, DateTimeOffset, FixedBusinessDateClock, Today, FixedClock, UtcNow, FixedEvaluator, Program

### Community 42 - "ClientEndpointConfiguration"
Cohesion: 0.07
Nodes (45): HINTERNET, INTERNET_PORT, EndpointRouter::EndpointRouter(), ClassifyClientHttpStatus(), ClassifySystemError(), ClientEndpoint, basePath, host (+37 more)

### Community 43 - "JsonFileFeatureSnapshotSource"
Cohesion: 0.13
Nodes (16): CheckoutDocument, SnapshotDocument, CheckoutDocument, RuleMode, Targets, ConnectedDocument, Checkout, Enabled (+8 more)

### Community 44 - "Application Service"
Cohesion: 0.47
Nodes (11): Architecture and Rule Ownership, Legacy Shared Credential Model, Eaglesoft POC North Star Architecture, Testing Baseline and Modernization Gates, Windows 11 Pro Development Setup, Windows Server 2019 Setup, Application Service, Checkout Request Flow (+3 more)

### Community 45 - "Summary"
Cohesion: 0.40
Nodes (4): Boundaries and dependencies, Entry points, Graph evidence, Summary

### Community 46 - "REQUIREMENTS.md"
Cohesion: 0.19
Nodes (6): Architecture, Durable constraint, Summary, Transaction and rule boundaries, Hotspots, Risks

### Community 47 - "FlagEvaluationRecord"
Cohesion: 0.20
Nodes (10): DateTimeOffset, Guid, FlagEvaluationRecord, CohortKeyHash, ConfigurationVersion, CorrelationId, EffectiveValue, FlagKey (+2 more)

### Community 48 - "IConnectedFeatureSnapshotSource"
Cohesion: 0.16
Nodes (8): IConnectedFeatureSnapshotSource, Exception, MutableClock, UtcNow, SequenceSource, Calls, ThrowingSource, Calls

### Community 49 - "CapabilityResponse"
Cohesion: 0.12
Nodes (16): ApiController, DateTimeOffset, Guid, HttpGet, IHttpActionResult, Route, CapabilitiesController, CapabilityApiReasons (+8 more)

### Community 50 - "RuleComparisonRecord"
Cohesion: 0.12
Nodes (15): RuleComparisonRecord, CohortKeyHash, ConfigurationVersion, CorrelationId, Duration, InputIdentityHash, LegacyAllowed, LegacyContractVersion (+7 more)

### Community 52 - "ConnectedTelemetryMetrics"
Cohesion: 0.24
Nodes (3): Dictionary, TimeSpan, ConnectedTelemetryMetrics

### Community 53 - "SafeConnectedTelemetrySink"
Cohesion: 0.44
Nodes (3): Action, SafeConnectedTelemetrySink, FailureCount

### Community 54 - "ConnectedFeatureSnapshot"
Cohesion: 0.16
Nodes (12): ConnectedFeatureSnapshot, CheckoutRuleMode, CheckoutTargets, ConfigurationVersion, Enabled, ExpiresAt, IssuedAt, ParentTargets (+4 more)

### Community 55 - "JsonDiagnosticConnectedTelemetrySink"
Cohesion: 0.33
Nodes (4): JObject, JsonDiagnosticConnectedTelemetrySink, Metrics, TextWriter

### Community 56 - "ThrowingTelemetrySink"
Cohesion: 0.39
Nodes (3): TimeSpan, ThrowingTelemetrySink, Calls

### Community 57 - "ConnectedFeatures.cs"
Cohesion: 0.14
Nodes (12): CheckoutRuleMode, Compare, Legacy, Service, ConnectedFeatureConfiguration, ConnectedFeatureReasons, FeatureEvaluationContext, ClientVersion (+4 more)

### Community 58 - "FeatureSnapshotLoadResult"
Cohesion: 0.20
Nodes (8): FeatureSnapshotLoadResult, Snapshot, Status, FeatureSnapshotLoadStatus, Invalid, Loaded, Missing, SourceError

### Community 59 - "ConnectedCapability"
Cohesion: 0.15
Nodes (18): CheckoutRuleMode, DateTimeOffset, Guid, TimeSpan, CachedConnectedFeatureEvaluator, ConnectedCapability, CheckoutRuleMode, ConfigurationVersion (+10 more)

### Community 60 - "ConnectedFeatureTargets"
Cohesion: 0.25
Nodes (7): IReadOnlyList, IEnumerable, ConnectedFeatureTargets, Environments, MinimumClientVersion, PracticeKeys, Rings

### Community 61 - "Summary"
Cohesion: 0.40
Nodes (4): Contract behavior, Current network boundary, Migration compatibility constraint, Summary

### Community 62 - "MemberEligibilityContext"
Cohesion: 0.20
Nodes (9): MemberEligibilityContext, Active, CheckoutLimit, HasOverdueLoan, MaximumLoanDays, MemberId, OpenLoans, Tier (+1 more)

### Community 63 - "ConnectedTelemetry.cs"
Cohesion: 0.17
Nodes (11): IDictionary, ConnectedDurationSummary, Count, MaximumMilliseconds, MinimumMilliseconds, TotalMilliseconds, ConnectedTelemetryMetricsSnapshot, Counters (+3 more)

### Community 64 - "Connected checkout operations"
Cohesion: 0.22
Nodes (8): Connected checkout operations, Desktop transport configuration, Mode behavior and compatibility, Ownership and retained components, Rollback and recovery, Service configuration, Telemetry and audit, Verification

### Community 65 - "InMemoryConnectedTelemetrySink"
Cohesion: 0.17
Nodes (9): LegacyCheckoutObservation, Allowed, ContractVersion, Reason, IList, InMemoryConnectedTelemetrySink, FlagEvaluations, Metrics (+1 more)

### Community 66 - "Summary"
Cohesion: 0.18
Nodes (11): Reconciliation disposition, Summary, Wave 0 update disposition, Wave 1 update disposition, Wave 2 update disposition, Wave 3 update disposition, Wave 4 update disposition, Wave 5 update disposition (+3 more)

### Community 67 - "CheckoutDecisionResponse"
Cohesion: 0.20
Nodes (10): CheckoutDecisionResponse, Allowed, CheckoutLimit, ConfigurationVersion, ContractVersion, CorrelationId, EffectiveMode, MaximumLoanDays (+2 more)

### Community 68 - "CheckoutDecisionRequest"
Cohesion: 0.10
Nodes (19): Exception, HttpStatusCode, DateTime, HttpPost, IHttpActionResult, Route, CapabilityStaleException, CheckoutDecisionErrorCodes (+11 more)

### Community 70 - "Summary"
Cohesion: 0.50
Nodes (4): Business-rule migration mode, Evaluation boundary, Implemented-state evidence, Summary

### Community 71 - "SnapshotDocument"
Cohesion: 0.29
Nodes (7): ConnectedDocument, SnapshotDocument, ConfigurationVersion, Connected, ExpiresAt, IssuedAt, SchemaVersion

### Community 72 - "CheckoutDecisionService"
Cohesion: 0.21
Nodes (6): CapabilityService, Func, Guid, TimeSpan, CheckoutDecisionService, IConnectedTelemetrySink

### Community 73 - "Summary"
Cohesion: 0.50
Nodes (4): Evidence confidence, Ownership implication, Responsibilities, Summary

## Knowledge Gaps
- **304 isolated node(s):** `NativeRules`, `DesktopClient`, `NativeRulesTests`, `idempotency_records`, `audit_log` (+299 more)
  These have ≤1 connection - possible missing edges or undocumented components.
- **10 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `ToolLending.AppServer` connect `ToolLending.AppServer` to `Repository`, `ApiControllerBase`, `CheckoutDecisionRequest`, `CapabilityResponse`, `CheckoutRuleEvaluator`, `ConnectedFeatures.cs`, `ConnectedTelemetry.cs`?**
  _High betweenness centrality (0.077) - this node is a cross-community bridge._
- **Why does `Program` connect `Program` to `InMemoryConnectedTelemetrySink`, `ToolLending.AppServer`, `IConnectedFeatureSnapshotSource`, `ConnectedTelemetryMetrics`, `SafeConnectedTelemetrySink`, `ConnectedFeatureSnapshot`, `JsonDiagnosticConnectedTelemetrySink`, `ThrowingTelemetrySink`, `ConnectedFeatureTargets`, `MemberEligibilityContext`?**
  _High betweenness centrality (0.056) - this node is a cross-community bridge._
- **Why does `Repository` connect `Repository` to `Program`, `ApiControllerBase`, `CheckoutRuleEvaluator`, `ToolLending.AppServer`?**
  _High betweenness centrality (0.034) - this node is a cross-community bridge._
- **Are the 2 inferred relationships involving `RuleComparisonRecord` (e.g. with `.Decide()` and `.RecordIncompleteComparison()`) actually correct?**
  _`RuleComparisonRecord` has 2 INFERRED edges - model-reasoned connections that need verification._
- **Are the 12 inferred relationships involving `InMemoryConnectedTelemetrySink` (e.g. with `.AssertServiceDecision()` and `.CapabilityApiMapsEveryEffectiveMode()`) actually correct?**
  _`InMemoryConnectedTelemetrySink` has 12 INFERRED edges - model-reasoned connections that need verification._
- **What connects `NativeRules`, `DesktopClient`, `NativeRulesTests` to the rest of the system?**
  _304 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `Repository` be split into smaller, more focused modules?**
  _Cohesion score 0.08163265306122448 - nodes in this community are weakly interconnected._