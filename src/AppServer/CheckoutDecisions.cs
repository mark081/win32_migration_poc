// PURPOSE
//
// This file exposes the read-only checkout eligibility decision used during the Connected
// migration. It validates versioned client input, re-evaluates the server-owned feature state,
// reads current member facts, and records optional Legacy-versus-service comparison evidence.
// It never submits a checkout or writes workflow, idempotency, or business-audit data.

using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Web.Http;
using Npgsql;

namespace ToolLending.AppServer
{
    // Defines stable error codes returned by the checkout-decision endpoint.
    public static class CheckoutDecisionErrorCodes
    {
        public const string CapabilityStale = "CAPABILITY_STALE";
        public const string DecisionUnavailable = "DECISION_UNAVAILABLE";
        public const string Unexpected = "UNEXPECTED";
    }

    // Carries the optional normalized NativeRules result used only in compare mode.
    public sealed class LegacyCheckoutObservation
    {
        [Range(1, 1)]
        public int ContractVersion { get; set; }

        public bool Allowed { get; set; }

        [
            Required,
            RegularExpression(
                "^(ALLOWED|MEMBER_INACTIVE|OVERDUE|CHECKOUT_LIMIT_REACHED|TIER_UNSUPPORTED)$"
            )
        ]
        public string Reason { get; set; }
    }

    // Carries bounded decision input. It contains no checkout command or persistence identity.
    public sealed class CheckoutDecisionRequest
    {
        [Range(1, int.MaxValue)]
        public int MemberId { get; set; }

        public DateTime DueOn { get; set; }

        [Required, StringLength(64, MinimumLength = 3)]
        public string ClientVersion { get; set; }

        [Required, StringLength(80, MinimumLength = 1)]
        public string CapabilityConfigurationVersion { get; set; }

        public LegacyCheckoutObservation LegacyObservation { get; set; }
    }

    // Returns an advisory eligibility result. A later PostgreSQL checkout command remains final.
    public sealed class CheckoutDecisionResponse
    {
        public int ContractVersion { get; set; }
        public string EffectiveMode { get; set; }
        public bool Allowed { get; set; }
        public string Reason { get; set; }
        public string MessageKey { get; set; }
        public int? CheckoutLimit { get; set; }
        public int? MaximumLoanDays { get; set; }
        public Guid CorrelationId { get; set; }
        public string ConfigurationVersion { get; set; }
    }

    // Carries a stable public error without exposing an internal exception or request body.
    public sealed class CheckoutDecisionErrorResponse
    {
        public string Code { get; set; }
        public string Message { get; set; }
        public Guid CorrelationId { get; set; }
    }

    // Signals that client routing evidence no longer matches a currently permitted server mode.
    internal sealed class CapabilityStaleException : Exception
    {
        public CapabilityStaleException()
            : base("The checkout capability must be refreshed.") { }
    }

    // Evaluates one read-only request using current service state and member facts.
    internal sealed class CheckoutDecisionService
    {
        private const int DecisionContractVersion = 1;
        private readonly IConnectedFeatureEvaluator featureEvaluator;
        private readonly Func<int, DateTime, MemberEligibilityContext> readMember;
        private readonly ICheckoutRuleEvaluator ruleEvaluator;
        private readonly IBusinessDateClock clock;
        private readonly IConnectedTelemetrySink telemetry;

        // Stores the production collaborators. Repository access remains read-only in this path.
        public CheckoutDecisionService(
            IConnectedFeatureEvaluator featureEvaluator,
            Repository repository,
            ICheckoutRuleEvaluator ruleEvaluator,
            IBusinessDateClock clock,
            IConnectedTelemetrySink telemetry
        )
            : this(
                featureEvaluator,
                repository == null
                    ? (Func<int, DateTime, MemberEligibilityContext>)null
                    : repository.GetMemberEligibilityContext,
                ruleEvaluator,
                clock,
                telemetry
            ) { }

        // Accepts a replaceable read function so focused tests can prove mode and failure behavior.
        internal CheckoutDecisionService(
            IConnectedFeatureEvaluator featureEvaluator,
            Func<int, DateTime, MemberEligibilityContext> readMember,
            ICheckoutRuleEvaluator ruleEvaluator,
            IBusinessDateClock clock,
            IConnectedTelemetrySink telemetry
        )
        {
            this.featureEvaluator =
                featureEvaluator ?? throw new ArgumentNullException(nameof(featureEvaluator));
            this.readMember = readMember ?? throw new ArgumentNullException(nameof(readMember));
            this.ruleEvaluator =
                ruleEvaluator ?? throw new ArgumentNullException(nameof(ruleEvaluator));
            this.clock = clock ?? throw new ArgumentNullException(nameof(clock));
            this.telemetry = telemetry ?? throw new ArgumentNullException(nameof(telemetry));
        }

        // Returns the compare-effective or service-effective decision without writing business data.
        public CheckoutDecisionResponse Decide(CheckoutDecisionRequest request, Guid correlationId)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            Version parsedVersion;
            if (
                request.DueOn == default(DateTime)
                || string.IsNullOrWhiteSpace(request.ClientVersion)
                || request.ClientVersion.Length > 64
                || !Version.TryParse(request.ClientVersion, out parsedVersion)
                || string.IsNullOrWhiteSpace(request.CapabilityConfigurationVersion)
            )
            {
                throw new ArgumentException("The decision request is invalid.");
            }

            var capability = featureEvaluator.Evaluate(
                ConnectedFeatureConfiguration.CreateServerContext(request.ClientVersion),
                correlationId
            );
            RecordCapability(capability);
            if (
                !capability.ConnectedEnabled
                || capability.CheckoutRuleMode == CheckoutRuleMode.Legacy
                || !string.Equals(
                    capability.ConfigurationVersion,
                    request.CapabilityConfigurationVersion,
                    StringComparison.Ordinal
                )
            )
            {
                RecordIncompleteComparison(request, capability, correlationId, "stale_capability");
                throw new CapabilityStaleException();
            }

            if (
                capability.CheckoutRuleMode == CheckoutRuleMode.Compare
                && request.LegacyObservation == null
            )
            {
                telemetry.IncrementMetric(
                    "checkout_decision",
                    "compare",
                    "legacy_observation_missing"
                );
                throw new ArgumentException("A Legacy observation is required in compare mode.");
            }

            var mode = capability.CheckoutRuleMode.ToString().ToLowerInvariant();
            var businessDate = clock.Today;
            var started = Stopwatch.StartNew();
            CheckoutDecision serviceDecision;
            try
            {
                serviceDecision = ruleEvaluator.Evaluate(
                    readMember(request.MemberId, businessDate),
                    request.DueOn,
                    businessDate
                );
            }
            catch
            {
                started.Stop();
                telemetry.RecordDuration("checkout_decision", mode, started.Elapsed);
                RecordIncompleteComparison(
                    request,
                    capability,
                    correlationId,
                    "service_error",
                    started.Elapsed
                );
                throw;
            }
            started.Stop();
            telemetry.RecordDuration("checkout_decision", mode, started.Elapsed);
            if (capability.CheckoutRuleMode == CheckoutRuleMode.Compare)
            {
                var legacy = request.LegacyObservation;
                telemetry.RecordRuleComparison(
                    new RuleComparisonRecord(
                        capability.EvaluatedAt,
                        correlationId,
                        capability.ConfigurationVersion,
                        ConnectedFeatureConfiguration.CreateServerContext(null).PracticeKey,
                        request.MemberId.ToString(CultureInfo.InvariantCulture)
                            + "|"
                            + request.DueOn.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                        legacy.ContractVersion,
                        legacy.Allowed,
                        legacy.Reason,
                        DecisionContractVersion,
                        serviceDecision.Allowed,
                        serviceDecision.Reason,
                        legacy.Allowed == serviceDecision.Allowed
                            && string.Equals(
                                legacy.Reason,
                                serviceDecision.Reason,
                                StringComparison.Ordinal
                            ),
                        started.Elapsed,
                        "completed"
                    )
                );
                return Response(capability, legacy.Allowed, legacy.Reason, serviceDecision);
            }

            return Response(
                capability,
                serviceDecision.Allowed,
                serviceDecision.Reason,
                serviceDecision
            );
        }

        // Emits the effective parent and child decision for every valid decision request.
        private void RecordCapability(ConnectedCapability capability)
        {
            var context = ConnectedFeatureConfiguration.CreateServerContext(null);
            telemetry.RecordFlagEvaluation(
                new FlagEvaluationRecord(
                    capability.EvaluatedAt,
                    "connected.enabled",
                    capability.ConnectedEnabled ? "true" : "false",
                    capability.Reason,
                    capability.ConfigurationVersion,
                    context.PracticeKey,
                    capability.CorrelationId
                )
            );
            telemetry.RecordFlagEvaluation(
                new FlagEvaluationRecord(
                    capability.EvaluatedAt,
                    "connected.checkout.rule-mode",
                    capability.CheckoutRuleMode.ToString().ToLowerInvariant(),
                    capability.Reason,
                    capability.ConfigurationVersion,
                    context.PracticeKey,
                    capability.CorrelationId
                )
            );
        }

        // Records a compare attempt that could not produce a service result. No raw input is kept.
        private void RecordIncompleteComparison(
            CheckoutDecisionRequest request,
            ConnectedCapability capability,
            Guid correlationId,
            string outcome,
            TimeSpan duration = default(TimeSpan)
        )
        {
            if (
                request.LegacyObservation == null
                || (
                    capability.CheckoutRuleMode != CheckoutRuleMode.Compare
                    && !string.Equals(outcome, "stale_capability", StringComparison.Ordinal)
                )
            )
            {
                return;
            }

            var legacy = request.LegacyObservation;
            telemetry.RecordRuleComparison(
                new RuleComparisonRecord(
                    capability.EvaluatedAt,
                    correlationId,
                    capability.ConfigurationVersion,
                    ConnectedFeatureConfiguration.CreateServerContext(null).PracticeKey,
                    request.MemberId.ToString(CultureInfo.InvariantCulture)
                        + "|"
                        + request.DueOn.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                    legacy.ContractVersion,
                    legacy.Allowed,
                    legacy.Reason,
                    DecisionContractVersion,
                    false,
                    string.Empty,
                    false,
                    duration,
                    outcome
                )
            );
        }

        // Maps an internal result to the stable public response and operator message key.
        private static CheckoutDecisionResponse Response(
            ConnectedCapability capability,
            bool allowed,
            string reason,
            CheckoutDecision serviceDecision
        )
        {
            return new CheckoutDecisionResponse
            {
                ContractVersion = DecisionContractVersion,
                EffectiveMode = capability.CheckoutRuleMode.ToString().ToLowerInvariant(),
                Allowed = allowed,
                Reason = reason,
                MessageKey = MessageKey(reason),
                CheckoutLimit = serviceDecision.CheckoutLimit,
                MaximumLoanDays = serviceDecision.MaximumLoanDays,
                CorrelationId = capability.CorrelationId,
                ConfigurationVersion = capability.ConfigurationVersion,
            };
        }

        // Maps stable decision reasons to operator-safe localization keys.
        private static string MessageKey(string reason)
        {
            return string.Equals(reason, CheckoutDecisionReasons.Overdue, StringComparison.Ordinal)
                ? "checkout.member_overdue"
                : "checkout." + reason.ToLowerInvariant();
        }
    }

    // Serves the authenticated decision route; ApiKeyHandler runs before this controller.
    [RoutePrefix("api/v1/checkout-decisions")]
    public sealed class CheckoutDecisionsController : ApiController
    {
        private static readonly CheckoutDecisionService Service = CreateService();

        // Creates the controller; process-wide evaluator state preserves the bounded snapshot cache.
        public CheckoutDecisionsController() { }

        // Returns 200 for a completed decision and stable safe errors for invalid or failed reads.
        [HttpPost, Route("")]
        public IHttpActionResult Post(CheckoutDecisionRequest request)
        {
            var correlationId = CorrelationId();
            if (request == null)
                return BadRequest("A request body is required.");
            if (!ModelState.IsValid || request.DueOn == default(DateTime))
                return BadRequest(ModelState);

            try
            {
                return Ok(Service.Decide(request, correlationId));
            }
            catch (CapabilityStaleException)
            {
                return Error(
                    HttpStatusCode.Conflict,
                    CheckoutDecisionErrorCodes.CapabilityStale,
                    "Refresh checkout capability.",
                    correlationId
                );
            }
            catch (ArgumentException exception)
            {
                ModelState.AddModelError("request", exception.Message);
                return BadRequest(ModelState);
            }
            catch (NpgsqlException)
            {
                return Error(
                    HttpStatusCode.ServiceUnavailable,
                    CheckoutDecisionErrorCodes.DecisionUnavailable,
                    "The checkout decision is temporarily unavailable.",
                    correlationId
                );
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine(
                    "Checkout decision {0} failed: {1}",
                    correlationId,
                    exception.GetType().Name
                );
                return Error(
                    HttpStatusCode.InternalServerError,
                    CheckoutDecisionErrorCodes.Unexpected,
                    "The checkout decision failed.",
                    correlationId
                );
            }
        }

        // Creates production collaborators while keeping telemetry outside database transactions.
        private static CheckoutDecisionService CreateService()
        {
            return new CheckoutDecisionService(
                ConnectedFeatureConfiguration.CreateEvaluator(),
                new Repository(),
                new CheckoutRuleEvaluator(),
                new SystemBusinessDateClock(),
                new SafeConnectedTelemetrySink(
                    new JsonDiagnosticConnectedTelemetrySink(Console.Error)
                )
            );
        }

        // Preserves a valid bounded correlation identifier or creates a safe replacement.
        private Guid CorrelationId()
        {
            System.Collections.Generic.IEnumerable<string> values;
            Guid parsed;
            return
                Request.Headers.TryGetValues("X-Correlation-ID", out values)
                && Guid.TryParse(System.Linq.Enumerable.FirstOrDefault(values), out parsed)
                ? parsed
                : Guid.NewGuid();
        }

        // Builds a stable error body without exposing raw dependency or validation details.
        private IHttpActionResult Error(
            HttpStatusCode status,
            string code,
            string message,
            Guid correlationId
        )
        {
            return Content(
                status,
                new CheckoutDecisionErrorResponse
                {
                    Code = code,
                    Message = message,
                    CorrelationId = correlationId,
                }
            );
        }
    }
}
