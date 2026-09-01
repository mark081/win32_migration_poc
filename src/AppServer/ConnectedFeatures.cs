// PURPOSE
// -------
// This file is the AppServer's feature-switch foundation for the Connected migration.
// It answers one question for later workflow code:
//
//     Should this request use Legacy, Compare, or Service checkout-rule behavior?
//
// It reads a versioned JSON configuration snapshot, validates its age and targets, caches it, and
// returns a short-lived capability decision. The parent connected.enabled flag always dominates.
// Missing, invalid, expired, or unreadable configuration returns Legacy behavior.
//
// This file does not execute checkout rules, expose an HTTP endpoint, or write business data.
// Those integrations belong to later tasks. At this foundation stage it is exercised by focused
// tests only, so deploying it cannot change runtime behavior by itself.

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace ToolLending.AppServer
{
    // Stable diagnostic reasons emitted with a Connected capability decision.
    //
    // These are observable contract tokens. Add values rather than changing an existing meaning
    // so mixed-version deployments remain diagnosable.
    internal static class ConnectedFeatureReasons
    {
        public const string ParentDisabled = "PARENT_DISABLED";
        public const string SnapshotMissing = "SNAPSHOT_MISSING";
        public const string SnapshotInvalid = "SNAPSHOT_INVALID";
        public const string SnapshotExpired = "SNAPSHOT_EXPIRED";
        public const string SourceError = "SOURCE_ERROR";
        public const string ParentTargetMiss = "PARENT_TARGET_MISS";
        public const string ChildMissing = "CHILD_MISSING";
        public const string ChildInvalid = "CHILD_INVALID";
        public const string ChildTargetMiss = "CHILD_TARGET_MISS";
        public const string Legacy = "LEGACY";
        public const string Compare = "COMPARE";
        public const string Service = "SERVICE";
    }

    // Identifies which checkout-rule implementation a later workflow must use.
    internal enum CheckoutRuleMode
    {
        Legacy,
        Compare,
        Service,
    }

    // Holds service-owned rollout attributes. Values select cohorts but grant no authorization.
    internal sealed class FeatureEvaluationContext
    {
        public string Environment { get; set; }
        public string PracticeKey { get; set; }
        public string DeploymentRing { get; set; }
        public string ClientVersion { get; set; }
    }

    // Carries one observable, time-limited routing decision to later service or client seams.
    internal sealed class ConnectedCapability
    {
        public int SchemaVersion { get; set; }
        public string ConfigurationVersion { get; set; }
        public DateTimeOffset EvaluatedAt { get; set; }
        public DateTimeOffset ExpiresAt { get; set; }
        public bool ConnectedEnabled { get; set; }
        public CheckoutRuleMode CheckoutRuleMode { get; set; }
        public string Reason { get; set; }
        public Guid CorrelationId { get; set; }
    }

    // Separates configuration failure causes without allowing any failure to enable Connected.
    internal enum FeatureSnapshotLoadStatus
    {
        Loaded,
        Missing,
        Invalid,
        SourceError,
    }

    // Couples a source outcome with a snapshot only when loading and validation succeeded.
    internal sealed class FeatureSnapshotLoadResult
    {
        private FeatureSnapshotLoadResult(
            FeatureSnapshotLoadStatus status,
            ConnectedFeatureSnapshot snapshot
        )
        {
            Status = status;
            Snapshot = snapshot;
        }

        public FeatureSnapshotLoadStatus Status { get; private set; }
        public ConnectedFeatureSnapshot Snapshot { get; private set; }

        // Reports a successfully parsed snapshot. The evaluator still checks time and targeting.
        public static FeatureSnapshotLoadResult Loaded(ConnectedFeatureSnapshot snapshot)
        {
            return new FeatureSnapshotLoadResult(FeatureSnapshotLoadStatus.Loaded, snapshot);
        }

        // Reports an absent or unconfigured source so the evaluator can return Legacy safely.
        public static FeatureSnapshotLoadResult Missing()
        {
            return new FeatureSnapshotLoadResult(FeatureSnapshotLoadStatus.Missing, null);
        }

        // Reports content that was readable but incompatible or malformed.
        public static FeatureSnapshotLoadResult Invalid()
        {
            return new FeatureSnapshotLoadResult(FeatureSnapshotLoadStatus.Invalid, null);
        }

        // Reports an unavailable source without exposing filesystem or provider details.
        public static FeatureSnapshotLoadResult SourceError()
        {
            return new FeatureSnapshotLoadResult(FeatureSnapshotLoadStatus.SourceError, null);
        }
    }

    // Loads one immutable, versioned feature configuration without evaluating a caller.
    //
    // Missing, invalid, and unavailable sources remain distinct diagnostic states, but none may
    // enable Connected behavior. The evaluator caches results, so loading is not per operation.
    internal interface IConnectedFeatureSnapshotSource
    {
        // Loads configuration relative to the supplied UTC instant and performs no targeting.
        FeatureSnapshotLoadResult Load(DateTimeOffset now);
    }

    // Produces the service-authoritative capability for a server-owned context.
    //
    // Parent flag: connected.enabled
    // Owner: application service
    // Safe default and provider failure: Legacy behavior
    // Targeting: environment, practice, deployment ring, and client version may only narrow rollout
    // Observability: reason, configuration version, expiry, and correlation ID
    // Rollback: set the parent flag to false
    // Removal: retain the parent throughout Connected; remove child modes only after equivalence
    // evidence permits retirement of the Legacy path.
    internal interface IConnectedFeatureEvaluator
    {
        // Returns a fail-closed routing decision and performs no authoritative business write.
        ConnectedCapability Evaluate(FeatureEvaluationContext context, Guid correlationId);
    }

    // Makes UTC time replaceable so freshness and expiry boundaries are deterministic in tests.
    internal interface IConnectedFeatureClock
    {
        DateTimeOffset UtcNow { get; }
    }

    // Supplies wall-clock UTC time in production.
    internal sealed class SystemConnectedFeatureClock : IConnectedFeatureClock
    {
        public DateTimeOffset UtcNow
        {
            get { return DateTimeOffset.UtcNow; }
        }
    }

    // Stores immutable allow-lists; an empty list means that dimension is unrestricted.
    internal sealed class ConnectedFeatureTargets
    {
        // Copies target lists so external mutation cannot change an active evaluation snapshot.
        public ConnectedFeatureTargets(
            IEnumerable<string> environments,
            IEnumerable<string> practiceKeys,
            IEnumerable<string> rings,
            string minimumClientVersion
        )
        {
            Environments = Copy(environments);
            PracticeKeys = Copy(practiceKeys);
            Rings = Copy(rings);
            MinimumClientVersion = minimumClientVersion;
        }

        public IReadOnlyList<string> Environments { get; private set; }
        public IReadOnlyList<string> PracticeKeys { get; private set; }
        public IReadOnlyList<string> Rings { get; private set; }
        public string MinimumClientVersion { get; private set; }

        // Normalizes a missing list to an immutable empty list used as an unrestricted target.
        private static IReadOnlyList<string> Copy(IEnumerable<string> values)
        {
            return Array.AsReadOnly((values ?? Enumerable.Empty<string>()).ToArray());
        }
    }

    // Represents one validated, immutable configuration version before freshness evaluation.
    internal sealed class ConnectedFeatureSnapshot
    {
        // Captures parent and checkout settings as one atomically replaceable configuration value.
        public ConnectedFeatureSnapshot(
            int schemaVersion,
            string configurationVersion,
            DateTimeOffset issuedAt,
            DateTimeOffset expiresAt,
            bool enabled,
            ConnectedFeatureTargets parentTargets,
            string checkoutRuleMode,
            ConnectedFeatureTargets checkoutTargets
        )
        {
            SchemaVersion = schemaVersion;
            ConfigurationVersion = configurationVersion;
            IssuedAt = issuedAt;
            ExpiresAt = expiresAt;
            Enabled = enabled;
            ParentTargets = parentTargets;
            CheckoutRuleMode = checkoutRuleMode;
            CheckoutTargets = checkoutTargets;
        }

        public int SchemaVersion { get; private set; }
        public string ConfigurationVersion { get; private set; }
        public DateTimeOffset IssuedAt { get; private set; }
        public DateTimeOffset ExpiresAt { get; private set; }
        public bool Enabled { get; private set; }
        public ConnectedFeatureTargets ParentTargets { get; private set; }
        public string CheckoutRuleMode { get; private set; }
        public ConnectedFeatureTargets CheckoutTargets { get; private set; }
    }

    // Reads the provider-neutral JSON snapshot used by the Connected POC.
    //
    // Times are UTC instants. Shape is validated here and freshness by the evaluator. File sharing
    // permits atomic replacement by an external process; partial or unreadable content cannot
    // enable Connected behavior.
    internal sealed class JsonFileFeatureSnapshotSource : IConnectedFeatureSnapshotSource
    {
        private readonly string path;

        // Binds the source to an externally configured path; an empty path remains safely missing.
        public JsonFileFeatureSnapshotSource(string path)
        {
            this.path = path;
        }

        // Reads and validates one file version. The UTC argument belongs to the source contract but
        // freshness remains the evaluator's responsibility.
        public FeatureSnapshotLoadResult Load(DateTimeOffset now)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                return FeatureSnapshotLoadResult.Missing();
            }

            try
            {
                string json;
                using (
                    var stream = new FileStream(
                        path,
                        FileMode.Open,
                        FileAccess.Read,
                        FileShare.ReadWrite | FileShare.Delete
                    )
                )
                using (var reader = new StreamReader(stream))
                {
                    json = reader.ReadToEnd();
                }

                var document = JsonConvert.DeserializeObject<SnapshotDocument>(json);
                ConnectedFeatureSnapshot snapshot;
                return TryCreateSnapshot(document, out snapshot)
                    ? FeatureSnapshotLoadResult.Loaded(snapshot)
                    : FeatureSnapshotLoadResult.Invalid();
            }
            catch (JsonException)
            {
                return FeatureSnapshotLoadResult.Invalid();
            }
            catch (IOException)
            {
                return FeatureSnapshotLoadResult.SourceError();
            }
            catch (UnauthorizedAccessException)
            {
                return FeatureSnapshotLoadResult.SourceError();
            }
        }

        // Converts the JSON document only when schema, timestamps, and target collections are valid.
        private static bool TryCreateSnapshot(
            SnapshotDocument document,
            out ConnectedFeatureSnapshot snapshot
        )
        {
            snapshot = null;
            if (
                document == null
                || document.SchemaVersion != 1
                || string.IsNullOrWhiteSpace(document.ConfigurationVersion)
                || document.Connected == null
                || document.Connected.Targets == null
                || document.IssuedAt == default(DateTimeOffset)
                || document.ExpiresAt <= document.IssuedAt
            )
            {
                return false;
            }

            var parentTargets = CreateTargets(document.Connected.Targets);
            var childTargets =
                document.Connected.Checkout == null
                    ? null
                    : CreateTargets(document.Connected.Checkout.Targets);
            if (
                parentTargets == null
                || (document.Connected.Checkout != null && childTargets == null)
            )
            {
                return false;
            }

            snapshot = new ConnectedFeatureSnapshot(
                document.SchemaVersion,
                document.ConfigurationVersion,
                document.IssuedAt,
                document.ExpiresAt,
                document.Connected.Enabled,
                parentTargets,
                document.Connected.Checkout == null ? null : document.Connected.Checkout.RuleMode,
                childTargets
            );
            return true;
        }

        // Validates target tokens and the optional minimum System.Version before copying them.
        private static ConnectedFeatureTargets CreateTargets(TargetsDocument targets)
        {
            if (
                targets == null
                || !ValidValues(targets.Environments)
                || !ValidValues(targets.PracticeKeys)
                || !ValidValues(targets.Rings)
            )
            {
                return null;
            }

            Version ignored;
            if (
                !string.IsNullOrWhiteSpace(targets.MinimumClientVersion)
                && !Version.TryParse(targets.MinimumClientVersion, out ignored)
            )
            {
                return null;
            }

            return new ConnectedFeatureTargets(
                targets.Environments,
                targets.PracticeKeys,
                targets.Rings,
                targets.MinimumClientVersion
            );
        }

        // Requires an explicit array and rejects blank target values that could match ambiguously.
        private static bool ValidValues(string[] values)
        {
            return values != null && values.All(value => !string.IsNullOrWhiteSpace(value));
        }

        // Defines the private JSON shape; it is not an API request or response contract.
        private sealed class SnapshotDocument
        {
            public int SchemaVersion { get; set; }
            public string ConfigurationVersion { get; set; }
            public DateTimeOffset IssuedAt { get; set; }
            public DateTimeOffset ExpiresAt { get; set; }
            public ConnectedDocument Connected { get; set; }
        }

        // Defines the parent flag and optional checkout child section in the JSON file.
        private sealed class ConnectedDocument
        {
            public bool Enabled { get; set; }
            public TargetsDocument Targets { get; set; }
            public CheckoutDocument Checkout { get; set; }
        }

        // Defines checkout mode and its additional rollout restrictions in the JSON file.
        private sealed class CheckoutDocument
        {
            public string RuleMode { get; set; }
            public TargetsDocument Targets { get; set; }
        }

        // Defines serialized allow-lists and minimum client version before validation.
        private sealed class TargetsDocument
        {
            public string[] Environments { get; set; }
            public string[] PracticeKeys { get; set; }
            public string[] Rings { get; set; }
            public string MinimumClientVersion { get; set; }
        }
    }

    // Applies parent dominance, targeting, freshness, and checkout mode to cached state.
    //
    // Legacy uses the existing rule. Compare observes both results but permits one authoritative
    // write. Service makes the service result authoritative. This evaluator only selects a mode;
    // PostgreSQL retains workflow-data and transactional-rule authority. Snapshot replacement is
    // serialized and becomes visible atomically. Scenarios: LOAN-001 through LOAN-003.
    internal sealed class CachedConnectedFeatureEvaluator : IConnectedFeatureEvaluator
    {
        private readonly object sync = new object();
        private readonly IConnectedFeatureSnapshotSource source;
        private readonly IConnectedFeatureClock clock;
        private readonly TimeSpan refreshInterval;
        private readonly TimeSpan maximumAge;
        private readonly TimeSpan capabilityLifetime;
        private FeatureSnapshotLoadResult cached;
        private DateTimeOffset nextRefresh = DateTimeOffset.MinValue;

        // Creates an evaluator with explicit cache, freshness, and capability lifetimes.
        // Production construction supplies bounded positive durations.
        public CachedConnectedFeatureEvaluator(
            IConnectedFeatureSnapshotSource source,
            IConnectedFeatureClock clock,
            TimeSpan refreshInterval,
            TimeSpan maximumAge,
            TimeSpan capabilityLifetime
        )
        {
            this.source = source ?? throw new ArgumentNullException(nameof(source));
            this.clock = clock ?? throw new ArgumentNullException(nameof(clock));
            this.refreshInterval = refreshInterval;
            this.maximumAge = maximumAge;
            this.capabilityLifetime = capabilityLifetime;
        }

        // Evaluates cached configuration without network, database, or business writes.
        //
        // context: Server-owned rollout attributes; null matches only unrestricted targets.
        // correlationId: Copied into the decision for diagnostics.
        // Returns: A time-limited decision. Failures select Legacy. An enabled parent can also
        // select Legacy when its child is absent, invalid, or outside the target cohort.
        public ConnectedCapability Evaluate(FeatureEvaluationContext context, Guid correlationId)
        {
            var now = clock.UtcNow;
            var load = GetCurrent(now);
            if (load.Status != FeatureSnapshotLoadStatus.Loaded)
            {
                return Disabled(now, correlationId, ReasonFor(load.Status), null);
            }

            var snapshot = load.Snapshot;
            var freshnessExpiry = snapshot.IssuedAt.Add(maximumAge);
            if (now < snapshot.IssuedAt || now >= snapshot.ExpiresAt || now >= freshnessExpiry)
            {
                return Disabled(
                    now,
                    correlationId,
                    ConnectedFeatureReasons.SnapshotExpired,
                    snapshot
                );
            }

            if (!snapshot.Enabled)
            {
                return Disabled(
                    now,
                    correlationId,
                    ConnectedFeatureReasons.ParentDisabled,
                    snapshot
                );
            }

            if (!Matches(snapshot.ParentTargets, context))
            {
                return Disabled(
                    now,
                    correlationId,
                    ConnectedFeatureReasons.ParentTargetMiss,
                    snapshot
                );
            }

            if (
                string.IsNullOrWhiteSpace(snapshot.CheckoutRuleMode)
                || snapshot.CheckoutTargets == null
            )
            {
                return EnabledLegacy(
                    now,
                    correlationId,
                    ConnectedFeatureReasons.ChildMissing,
                    snapshot
                );
            }

            CheckoutRuleMode mode;
            if (!TryParseMode(snapshot.CheckoutRuleMode, out mode))
            {
                return EnabledLegacy(
                    now,
                    correlationId,
                    ConnectedFeatureReasons.ChildInvalid,
                    snapshot
                );
            }

            if (!Matches(snapshot.CheckoutTargets, context))
            {
                return EnabledLegacy(
                    now,
                    correlationId,
                    ConnectedFeatureReasons.ChildTargetMiss,
                    snapshot
                );
            }

            return Capability(
                now,
                correlationId,
                snapshot,
                true,
                mode,
                mode == CheckoutRuleMode.Compare ? ConnectedFeatureReasons.Compare
                    : mode == CheckoutRuleMode.Service ? ConnectedFeatureReasons.Service
                    : ConnectedFeatureReasons.Legacy
            );
        }

        // Refreshes at most once per interval under a lock and caches failure as a safe result.
        private FeatureSnapshotLoadResult GetCurrent(DateTimeOffset now)
        {
            lock (sync)
            {
                if (cached == null || now >= nextRefresh)
                {
                    try
                    {
                        cached = source.Load(now) ?? FeatureSnapshotLoadResult.SourceError();
                    }
                    catch (Exception)
                    {
                        cached = FeatureSnapshotLoadResult.SourceError();
                    }
                    nextRefresh = now.Add(refreshInterval);
                }
                return cached;
            }
        }

        // Builds a parent-disabled Legacy decision while retaining available configuration evidence.
        private ConnectedCapability Disabled(
            DateTimeOffset now,
            Guid correlationId,
            string reason,
            ConnectedFeatureSnapshot snapshot
        )
        {
            return Capability(now, correlationId, snapshot, false, CheckoutRuleMode.Legacy, reason);
        }

        // Builds a parent-enabled decision whose child safely falls back to Legacy.
        private ConnectedCapability EnabledLegacy(
            DateTimeOffset now,
            Guid correlationId,
            string reason,
            ConnectedFeatureSnapshot snapshot
        )
        {
            return Capability(now, correlationId, snapshot, true, CheckoutRuleMode.Legacy, reason);
        }

        // Builds a decision whose expiry never outlives the snapshot or maximum-age boundary.
        private ConnectedCapability Capability(
            DateTimeOffset now,
            Guid correlationId,
            ConnectedFeatureSnapshot snapshot,
            bool enabled,
            CheckoutRuleMode mode,
            string reason
        )
        {
            var expiry = now.Add(capabilityLifetime);
            if (snapshot != null)
            {
                var freshnessExpiry = snapshot.IssuedAt.Add(maximumAge);
                if (snapshot.ExpiresAt < expiry)
                    expiry = snapshot.ExpiresAt;
                if (freshnessExpiry < expiry)
                    expiry = freshnessExpiry;
            }
            return new ConnectedCapability
            {
                SchemaVersion = 1,
                ConfigurationVersion =
                    snapshot == null ? string.Empty : snapshot.ConfigurationVersion,
                EvaluatedAt = now,
                ExpiresAt = expiry < now ? now : expiry,
                ConnectedEnabled = enabled,
                CheckoutRuleMode = mode,
                Reason = reason,
                CorrelationId = correlationId,
            };
        }

        // Requires every restricted targeting dimension and minimum client version to match.
        private static bool Matches(
            ConnectedFeatureTargets targets,
            FeatureEvaluationContext context
        )
        {
            context = context ?? new FeatureEvaluationContext();
            if (
                !MatchesValue(targets.Environments, context.Environment)
                || !MatchesValue(targets.PracticeKeys, context.PracticeKey)
                || !MatchesValue(targets.Rings, context.DeploymentRing)
            )
            {
                return false;
            }
            if (string.IsNullOrWhiteSpace(targets.MinimumClientVersion))
                return true;
            Version actual;
            Version minimum;
            return Version.TryParse(context.ClientVersion, out actual)
                && Version.TryParse(targets.MinimumClientVersion, out minimum)
                && actual >= minimum;
        }

        // Matches tokens case-insensitively; an empty allow-list is intentionally unrestricted.
        private static bool MatchesValue(IReadOnlyList<string> allowed, string actual)
        {
            return allowed.Count == 0
                || allowed.Any(value =>
                    string.Equals(value, actual, StringComparison.OrdinalIgnoreCase)
                );
        }

        // Accepts only the three migration modes; unknown values return Legacy and false.
        private static bool TryParseMode(string value, out CheckoutRuleMode mode)
        {
            if (string.Equals(value, "legacy", StringComparison.OrdinalIgnoreCase))
            {
                mode = CheckoutRuleMode.Legacy;
                return true;
            }
            if (string.Equals(value, "compare", StringComparison.OrdinalIgnoreCase))
            {
                mode = CheckoutRuleMode.Compare;
                return true;
            }
            if (string.Equals(value, "service", StringComparison.OrdinalIgnoreCase))
            {
                mode = CheckoutRuleMode.Service;
                return true;
            }
            mode = CheckoutRuleMode.Legacy;
            return false;
        }

        // Maps source outcomes to stable observable reason tokens without leaking exceptions.
        private static string ReasonFor(FeatureSnapshotLoadStatus status)
        {
            if (status == FeatureSnapshotLoadStatus.Missing)
                return ConnectedFeatureReasons.SnapshotMissing;
            if (status == FeatureSnapshotLoadStatus.Invalid)
                return ConnectedFeatureReasons.SnapshotInvalid;
            return ConnectedFeatureReasons.SourceError;
        }
    }

    // Builds evaluator dependencies and targeting context from bounded external AppServer settings.
    internal static class ConnectedFeatureConfiguration
    {
        // Creates the process-local evaluator. Invalid numeric settings revert to safe defaults.
        public static IConnectedFeatureEvaluator CreateEvaluator()
        {
            var refresh = BoundedInt("ConnectedFeatureRefreshSeconds", 30, 5, 300);
            var maximumAge = BoundedInt("ConnectedFeatureMaxAgeSeconds", 300, 30, 3600);
            var lifetime = BoundedInt(
                "ConnectedCapabilityLifetimeSeconds",
                Math.Min(30, refresh),
                5,
                refresh
            );
            var source = new JsonFileFeatureSnapshotSource(
                ConfigurationManager.AppSettings["ConnectedFeatureSnapshotPath"]
            );
            return new CachedConnectedFeatureEvaluator(
                source,
                new SystemConnectedFeatureClock(),
                TimeSpan.FromSeconds(refresh),
                TimeSpan.FromSeconds(maximumAge),
                TimeSpan.FromSeconds(lifetime)
            );
        }

        // Creates cohort context from server configuration. Client version can only narrow rollout
        // and is not treated as authentication or authorization evidence.
        public static FeatureEvaluationContext CreateServerContext(string clientVersion)
        {
            return new FeatureEvaluationContext
            {
                Environment = ValueOrDefault("ConnectedEnvironment", "local"),
                PracticeKey = ValueOrDefault("ConnectedPracticeKey", string.Empty),
                DeploymentRing = ValueOrDefault("ConnectedDeploymentRing", "default"),
                ClientVersion = clientVersion,
            };
        }

        // Parses invariant, unsigned decimal seconds and uses the default outside inclusive bounds.
        private static int BoundedInt(string key, int defaultValue, int minimum, int maximum)
        {
            int parsed;
            return
                int.TryParse(
                    ConfigurationManager.AppSettings[key],
                    NumberStyles.None,
                    CultureInfo.InvariantCulture,
                    out parsed
                )
                && parsed >= minimum
                && parsed <= maximum
                ? parsed
                : defaultValue;
        }

        // Treats missing and whitespace-only configuration as the supplied safe default.
        private static string ValueOrDefault(string key, string defaultValue)
        {
            var value = ConfigurationManager.AppSettings[key];
            return string.IsNullOrWhiteSpace(value) ? defaultValue : value;
        }
    }
}
