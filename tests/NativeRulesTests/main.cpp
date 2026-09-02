// PURPOSE
//
// This test executable protects the NativeRules compatibility contract and the desktop's small
// WinHTTP configuration boundary. It runs without changing workflow data. Network checks use an
// unreachable loopback port and never contact an external service.

#include <iostream>
#include "../../src/NativeRules/NativeRules.h"
#include "../../src/DesktopClient/ClientTransport.h"
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

// Runs every native rule and client transport contract check.
int main()
{
    checkTransportConfiguration();
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
