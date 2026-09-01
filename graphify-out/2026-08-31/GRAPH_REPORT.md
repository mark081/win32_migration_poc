# Graph Report - win32_migration_poc  (2026-08-29)

## Corpus Check
- 72 files · ~38,421 words
- Verdict: corpus is large enough that graph structure adds value.

## Summary
- 528 nodes · 907 edges · 52 communities (41 shown, 11 thin omitted)
- Extraction: 97% EXTRACTED · 3% INFERRED · 0% AMBIGUOUS · INFERRED: 28 edges (avg confidence: 0.84)
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
- ToolLending.AppServer
- 001_schema.sql
- DesktopClient.UiTests.csproj
- REQUIREMENTS.md
- checkEligibility
- DllMain
- ApiTests.ps1
- Connected Stage Mission
- Rule Distribution
- knowledge/index.md
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
- Summary
- Summary
- Summary
- build-test-deploy.md
- hotspots-and-risks.md

## God Nodes (most connected - your core abstractions)
1. `Program` - 28 edges
2. `ConnectedFeatureSnapshot` - 24 edges
3. `CachedConnectedFeatureEvaluator` - 19 edges
4. `ConnectedCapability` - 18 edges
5. `FeatureSnapshotLoadResult` - 18 edges
6. `DesktopClientTests` - 17 edges
7. `Repository` - 15 edges
8. `WindowProc()` - 15 edges
9. `Requirements: Service-Owned Client Business Rules` - 15 edges
10. `ApiControllerBase` - 13 edges

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

## Communities (52 total, 11 thin omitted)

### Community 0 - "Repository"
Cohesion: 0.08
Nodes (34): NpgsqlConnection, NpgsqlTransaction, DateTime, Guid, IList, AuditDto, CheckoutRequest, DueOn (+26 more)

### Community 1 - "DesktopClient/main.cpp"
Cohesion: 0.17
Nodes (33): HINSTANCE, HWND, LPARAM, LPWSTR, LRESULT, AddTool(), AddUser(), Checkout() (+25 more)

### Community 2 - "ApiControllerBase"
Cohesion: 0.17
Nodes (16): ApiController, HttpGet, HttpPost, IHttpActionResult, Route, Func, Guid, ApiControllerBase (+8 more)

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

### Community 7 - "REQUIREMENTS.md"
Cohesion: 0.24
Nodes (4): Architecture, Durable constraint, Summary, Transaction and rule boundaries

### Community 8 - "checkEligibility"
Cohesion: 0.29
Nodes (12): CheckoutEligibilityReasonV1(), CheckoutLimit(), CheckoutEligibilityReasonCode, wchar_t, IsEligibleForCheckout(), MaximumLoanDays(), tier_is(), check() (+4 more)

### Community 9 - "DllMain"
Cohesion: 0.33
Nodes (5): BOOL, HMODULE, LPVOID, DWORD, DllMain()

### Community 19 - "knowledge/index.md"
Cohesion: 0.15
Nodes (8): North Star Architecture, Reconciliation disposition, Summary, Wave 0 update disposition, Summary, Terms, Evidence gaps, Open questions

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
Nodes (27): Action, Exception, SnapshotDocument, ConnectedFeatureSnapshot, CheckoutTargets, ConfigurationVersion, Enabled, ExpiresAt (+19 more)

### Community 42 - "CachedConnectedFeatureEvaluator"
Cohesion: 0.06
Nodes (42): IReadOnlyList, DateTimeOffset, Guid, IEnumerable, CachedConnectedFeatureEvaluator, CheckoutRuleMode, Compare, Legacy (+34 more)

### Community 43 - "JsonFileFeatureSnapshotSource"
Cohesion: 0.09
Nodes (22): CheckoutDocument, ConnectedDocument, CheckoutDocument, RuleMode, Targets, ConnectedDocument, Checkout, Enabled (+14 more)

### Community 44 - "Application Service"
Cohesion: 0.47
Nodes (11): Architecture and Rule Ownership, Legacy Shared Credential Model, Eaglesoft POC North Star Architecture, Testing Baseline and Modernization Gates, Windows 11 Pro Development Setup, Windows Server 2019 Setup, Application Service, Checkout Request Flow (+3 more)

### Community 45 - "Summary"
Cohesion: 0.40
Nodes (4): Boundaries and dependencies, Entry points, Graph evidence, Summary

### Community 46 - "Summary"
Cohesion: 0.50
Nodes (4): Business-rule migration mode, Evaluation boundary, Implemented-state evidence, Summary

### Community 47 - "Summary"
Cohesion: 0.50
Nodes (4): Evidence confidence, Ownership implication, Responsibilities, Summary

### Community 48 - "Summary"
Cohesion: 0.50
Nodes (4): Contract behavior, Current network limitation, Migration compatibility constraint, Summary

### Community 49 - "build-test-deploy.md"
Cohesion: 0.50
Nodes (3): Change-sensitive scenarios, Summary, Verification layers

## Knowledge Gaps
- **185 isolated node(s):** `NativeRules`, `DesktopClient`, `NativeRulesTests`, `idempotency_records`, `audit_log` (+180 more)
  These have ≤1 connection - possible missing edges or undocumented components.
- **11 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `ToolLending.AppServer` connect `ToolLending.AppServer` to `Repository`, `CachedConnectedFeatureEvaluator`, `ApiControllerBase`?**
  _High betweenness centrality (0.114) - this node is a cross-community bridge._
- **Why does `JsonFileFeatureSnapshotSource` connect `JsonFileFeatureSnapshotSource` to `Program`, `CachedConnectedFeatureEvaluator`?**
  _High betweenness centrality (0.035) - this node is a cross-community bridge._
- **Why does `Design Document: Service-Owned Client Business Rules` connect `Design Document: Service-Owned Client Business Rules` to `REQUIREMENTS.md`?**
  _High betweenness centrality (0.028) - this node is a cross-community bridge._
- **What connects `NativeRules`, `DesktopClient`, `NativeRulesTests` to the rest of the system?**
  _185 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `Repository` be split into smaller, more focused modules?**
  _Cohesion score 0.08421985815602837 - nodes in this community are weakly interconnected._
- **Should `ToolLending.AppServer` be split into smaller, more focused modules?**
  _Cohesion score 0.09666666666666666 - nodes in this community are weakly interconnected._
- **Should `Design Document: Service-Owned Client Business Rules` be split into smaller, more focused modules?**
  _Cohesion score 0.05263157894736842 - nodes in this community are weakly interconnected._