// PURPOSE
//
// This file parses external Legacy and Connected endpoint configuration and performs bounded
// WinHTTP requests for the desktop client. Windows keeps its normal TLS chain and hostname checks;
// this code never installs a validation bypass. It does not select Connected routing, interpret
// capabilities, make business decisions, or create idempotency keys.

#include "ClientTransport.h"

#include <winhttp.h>

// Maps HTTP status codes into the failure categories shown by later UI routing.
TransportFailure ClassifyClientHttpStatus(DWORD status)
{
    if (status >= 200 && status < 300)
        return TransportFailure::None;
    if (status == 400 || status == 404 || status == 405 || status == 422)
        return TransportFailure::Validation;
    if (status == 401)
        return TransportFailure::Authentication;
    if (status == 403)
        return TransportFailure::Authorization;
    if (status == 409)
        return TransportFailure::Conflict;
    if (status == 408 || status == 504)
        return TransportFailure::Timeout;
    if (status == 502 || status == 503)
        return TransportFailure::Unavailable;
    return TransportFailure::Unexpected;
}

namespace
{
// Closes one WinHTTP handle at scope exit so every failure path releases Windows resources.
class InternetHandle
{
  public:
    // Takes ownership of a handle returned by WinHTTP; null is permitted.
    explicit InternetHandle(HINTERNET value = nullptr) : value_(value) {}

    // Releases the owned handle once.
    ~InternetHandle()
    {
        if (value_)
            WinHttpCloseHandle(value_);
    }

    // Returns the borrowed handle for WinHTTP calls.
    operator HINTERNET() const
    {
        return value_;
    }

    // Prevents two objects from closing the same Windows handle.
    InternetHandle(const InternetHandle &) = delete;
    InternetHandle &operator=(const InternetHandle &) = delete;

  private:
    HINTERNET value_;
};

// Reads one process environment setting without retaining its name or value in diagnostics.
std::wstring EnvironmentValue(const wchar_t *name)
{
    const DWORD size = GetEnvironmentVariableW(name, nullptr, 0);
    if (!size)
        return L"";
    std::wstring value(size, L'\0');
    if (!GetEnvironmentVariableW(name, &value[0], size))
        return L"";
    value.resize(size - 1);
    return value;
}

// Reads one bounded UTF-8 Connected credential file. The value stays in process memory and never
// enters an error string or diagnostic record.
bool ReadConnectedCredential(std::wstring &apiKey)
{
    const std::wstring path = EnvironmentValue(L"TOOL_LENDING_CONNECTED_CREDENTIAL_FILE");
    if (path.empty())
        return false;
    HANDLE file = CreateFileW(path.c_str(), GENERIC_READ, FILE_SHARE_READ, nullptr, OPEN_EXISTING,
                              FILE_ATTRIBUTE_NORMAL, nullptr);
    if (file == INVALID_HANDLE_VALUE)
        return false;
    const DWORD size = GetFileSize(file, nullptr);
    if (size == INVALID_FILE_SIZE || size == 0 || size > 4096)
    {
        CloseHandle(file);
        return false;
    }
    std::string value(size, '\0');
    DWORD read = 0;
    const BOOL succeeded = ReadFile(file, &value[0], size, &read, nullptr);
    CloseHandle(file);
    if (!succeeded || read != size)
        return false;
    if (value.compare(0, 3, "\xEF\xBB\xBF") == 0)
        value.erase(0, 3);
    const std::string::size_type first = value.find_first_not_of(" \t\r\n");
    const std::string::size_type last = value.find_last_not_of(" \t\r\n");
    if (first == std::string::npos)
        return false;
    value = value.substr(first, last - first + 1);
    const int wideSize = MultiByteToWideChar(CP_UTF8, MB_ERR_INVALID_CHARS, value.data(),
                                             static_cast<int>(value.size()), nullptr, 0);
    if (!wideSize)
        return false;
    apiKey.assign(wideSize, L'\0');
    return MultiByteToWideChar(CP_UTF8, MB_ERR_INVALID_CHARS, value.data(),
                               static_cast<int>(value.size()), &apiKey[0], wideSize) == wideSize;
}

// Parses one absolute HTTP URL. Connected callers separately require the secure scheme.
bool ParseEndpoint(const std::wstring &value, ClientEndpoint &endpoint)
{
    URL_COMPONENTS parts = {};
    parts.dwStructSize = sizeof(parts);
    parts.dwSchemeLength = static_cast<DWORD>(-1);
    parts.dwHostNameLength = static_cast<DWORD>(-1);
    parts.dwUserNameLength = static_cast<DWORD>(-1);
    parts.dwPasswordLength = static_cast<DWORD>(-1);
    parts.dwUrlPathLength = static_cast<DWORD>(-1);
    parts.dwExtraInfoLength = static_cast<DWORD>(-1);
    if (!WinHttpCrackUrl(value.c_str(), static_cast<DWORD>(value.size()), 0, &parts))
        return false;
    if (parts.nScheme != INTERNET_SCHEME_HTTP && parts.nScheme != INTERNET_SCHEME_HTTPS)
        return false;
    if (!parts.lpszHostName || parts.dwHostNameLength == 0)
        return false;
    if (parts.dwUserNameLength || parts.dwPasswordLength || parts.dwExtraInfoLength)
        return false;

    endpoint.host.assign(parts.lpszHostName, parts.dwHostNameLength);
    endpoint.port = parts.nPort;
    endpoint.basePath =
        parts.dwUrlPathLength ? std::wstring(parts.lpszUrlPath, parts.dwUrlPathLength) : L"/";
    if (endpoint.basePath.back() == L'/')
        endpoint.basePath.pop_back();
    endpoint.secure = parts.nScheme == INTERNET_SCHEME_HTTPS;
    return true;
}

// Reads a bounded positive millisecond value or retains the documented default when absent.
bool ReadTimeout(const wchar_t *name, int minimum, int maximum, int &value)
{
    const std::wstring text = EnvironmentValue(name);
    if (text.empty())
        return true;
    wchar_t *end = nullptr;
    const long parsed = wcstol(text.c_str(), &end, 10);
    if (!end || *end != L'\0' || parsed < minimum || parsed > maximum)
        return false;
    value = static_cast<int>(parsed);
    return true;
}

// Maps WinHTTP errors without weakening certificate or hostname failures into availability.
TransportFailure ClassifySystemError(DWORD error)
{
    if (error == ERROR_WINHTTP_TIMEOUT)
        return TransportFailure::Timeout;
    if (error == ERROR_WINHTTP_CANNOT_CONNECT || error == ERROR_WINHTTP_CONNECTION_ERROR ||
        error == ERROR_WINHTTP_NAME_NOT_RESOLVED || error == ERROR_WINHTTP_RESEND_REQUEST)
        return TransportFailure::Unavailable;
    return TransportFailure::Unexpected;
}

// Returns true only for one bounded replay of a keyed write after an ambiguous network failure.
bool MayReplay(const std::wstring &key, TransportFailure failure)
{
    return !key.empty() &&
           (failure == TransportFailure::Timeout || failure == TransportFailure::Unavailable);
}

// Performs one WinHTTP attempt. Default Windows TLS validation remains enabled.
ClientHttpResult SendAttempt(const ClientEndpointConfiguration &configuration,
                             const ClientEndpoint &endpoint, const wchar_t *verb,
                             const std::wstring &path, const std::wstring &apiKey,
                             const std::string &body, const std::wstring &key)
{
    ClientHttpResult result;
    InternetHandle session(WinHttpOpen(L"ToolLendingClient/1.0", WINHTTP_ACCESS_TYPE_NO_PROXY,
                                       WINHTTP_NO_PROXY_NAME, WINHTTP_NO_PROXY_BYPASS, 0));
    if (!session)
    {
        result.systemError = GetLastError();
        result.failure = ClassifySystemError(result.systemError);
        return result;
    }
    if (!WinHttpSetTimeouts(session, configuration.resolveTimeoutMs, configuration.connectTimeoutMs,
                            configuration.sendTimeoutMs, configuration.receiveTimeoutMs))
    {
        result.systemError = GetLastError();
        result.failure = TransportFailure::Configuration;
        return result;
    }

    InternetHandle connect(WinHttpConnect(session, endpoint.host.c_str(), endpoint.port, 0));
    if (!connect)
    {
        result.systemError = GetLastError();
        result.failure = ClassifySystemError(result.systemError);
        return result;
    }
    std::wstring requestPath = endpoint.basePath + path;
    InternetHandle request(WinHttpOpenRequest(connect, verb, requestPath.c_str(), nullptr,
                                              WINHTTP_NO_REFERER, WINHTTP_DEFAULT_ACCEPT_TYPES,
                                              endpoint.secure ? WINHTTP_FLAG_SECURE : 0));
    if (!request)
    {
        result.systemError = GetLastError();
        result.failure = ClassifySystemError(result.systemError);
        return result;
    }

    std::wstring headers = L"X-Api-Key: " + apiKey + L"\r\nX-Actor: legacy.desktop\r\n";
    if (!key.empty())
        headers += L"Idempotency-Key: " + key + L"\r\n";
    if (!body.empty())
        headers += L"Content-Type: application/json\r\n";
    const BOOL sent =
        WinHttpSendRequest(request, headers.c_str(), static_cast<DWORD>(-1L),
                           body.empty() ? WINHTTP_NO_REQUEST_DATA : const_cast<char *>(body.data()),
                           static_cast<DWORD>(body.size()), static_cast<DWORD>(body.size()), 0);
    if (!sent || !WinHttpReceiveResponse(request, nullptr))
    {
        result.systemError = GetLastError();
        result.failure = ClassifySystemError(result.systemError);
        return result;
    }

    DWORD statusSize = sizeof(result.statusCode);
    WinHttpQueryHeaders(request, WINHTTP_QUERY_STATUS_CODE | WINHTTP_QUERY_FLAG_NUMBER,
                        WINHTTP_HEADER_NAME_BY_INDEX, &result.statusCode, &statusSize,
                        WINHTTP_NO_HEADER_INDEX);
    result.failure = ClassifyClientHttpStatus(result.statusCode);

    DWORD available = 0;
    while (WinHttpQueryDataAvailable(request, &available) && available)
    {
        std::string part(available, '\0');
        DWORD read = 0;
        if (!WinHttpReadData(request, &part[0], available, &read))
        {
            result.systemError = GetLastError();
            result.failure = ClassifySystemError(result.systemError);
            return result;
        }
        part.resize(read);
        result.body += part;
    }
    return result;
}

// Names one stable failure category for the existing text UI.
const wchar_t *FailureName(TransportFailure failure)
{
    switch (failure)
    {
    case TransportFailure::Configuration:
        return L"configuration";
    case TransportFailure::Timeout:
        return L"timeout";
    case TransportFailure::Unavailable:
        return L"service unavailable";
    case TransportFailure::Authentication:
        return L"authentication";
    case TransportFailure::Authorization:
        return L"authorization";
    case TransportFailure::Validation:
        return L"validation";
    case TransportFailure::Conflict:
        return L"conflict";
    case TransportFailure::Unexpected:
        return L"unexpected";
    default:
        return L"none";
    }
}
} // namespace

// Loads Legacy/Connected URLs and bounded timeouts from process environment variables.
bool LoadClientEndpointConfiguration(ClientEndpointConfiguration &configuration,
                                     std::wstring &error)
{
    std::wstring legacy = EnvironmentValue(L"TOOL_LENDING_LEGACY_BASE_URL");
    if (legacy.empty())
        legacy = L"http://localhost:8088/";
    if (!ParseEndpoint(legacy, configuration.legacy))
    {
        error = L"TOOL_LENDING_LEGACY_BASE_URL must be an absolute HTTP or HTTPS URL.";
        return false;
    }

    const std::wstring connected = EnvironmentValue(L"TOOL_LENDING_CONNECTED_BASE_URL");
    if (!connected.empty())
    {
        if (!ParseEndpoint(connected, configuration.connected) || !configuration.connected.secure)
        {
            error = L"TOOL_LENDING_CONNECTED_BASE_URL must be an absolute HTTPS URL.";
            return false;
        }
        configuration.hasConnected = true;
        if (!ReadConnectedCredential(configuration.connectedApiKey))
        {
            error = L"A non-empty TOOL_LENDING_CONNECTED_CREDENTIAL_FILE is required when the "
                    L"Connected endpoint is configured.";
            return false;
        }
    }

    if (!ReadTimeout(L"TOOL_LENDING_RESOLVE_TIMEOUT_MS", 100, 60000,
                     configuration.resolveTimeoutMs) ||
        !ReadTimeout(L"TOOL_LENDING_CONNECT_TIMEOUT_MS", 100, 60000,
                     configuration.connectTimeoutMs) ||
        !ReadTimeout(L"TOOL_LENDING_SEND_TIMEOUT_MS", 100, 120000, configuration.sendTimeoutMs) ||
        !ReadTimeout(L"TOOL_LENDING_RECEIVE_TIMEOUT_MS", 100, 120000,
                     configuration.receiveTimeoutMs))
    {
        error = L"Tool Lending HTTP timeout values must be bounded positive milliseconds.";
        return false;
    }
    return true;
}

// Sends one request and replays a keyed ambiguous write once with the same key.
ClientHttpResult SendClientHttp(const ClientEndpointConfiguration &configuration,
                                const ClientEndpoint &endpoint, const wchar_t *verb,
                                const std::wstring &path, const std::wstring &apiKey,
                                const std::string &body, const std::wstring &key)
{
    ClientHttpResult result = SendAttempt(configuration, endpoint, verb, path, apiKey, body, key);
    if (MayReplay(key, result.failure))
    {
        Sleep(50 + (GetTickCount() % 100));
        result = SendAttempt(configuration, endpoint, verb, path, apiKey, body, key);
    }
    return result;
}

// Formats a successful body or a stable failure category and non-sensitive numeric detail.
std::wstring FormatClientHttpResult(const ClientHttpResult &result)
{
    if (result.failure == TransportFailure::None)
    {
        if (result.body.empty())
            return L"";
        const int size = MultiByteToWideChar(CP_UTF8, 0, result.body.data(),
                                             static_cast<int>(result.body.size()), nullptr, 0);
        std::wstring wide(size, L'\0');
        MultiByteToWideChar(CP_UTF8, 0, result.body.data(), static_cast<int>(result.body.size()),
                            &wide[0], size);
        return wide;
    }
    return L"ERROR: " + std::wstring(FailureName(result.failure)) + L" (" +
           std::to_wstring(result.statusCode ? result.statusCode : result.systemError) + L")";
}
