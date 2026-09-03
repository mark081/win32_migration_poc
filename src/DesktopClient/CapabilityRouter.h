// PURPOSE
//
// This file defines the desktop's in-memory cache for service-authored Connected routing.
// CapabilityRouter.cpp parses the small version 1 response and chooses an endpoint. It never
// authorizes business work or changes checkout results; the service and PostgreSQL retain those
// decisions. Invalid, unsupported, missing, or expired data always selects Legacy.

#pragma once

#include "ClientTransport.h"

#include <ctime>
#include <string>

// Names the only checkout routing modes understood by this client version.
enum class ClientRuleMode
{
    Legacy,
    Compare,
    Service
};

// Retains one validated service capability until its UTC expiry time or process exit.
struct CapabilityCache
{
    ClientRuleMode mode = ClientRuleMode::Legacy;
    std::wstring configurationVersion;
    std::time_t evaluatedAt = 0;
    std::time_t expiresAt = 0;
    bool valid = false;
};

// Parses, caches, refreshes, and applies service routing metadata. Callers still submit business
// requests through ClientTransport, and the server rechecks capability for every decision.
class EndpointRouter
{
  public:
    // Uses the validated endpoint configuration without taking ownership of credential storage.
    explicit EndpointRouter(const ClientEndpointConfiguration &configuration);

    // Fetches a capability only when Connected is configured and the cache is no longer current.
    // Failures clear the cache and therefore restore Legacy routing immediately.
    void Refresh(std::time_t now);

    // Parses a response for tests and bootstrap. It accepts schema 1, a known mode, parent true,
    // and a lifetime no longer than five minutes; every other input clears the cache.
    bool Accept(const std::string &json, std::time_t now);

    // Returns Connected only while a current compare/service capability is cached.
    const ClientEndpoint &Endpoint(std::time_t now) const;

    // Returns the credential paired with the selected endpoint without copying it to diagnostics.
    const std::wstring &ApiKey(std::time_t now, const std::wstring &legacyApiKey) const;

    // Reports the effective routing mode; stale cache state is always reported as Legacy.
    ClientRuleMode Mode(std::time_t now) const;

  private:
    const ClientEndpointConfiguration &configuration_;
    CapabilityCache cache_;
};

// Returns current UTC epoch seconds for cache comparisons and deterministic test injection.
std::time_t ClientUtcNow();
