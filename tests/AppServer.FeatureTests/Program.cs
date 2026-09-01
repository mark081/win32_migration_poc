using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
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
