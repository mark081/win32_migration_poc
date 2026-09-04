// PURPOSE
//
// This file implements the checkout input, native-result, service-contract, and safe-message
// helpers used by the Connected migration. Network behavior belongs in ClientTransport, routing
// belongs in CapabilityRouter, and final checkout validation and writes remain in PostgreSQL.

#include "CheckoutMode.h"

#include "../NativeRules/NativeRules.h"
#include "CapabilityRouter.h"

#include <windows.h>

#include <climits>

namespace
{
// Converts Win32 UTF-16 input to UTF-8 for the JSON request. Invalid text returns empty and is
// rejected by the service's normal request validation.
std::string Utf8(const std::wstring &value)
{
    if (value.empty())
        return {};
    const int size =
        WideCharToMultiByte(CP_UTF8, WC_ERR_INVALID_CHARS, value.data(),
                            static_cast<int>(value.size()), nullptr, 0, nullptr, nullptr);
    if (!size)
        return {};
    std::string result(size, '\0');
    return WideCharToMultiByte(CP_UTF8, WC_ERR_INVALID_CHARS, value.data(),
                               static_cast<int>(value.size()), &result[0], size, nullptr,
                               nullptr) == size
               ? result
               : std::string();
}
} // namespace

// Produces exactly one native observation and preserves the version 1 reason contract.
NativeCheckoutObservation ObserveNativeCheckout(bool active, bool overdue, int openLoans,
                                                const std::wstring &tier)
{
    NativeCheckoutObservation observation;
    switch (CheckoutEligibilityReasonV1(active ? 1 : 0, overdue ? 1 : 0, openLoans, tier.c_str()))
    {
    case NR_ALLOWED:
        observation.allowed = true;
        observation.reason = "ALLOWED";
        break;
    case NR_INACTIVE:
        observation.reason = "MEMBER_INACTIVE";
        break;
    case NR_OVERDUE:
        observation.reason = "OVERDUE";
        break;
    case NR_CHECKOUT_LIMIT_REACHED:
        observation.reason = "CHECKOUT_LIMIT_REACHED";
        break;
    default:
        observation.reason = "TIER_UNSUPPORTED";
        break;
    }
    return observation;
}

// Builds the exact version 1 body accepted by POST /api/v1/checkout-decisions in compare mode.
std::string BuildCompareDecisionRequest(const std::wstring &memberId, const std::wstring &dueOn,
                                        const std::wstring &configurationVersion,
                                        const NativeCheckoutObservation &observation)
{
    return "{\"memberId\":" + Utf8(memberId) + ",\"dueOn\":\"" + Utf8(dueOn) +
           "\",\"clientVersion\":\"1.0.0\",\"capabilityConfigurationVersion\":\"" +
           Utf8(configurationVersion) +
           "\",\"legacyObservation\":{\"contractVersion\":1,\"allowed\":" +
           (observation.allowed ? "true" : "false") + ",\"reason\":\"" + observation.reason +
           "\"}}";
}

// Keeps the temporary native path limited to Legacy and compare modes.
bool RequiresNativeCheckoutDecision(ClientRuleMode mode)
{
    return mode != ClientRuleMode::Service;
}

// Builds the service-mode decision body without any client-derived eligibility evidence.
std::string BuildServiceDecisionRequest(const std::wstring &memberId, const std::wstring &dueOn,
                                        const std::wstring &configurationVersion)
{
    return "{\"memberId\":" + Utf8(memberId) + ",\"dueOn\":\"" + Utf8(dueOn) +
           "\",\"clientVersion\":\"1.0.0\",\"capabilityConfigurationVersion\":\"" +
           Utf8(configurationVersion) + "\"}";
}

// Rejects stale, malformed, unknown, or contradictory service decision responses.
bool IsValidServiceDecision(int contractVersion, const std::wstring &effectiveMode, bool allowed,
                            const std::wstring &reason, const std::wstring &configurationVersion,
                            const std::wstring &expectedConfigurationVersion)
{
    if (contractVersion != 1 || effectiveMode != L"service" || configurationVersion.empty() ||
        configurationVersion != expectedConfigurationVersion)
        return false;
    if (reason == L"ALLOWED")
        return allowed;
    return !allowed && (reason == L"MEMBER_NOT_FOUND" || reason == L"MEMBER_INACTIVE" ||
                        reason == L"OVERDUE" || reason == L"CHECKOUT_LIMIT_REACHED" ||
                        reason == L"DUE_DATE_INVALID" || reason == L"TIER_UNSUPPORTED");
}

// Provides stable service-denial text without exposing internal response or feature details.
const wchar_t *ServiceDecisionMessage(const std::wstring &reason)
{
    if (reason == L"MEMBER_NOT_FOUND")
        return L"The member was not found.";
    if (reason == L"MEMBER_INACTIVE")
        return L"The member is inactive and cannot check out tools.";
    if (reason == L"OVERDUE")
        return L"The member must return overdue tools before another checkout.";
    if (reason == L"CHECKOUT_LIMIT_REACHED")
        return L"The member has reached the checkout limit for their tier.";
    if (reason == L"DUE_DATE_INVALID")
        return L"Choose a due date within the member tier's allowed loan period.";
    if (reason == L"TIER_UNSUPPORTED")
        return L"The member's tier is not supported for checkout.";
    return nullptr;
}

// Rejects malformed or out-of-range identifiers before hand-built JSON is created.
bool IsPositiveCheckoutId(const std::wstring &value)
{
    if (value.empty() || value.find_first_not_of(L"0123456789") != std::wstring::npos)
        return false;
    wchar_t *end = nullptr;
    const long long parsed = _wcstoi64(value.c_str(), &end, 10);
    return end && *end == L'\0' && parsed > 0 && parsed <= INT_MAX;
}

// Uses the native Windows calendar conversion to reject impossible YYYY-MM-DD dates.
bool IsCheckoutDate(const std::wstring &value)
{
    if (value.size() != 10 || value[4] != L'-' || value[7] != L'-')
        return false;
    for (std::size_t index = 0; index < value.size(); ++index)
        if (index != 4 && index != 7 && (value[index] < L'0' || value[index] > L'9'))
            return false;
    SYSTEMTIME date = {};
    date.wYear = static_cast<WORD>(_wtoi(value.substr(0, 4).c_str()));
    date.wMonth = static_cast<WORD>(_wtoi(value.substr(5, 2).c_str()));
    date.wDay = static_cast<WORD>(_wtoi(value.substr(8, 2).c_str()));
    FILETIME ignored = {};
    return SystemTimeToFileTime(&date, &ignored) != FALSE;
}
