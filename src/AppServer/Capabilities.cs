// PURPOSE
//
// This file exposes the service-owned Connected routing decision through the authenticated
// /api/v1/capabilities endpoint. It validates caller-supplied version and correlation headers,
// converts the internal decision to a stable public shape, and emits non-authoritative telemetry.
// It does not authorize business work, execute checkout rules, or write workflow data.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;

namespace ToolLending.AppServer
{
    // Defines API-only reasons that safely narrow unsupported client input to Legacy behavior.
    // Existing evaluator reasons remain unchanged because client headers are an HTTP concern.
    internal static class CapabilityApiReasons
    {
        public const string ClientVersionMissing = "CLIENT_VERSION_MISSING";
        public const string ClientVersionInvalid = "CLIENT_VERSION_INVALID";
    }

    // Carries non-sensitive, short-lived routing metadata to supported desktop clients.
    // The response is not authentication, authorization, or proof that a checkout will succeed.
    public sealed class CapabilityResponse
    {
        // Creates an empty response for the Web API serializer and the service mapping below.
        public CapabilityResponse() { }

        public int SchemaVersion { get; set; }
        public string ConfigurationVersion { get; set; }
        public DateTimeOffset EvaluatedAt { get; set; }
        public DateTimeOffset ExpiresAt { get; set; }
        public bool ConnectedEnabled { get; set; }
        public string CheckoutRuleMode { get; set; }
        public string Reason { get; set; }
        public Guid CorrelationId { get; set; }
    }

    // Evaluates one capability request from bounded client input and server-owned rollout context.
    // Tests supply fixed collaborators; production uses the cached evaluator and safe telemetry.
    internal sealed class CapabilityService
    {
        private const int MaximumClientVersionLength = 64;
        private readonly IConnectedFeatureEvaluator evaluator;
        private readonly IConnectedTelemetrySink telemetry;

        // Stores the evaluator and telemetry sink required for every request.
        // Null collaborators are rejected so routing decisions cannot silently skip evaluation.
        public CapabilityService(
            IConnectedFeatureEvaluator evaluator,
            IConnectedTelemetrySink telemetry
        )
        {
            this.evaluator = evaluator ?? throw new ArgumentNullException(nameof(evaluator));
            this.telemetry = telemetry ?? throw new ArgumentNullException(nameof(telemetry));
        }

        // Returns a service-authored routing decision. Missing, malformed, or overlong semantic
        // versions are evaluated for freshness evidence and then narrowed to Legacy. Request data
        // can never elevate the server decision. Telemetry failure is isolated by the supplied
        // production sink and does not change the response.
        public CapabilityResponse Get(string clientVersion, Guid correlationId)
        {
            Version parsed;
            var missing = string.IsNullOrWhiteSpace(clientVersion);
            var valid =
                !missing
                && clientVersion.Length <= MaximumClientVersionLength
                && Version.TryParse(clientVersion, out parsed)
                && parsed.Major >= 0
                && parsed.Minor >= 0;
            var capability = evaluator.Evaluate(
                ConnectedFeatureConfiguration.CreateServerContext(valid ? clientVersion : null),
                correlationId
            );

            if (!valid)
            {
                capability.ConnectedEnabled = false;
                capability.CheckoutRuleMode = CheckoutRuleMode.Legacy;
                capability.Reason = missing
                    ? CapabilityApiReasons.ClientVersionMissing
                    : CapabilityApiReasons.ClientVersionInvalid;
            }

            telemetry.RecordFlagEvaluation(
                new FlagEvaluationRecord(
                    capability.EvaluatedAt,
                    "connected.enabled",
                    capability.ConnectedEnabled ? "true" : "false",
                    capability.Reason,
                    capability.ConfigurationVersion,
                    ConnectedFeatureConfiguration.CreateServerContext(null).PracticeKey,
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
                    ConnectedFeatureConfiguration.CreateServerContext(null).PracticeKey,
                    capability.CorrelationId
                )
            );

            return new CapabilityResponse
            {
                SchemaVersion = capability.SchemaVersion,
                ConfigurationVersion = capability.ConfigurationVersion,
                EvaluatedAt = capability.EvaluatedAt,
                ExpiresAt = capability.ExpiresAt,
                ConnectedEnabled = capability.ConnectedEnabled,
                CheckoutRuleMode = capability.CheckoutRuleMode.ToString().ToLowerInvariant(),
                Reason = capability.Reason,
                CorrelationId = capability.CorrelationId,
            };
        }
    }

    // Serves the additive authenticated capability route without changing existing API routes.
    // ApiKeyHandler authenticates the request before this controller runs.
    [RoutePrefix("api/v1/capabilities")]
    public sealed class CapabilitiesController : ApiController
    {
        private static readonly CapabilityService Service = new CapabilityService(
            ConnectedFeatureConfiguration.CreateEvaluator(),
            new SafeConnectedTelemetrySink(new JsonDiagnosticConnectedTelemetrySink(Console.Error))
        );

        // Creates the Web API controller. Process-wide collaborators retain the evaluator cache
        // across requests and write only safe diagnostic output.
        public CapabilitiesController() { }

        // Returns the current routing capability. X-Client-Version may only narrow rollout.
        // A valid UUID in X-Correlation-ID is preserved; absent or malformed values are replaced.
        [HttpGet, Route("")]
        public IHttpActionResult Get()
        {
            return Ok(
                Service.Get(
                    FirstHeader("X-Client-Version"),
                    CorrelationId(FirstHeader("X-Correlation-ID"))
                )
            );
        }

        // Returns the first header value and treats absent or empty collections as missing.
        private string FirstHeader(string name)
        {
            IEnumerable<string> values;
            return Request.Headers.TryGetValues(name, out values) ? values.FirstOrDefault() : null;
        }

        // Preserves only a valid UUID so unbounded caller text cannot enter diagnostics.
        private static Guid CorrelationId(string value)
        {
            Guid parsed;
            return Guid.TryParse(value, out parsed) ? parsed : Guid.NewGuid();
        }
    }
}
