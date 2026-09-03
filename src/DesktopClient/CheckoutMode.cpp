// PURPOSE
//
// This file adapts the versioned native checkout reason to the service comparison contract and
// serializes its bounded request. Network behavior belongs in ClientTransport, routing belongs in
// CapabilityRouter, and final checkout validation and writes remain in PostgreSQL.

#include "CheckoutMode.h"

#include "../NativeRules/NativeRules.h"

#include <windows.h>

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
