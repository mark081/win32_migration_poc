// PURPOSE
//
// This file implements the Win32 client's short-lived service capability cache and endpoint
// router. It contains only routing safeguards needed in the Connected stage. Business eligibility,
// authorization, workflow writes, and durable state remain in the service and PostgreSQL.

#include "CapabilityRouter.h"

#include <windows.h>

#include <cctype>
#include <climits>
#include <cstdlib>

namespace
{
// Finds one JSON member value after optional whitespace. Duplicate members are rejected so a
// tampered response cannot depend on parser ordering. Strings with escapes are rejected because
// capability values use a deliberately small ASCII contract.
bool JsonValue(const std::string &json, const char *name, std::string &value, bool &quoted)
{
    const std::string key = "\"" + std::string(name) + "\"";
    const std::string::size_type keyAt = json.find(key);
    if (keyAt == std::string::npos || json.find(key, keyAt + key.size()) != std::string::npos)
        return false;
    std::string::size_type at = json.find(':', keyAt + key.size());
    if (at == std::string::npos)
        return false;
    do
        ++at;
    while (at < json.size() && std::isspace(static_cast<unsigned char>(json[at])));
    if (at >= json.size())
        return false;
    quoted = json[at] == '"';
    if (quoted)
    {
        const std::string::size_type end = json.find('"', at + 1);
        if (end == std::string::npos || json.find('\\', at + 1) < end)
            return false;
        value = json.substr(at + 1, end - at - 1);
        return true;
    }
    const std::string::size_type end = json.find_first_of(",}", at);
    if (end == std::string::npos)
        return false;
    value = json.substr(at, end - at);
    while (!value.empty() && std::isspace(static_cast<unsigned char>(value.back())))
        value.pop_back();
    return !value.empty();
}

// Converts one strict integer JSON value without accepting quoted or partial input.
bool JsonInteger(const std::string &json, const char *name, int &value)
{
    std::string text;
    bool quoted = false;
    if (!JsonValue(json, name, text, quoted) || quoted || text.empty())
        return false;
    char *end = nullptr;
    const long parsed = std::strtol(text.c_str(), &end, 10);
    if (!end || *end || parsed < INT_MIN || parsed > INT_MAX)
        return false;
    value = static_cast<int>(parsed);
    return true;
}

// Converts one strict JSON boolean.
bool JsonBoolean(const std::string &json, const char *name, bool &value)
{
    std::string text;
    bool quoted = false;
    if (!JsonValue(json, name, text, quoted) || quoted || (text != "true" && text != "false"))
        return false;
    value = text == "true";
    return true;
}

// Converts the service's UTC ISO-8601 timestamp to epoch seconds. Fractional seconds are accepted
// but ignored because cache routing needs only whole-second precision.
bool ParseUtc(const std::string &text, std::time_t &result)
{
    if (text.size() < 20 || text[4] != '-' || text[7] != '-' || text[10] != 'T' ||
        text[13] != ':' || text[16] != ':' || text.back() != 'Z')
        return false;
    SYSTEMTIME time = {};
    char tail = 0;
    if (sscanf_s(text.c_str(), "%hu-%hu-%huT%hu:%hu:%hu%c", &time.wYear, &time.wMonth, &time.wDay,
                 &time.wHour, &time.wMinute, &time.wSecond, &tail, 1) != 7 ||
        (tail != 'Z' && tail != '.'))
        return false;
    FILETIME fileTime = {};
    if (!SystemTimeToFileTime(&time, &fileTime))
        return false;
    ULARGE_INTEGER ticks = {};
    ticks.LowPart = fileTime.dwLowDateTime;
    ticks.HighPart = fileTime.dwHighDateTime;
    const unsigned long long epochTicks = 116444736000000000ULL;
    if (ticks.QuadPart < epochTicks)
        return false;
    result = static_cast<std::time_t>((ticks.QuadPart - epochTicks) / 10000000ULL);
    return true;
}

// Accepts only bounded printable ASCII version identifiers suitable for later decision requests.
bool ValidConfigurationVersion(const std::string &value)
{
    if (value.empty() || value.size() > 128)
        return false;
    for (unsigned char c : value)
        if (c < 0x21 || c > 0x7e)
            return false;
    return true;
}
} // namespace

// Captures the endpoint configuration used for every refresh and route decision.
EndpointRouter::EndpointRouter(const ClientEndpointConfiguration &configuration)
    : configuration_(configuration)
{
}

// Refreshes only an absent/expired cache. The capability GET is safe to retry on a later call and
// cannot itself authorize a workflow operation.
void EndpointRouter::Refresh(std::time_t now)
{
    if (!configuration_.hasConnected || (cache_.valid && now < cache_.expiresAt))
        return;
    cache_ = CapabilityCache();
    const ClientHttpResult response =
        SendClientHttp(configuration_, configuration_.connected, L"GET", L"/api/v1/capabilities",
                       configuration_.connectedApiKey);
    if (response.failure == TransportFailure::None)
        Accept(response.body, now);
}

// Validates the complete routing subset before replacing the current cache atomically.
bool EndpointRouter::Accept(const std::string &json, std::time_t now)
{
    CapabilityCache candidate;
    int schema = 0;
    bool enabled = false;
    std::string version;
    std::string evaluated;
    std::string expires;
    std::string mode;
    bool quoted = false;
    if (!JsonInteger(json, "schemaVersion", schema) || schema != 1 ||
        !JsonBoolean(json, "connectedEnabled", enabled) || !enabled ||
        !JsonValue(json, "configurationVersion", version, quoted) || !quoted ||
        !ValidConfigurationVersion(version) || !JsonValue(json, "evaluatedAt", evaluated, quoted) ||
        !quoted || !JsonValue(json, "expiresAt", expires, quoted) || !quoted ||
        !JsonValue(json, "checkoutRuleMode", mode, quoted) || !quoted ||
        !ParseUtc(evaluated, candidate.evaluatedAt) || !ParseUtc(expires, candidate.expiresAt) ||
        candidate.evaluatedAt > now || now >= candidate.expiresAt ||
        candidate.expiresAt <= candidate.evaluatedAt ||
        candidate.expiresAt - candidate.evaluatedAt > 300 ||
        (mode != "compare" && mode != "service"))
    {
        cache_ = CapabilityCache();
        return false;
    }
    candidate.mode = mode == "compare" ? ClientRuleMode::Compare : ClientRuleMode::Service;
    candidate.configurationVersion.assign(version.begin(), version.end());
    candidate.valid = true;
    cache_ = candidate;
    return true;
}

// Chooses the HTTPS Connected endpoint only from a current validated cache entry.
const ClientEndpoint &EndpointRouter::Endpoint(std::time_t now) const
{
    return Mode(now) == ClientRuleMode::Legacy ? configuration_.legacy : configuration_.connected;
}

// Keeps each endpoint paired with its externally supplied credential.
const std::wstring &EndpointRouter::ApiKey(std::time_t now, const std::wstring &legacyApiKey) const
{
    return Mode(now) == ClientRuleMode::Legacy ? legacyApiKey : configuration_.connectedApiKey;
}

// Applies cache expiry at every route lookup so rollback needs no process restart.
ClientRuleMode EndpointRouter::Mode(std::time_t now) const
{
    return configuration_.hasConnected && cache_.valid && now < cache_.expiresAt
               ? cache_.mode
               : ClientRuleMode::Legacy;
}

// Returns UTC epoch seconds using the Windows system clock.
std::time_t ClientUtcNow()
{
    return std::time(nullptr);
}
