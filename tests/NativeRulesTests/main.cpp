// PURPOSE
//
// This test executable protects the NativeRules compatibility contract and the desktop's small
// WinHTTP configuration boundary. It runs without changing workflow data. Network checks use an
// unreachable loopback port and never contact an external service.

#include <iostream>
#include "../../src/NativeRules/NativeRules.h"
#include "../../src/DesktopClient/ClientTransport.h"
#include "../../src/DesktopClient/CapabilityRouter.h"
#include "../../src/DesktopClient/CheckoutMode.h"
static_assert(NR_ALLOWED == 0, "version 1 allowed reason changed");
static_assert(NR_INACTIVE == 1, "version 1 inactive reason changed");
static_assert(NR_OVERDUE == 2, "version 1 overdue reason changed");
static_assert(NR_CHECKOUT_LIMIT_REACHED == 3, "version 1 limit reason changed");
static_assert(NR_TIER_UNSUPPORTED == 4, "version 1 unsupported-tier reason changed");
static int failures = 0;

// Records one dependency-free assertion and keeps later cases running after a failure.
static void check(bool value, const char *name)
{
    if (!value)
    {
        std::cerr << "FAIL: " << name << "\n";
        ++failures;
    }
}

// Confirms the stable native reason and the retained boolean export agree for one input row.
static void checkEligibility(int active, int overdue, int openLoans, const wchar_t *tier,
                             CheckoutEligibilityReasonCode expected, const char *name)
{
    const CheckoutEligibilityReasonCode reason =
        CheckoutEligibilityReasonV1(active, overdue, openLoans, tier);
    check(reason == expected, name);
    check((IsEligibleForCheckout(active, overdue, openLoans, tier) != 0) == (reason == NR_ALLOWED),
          "legacy boolean matches structured eligibility");
}

// Checks default/external endpoint parsing, HTTPS enforcement, bounded timeouts, and a classified
// unavailable result without sending credentials or business data outside loopback.
static void checkTransportConfiguration()
{
    SetEnvironmentVariableW(L"TOOL_LENDING_LEGACY_BASE_URL", nullptr);
    SetEnvironmentVariableW(L"TOOL_LENDING_CONNECTED_BASE_URL", nullptr);
    SetEnvironmentVariableW(L"TOOL_LENDING_CONNECT_TIMEOUT_MS", nullptr);
    ClientEndpointConfiguration configuration;
    std::wstring error;
    check(LoadClientEndpointConfiguration(configuration, error), "default endpoint configuration");
    check(configuration.legacy.host == L"localhost", "default Legacy host");
    check(configuration.legacy.port == 8088, "default Legacy port");
    check(!configuration.hasConnected, "Connected endpoint absent by default");
    check(ClassifyClientHttpStatus(400) == TransportFailure::Validation,
          "HTTP validation classified");
    check(ClassifyClientHttpStatus(401) == TransportFailure::Authentication,
          "HTTP authentication classified");
    check(ClassifyClientHttpStatus(403) == TransportFailure::Authorization,
          "HTTP authorization classified");
    check(ClassifyClientHttpStatus(409) == TransportFailure::Conflict, "HTTP conflict classified");
    check(ClassifyClientHttpStatus(504) == TransportFailure::Timeout, "HTTP timeout classified");
    check(ClassifyClientHttpStatus(503) == TransportFailure::Unavailable,
          "HTTP unavailable classified");
    check(ClassifyClientHttpStatus(500) == TransportFailure::Unexpected,
          "HTTP unexpected classified");

    SetEnvironmentVariableW(L"TOOL_LENDING_CONNECTED_BASE_URL", L"http://connected.example/");
    configuration = ClientEndpointConfiguration();
    error.clear();
    check(!LoadClientEndpointConfiguration(configuration, error),
          "Connected endpoint requires HTTPS");

    SetEnvironmentVariableW(L"TOOL_LENDING_CONNECTED_BASE_URL",
                            L"https://user:password@connected.example/");
    configuration = ClientEndpointConfiguration();
    error.clear();
    check(!LoadClientEndpointConfiguration(configuration, error),
          "Connected URL rejects embedded credential");

    SetEnvironmentVariableW(L"TOOL_LENDING_CONNECTED_BASE_URL", L"https://connected.example/base/");
    configuration = ClientEndpointConfiguration();
    error.clear();
    check(!LoadClientEndpointConfiguration(configuration, error),
          "Connected endpoint requires external credential");

    wchar_t temporaryPath[MAX_PATH] = {};
    wchar_t credentialPath[MAX_PATH] = {};
    GetTempPathW(MAX_PATH, temporaryPath);
    GetTempFileNameW(temporaryPath, L"tlc", 0, credentialPath);
    HANDLE credentialFile = CreateFileW(credentialPath, GENERIC_WRITE, 0, nullptr, CREATE_ALWAYS,
                                        FILE_ATTRIBUTE_TEMPORARY, nullptr);
    const char credential[] = "synthetic-connected-key\n";
    DWORD written = 0;
    WriteFile(credentialFile, credential, sizeof(credential) - 1, &written, nullptr);
    CloseHandle(credentialFile);
    SetEnvironmentVariableW(L"TOOL_LENDING_CONNECTED_CREDENTIAL_FILE", credentialPath);
    check(LoadClientEndpointConfiguration(configuration, error),
          "HTTPS Connected endpoint accepted");
    check(configuration.hasConnected && configuration.connected.secure,
          "Connected endpoint marked secure");
    check(configuration.connected.basePath == L"/base", "Connected base path retained");

    SetEnvironmentVariableW(L"TOOL_LENDING_CONNECT_TIMEOUT_MS", L"0");
    configuration = ClientEndpointConfiguration();
    error.clear();
    check(!LoadClientEndpointConfiguration(configuration, error), "zero timeout rejected");

    SetEnvironmentVariableW(L"TOOL_LENDING_CONNECTED_BASE_URL", nullptr);
    SetEnvironmentVariableW(L"TOOL_LENDING_CONNECTED_CREDENTIAL_FILE", nullptr);
    SetEnvironmentVariableW(L"TOOL_LENDING_CONNECT_TIMEOUT_MS", L"100");
    SetEnvironmentVariableW(L"TOOL_LENDING_RECEIVE_TIMEOUT_MS", L"100");
    SetEnvironmentVariableW(L"TOOL_LENDING_SEND_TIMEOUT_MS", L"100");
    SetEnvironmentVariableW(L"TOOL_LENDING_RESOLVE_TIMEOUT_MS", L"100");
    SetEnvironmentVariableW(L"TOOL_LENDING_LEGACY_BASE_URL", L"http://127.0.0.1:1/");
    configuration = ClientEndpointConfiguration();
    error.clear();
    check(LoadClientEndpointConfiguration(configuration, error), "loopback endpoint accepted");
    const ClientHttpResult unavailable = SendClientHttp(configuration, configuration.legacy, L"GET",
                                                        L"/api/v1/health", L"synthetic-test-key");
    check(unavailable.failure == TransportFailure::Unavailable,
          "connection failure classified unavailable");

    if (GetEnvironmentVariableW(L"TOOL_LENDING_RUN_TLS_TESTS", nullptr, 0))
    {
        SetEnvironmentVariableW(L"TOOL_LENDING_CONNECTED_CREDENTIAL_FILE", credentialPath);
        SetEnvironmentVariableW(L"TOOL_LENDING_CONNECT_TIMEOUT_MS", L"10000");
        SetEnvironmentVariableW(L"TOOL_LENDING_RECEIVE_TIMEOUT_MS", L"10000");
        SetEnvironmentVariableW(L"TOOL_LENDING_SEND_TIMEOUT_MS", L"10000");
        SetEnvironmentVariableW(L"TOOL_LENDING_RESOLVE_TIMEOUT_MS", L"10000");
        SetEnvironmentVariableW(L"TOOL_LENDING_CONNECTED_BASE_URL", L"https://sha256.badssl.com/");
        configuration = ClientEndpointConfiguration();
        error.clear();
        check(LoadClientEndpointConfiguration(configuration, error),
              "trusted TLS endpoint configuration");
        const ClientHttpResult trusted = SendClientHttp(
            configuration, configuration.connected, L"GET", L"/", configuration.connectedApiKey);
        check(trusted.failure == TransportFailure::None, "trusted TLS certificate accepted");

        SetEnvironmentVariableW(L"TOOL_LENDING_CONNECTED_BASE_URL",
                                L"https://wrong.host.badssl.com/");
        configuration = ClientEndpointConfiguration();
        error.clear();
        check(LoadClientEndpointConfiguration(configuration, error),
              "hostname-mismatch endpoint configuration");
        const ClientHttpResult wrongHost = SendClientHttp(
            configuration, configuration.connected, L"GET", L"/", configuration.connectedApiKey);
        check(wrongHost.failure != TransportFailure::None, "TLS hostname mismatch rejected");
    }

    SetEnvironmentVariableW(L"TOOL_LENDING_LEGACY_BASE_URL", nullptr);
    SetEnvironmentVariableW(L"TOOL_LENDING_CONNECTED_BASE_URL", nullptr);
    SetEnvironmentVariableW(L"TOOL_LENDING_CONNECTED_CREDENTIAL_FILE", nullptr);
    SetEnvironmentVariableW(L"TOOL_LENDING_CONNECT_TIMEOUT_MS", nullptr);
    SetEnvironmentVariableW(L"TOOL_LENDING_RECEIVE_TIMEOUT_MS", nullptr);
    SetEnvironmentVariableW(L"TOOL_LENDING_SEND_TIMEOUT_MS", nullptr);
    SetEnvironmentVariableW(L"TOOL_LENDING_RESOLVE_TIMEOUT_MS", nullptr);
    DeleteFileW(credentialPath);
}

// Proves every transport category has distinct operator-safe text and that failures cannot be
// mistaken for a successful response body. HTTP status is preferred over a Win32 error number.
static void checkFailurePresentation()
{
    struct FailureCase
    {
        TransportFailure failure;
        DWORD status;
        const wchar_t *text;
    };
    const FailureCase cases[] = {
        {TransportFailure::Configuration, 0, L"ERROR: configuration (87)"},
        {TransportFailure::Timeout, 504, L"ERROR: timeout (504)"},
        {TransportFailure::Unavailable, 503, L"ERROR: service unavailable (503)"},
        {TransportFailure::Authentication, 401, L"ERROR: authentication (401)"},
        {TransportFailure::Authorization, 403, L"ERROR: authorization (403)"},
        {TransportFailure::Validation, 400, L"ERROR: validation (400)"},
        {TransportFailure::Conflict, 409, L"ERROR: conflict (409)"},
        {TransportFailure::Unexpected, 500, L"ERROR: unexpected (500)"},
    };
    for (const FailureCase &item : cases)
    {
        ClientHttpResult result;
        result.failure = item.failure;
        result.statusCode = item.status;
        result.systemError = ERROR_INVALID_PARAMETER;
        result.body = "must-not-be-presented-as-success";
        check(FormatClientHttpResult(result) == item.text, "transport failure has stable UI text");
    }
}

// Exercises the complete capability truth table without network access or workflow writes.
static void checkCapabilityRouting()
{
    ClientEndpointConfiguration configuration;
    configuration.legacy.host = L"legacy";
    configuration.connected.host = L"connected";
    configuration.connected.secure = true;
    configuration.hasConnected = true;
    EndpointRouter router(configuration);
    const std::time_t now = 1788451200; // 2026-09-03T16:00:00Z
    const std::string prefix = "{\"schemaVersion\":1,\"configurationVersion\":\"v1\","
                               "\"evaluatedAt\":\"2026-09-03T15:59:50Z\","
                               "\"expiresAt\":\"2026-09-03T16:00:30Z\",\"connectedEnabled\":true,"
                               "\"checkoutRuleMode\":\"";
    check(router.Mode(now) == ClientRuleMode::Legacy, "absent capability selects Legacy");
    check(router.Accept(prefix + "compare\",\"reason\":\"ENABLED\"}", now),
          "compare capability accepted");
    check(router.Mode(now) == ClientRuleMode::Compare, "compare selects Connected");
    check(router.Endpoint(now).host == L"connected", "compare endpoint is Connected");
    check(router.Mode(now + 30) == ClientRuleMode::Legacy, "expiry restores Legacy");
    check(router.Accept(prefix + "service\",\"reason\":\"ENABLED\"}", now),
          "service capability accepted");
    check(router.Mode(now) == ClientRuleMode::Service, "service selects Connected");
    check(!router.Accept(prefix + "legacy\",\"reason\":\"LEGACY\"}", now),
          "Legacy response cannot select Connected");
    check(!router.Accept(prefix + "compare\",\"connectedEnabled\":false}", now),
          "duplicate parent field rejected");
    check(!router.Accept("{\"schemaVersion\":2}", now), "unsupported schema selects Legacy");
    check(!router.Accept("not-json", now), "malformed capability selects Legacy");
    check(!router.Accept("{\"schemaVersion\":1,\"configurationVersion\":\"v1\","
                         "\"evaluatedAt\":\"2026-09-03T15:59:00Z\","
                         "\"expiresAt\":\"2026-09-03T15:59:59Z\",\"connectedEnabled\":true,"
                         "\"checkoutRuleMode\":\"service\"}",
                         now),
          "stale capability selects Legacy");
    check(!router.Accept("{\"schemaVersion\":1,\"configurationVersion\":\"v1\","
                         "\"evaluatedAt\":\"2026-09-03T16:00:01Z\","
                         "\"expiresAt\":\"2026-09-03T16:00:30Z\",\"connectedEnabled\":true,"
                         "\"checkoutRuleMode\":\"service\"}",
                         now),
          "future evaluation selects Legacy");
    configuration.hasConnected = false;
    EndpointRouter unconfigured(configuration);
    check(unconfigured.Mode(now) == ClientRuleMode::Legacy, "unconfigured client preserves Legacy");
}

// Confirms the native adapter preserves every stable reason and the compare body carries one
// observation without an idempotency key or checkout command.
static void checkCompareCheckout()
{
    const NativeCheckoutObservation allowed = ObserveNativeCheckout(true, false, 0, L"STANDARD");
    check(allowed.allowed && allowed.reason == "ALLOWED", "native adapter maps allowed");
    check(ObserveNativeCheckout(false, false, 0, L"STANDARD").reason == "MEMBER_INACTIVE",
          "native adapter maps inactive");
    check(ObserveNativeCheckout(true, true, 0, L"STANDARD").reason == "OVERDUE",
          "native adapter maps overdue");
    check(ObserveNativeCheckout(true, false, 2, L"STANDARD").reason == "CHECKOUT_LIMIT_REACHED",
          "native adapter maps limit");
    check(ObserveNativeCheckout(true, false, 0, L"UNKNOWN").reason == "TIER_UNSUPPORTED",
          "native adapter maps tier");
    const std::string request =
        BuildCompareDecisionRequest(L"7", L"2026-09-04", L"configuration-1", allowed);
    check(request.find("\"memberId\":7") != std::string::npos, "compare request contains member");
    check(request.find("\"capabilityConfigurationVersion\":\"configuration-1\"") !=
              std::string::npos,
          "compare request contains capability version");
    check(request.find("\"legacyObservation\":{\"contractVersion\":1,\"allowed\":true,"
                       "\"reason\":\"ALLOWED\"}") != std::string::npos,
          "compare request contains one native observation");
    check(request.find("idempotency") == std::string::npos,
          "compare request contains no idempotency key");
    check(request.find("toolId") == std::string::npos,
          "compare request cannot submit checkout command");
}

// Proves service mode bypasses NativeRules, sends no client policy observation, and accepts only
// the versioned service reason table used for operator feedback.
static void checkServiceCheckout()
{
    check(RequiresNativeCheckoutDecision(ClientRuleMode::Legacy), "Legacy mode calls NativeRules");
    check(RequiresNativeCheckoutDecision(ClientRuleMode::Compare),
          "compare mode calls NativeRules");
    check(!RequiresNativeCheckoutDecision(ClientRuleMode::Service),
          "service mode bypasses NativeRules");

    const std::string request =
        BuildServiceDecisionRequest(L"7", L"2026-09-05", L"configuration-1");
    check(request.find("\"memberId\":7") != std::string::npos, "service request contains member");
    check(request.find("legacyObservation") == std::string::npos,
          "service request contains no native observation");
    check(request.find("active") == std::string::npos &&
              request.find("openLoans") == std::string::npos &&
              request.find("tier") == std::string::npos,
          "service request contains no member policy fields");
    check(request.find("idempotency") == std::string::npos &&
              request.find("toolId") == std::string::npos,
          "service decision cannot submit a checkout command");

    const wchar_t *denials[] = {L"MEMBER_NOT_FOUND", L"MEMBER_INACTIVE",
                                L"OVERDUE",          L"CHECKOUT_LIMIT_REACHED",
                                L"DUE_DATE_INVALID", L"TIER_UNSUPPORTED"};
    check(IsValidServiceDecision(1, L"service", true, L"ALLOWED", L"configuration-1",
                                 L"configuration-1"),
          "service allow accepted");
    for (const wchar_t *reason : denials)
    {
        check(IsValidServiceDecision(1, L"service", false, reason, L"configuration-1",
                                     L"configuration-1"),
              "stable service denial accepted");
        check(ServiceDecisionMessage(reason) != nullptr, "stable service denial has UI text");
    }
    check(std::wstring(ServiceDecisionMessage(L"MEMBER_NOT_FOUND")) == L"The member was not found.",
          "member-not-found message is stable");
    check(std::wstring(ServiceDecisionMessage(L"OVERDUE")) ==
              L"The member must return overdue tools before another checkout.",
          "overdue message is stable");
    check(!IsValidServiceDecision(1, L"service", false, L"ALLOWED", L"configuration-1",
                                  L"configuration-1"),
          "contradictory service allow rejected");
    check(!IsValidServiceDecision(1, L"service", true, L"OVERDUE", L"configuration-1",
                                  L"configuration-1"),
          "contradictory service denial rejected");
    check(!IsValidServiceDecision(1, L"compare", true, L"ALLOWED", L"configuration-1",
                                  L"configuration-1"),
          "non-service response rejected");
    check(!IsValidServiceDecision(2, L"service", true, L"ALLOWED", L"configuration-1",
                                  L"configuration-1"),
          "unsupported service contract rejected");
    check(!IsValidServiceDecision(1, L"service", true, L"ALLOWED", L"stale", L"configuration-1"),
          "stale service configuration rejected");
    check(ServiceDecisionMessage(L"UNKNOWN") == nullptr, "unknown service reason has no UI text");
    check(IsPositiveCheckoutId(L"1") && IsPositiveCheckoutId(L"2147483647"),
          "positive checkout IDs accepted");
    check(!IsPositiveCheckoutId(L"0") && !IsPositiveCheckoutId(L"1x") &&
              !IsPositiveCheckoutId(L"2147483648"),
          "invalid checkout IDs rejected");
    check(IsCheckoutDate(L"2026-09-05"), "calendar checkout date accepted");
    check(!IsCheckoutDate(L"2026-02-29") && !IsCheckoutDate(L"2026/09/05"),
          "invalid checkout dates rejected");
}

// Runs every native rule and client transport contract check.
int main()
{
    checkTransportConfiguration();
    checkFailurePresentation();
    checkCapabilityRouting();
    checkCompareCheckout();
    checkServiceCheckout();
    check(CheckoutLimit(L"STANDARD") == 2, "standard limit");
    check(CheckoutLimit(L"SUPPORTER") == 5, "supporter limit");
    check(CheckoutLimit(L"STAFF") == 10, "staff limit");
    check(CheckoutLimit(L"UNKNOWN") == 0, "unknown tier limit");
    check(MaximumLoanDays(L"STANDARD") == 7, "standard duration");
    check(MaximumLoanDays(L"SUPPORTER") == 14, "supporter duration");
    check(MaximumLoanDays(L"STAFF") == 30, "staff duration");
    check(MaximumLoanDays(L"UNKNOWN") == 0, "unknown tier duration");
    check(IsEligibleForCheckout(1, 0, 0, L"STANDARD") == 1, "standard eligible with no open loans");
    check(IsEligibleForCheckout(1, 0, 1, L"STANDARD") == 1, "standard eligible below limit");
    check(IsEligibleForCheckout(1, 1, 0, L"STANDARD") == 0, "overdue blocked");
    check(IsEligibleForCheckout(1, 0, 2, L"STANDARD") == 0, "standard blocked at limit");
    check(IsEligibleForCheckout(1, 0, 3, L"STANDARD") == 0, "standard blocked above limit");
    check(IsEligibleForCheckout(1, 0, 4, L"SUPPORTER") == 1, "supporter eligible below limit");
    check(IsEligibleForCheckout(1, 0, 5, L"SUPPORTER") == 0, "supporter blocked at limit");
    check(IsEligibleForCheckout(1, 0, 9, L"STAFF") == 1, "staff eligible below limit");
    check(IsEligibleForCheckout(1, 0, 10, L"STAFF") == 0, "staff blocked at limit");
    check(IsEligibleForCheckout(0, 0, 0, L"STAFF") == 0, "inactive blocked");
    check(IsEligibleForCheckout(1, 0, 0, L"UNKNOWN") == 0, "unknown tier blocked");

    checkEligibility(1, 0, 0, L"STANDARD", NR_ALLOWED, "structured allowed");
    checkEligibility(1, 0, 1, L"STANDARD", NR_ALLOWED, "structured standard below limit");
    checkEligibility(0, 0, 0, L"STANDARD", NR_INACTIVE, "structured inactive");
    checkEligibility(0, 0, 0, L"STAFF", NR_INACTIVE, "structured inactive staff");
    checkEligibility(1, 1, 0, L"STANDARD", NR_OVERDUE, "structured overdue");
    checkEligibility(1, 0, 2, L"STANDARD", NR_CHECKOUT_LIMIT_REACHED,
                     "structured standard at limit");
    checkEligibility(1, 0, 3, L"STANDARD", NR_CHECKOUT_LIMIT_REACHED,
                     "structured standard above limit");
    checkEligibility(1, 0, 5, L"SUPPORTER", NR_CHECKOUT_LIMIT_REACHED,
                     "structured supporter at limit");
    checkEligibility(1, 0, 4, L"SUPPORTER", NR_ALLOWED, "structured supporter below limit");
    checkEligibility(1, 0, 10, L"STAFF", NR_CHECKOUT_LIMIT_REACHED, "structured staff at limit");
    checkEligibility(1, 0, 9, L"STAFF", NR_ALLOWED, "structured staff below limit");
    checkEligibility(1, 0, 0, L"UNKNOWN", NR_TIER_UNSUPPORTED, "structured unknown tier");
    checkEligibility(1, 0, 0, nullptr, NR_TIER_UNSUPPORTED, "structured null tier");

    checkEligibility(0, 1, 2, L"STANDARD", NR_INACTIVE, "inactive reason has precedence");
    checkEligibility(1, 1, 0, L"UNKNOWN", NR_TIER_UNSUPPORTED,
                     "unsupported tier reason has precedence over overdue");
    std::cout << (failures ? "Native and transport tests failed\n"
                           : "Native and transport tests passed\n");
    return failures ? 1 : 0;
}
