// PURPOSE
// -------
// This file provides safe diagnostics for the Connected migration.
// It gives later workflow code two ways to observe a rollout:
//
//     1. Record why feature evaluation chose Legacy, Compare, or Service.
//     2. In Compare mode, record whether Legacy and Service rule results agreed.
//
// Records contain bounded or hashed values rather than raw practice/member data. Metrics are
// bounded in memory, and SafeConnectedTelemetrySink prevents diagnostic failures from affecting
// a checkout. Telemetry is additive evidence only; it is not an audit store, business database,
// rule authority, or second writer.
//
// This file is not wired into a production workflow yet. Later tasks connect it at the feature
// evaluation and rule-comparison seams. At this foundation stage it is used by focused tests only.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Newtonsoft.Json;

namespace ToolLending.AppServer
{
    // Receives non-authoritative Connected feature and comparison diagnostics.
    //
    // Emit hashes or bounded tokens, never raw cohort attributes, request bodies, credentials, or
    // identity data. A sink is outside the business transaction: failure must not fail, retry, or
    // alter an authoritative operation. Production callers use SafeConnectedTelemetrySink.
    internal interface IConnectedTelemetrySink
    {
        // Records one parent or child flag decision using sanitized fields.
        void RecordFlagEvaluation(FlagEvaluationRecord record);

        // Records one non-authoritative Legacy-versus-Service comparison.
        void RecordRuleComparison(RuleComparisonRecord record);

        // Increments a bounded process-local counter identified by safe low-cardinality tokens.
        void IncrementMetric(string name, string mode, string outcome);

        // Adds an operation duration; built-in sinks normalize negative values to zero.
        void RecordDuration(string name, string mode, TimeSpan duration);
    }

    // Stores the safe, observable fields for one feature decision.
    internal sealed class FlagEvaluationRecord
    {
        // Sanitizes tokens and hashes the cohort key before the record can reach a sink.
        public FlagEvaluationRecord(
            DateTimeOffset timestamp,
            string flagKey,
            string effectiveValue,
            string reason,
            string configurationVersion,
            string cohortKey,
            Guid correlationId
        )
        {
            Timestamp = timestamp;
            FlagKey = ConnectedTelemetryRedaction.SafeToken(flagKey);
            EffectiveValue = ConnectedTelemetryRedaction.SafeToken(effectiveValue);
            Reason = ConnectedTelemetryRedaction.SafeToken(reason);
            ConfigurationVersion = ConnectedTelemetryRedaction.SafeToken(configurationVersion);
            CohortKeyHash = ConnectedTelemetryRedaction.HashOpaque(cohortKey);
            CorrelationId = correlationId;
        }

        public DateTimeOffset Timestamp { get; private set; }
        public string FlagKey { get; private set; }
        public string EffectiveValue { get; private set; }
        public string Reason { get; private set; }
        public string ConfigurationVersion { get; private set; }
        public string CohortKeyHash { get; private set; }
        public Guid CorrelationId { get; private set; }
    }

    // Stores normalized Legacy and Service decisions without carrying raw business input.
    internal sealed class RuleComparisonRecord
    {
        // Hashes cohort and input identity, bounds tokens, and clamps negative duration to zero.
        public RuleComparisonRecord(
            DateTimeOffset timestamp,
            Guid correlationId,
            string configurationVersion,
            string cohortKey,
            string inputIdentity,
            int legacyContractVersion,
            bool legacyAllowed,
            string legacyReason,
            int serviceContractVersion,
            bool serviceAllowed,
            string serviceReason,
            bool match,
            TimeSpan duration,
            string outcome
        )
        {
            Timestamp = timestamp;
            CorrelationId = correlationId;
            ConfigurationVersion = ConnectedTelemetryRedaction.SafeToken(configurationVersion);
            CohortKeyHash = ConnectedTelemetryRedaction.HashOpaque(cohortKey);
            InputIdentityHash = ConnectedTelemetryRedaction.HashOpaque(inputIdentity);
            LegacyContractVersion = legacyContractVersion;
            LegacyAllowed = legacyAllowed;
            LegacyReason = ConnectedTelemetryRedaction.SafeToken(legacyReason);
            ServiceContractVersion = serviceContractVersion;
            ServiceAllowed = serviceAllowed;
            ServiceReason = ConnectedTelemetryRedaction.SafeToken(serviceReason);
            Match = match;
            Duration = duration < TimeSpan.Zero ? TimeSpan.Zero : duration;
            Outcome = ConnectedTelemetryRedaction.SafeToken(outcome);
        }

        public DateTimeOffset Timestamp { get; private set; }
        public Guid CorrelationId { get; private set; }
        public string ConfigurationVersion { get; private set; }
        public string CohortKeyHash { get; private set; }
        public string InputIdentityHash { get; private set; }
        public int LegacyContractVersion { get; private set; }
        public bool LegacyAllowed { get; private set; }
        public string LegacyReason { get; private set; }
        public int ServiceContractVersion { get; private set; }
        public bool ServiceAllowed { get; private set; }
        public string ServiceReason { get; private set; }
        public bool Match { get; private set; }
        public TimeSpan Duration { get; private set; }
        public string Outcome { get; private set; }
    }

    // Converts sensitive or unbounded inputs into diagnostic hashes and safe tokens.
    //
    // Hashes support correlation only; they are not authentication or anonymization. Never emit
    // raw practice, member, authorization, or request values alongside them.
    internal static class ConnectedTelemetryRedaction
    {
        private const int MaximumTokenLength = 80;

        // Returns a lowercase SHA-256 correlation hash; missing values hash as an empty string.
        public static string HashOpaque(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;
            using (var hash = SHA256.Create())
            {
                return BitConverter
                    .ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(value)))
                    .Replace("-", string.Empty)
                    .ToLowerInvariant();
            }
        }

        // Replaces unsupported characters and caps a low-cardinality token at 80 characters.
        public static string SafeToken(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;
            return new string(
                value
                    .Take(MaximumTokenLength)
                    .Select(character =>
                        char.IsLetterOrDigit(character)
                        || character == '.'
                        || character == '_'
                        || character == '-'
                            ? character
                            : '_'
                    )
                    .ToArray()
            );
        }
    }

    // Holds count, total, minimum, and maximum duration values in milliseconds.
    internal sealed class ConnectedDurationSummary
    {
        public long Count { get; internal set; }
        public double TotalMilliseconds { get; internal set; }
        public double MinimumMilliseconds { get; internal set; }
        public double MaximumMilliseconds { get; internal set; }
    }

    // Exposes a detached read-only view of metrics at one point in process time.
    internal sealed class ConnectedTelemetryMetricsSnapshot
    {
        // Captures copied counter and duration dictionaries plus the number of rejected series.
        public ConnectedTelemetryMetricsSnapshot(
            IDictionary<string, long> counters,
            IDictionary<string, ConnectedDurationSummary> durations,
            long droppedSeries
        )
        {
            Counters = counters;
            Durations = durations;
            DroppedSeries = droppedSeries;
        }

        public IDictionary<string, long> Counters { get; private set; }
        public IDictionary<string, ConnectedDurationSummary> Durations { get; private set; }
        public long DroppedSeries { get; private set; }
    }

    // Maintains bounded, process-local diagnostic aggregates with no workflow authority.
    //
    // Excess series are counted as dropped rather than growing memory without limit. Durations are
    // non-negative milliseconds. Aggregates reset with the process and are not audit records.
    internal sealed class ConnectedTelemetryMetrics
    {
        private readonly object sync = new object();
        private readonly int maximumSeries;
        private readonly Dictionary<string, long> counters = new Dictionary<string, long>();
        private readonly Dictionary<string, ConnectedDurationSummary> durations =
            new Dictionary<string, ConnectedDurationSummary>();
        private long droppedSeries;

        // Creates a bounded aggregator; a non-positive limit is safely normalized to one series.
        public ConnectedTelemetryMetrics(int maximumSeries = 128)
        {
            this.maximumSeries = Math.Max(1, maximumSeries);
        }

        // Increments an existing series or admits a new one while capacity remains.
        public void Increment(string name, string mode, string outcome)
        {
            var key = Key(name, mode, outcome);
            lock (sync)
            {
                long count;
                if (!counters.TryGetValue(key, out count))
                {
                    if (counters.Count + durations.Count >= maximumSeries)
                    {
                        droppedSeries++;
                        return;
                    }
                    count = 0;
                }
                counters[key] = count + 1;
            }
        }

        // Adds a duration summary, clamping negative milliseconds to zero.
        public void RecordDuration(string name, string mode, TimeSpan duration)
        {
            var key = Key(name, mode, null);
            var milliseconds = Math.Max(0, duration.TotalMilliseconds);
            lock (sync)
            {
                ConnectedDurationSummary summary;
                if (!durations.TryGetValue(key, out summary))
                {
                    if (counters.Count + durations.Count >= maximumSeries)
                    {
                        droppedSeries++;
                        return;
                    }
                    summary = new ConnectedDurationSummary { MinimumMilliseconds = milliseconds };
                    durations.Add(key, summary);
                }
                summary.Count++;
                summary.TotalMilliseconds += milliseconds;
                summary.MinimumMilliseconds = Math.Min(summary.MinimumMilliseconds, milliseconds);
                summary.MaximumMilliseconds = Math.Max(summary.MaximumMilliseconds, milliseconds);
            }
        }

        // Copies all aggregates under the lock so callers cannot mutate live process state.
        public ConnectedTelemetryMetricsSnapshot Snapshot()
        {
            lock (sync)
            {
                return new ConnectedTelemetryMetricsSnapshot(
                    new Dictionary<string, long>(counters),
                    durations.ToDictionary(
                        pair => pair.Key,
                        pair => new ConnectedDurationSummary
                        {
                            Count = pair.Value.Count,
                            TotalMilliseconds = pair.Value.TotalMilliseconds,
                            MinimumMilliseconds = pair.Value.MinimumMilliseconds,
                            MaximumMilliseconds = pair.Value.MaximumMilliseconds,
                        }
                    ),
                    droppedSeries
                );
            }
        }

        // Forms a stable low-cardinality series key from individually sanitized tokens.
        private static string Key(string name, string mode, string outcome)
        {
            return string.Join(
                "|",
                new[]
                {
                    ConnectedTelemetryRedaction.SafeToken(name),
                    ConnectedTelemetryRedaction.SafeToken(mode),
                    ConnectedTelemetryRedaction.SafeToken(outcome),
                }
            );
        }
    }

    // Writes one sanitized JSON diagnostic record per line.
    //
    // The caller owns the writer. This sink does not flush, dispose, retry, or join a database
    // transaction. Wrap it in SafeConnectedTelemetrySink to isolate failures.
    internal sealed class JsonDiagnosticConnectedTelemetrySink : IConnectedTelemetrySink
    {
        private readonly object sync = new object();
        private readonly TextWriter writer;
        private readonly ConnectedTelemetryMetrics metrics;

        // Uses the caller-owned writer and a bounded in-memory metric aggregator.
        public JsonDiagnosticConnectedTelemetrySink(TextWriter writer, int maximumSeries = 128)
        {
            this.writer = writer ?? throw new ArgumentNullException(nameof(writer));
            metrics = new ConnectedTelemetryMetrics(maximumSeries);
        }

        public ConnectedTelemetryMetricsSnapshot Metrics
        {
            get { return metrics.Snapshot(); }
        }

        // Serializes a non-null flag record as one JSON line; null produces no output.
        public void RecordFlagEvaluation(FlagEvaluationRecord record)
        {
            if (record == null)
                return;
            Write(
                new
                {
                    eventType = "connected.flag_evaluation",
                    timestamp = record.Timestamp,
                    flagKey = record.FlagKey,
                    effectiveValue = record.EffectiveValue,
                    reason = record.Reason,
                    configurationVersion = record.ConfigurationVersion,
                    cohortKey = record.CohortKeyHash,
                    correlationId = record.CorrelationId,
                }
            );
        }

        // Serializes a non-null comparison as one JSON line; null produces no output.
        public void RecordRuleComparison(RuleComparisonRecord record)
        {
            if (record == null)
                return;
            Write(
                new
                {
                    eventType = "connected.rule_comparison",
                    timestamp = record.Timestamp,
                    correlationId = record.CorrelationId,
                    configurationVersion = record.ConfigurationVersion,
                    cohortKey = record.CohortKeyHash,
                    inputIdentity = record.InputIdentityHash,
                    legacyContractVersion = record.LegacyContractVersion,
                    legacyAllowed = record.LegacyAllowed,
                    legacyReason = record.LegacyReason,
                    serviceContractVersion = record.ServiceContractVersion,
                    serviceAllowed = record.ServiceAllowed,
                    serviceReason = record.ServiceReason,
                    match = record.Match,
                    durationMs = record.Duration.TotalMilliseconds,
                    outcome = record.Outcome,
                }
            );
        }

        // Adds a caller-defined bounded counter without writing a separate JSON record.
        public void IncrementMetric(string name, string mode, string outcome)
        {
            metrics.Increment(name, mode, outcome);
        }

        // Adds a caller-defined bounded duration without writing a separate JSON record.
        public void RecordDuration(string name, string mode, TimeSpan duration)
        {
            metrics.RecordDuration(name, mode, duration);
        }

        // Serializes and writes under a lock so concurrent records cannot interleave.
        private void Write(object value)
        {
            var json = JsonConvert.SerializeObject(value, Formatting.None);
            lock (sync)
            {
                writer.WriteLine(json);
            }
        }
    }

    // Makes each telemetry call once and suppresses failures from the business path.
    //
    // Failures increment FailureCount. There is deliberately no retry because additive telemetry
    // must not duplicate or delay an authoritative operation.
    internal sealed class SafeConnectedTelemetrySink : IConnectedTelemetrySink
    {
        private readonly IConnectedTelemetrySink inner;
        private long failureCount;

        // Wraps the required sink; null is rejected because silent telemetry loss is ambiguous.
        public SafeConnectedTelemetrySink(IConnectedTelemetrySink inner)
        {
            this.inner = inner ?? throw new ArgumentNullException(nameof(inner));
        }

        public long FailureCount
        {
            get { return Interlocked.Read(ref failureCount); }
        }

        // Attempts one flag write and converts any exception into FailureCount.
        public void RecordFlagEvaluation(FlagEvaluationRecord record)
        {
            Try(() => inner.RecordFlagEvaluation(record));
        }

        // Attempts one comparison write and converts any exception into FailureCount.
        public void RecordRuleComparison(RuleComparisonRecord record)
        {
            Try(() => inner.RecordRuleComparison(record));
        }

        // Attempts one metric update and converts any exception into FailureCount.
        public void IncrementMetric(string name, string mode, string outcome)
        {
            Try(() => inner.IncrementMetric(name, mode, outcome));
        }

        // Attempts one duration update and converts any exception into FailureCount.
        public void RecordDuration(string name, string mode, TimeSpan duration)
        {
            Try(() => inner.RecordDuration(name, mode, duration));
        }

        // Isolates all sink exceptions; business code never observes or retries them.
        private void Try(Action action)
        {
            try
            {
                action();
            }
            catch (Exception)
            {
                Interlocked.Increment(ref failureCount);
            }
        }
    }

    // Captures sanitized records and bounded metrics for tests and local diagnostics.
    // This process-local sink is neither durable telemetry nor a business audit store.
    internal sealed class InMemoryConnectedTelemetrySink : IConnectedTelemetrySink
    {
        private readonly object sync = new object();
        private readonly List<FlagEvaluationRecord> flagEvaluations =
            new List<FlagEvaluationRecord>();
        private readonly List<RuleComparisonRecord> ruleComparisons =
            new List<RuleComparisonRecord>();
        private readonly ConnectedTelemetryMetrics metrics;

        // Creates thread-safe record collections and a bounded metrics aggregator.
        public InMemoryConnectedTelemetrySink(int maximumSeries = 128)
        {
            metrics = new ConnectedTelemetryMetrics(maximumSeries);
        }

        public IList<FlagEvaluationRecord> FlagEvaluations
        {
            get
            {
                lock (sync)
                {
                    return flagEvaluations.ToArray();
                }
            }
        }

        public IList<RuleComparisonRecord> RuleComparisons
        {
            get
            {
                lock (sync)
                {
                    return ruleComparisons.ToArray();
                }
            }
        }

        public ConnectedTelemetryMetricsSnapshot Metrics
        {
            get { return metrics.Snapshot(); }
        }

        // Retains a flag record in insertion order; callers are responsible for passing non-null.
        public void RecordFlagEvaluation(FlagEvaluationRecord record)
        {
            lock (sync)
            {
                flagEvaluations.Add(record);
            }
        }

        // Retains a comparison in insertion order; callers are responsible for passing non-null.
        public void RecordRuleComparison(RuleComparisonRecord record)
        {
            lock (sync)
            {
                ruleComparisons.Add(record);
            }
        }

        // Delegates counter aggregation to the same bounded implementation used by the JSON sink.
        public void IncrementMetric(string name, string mode, string outcome)
        {
            metrics.Increment(name, mode, outcome);
        }

        // Delegates duration aggregation to the same bounded implementation used by the JSON sink.
        public void RecordDuration(string name, string mode, TimeSpan duration)
        {
            metrics.RecordDuration(name, mode, duration);
        }
    }
}
