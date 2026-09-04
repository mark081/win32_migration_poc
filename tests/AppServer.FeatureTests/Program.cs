// PURPOSE
//
// This executable runs focused checks for the Connected feature evaluator, telemetry, and the
// service checkout rule foundation. It uses small in-process assertions instead of another test
// framework. The repository case reads synthetic PostgreSQL data but never submits a workflow
// command. This file is test-only and does not run in the product.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using Npgsql;
using ToolLending.AppServer;

namespace ToolLending.AppServer.FeatureTests
{
    // Runs the dependency-free executable checks for feature evaluation and Connected telemetry.
    internal static class Program
    {
        private static readonly DateTimeOffset Now = new DateTimeOffset(
            2026,
            8,
            29,
            12,
            0,
            0,
            TimeSpan.Zero
        );
        private static int failures;

        // Runs every focused case and returns a non-zero process exit code if any case fails.
        private static int Main()
        {
            Run("missing snapshot is disabled", MissingSnapshotIsDisabled);
            Run("false parent dominates service child", FalseParentDominatesChild);
            Run("expired snapshot is disabled", ExpiredSnapshotIsDisabled);
            Run("future snapshot is disabled", FutureSnapshotIsDisabled);
            Run("maximum-age snapshot is disabled", MaximumAgeSnapshotIsDisabled);
            Run("source exception is disabled and cached", SourceExceptionIsDisabledAndCached);
            Run("source timeout is disabled", SourceTimeoutIsDisabled);
            Run("snapshot refresh replaces cached state", SnapshotRefreshReplacesCachedState);
            Run("targeting only narrows parent", ParentTargetMissIsDisabled);
            Run("child target miss falls back to legacy", ChildTargetMissFallsBackToLegacy);
            Run("invalid child falls back to legacy", InvalidChildFallsBackToLegacy);
            Run("valid targeted service mode", ValidTargetedServiceMode);
            Run("malformed JSON is invalid", MalformedJsonIsInvalid);
            Run("telemetry emits safe required fields", TelemetryEmitsSafeRequiredFields);
            Run("telemetry metrics are bounded", TelemetryMetricsAreBounded);
            Run("telemetry durations are summarized", TelemetryDurationsAreSummarized);
            Run("telemetry failures are isolated", TelemetryFailuresAreIsolated);
            Run("in-memory telemetry captures records", InMemoryTelemetryCapturesRecords);
            Run("checkout decisions match the native rule table", CheckoutDecisionTable);
            Run("checkout reason ordering is stable", CheckoutReasonOrderingIsStable);
            Run("checkout due-date boundaries use a fixed clock", CheckoutDueDateBoundaries);
            Run("member eligibility query is read only", MemberEligibilityQueryIsReadOnly);
            Run("capability API maps every effective mode", CapabilityApiMapsEveryEffectiveMode);
            Run(
                "capability API rejects unsafe client versions",
                CapabilityApiRejectsUnsafeVersions
            );
            Run("checkout decision uses service result in service mode", DecisionUsesServiceResult);
            Run("checkout decision maps every service result", DecisionMapsEveryServiceResult);
            Run(
                "checkout decision preserves Legacy result in compare mode",
                DecisionPreservesLegacyResult
            );
            Run("checkout decision records compare match", DecisionRecordsCompareMatch);
            Run("checkout decision rejects stale capability", DecisionRejectsStaleCapability);
            Run(
                "checkout decision requires compare observation",
                DecisionRequiresCompareObservation
            );
            Run(
                "checkout decision records compare read failure",
                DecisionRecordsCompareReadFailure
            );
            Run(
                "checkout decision repository read is side effect free",
                DecisionRepositoryReadIsSideEffectFree
            );
            Run(
                "migration mode matrix preserves database state",
                MigrationModeMatrixPreservesDatabaseState
            );
            Run("checkout decision isolates telemetry failure", DecisionIsolatesTelemetryFailure);
            Console.WriteLine(
                failures == 0
                    ? "All feature evaluator tests passed."
                    : failures + " feature evaluator test(s) failed."
            );
            return failures == 0 ? 0 : 1;
        }

        // Proves absent configuration cannot activate the parent or a child mode.
        private static void MissingSnapshotIsDisabled()
        {
            var result = Evaluate(new FixedSource(FeatureSnapshotLoadResult.Missing()));
            Equal(false, result.ConnectedEnabled);
            Equal(CheckoutRuleMode.Legacy, result.CheckoutRuleMode);
            Equal(ConnectedFeatureReasons.SnapshotMissing, result.Reason);
        }

        // Proves a Service child cannot bypass a disabled parent.
        private static void FalseParentDominatesChild()
        {
            var result = Evaluate(
                new FixedSource(FeatureSnapshotLoadResult.Loaded(Snapshot(false, "service")))
            );
            Equal(false, result.ConnectedEnabled);
            Equal(CheckoutRuleMode.Legacy, result.CheckoutRuleMode);
            Equal(ConnectedFeatureReasons.ParentDisabled, result.Reason);
        }

        // Proves provider expiry forces the safe Legacy fallback.
        private static void ExpiredSnapshotIsDisabled()
        {
            var snapshot = new ConnectedFeatureSnapshot(
                1,
                "expired",
                Now.AddMinutes(-10),
                Now.AddMinutes(-1),
                true,
                Targets(),
                "service",
                Targets()
            );
            var result = Evaluate(new FixedSource(FeatureSnapshotLoadResult.Loaded(snapshot)));
            Equal(false, result.ConnectedEnabled);
            Equal(ConnectedFeatureReasons.SnapshotExpired, result.Reason);
        }

        // Proves source failure is isolated, disabled, and not retried per operation.
        private static void SourceExceptionIsDisabledAndCached()
        {
            var source = new ThrowingSource();
            var evaluator = Evaluator(source);
            Equal(
                ConnectedFeatureReasons.SourceError,
                evaluator.Evaluate(Context(), Guid.NewGuid()).Reason
            );
            Equal(
                ConnectedFeatureReasons.SourceError,
                evaluator.Evaluate(Context(), Guid.NewGuid()).Reason
            );
            Equal(1, source.Calls);
        }

        // Proves a not-yet-issued snapshot cannot activate Connected behavior.
        private static void FutureSnapshotIsDisabled()
        {
            var snapshot = new ConnectedFeatureSnapshot(
                1,
                "future",
                Now.AddMinutes(1),
                Now.AddMinutes(10),
                true,
                Targets(),
                "service",
                Targets()
            );
            var result = Evaluate(new FixedSource(FeatureSnapshotLoadResult.Loaded(snapshot)));
            Equal(false, result.ConnectedEnabled);
            Equal(ConnectedFeatureReasons.SnapshotExpired, result.Reason);
        }

        // Proves local freshness policy can reject a provider snapshot before provider expiry.
        private static void MaximumAgeSnapshotIsDisabled()
        {
            var snapshot = new ConnectedFeatureSnapshot(
                1,
                "stale",
                Now.AddMinutes(-6),
                Now.AddMinutes(10),
                true,
                Targets(),
                "service",
                Targets()
            );
            var result = Evaluate(new FixedSource(FeatureSnapshotLoadResult.Loaded(snapshot)));
            Equal(false, result.ConnectedEnabled);
            Equal(ConnectedFeatureReasons.SnapshotExpired, result.Reason);
        }

        // Proves a timeout is treated as provider failure and selects Legacy.
        private static void SourceTimeoutIsDisabled()
        {
            var result = Evaluate(new ThrowingSource(new TimeoutException("synthetic timeout")));
            Equal(false, result.ConnectedEnabled);
            Equal(ConnectedFeatureReasons.SourceError, result.Reason);
        }

        // Proves cached state remains stable until the refresh boundary and then changes atomically.
        private static void SnapshotRefreshReplacesCachedState()
        {
            var clock = new MutableClock(Now);
            var source = new SequenceSource(
                FeatureSnapshotLoadResult.Loaded(Snapshot(false, "service")),
                FeatureSnapshotLoadResult.Loaded(Snapshot(true, "service"))
            );
            var evaluator = new CachedConnectedFeatureEvaluator(
                source,
                clock,
                TimeSpan.FromSeconds(30),
                TimeSpan.FromMinutes(5),
                TimeSpan.FromSeconds(30)
            );

            Equal(false, evaluator.Evaluate(Context(), Guid.NewGuid()).ConnectedEnabled);
            clock.UtcNow = Now.AddSeconds(29);
            Equal(false, evaluator.Evaluate(Context(), Guid.NewGuid()).ConnectedEnabled);
            Equal(1, source.Calls);
            clock.UtcNow = Now.AddSeconds(30);
            Equal(true, evaluator.Evaluate(Context(), Guid.NewGuid()).ConnectedEnabled);
            Equal(
                CheckoutRuleMode.Service,
                evaluator.Evaluate(Context(), Guid.NewGuid()).CheckoutRuleMode
            );
            Equal(2, source.Calls);
        }

        // Proves parent targeting can narrow but never broaden the Connected rollout.
        private static void ParentTargetMissIsDisabled()
        {
            var snapshot = new ConnectedFeatureSnapshot(
                1,
                "target",
                Now.AddMinutes(-1),
                Now.AddMinutes(10),
                true,
                Targets(new[] { "connected-test" }),
                "service",
                Targets()
            );
            var result = Evaluate(new FixedSource(FeatureSnapshotLoadResult.Loaded(snapshot)));
            Equal(false, result.ConnectedEnabled);
            Equal(ConnectedFeatureReasons.ParentTargetMiss, result.Reason);
        }

        // Proves an unknown checkout mode retains an enabled parent but selects Legacy checkout.
        private static void InvalidChildFallsBackToLegacy()
        {
            var result = Evaluate(
                new FixedSource(FeatureSnapshotLoadResult.Loaded(Snapshot(true, "unexpected")))
            );
            Equal(true, result.ConnectedEnabled);
            Equal(CheckoutRuleMode.Legacy, result.CheckoutRuleMode);
            Equal(ConnectedFeatureReasons.ChildInvalid, result.Reason);
        }

        // Proves child targeting can narrow checkout migration independently of the parent.
        private static void ChildTargetMissFallsBackToLegacy()
        {
            var snapshot = new ConnectedFeatureSnapshot(
                1,
                "child-target",
                Now.AddMinutes(-1),
                Now.AddMinutes(10),
                true,
                Targets(),
                "service",
                Targets(new[] { "connected-test" })
            );
            var result = Evaluate(new FixedSource(FeatureSnapshotLoadResult.Loaded(snapshot)));
            Equal(true, result.ConnectedEnabled);
            Equal(CheckoutRuleMode.Legacy, result.CheckoutRuleMode);
            Equal(ConnectedFeatureReasons.ChildTargetMiss, result.Reason);
        }

        // Proves matching all target dimensions can select Service and preserve configuration version.
        private static void ValidTargetedServiceMode()
        {
            var targets = new ConnectedFeatureTargets(
                new[] { "local" },
                new[] { "synthetic-practice" },
                new[] { "default" },
                "1.2.0"
            );
            var snapshot = new ConnectedFeatureSnapshot(
                1,
                "service-1",
                Now.AddMinutes(-1),
                Now.AddMinutes(10),
                true,
                targets,
                "service",
                targets
            );
            var result = Evaluate(new FixedSource(FeatureSnapshotLoadResult.Loaded(snapshot)));
            Equal(true, result.ConnectedEnabled);
            Equal(CheckoutRuleMode.Service, result.CheckoutRuleMode);
            Equal(ConnectedFeatureReasons.Service, result.Reason);
            Equal("service-1", result.ConfigurationVersion);
        }

        // Proves malformed provider content is classified as invalid rather than throwing.
        private static void MalformedJsonIsInvalid()
        {
            var path = Path.Combine(
                Path.GetTempPath(),
                "connected-feature-test-" + Guid.NewGuid().ToString("N") + ".json"
            );
            try
            {
                File.WriteAllText(path, "{ not-json }");
                Equal(
                    FeatureSnapshotLoadStatus.Invalid,
                    new JsonFileFeatureSnapshotSource(path).Load(Now).Status
                );
            }
            finally
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
        }

        // Proves required diagnostic fields are present while raw sensitive inputs are absent.
        private static void TelemetryEmitsSafeRequiredFields()
        {
            var correlationId = Guid.NewGuid();
            var output = new StringWriter();
            var sink = new JsonDiagnosticConnectedTelemetrySink(output, 8);
            sink.RecordFlagEvaluation(
                new FlagEvaluationRecord(
                    Now,
                    "connected.enabled",
                    "false",
                    "SNAPSHOT_MISSING",
                    "config-1",
                    "practice-secret",
                    correlationId
                )
            );
            sink.RecordRuleComparison(
                new RuleComparisonRecord(
                    Now,
                    correlationId,
                    "config-1",
                    "practice-secret",
                    "member-42|Alice",
                    1,
                    false,
                    "OVERDUE",
                    1,
                    true,
                    "ALLOWED",
                    false,
                    TimeSpan.FromMilliseconds(12),
                    "completed"
                )
            );

            var lines = output
                .ToString()
                .Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            Equal(2, lines.Length);
            var flag = JObject.Parse(lines[0]);
            var comparison = JObject.Parse(lines[1]);
            HasFields(
                flag,
                "eventType",
                "timestamp",
                "flagKey",
                "effectiveValue",
                "reason",
                "configurationVersion",
                "cohortKey",
                "correlationId"
            );
            HasFields(
                comparison,
                "eventType",
                "timestamp",
                "correlationId",
                "configurationVersion",
                "cohortKey",
                "inputIdentity",
                "legacyContractVersion",
                "legacyAllowed",
                "legacyReason",
                "serviceContractVersion",
                "serviceAllowed",
                "serviceReason",
                "match",
                "durationMs",
                "outcome"
            );
            Equal(false, output.ToString().Contains("practice-secret"));
            Equal(false, output.ToString().Contains("member-42"));
            Equal(false, output.ToString().Contains("Alice"));
            Equal(false, output.ToString().Contains("Authorization"));
            Equal(false, output.ToString().Contains("requestBody"));
        }

        // Proves excess metric cardinality is counted and discarded at the configured limit.
        private static void TelemetryMetricsAreBounded()
        {
            var metrics = new ConnectedTelemetryMetrics(2);
            metrics.Increment("flag_evaluations", "legacy", "disabled");
            metrics.Increment("flag_evaluations", "compare", "enabled");
            metrics.Increment("flag_evaluations", "service", "enabled");
            var snapshot = metrics.Snapshot();
            Equal(2, snapshot.Counters.Count);
            Equal(1L, snapshot.DroppedSeries);
        }

        // Proves duration count, total, minimum, and maximum use milliseconds correctly.
        private static void TelemetryDurationsAreSummarized()
        {
            var metrics = new ConnectedTelemetryMetrics(2);
            metrics.RecordDuration("decision_duration", "compare", TimeSpan.FromMilliseconds(10));
            metrics.RecordDuration("decision_duration", "compare", TimeSpan.FromMilliseconds(20));
            var duration = metrics.Snapshot().Durations.Single().Value;
            Equal(2L, duration.Count);
            Equal(30d, duration.TotalMilliseconds);
            Equal(10d, duration.MinimumMilliseconds);
            Equal(20d, duration.MaximumMilliseconds);
        }

        // Proves each sink failure is counted without escaping into the business caller.
        private static void TelemetryFailuresAreIsolated()
        {
            var inner = new ThrowingTelemetrySink();
            var sink = new SafeConnectedTelemetrySink(inner);
            sink.RecordFlagEvaluation(null);
            sink.RecordRuleComparison(null);
            sink.IncrementMetric("metric", "legacy", "ok");
            sink.RecordDuration("duration", "legacy", TimeSpan.Zero);
            Equal(4, inner.Calls);
            Equal(4L, sink.FailureCount);
        }

        // Proves the test sink retains sanitized records without raw cohort values.
        private static void InMemoryTelemetryCapturesRecords()
        {
            var sink = new InMemoryConnectedTelemetrySink(4);
            sink.RecordFlagEvaluation(
                new FlagEvaluationRecord(
                    Now,
                    "connected.enabled",
                    "false",
                    "PARENT_DISABLED",
                    "config-1",
                    "practice-secret",
                    Guid.NewGuid()
                )
            );
            Equal(1, sink.FlagEvaluations.Count);
            Equal(false, sink.FlagEvaluations[0].CohortKeyHash.Contains("practice-secret"));
        }

        // Proves all supported tier limits and NativeRules eligibility boundaries have matching
        // service outcomes for LOAN-001, LOAN-002, and LOAN-003.
        private static void CheckoutDecisionTable()
        {
            var today = new DateTime(2026, 9, 1);
            AssertDecision(
                Member("STANDARD", true, false, 0, 2, 7),
                today.AddDays(1),
                today,
                true,
                CheckoutDecisionReasons.Allowed,
                2,
                7
            );
            AssertDecision(
                Member("STANDARD", true, false, 1, 2, 7),
                today.AddDays(1),
                today,
                true,
                CheckoutDecisionReasons.Allowed,
                2,
                7
            );
            AssertDecision(
                Member("STANDARD", true, false, 2, 2, 7),
                today.AddDays(1),
                today,
                false,
                CheckoutDecisionReasons.CheckoutLimitReached,
                2,
                7
            );
            AssertDecision(
                Member("STANDARD", true, false, 3, 2, 7),
                today.AddDays(1),
                today,
                false,
                CheckoutDecisionReasons.CheckoutLimitReached,
                2,
                7
            );
            AssertDecision(
                Member("SUPPORTER", true, false, 4, 5, 14),
                today.AddDays(1),
                today,
                true,
                CheckoutDecisionReasons.Allowed,
                5,
                14
            );
            AssertDecision(
                Member("SUPPORTER", true, false, 5, 5, 14),
                today.AddDays(1),
                today,
                false,
                CheckoutDecisionReasons.CheckoutLimitReached,
                5,
                14
            );
            AssertDecision(
                Member("SUPPORTER", true, false, 6, 5, 14),
                today.AddDays(1),
                today,
                false,
                CheckoutDecisionReasons.CheckoutLimitReached,
                5,
                14
            );
            AssertDecision(
                Member("STAFF", true, false, 9, 10, 30),
                today.AddDays(1),
                today,
                true,
                CheckoutDecisionReasons.Allowed,
                10,
                30
            );
            AssertDecision(
                Member("STAFF", true, false, 10, 10, 30),
                today.AddDays(1),
                today,
                false,
                CheckoutDecisionReasons.CheckoutLimitReached,
                10,
                30
            );
            AssertDecision(
                Member("STAFF", true, false, 11, 10, 30),
                today.AddDays(1),
                today,
                false,
                CheckoutDecisionReasons.CheckoutLimitReached,
                10,
                30
            );
            AssertDecision(
                Member("STANDARD", false, false, 0, 2, 7),
                today.AddDays(1),
                today,
                false,
                CheckoutDecisionReasons.MemberInactive,
                2,
                7
            );
            AssertDecision(
                Member("STANDARD", true, true, 0, 2, 7),
                today.AddDays(1),
                today,
                false,
                CheckoutDecisionReasons.Overdue,
                2,
                7
            );
            AssertDecision(
                Member("UNKNOWN", true, false, 0, 0, 0),
                today.AddDays(1),
                today,
                false,
                CheckoutDecisionReasons.TierUnsupported,
                null,
                null
            );
        }

        // Proves multiple failures select the first reason in the approved presentation order.
        private static void CheckoutReasonOrderingIsStable()
        {
            var today = new DateTime(2026, 9, 1);
            AssertDecision(
                null,
                today.AddDays(-1),
                today,
                false,
                CheckoutDecisionReasons.MemberNotFound,
                null,
                null
            );
            AssertDecision(
                Member("UNKNOWN", false, true, 20, 0, 0),
                today.AddDays(-1),
                today,
                false,
                CheckoutDecisionReasons.MemberInactive,
                0,
                0
            );
            AssertDecision(
                Member("UNKNOWN", true, true, 20, 0, 0),
                today.AddDays(-1),
                today,
                false,
                CheckoutDecisionReasons.TierUnsupported,
                null,
                null
            );
            AssertDecision(
                Member("STANDARD", true, true, 2, 2, 7),
                today.AddDays(-1),
                today,
                false,
                CheckoutDecisionReasons.Overdue,
                2,
                7
            );
            AssertDecision(
                Member("STANDARD", true, false, 2, 2, 7),
                today.AddDays(-1),
                today,
                false,
                CheckoutDecisionReasons.CheckoutLimitReached,
                2,
                7
            );
        }

        // Proves today and the maximum tier date are allowed while dates on either side are denied.
        // The fixed clock keeps this test independent of workstation time and time zone.
        private static void CheckoutDueDateBoundaries()
        {
            IBusinessDateClock clock = new FixedBusinessDateClock(new DateTime(2026, 9, 1));
            AssertDueDateBoundariesForTier(Member("STANDARD", true, false, 0, 2, 7), clock.Today);
            AssertDueDateBoundariesForTier(Member("SUPPORTER", true, false, 0, 5, 14), clock.Today);
            AssertDueDateBoundariesForTier(Member("STAFF", true, false, 0, 10, 30), clock.Today);
        }

        // Checks both sides of the allowed date range for one tier and its maximum duration.
        private static void AssertDueDateBoundariesForTier(
            MemberEligibilityContext member,
            DateTime today
        )
        {
            AssertDecision(
                member,
                today.AddDays(-1),
                today,
                false,
                CheckoutDecisionReasons.DueDateInvalid,
                member.CheckoutLimit,
                member.MaximumLoanDays
            );
            AssertDecision(
                member,
                today,
                today,
                true,
                CheckoutDecisionReasons.Allowed,
                member.CheckoutLimit,
                member.MaximumLoanDays
            );
            AssertDecision(
                member,
                today.AddDays(member.MaximumLoanDays),
                today,
                true,
                CheckoutDecisionReasons.Allowed,
                member.CheckoutLimit,
                member.MaximumLoanDays
            );
            AssertDecision(
                member,
                today.AddDays(member.MaximumLoanDays + 1),
                today,
                false,
                CheckoutDecisionReasons.DueDateInvalid,
                member.CheckoutLimit,
                member.MaximumLoanDays
            );
        }

        // Proves the repository reads PostgreSQL tier functions plus open and overdue loan facts,
        // returns null for a missing member, and changes no workflow, retry, or audit table data.
        private static void MemberEligibilityQueryIsReadOnly()
        {
            var connectionString = TestConnectionString();
            var before = CaptureDatabaseState(connectionString);
            var repository = new Repository(connectionString);
            var today = DateTime.UtcNow.Date;
            Equal(today, ReadDatabaseBusinessDate(connectionString));

            var standard = repository.GetMemberEligibilityContext(1, today);
            Equal(1, standard.MemberId);
            Equal("STANDARD", standard.Tier);
            Equal(true, standard.Active);
            Equal(2, standard.CheckoutLimit);
            Equal(7, standard.MaximumLoanDays);

            var overdue = repository.GetMemberEligibilityContext(3, today);
            Equal(true, overdue.HasOverdueLoan);
            Equal(1, overdue.OpenLoans);
            Equal<MemberEligibilityContext>(
                null,
                repository.GetMemberEligibilityContext(999999, today)
            );
            Equal(before, CaptureDatabaseState(connectionString));
        }

        // Creates a synthetic member context for one service decision-table row.
        private static MemberEligibilityContext Member(
            string tier,
            bool active,
            bool overdue,
            int openLoans,
            int checkoutLimit,
            int maximumLoanDays
        )
        {
            return new MemberEligibilityContext(
                42,
                tier,
                active,
                openLoans,
                overdue,
                checkoutLimit,
                maximumLoanDays
            );
        }

        // Evaluates one row and checks its stable reason plus presentation facts.
        private static void AssertDecision(
            MemberEligibilityContext member,
            DateTime dueOn,
            DateTime businessDate,
            bool allowed,
            string reason,
            int? checkoutLimit,
            int? maximumLoanDays
        )
        {
            var decision = new CheckoutRuleEvaluator().Evaluate(member, dueOn, businessDate);
            Equal(allowed, decision.Allowed);
            Equal(reason, decision.Reason);
            Equal(checkoutLimit, decision.CheckoutLimit);
            Equal(maximumLoanDays, decision.MaximumLoanDays);
        }

        // Builds the local synthetic PostgreSQL connection without logging its password.
        private static string TestConnectionString()
        {
            return new NpgsqlConnectionStringBuilder
            {
                Host = "127.0.0.1",
                Port = 5432,
                Database = "tool_lending",
                Username = "tool_lending_app",
                Password =
                    Environment.GetEnvironmentVariable("TOOLLENDING_DB_PASSWORD")
                    ?? "ChangeMe-LocalOnly!",
                SearchPath = "tool_lending,public",
            }.ConnectionString;
        }

        // Returns a fingerprint of every row in workflow, retry, and audit tables so a repository
        // read can be checked for inserts, updates, and deletes.
        private static string CaptureDatabaseState(string connectionString)
        {
            const string sql =
                @"
                SELECT concat_ws('|',
                    (SELECT md5(coalesce(string_agg(to_jsonb(m)::text, '|' ORDER BY m.member_id), '')) FROM tool_lending.members m),
                    (SELECT md5(coalesce(string_agg(to_jsonb(t)::text, '|' ORDER BY t.tool_id), '')) FROM tool_lending.tools t),
                    (SELECT md5(coalesce(string_agg(to_jsonb(r)::text, '|' ORDER BY r.reservation_id), '')) FROM tool_lending.reservations r),
                    (SELECT md5(coalesce(string_agg(to_jsonb(l)::text, '|' ORDER BY l.loan_id), '')) FROM tool_lending.loans l),
                    (SELECT md5(coalesce(string_agg(to_jsonb(i)::text, '|' ORDER BY i.operation, i.idempotency_key), '')) FROM tool_lending.idempotency_records i),
                    (SELECT md5(coalesce(string_agg(to_jsonb(a)::text, '|' ORDER BY a.audit_id), '')) FROM tool_lending.audit_log a))";

            using (var connection = new NpgsqlConnection(connectionString))
            using (var command = new NpgsqlCommand(sql, connection))
            {
                connection.Open();
                return Convert.ToString(command.ExecuteScalar());
            }
        }

        // Reads PostgreSQL's current date so the test proves it matches the service's UTC date.
        // This guards the date boundary still used by the existing checkout routine.
        private static DateTime ReadDatabaseBusinessDate(string connectionString)
        {
            using (var connection = new NpgsqlConnection(connectionString))
            using (var command = new NpgsqlCommand("SELECT CURRENT_DATE", connection))
            {
                connection.Open();
                return Convert.ToDateTime(command.ExecuteScalar()).Date;
            }
        }

        // Proves the public capability shape preserves every server-selected parent/child state,
        // freshness field, correlation ID, and safe reason while emitting evaluation telemetry.
        private static void CapabilityApiMapsEveryEffectiveMode()
        {
            var cases = new[]
            {
                Capability(false, CheckoutRuleMode.Legacy, ConnectedFeatureReasons.ParentDisabled),
                Capability(true, CheckoutRuleMode.Legacy, ConnectedFeatureReasons.Legacy),
                Capability(true, CheckoutRuleMode.Compare, ConnectedFeatureReasons.Compare),
                Capability(true, CheckoutRuleMode.Service, ConnectedFeatureReasons.Service),
            };

            foreach (var expected in cases)
            {
                var telemetry = new InMemoryConnectedTelemetrySink();
                var service = new CapabilityService(new FixedEvaluator(expected), telemetry);
                var actual = service.Get("1.2.3", expected.CorrelationId);
                Equal(1, actual.SchemaVersion);
                Equal(expected.ConfigurationVersion, actual.ConfigurationVersion);
                Equal(expected.EvaluatedAt, actual.EvaluatedAt);
                Equal(expected.ExpiresAt, actual.ExpiresAt);
                Equal(expected.ConnectedEnabled, actual.ConnectedEnabled);
                Equal(
                    expected.CheckoutRuleMode.ToString().ToLowerInvariant(),
                    actual.CheckoutRuleMode
                );
                Equal(expected.Reason, actual.Reason);
                Equal(expected.CorrelationId, actual.CorrelationId);
                Equal(2, telemetry.FlagEvaluations.Count);
                Equal(expected.Reason, telemetry.FlagEvaluations[0].Reason);
                Equal("connected.checkout.rule-mode", telemetry.FlagEvaluations[1].FlagKey);
                Equal(false, telemetry.FlagEvaluations[0].CohortKeyHash.Contains("practice"));
            }
        }

        // Proves missing, malformed, and overlong request versions can only narrow a server Service
        // decision to Legacy. The original server configuration and freshness evidence is retained.
        private static void CapabilityApiRejectsUnsafeVersions()
        {
            var expected = Capability(
                true,
                CheckoutRuleMode.Service,
                ConnectedFeatureReasons.Service
            );
            var service = new CapabilityService(
                new FixedEvaluator(expected),
                new InMemoryConnectedTelemetrySink()
            );

            var missing = service.Get(null, expected.CorrelationId);
            Equal(false, missing.ConnectedEnabled);
            Equal("legacy", missing.CheckoutRuleMode);
            Equal(CapabilityApiReasons.ClientVersionMissing, missing.Reason);
            Equal(expected.ConfigurationVersion, missing.ConfigurationVersion);

            foreach (var value in new[] { "not-semver", new string('9', 65) })
            {
                var invalid = service.Get(value, expected.CorrelationId);
                Equal(false, invalid.ConnectedEnabled);
                Equal("legacy", invalid.CheckoutRuleMode);
                Equal(CapabilityApiReasons.ClientVersionInvalid, invalid.Reason);
            }
        }

        // Proves Service mode returns the service rule result and records no comparison.
        private static void DecisionUsesServiceResult()
        {
            var telemetry = new InMemoryConnectedTelemetrySink();
            var service = DecisionService(CheckoutRuleMode.Service, telemetry);
            var response = service.Decide(DecisionRequest(), Guid.NewGuid());

            Equal("service", response.EffectiveMode);
            Equal(true, response.Allowed);
            Equal(CheckoutDecisionReasons.Allowed, response.Reason);
            Equal(0, telemetry.RuleComparisons.Count);
            Equal(2, telemetry.FlagEvaluations.Count);
        }

        // Proves the service-mode contract returns every stable allow/deny reason without a Legacy
        // observation or comparison side effect.
        private static void DecisionMapsEveryServiceResult()
        {
            var today = new DateTime(2026, 9, 2);
            AssertServiceDecision(
                null,
                today.AddDays(1),
                false,
                CheckoutDecisionReasons.MemberNotFound
            );
            AssertServiceDecision(
                Member("STANDARD", false, false, 0, 2, 7),
                today.AddDays(1),
                false,
                CheckoutDecisionReasons.MemberInactive
            );
            AssertServiceDecision(
                Member("UNKNOWN", true, false, 0, 0, 0),
                today.AddDays(1),
                false,
                CheckoutDecisionReasons.TierUnsupported
            );
            AssertServiceDecision(
                Member("STANDARD", true, true, 0, 2, 7),
                today.AddDays(1),
                false,
                CheckoutDecisionReasons.Overdue
            );
            AssertServiceDecision(
                Member("STANDARD", true, false, 2, 2, 7),
                today.AddDays(1),
                false,
                CheckoutDecisionReasons.CheckoutLimitReached
            );
            AssertServiceDecision(
                Member("STANDARD", true, false, 0, 2, 7),
                today.AddDays(8),
                false,
                CheckoutDecisionReasons.DueDateInvalid
            );
            AssertServiceDecision(
                Member("STANDARD", true, false, 0, 2, 7),
                today.AddDays(1),
                true,
                CheckoutDecisionReasons.Allowed
            );
        }

        // Runs one service-mode decision row and verifies no comparison evidence was produced.
        private static void AssertServiceDecision(
            MemberEligibilityContext member,
            DateTime dueOn,
            bool allowed,
            string reason
        )
        {
            var telemetry = new InMemoryConnectedTelemetrySink();
            var service = new CheckoutDecisionService(
                new FixedEvaluator(Capability(true, CheckoutRuleMode.Service, "SERVICE")),
                (memberId, businessDate) => member,
                new CheckoutRuleEvaluator(),
                new FixedBusinessDateClock(new DateTime(2026, 9, 2)),
                telemetry
            );
            var request = DecisionRequest();
            request.DueOn = dueOn;

            var response = service.Decide(request, Guid.NewGuid());

            Equal("service", response.EffectiveMode);
            Equal(allowed, response.Allowed);
            Equal(reason, response.Reason);
            Equal(0, telemetry.RuleComparisons.Count);
        }

        // Proves Compare mode reports the Legacy result while recording the service mismatch.
        private static void DecisionPreservesLegacyResult()
        {
            var telemetry = new InMemoryConnectedTelemetrySink();
            var service = DecisionService(CheckoutRuleMode.Compare, telemetry);
            var request = DecisionRequest();
            request.LegacyObservation = new LegacyCheckoutObservation
            {
                ContractVersion = 1,
                Allowed = false,
                Reason = "OVERDUE",
            };
            var response = service.Decide(request, Guid.NewGuid());

            Equal("compare", response.EffectiveMode);
            Equal(false, response.Allowed);
            Equal("OVERDUE", response.Reason);
            Equal(1, telemetry.RuleComparisons.Count);
            Equal(false, telemetry.RuleComparisons[0].Match);
            Equal("completed", telemetry.RuleComparisons[0].Outcome);
        }

        // Proves equal Legacy and service outcomes are recorded as a normalized match.
        private static void DecisionRecordsCompareMatch()
        {
            var telemetry = new InMemoryConnectedTelemetrySink();
            var service = DecisionService(CheckoutRuleMode.Compare, telemetry);
            var request = DecisionRequest();
            request.LegacyObservation = new LegacyCheckoutObservation
            {
                ContractVersion = 1,
                Allowed = true,
                Reason = "ALLOWED",
            };

            service.Decide(request, Guid.NewGuid());

            Equal(true, telemetry.RuleComparisons[0].Match);
        }

        // Proves changed server configuration cannot be overridden by client routing evidence.
        private static void DecisionRejectsStaleCapability()
        {
            var service = DecisionService(
                CheckoutRuleMode.Service,
                new InMemoryConnectedTelemetrySink()
            );
            var request = DecisionRequest();
            request.CapabilityConfigurationVersion = "stale-client-version";

            try
            {
                service.Decide(request, Guid.NewGuid());
                throw new InvalidOperationException("expected stale capability rejection");
            }
            catch (CapabilityStaleException) { }
        }

        // Proves Compare mode cannot silently produce comparison evidence without NativeRules data.
        private static void DecisionRequiresCompareObservation()
        {
            var service = DecisionService(
                CheckoutRuleMode.Compare,
                new InMemoryConnectedTelemetrySink()
            );
            try
            {
                service.Decide(DecisionRequest(), Guid.NewGuid());
                throw new InvalidOperationException("expected missing observation rejection");
            }
            catch (ArgumentException) { }
        }

        // Proves a failed compare read emits safe incomplete evidence and does not change outcome.
        private static void DecisionRecordsCompareReadFailure()
        {
            var telemetry = new InMemoryConnectedTelemetrySink();
            var request = DecisionRequest();
            request.LegacyObservation = new LegacyCheckoutObservation
            {
                ContractVersion = 1,
                Allowed = true,
                Reason = "ALLOWED",
            };
            var service = new CheckoutDecisionService(
                new FixedEvaluator(Capability(true, CheckoutRuleMode.Compare, "COMPARE")),
                (memberId, businessDate) => throw new NpgsqlException("synthetic read failure"),
                new CheckoutRuleEvaluator(),
                new FixedBusinessDateClock(new DateTime(2026, 9, 2)),
                telemetry
            );

            try
            {
                service.Decide(request, Guid.NewGuid());
                throw new InvalidOperationException("expected read failure");
            }
            catch (NpgsqlException) { }

            Equal(1, telemetry.RuleComparisons.Count);
            Equal("service_error", telemetry.RuleComparisons[0].Outcome);
        }

        // Proves a completed service decision leaves workflow, retry, and audit tables unchanged.
        private static void DecisionRepositoryReadIsSideEffectFree()
        {
            var connectionString = TestConnectionString();
            var businessDate = ReadDatabaseBusinessDate(connectionString);
            var before = CaptureDatabaseState(connectionString);
            var service = new CheckoutDecisionService(
                new FixedEvaluator(Capability(true, CheckoutRuleMode.Service, "SERVICE")),
                new Repository(connectionString),
                new CheckoutRuleEvaluator(),
                new FixedBusinessDateClock(businessDate),
                new InMemoryConnectedTelemetrySink()
            );
            var request = DecisionRequest();
            request.MemberId = 1;
            request.DueOn = businessDate.AddDays(1);

            service.Decide(request, Guid.NewGuid());

            Equal(before, CaptureDatabaseState(connectionString));
        }

        // Runs disabled, Legacy, compare, and service decisions against the same synthetic database
        // fixture. Decision reads may emit telemetry, but no mode may write workflow, retry, or
        // business-audit state; PostgreSQL remains the only checkout writer.
        private static void MigrationModeMatrixPreservesDatabaseState()
        {
            var connectionString = TestConnectionString();
            var businessDate = ReadDatabaseBusinessDate(connectionString);
            var before = CaptureDatabaseState(connectionString);

            foreach (
                var capability in new[]
                {
                    Capability(false, CheckoutRuleMode.Legacy, "PARENT_DISABLED"),
                    Capability(true, CheckoutRuleMode.Legacy, "LEGACY"),
                    Capability(true, CheckoutRuleMode.Compare, "COMPARE"),
                    Capability(true, CheckoutRuleMode.Service, "SERVICE"),
                }
            )
            {
                var telemetry = new InMemoryConnectedTelemetrySink();
                var service = new CheckoutDecisionService(
                    new FixedEvaluator(capability),
                    new Repository(connectionString),
                    new CheckoutRuleEvaluator(),
                    new FixedBusinessDateClock(businessDate),
                    telemetry
                );
                var request = DecisionRequest();
                request.MemberId = 1;
                request.DueOn = businessDate.AddDays(1);
                if (capability.CheckoutRuleMode == CheckoutRuleMode.Compare)
                {
                    request.LegacyObservation = new LegacyCheckoutObservation
                    {
                        ContractVersion = 1,
                        Allowed = true,
                        Reason = CheckoutDecisionReasons.Allowed,
                    };
                }

                if (
                    !capability.ConnectedEnabled
                    || capability.CheckoutRuleMode == CheckoutRuleMode.Legacy
                )
                {
                    try
                    {
                        service.Decide(request, Guid.NewGuid());
                        throw new InvalidOperationException("expected Legacy routing rejection");
                    }
                    catch (CapabilityStaleException) { }
                }
                else
                {
                    var response = service.Decide(request, Guid.NewGuid());
                    Equal(
                        capability.CheckoutRuleMode.ToString().ToLowerInvariant(),
                        response.EffectiveMode
                    );
                    Equal(true, response.Allowed);
                }

                Equal(2, telemetry.FlagEvaluations.Count);
                Equal(
                    capability.CheckoutRuleMode == CheckoutRuleMode.Compare ? 1 : 0,
                    telemetry.RuleComparisons.Count
                );
                Equal(before, CaptureDatabaseState(connectionString));
            }
        }

        // Proves the production failure-isolating wrapper preserves a completed decision.
        private static void DecisionIsolatesTelemetryFailure()
        {
            var throwing = new ThrowingTelemetrySink();
            var safe = new SafeConnectedTelemetrySink(throwing);
            var response = DecisionService(CheckoutRuleMode.Service, safe)
                .Decide(DecisionRequest(), Guid.NewGuid());

            Equal(true, response.Allowed);
            Equal(true, safe.FailureCount > 0);
        }

        // Builds one valid synthetic request matching the fixed capability configuration.
        private static CheckoutDecisionRequest DecisionRequest()
        {
            return new CheckoutDecisionRequest
            {
                MemberId = 42,
                DueOn = new DateTime(2026, 9, 5),
                ClientVersion = "1.2.3",
                CapabilityConfigurationVersion = "test-configuration-1",
            };
        }

        // Builds a decision service with an in-memory member read and deterministic business date.
        private static CheckoutDecisionService DecisionService(
            CheckoutRuleMode mode,
            IConnectedTelemetrySink telemetry
        )
        {
            return new CheckoutDecisionService(
                new FixedEvaluator(Capability(true, mode, mode.ToString().ToUpperInvariant())),
                (memberId, businessDate) => Member("STANDARD", true, false, 0, 2, 7),
                new CheckoutRuleEvaluator(),
                new FixedBusinessDateClock(new DateTime(2026, 9, 2)),
                telemetry
            );
        }

        // Creates one deterministic server decision for capability contract mapping tests.
        private static ConnectedCapability Capability(
            bool enabled,
            CheckoutRuleMode mode,
            string reason
        )
        {
            var evaluatedAt = new DateTimeOffset(2026, 9, 2, 12, 0, 0, TimeSpan.Zero);
            return new ConnectedCapability
            {
                SchemaVersion = 1,
                ConfigurationVersion = "test-configuration-1",
                EvaluatedAt = evaluatedAt,
                ExpiresAt = evaluatedAt.AddSeconds(30),
                ConnectedEnabled = enabled,
                CheckoutRuleMode = mode,
                Reason = reason,
                CorrelationId = Guid.NewGuid(),
            };
        }

        // Fails a telemetry schema check when any named JSON field is absent.
        private static void HasFields(JObject value, params string[] names)
        {
            foreach (var name in names)
            {
                if (value[name] == null)
                    throw new InvalidOperationException("missing telemetry field " + name);
            }
        }

        // Evaluates one source with the standard synthetic context and a unique correlation ID.
        private static ConnectedCapability Evaluate(IConnectedFeatureSnapshotSource source)
        {
            return Evaluator(source).Evaluate(Context(), Guid.NewGuid());
        }

        // Builds the evaluator with fixed test lifetimes and deterministic UTC time.
        private static CachedConnectedFeatureEvaluator Evaluator(
            IConnectedFeatureSnapshotSource source
        )
        {
            return new CachedConnectedFeatureEvaluator(
                source,
                new FixedClock(Now),
                TimeSpan.FromSeconds(30),
                TimeSpan.FromMinutes(5),
                TimeSpan.FromSeconds(30)
            );
        }

        // Supplies synthetic rollout attributes shared by the focused cases.
        private static FeatureEvaluationContext Context()
        {
            return new FeatureEvaluationContext
            {
                Environment = "local",
                PracticeKey = "synthetic-practice",
                DeploymentRing = "default",
                ClientVersion = "1.2.3",
            };
        }

        // Builds a current synthetic snapshot with unrestricted targets.
        private static ConnectedFeatureSnapshot Snapshot(bool enabled, string mode)
        {
            return new ConnectedFeatureSnapshot(
                1,
                "test-1",
                Now.AddMinutes(-1),
                Now.AddMinutes(10),
                enabled,
                Targets(),
                mode,
                Targets()
            );
        }

        // Builds targets that optionally restrict environment and leave other dimensions open.
        private static ConnectedFeatureTargets Targets(IEnumerable<string> environments = null)
        {
            return new ConnectedFeatureTargets(environments, null, null, null);
        }

        // Runs one case, prints a stable PASS or FAIL line, and accumulates failures.
        private static void Run(string name, Action test)
        {
            try
            {
                test();
                Console.WriteLine("PASS: " + name);
            }
            catch (Exception exception)
            {
                failures++;
                Console.WriteLine("FAIL: " + name + " - " + exception.Message);
            }
        }

        // Provides the executable's minimal dependency-free equality assertion.
        private static void Equal<T>(T expected, T actual)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException("expected " + expected + ", got " + actual);
        }

        // Holds UTC time constant for cases that do not exercise refresh behavior.
        private sealed class FixedClock : IConnectedFeatureClock
        {
            // Initializes the immutable test time.
            public FixedClock(DateTimeOffset now)
            {
                UtcNow = now;
            }

            public DateTimeOffset UtcNow { get; private set; }
        }

        // Allows a case to cross cache and expiry boundaries without waiting in real time.
        private sealed class MutableClock : IConnectedFeatureClock
        {
            // Initializes the starting test time.
            public MutableClock(DateTimeOffset now)
            {
                UtcNow = now;
            }

            public DateTimeOffset UtcNow { get; set; }
        }

        // Supplies a fixed UTC business date to deterministic checkout boundary tests.
        private sealed class FixedBusinessDateClock : IBusinessDateClock
        {
            // Stores the supplied date without its time-of-day component.
            public FixedBusinessDateClock(DateTime today)
            {
                Today = today.Date;
            }

            public DateTime Today { get; }
        }

        // Returns one service-authored capability and captures the bounded client context supplied
        // by the API adapter. It performs no feature-source or network work.
        private sealed class FixedEvaluator : IConnectedFeatureEvaluator
        {
            private readonly ConnectedCapability capability;

            // Stores the immutable decision returned by each focused API adapter test.
            public FixedEvaluator(ConnectedCapability capability)
            {
                this.capability = capability;
            }

            // Returns a copy with the request correlation ID, matching production evaluator output.
            public ConnectedCapability Evaluate(
                FeatureEvaluationContext context,
                Guid correlationId
            )
            {
                return new ConnectedCapability
                {
                    SchemaVersion = capability.SchemaVersion,
                    ConfigurationVersion = capability.ConfigurationVersion,
                    EvaluatedAt = capability.EvaluatedAt,
                    ExpiresAt = capability.ExpiresAt,
                    ConnectedEnabled = capability.ConnectedEnabled,
                    CheckoutRuleMode = capability.CheckoutRuleMode,
                    Reason = capability.Reason,
                    CorrelationId = correlationId,
                };
            }
        }

        // Returns the same load result and isolates evaluator behavior from file access.
        private sealed class FixedSource : IConnectedFeatureSnapshotSource
        {
            private readonly FeatureSnapshotLoadResult result;

            // Captures the result returned by every load.
            public FixedSource(FeatureSnapshotLoadResult result)
            {
                this.result = result;
            }

            // Returns the configured result; the supplied UTC instant is intentionally irrelevant.
            public FeatureSnapshotLoadResult Load(DateTimeOffset now)
            {
                return result;
            }
        }

        // Simulates provider failures while exposing load count for cache assertions.
        private sealed class ThrowingSource : IConnectedFeatureSnapshotSource
        {
            private readonly Exception exception;

            // Uses the supplied failure or a synthetic I/O failure by default.
            public ThrowingSource(Exception exception = null)
            {
                this.exception = exception ?? new IOException("synthetic source failure");
            }

            public int Calls { get; private set; }

            // Counts the attempted load and throws the configured provider failure.
            public FeatureSnapshotLoadResult Load(DateTimeOffset now)
            {
                Calls++;
                throw exception;
            }
        }

        // Returns successive results to model an external snapshot replacement.
        private sealed class SequenceSource : IConnectedFeatureSnapshotSource
        {
            private readonly FeatureSnapshotLoadResult[] results;

            // Captures the ordered results used by later loads.
            public SequenceSource(params FeatureSnapshotLoadResult[] results)
            {
                this.results = results;
            }

            public int Calls { get; private set; }

            // Advances through results and repeats the final result after the sequence is exhausted.
            public FeatureSnapshotLoadResult Load(DateTimeOffset now)
            {
                var index = Math.Min(Calls, results.Length - 1);
                Calls++;
                return results[index];
            }
        }

        // Simulates failure from every telemetry operation and counts each attempt.
        private sealed class ThrowingTelemetrySink : IConnectedTelemetrySink
        {
            public int Calls { get; private set; }

            // Fails a flag-record attempt through the common throwing path.
            public void RecordFlagEvaluation(FlagEvaluationRecord record)
            {
                Throw();
            }

            // Fails a comparison-record attempt through the common throwing path.
            public void RecordRuleComparison(RuleComparisonRecord record)
            {
                Throw();
            }

            // Fails a counter attempt through the common throwing path.
            public void IncrementMetric(string name, string mode, string outcome)
            {
                Throw();
            }

            // Fails a duration attempt through the common throwing path.
            public void RecordDuration(string name, string mode, TimeSpan duration)
            {
                Throw();
            }

            // Counts and throws one synthetic telemetry failure.
            private void Throw()
            {
                Calls++;
                throw new IOException("synthetic telemetry failure");
            }
        }
    }
}
