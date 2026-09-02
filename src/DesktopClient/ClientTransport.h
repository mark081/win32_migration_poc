// PURPOSE
//
// This file defines the Win32 client's externally configured HTTP endpoint and failure contracts.
// ClientTransport.cpp owns URL parsing and WinHTTP calls. UI routing and business rules belong in
// main.cpp and later capability-router code. The Connected endpoint is configuration only here and
// cannot become active until a service capability permits it.

#pragma once

#include <windows.h>
#include <winhttp.h>
#include <string>

// Identifies stable transport outcomes that the desktop can map to operator-safe messages.
enum class TransportFailure
{
    None,
    Configuration,
    Timeout,
    Unavailable,
    Authentication,
    Authorization,
    Validation,
    Conflict,
    Unexpected
};

// Holds a parsed HTTP origin and optional base path used by WinHttpConnect and requests.
struct ClientEndpoint
{
    std::wstring host;
    INTERNET_PORT port = 0;
    std::wstring basePath;
    bool secure = false;
};

// Holds Legacy and optional Connected endpoints plus bounded WinHTTP timeout values.
struct ClientEndpointConfiguration
{
    ClientEndpoint legacy;
    ClientEndpoint connected;
    std::wstring connectedApiKey;
    bool hasConnected = false;
    int resolveTimeoutMs = 5000;
    int connectTimeoutMs = 5000;
    int sendTimeoutMs = 10000;
    int receiveTimeoutMs = 15000;
};

// Returns an HTTP body/status or a classified failure. It never contains credentials.
struct ClientHttpResult
{
    TransportFailure failure = TransportFailure::None;
    DWORD systemError = ERROR_SUCCESS;
    DWORD statusCode = 0;
    std::string body;
};

// Maps an HTTP response status to the stable desktop failure contract.
TransportFailure ClassifyClientHttpStatus(DWORD status);

// Loads and validates external endpoint and timeout settings. Legacy defaults to localhost;
// Connected, when present, must be HTTPS. Invalid values fail startup without network access.
bool LoadClientEndpointConfiguration(ClientEndpointConfiguration &configuration,
                                     std::wstring &error);

// Sends one request with normal WinHTTP certificate and hostname validation. A retryable ambiguous
// write is replayed at most once with the exact same caller-supplied idempotency key.
ClientHttpResult SendClientHttp(const ClientEndpointConfiguration &configuration,
                                const ClientEndpoint &endpoint, const wchar_t *verb,
                                const std::wstring &path, const std::wstring &apiKey,
                                const std::string &body = "", const std::wstring &key = L"");

// Converts a transport result into the existing UI text form without exposing secret material.
std::wstring FormatClientHttpResult(const ClientHttpResult &result);
